using CommunityToolkit.Mvvm.ComponentModel;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.ViewModels.Shell
{
    /// <summary>
    /// Read-only адаптер панели «Сводка» (Фаза 1 редизайна, план п. 3):
    /// q↑/q↓/q, тепловая мощность, Tпод/Tобр, режим, город;
    /// с Фазы 4 — конструкция: общая толщина, R₁/R₂, λE (переезд из
    /// карточки «Результаты расчёта» ConstructionView).
    /// </summary>
    /// <remarks>
    /// НЕ владеет состоянием и ничего не пишет (инварианты R2/R3/R5):
    /// только чтение слайсов <see cref="IProjectSession"/> и агрегатов
    /// <see cref="ResultsViewModel"/>. Обновление — по подпискам:
    /// PropertyChanged модульных VM/сессии и Changed слайсов Climate/Thermal/
    /// Construction (слайсы не INotifyPropertyChanged — ревью Ф1, F2).
    /// </remarks>
    public partial class SummaryViewModel : ObservableObject
    {
        private readonly IProjectSession _session;
        private readonly ResultsViewModel _resultsViewModel;
        private readonly ICalculationStateService? _calculationStateService;

        public SummaryViewModel(
            IProjectSession session,
            ResultsViewModel resultsViewModel,
            ICalculationStateService? calculationStateService = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _resultsViewModel = resultsViewModel ?? throw new ArgumentNullException(nameof(resultsViewModel));
            _calculationStateService = calculationStateService;

            _session.PropertyChanged += OnSourceChanged;
            _session.ClimateState.Changed += OnSourceChanged;
            _session.ThermalState.Changed += OnSourceChanged;
            _session.ConstructionState.Changed += OnSourceChanged;
            _resultsViewModel.PropertyChanged += OnSourceChanged;
            if (_calculationStateService is not null)
            {
                _calculationStateService.StateChanged += OnSourceChanged;
            }

            Refresh();
        }

        private void OnSourceChanged(object? sender, EventArgs e) => Refresh();

        private void OnSourceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Refresh();

        /// <summary>Перечитывает все значения из источников.</summary>
        private void Refresh()
        {
            var climate = _session.ClimateState.Snapshot;
            City = climate?.SelectedCity ?? string.Empty;

            var thermalResult = _session.ThermalState.Snapshot.Result;
            PowerUp = thermalResult?.PowerUp ?? 0;
            PowerDown = thermalResult?.PowerDown ?? 0;
            PowerTotal = thermalResult?.PowerTotal ?? 0;
            SupplyTemperature = thermalResult?.SupplyTemperature ?? 0;
            ReturnTemperature = thermalResult?.ReturnTemperature ?? 0;

            TotalThermalPowerKw = _resultsViewModel.TotalThermalPower_kW;
            ModeText = _resultsViewModel.CurrentModeText ?? string.Empty;

            RefreshConstruction();
        }

        /// <summary>
        /// Конструктивные агрегаты из снапшота; формулы 1:1 с
        /// ConstructionViewModel (R = d/λ/1000, λE — λ последнего слоя над
        /// трубой, дефолт 1.6).
        /// </summary>
        private void RefreshConstruction()
        {
            var construction = _session.ConstructionState.Snapshot;

            var rAbove = 0.0;
            foreach (var layer in construction.LayersAbovePipe)
            {
                rAbove += layer.CalculatedLambda > 0 ? layer.Thickness / layer.CalculatedLambda / 1000.0 : 0;
            }

            var rBelow = 0.0;
            foreach (var layer in construction.LayersBelowPipe)
            {
                rBelow += layer.CalculatedLambda > 0 ? layer.Thickness / layer.CalculatedLambda / 1000.0 : 0;
            }

            ConstructionR1 = rAbove;
            ConstructionR2 = rBelow;
            ConstructionTotalThickness = construction.LayersAbovePipe.Sum(l => l.Thickness)
                                         + construction.LayersBelowPipe.Sum(l => l.Thickness);
            ConstructionLambdaE = construction.LayersAbovePipe.LastOrDefault()?.CalculatedLambda ?? 1.6;
        }

        private string _city = string.Empty;
        /// <summary>Город проекта (климатический слайс).</summary>
        public string City
        {
            get => _city;
            private set => SetProperty(ref _city, value);
        }

        private double _powerUp;
        /// <summary>Поток вверх q↑, Вт/м².</summary>
        public double PowerUp
        {
            get => _powerUp;
            private set => SetProperty(ref _powerUp, value);
        }

        private double _powerDown;
        /// <summary>Поток вниз q↓, Вт/м².</summary>
        public double PowerDown
        {
            get => _powerDown;
            private set => SetProperty(ref _powerDown, value);
        }

        private double _powerTotal;
        /// <summary>Суммарный поток q, Вт/м².</summary>
        public double PowerTotal
        {
            get => _powerTotal;
            private set => SetProperty(ref _powerTotal, value);
        }

        private double _supplyTemperature;
        /// <summary>Температура подачи, °C.</summary>
        public double SupplyTemperature
        {
            get => _supplyTemperature;
            private set => SetProperty(ref _supplyTemperature, value);
        }

        private double _returnTemperature;
        /// <summary>Температура обратки, °C.</summary>
        public double ReturnTemperature
        {
            get => _returnTemperature;
            private set => SetProperty(ref _returnTemperature, value);
        }

        private double _totalThermalPowerKw;
        /// <summary>Суммарная тепловая мощность, кВт (агрегат Results).</summary>
        public double TotalThermalPowerKw
        {
            get => _totalThermalPowerKw;
            private set => SetProperty(ref _totalThermalPowerKw, value);
        }

        private string _modeText = string.Empty;
        /// <summary>Режим работы («Рабочий»/«Расчётный»).</summary>
        public string ModeText
        {
            get => _modeText;
            private set => SetProperty(ref _modeText, value);
        }

        private double _constructionTotalThickness;
        /// <summary>Общая толщина пирога (над + под трубой), мм.</summary>
        public double ConstructionTotalThickness
        {
            get => _constructionTotalThickness;
            private set => SetProperty(ref _constructionTotalThickness, value);
        }

        private double _constructionR1;
        /// <summary>Суммарное R слоёв над трубой, м²·К/Вт.</summary>
        public double ConstructionR1
        {
            get => _constructionR1;
            private set => SetProperty(ref _constructionR1, value);
        }

        private double _constructionR2;
        /// <summary>Суммарное R слоёв под трубой, м²·К/Вт.</summary>
        public double ConstructionR2
        {
            get => _constructionR2;
            private set => SetProperty(ref _constructionR2, value);
        }

        private double _constructionLambdaE = 1.6;
        /// <summary>Теплопроводность вокруг трубы λE, Вт/м·К.</summary>
        public double ConstructionLambdaE
        {
            get => _constructionLambdaE;
            private set => SetProperty(ref _constructionLambdaE, value);
        }
    }
}
