// ================================================================================
// Phase 4 Todos 5+6+7 (AMZ-1) - ThermalStateCoordinator suite.
// ================================================================================
//
// Proves the canonical application boundary (DEC-T04A): mutation matrix with
// dirty-intent counts, DEC-T05 calculation order/failure/reentrancy, upstream
// mapping with and without a canonical result, lifecycle silence of Reset(),
// the AMZ-1 ApplyNeedsRecalculation bridge semantics and disposal hygiene.
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Project;

[TestFixture]
public sealed class ThermalStateCoordinatorTests
{
    private const string SupplyMessage = "Температура подачи изменена. Требуется пересчёт.";
    private const string ClimateMessage = "Климатические данные изменены. Требуется пересчёт.";
    private const string ConstructionMessage = "Данные конструкции изменены. Требуется пересчёт.";

    private CalculationContext _context = null!;
    private ClimateData _climateData = null!;
    private ConstructionData _constructionData = null!;
    private ProjectSession _session = null!;
    private Mock<IThermalCalculator> _calculator = null!;
    private CountingMarkDirty _markDirty = null!;
    private ThermalStateCoordinator _coordinator = null!;
    private int _completions;

    [SetUp]
    public void SetUp()
    {
        _context = new CalculationContext();
        _climateData = new ClimateData();
        _constructionData = new ConstructionData();
        _session = new ProjectSession(_climateData, _context);
        _calculator = new Mock<IThermalCalculator>();
        _calculator
            .Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Returns(new ThermalCalculationResult
            {
                PowerTotal = 500.0,
                DeltaT = 15.0,
                MeanTemperature = 47.5,
                SupplyTemperature = 50.0,
                ReturnTemperature = 35.0,
                IsValid = true
            });
        _markDirty = new CountingMarkDirty(_session);
        _coordinator = CreateCoordinator();
        _coordinator.Completion += (_, _) => _completions++;
        _completions = 0;
    }

    private ThermalStateCoordinator CreateCoordinator()
    {
        return new ThermalStateCoordinator(
            _session.ThermalState,
            _context,
            _markDirty,
            _calculator.Object,
            _climateData,
            _constructionData,
            new PassThroughInputValidator(),
            new ThermalResultValidator());
    }

    #region Mutation matrix

    [Test]
    public void ChangedUserEdit_OneCanonicalMutationOneDirtyIntent()
    {
        var mutation = _coordinator.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(55.0));

