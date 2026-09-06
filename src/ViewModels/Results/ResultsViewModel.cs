using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.ViewModels.Results
{
    /// <summary>
    /// ViewModel для экрана результатов расчёта
    /// </summary>
    public partial class ResultsViewModel : ObservableObject
    {
        private readonly IProjectSession _projectSession;
        private readonly IDialogService _dialogService;
        private readonly IPdfExportService _pdfExportService;
        private readonly ICalculationReportExportService _calculationReportExportService;
        private readonly IProjectFileService _projectFileService;
        private readonly IProjectSaveService? _projectSaveService;
        private readonly IProjectDisplayModeState? _displayModeState;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionService _constructionService;
        private readonly ProjectLoadOrchestrator _projectLoadOrchestrator;
        private readonly ResultsPdfDataBuilder _resultsPdfDataBuilder;
        private readonly HydraulicSummaryBuilder _hydraulicSummaryBuilder;
        private readonly ResultsKpiPresenter _resultsKpiPresenter;
        private DateTime _createdDate;

        private bool _isResetting;

        #region Observable Properties

        // ============================================
        // Блок 0 - Информация о проекте
        // ============================================

        /// <summary>
        /// Номер проекта
        /// </summary>
        /// <remarks>
        /// Источник истины — <see cref="IProjectSession"/>; свойство VM — pass-through
        /// с уведомлением UI и пометкой dirty при изменении вне сброса/загрузки проекта.
        /// </remarks>
        public string ProjectNumber
        {
            get => _projectSession.ProjectNumber;
            set
            {
                if (_projectSession.ProjectNumber == value) return;
                _projectSession.ProjectNumber = value;
                OnPropertyChanged();
                if (_isResetting || _projectSession.IsLoadProjectInProgress) return;
                _projectSession.MarkDirty();
            }
        }

        /// <summary>
        /// Наименование объекта
        /// </summary>
        /// <remarks>
        /// Источник истины — <see cref="IProjectSession"/>; свойство VM — pass-through
        /// с уведомлением UI и пометкой dirty при изменении вне сброса/загрузки проекта.
        /// </remarks>
        public string ProjectObject
        {
            get => _projectSession.ProjectObject;
            set
            {
                if (_projectSession.ProjectObject == value) return;
                _projectSession.ProjectObject = value;
                OnPropertyChanged();
                if (_isResetting || _projectSession.IsLoadProjectInProgress) return;
                _projectSession.MarkDirty();
            }
        }

        // ============================================
        // Блок 1 - KPI (вычисляемые показатели)
        // ============================================

        /// <summary>
        /// Суммарная тепловая мощность всех контуров, кВт
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalThermalPower_W))]
        private double _totalThermalPower_kW;

        /// <summary>
        /// Суммарная тепловая мощность в Вт
        /// </summary>
        public double TotalThermalPower_W => TotalThermalPower_kW * 1000;

        /// <summary>
        /// Объём системы (труб), литры
        /// </summary>
        [ObservableProperty]
        private double _systemVolume_L;

        /// <summary>
        /// Суммарный расход насоса, м³/ч
        /// </summary>
        [ObservableProperty]
        private double _pumpFlowRate_m3h;

        /// <summary>
        /// Напор насоса (максимальные потери), кПа
        /// </summary>
        [ObservableProperty]
        private double _pumpHead_kPa;

        /// <summary>
        /// Объём расширительного бака, литры
        /// Формула: V_системы × β × 1.2
        /// </summary>
        [ObservableProperty]
        private double _expansionTankVolume_L;

        // ============================================
        // Block 1 - Temperature KPIs
        // ============================================

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        [ObservableProperty]
        private double _supplyTemperature;

        /// <summary>
        /// Температура обратки, °C
        /// </summary>
        [ObservableProperty]
        private double _returnTemperature;

        /// <summary>
        /// Рабочая температура (средняя), °C
        /// </summary>
        [ObservableProperty]
        private double _operatingTemperature;

        /// <summary>
        /// Температура грунта, °C
        /// </summary>
        [ObservableProperty]
        private double _groundTemperature;

        /// <summary>
        /// Скорость ветра, м/с
        /// </summary>
        [ObservableProperty]
        private double _windSpeed;

        /// <summary>
        /// Интенсивность снегопада, мм/ч
        /// </summary>
        [ObservableProperty]
        private double _snowfallIntensity;

        /// <summary>
        /// Температура поверхности (+3, +5, +7)
        /// </summary>
        [ObservableProperty]
        private int _surfaceTemperature;

        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        [ObservableProperty]
        private double _glycolConcentration;

        /// <summary>
        /// Суммарная удельная мощность, Вт/м²
        /// </summary>
        [ObservableProperty]
        private double _totalPowerDensity;

        // ============================================
        // Блок 2 - Исходные данные (Климат)
        // ============================================

        /// <summary>
        /// Выбранный город
        /// </summary>
        [ObservableProperty]
        private string _selectedCity = string.Empty;

        /// <summary>
        /// Расчётная температура наружного воздуха, °C
        /// </summary>
        [ObservableProperty]
        private double _designTemperature;

        /// <summary>
        /// Тип трубы
        /// </summary>
        [ObservableProperty]
        private string _pipeType = string.Empty;

        /// <summary>
        /// Шаг укладки труб, мм
        /// </summary>
        [ObservableProperty]
        private int _pipeSpacing;

        /// <summary>
        /// Климатическая зона
        /// </summary>
        [ObservableProperty]
        private ClimateZone _climateZone;

        /// <summary>
        /// Количество дней холодного периода
        /// </summary>
        [ObservableProperty]
        private int _coldPeriodDays;

        /// <summary>
        /// Тип гликоля
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GlycolTypeDisplayName))]
        private GlycolType _glycolType;

        /// <summary>
        /// Название теплоносителя на русском
        /// </summary>
        public string GlycolTypeDisplayName => GetGlycolTypeName(GlycolType);

        /// <summary>
        /// Режим работы системы
        /// </summary>
        [ObservableProperty]
        private OperatingMode _operatingMode;

        // ============================================
        // Блок 3 - Конструкция
        // ============================================

        /// <summary>
        /// Термическое сопротивление над трубой (R1), м²·К/Вт
        /// </summary>
        [ObservableProperty]
        private double _r1;

        /// <summary>
        /// Термическое сопротивление под трубой (R2), м²·К/Вт
        /// </summary>
        [ObservableProperty]
        private double _r2;

        /// <summary>
        /// Теплопроводность материала вокруг трубы (LambdaE), Вт/м·К
        /// </summary>
        [ObservableProperty]
        private double _lambdaE;

        /// <summary>
        /// Удельная мощность вверх, Вт/м²
        /// </summary>
        [ObservableProperty]
        private double _powerUp;

        /// <summary>
        /// Удельная мощность вниз, Вт/м²
        /// </summary>
        [ObservableProperty]
        private double _powerDown;

        /// <summary>
        /// Слои конструкции для визуализации
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Layer> _layers = new();

        // ============================================
        // Блок 4 - Режим отображения
        // ============================================

        /// <summary>
        /// Признак рабочего режима (true = рабочая температура, false = расчётная)
        /// </summary>
        /// <remarks>
        /// Phase 8 / ST-003: источник истины — app-owned
        /// <see cref="IProjectDisplayModeState"/> (когда зарегистрирован);
        /// VM-поле остаётся fallback для legacy-сборки без seam.
        /// </remarks>
        private bool _isOperatingMode = true;

        public bool IsOperatingMode
        {
            get => _displayModeState?.IsOperatingMode ?? _isOperatingMode;
            set
            {
                if (IsOperatingMode == value) return;
                if (_displayModeState is not null)
                {
                    _displayModeState.IsOperatingMode = value;
                }
                else
                {
                    _isOperatingMode = value;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentModeText));
                OnPropertyChanged(nameof(IsDesignMode));
                OnPropertyChanged(nameof(MaxPressureLoss));
            }
        }

        /// <summary>
        /// Признак расчётного режима
        /// </summary>
        public bool IsDesignMode => !IsOperatingMode;

        /// <summary>
        /// Текст текущего режима
        /// </summary>
        public string CurrentModeText => IsOperatingMode
            ? "Рабочий режим"
            : "Расчётный режим (холодный пуск)";

        /// <summary>
        /// Сервис состояния расчёта (канонический источник шага укладки)
        /// </summary>
        public ICalculationStateService CalculationStateService => _calculationStateService;

        // ============================================
        // Блок 5 - Гидравлика
        // ============================================

        /// <summary>
        /// Список коллекторов для переключателя
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CollectorInfo> _collectors = new();

        /// <summary>
        /// Индекс выбранного коллектора
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorInfo))]
        private int _selectedCollectorIndex;

        /// <summary>
        /// Выбранный коллектор
        /// </summary>
        public CollectorInfo? SelectedCollectorInfo =>
            SelectedCollectorIndex >= 0 && SelectedCollectorIndex < Collectors.Count
                ? Collectors[SelectedCollectorIndex]
                : null;

        /// <summary>
        /// Контуры для отображения (фильтруются по режиму)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CircuitRow> _circuits = new();

        /// <summary>
        /// Итоги по выбранному коллектору
        /// </summary>
        [ObservableProperty]
        private CollectorSummary? _collectorSummary;

        /// <summary>
        /// Количество контуров в выбранном коллекторе
        /// </summary>
        public int TotalCircuits => CollectorSummary?.CircuitCount ?? 0;

        /// <summary>
        /// Общий расход выбранного коллектора, л/ч
        /// </summary>
        public double TotalFlowRate => CollectorSummary?.TotalFlowRate ?? 0;

        /// <summary>
        /// Максимальные потери давления в выбранном коллекторе, Па
        /// </summary>
        public double MaxPressureLoss => IsOperatingMode
            ? (CollectorSummary?.PressureLoss_Operating_Pa ?? 0)
            : (CollectorSummary?.PressureLoss_Cold_Pa ?? 0);

        // ============================================
        // Блок 6 - Оборудование
        // ============================================

        /// <summary>
        /// Спецификации коллекторов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CollectorSpecification> _collectorSpecifications = new();

        /// <summary>
        /// Сгруппированные элементы оборудования коллекторов для карточки Оборудование → Коллекторы
        /// </summary>
        /// <remarks>
        /// Группировка по (ValveType, CircuitCount) с явным количеством коллекторов в группе.
        /// </remarks>
        [ObservableProperty]
        private ObservableCollection<CollectorEquipmentItem> _collectorEquipmentItems = new();

        /// <summary>
        /// Карточки итогов коллекторов (для отображения в Results)
        /// </summary>
        /// <remarks>
        /// Заполняется через RebuildHydraulicSummaryCards() из канонического HydraulicsState.
        /// Каждая карточка — снимок CollectorData.Summary + CollectorNumber + CollectorTypeDisplayWithCount.
        /// </remarks>
        [ObservableProperty]
        private ObservableCollection<CollectorHydraulicSummaryCard> _hydraulicSummaryCards = new();

        /// <summary>
        /// Общая длина труб, м
        /// </summary>
        [ObservableProperty]
        private double _totalPipeLength;

        /// <summary>
        /// Количество РЗС (распределительных узлов)
        /// </summary>
        [ObservableProperty]
        private int _rzsCount;

        /// <summary>
        /// Расход насоса, м³/ч
        /// </summary>
        [ObservableProperty]
        private double _pumpQ;

        /// <summary>
        /// Напор насоса, кПа
        /// </summary>
        [ObservableProperty]
        private double _pumpH;

        /// <summary>
        /// Объём расширительного бака, литры
        /// </summary>
        [ObservableProperty]
        private double _expansionTankV;

        // ============================================
        // Состояния
        // ============================================

        /// <summary>
        /// Признак готовности данных (все модули валидны)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDataNotReady))]
        private bool _isDataReady;

        /// <summary>
        /// Признак неготовности данных
        /// </summary>
        public bool IsDataNotReady => !IsDataReady;

        /// <summary>
        /// Список неготовых модулей
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _missingModules = new();

        /// <summary>
        /// Сообщение о состоянии готовности
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        #endregion

        #region Constructor

        /// <summary>
        /// Событие изменения данных проекта
        /// </summary>
        public event EventHandler<ProjectData>? ProjectChanged;

        /// <summary>
        /// Конструктор ViewModel результатов
        /// </summary>
        public ResultsViewModel(
            IProjectSession projectSession,
            IDialogService dialogService,
            IPdfExportService pdfExportService,
            ICalculationReportExportService calculationReportExportService,
            IProjectFileService projectFileService,
            ICalculationStateService calculationStateService,
            IMaterialRepository materialRepository,
            IConstructionService constructionService,
            ProjectLoadOrchestrator projectLoadOrchestrator,
            ResultsPdfDataBuilder resultsPdfDataBuilder,
            HydraulicSummaryBuilder hydraulicSummaryBuilder,
            ResultsKpiPresenter? resultsKpiPresenter = null,
            IProjectSaveService? projectSaveService = null,
            IProjectDisplayModeState? displayModeState = null)
        {
            _projectSession = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
            _calculationReportExportService = calculationReportExportService ?? throw new ArgumentNullException(nameof(calculationReportExportService));
            _projectFileService = projectFileService ?? throw new ArgumentNullException(nameof(projectFileService));
            _projectSaveService = projectSaveService;
            _displayModeState = displayModeState;
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _projectLoadOrchestrator = projectLoadOrchestrator ?? throw new ArgumentNullException(nameof(projectLoadOrchestrator));
            _resultsPdfDataBuilder = resultsPdfDataBuilder ?? throw new ArgumentNullException(nameof(resultsPdfDataBuilder));
            _hydraulicSummaryBuilder = hydraulicSummaryBuilder ?? throw new ArgumentNullException(nameof(hydraulicSummaryBuilder));
            _resultsKpiPresenter = resultsKpiPresenter ?? new ResultsKpiPresenter();
            if (_displayModeState is not null)
            {
                _displayModeState.IsOperatingMode = IsOperatingMode;
            }

            // Загружаем начальные данные
            LoadClimateData();
            LoadConstructionData();
            LoadThermalData();

            // Проверяем готовность данных
            CheckDataReadiness();
        }

        /// <summary>
        /// Загрузить данные гидравлики (вызывается при переходе на вкладку)
        /// </summary>
        public void LoadHydraulicsDataOnNavigate()
        {
            RefreshAll();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда переключения режима (рабочий/расчётный)
        /// </summary>
        [RelayCommand]
        private void ToggleMode()
        {
            IsOperatingMode = !IsOperatingMode;
            UpdateCircuitsFilter();
            UpdatePumpHead();
        }

        /// <summary>
        /// Команда выбора коллектора
        /// </summary>
        [RelayCommand]
        private void SelectCollector(int number)
        {
            var collector = Collectors.FirstOrDefault(c => c.Number == number);
            if (collector == null)
            {
                return;
            }

            var index = Collectors.IndexOf(collector);
            if (index >= 0)
            {
                SelectedCollectorIndex = index;
                UpdateCollectorSelectionState();
                UpdateCollectorSummary();
                UpdateCircuitsFilter();
            }
        }

        /// <summary>
        /// Обновить состояние выбора для всех коллекторов
        /// </summary>
        partial void OnSelectedCollectorIndexChanged(int value)
        {
            UpdateCollectorSelectionState();
        }

        /// <summary>
        /// Обновить свойство IsSelected для всех коллекторов
        /// </summary>
        private void UpdateCollectorSelectionState()
        {
            for (int i = 0; i < Collectors.Count; i++)
            {
                Collectors[i].IsSelected = (i == SelectedCollectorIndex);
            }
        }

        /// <summary>
        /// Команда экспорта в PDF
        /// </summary>
        [RelayCommand]
        private async Task ExportPdf()
        {
            RefreshAll();

            if (!IsDataReady)
            {
                StatusMessage = "Невозможно экспортировать: не все данные готовы";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                return;
            }

            var fileName = _dialogService.ShowSaveFileDialog(
                $"Результаты_{ProjectNumber}_{DateTime.Now:yyyyMMdd}.pdf",
                "PDF файлы (*.pdf)|*.pdf",
                title: "Экспорт результатов в PDF",
                defaultExt: "pdf");

            if (fileName == null)
                return;

            try
            {
                StatusMessage = "Экспорт в PDF...";
                var pdfData = _resultsPdfDataBuilder.Build(this);
                var success = await _pdfExportService.ExportResultsToPdfAsync(fileName, pdfData);

                if (success)
                {
                    StatusMessage = $"PDF сохранён: {Path.GetFileName(fileName)}";
                }
                else
                {
                    StatusMessage = "Ошибка при экспорте PDF";
                }

                await Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта: {ex.Message}";
                await Task.Delay(5000);
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Команда экспорта детального отчёта в Markdown для рабочего режима
        /// </summary>
        [RelayCommand]
        private async Task ExportOperatingMarkdownReport()
        {
            RefreshAll();

            await ExportMarkdownReportAsync(
                CalculationReportMode.Operating,
                $"Детальный_отчёт_рабочий_{ProjectNumber}_{DateTime.Now:yyyyMMdd}.md",
                "Экспорт детального отчёта рабочего режима в Markdown");
        }

        /// <summary>
        /// Команда экспорта детального отчёта в Markdown для расчётного холодного режима
        /// </summary>
        [RelayCommand]
        private async Task ExportDesignColdMarkdownReport()
        {
            RefreshAll();

            await ExportMarkdownReportAsync(
                CalculationReportMode.DesignCold,
                $"Детальный_отчёт_расчётный_холодный_{ProjectNumber}_{DateTime.Now:yyyyMMdd}.md",
                "Экспорт детального отчёта расчётного холодного режима в Markdown");
        }

        private async Task ExportMarkdownReportAsync(
            CalculationReportMode mode,
            string defaultFileName,
            string title)
        {
            if (!IsDataReady)
            {
                StatusMessage = "Невозможно экспортировать: не все данные готовы";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                return;
            }

            var fileName = _dialogService.ShowSaveFileDialog(
                defaultFileName,
                "Markdown файлы (*.md)|*.md",
                title: title,
                defaultExt: "md");

            if (fileName == null)
                return;

            try
            {
                StatusMessage = "Экспорт детального отчёта...";
                var success = await _calculationReportExportService.ExportReportAsync(fileName, SaveCurrentProject(), mode);

                if (success)
                {
                    StatusMessage = $"Отчёт сохранён: {Path.GetFileName(fileName)}";
                }
                else
                {
                    StatusMessage = "Ошибка при экспорте отчёта";
                }

                await Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта: {ex.Message}";
                await Task.Delay(5000);
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Команда экспорта в Excel (заглушка)
        /// </summary>
        [RelayCommand]
        private async Task ExportExcel()
        {
            RefreshAll();

            // TODO: Реализовать экспорт в Excel
            StatusMessage = "Экспорт в Excel будет реализован в следующей версии";
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// Команда сохранения проекта
        /// </summary>
        [RelayCommand]
        private async Task SaveProject(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_projectSession.CurrentFilePath))
            {
                await SaveProjectAs(cancellationToken);
                return;
            }

            await SaveToFile(_projectSession.CurrentFilePath, cancellationToken);
        }

        /// <summary>
        /// Команда сохранения проекта с выбором пути
        /// </summary>
        [RelayCommand]
        private async Task SaveProjectAs(CancellationToken cancellationToken)
        {
            var defaultFileName = $"{ProjectNumber}_{DateTime.Now:yyyyMMdd}";
            var filePath = _dialogService.ShowSaveFileDialog(defaultFileName);

            if (string.IsNullOrEmpty(filePath))
                return;

            if (await SaveToFile(filePath, cancellationToken))
            {
                _projectSession.CurrentFilePath = filePath;
            }
        }

        /// <summary>
        /// Команда открытия проекта
        /// </summary>
        [RelayCommand]
        private async Task OpenProject()
        {
            var filePath = _dialogService.ShowOpenFileDialog();

            if (string.IsNullOrEmpty(filePath))
                return;

            await LoadProjectFromPathAsync(filePath);
        }

        /// <summary>
        /// Загрузить проект по указанному пути (используется при открытии файла извне,
        /// например двойным кликом по файлу .smc в проводнике).
        /// </summary>
        /// <param name="filePath">Путь к файлу проекта</param>
        public async Task LoadProjectFromPathAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var result = await _projectFileService.LoadProjectResultAsync(filePath);
            if (!result.IsSuccess || result.Value == null)
            {
                _dialogService.ShowError($"Не удалось открыть проект: {result.Error}", "Ошибка");
                return;
            }

            await ApplyLoadedProjectAsync(filePath, result.Value);
        }

        /// <summary>
        /// Применить загруженные данные проекта ко всем модулям.
        /// </summary>
        /// <param name="filePath">Путь к файлу проекта</param>
        /// <param name="data">Данные проекта</param>
        private async Task ApplyLoadedProjectAsync(string filePath, ProjectData data)
        {
            // Подтверждение загрузки, если есть несохранённые данные
            if (_projectSession.IsDirty)
            {
                var confirmation = _dialogService.Show(
                    "Текущий проект будет заменён. Продолжить?",
                    "Открытие проекта",
                    DialogButtons.YesNo,
                    DialogIcon.Question);

                if (confirmation != DialogResult.Yes)
                    return;
            }

            // Сброс всех модулей перед загрузкой нового проекта,
            // чтобы избежать "залипания" старых результатов и ошибок.
            Reset();
            _projectLoadOrchestrator.ResetModules();
            _projectSession.MarkClean();

            await LoadProjectDataAsync(data);
            _projectSession.CurrentFilePath = filePath;
            _projectSession.MarkClean();

            StatusMessage = $"Проект загружен: {Path.GetFileName(filePath)}";
            await Task.Delay(3000);
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// Команда предпросмотра PDF
        /// </summary>
        [RelayCommand]
        private async Task PreviewPdf()
        {
            RefreshAll();

            if (!IsDataReady)
            {
                StatusMessage = "Невозможно создать предпросмотр: не все данные готовы";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                return;
            }

            try
            {
                StatusMessage = "Создание предпросмотра...";
                var pdfData = _resultsPdfDataBuilder.Build(this);
                var tempPath = _projectFileService.GetPreviewPdfPath();

                var success = await _pdfExportService.ExportResultsToPdfAsync(tempPath, pdfData);

                if (success)
                {
                    // Открываем PDF в приложении по умолчанию
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });
                    StatusMessage = "Предпросмотр открыт";
                }
                else
                {
                    StatusMessage = "Ошибка при создании предпросмотра";
                }

                await Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка предпросмотра: {ex.Message}";
                await Task.Delay(5000);
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Команда печати PDF
        /// </summary>
        [RelayCommand]
        private async Task PrintPdf()
        {
            RefreshAll();

            if (!IsDataReady)
            {
                StatusMessage = "Невозможно напечатать: не все данные готовы";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                return;
            }

            try
            {
                StatusMessage = "Подготовка к печати...";
                var pdfData = _resultsPdfDataBuilder.Build(this);
                var tempPath = _projectFileService.GetPreviewPdfPath();

                var success = await _pdfExportService.ExportResultsToPdfAsync(tempPath, pdfData);

                if (success)
                {
                    // Системный диалог печати — через тестовый шов IDialogService
                    if (_dialogService.ShowPrintDialog())
                    {
                        // Печать через Process с verb "print"
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = tempPath,
                                Verb = "print",
                                CreateNoWindow = true,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            }
                        };
                        process.Start();
                        StatusMessage = "Документ отправлен на печать";
                    }
                    else
                    {
                        StatusMessage = "Печать отменена";
                    }
                }
                else
                {
                    StatusMessage = "Ошибка при подготовке к печати";
                }

                await Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка печати: {ex.Message}";
                await Task.Delay(5000);
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Сохранить данные в файл
        /// </summary>
        private async Task<bool> SaveToFile(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                StatusMessage = "Сохранение проекта...";
                var dates = new ProjectSaveDates(_createdDate, DateTime.Now);
                var result = _projectSaveService is not null
                    ? await _projectSaveService.SaveAsync(
                        _projectSession,
                        filePath,
                        dates,
                        cancellationToken)
                    : await SaveLegacyFileAsync(filePath, dates, cancellationToken);
                if (!result.IsSuccess)
                {
                    _dialogService.ShowError($"Не удалось сохранить проект: {result.Error}", "Ошибка");
                    return false;
                }

                StatusMessage = $"Проект сохранён: {Path.GetFileName(filePath)}";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                _createdDate = dates.CreatedDate;
                _projectSession.MarkClean();
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                await Task.Delay(5000);
                StatusMessage = string.Empty;
                return false;
            }
        }

        private async Task<Core.Results.OperationResult<object?>> SaveLegacyFileAsync(
            string filePath,
            ProjectSaveDates dates,
            CancellationToken cancellationToken)
        {
            var data = SaveCurrentProject();
            data.CreatedDate = dates.CreatedDate;
            data.ModifiedDate = dates.ModifiedDate;
            return await _projectFileService.SaveProjectResultAsync(filePath, data, cancellationToken);
        }

        #endregion

        #region Data Loading Methods

        /// <summary>
        /// Загрузить климатические данные
        /// </summary>
        private void LoadClimateData()
        {
            // Phase 8: проекция Results читает канонический ClimateState snapshot,
            // а не кэши ClimateViewModel. ColdPeriodDays канонизирован как
            // ClimateStateSnapshot.Period0Days (Amendment 1, owner decision B).
            var climateSnapshot = _projectSession.ClimateState.Snapshot;
            SelectedCity = climateSnapshot.SelectedCity ?? string.Empty;
            DesignTemperature = climateSnapshot.AirTemperature;
            ClimateZone = climateSnapshot.Zone;
            ColdPeriodDays = string.IsNullOrEmpty(climateSnapshot.SelectedCity)
                ? 150
                : climateSnapshot.Period0Days;
            WindSpeed = climateSnapshot.WindSpeed;
            SnowfallIntensity = climateSnapshot.SnowfallIntensity;
        }

        /// <summary>
        /// Загрузить данные конструкции
        /// </summary>
        private void LoadConstructionData()
        {
            // Phase 8: R1/R2/LambdaE читаются из канонической проекции ConstructionState
            // (формулы идентичны модели Construction), слои реконструируются из канонического
            // снапшота в порядке присваиваний Layer.Clone(), чтобы сеттер Material не
            // перезаписал снапшотную λ.
            var constructionState = _projectSession.ConstructionState;
            var projection = constructionState.CurrentProjection;
            R1 = projection.R1Total;
            R2 = projection.R2Total;
            LambdaE = projection.LambdaE;

            Layers.Clear();
            AppendLayers(constructionState.Snapshot.LayersAbovePipe);
            AppendLayers(constructionState.Snapshot.LayersBelowPipe);
        }

        private void AppendLayers(IReadOnlyList<ConstructionLayerSnapshot> layerSnapshots)
        {
            foreach (var layerSnapshot in layerSnapshots)
            {
                Layers.Add(new Layer
                {
                    Id = layerSnapshot.Id,
                    Material = _materialRepository.GetMaterialById(layerSnapshot.MaterialId)
                        ?? new Material { Id = layerSnapshot.MaterialId, Name = layerSnapshot.MaterialName ?? "Не указан" },
                    Thickness = layerSnapshot.Thickness,
                    CalculatedLambda = layerSnapshot.CalculatedLambda,
                    IsLambdaOverridden = layerSnapshot.IsLambdaOverridden,
                    Position = layerSnapshot.Position,
                    Order = layerSnapshot.Order
                });
            }
        }

        /// <summary>
        /// Загрузить тепловые данные
        /// </summary>
        private void LoadThermalData()
        {
            // Todo 10 (DEC-T07): проекция Results читает входы из канонического
            // ThermalState snapshot, а не из кэшей VM/сервиса. Последний результат
            // остаётся на текущей проекции адаптера — frozen characterization
            // (ResultsStabilizationPhase1BehaviorContractsTests) фиксирует, что
            // очистка адаптерного результата обнуляет KPI без пересчёта.
            var thermalSnapshot = _projectSession.ThermalState.Snapshot;

            PipeType = thermalSnapshot.Inputs.Pipe?.Name ?? string.Empty;
            PipeSpacing = thermalSnapshot.Inputs.PipeSpacing;
            OperatingMode = thermalSnapshot.Inputs.Mode;
            GroundTemperature = thermalSnapshot.Inputs.GroundTemperature;

            // Surface temperature from mode: +3, +5, +7
            SurfaceTemperature = (int)thermalSnapshot.Inputs.Mode;

            var result = _projectSession.ThermalState.Snapshot.Result;
            if (result != null)
            {
                PowerUp = result.PowerUp;
                PowerDown = result.PowerDown;
                SupplyTemperature = result.SupplyTemperature;
                ReturnTemperature = result.ReturnTemperature;
                OperatingTemperature = result.MeanTemperature;
                TotalPowerDensity = result.PowerTotal;
            }
            else
            {
                // Reset values when no result available
                PowerUp = 0;
                PowerDown = 0;
                SupplyTemperature = 0;
                ReturnTemperature = 0;
                OperatingTemperature = 0;
                TotalPowerDensity = 0;
            }
        }

        /// <summary>
        /// Загрузить гидравлические данные
        /// </summary>
        private void LoadHydraulicsData()
        {
            // Phase 8: тип гликоля и концентрация читаются из канонического
            // HydraulicsState.GlobalInputs (Default = Ethylene/50 — те же значения,
            // что давал fallback адаптера).
            var globalInputs = _projectSession.HydraulicsState.Snapshot.GlobalInputs;
            GlycolType = globalInputs.GlycolType;
            GlycolConcentration = globalInputs.GlycolConcentration;

            // Обновляем список коллекторов
            UpdateCollectorsList();

            // Обновляем контуры
            UpdateCircuitsFilter();

            // Обновляем спецификации
            UpdateCollectorSpecifications();

            // Обновляем сгруппированное оборудование коллекторов
            UpdateCollectorEquipmentItems();
        }

        /// <summary>
        /// Получить название теплоносителя на русском
        /// </summary>
        private static string GetGlycolTypeName(GlycolType type)
        {
            return type switch
            {
                GlycolType.Ethylene => "Этиленгликоль",
                GlycolType.Propylene => "Пропиленгликоль",
                _ => "Вода"
            };
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Пересчитать все KPI показатели
        /// </summary>
        /// <remarks>
        /// DE-3: вычисления выполняет чистый <see cref="ResultsKpiPresenter"/>
        /// из канонических снимков (HydraulicsState + труба ThermalState);
        /// VM назначает read-model наблюдаемым свойствам. Состав назначений
        /// 1:1 с прежними CalculateTotalPower/CalculateSystemVolume/
        /// CalculatePumpParameters/CalculateExpansionTank.
        /// </remarks>
        private void RecalculateKpi()
        {
            var kpis = _resultsKpiPresenter.BuildKpis(
                _projectSession.HydraulicsState.Snapshot.Collectors,
                _projectSession.ThermalState.Snapshot.Inputs.Pipe?.InnerDiameter,
                IsOperatingMode);

            TotalThermalPower_kW = kpis.TotalThermalPower_kW;
            SystemVolume_L = kpis.SystemVolume_L;
            TotalPipeLength = kpis.TotalPipeLength;
            PumpFlowRate_m3h = kpis.PumpFlowRate_m3h;
            PumpQ = kpis.PumpFlowRate_m3h;
            PumpHead_kPa = kpis.PumpHead_kPa;
            PumpH = kpis.PumpHead_kPa;
            ExpansionTankVolume_L = kpis.ExpansionTankVolume_L;
            ExpansionTankV = kpis.ExpansionTankVolume_L;

            UpdateCollectorSpecifications();
            UpdateCollectorEquipmentItems();
        }

        /// <summary>
        /// Обновить напор насоса при переключении режима
        /// </summary>
        private void UpdatePumpHead()
        {
            PumpHead_kPa = _resultsKpiPresenter.BuildPumpHead(
                _projectSession.HydraulicsState.Snapshot.Collectors,
                IsOperatingMode);
            PumpH = PumpHead_kPa;
        }

        /// <summary>
        /// Обновить список коллекторов
        /// </summary>
        private void UpdateCollectorsList()
        {
            var previousIndex = SelectedCollectorIndex; // Сохраняем текущий выбор

            Collectors.Clear();

            // Phase 8: список коллекторов строится из канонического HydraulicsState
            // snapshot. Пустой канон: общий путь ниже сбрасывает RzsCount, выбор и
            // обновляет summary/filter (ранний return здесь оставил бы stale-итоги
            // выбранного коллектора от предыдущего проекта).
            // DE-3: маппинг строк — HydraulicSummaryBuilder.BuildCollectorInfos.
            var canonicalCollectors = _projectSession.HydraulicsState.Snapshot.Collectors;

            foreach (var collectorInfo in _hydraulicSummaryBuilder.BuildCollectorInfos(canonicalCollectors))
            {
                collectorInfo.SetParent(this);
                Collectors.Add(collectorInfo);
            }

            RzsCount = Collectors.Count;

            // Восстанавливаем выбор, если возможно
            if (previousIndex >= 0 && previousIndex < Collectors.Count)
            {
                SelectedCollectorIndex = previousIndex;
            }
            else if (Collectors.Count > 0)
            {
                SelectedCollectorIndex = 0; // Первый по умолчанию
            }
            else
            {
                SelectedCollectorIndex = -1;
            }

            // Обновляем данные выбранного коллектора
            UpdateCollectorSummary();
            UpdateCircuitsFilter();
        }

        /// <summary>
        /// Обновить итоги по выбранному коллектору
        /// </summary>
        /// <remarks>
        /// Phase 9 (ST-027): сводка строится из канонического снапшота по
        /// выбору Results; выбор модульного ViewModel не читается.
        /// </remarks>
        private void UpdateCollectorSummary()
        {
            // Проверяем валидность индекса
            if (SelectedCollectorIndex < 0 || SelectedCollectorIndex >= Collectors.Count)
            {
                CollectorSummary = null;
                return;
            }

            var canonicalCollectors = _projectSession.HydraulicsState.Snapshot.Collectors;
            if (SelectedCollectorIndex >= canonicalCollectors.Count)
            {
                CollectorSummary = null;
                return;
            }

            // DE-3: маппинг сводки — HydraulicSummaryBuilder.BuildCollectorSummary.
            CollectorSummary = _hydraulicSummaryBuilder.BuildCollectorSummary(
                canonicalCollectors[SelectedCollectorIndex]);
        }

        /// <summary>
        /// Обновить фильтр контуров по режиму
        /// </summary>
        /// <remarks>
        /// Phase 9 (ST-026): строки реконструируются из канонического
        /// HydraulicsState-снапшота и принадлежат Results; модульные объекты
        /// CircuitRow не читаются и не мутируются.
        /// </remarks>
        private void UpdateCircuitsFilter()
        {
            Circuits.Clear();

            // Получаем выбранный коллектор по индексу
            if (SelectedCollectorIndex < 0 || SelectedCollectorIndex >= Collectors.Count)
                return;

            var canonicalCollectors = _projectSession.HydraulicsState.Snapshot.Collectors;
            if (SelectedCollectorIndex >= canonicalCollectors.Count)
                return;

            // DE-3: реконструкция строк с режимом отображения —
            // HydraulicCircuitRowProjection.CreateRows.
            foreach (var circuit in HydraulicCircuitRowProjection.CreateRows(
                canonicalCollectors[SelectedCollectorIndex], IsOperatingMode))
            {
                Circuits.Add(circuit);
            }
        }

        /// <summary>
        /// Обновить спецификации коллекторов
        /// </summary>
        private void UpdateCollectorSpecifications()
        {
            CollectorSpecifications.Clear();

            foreach (var spec in _hydraulicSummaryBuilder.BuildSpecifications(
                _projectSession.HydraulicsState.Snapshot.Collectors, IsOperatingMode))
            {
                CollectorSpecifications.Add(spec);
            }
        }

        /// <summary>
        /// Обновить сгруппированный read-model оборудования коллекторов
        /// </summary>
        /// <remarks>
        /// При потере готовности данных, пустом или null-списке коллекторов коллекция очищается.
        /// </remarks>
        private void UpdateCollectorEquipmentItems()
        {
            CollectorEquipmentItems.Clear();

            if (!IsDataReady) return;

            foreach (var item in _hydraulicSummaryBuilder.BuildEquipmentItems(
                _projectSession.HydraulicsState.Snapshot.Collectors))
            {
                CollectorEquipmentItems.Add(item);
            }
        }

        /// <summary>
        /// Перестроить канонический read-model карточек итогов гидравлики по всем коллекторам.
        /// </summary>
        private void RebuildHydraulicSummaryCards()
        {
            HydraulicSummaryCards.Clear();

            foreach (var card in _hydraulicSummaryBuilder.BuildSummaryCards(
                _projectSession.HydraulicsState.Snapshot.Collectors))
            {
                HydraulicSummaryCards.Add(card);
            }
        }

        /// <summary>
        /// Проверить готовность данных всех модулей
        /// </summary>
        private void CheckDataReadiness()
        {
            MissingModules.Clear();

            // Проверка климатических данных (Phase 8: канонический ClimateState;
            // IsCitySelected выставляется в ApplyCitySelection/ApplyProjectSnapshot)
            if (!_projectSession.ClimateState.Snapshot.IsCitySelected)
            {
                MissingModules.Add("Климат - не выбран город");
            }

            // Проверка конструкции (Phase 8: каноническая проекция ConstructionState)
            if (!_projectSession.ConstructionState.CurrentProjection.IsValid)
            {
                MissingModules.Add("Конструкция - невалидные данные");
            }

            // Проверка теплового расчёта — из канонического ThermalState (Phase 8:
            // тот же источник, что и проекция значений)
            var thermalResult = _projectSession.ThermalState.Snapshot.Result;
            if (thermalResult == null || !thermalResult.IsValid)
            {
                MissingModules.Add("Тепловой расчёт - нет результата");
            }
            else if (_projectSession.ThermalState.Snapshot.Inputs.Pipe == null)
            {
                MissingModules.Add("Тепловой расчёт - не выбрана труба");
            }

            // Проверка гидравлического расчёта (Phase 8: канонический HydraulicsState)
            var hasValidCircuits = _projectSession.HydraulicsState.Snapshot.Collectors
                .Any(c => c?.Circuits != null && c.Circuits.Any(circuit => circuit.CircuitLength > 0));

            if (!hasValidCircuits)
            {
                MissingModules.Add("Гидравлика - нет контуров");
            }

            IsDataReady = MissingModules.Count == 0;

            StatusMessage = IsDataReady
                ? "Все данные готовы"
                : $"Не готовы модули: {string.Join(", ", MissingModules)}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обновить все данные
        /// </summary>
        public void RefreshAll()
        {
            LoadClimateData();
            LoadConstructionData();
            LoadThermalData();
            LoadHydraulicsData();
            CheckDataReadiness();
            RecalculateKpi();
            RebuildHydraulicSummaryCards();

            // Финальная синхронизация grouped read-model с учётом актуальной готовности данных
            UpdateCollectorEquipmentItems();
        }

        /// <summary>
        /// Сбросить ViewModel к начальному состоянию
        /// </summary>
        public void Reset()
        {
            _isResetting = true;
            try
            {
                ProjectNumber = string.Empty;
                ProjectObject = string.Empty;
                _projectSession.CurrentFilePath = null;
                StatusMessage = string.Empty;
                IsOperatingMode = true;

                // Очистка кэшированных результатов
                TotalThermalPower_kW = 0;
                SystemVolume_L = 0;
                PumpFlowRate_m3h = 0;
                PumpHead_kPa = 0;
                ExpansionTankVolume_L = 0;
                SupplyTemperature = 0;
                ReturnTemperature = 0;
                OperatingTemperature = 0;
                GroundTemperature = 0;
                WindSpeed = 0;
                SnowfallIntensity = 0;
                SurfaceTemperature = 0;
                GlycolConcentration = 0;
                TotalPowerDensity = 0;
                R1 = 0;
                R2 = 0;
                LambdaE = 0;
                PowerUp = 0;
                PowerDown = 0;
                TotalPipeLength = 0;
                RzsCount = 0;
                PumpQ = 0;
                PumpH = 0;
                ExpansionTankV = 0;
                Layers.Clear();
                Collectors.Clear();
                Circuits.Clear();
                CollectorSpecifications.Clear();
                CollectorEquipmentItems.Clear();
                // Очищаем карточки, а не перестраиваем: в момент Reset() канонический
                // HydraulicsState ещё не сброшен (координатор модуля сбрасывается позже),
                // иначе RebuildHydraulicSummaryCards() оставит stale-снимки из
                // предыдущего проекта. Карточки пересоберутся при следующем
                // LoadHydraulicsDataOnNavigate() / RefreshAll().
                HydraulicSummaryCards.Clear();
                SelectedCollectorIndex = 0;
                CollectorSummary = null;
                MissingModules.Clear();
                IsDataReady = false;

                _projectSession.MarkClean();
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Загрузить данные проекта из модели
        /// </summary>
        public async Task LoadProjectDataAsync(ProjectData data)
        {
            if (data == null) return;

            using var restoreScope = _projectSession.BeginProjectRestore();

            try
            {
                // Восстанавливаем режим отображения
                IsOperatingMode = data.IsOperatingMode;
                _createdDate = data.CreatedDate;

                // Загружаем информацию о проекте.
                // Свойства VM — pass-through к IProjectSession (этап C4):
                // присвоение обновляет и сервис; dirty не ставится — активен guard загрузки.
                ProjectNumber = data.ProjectNumber;
                ProjectObject = data.ProjectObject;

                // Восстанавливаем состояние модулей (климат, конструкция,
                // тепловой расчёт, гидравлика) — оркестрация вынесена в
                // ProjectLoadOrchestrator (этап C1). Файл — источник истины.
                var restored = await _projectLoadOrchestrator.RestoreModulesFromProjectAsync(data);
                if (!restored)
                {
                    return;
                }

                // Единственное обновление снимка Results — ПОСЛЕ финального теплового
                // результата, чтобы KPI не оставались снимком, снятым до расчёта.
                RefreshAll();

                // Уведомляем об изменении проекта
                ProjectChanged?.Invoke(this, data);

                _projectSession.MarkClean();
            }
            finally
            {
                // restoreScope.Dispose() clears the canonical guard even on exception.
            }
        }

        /// <summary>
        /// Сохранить текущие данные в модель проекта
        /// </summary>
        public ProjectData SaveCurrentProject()
        {
            var data = new ProjectData
            {
                Version = "1.1",
                ProjectNumber = ProjectNumber,
                ProjectObject = ProjectObject,
                IsOperatingMode = this.IsOperatingMode
            };

            // Сохраняем климатические данные из канонического ClimateState snapshot.
            // Совместимость с .smc форматом изолирована на границе persistence DTO <-> snapshot.
            var climateSnapshot = _projectSession.ClimateState.Snapshot;
            data.ClimateData = new ClimateProjectData
            {
                SelectedCity = climateSnapshot.SelectedCity,
                Region = climateSnapshot.SelectedRegion,
                AirTemperature = climateSnapshot.AirTemperature,
                WindSpeed = climateSnapshot.WindSpeed,
                Humidity = climateSnapshot.Humidity,
                SnowfallIntensity = climateSnapshot.SnowfallIntensity,
                SelectedZone = climateSnapshot.Zone,
                IsHighRequirements = climateSnapshot.IsHighRequirements
            };

            // DEC-006 (2026-09-03): каталоги живут только глобально — кастомные
            // материалы/шаблоны больше не встраиваются в сохраняемый проект,
            // import-less restore их и не читал.

            // Сохраняем данные конструкции из канонического ConstructionState snapshot.
            // Совместимость с .smc форматом изолирована на границе persistence DTO <-> snapshot.
            data.ConstructionData = ConstructionPersistenceMapper.ToProjectData(
                _projectSession.ConstructionState.Snapshot,
                _materialRepository);

            // Сохраняем данные теплового расчёта из канонического ThermalState
            // snapshot (Todo 10 / DEC-T08): save читает только каноническое
            // состояние проекта — никогда кэши ThermalViewModel или сервиса.
            // Точный wire-набор полей изолирован в ThermalPersistenceMapper.
            data.ThermalData = ThermalPersistenceMapper.BuildThermalProjectData(
                _projectSession.ThermalState.Snapshot);

            // Сохраняем данные гидравлики из канонического HydraulicsState snapshot.
            // BuildCanonicalSnapshot is represented by the canonical session snapshot;
            // HydraulicsPersistenceMapper remains the sole wire-format writer.
            data.HydraulicsData = HydraulicsPersistenceMapper.BuildHydraulicsProjectData(
                _projectSession.HydraulicsState.Snapshot);

            return data;
        }

        #endregion
    }

    /// <summary>
    /// Информация о коллекторе для отображения
    /// </summary>
    public partial class CollectorInfo : ObservableObject
    {
        private ResultsViewModel? _parent;

        /// <summary>
        /// Номер коллектора
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Отображаемое название
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Количество контуров
        /// </summary>
        public int CircuitCount { get; set; }

        /// <summary>
        /// Суммарный расход, м³/ч
        /// </summary>
        public double TotalFlowRate { get; set; }

        /// <summary>
        /// Признак выбранного коллектора
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Установить родительский ViewModel для обновления состояния
        /// </summary>
        public void SetParent(ResultsViewModel parent)
        {
            _parent = parent;
        }
    }

    /// <summary>
    /// Спецификация коллектора
    /// </summary>
    public class CollectorSpecification
    {
        /// <summary>
        /// Номер коллектора
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Тип коллектора
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Количество контуров
        /// </summary>
        public int CircuitCount { get; set; }

        /// <summary>
        /// Суммарная мощность, кВт
        /// </summary>
        public double TotalPower_kW { get; set; }

        /// <summary>
        /// Суммарный расход, м³/ч
        /// </summary>
        public double TotalFlowRate_m3h { get; set; }

        /// <summary>
        /// Потери давления, мбар
        /// </summary>
        public double PressureLoss_mbar { get; set; }

        /// <summary>
        /// Kv клапана
        /// </summary>
        public double Kv { get; set; }
    }

    /// <summary>
    /// Группированная строка оборудования коллектора для UI карточки Оборудование → Коллекторы
    /// </summary>
    public class CollectorEquipmentItem
    {
        /// <summary>
        /// Тип коллектора с количеством контуров (например "HKV-D (6 контуров)")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Тип балансировочного клапана, используемый как часть ключа группировки
        /// </summary>
        public ValveType ValveType { get; set; }

        /// <summary>
        /// Количество контуров в одном коллекторе группы (не количество коллекторов)
        /// </summary>
        public int CircuitCount { get; set; }

        /// <summary>
        /// Количество коллекторов в группе, шт
        /// </summary>
        public int CollectorQuantity { get; set; }
    }
}
