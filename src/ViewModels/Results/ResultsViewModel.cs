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
        private readonly IProjectStateService _projectStateService;
        private readonly IProjectSession _projectSession;
        private readonly IMarkDirtyService _markDirtyService;
        private readonly IDialogService _dialogService;
        private readonly IPdfExportService _pdfExportService;
        private readonly ICalculationReportExportService _calculationReportExportService;
        private readonly IProjectFileService _projectFileService;
        private readonly IProjectSaveService? _projectSaveService;
        private readonly IProjectDisplayModeState? _displayModeState;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionService _constructionService;
        private readonly ClimateViewModel _climateViewModel;
        private readonly ConstructionViewModel _constructionViewModel;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly CircuitsViewModel _circuitsViewModel;
        private readonly ProjectLoadOrchestrator _projectLoadOrchestrator;
        private readonly ResultsPdfDataBuilder _resultsPdfDataBuilder;
        private readonly HydraulicSummaryBuilder _hydraulicSummaryBuilder;
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
        /// Источник истины — <see cref="IProjectStateService"/>; свойство VM — pass-through
        /// с уведомлением UI и пометкой dirty при изменении вне сброса/загрузки проекта.
        /// </remarks>
        public string ProjectNumber
        {
            get => _projectStateService.ProjectNumber;
            set
            {
                if (_projectStateService.ProjectNumber == value) return;
                _projectStateService.ProjectNumber = value;
                OnPropertyChanged();
                if (_isResetting || _projectSession.IsLoadProjectInProgress) return;
                _markDirtyService.MarkDirty();
            }
        }

        /// <summary>
        /// Наименование объекта
        /// </summary>
        /// <remarks>
        /// Источник истины — <see cref="IProjectStateService"/>; свойство VM — pass-through
        /// с уведомлением UI и пометкой dirty при изменении вне сброса/загрузки проекта.
        /// </remarks>
        public string ProjectObject
        {
            get => _projectStateService.ProjectObject;
            set
            {
                if (_projectStateService.ProjectObject == value) return;
                _projectStateService.ProjectObject = value;
                OnPropertyChanged();
                if (_isResetting || _projectSession.IsLoadProjectInProgress) return;
                _markDirtyService.MarkDirty();
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
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentModeText))]
        [NotifyPropertyChangedFor(nameof(IsDesignMode))]
        [NotifyPropertyChangedFor(nameof(MaxPressureLoss))]
        private bool _isOperatingMode = true;

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
        /// ViewModel конструкции (для визуализации)
        /// </summary>
        public ConstructionViewModel ConstructionViewModel => _constructionViewModel;

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
        /// Заполняется через RebuildHydraulicSummaryCards() из _circuitsViewModel.Collectors.
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
            IProjectStateService projectStateService,
            IProjectSession projectSession,
            IMarkDirtyService markDirtyService,
            IDialogService dialogService,
            IPdfExportService pdfExportService,
            ICalculationReportExportService calculationReportExportService,
            IProjectFileService projectFileService,
            ICalculationStateService calculationStateService,
            IMaterialRepository materialRepository,
            IConstructionService constructionService,
            ClimateViewModel climateViewModel,
            ConstructionViewModel constructionViewModel,
            ThermalViewModel thermalViewModel,
            CircuitsViewModel circuitsViewModel,
            ProjectLoadOrchestrator projectLoadOrchestrator,
            ResultsPdfDataBuilder resultsPdfDataBuilder,
            HydraulicSummaryBuilder hydraulicSummaryBuilder,
            IProjectSaveService? projectSaveService = null,
            IProjectDisplayModeState? displayModeState = null)
        {
            _projectStateService = projectStateService ?? throw new ArgumentNullException(nameof(projectStateService));
            _projectSession = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
            _calculationReportExportService = calculationReportExportService ?? throw new ArgumentNullException(nameof(calculationReportExportService));
            _projectFileService = projectFileService ?? throw new ArgumentNullException(nameof(projectFileService));
            _projectSaveService = projectSaveService;
            _displayModeState = displayModeState;
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));
            _constructionViewModel = constructionViewModel ?? throw new ArgumentNullException(nameof(constructionViewModel));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _circuitsViewModel = circuitsViewModel ?? throw new ArgumentNullException(nameof(circuitsViewModel));
            _projectLoadOrchestrator = projectLoadOrchestrator ?? throw new ArgumentNullException(nameof(projectLoadOrchestrator));
            _resultsPdfDataBuilder = resultsPdfDataBuilder ?? throw new ArgumentNullException(nameof(resultsPdfDataBuilder));
            _hydraulicSummaryBuilder = hydraulicSummaryBuilder ?? throw new ArgumentNullException(nameof(hydraulicSummaryBuilder));
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

        partial void OnIsOperatingModeChanged(bool value)
        {
            if (_displayModeState is not null)
            {
                _displayModeState.IsOperatingMode = value;
            }
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
            if (string.IsNullOrEmpty(_projectStateService.CurrentFilePath))
            {
                await SaveProjectAs(cancellationToken);
                return;
            }

            await SaveToFile(_projectStateService.CurrentFilePath, cancellationToken);
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
                _projectStateService.CurrentFilePath = filePath;
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
            if (_projectStateService.IsDirty)
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
            _projectStateService.MarkClean();

            await LoadProjectDataAsync(data);
            _projectStateService.CurrentFilePath = filePath;
            _projectStateService.MarkClean();

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
                _projectStateService.MarkClean();
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
            SelectedCity = _climateViewModel.SelectedCity?.Name ?? string.Empty;
            DesignTemperature = _climateViewModel.AirTemperature;
            ClimateZone = _climateViewModel.SelectedZone;
            ColdPeriodDays = _climateViewModel.SelectedCity?.Period_0_Days ?? 150;
            WindSpeed = _climateViewModel.WindSpeed;
            SnowfallIntensity = _climateViewModel.SnowfallIntensity;
        }

        /// <summary>
        /// Загрузить данные конструкции
        /// </summary>
        private void LoadConstructionData()
        {
            R1 = _constructionViewModel.R1Total;
            R2 = _constructionViewModel.R2Total;
            LambdaE = _constructionViewModel.LambdaE;

            // Загружаем слои
            Layers.Clear();
            if (_constructionViewModel.LayersAbovePipe != null)
            {
                foreach (var layer in _constructionViewModel.LayersAbovePipe)
                {
                    Layers.Add(layer);
                }
            }
            if (_constructionViewModel.LayersBelowPipe != null)
            {
                foreach (var layer in _constructionViewModel.LayersBelowPipe)
                {
                    Layers.Add(layer);
                }
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

            var result = _thermalViewModel.Result;
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
            // Тип гликоля
            GlycolType = _circuitsViewModel.InputData?.GlycolType ?? GlycolType.Ethylene;

            // Концентрация гликоля с null-check
            GlycolConcentration = _circuitsViewModel.InputData?.GlycolConcentration ?? 50.0;

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
        /// Получить правильное склонение слова "контур"
        /// </summary>
        private static string GetContourWord(int count)
        {
            if (count % 100 >= 11 && count % 100 <= 19)
                return "контуров";
            int lastDigit = count % 10;
            return lastDigit switch
            {
                1 => "контур",
                2 or 3 or 4 => "контура",
                _ => "контуров"
            };
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
        private void RecalculateKpi()
        {
            CalculateTotalPower();
            CalculateSystemVolume();
            CalculatePumpParameters();
            CalculateExpansionTank();
            UpdateCollectorSpecifications();
            UpdateCollectorEquipmentItems();
        }

        /// <summary>
        /// Рассчитать суммарную тепловую мощность
        /// </summary>
        private void CalculateTotalPower()
        {
            // Суммируем мощности коллекторов; при пустом списке (или null)
            // итерация просто не выполняется, и итог корректно обнуляется.
            // Ранний return «оставляем текущее значение» убираем: иначе после
            // RefreshAll() с пустыми коллекторами остаётся stale-значение
            // из предыдущего проекта, а должно быть 0.
            double totalPower_W = 0;

            if (_circuitsViewModel.Collectors != null)
            {
                foreach (var collector in _circuitsViewModel.Collectors)
                {
                    if (collector?.Summary != null)
                    {
                        totalPower_W += collector.Summary.TotalPower;
                    }
                }
            }

            TotalThermalPower_kW = totalPower_W / 1000.0;
        }

        /// <summary>
        /// Рассчитать объём системы
        /// </summary>
        private void CalculateSystemVolume()
        {
            double totalLength = 0;
            double innerDiameter_m = 0;

            // Получаем внутренний диаметр трубы из канонического ThermalState
            // snapshot (Todo 10 / DEC-T07), а не из кэша адаптера.
            var canonicalPipe = _projectSession.ThermalState.Snapshot.Inputs.Pipe;
            if (canonicalPipe != null)
            {
                innerDiameter_m = canonicalPipe.InnerDiameter / 1000.0; // мм → м
            }

            // Суммируем длины всех контуров
            if (_circuitsViewModel.Collectors != null)
            {
                foreach (var collector in _circuitsViewModel.Collectors)
                {
                    if (collector?.Circuits == null) continue;
                    foreach (var circuit in collector.Circuits)
                    {
                        totalLength += circuit.TotalLength;
                    }
                }
            }

            // V = π × d²/4 × L × 1000 (литры)
            if (innerDiameter_m > 0)
            {
                SystemVolume_L = Math.PI * Math.Pow(innerDiameter_m, 2) / 4.0 * totalLength * 1000.0;
            }
            else
            {
                SystemVolume_L = 0;
            }

            TotalPipeLength = totalLength;
        }

        /// <summary>
        /// Рассчитать параметры насоса
        /// </summary>
        private void CalculatePumpParameters()
        {
            double totalFlowRate_Lh = 0;
            double maxPressureLoss_Pa = 0;

            if (_circuitsViewModel.Collectors == null)
            {
                PumpFlowRate_m3h = 0;
                PumpQ = 0;
                PumpHead_kPa = 0;
                PumpH = 0;
                return;
            }

            foreach (var collector in _circuitsViewModel.Collectors)
            {
                if (collector?.Summary != null)
                {
                    totalFlowRate_Lh += collector.Summary.TotalFlowRate;

                    // Максимальные потери в зависимости от режима
                    double pressureLoss = IsOperatingMode
                        ? collector.Summary.PressureLoss_Operating_Pa
                        : collector.Summary.PressureLoss_Cold_Pa;

                    if (pressureLoss > maxPressureLoss_Pa)
                    {
                        maxPressureLoss_Pa = pressureLoss;
                    }
                }
            }

            // Переводим расход в м³/ч
            PumpFlowRate_m3h = totalFlowRate_Lh / 1000.0;
            PumpQ = PumpFlowRate_m3h;

            // Переводим напор в кПа
            PumpHead_kPa = maxPressureLoss_Pa / 1000.0;
            PumpH = PumpHead_kPa;
        }

        /// <summary>
        /// Обновить напор насоса при переключении режима
        /// </summary>
        private void UpdatePumpHead()
        {
            double maxPressureLoss_Pa = 0;

            if (_circuitsViewModel.Collectors == null)
            {
                PumpHead_kPa = 0;
                PumpH = 0;
                return;
            }

            foreach (var collector in _circuitsViewModel.Collectors)
            {
                if (collector?.Summary != null)
                {
                    double pressureLoss = IsOperatingMode
                        ? collector.Summary.PressureLoss_Operating_Pa
                        : collector.Summary.PressureLoss_Cold_Pa;

                    if (pressureLoss > maxPressureLoss_Pa)
                    {
                        maxPressureLoss_Pa = pressureLoss;
                    }
                }
            }

            PumpHead_kPa = maxPressureLoss_Pa / 1000.0;
            PumpH = PumpHead_kPa;
        }

        /// <summary>
        /// Рассчитать объём расширительного бака
        /// Формула: V_системы × β × 1.2
        /// </summary>
        private void CalculateExpansionTank()
        {
            // Коэффициент расширения воды (примерно 0.034 при 80°C)
            double beta = 0.034;

            // Коэффициент запаса 1.2
            ExpansionTankVolume_L = SystemVolume_L * beta * 1.2;
            ExpansionTankV = ExpansionTankVolume_L;
        }

        /// <summary>
        /// Обновить список коллекторов
        /// </summary>
        private void UpdateCollectorsList()
        {
            var previousIndex = SelectedCollectorIndex; // Сохраняем текущий выбор

            Collectors.Clear();

            if (_circuitsViewModel.Collectors == null)
            {
                RzsCount = 0;
                SelectedCollectorIndex = -1;
                return;
            }

            for (int i = 0; i < _circuitsViewModel.Collectors.Count; i++)
            {
                var collectorData = _circuitsViewModel.Collectors[i];
                if (collectorData == null) continue;

                var collectorInfo = new CollectorInfo
                {
                    Number = collectorData.CollectorNumber,
                    DisplayName = $"Коллектор №{collectorData.CollectorNumber} ({collectorData.Circuits?.Count ?? 0} {GetContourWord(collectorData.Circuits?.Count ?? 0)})",
                    CircuitCount = collectorData.Circuits?.Count ?? 0,
                    TotalFlowRate = collectorData.Summary?.TotalFlowRate_m3h ?? 0,
                    IsSelected = (i == 0) // Первый коллектор выбран по умолчанию
                };
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
        private void UpdateCollectorSummary()
        {
            // Проверяем валидность индекса
            if (_circuitsViewModel.SelectedCollectorIndex < 0 ||
                _circuitsViewModel.Collectors == null ||
                _circuitsViewModel.SelectedCollectorIndex >= _circuitsViewModel.Collectors.Count)
            {
                CollectorSummary = null;
                return;
            }

            var collector = _circuitsViewModel.SelectedCollector;
            CollectorSummary = collector?.Summary;
        }

        /// <summary>
        /// Обновить фильтр контуров по режиму
        /// </summary>
        private void UpdateCircuitsFilter()
        {
            Circuits.Clear();

            // Получаем выбранный коллектор по индексу
            if (SelectedCollectorIndex < 0 || SelectedCollectorIndex >= Collectors.Count)
                return;

            var collectorData = _circuitsViewModel.Collectors?[SelectedCollectorIndex];
            if (collectorData?.Circuits == null) return;

            foreach (var circuit in collectorData.Circuits)
            {
                if (circuit == null) continue;
                // Устанавливаем режим отображения
                circuit.DisplayMode = IsOperatingMode
                    ? HydraulicMode.OperatingTemperature
                    : HydraulicMode.DesignTemperature;

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
                _circuitsViewModel.Collectors, IsOperatingMode))
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
                _circuitsViewModel.Collectors))
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
                _circuitsViewModel.Collectors))
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

            // Проверка климатических данных
            if (_climateViewModel.SelectedCity == null)
            {
                MissingModules.Add("Климат - не выбран город");
            }

            // Проверка конструкции
            if (!_constructionViewModel.IsValid)
            {
                MissingModules.Add("Конструкция - невалидные данные");
            }

            // Проверка теплового расчёта
            if (_thermalViewModel.Result == null || !_thermalViewModel.Result.IsValid)
            {
                MissingModules.Add("Тепловой расчёт - нет результата");
            }
            else if (_thermalViewModel.SelectedPipe == null)
            {
                MissingModules.Add("Тепловой расчёт - не выбрана труба");
            }

            // Проверка гидравлического расчёта
            bool hasValidCircuits = false;
            if (_circuitsViewModel.Collectors != null)
            {
                foreach (var collector in _circuitsViewModel.Collectors)
                {
                    if (collector?.Circuits != null && collector.Circuits.Any(c => c.CircuitLength > 0))
                    {
                        hasValidCircuits = true;
                        break;
                    }
                }
            }

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
                _projectStateService.CurrentFilePath = null;
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
                // Очищаем карточки, а не перестраиваем: в момент Reset() _circuitsViewModel.Collectors
                // ещё не сброшен (в PerformNewCalculationReset и ApplyLoadedProjectAsync
                // CircuitsViewModel.Reset() вызывается позже), иначе RebuildHydraulicSummaryCards()
                // оставит stale-снимки из предыдущего проекта. Карточки пересоберутся при следующем
                // LoadHydraulicsDataOnNavigate() / RefreshAll().
                HydraulicSummaryCards.Clear();
                SelectedCollectorIndex = 0;
                CollectorSummary = null;
                MissingModules.Clear();
                IsDataReady = false;

                _projectStateService.MarkClean();
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
                // Свойства VM — pass-through к IProjectStateService (этап C4):
                // присвоение обновляет и сервис; dirty не ставится — активен guard загрузки.
                ProjectNumber = data.ProjectNumber;
                ProjectObject = data.ProjectObject;

                // Восстанавливаем состояние модулей (климат, конструкция,
                // тепловой расчёт, гидравлика) — оркестрация вынесена в
                // ProjectLoadOrchestrator (этап C1). Файл — источник истины.
                await _projectLoadOrchestrator.RestoreModulesFromProjectAsync(data);

                // Единственное обновление снимка Results — ПОСЛЕ финального теплового
                // результата, чтобы KPI не оставались снимком, снятым до расчёта.
                RefreshAll();

                // Уведомляем об изменении проекта
                ProjectChanged?.Invoke(this, data);

                _projectStateService.MarkClean();
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

            // Сохраняем пользовательские материалы
            data.CustomMaterials = _materialRepository.GetAllMaterials()
                .Where(m => !m.IsBuiltIn)
                .Select(MaterialSnapshot.FromMaterial)
                .ToList();

            // Сохраняем пользовательские шаблоны конструкций с полными снимками материалов
            var allMaterials = _materialRepository.GetAllMaterials().ToList();
            data.CustomTemplates = _constructionViewModel.Templates
                .Where(t => !t.IsBuiltIn)
                .Select(t => new ConstructionTemplate
                {
                    Name = t.Name,
                    Description = t.Description,
                    HasLoads = t.HasLoads,
                    DefaultGroundwaterLevel = t.DefaultGroundwaterLevel,
                    IsBuiltIn = false,
                    LayersAbovePipe = t.LayersAbovePipe
                        .Select(l => new LayerTemplate
                        {
                            MaterialId = l.MaterialId,
                            Thickness = l.Thickness,
                            Position = l.Position,
                            Order = l.Order
                        })
                        .ToList(),
                    LayersBelowPipe = t.LayersBelowPipe
                        .Select(l => new LayerTemplate
                        {
                            MaterialId = l.MaterialId,
                            Thickness = l.Thickness,
                            Position = l.Position,
                            Order = l.Order
                        })
                        .ToList(),
                    MaterialSnapshots = t.LayersAbovePipe
                        .Concat(t.LayersBelowPipe)
                        .Select(l => l.MaterialId)
                        .Distinct()
                        .Select(id => allMaterials.FirstOrDefault(m => m.Id == id))
                        .Where(m => m != null)
                        .Select(m => MaterialSnapshot.FromMaterial(m!))
                        .ToList()
                })
                .ToList();

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

        /// <summary>
        /// Проверить наличие несохранённых данных
        /// </summary>
        [Obsolete("Use IProjectStateService.IsDirty instead.")]
        private bool HasUnsavedData()
        {
            // Проверяем, есть ли данные для сохранения
            return !string.IsNullOrEmpty(ProjectNumber) ||
                   !string.IsNullOrEmpty(ProjectObject) ||
                   _climateViewModel.SelectedCity != null ||
                   _thermalViewModel.SelectedPipe != null ||
                   _circuitsViewModel.Collectors.Any(c => c.Circuits.Any());
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
