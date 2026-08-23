using System;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 4 Todo 3: direct unit tests for <see cref="ProjectSessionThermalState"/>,
    /// proving DEC-T01/T02 (immutable snapshots, structural equality, exact defaults,
    /// closed origin set, atomic rejection, single canonical completion) BEFORE any
    /// runtime consumer is wired (Todos 4-8 explicitly deferred).
    /// QA-failure categories inside the suite: <c>Category=DefensiveCopy</c> cases pass
    /// only by proving ingress/egress immutability; <c>Category=RejectedCandidate</c>
    /// cases pass only by proving atomic rejection with zero events.
    /// </summary>
    [TestFixture]
    public sealed class ProjectSessionThermalStateTests
    {
        // Точные русские формулировки причин пересчёта — посимвольно из
        // охарактеризованного поведения ThermalViewModel (Todo 2 receipt §3, строки 47).
        private const string ModeChangedMessage = "Режим работы изменён. Требуется пересчёт.";
        private const string SupplyTemperatureChangedMessage = "Температура подачи изменена. Требуется пересчёт.";
        private const string GroundTemperatureChangedMessage = "Температура грунта изменена. Требуется пересчёт.";
        private const string PipeChangedMessage = "Тип трубы изменён. Требуется пересчёт.";
        private const string PipeSpacingChangedMessage = "Шаг укладки изменён. Требуется пересчёт.";

        private ProjectSessionThermalState _state = null!;
        private int _completions;
        private ThermalMutationResult _lastMutation = null!;

        [SetUp]
        public void Setup()
        {
            _state = new ProjectSessionThermalState();
            _completions = 0;
            _lastMutation = null!;
            _state.Changed += OnStateChanged;
        }

        [TearDown]
        public void TearDown()
        {
            _state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(object? sender, ThermalStateChangedEventArgs e)
        {
            _completions++;
            _lastMutation = e.Mutation;
        }

        #region Builders

        private static ThermalPipeSnapshot Pipe(int marker = 0) =>
            new($"RAUTHERM S {marker}", $"12180501{marker:00}", 17.0 + marker, 13.0 + marker, 2.0, 0.35);

        private static ThermalInputsSnapshot Inputs(
            OperatingMode mode = OperatingMode.Melting,
            double supply = 50.0,
            double ground = 10.0,
            ThermalPipeSnapshot? pipe = null,
            int spacing = 200) =>
            new(mode, supply, ground, pipe, spacing);

        private static ThermalResultSnapshot BuildResult(
            double alpha = 1,
            double powerUp = 2,
            double powerDown = 3,
            double powerTotal = 4,
            double meltingHeat = 5,
            double radiationHeat = 6,
            double convectionHeat = 7,
            double excessTemperature = 8,
            double meanTemperature = 9,
            double supplyTemperature = 10,
            double returnTemperature = 11,
            double deltaT = 12,
            double rFb = 13,
            double rD = 14,
            double parameterM = 15,
            double efficiencyEtaR = 16,
            double massFlowRate = 17,
            double volumeFlowRate = 18,
            bool isValid = true,
            string[]? errors = null) =>
            new(alpha, powerUp, powerDown, powerTotal, meltingHeat, radiationHeat, convectionHeat,
                excessTemperature, meanTemperature, supplyTemperature, returnTemperature, deltaT,
                rFb, rD, parameterM, efficiencyEtaR, massFlowRate, volumeFlowRate, isValid, errors);

        /// <summary>Заполнить состояние результатом расчёта (фаза Actual).</summary>
        private void ArrangeWithResult(double powerTotal = 111.0)
        {
            _state.ApplyInputs(Inputs(pipe: Pipe()), ThermalMutationOrigin.Initialization);
            _state.BeginCalculation();
            _state.CompleteCalculation(Inputs(pipe: Pipe()), BuildResult(powerTotal: powerTotal), string.Empty);
            _completions = 0;
            _lastMutation = null!;
        }

        #endregion

        #region Exact defaults (DEC-T01)

        [Test]
        public void FreshState_HasExactContractDefaults()
        {
            var snapshot = _state.Snapshot;

            Assert.That(snapshot.Inputs.Mode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(snapshot.Inputs.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(snapshot.Inputs.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(snapshot.Inputs.Pipe, Is.Null);
            Assert.That(snapshot.Inputs.PipeSpacing, Is.EqualTo(200));
            Assert.That(snapshot.Result, Is.Null);
            Assert.That(snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(snapshot.Status.RecalculationMessage, Is.Empty);
            Assert.That(snapshot.Status.ValidationMessage, Is.Empty);

            Assert.That(snapshot, Is.EqualTo(ThermalStateSnapshot.Default));
            Assert.That(snapshot.Inputs, Is.EqualTo(ThermalInputsSnapshot.Default));
            Assert.That(snapshot.Status, Is.EqualTo(ThermalStatusSnapshot.Default));
        }

        [Test]
        public void ResetToDefaults_FromModifiedState_RestoresExactContractDefaults()
        {
            ArrangeWithResult();
            _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(80.0), ThermalMutationOrigin.User);

            var result = _state.ResetToDefaults(ThermalMutationOrigin.UserReset);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot, Is.EqualTo(ThermalStateSnapshot.Default));
            Assert.That(_state.Snapshot.Result, Is.Null);
        }

        [Test]
        public void ResetToDefaults_OnFreshState_IsNoChangeWithZeroCompletions()
        {
            var result = _state.ResetToDefaults(ThermalMutationOrigin.UserReset);

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(_completions, Is.Zero);
        }

        #endregion

        #region Structural equality: independent equal snapshots, per-field detection

        [Test]
        public void SnapshotProperty_ReturnsEqualInstancesWithoutSharedMutability()
        {
            var first = _state.Snapshot;
            var second = _state.Snapshot;

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void ApplyInputs_StructurallyEqualCandidate_IsNoChangeWithZeroCompletions_WithoutResult()
        {
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(ThermalInputsSnapshot.Default, ThermalMutationOrigin.User);

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void ApplyInputs_StructurallyEqualCandidate_IsNoChangeWithZeroCompletions_WithResult()
        {
            ArrangeWithResult();
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(Inputs(pipe: Pipe()), ThermalMutationOrigin.User);

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void InputsSnapshot_Equality_DetectsEveryFieldChange()
        {
            var @base = ThermalInputsSnapshot.Default;
            var mutants = new (string Name, ThermalInputsSnapshot Snapshot)[]
            {
                ("Mode", Inputs(mode: OperatingMode.AntiIcing)),
                ("SupplyTemperature", Inputs(supply: 60.0)),
                ("GroundTemperature", Inputs(ground: 20.0)),
                ("Pipe", Inputs(pipe: Pipe())),
                ("PipeSpacing", Inputs(spacing: 250)),
            };

            foreach (var (name, mutant) in mutants)
            {
                Assert.That(mutant.Equals(@base), Is.False, $"Inputs.{name} change must be detected");
                Assert.That(@base.Equals(mutant), Is.False, $"Inputs.{name} symmetric detection");
            }
        }

        [Test]
        public void StatusSnapshot_Equality_DetectsEveryFieldChange()
        {
            var @base = ThermalStatusSnapshot.Default;
            var mutants = new (string Name, ThermalStatusSnapshot Snapshot)[]
            {
                ("Phase", new ThermalStatusSnapshot(ThermalCalculationPhase.NeedsRecalculation, string.Empty, string.Empty)),
                ("RecalculationMessage", new ThermalStatusSnapshot(ThermalCalculationPhase.Actual, "msg", string.Empty)),
                ("ValidationMessage", new ThermalStatusSnapshot(ThermalCalculationPhase.Actual, string.Empty, "msg")),
            };

            foreach (var (name, mutant) in mutants)
            {
                Assert.That(mutant.Equals(@base), Is.False, $"Status.{name} change must be detected");
                Assert.That(@base.Equals(mutant), Is.False, $"Status.{name} symmetric detection");
            }
        }

        [Test]
        public void PipeSnapshot_Equality_DetectsEveryFieldChange()
        {
            var @base = Pipe();
            var mutants = new (string Name, ThermalPipeSnapshot Snapshot)[]
            {
                ("Name", new ThermalPipeSnapshot("OTHER", @base.Article, @base.OuterDiameter, @base.InnerDiameter, @base.WallThickness, @base.ThermalConductivity)),
                ("Article", new ThermalPipeSnapshot(@base.Name, "OTHER", @base.OuterDiameter, @base.InnerDiameter, @base.WallThickness, @base.ThermalConductivity)),
                ("OuterDiameter", new ThermalPipeSnapshot(@base.Name, @base.Article, 99.0, @base.InnerDiameter, @base.WallThickness, @base.ThermalConductivity)),
                ("InnerDiameter", new ThermalPipeSnapshot(@base.Name, @base.Article, @base.OuterDiameter, 99.0, @base.WallThickness, @base.ThermalConductivity)),
                ("WallThickness", new ThermalPipeSnapshot(@base.Name, @base.Article, @base.OuterDiameter, @base.InnerDiameter, 9.9, @base.ThermalConductivity)),
                ("ThermalConductivity", new ThermalPipeSnapshot(@base.Name, @base.Article, @base.OuterDiameter, @base.InnerDiameter, @base.WallThickness, 9.9)),
            };

            foreach (var (name, mutant) in mutants)
            {
                Assert.That(mutant.Equals(@base), Is.False, $"Pipe.{name} change must be detected");
                Assert.That(@base.Equals(mutant), Is.False, $"Pipe.{name} symmetric detection");
            }

            Assert.That(@base.Equals(null), Is.False);
        }

        [Test]
        public void ResultSnapshot_Equality_DetectsEveryScalarFieldChange()
        {
            var @base = BuildResult();
            var mutants = new (string Name, ThermalResultSnapshot Snapshot)[]
            {
                ("Alpha", BuildResult(alpha: 101)),
                ("PowerUp", BuildResult(powerUp: 102)),
                ("PowerDown", BuildResult(powerDown: 103)),
                ("PowerTotal", BuildResult(powerTotal: 104)),
                ("MeltingHeat", BuildResult(meltingHeat: 105)),
                ("RadiationHeat", BuildResult(radiationHeat: 106)),
                ("ConvectionHeat", BuildResult(convectionHeat: 107)),
                ("ExcessTemperature", BuildResult(excessTemperature: 108)),
                ("MeanTemperature", BuildResult(meanTemperature: 109)),
                ("SupplyTemperature", BuildResult(supplyTemperature: 110)),
                ("ReturnTemperature", BuildResult(returnTemperature: 111)),
                ("DeltaT", BuildResult(deltaT: 112)),
                ("RFb", BuildResult(rFb: 113)),
                ("RD", BuildResult(rD: 114)),
                ("ParameterM", BuildResult(parameterM: 115)),
                ("EfficiencyEtaR", BuildResult(efficiencyEtaR: 116)),
                ("MassFlowRate", BuildResult(massFlowRate: 117)),
                ("VolumeFlowRate", BuildResult(volumeFlowRate: 118)),
                ("IsValid", BuildResult(isValid: false)),
            };

            Assert.That(mutants.Count, Is.EqualTo(19), "full DEC-T01 value surface must be covered");

            foreach (var (name, mutant) in mutants)
            {
                Assert.That(mutant.Equals(@base), Is.False, $"Result.{name} change must be detected");
                Assert.That(@base.Equals(mutant), Is.False, $"Result.{name} symmetric detection");
            }
        }

        [Test]
        public void ResultSnapshot_Equality_ValidationErrorsAreOrderedAndContentSignificant()
        {
            var @base = BuildResult(errors: new[] { "e1", "e2" });

            Assert.That(BuildResult(errors: new[] { "e1", "e2" }), Is.EqualTo(@base), "equal ordered content is equal");
            Assert.That(BuildResult(errors: new[] { "e1", "e2", "e3" }), Is.Not.EqualTo(@base), "append must be detected");
            Assert.That(BuildResult(errors: new[] { "e1", "e3" }), Is.Not.EqualTo(@base), "content change must be detected");
            Assert.That(BuildResult(errors: new[] { "e2", "e1" }), Is.Not.EqualTo(@base), "order change must be detected");
            Assert.That(BuildResult(errors: Array.Empty<string>()), Is.Not.EqualTo(@base), "empty vs non-empty must be detected");
        }

        [Test]
        public void StateSnapshot_Equality_DetectsEveryComponentChange()
        {
            var @default = ThermalStateSnapshot.Default;

            Assert.That(new ThermalStateSnapshot(Inputs(supply: 77.0), null, ThermalStatusSnapshot.Default), Is.Not.EqualTo(@default), "Inputs component");
            Assert.That(new ThermalStateSnapshot(ThermalInputsSnapshot.Default, BuildResult(), ThermalStatusSnapshot.Default), Is.Not.EqualTo(@default), "Result component");
            Assert.That(new ThermalStateSnapshot(ThermalInputsSnapshot.Default, null, new ThermalStatusSnapshot(ThermalCalculationPhase.Calculating, string.Empty, string.Empty)), Is.Not.EqualTo(@default), "Status component");

            Assert.That(new ThermalStateSnapshot(ThermalInputsSnapshot.Default, null, ThermalStatusSnapshot.Default), Is.EqualTo(@default));
        }

        #endregion

        #region Defensive copies (QA-failure category: passes only by proving immutability)

        [Test]
        [Category("DefensiveCopy")]
        public void PipeIngress_MutatingOriginalPipeType_DoesNotMutateOwner()
        {
            var domainPipe = new PipeType
            {
                Name = "RAUTHERM S 17x2,0",
                Article = "12180501001",
                OuterDiameter = 17,
                InnerDiameter = 13,
                WallThickness = 2.0,
                ThermalConductivity = 0.35
            };
            var pipeSnapshot = ThermalPipeSnapshot.FromPipeType(domainPipe);

            _state.ApplyInputs(Inputs(pipe: pipeSnapshot), ThermalMutationOrigin.User);

            domainPipe.Name = "TAMPERED";
            domainPipe.OuterDiameter = 999;
            domainPipe.InnerDiameter = 999;
            domainPipe.WallThickness = 999;
            domainPipe.ThermalConductivity = 999;

            var owned = _state.Snapshot.Inputs.Pipe;
            Assert.That(owned, Is.Not.Null);
            Assert.That(owned, Is.EqualTo(pipeSnapshot));
            Assert.That(owned!.Name, Is.EqualTo("RAUTHERM S 17x2,0"));
            Assert.That(owned.OuterDiameter, Is.EqualTo(17.0));
        }

        [Test]
        [Category("DefensiveCopy")]
        public void ResultIngress_MutatingOriginalResult_DoesNotMutateOwner()
        {
            var domainResult = new ThermalCalculationResult
            {
                PowerTotal = 100.0,
                IsValid = true,
                ValidationErrors = new[] { "e1", "e2" }
            };

            _state.CompleteCalculation(
                ThermalInputsSnapshot.Default,
                ThermalResultSnapshot.FromResult(domainResult)!,
                string.Empty);

            domainResult.PowerTotal = 999.0;
            domainResult.IsValid = false;
            domainResult.ValidationErrors[0] = "tampered";
            domainResult.ValidationErrors = new[] { "wholly", "different" };

            var owned = _state.Snapshot.Result;
            Assert.That(owned, Is.Not.Null);
            Assert.That(owned!.PowerTotal, Is.EqualTo(100.0));
            Assert.That(owned.IsValid, Is.True);
            Assert.That(owned.ValidationErrors, Is.EqualTo(new[] { "e1", "e2" }));
        }

        [Test]
        [Category("DefensiveCopy")]
        public void Restore_SavedResultIngress_MutatingOriginal_DoesNotMutateOwner()
        {
            var saved = new ThermalCalculationResult { DeltaT = 7.5, IsValid = true };

            _state.Restore(Inputs(spacing: 250), ThermalResultSnapshot.FromResult(saved));

            saved.DeltaT = 999.0;

            Assert.That(_state.Snapshot.Result!.DeltaT, Is.EqualTo(7.5));
        }

        [Test]
        [Category("DefensiveCopy")]
        public void EgressValidationErrors_ExposeNoWritableBackingReference()
        {
            var raw = new[] { "e1", "e2" };
            _state.CompleteCalculation(ThermalInputsSnapshot.Default, BuildResult(errors: raw), string.Empty);
            raw[0] = "tampered";

            var errors = _state.Snapshot.Result!.ValidationErrors;
            Assert.That(errors, Is.EqualTo(new[] { "e1", "e2" }));

            // Приведение к записываемому массиву невозможно: сырой backing не утекает.
            Assert.Throws<InvalidCastException>(() =>
            {
                _ = (string[])errors;
            });

            // Мутация через IList-интерфейс запрещена обёрткой только для чтения.
            Assert.That(errors, Is.InstanceOf<IList<string>>());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)errors).Add("intruder"));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)errors).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)errors)[0] = "intruder");

            // Состояние байт-в-байт равно исходному после всех попыток.
            Assert.That(_state.Snapshot.Result!.ValidationErrors, Is.EqualTo(new[] { "e1", "e2" }));
        }

        [Test]
        [Category("DefensiveCopy")]
        public void StructuralSnapshots_ReferenceEqualityIsNotIdentity()
        {
            var pipeA = Pipe(1);
            var pipeB = Pipe(1);
            Assert.That(pipeA, Is.Not.SameAs(pipeB));
            Assert.That(pipeA, Is.EqualTo(pipeB));

            var resultA = BuildResult(powerTotal: 42);
            var resultB = BuildResult(powerTotal: 42);
            Assert.That(resultA, Is.Not.SameAs(resultB));
            Assert.That(resultA, Is.EqualTo(resultB));

            var inputsA = Inputs(pipe: Pipe(2));
            var inputsB = Inputs(pipe: Pipe(2));
            Assert.That(inputsA, Is.Not.SameAs(inputsB));
            Assert.That(inputsA, Is.EqualTo(inputsB));

            // Возвращённый снимок нельзя использовать как канал мутации владельца:
            // все члены неизменяемы, повторный Snapshot структурно идентичен.
            _state.ApplyInputs(inputsA, ThermalMutationOrigin.SystemApply);
            Assert.That(_state.Snapshot, Is.EqualTo(_state.Snapshot));
        }

        #endregion

        #region Rejected candidates (QA-failure category: passes only by proving atomic rejection)

        [Test]
        [Category("RejectedCandidate")]
        public void ApplyInputs_OutOfRangeSupplyTemperature_RejectedAtomically(
            [Values(19.9, 90.1, double.NaN)] double supply)
        {
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(Inputs(supply: supply), ThermalMutationOrigin.User);

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        [Category("RejectedCandidate")]
        public void ApplyInputs_OutOfRangeGroundTemperature_RejectedAtomically(
            [Values(-10.1, 30.1)] double ground)
        {
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(Inputs(ground: ground), ThermalMutationOrigin.User);

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        [Category("RejectedCandidate")]
        public void ApplyInputs_OutOfRangePipeSpacing_RejectedAtomically(
            [Values(49, 501)] int spacing)
        {
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(Inputs(spacing: spacing), ThermalMutationOrigin.User);

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        [Category("RejectedCandidate")]
        public void ApplyInputs_UndefinedOperatingMode_RejectedAtomically()
        {
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(Inputs(mode: (OperatingMode)99), ThermalMutationOrigin.User);

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        [Category("RejectedCandidate")]
        public void ApplyInputEdit_InvalidValue_RejectedAtomically(
            [Values(19.9, 90.1)] double supply)
        {
            ArrangeWithResult();
            var before = _state.Snapshot;

            var result = _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(supply), ThermalMutationOrigin.User);

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void BoundaryValues_AreAccepted_InclusiveRanges()
        {
            Assert.That(_state.ApplyInputs(Inputs(supply: 20.0), ThermalMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_state.ApplyInputs(Inputs(supply: 90.0), ThermalMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_state.ApplyInputs(Inputs(ground: -10.0), ThermalMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_state.ApplyInputs(Inputs(ground: 30.0), ThermalMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_state.ApplyInputs(Inputs(spacing: 50), ThermalMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_state.ApplyInputs(Inputs(spacing: 500), ThermalMutationOrigin.SystemApply).IsChanged, Is.True);
            Assert.That(_completions, Is.EqualTo(6));
        }

        #endregion

        #region Completion multiplicity (DEC-T02)

        [Test]
        public void ChangedMutation_EmitsExactlyOneCanonicalCompletion_CarryingOriginBeforeAfter()
        {
            ArrangeWithResult();
            var before = _state.Snapshot;

            var result = _state.ApplyInputEdit(ThermalInputEdit.ForGroundTemperature(25.0), ThermalMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_completions, Is.EqualTo(1));
            Assert.That(_lastMutation, Is.Not.Null);
            Assert.That(_lastMutation.Status, Is.EqualTo(ThermalMutationStatus.Changed));
            Assert.That(_lastMutation.Origin, Is.EqualTo(ThermalMutationOrigin.User));
            Assert.That(_lastMutation.Before, Is.EqualTo(before));
            Assert.That(_lastMutation.After, Is.EqualTo(_state.Snapshot));
        }

        [Test]
        public void NoChangeMutation_EmitsZeroCompletions()
        {
            _state.BeginCalculation(); // Actual -> Calculating
            _completions = 0;

            var equalApply = _state.ApplyInputs(ThermalInputsSnapshot.Default, ThermalMutationOrigin.User);
            var beginAgain = _state.BeginCalculation(); // уже Calculating с пустыми сообщениями

            Assert.That(equalApply.IsNoChange, Is.True);
            Assert.That(beginAgain.IsNoChange, Is.True);
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void NullArgumentContracts_ThrowWithoutEvents()
        {
            Assert.Throws<ArgumentNullException>(() => _state.ApplyInputs(null!, ThermalMutationOrigin.User));
            Assert.Throws<ArgumentNullException>(() => _state.ApplyInputEdit(null!, ThermalMutationOrigin.User));
            Assert.Throws<ArgumentNullException>(
                () => _state.CompleteCalculation(null!, BuildResult(), string.Empty));
            Assert.Throws<ArgumentNullException>(
                () => _state.CompleteCalculation(ThermalInputsSnapshot.Default, null!, string.Empty));
            Assert.Throws<ArgumentNullException>(
                () => _state.FailCalculation(null!, string.Empty));
            Assert.Throws<ArgumentNullException>(() => _state.Restore(null!, null));
            Assert.That(_completions, Is.Zero);
        }

        #endregion

        #region Closed origin set (DEC-T02): exhaustive coverage

        [Test]
        public void OriginEnum_HasExactlyTheClosedDecT02MemberSetInOrder()
        {
            var expected = new[]
            {
                "User",
                "UserReset",
                "ProjectLoadReset",
                "ProjectLoad",
                "ClimateInvalidation",
                "ConstructionInvalidation",
                "Calculation",
                "Initialization",
                "SystemApply"
            };

            Assert.That(Enum.GetNames(typeof(ThermalMutationOrigin)), Is.EqualTo(expected));
        }

        [Test]
        public void EveryOrigin_FlowsThroughChangedMutation_ResultAndEventCarryIt(
            [ValueSource(nameof(AllOrigins))] ThermalMutationOrigin origin)
        {
            var before = _state.Snapshot;

            var result = _state.ApplyInputs(Inputs(supply: 65.0), origin);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(origin));
            Assert.That(result.Before, Is.EqualTo(before));
            Assert.That(result.After, Is.EqualTo(_state.Snapshot));
            Assert.That(_completions, Is.EqualTo(1));
            Assert.That(_lastMutation.Origin, Is.EqualTo(origin));
        }

        private static ThermalMutationOrigin[] AllOrigins() => Enum.GetValues<ThermalMutationOrigin>();

        [Test]
        public void OriginSwitchExpression_CoversEveryMemberExhaustively()
        {
            var labels = Enum.GetValues<ThermalMutationOrigin>()
                .Select(LabelFor)
                .ToArray();

            Assert.That(labels.Length, Is.EqualTo(9));
            Assert.That(labels.Distinct().Count(), Is.EqualTo(9));
        }

        private static string LabelFor(ThermalMutationOrigin origin) => origin switch
        {
            ThermalMutationOrigin.User => "user",
            ThermalMutationOrigin.UserReset => "user-reset",
            ThermalMutationOrigin.ProjectLoadReset => "project-load-reset",
            ThermalMutationOrigin.ProjectLoad => "project-load",
            ThermalMutationOrigin.ClimateInvalidation => "climate-invalidation",
            ThermalMutationOrigin.ConstructionInvalidation => "construction-invalidation",
            ThermalMutationOrigin.Calculation => "calculation",
            ThermalMutationOrigin.Initialization => "initialization",
            ThermalMutationOrigin.SystemApply => "system-apply",
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown thermal mutation origin."),
        };

        #endregion

        #region Own-input edit semantics (DEC-T03): exact Russian cause messages

        [Test]
        public void ApplyInputEdit_User_WithResult_PreservesResultAndSetsExactCauseMessage(
            [Values] ThermalInputField field)
        {
            ArrangeWithResult();
            var preservedResult = _state.Snapshot.Result;

            var (edit, expectedMessage) = field switch
            {
                ThermalInputField.Mode => (ThermalInputEdit.ForMode(OperatingMode.Intensive), ModeChangedMessage),
                ThermalInputField.SupplyTemperature => (ThermalInputEdit.ForSupplyTemperature(75.0), SupplyTemperatureChangedMessage),
                ThermalInputField.GroundTemperature => (ThermalInputEdit.ForGroundTemperature(15.0), GroundTemperatureChangedMessage),
                ThermalInputField.Pipe => (ThermalInputEdit.ForPipe(Pipe(3)), PipeChangedMessage),
                ThermalInputField.PipeSpacing => (ThermalInputEdit.ForPipeSpacing(300), PipeSpacingChangedMessage),
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };

            var result = _state.ApplyInputEdit(edit, ThermalMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.EqualTo(expectedMessage));
            Assert.That(_state.Snapshot.Result, Is.EqualTo(preservedResult), "own-input edit preserves the last result");
            Assert.That(_completions, Is.EqualTo(1));
        }

        [Test]
        public void ApplyInputEdit_User_WithoutResult_DoesNotSynthesizeRecalculation(
            [Values] ThermalInputField field)
        {
            var (edit, _) = field switch
            {
                ThermalInputField.Mode => (ThermalInputEdit.ForMode(OperatingMode.Intensive), ModeChangedMessage),
                ThermalInputField.SupplyTemperature => (ThermalInputEdit.ForSupplyTemperature(75.0), SupplyTemperatureChangedMessage),
                ThermalInputField.GroundTemperature => (ThermalInputEdit.ForGroundTemperature(15.0), GroundTemperatureChangedMessage),
                ThermalInputField.Pipe => (ThermalInputEdit.ForPipe(Pipe(3)), PipeChangedMessage),
                ThermalInputField.PipeSpacing => (ThermalInputEdit.ForPipeSpacing(300), PipeSpacingChangedMessage),
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };

            var result = _state.ApplyInputEdit(edit, ThermalMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.Empty);
            Assert.That(_completions, Is.EqualTo(1), "changed logical mutation still emits exactly one completion");
        }

        [Test]
        public void ApplyInputs_User_WithResult_UsesFirstChangedFieldMessage()
        {
            ArrangeWithResult();

            var candidate = Inputs(mode: OperatingMode.AntiIcing, supply: 70.0, pipe: Pipe());
            var result = _state.ApplyInputs(candidate, ThermalMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.EqualTo(ModeChangedMessage));
            Assert.That(_state.Snapshot.Result, Is.Not.Null);
        }

        [Test]
        public void ApplyInputEdit_NonUserOrigin_WithResult_DoesNotSynthesizeRecalculation()
        {
            ArrangeWithResult();

            var result = _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(75.0), ThermalMutationOrigin.SystemApply);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Inputs.SupplyTemperature, Is.EqualTo(75.0));
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
        }

        #endregion

        #region Lifecycle/system origins normalize status (DEC-T03)

        [Test]
        public void LifecycleOrigins_ApplyInputs_NormalizeStatusToActualAndClearMessages()
        {
            ArrangeWithResult();
            _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(75.0), ThermalMutationOrigin.User);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));

            var lifecycleOrigins = new[]
            {
                ThermalMutationOrigin.ProjectLoadReset,
                ThermalMutationOrigin.ProjectLoad,
                ThermalMutationOrigin.Initialization,
                ThermalMutationOrigin.SystemApply,
                ThermalMutationOrigin.UserReset
            };

            for (var i = 0; i < lifecycleOrigins.Length; i++)
            {
                var origin = lifecycleOrigins[i];
                _completions = 0;

                // Уникальный кандидат на каждой итерации, чтобы мутация была Changed.
                var result = _state.ApplyInputs(Inputs(supply: 60.0 + i, pipe: Pipe(i)), origin);

                Assert.That(result.IsChanged, Is.True, origin.ToString());
                Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual), origin.ToString());
                Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty, origin.ToString());
                Assert.That(_state.Snapshot.Status.ValidationMessage, Is.Empty, origin.ToString());
                Assert.That(_state.Snapshot.Result, Is.Not.Null, "input application never clears the result");
                Assert.That(_completions, Is.EqualTo(1), origin.ToString());
            }
        }

        [Test]
        public void ResetToDefaults_ProjectLoadReset_RestoresDefaultsIncludingResultClear()
        {
            ArrangeWithResult();
            _state.ApplyInputEdit(ThermalInputEdit.ForPipeSpacing(300), ThermalMutationOrigin.User);
            _completions = 0;

            var result = _state.ResetToDefaults(ThermalMutationOrigin.ProjectLoadReset);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.ProjectLoadReset));
            Assert.That(_state.Snapshot, Is.EqualTo(ThermalStateSnapshot.Default));
            Assert.That(_completions, Is.EqualTo(1));
        }

        [Test]
        public void ResetToDefaults_UserReset_ClearsResultAndStatusWithoutDirtyConcerns()
        {
            ArrangeWithResult();

            var result = _state.ResetToDefaults(ThermalMutationOrigin.UserReset);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Result, Is.Null);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
            // Dirty-семантика вне зоны состояния: класс не имеет dirty-зависимостей вовсе.
        }

        #endregion

        #region Calculation lifecycle (DEC-T02)

        [Test]
        public void BeginCalculation_TransitionsPhaseAndClearsMessages()
        {
            ArrangeWithResult();
            _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(75.0), ThermalMutationOrigin.User);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Not.Empty);

            var result = _state.BeginCalculation();

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.Calculation));
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Calculating));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.Empty);
            Assert.That(_state.Snapshot.Result, Is.Not.Null, "begin keeps the last result until completion/failure");
        }

        [Test]
        public void BeginCalculation_SecondCallWhileCalculating_IsNoChange()
        {
            _state.BeginCalculation();
            _completions = 0;

            var result = _state.BeginCalculation();

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void CompleteCalculation_StoresResultCanonically_PhaseActual_ClearsRecalcMessage_SetsValidationMessage()
        {
            _state.ApplyInputs(Inputs(pipe: Pipe()), ThermalMutationOrigin.Initialization);
            _state.BeginCalculation();

            var calculated = BuildResult(powerTotal: 250.0, isValid: false, errors: new[] { "out of range" });
            var result = _state.CompleteCalculation(Inputs(pipe: Pipe()), calculated, "out of range");

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.Calculation));
            Assert.That(_state.Snapshot.Result, Is.EqualTo(calculated));
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.EqualTo("out of range"));
        }

        [Test]
        public void CompleteCalculation_IdenticalRepeat_IsNoChangeWithZeroCompletions()
        {
            var inputs = Inputs(pipe: Pipe());
            var calculated = BuildResult(powerTotal: 250.0);
            _state.CompleteCalculation(inputs, calculated, string.Empty);
            _completions = 0;

            var result = _state.CompleteCalculation(inputs, calculated, string.Empty);

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void FailCalculation_StoresCompatibilityInvalidResult_PhaseActual_ExactMessage()
        {
            _state.ApplyInputs(Inputs(pipe: Pipe()), ThermalMutationOrigin.Initialization);
            _state.BeginCalculation();

            var invalid = BuildResult(powerTotal: 0.0, isValid: false, errors: new[] { "Ошибка расчёта: boom" });
            var result = _state.FailCalculation(Inputs(pipe: Pipe()), "Ошибка расчёта: boom", invalid);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.Calculation));
            Assert.That(_state.Snapshot.Result, Is.EqualTo(invalid));
            Assert.That(_state.Snapshot.Result!.IsValid, Is.False);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.EqualTo("Ошибка расчёта: boom"));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
        }

        [Test]
        public void FailCalculation_WithoutCompatibilityResult_NullResult_ExactMessage()
        {
            ArrangeWithResult();

            var result = _state.FailCalculation(Inputs(pipe: Pipe()), "Ошибка расчёта: kaboom");

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Result, Is.Null);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.EqualTo("Ошибка расчёта: kaboom"));
        }

        [Test]
        public void FullCalculationSequence_BeginThenComplete_EndsActualWithFreshResult()
        {
            ArrangeWithResult(powerTotal: 111.0);

            _state.BeginCalculation();
            _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(80.0), ThermalMutationOrigin.User);
            _state.BeginCalculation();

            var fresh = BuildResult(powerTotal: 222.0);
            _state.CompleteCalculation(Inputs(supply: 80.0, pipe: Pipe()), fresh, string.Empty);

            Assert.That(_state.Snapshot.Result, Is.EqualTo(fresh));
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.Empty);
        }

        #endregion

        #region Restore (DEC-T02: origin bound to ProjectLoad)

        [Test]
        public void Restore_BindsProjectLoadOrigin_ReplacesFullStateAndNormalizesStatus()
        {
            ArrangeWithResult();
            _state.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(75.0), ThermalMutationOrigin.User);
            _completions = 0;

            var restoredInputs = Inputs(mode: OperatingMode.AntiIcing, supply: 45.0, ground: 5.0, pipe: Pipe(4), spacing: 150);
            var savedResult = BuildResult(powerTotal: 333.0);

            var result = _state.Restore(restoredInputs, savedResult);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.ProjectLoad));
            Assert.That(_completions, Is.EqualTo(1));
            Assert.That(_lastMutation.Origin, Is.EqualTo(ThermalMutationOrigin.ProjectLoad));
            Assert.That(_state.Snapshot.Inputs, Is.EqualTo(restoredInputs));
            Assert.That(_state.Snapshot.Result, Is.EqualTo(savedResult));
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.Empty);
            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.Empty);
        }

        [Test]
        public void Restore_WithNullSavedResult_ClearsPreviousResult()
        {
            ArrangeWithResult();

            var result = _state.Restore(Inputs(spacing: 250), null);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(_state.Snapshot.Result, Is.Null);
            Assert.That(_state.Snapshot.Inputs.PipeSpacing, Is.EqualTo(250));
        }

        [Test]
        public void Restore_StructurallyEqualState_IsNoChangeWithZeroCompletions()
        {
            var inputs = Inputs(pipe: Pipe());
            var saved = BuildResult(powerTotal: 55.0);
            _state.Restore(inputs, saved);
            _completions = 0;

            var result = _state.Restore(Inputs(pipe: Pipe()), BuildResult(powerTotal: 55.0));

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        [Category("RejectedCandidate")]
        public void Restore_InvalidInputs_RejectedAtomically()
        {
            ArrangeWithResult();
            var before = _state.Snapshot;

            var result = _state.Restore(Inputs(supply: 999.0), null);

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        #endregion

        #region Upstream invalidation (DEC-T04): clear once, NeedsRecalculation once, only if result existed

        [Test]
        public void InvalidateFromClimate_WithResult_ClearsOnceNeedsRecalculationOnce()
        {
            ArrangeWithResult();

            var result = _state.InvalidateFromClimate("Климатические данные изменены. Требуется пересчёт.");

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.ClimateInvalidation));
            Assert.That(_state.Snapshot.Result, Is.Null);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.EqualTo("Климатические данные изменены. Требуется пересчёт."));
            Assert.That(_completions, Is.EqualTo(1));

            // Повторная инвалидация без результата — нулевой эффект («ровно один раз»).
            var second = _state.InvalidateFromClimate("Климатические данные изменены. Требуется пересчёт.");
            Assert.That(second.IsNoChange, Is.True);
            Assert.That(_completions, Is.EqualTo(1));
        }

        [Test]
        public void InvalidateFromClimate_WithoutResult_HasZeroEffect()
        {
            var before = _state.Snapshot;

            var result = _state.InvalidateFromClimate("Климатические данные изменены. Требуется пересчёт.");

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(result.Before, Is.SameAs(result.After));
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void InvalidateFromConstruction_WithResult_ClearsOnceNeedsRecalculationOnce()
        {
            ArrangeWithResult();

            var result = _state.InvalidateFromConstruction("Данные конструкции изменены. Требуется пересчёт.");

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ThermalMutationOrigin.ConstructionInvalidation));
            Assert.That(_state.Snapshot.Result, Is.Null);
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(_state.Snapshot.Status.RecalculationMessage, Is.EqualTo("Данные конструкции изменены. Требуется пересчёт."));
            Assert.That(_completions, Is.EqualTo(1));

            var second = _state.InvalidateFromConstruction("Данные конструкции изменены. Требуется пересчёт.");
            Assert.That(second.IsNoChange, Is.True);
            Assert.That(_completions, Is.EqualTo(1));
        }

        [Test]
        public void InvalidateFromConstruction_WithoutResult_HasZeroEffect()
        {
            var before = _state.Snapshot;

            var result = _state.InvalidateFromConstruction("Данные конструкции изменены. Требуется пересчёт.");

            Assert.That(result.IsNoChange, Is.True);
            Assert.That(_state.Snapshot, Is.EqualTo(before));
            Assert.That(_completions, Is.Zero);
        }

        [Test]
        public void UpstreamInvalidation_PreservesValidationMessageUntouched()
        {
            _state.CompleteCalculation(ThermalInputsSnapshot.Default, BuildResult(), "validation text");

            _state.InvalidateFromClimate("Климатические данные изменены. Требуется пересчёт.");

            Assert.That(_state.Snapshot.Status.ValidationMessage, Is.EqualTo("validation text"));
            Assert.That(_state.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
        }

        #endregion
    }
}
