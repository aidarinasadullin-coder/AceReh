using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.ViewModels.Results
{
    /// <summary>
    /// ViewModel для экрана результатов расчёта
    /// </summary>
    public partial class ResultsViewModel : ObservableObject
    {
        private readonly IProjectStateService _projectStateService;
        private readonly IMarkDirtyService _markDirtyService;
        private readonly IDialogService _dialogService;
        private readonly IPdfExportService _pdfExportService;
        private readonly IProjectFileService _projectFileService;
        private readonly IConstructionVisualizationImageService _constructionVisualizationImageService;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionService _constructionService;
        private readonly CalculationContext _calculationContext;
        private readonly ClimateViewModel _climateViewModel;
        private readonly ConstructionViewModel _constructionViewModel;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly CircuitsViewModel _circuitsViewModel;

        /// <summary>
        /// Текущий путь к файлу проекта
        /// </summary>
        private string? _currentFilePath;
        private bool _isResetting;

        #region Observable Properties

        // ============================================
        // Блок 0 - Информация о проекте
        // ============================================

        /// <summary>
        /// Номер проекта
        /// </summary>
        [ObservableProperty]
        private string _projectNumber = string.Empty;

        /// <summary>
        /// Наименование объекта
        /// </summary>
        [ObservableProperty]
        private string _projectObject = string.Empty;

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
            IMarkDirtyService markDirtyService,
            IDialogService dialogService,
            IPdfExportService pdfExportService,
            IProjectFileService projectFileService,
            IConstructionVisualizationImageService constructionVisualizationImageService,
            ICalculationStateService calculationStateService,
            IMaterialRepository materialRepository,
            IConstructionService constructionService,
            CalculationContext calculationContext,
            ClimateViewModel climateViewModel,
            ConstructionViewModel constructionViewModel,
            ThermalViewModel thermalViewModel,
            CircuitsViewModel circuitsViewModel)
        {
            _projectStateService = projectStateService ?? throw new ArgumentNullException(nameof(projectStateService));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
            _projectFileService = projectFileService ?? throw new ArgumentNullException(nameof(projectFileService));
            _constructionVisualizationImageService = constructionVisualizationImageService ?? throw new ArgumentNullException(nameof(constructionVisualizationImageService));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));
            _constructionViewModel = constructionViewModel ?? throw new ArgumentNullException(nameof(constructionViewModel));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _circuitsViewModel = circuitsViewModel ?? throw new ArgumentNullException(nameof(circuitsViewModel));

            // Загружаем начальные данные
            LoadProjectInfo();
            LoadClimateData();
            LoadConstructionData();
            LoadThermalData();

            // Проверяем готовность данных
            CheckDataReadiness();

            // Подписываемся на изменения свойств проекта
            PropertyChanged += OnProjectPropertyChanged;
        }

        /// <summary>
        /// Обработчик изменения свойств проекта для сохранения номера и объекта
        /// </summary>
        private void OnProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isResetting) return;
            if (_calculationStateService.IsLoadProjectInProgress) return;

            if (e.PropertyName == nameof(ProjectNumber))
            {
                _projectStateService.ProjectNumber = ProjectNumber;
                _markDirtyService.MarkDirty();
            }
            else if (e.PropertyName == nameof(ProjectObject))
            {
                _projectStateService.ProjectObject = ProjectObject;
                _markDirtyService.MarkDirty();
            }
        }

        /// <summary>
        /// Загрузить данные гидравлики (вызывается при переходе на вкладку)
        /// </summary>
        public void LoadHydraulicsDataOnNavigate()
        {
            // Обновляем климатические данные (могли измениться)
            LoadClimateData();

            // Обновляем данные конструкции и теплового расчёта
            LoadConstructionData();
            LoadThermalData();

            // Обновляем гидравлические данные
            LoadHydraulicsData();
            RecalculateKpi();

            // Проверяем готовность данных после загрузки всех модулей
            CheckDataReadiness();
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
            if (!IsDataReady)
            {
                StatusMessage = "Невозможно экспортировать: не все данные готовы";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF файлы (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = $"Результаты_{ProjectNumber}_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            try
            {
                StatusMessage = "Экспорт в PDF...";
                var pdfData = BuildResultsPdfData();
                var success = await _pdfExportService.ExportResultsToPdfAsync(saveFileDialog.FileName, pdfData);

                if (success)
                {
                    StatusMessage = $"PDF сохранён: {Path.GetFileName(saveFileDialog.FileName)}";
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
        /// Собрать данные для PDF экспорта
        /// </summary>
        private ResultsPdfData BuildResultsPdfData()
        {
            var pdfData = new ResultsPdfData
            {
                // Информация о проекте
                ProjectNumber = ProjectNumber,
                ProjectObject = ProjectObject,
                ReportDate = DateTime.Now,

                // KPI
                TotalThermalPower_kW = TotalThermalPower_kW,
                SystemVolume_L = SystemVolume_L,
                PumpFlowRate_m3h = PumpFlowRate_m3h,
                PumpHead_kPa = PumpHead_kPa,
                ExpansionTankVolume_L = ExpansionTankVolume_L,

                // Температуры
                SupplyTemperature = SupplyTemperature,
                ReturnTemperature = ReturnTemperature,
                OperatingTemperature = OperatingTemperature,
                GroundTemperature = GroundTemperature,
                SurfaceTemperature = SurfaceTemperature,

                // Климат
                City = SelectedCity,
                DesignTemperature = DesignTemperature,
                WindSpeed = WindSpeed,
                SnowfallIntensity = SnowfallIntensity,
                ClimateZone = ClimateZone,
                ColdPeriodDays = ColdPeriodDays,

                // Труба
                PipeType = PipeType,
                PipeSpacing = PipeSpacing,

                // Режим и теплоноситель
                OperatingMode = OperatingMode,
                GlycolType = GlycolType,
                GlycolConcentration = GlycolConcentration,

                // Конструкция
                R1 = R1,
                R2 = R2,
                LambdaE = LambdaE,
                PowerUp = PowerUp,
                PowerDown = PowerDown,

                // Оборудование
                TotalPipeLength = TotalPipeLength,
                RzsCount = RzsCount
            };

            // Слои конструкции
            foreach (var layer in Layers)
            {
                pdfData.Layers.Add(new LayerPdfData
                {
                    MaterialName = layer.Material?.Name ?? "Не указан",
                    Thickness = layer.Thickness,
                    Lambda = layer.CalculatedLambda,
                    R = layer.CalculatedR,
                    Position = layer.Position == LayerPosition.AbovePipe ? "Над трубой" : "Под трубой"
                });
            }

            // Изображение схемы конструкции для PDF
            pdfData.ConstructionImageBytes = _constructionVisualizationImageService.GenerateImage(
                new ConstructionVisualizationParameters
                {
                    LayersAbovePipe = _constructionViewModel.LayersAbovePipe,
                    LayersBelowPipe = _constructionViewModel.LayersBelowPipe,
                    PipeSpacing = _calculationStateService.PipeSpacing,
                    CompactMode = true,
                    ShowDimensionLine = true,
                    FixedScaleFactor = 0.25
                },
                width: 400,
                height: 300);

            // Коллекторы и контуры
            if (_circuitsViewModel.Collectors != null)
            {
                foreach (var collector in _circuitsViewModel.Collectors)
                {
                    if (collector == null) continue;

                    var collectorPdf = new CollectorPdfData
                    {
                        Number = collector.CollectorNumber,
                        Type = collector.CollectorTypeDisplayWithCount,
                        Summary = new CollectorSummaryPdfData
                        {
                            CircuitCount = collector.Circuits?.Count ?? 0,
                            TotalPipeLength = collector.Summary?.TotalPipeLength ?? 0,
                            TotalPower = collector.Summary?.TotalPower ?? 0,
                            TotalFlowRate = collector.Summary?.TotalFlowRate ?? 0,
                            PressureLoss_Operating_kPa = (collector.Summary?.PressureLoss_Operating_Pa ?? 0) / 1000.0,
                            PressureLoss_Cold_kPa = (collector.Summary?.PressureLoss_Cold_Pa ?? 0) / 1000.0,
                            Kv = collector.Summary?.Kv ?? 1.2,
                            CollectorType = collector.Summary?.CollectorType ?? "HKV-D"
                        }
                    };

                    // Контуры коллектора
                    if (collector.Circuits != null)
                    {
                        foreach (var circuit in collector.Circuits)
                        {
                            if (circuit == null) continue;

                            // Используем данные для рабочего режима
                            var result = circuit.OperatingResult;

                            // Расчёт удельных потерь (Па/м)
                            double pressureLossPerMeter = 0;
                            if (result?.DpRohr > 0 && circuit.TotalLength > 0)
                            {
                                pressureLossPerMeter = result.DpRohr / circuit.TotalLength;
                            }

                            collectorPdf.Circuits.Add(new CircuitPdfData
                            {
                                CircuitNumber = circuit.CircuitNumber,
                                Length = circuit.TotalLength,
                                Area = circuit.CircuitArea,
                                Power = circuit.Power,
                                FlowRate = circuit.FlowRate,
                                Velocity = circuit.Velocity,
                                FlowRegime = circuit.FlowRegimeDescription,
                                PressureLossPerMeter = pressureLossPerMeter,
                                DpRohr = (result?.DpRohr ?? 0) / 1000.0,        // кПа
                                DpVerteiler = (result?.DpVerteiler ?? 0) / 1000.0, // кПа
                                DpVent = (result?.DpVent ?? 0) / 1000.0,          // кПа
                                DpGesamt = (result?.DpGesamt ?? 0) / 1000.0,      // кПа
                                Throttling = circuit.Throttling / 1000.0,         // кПа
                                ZuDrosseln = (circuit.OperatingResult?.ZuDrosseln ?? 0) / 1000.0, // кПа
                                ValveTurns = circuit.ValveTurns
                            });
                        }
                    }

                    pdfData.Collectors.Add(collectorPdf);
                }
            }

            // Спецификации коллекторов
            foreach (var spec in CollectorSpecifications)
            {
                pdfData.CollectorSpecifications.Add(new CollectorSpecPdfData
                {
                    Number = spec.Number,
                    Type = spec.Type,
                    CircuitCount = spec.CircuitCount,
                    TotalPower_kW = spec.TotalPower_kW,
                    TotalFlowRate_m3h = spec.TotalFlowRate_m3h,
                    PressureLoss_mbar = spec.PressureLoss_mbar,
                    Kv = spec.Kv
                });
            }

            return pdfData;
        }

        /// <summary>
        /// Команда экспорта в Excel (заглушка)
        /// </summary>
        [RelayCommand]
        private async Task ExportExcel()
        {
            // TODO: Реализовать экспорт в Excel
            StatusMessage = "Экспорт в Excel будет реализован в следующей версии";
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// Команда сохранения проекта
        /// </summary>
        [RelayCommand]
        private async Task SaveProject()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveProjectAs();
                return;
            }

            await SaveToFile(_currentFilePath);
        }

        /// <summary>
        /// Команда сохранения проекта с выбором пути
        /// </summary>
        [RelayCommand]
        private async Task SaveProjectAs()
        {
            var defaultFileName = $"{ProjectNumber}_{DateTime.Now:yyyyMMdd}";
            var filePath = _dialogService.ShowSaveFileDialog(defaultFileName);

            if (string.IsNullOrEmpty(filePath))
                return;

            if (await SaveToFile(filePath))
            {
                _currentFilePath = filePath;
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

            var result = await _projectFileService.LoadProjectResultAsync(filePath);
            if (!result.IsSuccess || result.Value == null)
            {
                _dialogService.ShowError($"Не удалось открыть проект: {result.Error}", "Ошибка");
                return;
            }

            var data = result.Value;

            // Подтверждение загрузки, если есть несохранённые данные
            if (_projectStateService.IsDirty)
            {
                var confirmation = _dialogService.Show(
                    "Текущий проект будет заменён. Продолжить?",
                    "Открытие проекта",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;
            }

            // Сброс всех модулей перед загрузкой нового проекта,
            // чтобы избежать "залипания" старых результатов и ошибок.
            Reset();
            _calculationContext.Reset();
            _climateViewModel.Reset();
            _constructionViewModel.Reset();
            _thermalViewModel.Reset();
            _circuitsViewModel.Reset();
            _projectStateService.MarkClean();

            await LoadProjectDataAsync(data);
            _currentFilePath = filePath;
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
                var pdfData = BuildResultsPdfData();
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
                var pdfData = BuildResultsPdfData();
                var tempPath = _projectFileService.GetPreviewPdfPath();

                var success = await _pdfExportService.ExportResultsToPdfAsync(tempPath, pdfData);

                if (success)
                {
                    // Используем диалог печати Windows
                    var printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
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
        private async Task<bool> SaveToFile(string filePath)
        {
            try
            {
                StatusMessage = "Сохранение проекта...";
                var data = SaveCurrentProject();
                data.ModifiedDate = DateTime.Now;
                if (data.CreatedDate == default)
                {
                    data.CreatedDate = DateTime.Now;
                }

                var result = await _projectFileService.SaveProjectResultAsync(filePath, data);
                if (!result.IsSuccess)
                {
                    _dialogService.ShowError($"Не удалось сохранить проект: {result.Error}", "Ошибка");
                    return false;
                }

                StatusMessage = $"Проект сохранён: {Path.GetFileName(filePath)}";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
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

        #endregion

        #region Data Loading Methods

        /// <summary>
        /// Загрузить информацию о проекте из сервиса
        /// </summary>
        private void LoadProjectInfo()
        {
            ProjectNumber = _projectStateService.ProjectNumber;
            ProjectObject = _projectStateService.ProjectObject;
        }

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
            PipeType = _thermalViewModel.SelectedPipe?.Name ?? string.Empty;
            PipeSpacing = _calculationStateService.PipeSpacing;
            OperatingMode = _thermalViewModel.SelectedMode;
            GroundTemperature = _thermalViewModel.GroundTemperature;

            // Surface temperature from mode: +3, +5, +7
            SurfaceTemperature = (int)_thermalViewModel.SelectedMode;

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
        }

        /// <summary>
        /// Рассчитать суммарную тепловую мощность
        /// </summary>
        private void CalculateTotalPower()
        {
            // Если нет коллекторов, оставляем текущее значение (заглушка)
            if (_circuitsViewModel.Collectors == null || _circuitsViewModel.Collectors.Count == 0)
            {
                return;
            }

            double totalPower_W = 0;

            foreach (var collector in _circuitsViewModel.Collectors)
            {
                if (collector?.Summary != null)
                {
                    totalPower_W += collector.Summary.TotalPower;
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

            // Получаем внутренний диаметр трубы
            var selectedPipe = _thermalViewModel.SelectedPipe;
            if (selectedPipe != null)
            {
                innerDiameter_m = selectedPipe.InnerDiameter / 1000.0; // мм → м
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

            if (_circuitsViewModel.Collectors == null) return;

            foreach (var collector in _circuitsViewModel.Collectors)
            {
                if (collector?.Summary == null) continue;

                CollectorSpecifications.Add(new CollectorSpecification
                {
                    Number = collector.CollectorNumber,
                    Type = collector.CollectorTypeDisplayWithCount,
                    CircuitCount = collector.Circuits?.Count ?? 0,
                    TotalPower_kW = collector.Summary.TotalPower / 1000.0,
                    TotalFlowRate_m3h = collector.Summary.TotalFlowRate_m3h,
                    PressureLoss_mbar = IsOperatingMode
                        ? collector.Summary.PressureLoss_Operating_mbar
                        : collector.Summary.PressureLoss_Cold_mbar,
                    Kv = collector.Summary.Kv
                });
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
            LoadProjectInfo();
            LoadClimateData();
            LoadConstructionData();
            LoadThermalData();
            LoadHydraulicsData();
            CheckDataReadiness();
            RecalculateKpi();
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
                _currentFilePath = null;
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

            _calculationStateService.IsLoadProjectInProgress = true;

            try
            {
                // Восстанавливаем режим отображения
                IsOperatingMode = data.IsOperatingMode;

                // Загружаем информацию о проекте
                ProjectNumber = data.ProjectNumber;
                ProjectObject = data.ProjectObject;
                _projectStateService.ProjectNumber = data.ProjectNumber;
                _projectStateService.ProjectObject = data.ProjectObject;

                // Загружаем климатические данные и коллекторы под guard загрузки проекта,
                // чтобы восстановление города не перезаписало сохранённые пользовательские параметры.
                _climateViewModel.BeginLoadProject();
                try
                {
                    _climateViewModel.SelectedCity = null;
                    var city = _climateViewModel.FindCityByName(data.ClimateData.SelectedCity);
                    _climateViewModel.SearchQuery = data.ClimateData.SelectedCity;
                    _climateViewModel.AirTemperature = data.ClimateData.AirTemperature;
                    _climateViewModel.WindSpeed = data.ClimateData.WindSpeed;
                    _climateViewModel.Humidity = data.ClimateData.Humidity;
                    _climateViewModel.SnowfallIntensity = data.ClimateData.SnowfallIntensity;
                    _climateViewModel.SelectedZone = data.ClimateData.SelectedZone;
                    _climateViewModel.IsHighRequirements = data.ClimateData.IsHighRequirements;
                    _climateViewModel.SelectedCity = city;

                    // Импортируем пользовательские материалы проекта перед загрузкой слоёв
                    if (data.CustomMaterials.Any())
                    {
                        await _constructionService.ImportProjectMaterialsAsync(data.CustomMaterials);
                        await _constructionViewModel.ReloadMaterialsAsync();
                    }

                    // Импортируем пользовательские шаблоны конструкций проекта
                    if (data.CustomTemplates.Any())
                    {
                        await _constructionService.ImportProjectTemplatesAsync(data.CustomTemplates);
                        await _constructionViewModel.ReloadMaterialsAsync();
                    }

                    // Загружаем данные конструкции
                    // Сначала восстанавливаем УГВ и признак нагрузок, чтобы UpdateLambda при загрузке слоёв
                    // использовал корректный уровень грунтовых вод (λБ при УГВ < 1 м, λА при УГВ >= 1 м).
                    _constructionViewModel.GroundwaterLevel = data.ConstructionData.GroundwaterLevel;
                    _constructionViewModel.HasLoads = data.ConstructionData.HasLoads;
                    if (data.ConstructionData.Layers.Any())
                    {
                        LoadLayersFromProjectData(data.ConstructionData.Layers, data.Version);
                    }

                    // Загружаем данные теплового расчёта
                    _thermalViewModel.SelectedMode = data.ThermalData.SelectedMode;
                    _thermalViewModel.SupplyTemperature = data.ThermalData.SupplyTemperature;
                    _thermalViewModel.GroundTemperature = data.ThermalData.GroundTemperature;
                    _calculationStateService.SetPipeSpacing(data.ThermalData.PipeSpacing, "ResultsViewModel.LoadProject");

                    // Восстанавливаем выбранную трубу
                    var restoredPipe = data.ThermalData.SelectedPipe;
                    if (restoredPipe != null)
                    {
                        var restoredPipeType = new PipeType
                        {
                            Name = restoredPipe.Name,
                            OuterDiameter = restoredPipe.OuterDiameter,
                            InnerDiameter = restoredPipe.InnerDiameter,
                            WallThickness = restoredPipe.WallThickness
                        };
                        _thermalViewModel.SelectedPipe = _thermalViewModel.AvailablePipes
                            .FirstOrDefault(p => p == restoredPipeType)
                            ?? _thermalViewModel.AvailablePipes.FirstOrDefault();
                    }

                    // Восстанавливаем результат теплового расчёта
                    if (data.ThermalData.Result != null)
                    {
                        _thermalViewModel.Result = new ThermalCalculationResult
                        {
                            PowerUp = data.ThermalData.Result.PowerUp,
                            PowerDown = data.ThermalData.Result.PowerDown,
                            PowerTotal = data.ThermalData.Result.PowerTotal,
                            SupplyTemperature = data.ThermalData.Result.SupplyTemperature,
                            ReturnTemperature = data.ThermalData.Result.ReturnTemperature,
                            MeanTemperature = data.ThermalData.Result.MeanTemperature,
                            DeltaT = data.ThermalData.Result.DeltaT,
                            IsValid = data.ThermalData.Result.IsValid
                        };
                    }

                    // Загружаем коллекторы
                    _circuitsViewModel.Collectors.Clear();
                    foreach (var collectorData in data.HydraulicsData.Collectors)
                    {
                        var collector = new CollectorData(collectorData.CollectorNumber)
                        {
                            CollectorType = collectorData.CollectorType,
                            ValveType = collectorData.ValveType
                        };

                        foreach (var circuitData in collectorData.Circuits)
                        {
                            collector.Circuits.Add(new CircuitRow
                            {
                                CircuitNumber = circuitData.CircuitNumber,
                                CircuitLength = circuitData.CircuitLength,
                                SupplyLength = circuitData.SupplyLength,
                                SupplySpacing_cm = circuitData.SupplySpacingCm,
                                SupplyHeatPercent = circuitData.SupplyHeatPercent,
                                PipeSpacing_cm = circuitData.PipeSpacingCm
                            });
                        }

                        _circuitsViewModel.Collectors.Add(collector);
                    }

                    // Загружаем данные гидравлики после восстановления коллекторов,
                    // чтобы присвоения InputData не пометили проект dirty до завершения загрузки.
                    _circuitsViewModel.InputData.GlycolType = data.HydraulicsData.GlycolType;
                    _circuitsViewModel.InputData.GlycolConcentration = data.HydraulicsData.GlycolConcentration;
                    _circuitsViewModel.InputData.SupplySpacing_cm = data.HydraulicsData.SupplySpacingCm;
                    _circuitsViewModel.InputData.SupplyHeatPercent = data.HydraulicsData.SupplyHeatPercent;

                    // Выбираем первый загруженный коллектор и обновляем состояние команд
                    if (_circuitsViewModel.Collectors.Count > 0)
                    {
                        _circuitsViewModel.SelectedCollectorIndex = 0;
                    }
                    _circuitsViewModel.AddCircuitCommand.NotifyCanExecuteChanged();
                    _circuitsViewModel.RemoveCircuitCommand.NotifyCanExecuteChanged();
                }
                finally
                {
                    _climateViewModel.EndLoadProject();
                    // После загрузки проекта явно синхронизируем singleton IClimateData
                    // с параметрами, восстановленными из файла. Иначе ThermalCalculator
                    // будет считать по старым/нулевым климатическим данным.
                    _climateViewModel.SyncToClimateData();
                }

                // Обновляем все данные
                RefreshAll();

                // Canonical writer thermal: ThermalViewModel публикует в контекст.
                // CircuitsViewModel — чистый потребитель; Calculate срабатывает через OnCalculationContextChanged.
                if (_thermalViewModel.Result != null)
                {
                    _thermalViewModel.LoadResult(_thermalViewModel.Result);
                    // Если invalid, OnCalculationContextChanged сделает Notify-only без Calculate.
                    // Если валидный, Calculate сработает автоматически — ручной вызов ниже не нужен.
                }

                // Восстанавливаем результаты контуров из сохранённых данных
                RestoreCircuitsResults(data.HydraulicsData.Collectors);

                // Уведомляем об изменении проекта
                ProjectChanged?.Invoke(this, data);

                _projectStateService.MarkClean();
            }
            finally
            {
                _calculationStateService.IsLoadProjectInProgress = false;
            }
        }

        /// <summary>
        /// Восстанавливает результаты контуров из сохранённых данных проекта
        /// </summary>
        private void RestoreCircuitsResults(List<CollectorProjectData> collectorsData)
        {
            if (collectorsData == null || _circuitsViewModel.Collectors == null) return;

            for (int i = 0; i < collectorsData.Count && i < _circuitsViewModel.Collectors.Count; i++)
            {
                var collectorData = collectorsData[i];
                var collector = _circuitsViewModel.Collectors[i];

                // Восстанавливаем Summary
                if (collectorData.Summary != null && collector.Summary != null)
                {
                    collector.Summary.TotalPower = collectorData.Summary.TotalPower;
                    collector.Summary.TotalFlowRate = collectorData.Summary.TotalFlowRate;
                    collector.Summary.TotalPipeLength = collectorData.Summary.TotalPipeLength;
                    collector.Summary.PressureLoss_Operating_Pa = collectorData.Summary.PressureLoss_Operating_Pa;
                    collector.Summary.PressureLoss_Cold_Pa = collectorData.Summary.PressureLoss_Cold_Pa;
                    collector.Summary.Kv = collectorData.Summary.Kv;
                    collector.Summary.CollectorType = collectorData.Summary.CollectorType;
                }

                // Восстанавливаем результаты контуров
                if (collectorData.Circuits != null && collector.Circuits != null)
                {
                    for (int j = 0; j < collectorData.Circuits.Count && j < collector.Circuits.Count; j++)
                    {
                        var circuitData = collectorData.Circuits[j];
                        var circuit = collector.Circuits[j];

                        circuit.Power = circuitData.Power;
                        circuit.FlowRate = circuitData.FlowRate;
                        circuit.Velocity = circuitData.Velocity;
                        circuit.Throttling = circuitData.Throttling;
                        circuit.ValveTurns = circuitData.ValveTurns;

                        // Восстанавливаем OperatingResult
                        if (circuitData.OperatingResult != null)
                        {
                            if (!Enum.TryParse<FlowRegime>(circuitData.OperatingResult.FlowRegimeString, true, out var operatingFlowRegime) &&
                                !Enum.TryParse<FlowRegime>(circuitData.OperatingResult.FlowRegime, true, out operatingFlowRegime))
                            {
                                operatingFlowRegime = FlowRegime.Laminar;
                            }

                            circuit.OperatingResult = new CircuitTemperatureResult
                            {
                                DpRohr = circuitData.OperatingResult.DpRohr,
                                DpVerteiler = circuitData.OperatingResult.DpVerteiler,
                                DpVent = circuitData.OperatingResult.DpVent,
                                ZuDrosseln = circuitData.OperatingResult.Throttling,
                                FlowRegime = operatingFlowRegime,
                                Density = circuitData.OperatingResult.Density,
                                KinematicViscosity = circuitData.OperatingResult.KinematicViscosity,
                                ReynoldsNumber = circuitData.OperatingResult.ReynoldsNumber,
                                FrictionFactor = circuitData.OperatingResult.FrictionFactor,
                                PressureLossPerMeter = circuitData.OperatingResult.PressureLossPerMeter
                            };
                        }

                        // Восстанавливаем DesignResult
                        if (circuitData.DesignResult != null)
                        {
                            if (!Enum.TryParse<FlowRegime>(circuitData.DesignResult.FlowRegimeString, true, out var designFlowRegime) &&
                                !Enum.TryParse<FlowRegime>(circuitData.DesignResult.FlowRegime, true, out designFlowRegime))
                            {
                                designFlowRegime = FlowRegime.Laminar;
                            }

                            circuit.DesignResult = new CircuitTemperatureResult
                            {
                                DpRohr = circuitData.DesignResult.DpRohr,
                                DpVerteiler = circuitData.DesignResult.DpVerteiler,
                                DpVent = circuitData.DesignResult.DpVent,
                                ZuDrosseln = circuitData.DesignResult.Throttling,
                                FlowRegime = designFlowRegime,
                                Density = circuitData.DesignResult.Density,
                                KinematicViscosity = circuitData.DesignResult.KinematicViscosity,
                                ReynoldsNumber = circuitData.DesignResult.ReynoldsNumber,
                                FrictionFactor = circuitData.DesignResult.FrictionFactor,
                                PressureLossPerMeter = circuitData.DesignResult.PressureLossPerMeter
                            };
                        }
                    }
                }
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

            // Сохраняем климатические данные
            data.ClimateData = new ClimateProjectData
            {
                SelectedCity = _climateViewModel.SelectedCity?.Name ?? string.Empty,
                Region = _climateViewModel.SelectedCity?.Region ?? string.Empty,
                AirTemperature = _climateViewModel.AirTemperature,
                WindSpeed = _climateViewModel.WindSpeed,
                Humidity = _climateViewModel.Humidity,
                SnowfallIntensity = _climateViewModel.SnowfallIntensity,
                SelectedZone = _climateViewModel.SelectedZone,
                IsHighRequirements = _climateViewModel.IsHighRequirements
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

            // Сохраняем данные конструкции
            data.ConstructionData = new ConstructionProjectData
            {
                R1 = _constructionViewModel.R1Total,
                R2 = _constructionViewModel.R2Total,
                LambdaE = _constructionViewModel.LambdaE,
                GroundwaterLevel = _constructionViewModel.GroundwaterLevel,
                HasLoads = _constructionViewModel.HasLoads,
                Layers = _constructionViewModel.LayersAbovePipe.Select(l => new LayerProjectData
                {
                    Position = LayerPosition.AbovePipe,
                    MaterialName = l.Material?.Name ?? string.Empty,
                    MaterialLambda = l.Material?.LambdaA ?? 0,
                    Thickness = l.Thickness,
                    CalculatedLambda = l.CalculatedLambda,
                    IsLambdaOverridden = l.IsLambdaOverridden,
                    Order = l.Order
                }).Concat(_constructionViewModel.LayersBelowPipe.Select(l => new LayerProjectData
                {
                    Position = LayerPosition.BelowPipe,
                    MaterialName = l.Material?.Name ?? string.Empty,
                    MaterialLambda = l.Material?.LambdaA ?? 0,
                    Thickness = l.Thickness,
                    CalculatedLambda = l.CalculatedLambda,
                    IsLambdaOverridden = l.IsLambdaOverridden,
                    Order = l.Order
                })).ToList()
            };

            // Сохраняем данные теплового расчёта
            data.ThermalData = new ThermalProjectData
            {
                SelectedMode = _thermalViewModel.SelectedMode,
                SupplyTemperature = _thermalViewModel.SupplyTemperature,
                GroundTemperature = _thermalViewModel.GroundTemperature,
                PipeSpacing = _calculationStateService.PipeSpacing,
                SelectedPipe = _thermalViewModel.SelectedPipe != null ? new PipeTypeProjectData
                {
                    Name = _thermalViewModel.SelectedPipe.Name,
                    OuterDiameter = _thermalViewModel.SelectedPipe.OuterDiameter,
                    InnerDiameter = _thermalViewModel.SelectedPipe.InnerDiameter,
                    WallThickness = _thermalViewModel.SelectedPipe.WallThickness
                } : null,
                Result = _thermalViewModel.Result != null ? new ThermalResultProjectData
                {
                    PowerUp = _thermalViewModel.Result.PowerUp,
                    PowerDown = _thermalViewModel.Result.PowerDown,
                    PowerTotal = _thermalViewModel.Result.PowerTotal,
                    SupplyTemperature = _thermalViewModel.Result.SupplyTemperature,
                    ReturnTemperature = _thermalViewModel.Result.ReturnTemperature,
                    MeanTemperature = _thermalViewModel.Result.MeanTemperature,
                    DeltaT = _thermalViewModel.Result.DeltaT,
                    IsValid = _thermalViewModel.Result.IsValid
                } : null
            };

            // Сохраняем данные гидравлики
            data.HydraulicsData = new HydraulicsProjectData
            {
                GlycolType = _circuitsViewModel.InputData.GlycolType,
                GlycolConcentration = _circuitsViewModel.InputData.GlycolConcentration,
                SupplySpacingCm = _circuitsViewModel.InputData.SupplySpacing_cm,
                SupplyHeatPercent = _circuitsViewModel.InputData.SupplyHeatPercent,
                Collectors = _circuitsViewModel.Collectors.Select(c => new CollectorProjectData
                {
                    CollectorNumber = c.CollectorNumber,
                    CollectorType = c.CollectorType,
                    ValveType = c.ValveType,
                    Circuits = c.Circuits.Select(circuit => new CircuitProjectData
                    {
                        CircuitNumber = circuit.CircuitNumber,
                        CircuitLength = circuit.CircuitLength,
                        SupplyLength = circuit.SupplyLength,
                        SupplySpacingCm = circuit.SupplySpacing_cm,
                        SupplyHeatPercent = circuit.SupplyHeatPercent,
                        PipeSpacingCm = circuit.PipeSpacing_cm,
                        Power = circuit.Power,
                        FlowRate = circuit.FlowRate,
                        Velocity = circuit.Velocity,
                        FlowRegimeDescription = circuit.FlowRegimeDescription,
                        Throttling = circuit.Throttling,
                        ValveTurns = circuit.ValveTurns,
                        OperatingResult = circuit.OperatingResult != null ? new CircuitResultProjectData
                        {
                            Power = circuit.Power,
                            FlowRate = circuit.FlowRate,
                            Velocity = circuit.Velocity,
                            DpRohr = circuit.OperatingResult.DpRohr,
                            DpVerteiler = circuit.OperatingResult.DpVerteiler,
                            DpVent = circuit.OperatingResult.DpVent,
                            DpGesamt = circuit.OperatingResult.DpGesamt,
                            Throttling = circuit.Throttling,
                            ValveTurns = circuit.ValveTurns,
                            FlowRegime = circuit.OperatingResult.FlowRegime.ToString(),
                            FlowRegimeString = circuit.OperatingResult.FlowRegime.ToString(),
                            Density = circuit.OperatingResult.Density,
                            KinematicViscosity = circuit.OperatingResult.KinematicViscosity,
                            ReynoldsNumber = circuit.OperatingResult.ReynoldsNumber,
                            FrictionFactor = circuit.OperatingResult.FrictionFactor,
                            PressureLossPerMeter = circuit.OperatingResult.PressureLossPerMeter
                        } : null,
                        DesignResult = circuit.DesignResult != null ? new CircuitResultProjectData
                        {
                            Power = circuit.Power,
                            FlowRate = circuit.FlowRate,
                            Velocity = circuit.Velocity,
                            DpRohr = circuit.DesignResult.DpRohr,
                            DpVerteiler = circuit.DesignResult.DpVerteiler,
                            DpVent = circuit.DesignResult.DpVent,
                            DpGesamt = circuit.DesignResult.DpGesamt,
                            Throttling = circuit.Throttling,
                            ValveTurns = circuit.ValveTurns,
                            FlowRegime = circuit.DesignResult.FlowRegime.ToString(),
                            FlowRegimeString = circuit.DesignResult.FlowRegime.ToString(),
                            Density = circuit.DesignResult.Density,
                            KinematicViscosity = circuit.DesignResult.KinematicViscosity,
                            ReynoldsNumber = circuit.DesignResult.ReynoldsNumber,
                            FrictionFactor = circuit.DesignResult.FrictionFactor,
                            PressureLossPerMeter = circuit.DesignResult.PressureLossPerMeter
                        } : null
                    }).ToList(),
                    Summary = c.Summary != null ? new CollectorSummaryProjectData
                    {
                        CircuitCount = c.Summary.CircuitCount,
                        TotalPipeLength = c.Summary.TotalPipeLength,
                        TotalPower = c.Summary.TotalPower,
                        TotalFlowRate = c.Summary.TotalFlowRate,
                        PressureLoss_Operating_Pa = c.Summary.PressureLoss_Operating_Pa,
                        PressureLoss_Cold_Pa = c.Summary.PressureLoss_Cold_Pa,
                        Kv = c.Summary.Kv,
                        CollectorType = c.Summary.CollectorType
                    } : null
                }).ToList()
            };

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

        /// <summary>
        /// Загрузить слои конструкции из данных проекта
        /// </summary>
        private void LoadLayersFromProjectData(List<LayerProjectData> layerDataList, string version)
        {
            // До v1.1 слои AbovePipe сохранялись в хронологическом порядке (Add в конец),
            // т.е. [у трубы, поверхность]. С v1.1 физический top-to-bottom: [поверхность, ..., у трубы].
            var needsAbovePipeReverse = string.Compare(version, "1.1", StringComparison.OrdinalIgnoreCase) < 0;

            var aboveLayers = layerDataList
                .Where(l => l.Position == LayerPosition.AbovePipe)
                .Reverse();
            if (!needsAbovePipeReverse)
                aboveLayers = aboveLayers.Reverse();
            aboveLayers = aboveLayers.ToList();

            var belowLayers = layerDataList
                .Where(l => l.Position == LayerPosition.BelowPipe)
                .ToList(); // порядок below не менялся

            // Clear + Add по мигрированным коллекциям
            _constructionViewModel.LayersAbovePipe.Clear();
            _constructionViewModel.LayersBelowPipe.Clear();

            foreach (var layerData in aboveLayers)
            {
                var material = _constructionViewModel.AvailableMaterials
                    .FirstOrDefault(m => m.Name == layerData.MaterialName)
                    ?? Material.GetDefaultMaterial();

                var layer = new Layer
                {
                    Position = layerData.Position,
                    Material = material,
                    Thickness = layerData.Thickness,
                    CalculatedLambda = layerData.CalculatedLambda,
                    IsLambdaOverridden = layerData.IsLambdaOverridden,
                    Order = layerData.Order
                };

                _constructionViewModel.LayersAbovePipe.Add(layer);
            }

            foreach (var layerData in belowLayers)
            {
                var material = _constructionViewModel.AvailableMaterials
                    .FirstOrDefault(m => m.Name == layerData.MaterialName)
                    ?? Material.GetDefaultMaterial();

                var layer = new Layer
                {
                    Position = layerData.Position,
                    Material = material,
                    Thickness = layerData.Thickness,
                    CalculatedLambda = layerData.CalculatedLambda,
                    IsLambdaOverridden = layerData.IsLambdaOverridden,
                    Order = layerData.Order
                };

                _constructionViewModel.LayersBelowPipe.Add(layer);
            }

            // Обновляем λ для слоёв под трубой в соответствии с восстановленным УГВ.
            // Метод UpdateLambda учитывает флаг IsLambdaOverridden и оставляет ручные значения нетронутыми.
            foreach (var layer in _constructionViewModel.LayersBelowPipe)
            {
                layer.UpdateLambda(_constructionViewModel.GroundwaterLevel);
            }

            _constructionViewModel.UpdateCalculations();
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
}