        Assert.Multiple(() =>
        {
            Assert.That(mutation.IsChanged, Is.True);
            Assert.That(mutation.After.Inputs.SupplyTemperature, Is.EqualTo(55.0));
            Assert.That(mutation.After.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual),
                "No result yet: no recalculation status is synthesized.");
            Assert.That(_markDirty.IntentCount, Is.EqualTo(1));
            Assert.That(_completions, Is.EqualTo(1));
            Assert.That(_session.IsDirty, Is.True);
        });
    }

    [Test]
    public void ChangedUserEditWithResult_PreservesResultAndSetsExactMessage()
    {
        LoadResult(321.0);
        _session.MarkClean();
        _markDirty.Reset();
        _completions = 0;

        var mutation = _coordinator.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(55.0));

        Assert.Multiple(() =>
        {
            Assert.That(mutation.IsChanged, Is.True);
            Assert.That(mutation.After.Result!.PowerTotal, Is.EqualTo(321.0),
                "Own input edit preserves the last derived result.");
            Assert.That(mutation.After.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(mutation.After.Status.RecalculationMessage, Is.EqualTo(SupplyMessage));
            Assert.That(_markDirty.IntentCount, Is.EqualTo(1));
            Assert.That(_completions, Is.EqualTo(1));
        });
    }

    [Test]
    public void NoOpUserEdit_ZeroMutationsZeroDirty()
    {
        _coordinator.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(55.0));
        var before = _session.ThermalState.Snapshot;
        _markDirty.Reset();
        _completions = 0;

        var mutation = _coordinator.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(55.0));

        Assert.Multiple(() =>
        {
            Assert.That(mutation.IsNoChange, Is.True);
            Assert.That(_session.ThermalState.Snapshot, Is.EqualTo(before));
            Assert.That(_markDirty.IntentCount, Is.Zero);
            Assert.That(_completions, Is.Zero);
        });
    }

    [Test]
    public void RejectedUserEdit_ZeroEffectsAtomic()
    {
        var before = _session.ThermalState.Snapshot;

        // PipeSpacing вне допустимого диапазона отклоняется валидацией состояния.
        var mutation = _coordinator.ApplyInputEdit(ThermalInputEdit.ForPipeSpacing(int.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That(mutation.IsRejected, Is.True);
            Assert.That(mutation.Errors, Is.Not.Empty);
            Assert.That(_session.ThermalState.Snapshot, Is.EqualTo(before));
            Assert.That(_markDirty.IntentCount, Is.Zero);
            Assert.That(_completions, Is.Zero);
        });
    }

    [Test]
    public void ApplyNeedsRecalculationBridge_PreservesResultIdempotentByValue()
    {
        LoadResult(777.0);

        var first = _session.ThermalState.ApplyNeedsRecalculation("мост AMZ-1", ThermalMutationOrigin.User);
        var second = _session.ThermalState.ApplyNeedsRecalculation("мост AMZ-1", ThermalMutationOrigin.User);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsChanged, Is.True);
            Assert.That(first.After.Result!.PowerTotal, Is.EqualTo(777.0));
            Assert.That(first.After.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(first.After.Status.RecalculationMessage, Is.EqualTo("мост AMZ-1"));
            Assert.That(second.IsNoChange, Is.True,
                "Bridge mutation is idempotent by value: same message emits zero completions.");
        });
    }

    #endregion

    #region DEC-T05 calculation orchestration

    [Test]
    public async Task CalculateAsync_ValidResult_ExactOrderCountsAndNoDirty()
    {
        LoadResult(111.0);
        _session.MarkClean();
        var contextEvents = 0;
        _context.ContextChanged += (_, _) => contextEvents++;

        var outcome = await _coordinator.CalculateAsync(BuildInputs());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Result, Is.Not.Null);
            Assert.That(outcome.ValidationMessage, Is.Empty);
            Assert.That(contextEvents, Is.EqualTo(2),
                "Exactly one ThermalInputs publication then one ThermalResult publication.");
            Assert.That(_calculator.Invocations.Count(i => i.Method.Name == nameof(IThermalCalculator.Calculate)),
                Is.EqualTo(1));
            Assert.That(_session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_session.ThermalState.Snapshot.Result!.PowerTotal, Is.EqualTo(500.0));
            Assert.That(_markDirty.IntentCount, Is.Zero, "Calculation creates no dirty intent.");
            Assert.That(_session.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task CalculateAsync_InvalidResultStoredCanonicallyPublishedOnce()
    {
        _calculator
            .Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Returns(new ThermalCalculationResult
            {
                PowerTotal = 10.0,
                DeltaT = 15.0,
                MeanTemperature = 47.5,
                IsValid = false,
                ValidationErrors = new[] { "Мощность недостаточна" }
            });

        var outcome = await _coordinator.CalculateAsync(BuildInputs());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Result!.IsValid, Is.False);
            Assert.That(outcome.ValidationMessage, Is.EqualTo("Мощность недостаточна"));
            Assert.That(_session.ThermalState.Snapshot.Result!.IsValid, Is.False,
                "Calculator-returned invalid result is stored canonically.");
            Assert.That(_session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
        });
    }

    [Test]
    public async Task CalculateAsync_Exception_NullResultExactMessageCompatibilityPublication()
    {
        _calculator
            .Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Throws(new InvalidOperationException("инъектированный сбой калькулятора"));

        var outcome = await _coordinator.CalculateAsync(BuildInputs());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Result, Is.Null);
            Assert.That(outcome.ValidationMessage, Is.EqualTo("Ошибка расчёта: инъектированный сбой калькулятора"));
            Assert.That(_session.ThermalState.Snapshot.Result, Is.Null);
            Assert.That(_session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_context.ThermalResult, Is.Not.Null,
                "Exception path still publishes the compatibility invalid result once.");
            Assert.That(_context.ThermalResult!.IsValid, Is.False);
            Assert.That(_markDirty.IntentCount, Is.Zero);
        });
    }

    [Test]
    public async Task CalculateAsync_ReentrantWhileCalculating_NoSecondCalculatorHit()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _calculator
            .Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Callback(() =>
            {
                entered.TrySetResult();
                release.Task.Wait();
            })
            .Returns(new ThermalCalculationResult { PowerTotal = 42.0, IsValid = true });

        var firstRun = _coordinator.CalculateAsync(BuildInputs());
        await entered.Task;
        Assert.That(_coordinator.IsCalculating, Is.True);

        var secondRun = await _coordinator.CalculateAsync(BuildInputs());

        Assert.Multiple(() =>
        {
            Assert.That(secondRun.Result, Is.Null, "Reentrant call performs no work.");
            Assert.That(_calculator.Invocations.Count(i => i.Method.Name == nameof(IThermalCalculator.Calculate)),
                Is.EqualTo(1));
        });

        release.TrySetResult();
        var firstOutcome = await firstRun;

        Assert.Multiple(() =>
        {
            Assert.That(firstOutcome.Result!.PowerTotal, Is.EqualTo(42.0));
            Assert.That(_calculator.Invocations.Count(i => i.Method.Name == nameof(IThermalCalculator.Calculate)),
                Is.EqualTo(1), "Still exactly one calculator hit after the gated call finished.");
            Assert.That(_coordinator.IsCalculating, Is.False);
        });
    }

    #endregion

    #region Upstream mapping

    [Test]
    public void ClimateUpstream_WithResult_InvalidatesOnceWithoutDirty()
    {
        LoadResult(888.0);
        _session.MarkClean();
        _completions = 0;

        _climateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

        var snapshot = _session.ThermalState.Snapshot;
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Result, Is.Null);
            Assert.That(snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.NeedsRecalculation));
            Assert.That(snapshot.Status.RecalculationMessage, Is.EqualTo(ClimateMessage));
            Assert.That(_markDirty.IntentCount, Is.Zero,
                "Upstream invalidation never marks dirty again.");
            Assert.That(_completions, Is.EqualTo(1));
        });
    }

    [Test]
    public void ClimateUpstream_WithoutResult_Silent()
    {
        _climateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(_session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_completions, Is.Zero);
            Assert.That(_markDirty.IntentCount, Is.Zero);
        });
    }

    [Test]
    public void ConstructionUpstream_WithResult_InvalidatesOnce()
    {
        LoadResult(999.0);
        _completions = 0;

        _constructionData.RaiseDataChanged("R1Total", 0.05, 0.06, true);

        var snapshot = _session.ThermalState.Snapshot;
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Result, Is.Null);
            Assert.That(snapshot.Status.RecalculationMessage, Is.EqualTo(ConstructionMessage));
            Assert.That(_completions, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConstructionUpstream_SecondChangeAfterClearing_Silent()
    {
        LoadResult(999.0);
        _constructionData.RaiseDataChanged("R1Total", 0.05, 0.06, true);
        _completions = 0;

        _constructionData.RaiseDataChanged("R1Total", 0.06, 0.07, true);

        Assert.That(_completions, Is.Zero, "No result left: repeated upstream change has zero effect.");
    }

    #endregion

    #region Lifecycle and disposal

    [Test]
    public void Reset_IsCanonicalSilentAdapterSeam()
    {
        LoadResult(555.0);
        _coordinator.ApplyInputEdit(ThermalInputEdit.ForGroundTemperature(5.0));
        var before = _session.ThermalState.Snapshot;
        _markDirty.Reset();
        _completions = 0;

        _coordinator.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(_session.ThermalState.Snapshot, Is.EqualTo(before),
                "ST-013/ST-015: adapter reset does not touch canonical values.");
            Assert.That(_markDirty.IntentCount, Is.Zero);
            Assert.That(_completions, Is.Zero);
        });
    }

    [Test]
    public void Dispose_UnsubscribesUpstreamExactlyOnce()
    {
        _coordinator.Dispose();
        _coordinator.Dispose();

        LoadResultViaCoordinatorDisposedCase();

        _climateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

        Assert.That(_session.ThermalState.Snapshot.Result, Is.Not.Null,
            "After disposal upstream changes no longer reach the coordinator.");
    }

    [Test]
    public void LoadResult_RestoresCanonicallyAndPublishesContextInFrozenOrder()
    {
        var publications = new List<string>();
        _context.ContextChanged += (_, args) => publications.Add(args.PropertyName);

        _coordinator.LoadResult(
            new ThermalCalculationResult { PowerTotal = 123.0, IsValid = true },
            BuildInputs());

        Assert.Multiple(() =>
        {
            Assert.That(publications, Is.EqualTo(new[]
            {
                nameof(CalculationContext.ThermalInputs), nameof(CalculationContext.ThermalResult)
            }));
            Assert.That(_session.ThermalState.Snapshot.Result!.PowerTotal, Is.EqualTo(123.0));
            Assert.That(_session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            Assert.That(_markDirty.IntentCount, Is.Zero, "Restore path creates no dirty intent.");
        });
    }

    private void LoadResultViaCoordinatorDisposedCase()
    {
        // Канонический Restore напрямую: координатор уже освобождён, но состояние
        // продолжает работать для других потребителей.
        _session.ThermalState.Restore(
            new ThermalInputsSnapshot(OperatingMode.Melting, 50.0, 10.0, null, 200),
            ThermalResultSnapshot.FromResult(new ThermalCalculationResult { PowerTotal = 1.0, IsValid = true }));
    }

    #endregion

    #region Helpers

    private void LoadResult(double powerTotal)
    {
        _coordinator.LoadResult(
            new ThermalCalculationResult { PowerTotal = powerTotal, IsValid = true },
            BuildInputs());
    }

    private static ThermalInputs BuildInputs()
    {
        return new ThermalInputs
        {
            Mode = OperatingMode.Melting,
            SupplyTemperature = 50.0,
            GroundTemperature = 10.0,
            Pipe = null,
            PipeSpacing = 200,
            LambdaE = 1.6
        };
    }

    private sealed class CountingMarkDirty : IMarkDirtyService
    {
        private readonly ProjectSession _session;

        public CountingMarkDirty(ProjectSession session)
        {
            _session = session;
        }

        public int IntentCount { get; private set; }

        public void MarkDirty()
        {
            IntentCount++;
            _session.MarkDirty();
        }

        public void Reset() => IntentCount = 0;
    }

    private sealed class PassThroughInputValidator : IValidator<ThermalInputs>
    {
        public ValidationResult Validate(ThermalInputs target) => ValidationResult.Success();
    }

    #endregion
}
