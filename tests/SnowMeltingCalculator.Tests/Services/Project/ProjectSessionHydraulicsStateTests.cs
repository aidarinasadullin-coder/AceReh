using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public sealed class ProjectSessionHydraulicsStateTests
    {
        private sealed class DirtySpy : IMarkDirtyService
        {
            public int Calls { get; private set; }
            public void MarkDirty() => Calls++;
        }

        private ProjectSessionHydraulicsState _state = null!;
        private DirtySpy _dirty = null!;
        private int _events;
        private HydraulicsStateChangedEventArgs _last = null!;

        [SetUp]
        public void SetUp()
        {
            _dirty = new DirtySpy();
            _state = new ProjectSessionHydraulicsState(_dirty);
            _events = 0;
            _state.Changed += (_, args) => { _events++; _last = args; };
        }

        private static HydraulicGlobalInputsSnapshot Inputs(double concentration = 50, double spacing = 5, double heat = 10) =>
            new(GlycolType.Ethylene, concentration, spacing, heat);

        private static HydraulicCircuitSnapshot Circuit(int number = 1) =>
            new(number, 100 + number, 5, 5, 10, 20);

        private static HydraulicCollectorSnapshot Collector(int number = 1, IEnumerable<HydraulicCircuitSnapshot>? circuits = null) =>
            new(number, "HKV-D", ValveType.HKV_D, circuits ?? new[] { Circuit() });

        private static HydraulicCollectorSummarySnapshot Summary() => new(1, 105, 10, 20, 30, 40, 1.2, "HKV-D");

        [Test]
        public void OriginEnum_IsExactlyTheNineMemberClosedContract()
        {
            // ADR-014: к семичленному контракту добавлены Undo/Redo
            // (memento-дневник отмены; dirty не создают).
            Assert.That(Enum.GetNames<HydraulicsMutationOrigin>(), Is.EqualTo(new[]
            {
                "User", "UserReset", "ProjectLoadReset", "ProjectLoad", "Calculation", "Initialization",
                "SystemApply", "Undo", "Redo"
            }));
        }

        [Test]
        public void ApplyGlobalInputs_Changed_DirtiesAndEmitsOnce()
        {
            var before = _state.Snapshot;
            var result = _state.ApplyGlobalInputs(Inputs(60), HydraulicsMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(HydraulicsMutationStatus.Changed));
            Assert.That(_events, Is.EqualTo(1));
            Assert.That(_dirty.Calls, Is.EqualTo(1));
            Assert.That(_last.OldSnapshot, Is.EqualTo(before));
            Assert.That(_last.NewSnapshot, Is.EqualTo(_state.Snapshot));
            Assert.That(_last.Origin, Is.EqualTo(HydraulicsMutationOrigin.User));
        }

        [Test]
        public void ReplaceCollectors_Changed_StoresDefensiveCollectionCopy()
        {
            var source = new List<HydraulicCollectorSnapshot> { Collector() };
            Assert.That(_state.ReplaceCollectors(source, HydraulicsMutationOrigin.Initialization).IsChanged, Is.True);
            source.Clear();
            Assert.That(_state.Snapshot.Collectors, Has.Count.EqualTo(1));
        }

        [Test]
        public void BeginCompleteAndFailCalculation_UseExpectedPhasesAndResultSubtree()
        {
            Assert.That(_state.BeginCalculation().IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(HydraulicsCalculationPhase.Calculating));
            var result = _state.CompleteCalculation(new[] { Collector() }, new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() });
            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(HydraulicsCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Collectors[0].Summary, Is.EqualTo(Summary()));

            var completedSnapshot = _state.Snapshot;
            var completedEvents = _events;
            Assert.That(_state.FailCalculation("boom").IsRejected, Is.True);
            Assert.That(_state.Snapshot, Is.EqualTo(completedSnapshot));
            Assert.That(_events, Is.EqualTo(completedEvents));

            Assert.That(_state.BeginCalculation().IsChanged, Is.True);
            Assert.That(_state.FailCalculation("boom").IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(HydraulicsCalculationPhase.Error));
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.EqualTo("boom"));

            Assert.That(_state.ApplyGlobalInputs(_state.Snapshot.GlobalInputs, HydraulicsMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(HydraulicsCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.Empty);
        }

        [Test]
        public void Restore_OnlyAcceptsProjectLoad_AndResetRestoresDefaults()
        {
            var restored = new HydraulicsStateSnapshot(Inputs(65), new[] { Collector(2) }, new(HydraulicsCalculationPhase.Error, "saved"));
            Assert.That(_state.Restore(restored, HydraulicsMutationOrigin.User).Status, Is.EqualTo(HydraulicsMutationStatus.Rejected));
            Assert.That(_events, Is.Zero);
            Assert.That(_state.Restore(restored, HydraulicsMutationOrigin.ProjectLoad).IsChanged, Is.True);
            Assert.That(_state.ResetToDefaults(HydraulicsMutationOrigin.ProjectLoadReset).IsChanged, Is.True);
            Assert.That(_state.Snapshot, Is.EqualTo(HydraulicsStateSnapshot.Default));
        }

        [Test]
        public void NoChangeAndRejected_EmitNoEvents()
        {
            Assert.That(_state.ApplyGlobalInputs(Inputs(), HydraulicsMutationOrigin.User).IsNoChange, Is.True);
            Assert.That(_state.ApplyGlobalInputs(new(GlycolType.Ethylene, 101, 5, 10), HydraulicsMutationOrigin.User).IsRejected, Is.True);
            Assert.That(_events, Is.Zero);
            Assert.That(_dirty.Calls, Is.Zero);
        }

        [Test]
        public void SnapshotEquality_IsStructuralAndDetectsFieldChanges()
        {
            var a = new HydraulicsStateSnapshot(Inputs(), new[] { Collector() }, HydraulicsStatusSnapshot.Default);
            var b = new HydraulicsStateSnapshot(Inputs(), new[] { Collector() }, HydraulicsStatusSnapshot.Default);
            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(new HydraulicsStateSnapshot(Inputs(60), b.Collectors, b.Status), Is.Not.EqualTo(a));
            Assert.That(new HydraulicCircuitResultSnapshot(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14), Is.EqualTo(new HydraulicCircuitResultSnapshot(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)));
        }

        [Test]
        public void ReturnedCollectionsAreReadOnly()
        {
            _state.ReplaceCollectors(new[] { Collector() }, HydraulicsMutationOrigin.SystemApply);
            var collectors = _state.Snapshot.Collectors;
            Assert.That(collectors, Is.InstanceOf<IList<HydraulicCollectorSnapshot>>());
            Assert.Throws<NotSupportedException>(() => ((IList<HydraulicCollectorSnapshot>)collectors).Clear());
            Assert.That(_state.Snapshot.Collectors, Has.Count.EqualTo(1));
        }

        [Test]
        public void DirtyIntentMatrix_HasExactlyOneUserIntentAndNoneForOtherOrigins()
        {
            // ADR-014: девять origins, dirty — только User.
            foreach (var origin in Enum.GetValues<HydraulicsMutationOrigin>())
            {
                _state.ApplyGlobalInputs(Inputs(51 + (int)origin), origin);
            }

            Assert.That(_dirty.Calls, Is.EqualTo(1));
            Assert.That(_events, Is.EqualTo(9));
        }

        #region ADR-012: инвалидация результатов при User-мутациях

        private static HydraulicCircuitResultSnapshot Result(double seed = 1) =>
            new(seed, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);

        private static HydraulicCollectorSnapshot CollectorWithResults() =>
            new(1, "HKV-D", ValveType.HKV_D,
                new[]
                {
                    new HydraulicCircuitSnapshot(1, 110, 10, 5, 10, 20, Result(1), Result(2)),
                    new HydraulicCircuitSnapshot(2, 120, 10, 5, 10, 20, Result(3), Result(4))
                },
                Summary());

        private static void AssertResultsCleared(HydraulicsStateSnapshot snapshot)
        {
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Collectors.Select(c => c.Summary), Has.All.Null,
                    "Summary коллекторов обнулён");
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.OperatingResult), Has.All.Null, "OperatingResult обнулён");
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.DesignResult), Has.All.Null, "DesignResult обнулён");
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.CircuitLength), Is.Not.Empty,
                    "введённые длины сохраняются");
            });
        }

        [Test]
        public void ReplaceCollectors_User_ClearsResultsAndSummary()
        {
            _state.Restore(new HydraulicsStateSnapshot(Inputs(), new[] { CollectorWithResults() }, HydraulicsStatusSnapshot.Default), HydraulicsMutationOrigin.ProjectLoad);

            var edited = new[] { new HydraulicCollectorSnapshot(1, "HKV-D", ValveType.HKV_D, new[] { new HydraulicCircuitSnapshot(1, 130, 10, 5, 10, 20) }) };
            var result = _state.ReplaceCollectors(edited, HydraulicsMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            AssertResultsCleared(_state.Snapshot);
        }

        [Test]
        public void ReplaceCollectors_UserReset_ClearsResultsAndSummary()
        {
            _state.Restore(new HydraulicsStateSnapshot(Inputs(), new[] { CollectorWithResults() }, HydraulicsStatusSnapshot.Default), HydraulicsMutationOrigin.ProjectLoad);

            var edited = new[] { new HydraulicCollectorSnapshot(1, "HKV-D", ValveType.HKV_D, new[] { new HydraulicCircuitSnapshot(1, 130, 10, 5, 10, 20) }) };
            _state.ReplaceCollectors(edited, HydraulicsMutationOrigin.UserReset);

            AssertResultsCleared(_state.Snapshot);
        }

        [Test]
        public void ReplaceCollectors_LifecycleOrigins_PreserveResults(
            [Values(
                HydraulicsMutationOrigin.ProjectLoadReset,
                HydraulicsMutationOrigin.ProjectLoad,
                HydraulicsMutationOrigin.Calculation,
                HydraulicsMutationOrigin.Initialization,
                HydraulicsMutationOrigin.SystemApply)] HydraulicsMutationOrigin origin)
        {
            _state.Restore(new HydraulicsStateSnapshot(Inputs(), new[] { CollectorWithResults() }, HydraulicsStatusSnapshot.Default), HydraulicsMutationOrigin.ProjectLoad);

            var edited = new[] { new HydraulicCollectorSnapshot(1, "HKV-D", ValveType.HKV_D, new[] { new HydraulicCircuitSnapshot(1, 130, 10, 5, 10, 20, Result(9), Result(9)) }, Summary()) };
            _state.ReplaceCollectors(edited, origin);

            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.Collectors.Select(c => c.Summary), Has.All.Not.Null,
                    $"{origin}: загрузка/сбросы/расчёт сохраняют результаты");
                Assert.That(_state.Snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.OperatingResult), Has.All.Not.Null, $"{origin}: OperatingResult сохранён");
                Assert.That(_state.Snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.DesignResult), Has.All.Not.Null, $"{origin}: DesignResult сохранён");
            });
        }

        [Test]
        public void FailCalculation_ClearsCircuitResultsAndSummary()
        {
            _state.Restore(new HydraulicsStateSnapshot(Inputs(), new[] { CollectorWithResults() }, HydraulicsStatusSnapshot.Default), HydraulicsMutationOrigin.ProjectLoad);
            _state.BeginCalculation();

            var result = _state.FailCalculation("boom");

            Assert.That(result.IsChanged, Is.True);
            AssertResultsCleared(_state.Snapshot);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(HydraulicsCalculationPhase.Error));
        }

        [Test]
        public void Restore_ProjectLoad_PreservesResults()
        {
            var restored = new HydraulicsStateSnapshot(Inputs(), new[] { CollectorWithResults() }, HydraulicsStatusSnapshot.Default);

            _state.Restore(restored, HydraulicsMutationOrigin.ProjectLoad);

            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.Collectors.Select(c => c.Summary), Has.All.Not.Null,
                    "загрузка сохраняет результаты файла");
                Assert.That(_state.Snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.OperatingResult), Has.All.Not.Null);
                Assert.That(_state.Snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.DesignResult), Has.All.Not.Null);
            });
        }

        [Test]
        public void CompleteCalculation_WithGlycolProperties_FixesThemInSnapshot()
        {
            // ADR-013: свойства теплоносителя фиксируются тем же расчётом,
            // что и результаты.
            var operating = new GlycolPropertiesSnapshot(1053.0, 3.39, 4.5, 0.47, 38.0);
            var design = new GlycolPropertiesSnapshot(1049.0, 3.41, 12.0, 0.45, 96.0);

            _state.BeginCalculation();
            _state.CompleteCalculation(
                new[] { Collector() },
                new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() },
                operating,
                design);

            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.OperatingGlycolProperties, Is.EqualTo(operating));
                Assert.That(_state.Snapshot.DesignGlycolProperties, Is.EqualTo(design));
            });
        }

        [Test]
        public void CompleteCalculation_WithoutGlycol_KeepsPropertiesNull()
        {
            _state.BeginCalculation();
            _state.CompleteCalculation(
                new[] { Collector() },
                new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() });

            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.OperatingGlycolProperties, Is.Null);
                Assert.That(_state.Snapshot.DesignGlycolProperties, Is.Null);
            });
        }

        [Test]
        public void InputMutations_ClearGlycolProperties_LikeResults()
        {
            // Свойства живут вместе с результатами: правка входов
            // пользователем инвалидирует и их (ADR-013).
            var operating = new GlycolPropertiesSnapshot(1053.0, 3.39, 4.5, 0.47, 38.0);
            var design = new GlycolPropertiesSnapshot(1049.0, 3.41, 12.0, 0.45, 96.0);
            _state.BeginCalculation();
            _state.CompleteCalculation(new[] { Collector() }, new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() }, operating, design);

            _state.ApplyGlobalInputs(HydraulicGlobalInputsSnapshot.Default, HydraulicsMutationOrigin.User);
            Assert.That(_state.Snapshot.OperatingGlycolProperties, Is.Null, "ApplyGlobalInputs(User) сбрасывает свойства");

            _state.BeginCalculation();
            _state.CompleteCalculation(new[] { Collector() }, new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() }, operating, design);
            _state.ReplaceCollectors(new[] { Collector() }, HydraulicsMutationOrigin.User);
            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.OperatingGlycolProperties, Is.Null, "ReplaceCollectors(User) сбрасывает свойства");
                Assert.That(_state.Snapshot.DesignGlycolProperties, Is.Null, "ReplaceCollectors(User) сбрасывает свойства");
            });
        }

        [Test]
        public void FailCalculation_ClearsGlycolProperties()
        {
            var operating = new GlycolPropertiesSnapshot(1053.0, 3.39, 4.5, 0.47, 38.0);
            _state.BeginCalculation();
            _state.CompleteCalculation(new[] { Collector() }, new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() }, operating, operating);

            _state.BeginCalculation();
            _state.FailCalculation("boom");

            Assert.That(_state.Snapshot.OperatingGlycolProperties, Is.Null, "провалившийся расчёт не оставляет полурезультатов");
        }

        [Test]
        public void Restore_KeepsGlycolProperties_Passthrough()
        {
            var operating = new GlycolPropertiesSnapshot(1053.0, 3.39, 4.5, 0.47, 38.0);
            var design = new GlycolPropertiesSnapshot(1049.0, 3.41, 12.0, 0.45, 96.0);
            var snapshot = new HydraulicsStateSnapshot(
                HydraulicGlobalInputsSnapshot.Default,
                new[] { Collector() },
                HydraulicsStatusSnapshot.Default,
                operating,
                design);

            _state.Restore(snapshot, HydraulicsMutationOrigin.ProjectLoad);

            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.OperatingGlycolProperties, Is.EqualTo(operating));
                Assert.That(_state.Snapshot.DesignGlycolProperties, Is.EqualTo(design));
            });
        }

        [Test]
        public void CompleteCalculation_StoresResults_IsCalculatedTrue()
        {
            _state.ReplaceCollectors(new[] { Collector() }, HydraulicsMutationOrigin.ProjectLoad);

            _state.CompleteCalculation(
                new[] { new HydraulicCollectorSnapshot(1, "HKV-D", ValveType.HKV_D, new[] { new HydraulicCircuitSnapshot(1, 110, 10, 5, 10, 20, Result(1), Result(2)) }) },
                new Dictionary<int, HydraulicCollectorSummarySnapshot> { [1] = Summary() });

            Assert.Multiple(() =>
            {
                Assert.That(_state.Snapshot.Collectors.Select(c => c.Summary), Has.All.Not.Null);
                Assert.That(_state.Snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.OperatingResult), Has.All.Not.Null);
                Assert.That(_state.Snapshot.IsCalculated(), Is.True,
                    "после расчёта предикат «рассчитано и валидно» истинен");
            });
        }

        #endregion
    }
}
