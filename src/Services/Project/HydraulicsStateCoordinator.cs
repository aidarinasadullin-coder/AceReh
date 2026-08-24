using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Services.Navigation;

namespace SnowMeltingCalculator.Services.Project
{
    public sealed class HydraulicsStateCoordinator : IHydraulicsStateCoordinator
    {
        private readonly IProjectSessionHydraulicsState _state;
        private readonly ICalculationStateService _calculationStateService;
        private readonly CalculationContext _calculationContext;
        private Func<List<CollectorSummary>?>? _calculateSelected;
        private Func<List<CollectorSummary>?>? _calculateAll;
        private Func<IReadOnlyList<HydraulicCollectorSnapshot>>? _captureCollectors;
        private Action? _notifyThermal;
        private Action? _notifyClimate;
        private Action<double>? _mirrorPipeSpacing;

        public HydraulicsStateCoordinator(
            IProjectSessionHydraulicsState state,
            ICalculationStateService calculationStateService,
            CalculationContext calculationContext)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _calculationContext.ContextChanged += OnContextChanged;
            _calculationStateService.PipeSpacingChanged += OnPipeSpacingChanged;
            _calculationStateService.StateChanged += OnStateChanged;
        }

        public void Connect(Func<List<CollectorSummary>?> calculateSelected, Func<List<CollectorSummary>?> calculateAll, Func<IReadOnlyList<HydraulicCollectorSnapshot>> captureCollectors, Action notifyThermal, Action notifyClimate, Action<double> mirrorPipeSpacing)
        {
            _calculateSelected = calculateSelected ?? throw new ArgumentNullException(nameof(calculateSelected));
            _calculateAll = calculateAll ?? throw new ArgumentNullException(nameof(calculateAll));
            _captureCollectors = captureCollectors ?? throw new ArgumentNullException(nameof(captureCollectors));
            _notifyThermal = notifyThermal ?? throw new ArgumentNullException(nameof(notifyThermal));
            _notifyClimate = notifyClimate ?? throw new ArgumentNullException(nameof(notifyClimate));
            _mirrorPipeSpacing = mirrorPipeSpacing ?? throw new ArgumentNullException(nameof(mirrorPipeSpacing));
        }

        public void Calculate(Func<List<CollectorSummary>?> calculation) => RunCalculation(calculation);

        public void CalculateAll(Func<List<CollectorSummary>?> calculation) => RunCalculation(calculation);

        public void ApplyPipeSpacing(int spacing, Action<double> mirror)
        {
            RunCalculation(_calculateAll!);
            mirror(spacing / 10.0);
        }

        public void PublishHydraulics(List<CollectorSummary>? summaries) =>
            _calculationContext.UpdateHydraulics(summaries, "CircuitsViewModel");

        private void RunCalculation(Func<List<CollectorSummary>?> calculation, Action? beforeComplete = null)
        {
            _calculationStateService.SetHydraulicsCalculating();
            try
            {
                var summaries = calculation();
                if (summaries == null || !string.IsNullOrEmpty(_calculationStateService.HydraulicsValidationMessage))
                {
                    if (summaries is null)
                    {
                        PublishHydraulics(null);
                    }

                    return;
                }

                PublishHydraulics(summaries);
                beforeComplete?.Invoke();
                _state.CompleteCalculation(_captureCollectors!(),
                    new Dictionary<int, HydraulicCollectorSummarySnapshot>());
            }
            finally
            {
                _calculationStateService.ResetHydraulicsState();
            }
        }

        private void OnContextChanged(object? sender, ContextChangedEventArgs e)
        {
            if (e.Source == "CircuitsViewModel") return;
            switch (e.PropertyName)
            {
                case nameof(CalculationContext.ThermalInputs):
                    _notifyThermal?.Invoke();
                    break;
                case nameof(CalculationContext.ThermalResult):
                    _notifyThermal?.Invoke();
                    if (_calculationContext.ThermalResult?.IsValid == true) _calculateAll?.Invoke();
                    break;
                case nameof(CalculationContext.Climate):
                    _notifyClimate?.Invoke();
                    break;
            }
        }

        private void OnPipeSpacingChanged(object? sender, int spacing)
        {
            ApplyPipeSpacing(spacing, _mirrorPipeSpacing ?? (_ => { }));
        }

        private static void OnStateChanged(object? sender, ModuleStateChangedEventArgs e)
        {
        }
    }
}
