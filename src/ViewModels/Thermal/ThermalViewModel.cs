using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.ViewModels.Thermal
{
    /// <summary>
    /// ViewModel для модуля теплового расчёта.
    /// Phase 4 (AMZ-1): WPF-адаптер над канонической границей
    /// <see cref="IThermalStateCoordinator"/>; все пользовательские правки,
    /// расчёт и восстановление идут через координатор. ViewModel не хранит
    /// dirty/context/status политики и не подписан на upstream-события.
    /// </summary>
    public partial class ThermalViewModel : ObservableObject, Services.Project.IProjectLoadThermalAdapter
    {
        private readonly IConstructionData _constructionData;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IValidator<ThermalInputs> _thermalValidator;
        private readonly IThermalStateCoordinator _coordinator;
        private bool _isResetting;

        #region Observable Properties

        /// <summary>
        /// Выбранный режим работы
        /// </summary>
        [ObservableProperty]
        private OperatingMode _selectedMode = OperatingMode.Melting;

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        [ObservableProperty]
        private double _supplyTemperature = 50.0;

        /// <summary>
        /// Температурный перепад, К (только для чтения, рассчитывается автоматически)
        /// </summary>
        public double? DeltaT => Result?.DeltaT;

        /// <summary>
        /// Температура грунта, °C
        /// </summary>
        [ObservableProperty]
        private double _groundTemperature = 10.0;

        /// <summary>
        /// Выбранный тип трубы
        /// </summary>
        [ObservableProperty]
        private PipeType? _selectedPipe;

        /// <summary>
        /// Шаг укладки трубы, мм
        /// </summary>
        [ObservableProperty]
        private int _pipeSpacing = 200;

        /// <summary>
        /// Температура поверхности, °C (только чтение): следует режиму
        /// работы (AntiIcing=3, Melting=5, Intensive=7); проекция для UI.
        /// </summary>
        public double SurfaceTemperature => (double)SelectedMode;

        /// <summary>
        /// Доступные значения шага укладки, мм
        /// </summary>
        public int[] AvailablePipeSpacings { get; } = new[] { 150, 200, 250, 300 };

        /// <summary>
        /// Признак доступности поля Шаг укладки
        /// </summary>
        public bool IsPipeSpacingEnabled => SelectedPipe != null;

        /// <summary>
        /// Результат расчёта
        /// </summary>
        [ObservableProperty]
        private ThermalCalculationResult? _result;

        /// <summary>
        /// Уведомление об изменении результата для связанных свойств
        /// </summary>
        partial void OnResultChanged(ThermalCalculationResult? value)
        {
            OnPropertyChanged(nameof(DeltaT));
            OnPropertyChanged(nameof(RecommendedSupplyTemperature));
            OnPropertyChanged(nameof(SupplyTemperatureHint));
            OnPropertyChanged(nameof(PowerSummary));
            OnPropertyChanged(nameof(AdditionalSummary));
        }

        /// <summary>
        /// Уведомление об изменении выбранной трубы: правка уходит в координатор
        /// (одна каноническая мутация + один dirty-intent при изменении).
        /// </summary>
        partial void OnSelectedPipeChanged(PipeType? value)
        {
            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            OnPropertyChanged(nameof(IsPipeSpacingEnabled));
            _coordinator.ApplyInputEdit(ThermalInputEdit.ForPipe(ThermalPipeSnapshot.FromPipeType(value)));
        }

        /// <summary>
        /// Уведомление об изменении шага укладки трубы: правка уходит в координатор.
        /// </summary>
        partial void OnPipeSpacingChanged(int value)
        {
            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            // Каноническая правка (dirty-intent + завершение) и затем совместимый
            // эхо-вызов legacy-поверхности: в реальной композиции он no-op
            // (значение уже применено канонически), а изолированные композиции с
            // подменой ICalculationStateService продолжают получать
            // PipeSpacingChanged (замороженный интеграционный контракт).
            _coordinator.ApplyInputEdit(ThermalInputEdit.ForPipeSpacing(value));
            _calculationStateService.SetPipeSpacing(value, "ThermalViewModel");
        }

        /// <summary>
        /// Уведомление об изменении температуры подачи: правка уходит в координатор.
        /// </summary>
        partial void OnSupplyTemperatureChanged(double value)
        {
            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            _coordinator.ApplyInputEdit(ThermalInputEdit.ForSupplyTemperature(value));
        }

        /// <summary>
        /// Уведомление об изменении температуры грунта: правка уходит в координатор.
        /// </summary>
        partial void OnGroundTemperatureChanged(double value)
        {
            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            _coordinator.ApplyInputEdit(ThermalInputEdit.ForGroundTemperature(value));
        }

        /// <summary>
        /// Уведомление об изменении режима работы: правка уходит в координатор.
        /// Notify UI-проекций (температура поверхности, строка HeroKPI) —
        /// до guard'ов: загрузка проекта и сброс присваивают режим под ними,
        /// и без этого поле показывало бы значение предыдущего состояния
        /// (ревью Ф5, P1). Проекции мутаций не создают.
        /// </summary>
        partial void OnSelectedModeChanged(OperatingMode value)
        {
            OnPropertyChanged(nameof(SurfaceTemperature));
            OnPropertyChanged(nameof(PowerSummary));

            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            _coordinator.ApplyInputEdit(ThermalInputEdit.ForMode(value));
        }

        /// <summary>
        /// Рекомендуемая температура подачи для ΔT ≈ 15 К
        /// </summary>
        public double? RecommendedSupplyTemperature => Result?.MeanTemperature + 7.5;

        /// <summary>
        /// Подсказка для температуры подачи. Форматирование — по канону
        /// <see cref="AppCulture.Culture"/> (запятая), не по CurrentCulture:
        /// на не-RU ОС интерполяция давала бы точки (Ф7.0, ревью диффа Ф5).
        /// </summary>
        public string SupplyTemperatureHint =>
            RecommendedSupplyTemperature.HasValue
                ? string.Create(AppCulture.Culture,
                    $"Рекомендуется: {RecommendedSupplyTemperature.Value:F0}°C (для ΔT ≈ 15 К)")
                : string.Empty;

        /// <summary>
        /// Детальная строка HeroKPI результатов: потоки вверх/вниз и
        /// температура поверхности текущего режима (Фаза 5, рендер 04).
        /// Числа — по канону <see cref="AppCulture.Culture"/> (Ф7.0).
        /// </summary>
        public string PowerSummary =>
            Result is null
                ? string.Empty
                : string.Create(AppCulture.Culture,
                    $"q↑ {Result.PowerUp:F1} вверх · q↓ {Result.PowerDown:F1} вниз · поверхность {SurfaceTemperature:+0.0} °C");

        /// <summary>
        /// Сводная строка заголовка свёрнутого блока «Дополнительные параметры».
        /// Числа — по канону <see cref="AppCulture.Culture"/> (Ф7.0).
        /// </summary>
        public string AdditionalSummary =>
            Result is null
                ? string.Empty
                : string.Create(AppCulture.Culture,
                    $"КПД ребра {Result.EfficiencyEtaR:F3} · R_FB {Result.RFb:F4} · m {Result.ParameterM:F2} 1/м · теплота плавления {Result.MeltingHeat:F1} Вт/м²");

        /// <summary>
        /// Признак выполнения расчёта
        /// </summary>
        [ObservableProperty]
        private bool _isCalculating;

        /// <summary>
        /// Сообщение валидации
        /// </summary>
        [ObservableProperty]
        private string _validationMessage = string.Empty;

        /// <summary>
        /// Сообщение о необходимости пересчёта
        /// Делегирует сервису ICalculationStateService
        /// </summary>
        public string RecalcMessage => _calculationStateService.ThermalValidationMessage;

        /// <summary>
        /// Признак того, что тепловой расчёт требует пересчёта
        /// </summary>
        public bool NeedsRecalculation => _calculationStateService.ThermalNeedsRecalculation;

        #endregion

        #region Collections

        /// <summary>
        /// Доступные типы труб
        /// </summary>
        public ObservableCollection<PipeType> AvailablePipes { get; }

        /// <summary>
        /// Доступные режимы работы
        /// </summary>
        public ObservableCollection<OperatingMode> AvailableModes { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Создать ViewModel. Координатор внедряется DI как application-singleton;
        /// legacy/тестовая композиция без явного координатора строит его из тех же
        /// зависимостей вокруг сессии переданного сервиса состояния.
        /// </summary>
        public ThermalViewModel(
            IThermalCalculator calculator,
            IClimateData climateData,
            IConstructionData constructionData,
            ICalculationStateService calculationStateService,
            CalculationContext calculationContext,
            IValidator<ThermalInputs> thermalValidator,
            IValidator<ThermalCalculationResult> thermalResultValidator,
            IMarkDirtyService markDirtyService,
            IThermalStateCoordinator? coordinator = null)
        {
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _thermalValidator = thermalValidator ?? throw new ArgumentNullException(nameof(thermalValidator));

            // Инициализация коллекций
            AvailablePipes = new ObservableCollection<PipeType>(PipeType.StandardPipes);
            AvailableModes = new ObservableCollection<OperatingMode>
            {
                OperatingMode.AntiIcing,
                OperatingMode.Melting,
                OperatingMode.Intensive
            };

            // Каноническая граница применения команд (DEC-T04A). В DI-композиции
            // координатор ровно один и внедряется сюда; изолированная композиция
            // строит координатор вокруг reference-identical срезов своей сессии.
            _coordinator = coordinator ?? CreateIsolatedCoordinator(
                calculationStateService,
                calculationContext,
                markDirtyService,
                calculator,
                climateData,
                constructionData,
                thermalValidator,
                thermalResultValidator);

            // Подписка на изменения состояния расчёта (обновление RecalcMessage/
            // NeedsRecalculation) и эхо канонического шага укладки.
            _calculationStateService.StateChanged += OnCalculationStateChanged;
            _calculationStateService.PipeSpacingChanged += OnPipeSpacingServiceChanged;

            // Единственная подписка адаптера на канонические завершения
            // (обновление привязок) и refresh-сигнал upstream-проекций.
            _coordinator.Completion += OnCoordinatorCompletion;
            _coordinator.UpstreamObserved += OnUpstreamObserved;

            // Инициализация команды сброса
            ResetCommand = new RelayCommand(Reset);
        }

        /// <summary>
        /// Команда сброса к дефолтным значениям
        /// </summary>
        public IRelayCommand ResetCommand { get; }

        /// <summary>
        /// Канонический координатор этого адаптера (для проверок идентичности DI).
        /// </summary>
        internal IThermalStateCoordinator Coordinator => _coordinator;

        private static IThermalStateCoordinator CreateIsolatedCoordinator(
            ICalculationStateService calculationStateService,
            CalculationContext calculationContext,
            IMarkDirtyService markDirtyService,
            IThermalCalculator calculator,
            IClimateData climateData,
            IConstructionData constructionData,
            IValidator<ThermalInputs> thermalValidator,
            IValidator<ThermalCalculationResult> thermalResultValidator)
        {
            var session = (calculationStateService as CalculationStateService)?.Session
                ?? new ProjectSession(climateData as ClimateData, calculationContext);
            return new ThermalStateCoordinator(
                session.ThermalState,
                calculationContext,
                markDirtyService,
                calculator,
                climateData,
                constructionData,
                thermalValidator,
                thermalResultValidator);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда выполнения расчёта: предвалидация входов, затем оркестрация
        /// DEC-T05 внутри координатора.
        /// </summary>
        [RelayCommand]
        private async Task Calculate()
        {
            if (IsCalculating || _coordinator.IsCalculating) return;

            // Валидация входных данных: невалидный кандидат не доходит до
            // калькулятора, контекста и фазы (DEC-T05 шаги 1-2).
            var inputValidation = ValidateInput();
            if (!inputValidation.IsValid)
            {
                ValidationMessage = string.Join("; ", inputValidation.Errors.Select(e => e.Message));
                return;
            }

            IsCalculating = true;
            ValidationMessage = string.Empty;

            try
            {
                var outcome = await _coordinator.CalculateAsync(BuildThermalInputs());
                Result = outcome.Result;
                ValidationMessage = outcome.ValidationMessage;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка расчёта: {ex.Message}";
                Result = null;
            }
            finally
            {
                IsCalculating = false;
            }
        }

        /// <summary>
        /// Сбросить ViewModel к дефолтным значениям. Наследуемое наблюдаемое
        /// поведение ST-013/ST-015: только адаптер; каноническое состояние и
        /// события не затрагиваются.
        /// </summary>
        public void Reset()
        {
            _isResetting = true;
            try
            {
                _coordinator.Reset();
                SelectedMode = OperatingMode.Melting;
                SupplyTemperature = 50.0;
                GroundTemperature = 10.0;
                SelectedPipe = null;
                PipeSpacing = 200;
                Result = null;
                ValidationMessage = string.Empty;
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Загрузить готовый результат теплового расчёта (без пересчёта) и опубликовать в контекст.
        /// Используется путём загрузки проекта как canonical writer thermal-данных.
        /// </summary>
        public void LoadResult(ThermalCalculationResult result, ThermalInputs? inputs = null)
        {
            var thermalInputs = inputs ?? BuildThermalInputs();
            _coordinator.LoadResult(result, thermalInputs);
            Result = result;
        }

        /// <summary>
        /// Restore-time fallback-расчёт (IProjectLoadThermalAdapter): ровно один
        /// запуск той же команды расчёта, что и пользовательская кнопка
        /// (Phase 7 exactly-once контракт сохранён).
        /// </summary>
        public Task CalculateFromRestoreAsync() => CalculateCommand.ExecuteAsync(null);

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить параметры теплового расчёта
        /// </summary>
        public ThermalInputs BuildThermalInputs()
        {
            return new ThermalInputs
            {
                Mode = SelectedMode,
                SupplyTemperature = SupplyTemperature,
                GroundTemperature = GroundTemperature,
                Pipe = SelectedPipe!, // Валидация гарантирует, что SelectedPipe не null при вызове
                PipeSpacing = PipeSpacing,
                LambdaE = _constructionData.LambdaE
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Валидация входных данных
        /// </summary>
        /// <returns>Результат валидации</returns>
        private ValidationResult ValidateInput()
        {
            var parameters = BuildThermalInputs();
            return _thermalValidator.Validate(parameters);
        }

        /// <summary>
        /// Обработчик канонического завершения: обновление привязок статуса и
        /// очистка результата при upstream-инвалидации.
        /// </summary>
        private void OnCoordinatorCompletion(object? sender, ThermalStateChangedEventArgs e)
        {
            var mutation = e.Mutation;
            if ((mutation.Origin == ThermalMutationOrigin.ClimateInvalidation
                || mutation.Origin == ThermalMutationOrigin.ConstructionInvalidation)
                && mutation.Before.Result != null
                && mutation.After.Result == null)
            {
                Result = null;
            }

            // ADR-014: откат/возврат теплового состояния восстанавливает
            // адаптерные привязки полным снимком (входы + результат; статус
            // транслируется через CalculationStateService сами собой).
            if (mutation.Origin is ThermalMutationOrigin.Undo or ThermalMutationOrigin.Redo)
            {
                ApplyStateSnapshotToAdapter(mutation.After);
            }

            OnPropertyChanged(nameof(RecalcMessage));
            OnPropertyChanged(nameof(NeedsRecalculation));
        }

        /// <summary>
        /// Зеркалирование полного канонического снимка теплового состояния в
        /// адаптерные привязки (ADR-014, откат/возврат действия). Вызывающий
        /// владеет канонической мутацией; присвоения идут под guards
        /// (<c>_isResetting</c> + guard загрузки) и мутаций не создают.
        /// </summary>
        public void ApplyStateSnapshotToAdapter(ThermalStateSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            _isResetting = true;
            try
            {
                SelectedMode = snapshot.Inputs.Mode;
                SupplyTemperature = snapshot.Inputs.SupplyTemperature;
                GroundTemperature = snapshot.Inputs.GroundTemperature;
                SelectedPipe = ThermalPersistenceMapper.ResolveStandardPipe(
                    snapshot.Inputs.Pipe,
                    AvailablePipes);
                PipeSpacing = snapshot.Inputs.PipeSpacing;
                Result = snapshot.Result is null
                    ? null
                    : ThermalPersistenceMapper.ToDomainResult(snapshot.Result);
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Refresh-сигнал upstream-проекций (подсказки подачи).
        /// R1Total/R2Total ушли из UI в панель «Сводка» каркаса (Фаза 4).
        /// </summary>
        private void OnUpstreamObserved(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(RecommendedSupplyTemperature));
            OnPropertyChanged(nameof(SupplyTemperatureHint));
        }

        /// <summary>
        /// Обработчик изменения состояния расчёта
        /// </summary>
        private void OnCalculationStateChanged(object? sender, ModuleStateChangedEventArgs e)
        {
            // Уведомить UI об изменении свойств RecalcMessage и NeedsRecalculation
            OnPropertyChanged(nameof(RecalcMessage));
            OnPropertyChanged(nameof(NeedsRecalculation));
        }

        /// <summary>
        /// Обработчик изменения канонического шага укладки из ICalculationStateService
        /// </summary>
        private void OnPipeSpacingServiceChanged(object? sender, int spacing)
        {
            PipeSpacing = spacing;
        }

        #endregion
    }
}
