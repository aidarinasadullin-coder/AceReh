// ================================================================================
// Phase 4 Todo 2 - Thermal multiplicity characterization suite.
// ================================================================================
//
// Characterizes CURRENT observable behavior of every Thermal writer, subscriber,
// calculation path, lifecycle path and persistence path BEFORE any ownership
// edit (frozen plan docs/architecture-migration/plans/phase-4-thermal-state.md,
// DEC-T03..T08, Todo 2 lines 366-374). NO production code is changed by this
// suite; every assertion records what the current sources do today.
//
// PRODUCTION WRITER / SUBSCRIBER INVENTORY (Todo 2 deliverable)
// -------------------------------------------------------------
// Current code locations that write Thermal state or raise/consume Thermal
// events, each mapped to the DEC it must satisfy after the migration:
//
// W1. src/ViewModels/Thermal/ThermalViewModel.cs
//     - Observable-property partial handlers OnSelectedModeChanged /
//       OnSupplyTemperatureChanged / OnGroundTemperatureChanged /
//       OnSelectedPipeChanged / OnPipeSpacingChanged (:107-182): user edits ->
//       IMarkDirtyService.MarkDirty() + SetThermalNeedsRecalculation(<exact
//       Russian cause message>) iff Result != null; silent under _isResetting
//       or IsLoadProjectInProgress. -> DEC-T03.
//     - Calculate() (:301-373): validate -> SetThermalCalculating ->
//       CalculationContext.UpdateThermalInputs -> calculator.Calculate once ->
//       store result (valid or invalid) -> UpdateThermal always when non-null
//       (incl. invalid) -> ResetThermalState; exception path sets exact
//       "Ошибка расчёта: {ex.Message}", nulls result, publishes synthetic
//       invalid context result; reentrancy guarded by IsCalculating. -> DEC-T05.
//     - Reset() (:378-395): defaults/result/VM ValidationMessage only; does NOT
//       touch ICalculationStateService thermal status or pipe spacing. -> DEC-T03.
//     - LoadResult(result, inputs) (:401-407): restore-time canonical writer;
//       publishes UpdateThermalInputs then UpdateThermal. -> DEC-T08.
//     - OnClimateDataChanged (:446-453) / OnConstructionDataChanged (:458-472):
//       upstream USER invalidation subscribers clearing result + one
//       NeedsRecalculation iff result existed. -> DEC-T04.
//     - Constructor subscriptions (:269-283): ClimateData.DataChanged,
//       ConstructionData.DataChanged, StateChanged, PipeSpacingChanged. -> DEC-T04A.
// W2. src/Services/Navigation/CalculationStateService.cs
//     - Backing fields _thermalNeedsRecalculation/_thermalIsCalculating/
//       _thermalValidationMessage/_pipeSpacing (:24-31); SetThermal*
//       methods (:72-94); SetPipeSpacing(spacing, source) writer authority
//       guard (:171-184) rejecting non-canonical sources. -> DEC-T06/T07.
// W3. src/Core/CalculationContext.cs
//     - UpdateThermal (:176-185) / UpdateThermalInputs (:192-204) projection
//       publications; UpdateClimate/UpdateConstruction clear ThermalResult;
//       Reset(). ST-021/ST-022 seams. -> DEC-T07.
// W4. src/Services/Project/ProjectLoadOrchestrator.cs
//     - RestoreModulesFromProjectAsync (:91-221): direct VM writes for mode/
//       supply/ground (:122-124), guarded SetPipeSpacing (:125), structural
//       pipe match with first-standard fallback (:128-141), saved-result
//       assignment (:144-157), finalization valid->LoadResult else fallback
//       CalculateCommand (:207-216); ResetModules (:70-82) lifecycle reset.
//       -> DEC-T08.
// W5. src/ViewModels/Results/ResultsViewModel.cs
//     - SaveCurrentProject ThermalData projection (:1693-1718) reading VM +
//       service spacing; LoadProjectDataAsync restore lease scope
//       (:1576-1611); LoadProjectFromPathAsync error boundary. -> DEC-T08.
// W6. src/ViewModels/Shell/MainViewModel.cs PerformNewCalculationReset
//     (:226-241): lifecycle user reset calling _thermalViewModel.Reset()
//     without touching service thermal status/spacing. -> DEC-T03/T04 origins.
// S1. Subscribers: CircuitsViewModel ctor (:721-730) StateChanged +
//     PipeSpacingChanged + ContextChanged; OnCalculationContextChanged
//     (:1062-1088) notification-only for inputs, exactly one
//     CalculateAllCollectors for valid results, zero for invalid/null;
//     OnPipeSpacingChanged (:1093-1107) spacing/10.0 propagation. -> DEC-T07.
//
// MEASURED COUNTER UNITS (frozen by this suite):
// - One logical CalculateAllCollectors() performs exactly TWO
//   ICircuitsCalculator.CalculateCollectorSummary invocations in the current
//   code (collector summary computation + hydraulic summary-card rebuild).
// - The Thermal dirty-intent counter below observes ONLY the IMarkDirtyService
//   instance injected into ThermalViewModel, isolating Thermal intent from the
//   intents issued by Construction/Circuits/Results components sharing the
//   session.
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Tests.Fixtures;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Services.Project;

/// <summary>
/// Phase 4 Todo 2 characterization of current Thermal writers, subscribers,
/// calculations, lifecycle and persistence behavior (DEC-T03..T08).
/// Every test freezes CURRENT observable behavior; no production code is changed.
/// </summary>
[TestFixture]
public sealed class ThermalMultiplicityCharacterizationTests
{
    private const string ModeMessage = "Режим работы изменён. Требуется пересчёт.";
    private const string SupplyMessage = "Температура подачи изменена. Требуется пересчёт.";
    private const string GroundMessage = "Температура грунта изменена. Требуется пересчёт.";
    private const string PipeMessage = "Тип трубы изменён. Требуется пересчёт.";
    private const string SpacingMessage = "Шаг укладки изменён. Требуется пересчёт.";
    private const string ClimateMessage = "Климатические данные изменены. Требуется пересчёт.";
    private const string ConstructionMessage = "Данные конструкции изменены. Требуется пересчёт.";

    #region Own input edits (DEC-T03)

    [Test]
    public void OwnInputEdit_ModeChanged_WithResult_PreservesResultAndSetsNeedsRecalculationExactlyOnce()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(111.0);
        fixture.Session.MarkClean();
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.SelectedMode = OperatingMode.Intensive;

