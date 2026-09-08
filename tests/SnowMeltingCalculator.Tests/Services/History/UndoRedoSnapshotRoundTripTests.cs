using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.History
{
    /// <summary>
    /// Этап 1 плана undo/redo (ADR-014): round-trip канонических снимков
    /// через новые Undo/Redo-методы записи. Контракты: побитовое равенство
    /// снимков (включая Period0Days/HasUserModifications/статус), ровно одно
    /// Changed, ноль dirty, ноль DataChanged-проекций, публикация контекста.
    /// </summary>
    [TestFixture]
    public class UndoRedoSnapshotRoundTripTests
    {
        private ProjectSession _session = null!;
        private ClimateData _climateData = null!;
        private CalculationContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _context = new CalculationContext();
            _climateData = new ClimateData();
            _session = new ProjectSession(_climateData, _context, hydraulicsDirtyService: null);
        }

        [Test]
        public void Climate_ApplySnapshot_Undo_RestoresAllTwelveFieldsBitForBit()
        {
            var city = MakeCity("Норильск", -45.0, period0Days: 277);
            _session.ClimateState.ApplyCitySelection(city, isHighRequirements: false, ClimateMutationOrigin.User);
            var expected = _session.ClimateState.Snapshot;
            Assert.That(expected.IsCitySelected, Is.True, "Sanity: city selected.");
            Assert.That(expected.HasUserModifications, Is.True, "Sanity: user origin keeps the flag.");

            // Пользовательская правка поверх выбранного города.
            _session.ClimateState.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.WindSpeed, 2.0),
                ClimateMutationOrigin.User);
            Assert.That(_session.ClimateState.Snapshot, Is.Not.EqualTo(expected), "Sanity: the edit changed the state.");

            var completions = new List<ClimateStateChangedEventArgs>();
            _session.ClimateState.Changed += (_, e) => completions.Add(e);

            var result = _session.ClimateState.ApplySnapshot(expected, ClimateMutationOrigin.Undo);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(completions, Has.Count.EqualTo(1), "Exactly one canonical completion per undo application.");
            Assert.That(completions[0].Origin, Is.EqualTo(ClimateMutationOrigin.Undo));
            Assert.That(_session.ClimateState.Snapshot, Is.EqualTo(expected),
                "Undo must restore the climate snapshot bit-for-bit, including Period0Days and HasUserModifications.");
            Assert.That(_session.ClimateState.Snapshot.Period0Days, Is.EqualTo(expected.Period0Days));
            Assert.That(_session.ClimateState.Snapshot.HasUserModifications, Is.EqualTo(expected.HasUserModifications));
        }

        [Test]
        public void Climate_ApplySnapshot_Undo_DoesNotRaiseDataChanged_PublishesContext_DoesNotDirty()
        {
            var city = MakeCity("Сургут", -32.0, period0Days: 224);
            _session.ClimateState.ApplyCitySelection(city, isHighRequirements: false, ClimateMutationOrigin.User);
            var saved = _session.ClimateState.Snapshot;
            _session.ClimateState.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.WindSpeed, 9.0),
                ClimateMutationOrigin.User);
            _session.MarkClean(); // изолируем dirty-эффект правки: Undo сам не должен испачкать

            var dataChangedCount = 0;
            _climateData.DataChanged += (_, _) => dataChangedCount++;
            var contextPublications = 0;
            _context.ContextChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CalculationContext.Climate))
                {
                    contextPublications++;
                }
            };

            _session.ClimateState.ApplySnapshot(saved, ClimateMutationOrigin.Undo);

            Assert.That(dataChangedCount, Is.Zero,
                "Undo must not raise the compatibility DataChanged projection (PublishesCompatibility == false, like Load).");
            Assert.That(contextPublications, Is.EqualTo(1),
                "The climate context publication is unconditional for changed origins.");
            Assert.That(_session.IsDirty, Is.False,
                "Undo origin must never create user dirty (the journal owns dirty by clean-point).");
        }

        [Test]
        public void Climate_ApplySnapshot_RejectsNonUndoRedoOrigin()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _session.ClimateState.ApplySnapshot(_session.ClimateState.Snapshot, ClimateMutationOrigin.User));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _session.ClimateState.ApplySnapshot(_session.ClimateState.Snapshot, ClimateMutationOrigin.Load));
        }

        [Test]
        public void Construction_UndoOrigin_DoesNotRaiseProjectionDataChanged()
        {
            var layer = new ConstructionLayerSnapshot(Guid.NewGuid(), 1, "Пеноплекс", 50.0, 0.031, false, LayerPosition.AbovePipe, 0);
            var target = new ConstructionStateSnapshot(1.5, new[] { layer }, Array.Empty<ConstructionLayerSnapshot>());
            _session.ConstructionState.ApplySnapshot(target, ConstructionMutationOrigin.User);
            var saved = _session.ConstructionState.Snapshot;

            _session.ConstructionState.Apply(new ConstructionMutation.ClearLayers(), ConstructionMutationOrigin.User);
            Assert.That(_session.ConstructionState.Snapshot.LayersAbovePipe, Is.Empty, "Sanity: layers cleared.");
            _session.MarkClean(); // изолируем dirty-эффект правок: Undo сам не должен испачкать

            var dataChangedCount = 0;
            ((ConstructionStateProjection)_session.ConstructionState.CurrentProjection).DataChanged += (_, _) => dataChangedCount++;
            var completions = new List<ConstructionStateChangedEventArgs>();
            _session.ConstructionState.Changed += (_, e) => completions.Add(e);

            var result = _session.ConstructionState.ApplySnapshot(saved, ConstructionMutationOrigin.Undo);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(completions, Has.Count.EqualTo(1));
            Assert.That(completions[0].Origin, Is.EqualTo(ConstructionMutationOrigin.Undo));
            Assert.That(dataChangedCount, Is.Zero,
                "PublishesDownstream is intentionally NOT extended to Undo/Redo (ADR-014 asymmetry): " +
                "RaiseDataChanged would invalidate thermal during the construction undo.");
            Assert.That(_session.ConstructionState.Snapshot, Is.EqualTo(saved));
            Assert.That(_session.IsDirty, Is.False, "Undo must not create user dirty.");
        }

        [Test]
        public void Thermal_RestoreState_AppliesStatusFromSnapshot_NotDefault()
        {
            var withResult = MakeThermalSnapshot(
                MakeThermalResult(powerTotal: 42.5),
                ThermalStatusSnapshot.Default);
            _session.ThermalState.RestoreState(withResult, ThermalMutationOrigin.Undo);
            Assert.That(_session.ThermalState.Snapshot.Result, Is.Not.Null, "Sanity: result applied.");

            // Снимок «после правки входов при существовавшем результате»:
            // статус NeedsRecalculation с сообщением — RestoreState обязан
            // применить его ИЗ снимка, не нормализуя к Default.
            var edited = new ThermalStateSnapshot(
                new ThermalInputsSnapshot(OperatingMode.Intensive, 60.0, 8.0, ThermalPipeSnapshot.FromPipeType(PipeType.StandardPipes[0]), 250),
                withResult.Result,
                new ThermalStatusSnapshot(ThermalCalculationPhase.NeedsRecalculation, "Режим работы изменён. Требуется пересчёт.", string.Empty));

            var completions = new List<ThermalStateChangedEventArgs>();
            _session.ThermalState.Changed += (_, e) => completions.Add(e);

            var result = _session.ThermalState.RestoreState(edited, ThermalMutationOrigin.Redo);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(completions, Has.Count.EqualTo(1));
            Assert.That(_session.ThermalState.Snapshot, Is.EqualTo(edited));
            Assert.That(_session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(_session.ThermalState.Snapshot.Status.RecalculationMessage,
                Is.EqualTo("Режим работы изменён. Требуется пересчёт."),
                "The status must come from the snapshot verbatim (review P2-4), not normalized to Default.");
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void Thermal_RestoreState_RejectsNonUndoRedoOrigin()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _session.ThermalState.RestoreState(_session.ThermalState.Snapshot, ThermalMutationOrigin.ProjectLoad));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _session.ThermalState.RestoreState(_session.ThermalState.Snapshot, ThermalMutationOrigin.User));
        }

        [Test]
        public void Hydraulics_Restore_AcceptsUndoAndRedo_RejectsOtherOrigins_NoDirty()
        {
            var collector = new HydraulicCollectorSnapshot(
                1, "HKV-D", ValveType.HKV_D,
                new[] { new HydraulicCircuitSnapshot(1, 120.0, 15.0, 7.5, 60.0, 20.0) },
                summary: null);
            var target = new HydraulicsStateSnapshot(HydraulicGlobalInputsSnapshot.Default, new[] { collector }, HydraulicsStatusSnapshot.Default);

            var completions = new List<HydraulicsStateChangedEventArgs>();
            _session.HydraulicsState.Changed += (_, e) => completions.Add(e);

            var undoResult = _session.HydraulicsState.Restore(target, HydraulicsMutationOrigin.Undo);
            Assert.That(undoResult.IsChanged, Is.True, "The weakened guard accepts Undo (ADR-014).");
            Assert.That(_session.HydraulicsState.Snapshot.Collectors, Has.Count.EqualTo(1));

            var redoResult = _session.HydraulicsState.Restore(HydraulicsStateSnapshot.Default, HydraulicsMutationOrigin.Redo);
            Assert.That(redoResult.IsChanged, Is.True, "The weakened guard accepts Redo (ADR-014).");

            Assert.That(_session.HydraulicsState.Restore(target, HydraulicsMutationOrigin.User).IsRejected, Is.True,
                "Origins outside ProjectLoad/Undo/Redo stay rejected.");

            Assert.That(
                completions.Count(c => c.Origin is HydraulicsMutationOrigin.Undo or HydraulicsMutationOrigin.Redo),
                Is.EqualTo(2));
            Assert.That(_session.IsDirty, Is.False, "Undo/Redo origins must not create user dirty.");
        }

        private static CityInfo MakeCity(string name, double t5days, int period0Days) => new()
        {
            Name = name,
            Region = "Тестовый регион",
            T5Days092 = t5days,
            WindAvgTempLe8 = 4.0,
            Humidity15hCold = 76.0,
            Period_0_Days = period0Days
        };

        private static ThermalStateSnapshot MakeThermalSnapshot(ThermalResultSnapshot result, ThermalStatusSnapshot status) =>
            new(ThermalInputsSnapshot.Default, result, status);

        private static ThermalResultSnapshot MakeThermalResult(double powerTotal) =>
            ThermalResultSnapshot.FromResult(new ThermalCalculationResult { PowerTotal = powerTotal, IsValid = true })!;
    }
}
