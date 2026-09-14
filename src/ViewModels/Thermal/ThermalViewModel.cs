using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
using SnowMeltingCalculator.Core.Constants;

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
        private readonly IClimateData _climateData;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IValidator<ThermalInputs> _thermalValidator;
        private readonly IThermalStateCoordinator _coordinator;
        private readonly IThermalAdviceService _adviceService;
        private bool _isResetting;

        /// <summary>
        /// Guard синхронизации поля ввода t_пов с каноническим режимом:
        /// присваивания из <see cref="SyncSurfaceEntryFromMode"/> мутаций
        /// не создают (план 2026-09-13, Ф2).
        /// </summary>
        private bool _isSyncingSurfaceEntry;

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
        /// Температура поверхности, °C (проекция): числовое значение режима
        /// работы (AntiIcing=3, Melting=5, Intensive=7, Manual1/2/4/6=1/2/4/6).
        /// Единственный владелец величины — поле Mode канонического состояния.
        /// </summary>
        public double SurfaceTemperature => (double)SelectedMode;

        /// <summary>
        /// Поле ручного ввода температуры поверхности (целое от +1 до +7).
        /// Мутация — только через <see cref="ThermalInputEdit.ForMode"/>:
        /// валидный ввод присваивает <see cref="SelectedMode"/>, невалидный
        /// поднимает <see cref="SurfaceTemperatureError"/> и состояние не трогает.
        /// </summary>
        [ObservableProperty]
        private string _surfaceTemperatureEntry = "+5";

        /// <summary>
        /// Подсказка об ошибке ввода t_пов («Введите целое число от +1 до +7»);
        /// пустая строка — ошибок нет.
        /// </summary>
        [ObservableProperty]
        private string _surfaceTemperatureError = string.Empty;

        /// <summary>
        /// Подпись происхождения значения t_пов: «из режима „<имя>“» для
        /// пресетов +3/+5/+7, «своё значение» для ручного ввода.
        /// </summary>
        public string SurfaceTemperatureCaption => (int)SelectedMode switch
        {
            3 => "из режима «Антиобледенение»",
            5 => "из режима «Таяние»",
            7 => "из режима «Интенсивное»",
            _ => "своё значение"
        };

        /// <summary>
        /// Необлокирующее предупреждение (решение V2 плана 2026-09-13):
        /// t_П не выше температуры наружного воздуха — расчётная мощность
        /// будет отрицательной. Форматирование — по канону
        /// <see cref="AppCulture.Culture"/> (Ф7.0).
        /// </summary>
        public string SurfaceTemperatureHint =>
            Result is not null
                && (double)SelectedMode <= _climateData.AirTemperature
                ? string.Create(AppCulture.Culture,
                    $"Температура поверхности не выше температуры воздуха ({_climateData.AirTemperature:+0.0;−0.0} °C) — расчётная мощность будет отрицательной. Расчёт не блокируется.")
                : string.Empty;

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
            OnPropertyChanged(nameof(SurfaceTemperatureHint));
            OnPropertyChanged(nameof(PowerSummary));
            OnPropertyChanged(nameof(AdditionalSummary));
            OnPropertyChanged(nameof(AdviceList));
            OnPropertyChanged(nameof(IsReturnTemperatureNegative));
            OnPropertyChanged(nameof(AdviceSupplyHint));
            OnPropertyChanged(nameof(AdviceSpacingHint));
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
        /// Notify UI-проекций (температура поверхности, подпись происхождения,
        /// предупреждение, строка HeroKPI) — до guard'ов: загрузка проекта и
        /// сброс присваивают режим под ними, и без этого поле показывало бы
        /// значение предыдущего состояния (ревью Ф5, P1). Проекции мутаций
        /// не создают.
        /// </summary>
        partial void OnSelectedModeChanged(OperatingMode value)
        {
            OnPropertyChanged(nameof(SurfaceTemperature));
            OnPropertyChanged(nameof(SurfaceTemperatureCaption));
            OnPropertyChanged(nameof(SurfaceTemperatureHint));
            OnPropertyChanged(nameof(PowerSummary));
            SyncSurfaceEntryFromMode(value);

            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            _coordinator.ApplyInputEdit(ThermalInputEdit.ForMode(value));
        }

        /// <summary>
        /// Правка поля ввода t_пов: валидное целое 1..7 присваивает
        /// <see cref="SelectedMode"/> (мутация идёт единственным каноном —
        /// <see cref="OnSelectedModeChanged"/> → <c>ForMode</c>); невалидный
        /// ввод поднимает подсказку и состояние не трогает. Эхо-присваивания
        /// из синхронизации пропускаются guard'ом.
        /// </summary>
        partial void OnSurfaceTemperatureEntryChanged(string value)
        {
            if (_isSyncingSurfaceEntry) return;
            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            if (!TryParseSurfaceTemperature(value, out int temperature))
            {
                SurfaceTemperatureError = "Введите целое число от +1 до +7";
                return;
            }

            SurfaceTemperatureError = string.Empty;
            if ((int)SelectedMode == temperature) return;

            SelectedMode = (OperatingMode)temperature;
        }

        /// <summary>
        /// Синхронизация поля ввода с каноническим режимом (вызывается из
        /// <see cref="OnSelectedModeChanged"/>, а также форсированно из
        /// <see cref="Reset"/> и <see cref="ApplyStateSnapshotToAdapter"/> —
        /// присваивание равного значения не поднимает PropertyChanged, без
        /// форса мусорный текст/ошибка пережили бы сброс и Undo/Redo).
        /// Поле всегда показывает актуальное t_пов, ошибка ввода гасится.
        /// </summary>
        private void SyncSurfaceEntryFromMode(OperatingMode value)
        {
            _isSyncingSurfaceEntry = true;
            try
            {
                SurfaceTemperatureEntry = $"+{(int)value}";
            }
            finally
            {
                _isSyncingSurfaceEntry = false;
            }

            SurfaceTemperatureError = string.Empty;
        }

        /// <summary>
        /// Разбор ввода t_пов: целое число в диапазоне
        /// [<see cref="ValidationConstants.MinSurfaceTemperature"/>,
        /// <see cref="ValidationConstants.MaxSurfaceTemperature"/>].
        /// Ведущий «+» (ровно один) и пробелы допускаются; второй знак,
        /// полуцелые и вне диапазона — отказ.
        /// </summary>
        private static bool TryParseSurfaceTemperature(string? text, out int temperature)
        {
            temperature = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var trimmed = text.Trim();
            var normalized = trimmed.StartsWith('+') ? trimmed[1..] : trimmed;

            // Без AllowLeadingSign: знак «+» учтён выше явно, «++5»/«-1» — мусор
            const NumberStyles unsigned = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
            if (!int.TryParse(normalized, unsigned, AppCulture.Culture, out temperature))
            {
                return false;
            }

            return temperature >= ValidationConstants.MinSurfaceTemperature
                && temperature <= ValidationConstants.MaxSurfaceTemperature;
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
            AdviceList.Count > 0
                ? string.Empty
                : RecommendedSupplyTemperature.HasValue
                    ? string.Create(AppCulture.Culture,
                        $"Рекомендуется: {RecommendedSupplyTemperature.Value:F0}°C (для ΔT ≈ 15 К)")
                    : string.Empty;

        /// <summary>
        /// Советы текущего результата — derived-слой поверх валидации
        /// (план thermal-advice §3): ничего не гейтят, канонического состояния
        /// не создают. Пустой список при отсутствии нарушений.
        /// </summary>
        public IReadOnlyList<ThermalAdvice> AdviceList =>
            Result is null ? Array.Empty<ThermalAdvice>() : _adviceService.Build(Result);

        /// <summary>
        /// Хинт совета под полем подачи — текст первого совета (решение №1);
        /// решение №8 (вариант A): постоянный «Рекомендуется: …» при активных
        /// советах скрыт — SupplyTemperatureHint возвращает пустую строку.
        /// </summary>
        public string? AdviceSupplyHint => AdviceList.FirstOrDefault()?.Message;

        /// <summary>Хинт совета под полем шага укладки</summary>
        public string? AdviceSpacingHint =>
            AdviceList.Count > 0
                ? "Увеличьте шаг укладки — это снизит перепад и поднимет обратку"
                : null;

        /// <summary>
        /// Акцент чипа «Температура обратки» (решение владельца 2026-09-15):
        /// тёплая охра при отрицательной обратке.
        /// </summary>
        public bool IsReturnTemperatureNegative => (Result?.ReturnTemperature ?? 0) < 0;

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
            IThermalStateCoordinator? coordinator = null,
            IThermalAdviceService? adviceService = null)
        {
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _thermalValidator = thermalValidator ?? throw new ArgumentNullException(nameof(thermalValidator));
            // Опциональный параметр — прецедент coordinator = null: ~30 тестовых
            // мест конструирования не ломаются (план thermal-advice §3, ревью P2-7)
            _adviceService = adviceService ?? new ThermalAdviceService();

            // Инициализация коллекций
            AvailablePipes = new ObservableCollection<PipeType>(PipeType.StandardPipes);
            // Порядок V5 (план 2026-09-13): три семантических пресета сверху,
            // затем ручные значения по возрастанию температуры
            AvailableModes = new ObservableCollection<OperatingMode>
            {
                OperatingMode.AntiIcing,
                OperatingMode.Melting,
                OperatingMode.Intensive,
                OperatingMode.Manual1,
                OperatingMode.Manual2,
                OperatingMode.Manual4,
                OperatingMode.Manual6
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

                // Ревью P2-1: присваивание равного SelectedMode не поднимает
                // PropertyChanged — форс-синхронизация гасит «застрявшие»
                // мусорный текст и ошибку ввода t_пов (также покрывает
                // ProjectLoadOrchestrator, вызывающий Reset перед загрузкой).
                SyncSurfaceEntryFromMode(SelectedMode);
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

                // Ревью P2-1: форс-синхронизация поля t_пов при откате/возврате
                SyncSurfaceEntryFromMode(SelectedMode);
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Refresh-сигнал upstream-проекций (подсказки подачи, предупреждение
        /// t_пов ≤ t_нар).
        /// R1Total/R2Total ушли из UI в панель «Сводка» каркаса (Фаза 4).
        /// </summary>
        private void OnUpstreamObserved(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(RecommendedSupplyTemperature));
            OnPropertyChanged(nameof(SupplyTemperatureHint));
            OnPropertyChanged(nameof(SurfaceTemperatureHint));
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