        AssertOwnEditWithResult(fixture, ModeMessage, 111.0, hydraulicsBefore);
        Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.Intensive));
    }

    [Test]
    public void OwnInputEdit_SupplyTemperatureChanged_WithResult_ExactMessageAndSingleCompletion()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(111.0);
        fixture.Session.MarkClean();
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.SupplyTemperature = 55.0;

        AssertOwnEditWithResult(fixture, SupplyMessage, 111.0, hydraulicsBefore);
        Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(55.0));
    }

    [Test]
    public void OwnInputEdit_GroundTemperatureChanged_WithResult_ExactMessageAndSingleCompletion()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(111.0);
        fixture.Session.MarkClean();
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.GroundTemperature = 5.0;

        AssertOwnEditWithResult(fixture, GroundMessage, 111.0, hydraulicsBefore);
        Assert.That(fixture.ThermalViewModel.GroundTemperature, Is.EqualTo(5.0));
    }

    [Test]
    public void OwnInputEdit_SelectedPipeChanged_WithResult_ExactMessageAndIsPipeSpacingEnabledNotification()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(111.0);
        fixture.Session.MarkClean();
        var spacingEnabledNotifications = 0;
        fixture.ThermalViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ThermalViewModel.IsPipeSpacingEnabled))
            {
                spacingEnabledNotifications++;
            }
        };
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];

        AssertOwnEditWithResult(fixture, PipeMessage, 111.0, hydraulicsBefore);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.SelectedPipe, Is.EqualTo(PipeType.StandardPipes[1]));
            Assert.That(spacingEnabledNotifications, Is.EqualTo(1));
        });
    }

    public enum ThermalInputKind
    {
        Mode,
        SupplyTemperature,
        GroundTemperature,
        SelectedPipe
    }

    [TestCase(ThermalInputKind.Mode)]
    [TestCase(ThermalInputKind.SupplyTemperature)]
    [TestCase(ThermalInputKind.GroundTemperature)]
    [TestCase(ThermalInputKind.SelectedPipe)]
    public void OwnInputEdit_ChangedWithoutResult_MarksDirtyOnceWithoutRecalculationEvent(ThermalInputKind input)
    {
        var fixture = CreateFixture();
        Assert.That(fixture.ThermalViewModel.Result, Is.Null);
        fixture.Recorder.Reset();

        switch (input)
        {
            case ThermalInputKind.Mode:
                fixture.ThermalViewModel.SelectedMode = OperatingMode.AntiIcing;
                break;
            case ThermalInputKind.SupplyTemperature:
                fixture.ThermalViewModel.SupplyTemperature = 60.0;
                break;
            case ThermalInputKind.GroundTemperature:
                fixture.ThermalViewModel.GroundTemperature = 15.0;
                break;
            case ThermalInputKind.SelectedPipe:
                fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[0];
                break;
        }

        Assert.Multiple(() =>
        {
            // Dirty intent is issued even without a result...
            Assert.That(fixture.DirtyIntentCount, Is.EqualTo(1));
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.EqualTo(1));
            Assert.That(fixture.Session.IsDirty, Is.True);
            // ...but no recalculation status is synthesized when nothing was calculated yet.
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty);
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.Zero);
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(HydraulicsCalculations(fixture), Is.Zero);
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
        });
    }

    [Test]
    public void OwnInputEdit_NoOpEdits_AreCompletelySilent()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];
        fixture.LoadResult(222.0);
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        // Same values as current state: CommunityToolkit setters skip equal values.
        fixture.ThermalViewModel.SelectedMode = OperatingMode.Melting;
        fixture.ThermalViewModel.SupplyTemperature = 50.0;
        fixture.ThermalViewModel.GroundTemperature = 10.0;
        fixture.ThermalViewModel.PipeSpacing = 200;
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];

        Assert.Multiple(() =>
        {
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero,
                "No-op edits issue zero additional MarkDirty intents.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero);
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty);
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty);
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.Zero);
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(222.0));
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public void OwnInputEdit_SecondEditWhileDirty_DirtyIntentAccumulates_TransitionsStayAtOne()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(333.0);

        // First edit on a clean session: one intent, one observable transition.
        fixture.ThermalViewModel.SupplyTemperature = 55.0;
        Assert.Multiple(() =>
        {
            Assert.That(fixture.DirtyIntentCount, Is.EqualTo(1));
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.EqualTo(1));
        });

        fixture.Recorder.Reset();

        // Second edit while already dirty: idempotent MarkDirty adds intent but no transition.
        fixture.ThermalViewModel.GroundTemperature = 5.0;

        Assert.Multiple(() =>
        {
            Assert.That(fixture.DirtyIntentCount, Is.EqualTo(2),
                "Each changed logical edit issues its own MarkDirty intent.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero,
                "ProjectSession.MarkDirty is idempotent: no additional IsDirty transition while already dirty.");
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.NeedsRecalculation }));
            Assert.That(fixture.Recorder.ThermalStates.Single().Message, Is.EqualTo(GroundMessage));
        });
    }

    [Test]
    public void OwnInputEdit_NeverInvokesCalculatorOrPublishesContextBeforeCalculate()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(444.0);
        fixture.Session.MarkClean();
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.SupplyTemperature = 65.0;
        fixture.ThermalViewModel.SelectedMode = OperatingMode.Intensive;

        Assert.Multiple(() =>
        {
            Assert.That(CalculatorInvocations(fixture), Is.Zero,
                "Input edits must never invoke the calculator.");
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty,
                "Input edits must not publish CalculationContext.ThermalInputs until Calculate/restore completion.");
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(444.0),
                "Own input edit preserves the last derived result.");
        });
    }

    #endregion

    #region User reset vs lifecycle reset (DEC-T03)

    [Test]
    public void UserReset_RestoresDefaultsAndClearsResultWithoutDirtyOrEvents()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];
        fixture.ThermalViewModel.SupplyTemperature = 70.0;
        fixture.ThermalViewModel.PipeSpacing = 250;
        fixture.LoadResult(555.0);
        fixture.ThermalViewModel.GroundTemperature = 5.0; // leaves session dirty and status NeedsRecalculation
        Assert.That(fixture.Session.IsDirty, Is.True);
        var intentsBefore = fixture.DirtyIntentCount;
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.ResetCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(fixture.ThermalViewModel.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(fixture.ThermalViewModel.SelectedPipe, Is.Null);
            Assert.That(fixture.ThermalViewModel.PipeSpacing, Is.EqualTo(200));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.ThermalViewModel.ValidationMessage, Is.Empty);
            // User reset does NOT mark dirty and does not emit any event...
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero,
                "User Thermal reset issues zero MarkDirty intents.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero);
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty);
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty);
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.Zero);
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
            // ...and currently does NOT roll back the dirty flag nor the service-side
            // thermal status/spacing stores (characterized legacy seams ST-013/ST-015).
            Assert.That(fixture.Session.IsDirty, Is.True,
                "Current behavior: user Thermal reset does not clean the project.");
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(250),
                "Current behavior: user Thermal reset does not write the service spacing store.");
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.True,
                "Current behavior: user Thermal reset does not reset the service-side thermal status.");
        });
    }

    [Test]
    public void LifecycleResetModules_IsSilentForThermalAndDoesNotDirty()
    {
        var fixture = CreateFixture();
        fixture.CalculationStateService.SetPipeSpacing(250, "ThermalViewModel");
        fixture.LoadResult(666.0);
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();
        // AMZ-2 (2026-08-23): row updated from pre-Todo-9 quirk pin to DEC-T08
        // lifecycle-reset target: canonical defaults incl. spacing 200, exactly
        // one canonical completion at state level, legacy surface stays silent.
        var canonicalCompletions = 0;
        fixture.Session.ThermalState.Changed += (_, _) => canonicalCompletions++;

        fixture.Orchestrator.ResetModules();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(fixture.ThermalViewModel.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(fixture.ThermalViewModel.SelectedPipe, Is.Null);
            Assert.That(fixture.ThermalViewModel.PipeSpacing, Is.EqualTo(200));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(canonicalCompletions, Is.EqualTo(1),
                "Lifecycle reset applies canonical defaults with exactly one canonical completion.");
            Assert.That(fixture.Session.ThermalState.Snapshot.Inputs.PipeSpacing, Is.EqualTo(200));
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(200),
                "AMZ-2: the service getter reflects canonical defaults after lifecycle reset.");
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero,
                "Lifecycle reset issues zero Thermal dirty intents.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero);
            Assert.That(fixture.Session.IsDirty, Is.False);
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty,
                "Lifecycle reset must not invalidate Thermal on the legacy surface.");
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty,
                "Lifecycle reset publishes no ThermalInputs/ThermalResult projections.");
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
        });
    }

    #endregion

    #region Upstream user invalidation (DEC-T04)

    [Test]
    public void ClimateUserInvalidation_WithResult_ClearsResultSetsRecalculationOnceWithoutThermalDirty()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(777.0);
        fixture.Session.MarkClean();
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ClimateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

        AssertUpstreamInvalidation(fixture, ClimateMessage, hydraulicsBefore);
    }

    [Test]
    public void ClimateUserInvalidation_WithoutResult_IsSilent()
    {
        var fixture = CreateFixture();
        Assert.That(fixture.ThermalViewModel.Result, Is.Null);
        fixture.Recorder.Reset();

        fixture.ClimateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

        AssertUpstreamSilence(fixture);
    }

    [Test]
    public void ConstructionUserInvalidation_WithResult_ClearsResultSetsRecalculationOnceWithoutThermalDirty()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(888.0);
        fixture.Session.MarkClean();
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        fixture.ConstructionProjection.RaiseDataChanged();

        AssertUpstreamInvalidation(fixture, ConstructionMessage, hydraulicsBefore);
    }

    [Test]
    public void ConstructionUserInvalidation_WithoutResult_IsSilent()
    {
        var fixture = CreateFixture();
        Assert.That(fixture.ThermalViewModel.Result, Is.Null);
        fixture.Recorder.Reset();

        fixture.ConstructionProjection.RaiseDataChanged();

        AssertUpstreamSilence(fixture);
    }

    #endregion

    #region Calculation matrix (DEC-T05)

    [Test]
    public async Task Calculate_ValidResult_ExactPublicationOrderCountsAndNoDirty()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        await fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);

        var publishedInputs = (ThermalInputs)fixture.Recorder.ContextEvents
            .Single(args => args.PropertyName == nameof(CalculationContext.ThermalInputs)).NewValue!;

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Recorder.ContextPublications.ToArray(),
                Is.EqualTo(new[] { nameof(CalculationContext.ThermalInputs), nameof(CalculationContext.ThermalResult) }),
                "Calculate publishes calculated inputs first, then the result.");
            Assert.That(publishedInputs.Mode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(publishedInputs.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(publishedInputs.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(publishedInputs.PipeSpacing, Is.EqualTo(200.0));
            Assert.That(publishedInputs.Pipe, Is.EqualTo(PipeType.StandardPipes[1]));
            Assert.That(publishedInputs.LambdaE, Is.EqualTo(fixture.ConstructionProjection.LambdaE));

            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.Calculating, ModuleState.Actual }));
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.IsValid, Is.True);
            Assert.That(fixture.ThermalViewModel.ValidationMessage, Is.Empty);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(fixture.CalculationStateService.ThermalValidationMessage, Is.Empty);
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero,
                "Calculation creates no dirty intent.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero);
            Assert.That(fixture.Session.IsDirty, Is.False);
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.EqualTo(2),
                "A valid result triggers exactly one logical CalculateAllCollectors, which currently performs two CalculateCollectorSummary invocations (collector summary + summary-card rebuild).");
        });
    }

    [Test]
    [Category("CalculationFailure")]
    public async Task Calculate_InvalidInput_ZeroCalculatorZeroContextPhaseUnchanged()
    {
        var fixture = CreateFixture();
        fixture.ThermalValidator.Setup(validator => validator.Validate(It.IsAny<ThermalInputs>()))
            .Returns(ValidationResult.Failure(new[] { "Температура подачи вне допустимого диапазона" }));
        fixture.LoadResult(999.0);
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        var hydraulicsBefore = HydraulicsCalculations(fixture);
        fixture.Recorder.Reset();

        // Awaited completion (same idiom as the green regression suites):
        // AsyncRelayCommand.Execute is fire-and-forget and must not be used here.
        await fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.ValidationMessage,
                Is.EqualTo("Температура подачи вне допустимого диапазона"));
            Assert.That(CalculatorInvocations(fixture), Is.Zero,
                "Invalid input must never reach the calculator.");
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty,
                "Invalid input publishes no context projections.");
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty,
                "Phase stays unchanged on rejected input.");
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(999.0),
                "Canonical last result is unchanged on rejected input.");
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero);
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
        });
    }

    [Test]
    [Category("CalculationFailure")]
    public async Task Calculate_Exception_SetsExactErrorMessageNullResultAndInvalidContextPublication()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        fixture.CalcMock.Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Throws(new InvalidOperationException("инъектированный сбой калькулятора"));
        fixture.Recorder.Reset();

        // Awaited completion (same idiom as the green regression suites):
        // AsyncRelayCommand.Execute is fire-and-forget and must not be used here.
        await fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);

        var failurePublication = fixture.Recorder.ContextEvents
            .Single(args => args.PropertyName == nameof(CalculationContext.ThermalResult));
        var publishedFailure = (ThermalCalculationResult)failurePublication.NewValue!;

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.ValidationMessage,
                Is.EqualTo("Ошибка расчёта: инъектированный сбой калькулятора"));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.Recorder.ContextPublications.ToArray(),
                Is.EqualTo(new[] { nameof(CalculationContext.ThermalInputs), nameof(CalculationContext.ThermalResult) }),
                "Exception path still publishes the compatibility invalid result once.");
            Assert.That(publishedFailure.IsValid, Is.False);
            Assert.That(publishedFailure.ValidationErrors,
                Is.EqualTo(new[] { "Ошибка расчёта: инъектированный сбой калькулятора" }));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.Calculating, ModuleState.Actual }));
            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1));
            Assert.That(HydraulicsCalculations(fixture), Is.Zero,
                "The synthetic invalid failure result must not trigger Hydraulics.");
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero,
                "Calculation failure creates no additional dirty intent.");
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    [Category("CalculationFailure")]
    public async Task Calculate_ReentrantWhileCalculating_PerformsNoSecondCalculatorHit()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.CalcMock.Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Callback(() =>
            {
                entered.TrySetResult();
                release.Task.Wait();
            })
            .Returns(new ThermalCalculationResult
            {
                PowerTotal = 42.0,
                DeltaT = 15.0,
                MeanTemperature = 47.5,
                IsValid = true
            });
        var hydraulicsBefore = HydraulicsCalculations(fixture);

        var firstRun = fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);
        await entered.Task; // deterministic handshake: the calculator is inside Calculate

        Assert.That(fixture.ThermalViewModel.IsCalculating, Is.True);

        var secondRun = fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);
        await secondRun;

        Assert.Multiple(() =>
        {
            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1),
                "Reentrant Calculate while Calculating performs no second calculator hit.");
            Assert.That(
                fixture.Recorder.ContextPublications.Count(name => name == nameof(CalculationContext.ThermalResult)),
                Is.Zero,
                "No result publication happened before the gated calculator returned.");
        });

        release.TrySetResult();
        await firstRun;

        Assert.Multiple(() =>
        {
            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1));
            Assert.That(fixture.Recorder.ContextPublications.ToArray(),
                Is.EqualTo(new[] { nameof(CalculationContext.ThermalInputs), nameof(CalculationContext.ThermalResult) }));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.Calculating, ModuleState.Actual }));
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.EqualTo(2),
                "Exactly one logical downstream calculation for the single valid publication.");
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(42.0));
        });
    }

    [Test]
    [Category("CalculationFailure")]
    public async Task Calculate_CalculatorReturnedInvalidResult_StoredCanonicallyPublishedOnceZeroHydraulics()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];
        fixture.CalcMock.Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Returns(new ThermalCalculationResult
            {
                PowerTotal = 10.0,
                DeltaT = 15.0,
                MeanTemperature = 47.5,
                IsValid = false,
                ValidationErrors = new[] { "Мощность недостаточна для таяния снега" }
            });
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        fixture.Recorder.Reset();

        // Awaited completion (same idiom as the green regression suites):
        // AsyncRelayCommand.Execute is fire-and-forget and must not be used here.
        await fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.IsValid, Is.False);
            Assert.That(fixture.ThermalViewModel.Result.PowerTotal, Is.EqualTo(10.0),
                "Calculator-returned invalid result is stored canonically.");
            Assert.That(fixture.ThermalViewModel.ValidationMessage,
                Is.EqualTo("Мощность недостаточна для таяния снега"));
            Assert.That(
                fixture.Recorder.ContextPublications.Count(name => name == nameof(CalculationContext.ThermalResult)),
                Is.EqualTo(1),
                "Invalid result is still published exactly once.");
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.Calculating, ModuleState.Actual }));
            Assert.That(HydraulicsCalculations(fixture), Is.Zero,
                "Invalid result causes zero Hydraulics calculations.");
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.Zero);
        });
    }

    #endregion

    #region Pipe structural equality semantics (DEC-T08)

    [Test]
    public void PipeStructuralEquality_IgnoresArticleAndConductivityCaseInsensitiveName()
    {
        var standard = PipeType.StandardPipes[1]; // RAUTHERM S 20x2,0

        var sameButMetadata = new PipeType
        {
            Name = "rautherm s 20x2,0",
            Article = "DIFFERENT-ARTICLE",
            OuterDiameter = 20,
            InnerDiameter = 16,
            WallThickness = 2.0,
            ThermalConductivity = 0.99
        };
        var differentThickness = new PipeType
        {
            Name = "RAUTHERM S 20x2,0",
            OuterDiameter = 20,
            InnerDiameter = 16,
            WallThickness = 2.5
        };

        Assert.Multiple(() =>
        {
            Assert.That(sameButMetadata == standard, Is.True,
                "Current comparison ignores Article/ThermalConductivity and matches name case-insensitively.");
            Assert.That(differentThickness == standard, Is.False,
                "Different wall thickness breaks structural equality.");
            Assert.That(PipeType.StandardPipes.Select(pipe => pipe.Name).ToArray(),
                Is.EqualTo(new[] { "RAUTHERM S 17x2,0", "RAUTHERM S 20x2,0", "RAUTHERM S 25x2,3" }));
        });
    }

    #endregion

    #region Pipe spacing compatibility (DEC-T06)

    [Test]
    public void Spacing_UserEdit_PropagatesToServiceCircuitsAndHydraulicsExactlyOnce()
    {
        // Fresh graph without a prior context publication: the circuit receives the
        // edited spacing through the service event path (spacing / 10.0).
        var fixture = CreateFixture();
        Assert.That(fixture.ThermalViewModel.Result, Is.Null);
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.PipeSpacing = 250;

        var circuit = fixture.CircuitsViewModel.Collectors.Single().Circuits.First();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(250));
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.EqualTo(1));
            Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(25.0),
                "Every circuit receives spacing / 10.0.");
            Assert.That(HydraulicsCalculations(fixture), Is.EqualTo(2),
                "One changed spacing edit causes exactly one logical CalculateAllCollectors (two summary invocations).");
            Assert.That(fixture.DirtyIntentCount, Is.EqualTo(1));
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.EqualTo(1));
            // No result existed, so no recalculation status is synthesized.
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
        });
    }

    [Test]
    public void Spacing_UserEdit_WithResult_SetsExactRecalcMessageAndPreservesResult()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(121.0);
        fixture.Session.MarkClean();
        var intentsBefore = fixture.DirtyIntentCount;
        fixture.Recorder.Reset();

        fixture.ThermalViewModel.PipeSpacing = 250;

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.EqualTo(1));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.NeedsRecalculation }));
            Assert.That(fixture.Recorder.ThermalStates.Single().Message, Is.EqualTo(SpacingMessage));
            Assert.That(fixture.DirtyIntentCount - intentsBefore, Is.EqualTo(1),
                "One changed spacing edit issues exactly one Thermal dirty intent.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.EqualTo(1));
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(121.0),
                "Spacing edit preserves the last result.");
        });
    }

    [Test]
    public void Spacing_ServiceSetPipeSpacing_ChangedFiresOnceNoOpSilent()
    {
        var fixture = CreateFixture();
        fixture.Recorder.Reset();

        fixture.CalculationStateService.SetPipeSpacing(150, "ThermalViewModel");
        fixture.CalculationStateService.SetPipeSpacing(150, "ThermalViewModel");

        Assert.Multiple(() =>
        {
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(150));
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.EqualTo(1),
                "Changed spacing fires exactly one event; the repeated no-op fires none.");
            Assert.That(HydraulicsCalculations(fixture), Is.EqualTo(2),
                "One logical downstream recalculation (two summary invocations) for one changed event.");
        });
    }

    [Test]
    public void Spacing_DirectWriterFromNonCanonicalSource_IsRejectedByGuard()
    {
        var fixture = CreateFixture();
        fixture.Recorder.Reset();

        Assert.Throws<InvalidOperationException>(
            () => fixture.CalculationStateService.SetPipeSpacing(300, "RogueDirectWriter"));

        Assert.Multiple(() =>
        {
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(200),
                "Rejected direct write must not change canonical spacing.");
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.Zero);
            Assert.That(HydraulicsCalculations(fixture), Is.Zero);
        });
    }

    [Test]
    public void Spacing_RestoreSourceAllowedOnlyUnderRestoreGuard()
    {
        var fixture = CreateFixture();
        fixture.Recorder.Reset();

        Assert.Throws<InvalidOperationException>(
            () => fixture.CalculationStateService.SetPipeSpacing(280, "ProjectLoadOrchestrator.RestoreModules"));
        Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(200));

        using (fixture.Session.BeginProjectRestore())
        {
            fixture.CalculationStateService.SetPipeSpacing(280, "ProjectLoadOrchestrator.RestoreModules");
        }

        Assert.Multiple(() =>
        {
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(280));
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.EqualTo(1));
        });
    }

    #endregion

    #region Restore and persistence matrix (DEC-T08)

    [Test]
    public async Task Restore_ValidSavedResult_CalculatorZeroResultSurvivesLoadClean()
    {
        var fixture = CreateFixture();
        var project = CreateProject(
            OperatingMode.Intensive, 55.0, 8.0, 250, 1,
            new ThermalResultProjectData
            {
                PowerTotal = 777.0,
                SupplyTemperature = 55.0,
                ReturnTemperature = 40.0,
                MeanTemperature = 47.5,
                DeltaT = 15.0,
                IsValid = true
            });
        fixture.Recorder.Reset();

        await fixture.ResultsViewModel.LoadProjectDataAsync(project);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(777.0));
            Assert.That(fixture.ThermalViewModel.Result.IsValid, Is.True);
            Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.Intensive));
            Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(55.0));
            Assert.That(fixture.ThermalViewModel.GroundTemperature, Is.EqualTo(8.0));
            Assert.That(fixture.ThermalViewModel.PipeSpacing, Is.EqualTo(250));
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(250));
            Assert.That(fixture.ThermalViewModel.SelectedPipe,
                Is.SameAs(PipeType.StandardPipes[1]),
                "Persisted pipe matching a standard resolves to the canonical standard instance.");
            Assert.That(CalculatorInvocations(fixture), Is.Zero,
                "Valid saved result restores without any calculator invocation.");
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty,
                "Phase 3.1 contract: load lifecycle produces zero Thermal invalidation states.");
            Assert.That(fixture.Recorder.ContextPublications.ToArray(),
                Is.EqualTo(new[] { nameof(CalculationContext.ThermalInputs), nameof(CalculationContext.ThermalResult) }),
                "LoadResult publishes restored inputs then result exactly once each.");
            Assert.That(fixture.Session.IsDirty, Is.False);
            Assert.That(fixture.Recorder.ProjectChangedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Restore_AbsentSavedResult_FallbackCalculatesExactlyOnce()
    {
        var fixture = CreateFixture();
        var project = CreateProject(OperatingMode.AntiIcing, 45.0, 5.0, 300, 2, result: null);
        fixture.Recorder.Reset();

        await fixture.ResultsViewModel.LoadProjectDataAsync(project);

        Assert.Multiple(() =>
        {
            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1),
                "Absent saved result falls back to exactly one full calculation.");
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.IsValid, Is.True);
            Assert.That(fixture.ThermalViewModel.Result.PowerTotal, Is.EqualTo(555.0));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.Calculating, ModuleState.Actual }));
            Assert.That(fixture.Recorder.ContextPublications.ToArray(),
                Is.EqualTo(new[] { nameof(CalculationContext.ThermalInputs), nameof(CalculationContext.ThermalResult) }));
            Assert.That(fixture.Session.IsDirty, Is.False);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
        });
    }

    [Test]
    [Category("PersistenceFailure")]
    public async Task Restore_InvalidSavedResult_CalculatorOnceInvalidResultNotFinalCanonical()
    {
        var fixture = CreateFixture();
        var project = CreateProject(
            OperatingMode.Melting, 50.0, 10.0, 200, 1,
            new ThermalResultProjectData { PowerTotal = 999.0, IsValid = false });
        fixture.Recorder.Reset();

        await fixture.ResultsViewModel.LoadProjectDataAsync(project);

        Assert.Multiple(() =>
        {
            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1),
                "An invalid saved result forces exactly one fallback calculation.");
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.IsValid, Is.True);
            Assert.That(fixture.ThermalViewModel.Result.PowerTotal, Is.EqualTo(555.0),
                "The invalid saved value must not become the final canonical result.");
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    [Category("PersistenceFailure")]
    public async Task Restore_UnknownPersistedPipe_FallsBackToFirstStandardPipe()
    {
        var fixture = CreateFixture();
        var project = CreateProject(
            OperatingMode.Melting, 50.0, 10.0, 200, null,
            new ThermalResultProjectData
            {
                PowerTotal = 777.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 35.0,
                MeanTemperature = 42.5,
                DeltaT = 15.0,
                IsValid = true
            });
        project.ThermalData.SelectedPipe = new PipeTypeProjectData
        {
            Name = "PHASE4 UNKNOWN PIPE",
            OuterDiameter = 99.0,
            InnerDiameter = 95.0,
            WallThickness = 2.0
        };
        fixture.Recorder.Reset();

        await fixture.ResultsViewModel.LoadProjectDataAsync(project);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.SelectedPipe,
                Is.SameAs(PipeType.StandardPipes[0]),
                "Unknown persisted pipe falls back to the first available standard pipe.");
            Assert.That(fixture.ThermalViewModel.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 17x2,0"));
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(777.0));
        });
    }

    [Test]
    public async Task Restore_NullPersistedPipe_PipeRemainsNullAfterLifecycleReset()
    {
        var fixture = CreateFixture();
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[0];
        fixture.Orchestrator.ResetModules();
        Assert.That(fixture.ThermalViewModel.SelectedPipe, Is.Null, "Lifecycle reset clears the pipe.");

        var project = CreateProject(
            OperatingMode.Melting, 50.0, 10.0, 200, null,
            new ThermalResultProjectData
            {
                PowerTotal = 777.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 35.0,
                MeanTemperature = 42.5,
                DeltaT = 15.0,
                IsValid = true
            });
        project.ThermalData.SelectedPipe = null;
        fixture.Recorder.Reset();

        await ResetAndRestoreAsync(fixture, project);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.SelectedPipe, Is.Null,
                "Persisted null pipe keeps the pipe null after lifecycle reset.");
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(777.0));
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task Restore_MissingLegacySpacing_FallsBackTo200ThroughPersistencePath()
    {
        var originalPath = Path.GetFullPath(FixturePath);
        var json = await File.ReadAllTextAsync(originalPath);
        using (var document = JsonDocument.Parse(json))
        {
            var root = document.RootElement;
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals("thermalData"))
                    {
                        writer.WritePropertyName(property.Name);
                        writer.WriteStartObject();
                        foreach (var thermalProperty in property.Value.EnumerateObject())
                        {
                            if (!thermalProperty.NameEquals("pipeSpacing"))
                            {
                                thermalProperty.WriteTo(writer);
                            }
                        }

                        writer.WriteEndObject();
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"phase4-t2-missing-{Guid.NewGuid():N}.smc");
            await File.WriteAllBytesAsync(tempPath, stream.ToArray());
            try
            {
                var operation = await new ProjectFileService().LoadProjectResultAsync(tempPath);
                Assert.That(operation.IsSuccess, Is.True, "Sanity: stripped fixture must deserialize.");
                var data = operation.Value!;
                Assert.That(data.ThermalData.PipeSpacing, Is.EqualTo(200),
                    "Legacy files without pipeSpacing fall back to the DTO default 200.");

                var fixture = CreateFixture();
                fixture.CalculationStateService.SetPipeSpacing(300, "ThermalViewModel");
                fixture.Recorder.Reset();

                await fixture.ResultsViewModel.LoadProjectDataAsync(data);

                Assert.Multiple(() =>
                {
                    Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(200));
                    Assert.That(fixture.ThermalViewModel.PipeSpacing, Is.EqualTo(200));
                    Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.EqualTo(1),
                        "Restoring 300 -> 200 emits exactly one compatibility spacing event.");
                });
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    [Category("PersistenceFailure")]
    public async Task LoadCorruptProjectFile_ShowsErrorKeepsPriorProjectAndThermalStateUntouched()
    {
        var corruptPath = Path.Combine(Path.GetTempPath(), $"phase4-t2-corrupt-{Guid.NewGuid():N}.smc");
        await File.WriteAllTextAsync(corruptPath, "{ this is not valid json");
        try
        {
            // Real persistence service: the failure is produced by the actual
            // JSON deserialization code path, not by a stubbed result.
            var fixture = CreateFixture(projectFileService: new ProjectFileService());
            var project = CreateProject(
                OperatingMode.Intensive, 55.0, 8.0, 250, 1,
                new ThermalResultProjectData
                {
                    PowerTotal = 777.0,
                    SupplyTemperature = 55.0,
                    ReturnTemperature = 40.0,
                    MeanTemperature = 47.5,
                    DeltaT = 15.0,
                    IsValid = true
                });
            await fixture.ResultsViewModel.LoadProjectDataAsync(project);
            fixture.Recorder.Reset();

            await fixture.ResultsViewModel.LoadProjectFromPathAsync(corruptPath);

            Assert.Multiple(() =>
            {
                Assert.That(fixture.ShownError, Does.StartWith("Не удалось открыть проект: Ошибка десериализации"),
                    "The current persistence boundary reports deserialization failures through the error dialog.");
                Assert.That(fixture.Session.CurrentFilePath, Is.Null,
                    "A failed open must not set the current file path.");
                Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(55.0));
                Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(777.0),
                    "Prior project thermal state remains untouched after a failed open.");
                Assert.That(CalculatorInvocations(fixture), Is.Zero);
                Assert.That(fixture.Recorder.ThermalStates, Is.Empty);
            });
        }
        finally
        {
            File.Delete(corruptPath);
        }
    }

    [Test]
    public async Task SecondProjectLoad_ReplacesAllThermalState_CalculatesFallbackOnce()
    {
        // AMZ-2 (2026-08-23): row updated from pre-Todo-9 quirk pin to DEC-T08 second-load target.
        var fixture = CreateFixture();
        var projectA = CreateProject(
            OperatingMode.Intensive, 55.0, 8.0, 250, 1,
            new ThermalResultProjectData
            {
                PowerTotal = 777.0,
                SupplyTemperature = 55.0,
                ReturnTemperature = 40.0,
                MeanTemperature = 47.5,
                DeltaT = 15.0,
                IsValid = true
            });
        var projectB = CreateProject(OperatingMode.AntiIcing, 45.0, 5.0, 300, 2, result: null);

        await fixture.ResultsViewModel.LoadProjectDataAsync(projectA);
        fixture.Recorder.Reset();
        await fixture.ResultsViewModel.LoadProjectDataAsync(projectB);

        Assert.Multiple(() =>
        {
            // Inputs from project B fully replace project A:
            Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.AntiIcing));
            Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(45.0));
            Assert.That(fixture.ThermalViewModel.GroundTemperature, Is.EqualTo(5.0));
            Assert.That(fixture.ThermalViewModel.PipeSpacing, Is.EqualTo(300));
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(300));
            Assert.That(fixture.ThermalViewModel.SelectedPipe, Is.SameAs(PipeType.StandardPipes[2]));
            // DEC-T08 second load: zero stale values — the fresh fallback result
            // fully replaces project A's saved result.
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(555.0),
                "Fresh fallback result replaces project A's saved result.");
            Assert.That(CalculatorInvocations(fixture), Is.EqualTo(1),
                "Absent saved result must fall back exactly once.");
            Assert.That(fixture.Session.ThermalState.Snapshot.Status.Phase,
                Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task RepeatedLoadResetCycles_DoNotMultiplyEventsSubscriptionsOrCalculations()
    {
        var fixture = CreateFixture();
        var project = CreateProject(
            OperatingMode.Intensive, 55.0, 8.0, 200, 1,
            new ThermalResultProjectData
            {
                PowerTotal = 777.0,
                SupplyTemperature = 55.0,
                ReturnTemperature = 40.0,
                MeanTemperature = 47.5,
                DeltaT = 15.0,
                IsValid = true
            });

        var calculatorBeforeFirst = CalculatorInvocations(fixture);
        var hydraulicsBeforeFirst = HydraulicsCalculations(fixture);
        await ResetAndRestoreAsync(fixture, project);
        var firstCycle = fixture.Recorder.Snapshot(
            CalculatorInvocations(fixture) - calculatorBeforeFirst,
            HydraulicsCalculations(fixture) - hydraulicsBeforeFirst);
        fixture.Recorder.Reset();

        var calculatorBeforeSecond = CalculatorInvocations(fixture);
        var hydraulicsBeforeSecond = HydraulicsCalculations(fixture);
        await ResetAndRestoreAsync(fixture, project);
        var secondCycle = fixture.Recorder.Snapshot(
            CalculatorInvocations(fixture) - calculatorBeforeSecond,
            HydraulicsCalculations(fixture) - hydraulicsBeforeSecond);

        Assert.Multiple(() =>
        {
            Assert.That(firstCycle.ThermalStateCount, Is.Zero);
            Assert.That(secondCycle.ThermalStateCount, Is.EqualTo(firstCycle.ThermalStateCount));
            Assert.That(secondCycle.ContextPublicationCount, Is.EqualTo(firstCycle.ContextPublicationCount));
            Assert.That(secondCycle.PipeSpacingChangedCount, Is.EqualTo(firstCycle.PipeSpacingChangedCount));
            // CANONICAL DIRTY CONTRACT (phase-5 correction): the second restore's
            // Climate publication still triggers the characterized +2 downstream
            // recalculation surplus (asserted below via HydraulicsCalculationDelta),
            // but calculation-origin work no longer raises dirty transitions at all
            // — only User-origin mutations reach ProjectSession.MarkDirty. The
            // equality pin keeps proving zero subscription/event multiplication.
            Assert.That(secondCycle.IsDirtyTransitionCount,
                Is.EqualTo(firstCycle.IsDirtyTransitionCount));
            Assert.That(secondCycle.ProjectChangedCount, Is.EqualTo(firstCycle.ProjectChangedCount));
            Assert.That(secondCycle.CalculatorInvocationDelta, Is.EqualTo(firstCycle.CalculatorInvocationDelta));
            // CHARACTERIZED STALE-RESULT SURPLUS (same legacy defect as the
            // second-load test above): in the second cycle the Climate lifecycle
            // publication still sees project A's valid ThermalResult and triggers
            // one extra logical downstream recalculation (+2 summary invocations).
            // Beyond that exactly-characterized surplus the cycles are identical,
            // which pins subscription/event multiplication at zero.
            Assert.That(secondCycle.HydraulicsCalculationDelta,
                Is.EqualTo(firstCycle.HydraulicsCalculationDelta + 2));
            Assert.That(fixture.Session.IsDirty, Is.False);

            // Subscription-balance probe: after repeated cycles a single user spacing
            // edit still produces exactly one delivery per consumer (no multiplied handlers).
            fixture.ThermalViewModel.PipeSpacing = 200; // normalize to a known value
            fixture.Recorder.Reset();
            var hydraulicsBeforeProbe = HydraulicsCalculations(fixture);
            fixture.ThermalViewModel.PipeSpacing = 250;

            Assert.Multiple(() =>
            {
                Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.EqualTo(1));
                Assert.That(HydraulicsDelta(fixture, hydraulicsBeforeProbe), Is.EqualTo(2),
                    "Repeated load/reset cycles must not multiply downstream subscriptions.");
            });
        });
    }

    #endregion

    #region Restore failure (DEC-T08 partial-state row)

    [Test]
    [Category("RestoreFailure")]
    public async Task LoadProjectDataAsync_EarlyRestoreFailure_ClearsLeasePreservesPartialThermalDefaults()
    {
        var constructionService = new Mock<IConstructionService>();
        constructionService
            .Setup(service => service.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
            .Throws(new InvalidOperationException("injected early boundary failure"));
        var fixture = CreateFixture(constructionService.Object);

        var project = CreateProject(
            OperatingMode.Intensive, 55.0, 8.0, 250, 1,
            new ThermalResultProjectData { PowerTotal = 777.0, IsValid = true });
        project.CustomMaterials = new List<MaterialSnapshot> { new MaterialSnapshot { Name = "Custom material" } };

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await fixture.ResultsViewModel.LoadProjectDataAsync(project));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("injected early boundary failure"));
            Assert.That(fixture.CalculationStateService.IsLoadProjectInProgress, Is.False,
                "Restore lease must be cleared even when restore throws.");
            Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting),
                "Thermal restore happens after the early failure point and retains defaults.");
            Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(200));
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(fixture.Session.IsDirty, Is.False,
                "Non-user lifecycle origins must not mark the partial project dirty.");
        });
    }

    [Test]
    [Category("RestoreFailure")]
    public async Task LoadProjectDataAsync_LateRestoreFailure_ClearsLeaseThermalRetainsPreFailureDefaults()
    {
        var constructionService = new Mock<IConstructionService>();
        constructionService
            .Setup(service => service.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
            .Returns(Task.CompletedTask);
        constructionService
            .Setup(service => service.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()))
            .Throws(new InvalidOperationException("injected late boundary failure"));
        var fixture = CreateFixture(constructionService.Object);

        var project = CreateProject(
            OperatingMode.Intensive, 55.0, 8.0, 250, 1,
            new ThermalResultProjectData { PowerTotal = 777.0, IsValid = true });
        project.CustomTemplates = new List<ConstructionTemplate> { new ConstructionTemplate { Name = "Custom template" } };

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await fixture.ResultsViewModel.LoadProjectDataAsync(project));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("injected late boundary failure"));
            Assert.That(fixture.CalculationStateService.IsLoadProjectInProgress, Is.False,
                "Late restore failure must also clear the lease.");
            Assert.That(fixture.ThermalViewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting),
                "Characterized non-transactional behavior: thermal inputs are restored after the late failure point.");
            Assert.That(fixture.ThermalViewModel.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.CalculationStateService.PipeSpacing, Is.EqualTo(200));
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    #endregion

    #region QA-failure modeling (plan Todo 2 QA-failure clause)

    /// <summary>
    /// Models a synthetic direct writer that bypasses the ThermalViewModel
    /// ownership and writes the compatibility status store directly. The
    /// characterization harness must REJECT the violation: the exact-sequence
    /// assertion throws on the violating recording while the canonical
    /// recording passes. Detection-only proof; no production change.
    /// </summary>
    [Test]
    public void QaFailure_SyntheticDirectWriter_ViolationDetectedByMultiplicityAssertions()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(321.0);
        fixture.Session.MarkClean();
        fixture.Recorder.Reset();

        // Canonical single-completion recording passes the harness.
        fixture.ThermalViewModel.SupplyTemperature = 56.0;
        AssertExactThermalStateSequence(fixture.Recorder, ModuleState.NeedsRecalculation);

        // Synthetic rogue direct writer bypassing the VM ownership surface.
        fixture.CalculationStateService.SetThermalNeedsRecalculation("rogue direct write");

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Recorder.ThermalStates, Has.Count.EqualTo(2),
                "The rogue write produced an extra completion beyond the characterized single one.");
            // The violation IS detected/rejected by the characterization assertion:
            Assert.Throws<AssertionException>(
                () => AssertExactThermalStateSequence(fixture.Recorder, ModuleState.NeedsRecalculation),
                "The multiplicity harness must reject the direct-writer violation.");
        });
    }

    /// <summary>
    /// Models a duplicated upstream subscriber whose handler re-executes the
    /// invalidation side effect. One climate change then yields two completions;
    /// the exact-count characterization detects the multiplication while the
    /// single-subscriber baseline passes.
    /// </summary>
    [Test]
    public void QaFailure_DuplicateSubscriber_ViolationDetectedByMultiplicityAssertions()
    {
        var fixture = CreateFixture();
        fixture.LoadResult(654.0);
        fixture.Session.MarkClean();

        void DuplicateEchoHandler(object? sender, ClimateDataChangedEventArgs args)
        {
            fixture.CalculationStateService.SetThermalNeedsRecalculation("duplicated subscriber echo");
        }

        fixture.ClimateData.DataChanged += DuplicateEchoHandler;
        try
        {
            fixture.Recorder.Reset();
            fixture.ClimateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

            Assert.Multiple(() =>
            {
                Assert.That(fixture.Recorder.ThermalStates, Has.Count.EqualTo(2),
                    "Duplicate subscription doubled the completion count.");
                Assert.Throws<AssertionException>(
                    () => AssertExactThermalStateSequence(fixture.Recorder, ModuleState.NeedsRecalculation),
                    "The multiplicity harness must detect duplicate-subscriber multiplication.");
            });
        }
        finally
        {
            fixture.ClimateData.DataChanged -= DuplicateEchoHandler;
        }

        // After removing the duplicate the canonical single-completion contract holds again.
        fixture.LoadResult(655.0);
        fixture.Session.MarkClean();
        fixture.Recorder.Reset();
        fixture.ThermalViewModel.SupplyTemperature = 57.0;
        Assert.DoesNotThrow(() => AssertExactThermalStateSequence(fixture.Recorder, ModuleState.NeedsRecalculation));
    }

    #endregion

    #region Helpers

    private static string FixturePath => Path.Combine(
        Path.GetDirectoryName(typeof(ThermalMultiplicityCharacterizationTests).Assembly.Location)!,
        "..", "..", "..", "Fixtures", "v1-sample.smc");

    private static int CalculatorInvocations(ThermalFixture fixture) =>
        fixture.CalcMock.Invocations.Count(invocation =>
            invocation.Method.Name == nameof(IThermalCalculator.Calculate));

    private static int HydraulicsCalculations(ThermalFixture fixture) =>
        fixture.CircuitsCalcMock.Invocations.Count(invocation =>
            invocation.Method.Name == nameof(ICircuitsCalculator.CalculateCollectorSummary));

    private static int HydraulicsDelta(ThermalFixture fixture, int before) =>
        HydraulicsCalculations(fixture) - before;

    private static void AssertOwnEditWithResult(
        ThermalFixture fixture,
        string expectedMessage,
        double resultPowerTotal,
        int hydraulicsBefore)
    {
        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(resultPowerTotal),
                "Own input edit preserves the last derived result.");
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.True);
            Assert.That(fixture.CalculationStateService.ThermalValidationMessage, Is.EqualTo(expectedMessage));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.NeedsRecalculation }));
            Assert.That(fixture.Recorder.ThermalStates.Single().Message, Is.EqualTo(expectedMessage));
            Assert.That(fixture.DirtyIntentCount, Is.EqualTo(1),
                "Exactly one Thermal MarkDirty intent per changed logical action.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.EqualTo(1),
                "Clean -> dirty transition happens exactly once.");
            Assert.That(fixture.Session.IsDirty, Is.True);
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty);
            Assert.That(fixture.Recorder.PipeSpacingChangedCount, Is.Zero);
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
        });
    }

    private static void AssertUpstreamInvalidation(ThermalFixture fixture, string expectedMessage, int hydraulicsBefore)
    {
        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Null,
                "Genuine upstream user change clears the canonical Thermal result once.");
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.True);
            Assert.That(fixture.CalculationStateService.ThermalValidationMessage, Is.EqualTo(expectedMessage));
            Assert.That(fixture.Recorder.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.NeedsRecalculation }));
            Assert.That(fixture.Recorder.ThermalStates.Single().Message, Is.EqualTo(expectedMessage));
            Assert.That(fixture.DirtyIntentCount, Is.Zero,
                "Upstream invalidation never marks dirty again; the upstream module owns that action.");
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero);
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty);
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(HydraulicsDelta(fixture, hydraulicsBefore), Is.Zero);
        });
    }

    private static void AssertUpstreamSilence(ThermalFixture fixture)
    {
        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(fixture.Recorder.ThermalStates, Is.Empty);
            Assert.That(fixture.DirtyIntentCount, Is.Zero);
            Assert.That(fixture.Recorder.IsDirtyTransitions, Is.Zero);
            Assert.That(fixture.Recorder.ContextPublications, Is.Empty);
            Assert.That(CalculatorInvocations(fixture), Is.Zero);
            Assert.That(HydraulicsCalculations(fixture), Is.Zero);
        });
    }

    private static void AssertExactThermalStateSequence(Recorder recorder, params ModuleState[] expected)
    {
        Assert.That(
            recorder.ThermalStates.Select(args => args.State).ToArray(),
            Is.EqualTo(expected),
            "Exact thermal state sequence violated.");
    }

    private static ProjectData CreateProject(
        OperatingMode mode,
        double supply,
        double ground,
        int spacing,
        int? pipeIndex,
        ThermalResultProjectData? result)
    {
        var project = new ProjectData
        {
            Version = "1.1",
            ProjectNumber = "PHASE4-T2",
            ProjectObject = "Thermal characterization",
            ClimateData = new ClimateProjectData
            {
                SelectedCity = "Loaded city",
                Region = "Loaded region",
                AirTemperature = -20.0,
                WindSpeed = 7.0,
                Humidity = 80.0,
                SnowfallIntensity = 3.0,
                SelectedZone = ClimateZone.Zone_M20
            },
            ConstructionData = new ConstructionProjectData(),
            ThermalData = new ThermalProjectData
            {
                SelectedMode = mode,
                SupplyTemperature = supply,
                GroundTemperature = ground,
                PipeSpacing = spacing,
                Result = result
            },
            HydraulicsData = new HydraulicsProjectData()
        };

        if (pipeIndex.HasValue)
        {
            var standard = PipeType.StandardPipes[pipeIndex.Value];
            project.ThermalData.SelectedPipe = new PipeTypeProjectData
            {
                Name = standard.Name,
                OuterDiameter = standard.OuterDiameter,
                InnerDiameter = standard.InnerDiameter,
                WallThickness = standard.WallThickness
            };
        }

        project.HydraulicsData.Collectors.Add(new CollectorProjectData
        {
            CollectorNumber = 1,
            Circuits = new List<CircuitProjectData>
            {
                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 100 }
            }
        });

        return project;
    }

    private static async Task ResetAndRestoreAsync(ThermalFixture fixture, ProjectData project)
    {
        fixture.Orchestrator.ResetModules();
        using var restoreScope = fixture.Session.BeginProjectRestore();
        fixture.CalculationStateService.IsLoadProjectInProgress = true;
        try
        {
            await fixture.Orchestrator.RestoreModulesFromProjectAsync(project);
            fixture.Session.MarkClean();
        }
        finally
        {
            fixture.CalculationStateService.IsLoadProjectInProgress = false;
        }
    }

    private static ThermalFixture CreateFixture(
        IConstructionService? constructionService = null,
        IProjectFileService? projectFileService = null)
    {
        var context = new CalculationContext();
        var climateData = new ClimateData();
        var session = new ProjectSession(climateData, context);
        var calculationState = new CalculationStateService(session);

        // The Thermal dirty-intent counter observes ONLY the service instance that
        // is injected into ThermalViewModel; every other component shares a second
        // counter so cross-module intents never contaminate Thermal assertions.
        var thermalMarkDirty = new CountingMarkDirtyService(session);
        var otherMarkDirty = new CountingMarkDirtyService(session);

        var materials = Material.GetDefaultMaterials();
        var materialRepository = new Mock<IMaterialRepository>();
        materialRepository.Setup(repository => repository.LoadMaterialsAsync()).ReturnsAsync(materials);
        materialRepository.Setup(repository => repository.GetMaterialById(It.IsAny<int>()))
            .Returns((int id) => materials.FirstOrDefault(material => material.Id == id));
        var templateRepository = new Mock<IConstructionTemplateRepository>();
        templateRepository.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

        var constructionServiceMock = new Mock<IConstructionService>();
        constructionServiceMock.Setup(service => service.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
            .Returns(Task.CompletedTask);
        constructionServiceMock.Setup(service => service.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()))
            .Returns(Task.CompletedTask);
        var effectiveConstructionService = constructionService ?? constructionServiceMock.Object;

        var defaultInitializer = new ConstructionDefaultStateInitializer(materialRepository.Object, session.ConstructionState);
        var constructionViewModel = new ConstructionViewModel(
            effectiveConstructionService,
            materialRepository.Object,
            new Mock<IConstructionRepository>().Object,
            calculationState,
            context,
            new ConstructionValidator(),
            new ConstructionModel(),
            otherMarkDirty,
            templateRepository.Object,
            new Mock<IDialogService>().Object,
            new Mock<IEditorDialogService>().Object,
            session.ConstructionState,
            defaultInitializer);

        var climateService = new Mock<IClimateDataService>();
        climateService.Setup(service => service.LoadClimateDataAsync()).Returns(Task.CompletedTask);
        climateService.Setup(service => service.GetAllCities()).Returns(Array.Empty<CityInfo>());
        var climateViewModel = new ClimateViewModel(
            climateService.Object,
            climateData,
            new ClimateValidator(),
            session);

        var calcMock = new Mock<IThermalCalculator>();
        calcMock.Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Returns(new ThermalCalculationResult
            {
                PowerTotal = 555.0,
                DeltaT = 15.0,
                MeanTemperature = 47.5,
                SupplyTemperature = 50.0,
                ReturnTemperature = 35.0,
                IsValid = true
            });
        var thermalValidator = new Mock<IValidator<ThermalInputs>>();
        thermalValidator.Setup(validator => validator.Validate(It.IsAny<ThermalInputs>()))
            .Returns(ValidationResult.Success());

        var thermalViewModel = new ThermalViewModel(
            calcMock.Object,
            climateData,
            session.ConstructionState.CurrentProjection,
            calculationState,
            context,
            thermalValidator.Object,
            new ThermalResultValidator(),
            thermalMarkDirty);

        var circuitsCalcMock = new Mock<ICircuitsCalculator>();
        circuitsCalcMock
            .Setup(calculator => calculator.CalculateCollectorSummary(
                It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>()))
            .Returns((List<CircuitRow> circuits, int number, ValveType valveType) => new CollectorSummary
            {
                CollectorNumber = number,
                CircuitCount = circuits.Count
            });
        circuitsCalcMock
            .Setup(calculator => calculator.CalculateCircuitPower(
                It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(1000.0);
        circuitsCalcMock
            .Setup(calculator => calculator.CalculateFlowRate(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(50.0);
        var glycol = new Mock<IGlycolDataService>();
        glycol.Setup(service => service.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });
        var selector = new Mock<ICollectorTypeSelector>();
        selector.Setup(service => service.SelectCollectorType(It.IsAny<CollectorData>()))
            .Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });
        var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationState, context);
        var circuitsViewModel = new CircuitsViewModel(
            circuitsCalcMock.Object,
            glycol.Object,
            calculationState,
            new Mock<ICircuitsValidator>().Object,
            selector.Object,
            context,
             otherMarkDirty,
             hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session);

        var orchestrator = new ProjectLoadOrchestrator(
            climateViewModel,
            constructionViewModel,
            thermalViewModel,
            circuitsViewModel,
            calculationState,
            effectiveConstructionService,
            context,
            session,
            defaultInitializer);

        var projectState = new ProjectStateService(session);
        var dialogService = new Mock<IDialogService>();
        string? shownError = null;
        dialogService
            .Setup(service => service.ShowError(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((message, title) => shownError = message);

        var resultsViewModel = new ResultsViewModel(
            projectState,
            session,
            otherMarkDirty,
            dialogService.Object,
            new Mock<IPdfExportService>().Object,
            new Mock<ICalculationReportExportService>().Object,
            projectFileService ?? new Mock<IProjectFileService>().Object,
            calculationState,
            materialRepository.Object,
            effectiveConstructionService,
            climateViewModel,
            constructionViewModel,
            thermalViewModel,
            circuitsViewModel,
            orchestrator,
            new ResultsPdfDataBuilder(
                new Mock<IConstructionVisualizationImageService>().Object,
                calculationState,
                constructionViewModel,
                circuitsViewModel),
            new HydraulicSummaryBuilder());

        var recorder = new Recorder(session, context, calculationState, resultsViewModel);
        var constructionProjection = (ConstructionStateProjection)session.ConstructionState.CurrentProjection;

        return new ThermalFixture(
            session,
            context,
            climateData,
            constructionProjection,
            calculationState,
            calcMock,
            thermalValidator,
            circuitsCalcMock,
            thermalViewModel,
            circuitsViewModel,
            orchestrator,
            resultsViewModel,
            thermalMarkDirty,
            recorder,
            () => shownError);
    }

    private sealed class CountingMarkDirtyService : IMarkDirtyService
    {
        private readonly ProjectSession _session;

        public CountingMarkDirtyService(ProjectSession session)
        {
            _session = session;
        }

        public int IntentCount { get; private set; }

        public void MarkDirty()
        {
            IntentCount++;
            _session.MarkDirty();
        }
    }

    private sealed class Recorder
    {
        public Recorder(
            ProjectSession session,
            CalculationContext context,
            CalculationStateService calculationState,
            ResultsViewModel resultsViewModel)
        {
            calculationState.StateChanged += (_, args) =>
            {
                if (args.Module == "Thermal")
                {
                    ThermalStates.Add(args);
                }
            };
            context.ContextChanged += (_, args) =>
            {
                ContextEvents.Add(args);
                if (args.PropertyName is nameof(CalculationContext.ThermalInputs)
                    or nameof(CalculationContext.ThermalResult))
                {
                    ContextPublications.Add(args.PropertyName);
                }
            };
            calculationState.PipeSpacingChanged += (_, _) => PipeSpacingChangedCount++;
            session.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ProjectSession.IsDirty))
                {
                    IsDirtyTransitions++;
                }
            };
            resultsViewModel.ProjectChanged += (_, _) => ProjectChangedCount++;
        }

        public List<ModuleStateChangedEventArgs> ThermalStates { get; } = new();
        public List<string> ContextPublications { get; } = new();
        public List<ContextChangedEventArgs> ContextEvents { get; } = new();
        public int PipeSpacingChangedCount { get; private set; }
        public int IsDirtyTransitions { get; private set; }
        public int ProjectChangedCount { get; private set; }

        public void Reset()
        {
            ThermalStates.Clear();
            ContextPublications.Clear();
            ContextEvents.Clear();
            PipeSpacingChangedCount = 0;
            IsDirtyTransitions = 0;
            ProjectChangedCount = 0;
        }

        public CycleSnapshot Snapshot(int calculatorDelta, int hydraulicsDelta) => new()
        {
            ThermalStateCount = ThermalStates.Count,
            ContextPublicationCount = ContextPublications.Count,
            PipeSpacingChangedCount = PipeSpacingChangedCount,
            IsDirtyTransitionCount = IsDirtyTransitions,
            ProjectChangedCount = ProjectChangedCount,
            CalculatorInvocationDelta = calculatorDelta,
            HydraulicsCalculationDelta = hydraulicsDelta
        };

        public sealed record CycleSnapshot
        {
            public int ThermalStateCount { get; init; }
            public int ContextPublicationCount { get; init; }
            public int PipeSpacingChangedCount { get; init; }
            public int IsDirtyTransitionCount { get; init; }
            public int ProjectChangedCount { get; init; }
            public int CalculatorInvocationDelta { get; init; }
            public int HydraulicsCalculationDelta { get; init; }
        }
    }

    private sealed class ThermalFixture
    {
        private readonly Func<string?> _shownErrorGetter;

        public ThermalFixture(
            ProjectSession session,
            CalculationContext context,
            ClimateData climateData,
            ConstructionStateProjection constructionProjection,
            CalculationStateService calculationStateService,
            Mock<IThermalCalculator> calcMock,
            Mock<IValidator<ThermalInputs>> thermalValidator,
            Mock<ICircuitsCalculator> circuitsCalcMock,
            ThermalViewModel thermalViewModel,
            CircuitsViewModel circuitsViewModel,
            ProjectLoadOrchestrator orchestrator,
            ResultsViewModel resultsViewModel,
            CountingMarkDirtyService markDirty,
            Recorder recorder,
            Func<string?> shownErrorGetter)
        {
            Session = session;
            Context = context;
            ClimateData = climateData;
            ConstructionProjection = constructionProjection;
            CalculationStateService = calculationStateService;
            CalcMock = calcMock;
            ThermalValidator = thermalValidator;
            CircuitsCalcMock = circuitsCalcMock;
            ThermalViewModel = thermalViewModel;
            CircuitsViewModel = circuitsViewModel;
            Orchestrator = orchestrator;
            ResultsViewModel = resultsViewModel;
            MarkDirty = markDirty;
            Recorder = recorder;
            _shownErrorGetter = shownErrorGetter;
        }

        public ProjectSession Session { get; }
        public CalculationContext Context { get; }
        public ClimateData ClimateData { get; }
        public ConstructionStateProjection ConstructionProjection { get; }
        public CalculationStateService CalculationStateService { get; }
        public Mock<IThermalCalculator> CalcMock { get; }
        public Mock<IValidator<ThermalInputs>> ThermalValidator { get; }
        public Mock<ICircuitsCalculator> CircuitsCalcMock { get; }
        public ThermalViewModel ThermalViewModel { get; }
        public CircuitsViewModel CircuitsViewModel { get; }
        public ProjectLoadOrchestrator Orchestrator { get; }
        public ResultsViewModel ResultsViewModel { get; }
        public CountingMarkDirtyService MarkDirty { get; }
        public Recorder Recorder { get; }
        public string? ShownError => _shownErrorGetter();
        public int DirtyIntentCount => MarkDirty.IntentCount;

        public void LoadResult(double powerTotal)
        {
            ThermalViewModel.LoadResult(new ThermalCalculationResult
            {
                PowerTotal = powerTotal,
                SupplyTemperature = 50.0,
                ReturnTemperature = 35.0,
                MeanTemperature = 42.5,
                DeltaT = 15.0,
                IsValid = true
            });
        }
    }

    #endregion
}
