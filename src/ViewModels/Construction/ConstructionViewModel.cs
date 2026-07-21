using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Core;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.ViewModels.Construction
{
    /// <summary>
    /// ViewModel для модуля "Конструктор конструкции"
    /// </summary>
    public partial class ConstructionViewModel : ObservableObject
    {
        private readonly IConstructionService _constructionService;
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionRepository _constructionRepository;
        private readonly ICalculationStateService _calculationStateService;
        private readonly CalculationContext _calculationContext;
        private readonly IValidator<ConstructionModel> _validator;
        private readonly ConstructionModel _construction;
        private readonly IMarkDirtyService _markDirtyService;
        private readonly IConstructionTemplateRepository _templateRepository;
        private readonly IDialogService _dialogService;
        private readonly IEditorDialogService _editorDialogService;
        private bool _isSyncing; // Флаг для предотвращения рекурсии при синхронизации
        private bool _isResetting;

        #region Observable Properties

        /// <summary>
        /// Слои над трубой
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Layer> _layersAbovePipe = new();

        /// <summary>
        /// Слои под трубой
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Layer> _layersBelowPipe = new();

        /// <summary>
        /// Доступные материалы
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Material> _availableMaterials = new();

        /// <summary>
        /// Шаблоны конструкций
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ConstructionTemplate> _templates = new();

        /// <summary>
        /// Выбранный слой
        /// </summary>
        [ObservableProperty]
        private Layer? _selectedLayer;

        /// <summary>
        /// Выбранный шаблон
        /// </summary>
        [ObservableProperty]
        private ConstructionTemplate? _selectedTemplate;

        /// <summary>
        /// Уровень грунтовых вод, м
        /// </summary>
        [ObservableProperty]
        private double _groundwaterLevel = 2.0;

        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        [ObservableProperty]
        private bool _hasLoads;

        /// <summary>
        /// Сообщение валидации
        /// </summary>
        [ObservableProperty]
        private string _validationMessage = string.Empty;

        /// <summary>
        /// Признак валидности конструкции
        /// </summary>
        [ObservableProperty]
        private bool _isValid = true;

        /// <summary>
        /// Суммарное термическое сопротивление над трубой (R1), м²·К/Вт
        /// </summary>
        [ObservableProperty]
        private double _r1Total;

        /// <summary>
        /// Суммарное термическое сопротивление под трубой (R2), м²·К/Вт
        /// </summary>
        [ObservableProperty]
        private double _r2Total;

        /// <summary>
        /// Теплопроводность материала вокруг трубы (LambdaE), Вт/м·К
        /// </summary>
        [ObservableProperty]
        private double _lambdaE = 1.6;

        /// <summary>
        /// Признак загрузки данных
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Признак того, что есть несохранённые изменения
        /// </summary>
        [ObservableProperty]
        private bool _hasUnsavedChanges;

        /// <summary>
        /// Шаг укладки труб, мм (получается из ThermalViewModel через сервис)
        /// </summary>
        public int PipeSpacing
        {
            get { return _calculationStateService.PipeSpacing; }
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Общая толщина слоёв над трубой, мм
        /// </summary>
        public double TotalThicknessAbovePipe => LayersAbovePipe.Sum(l => l.Thickness);

        /// <summary>
        /// Общая толщина слоёв под трубой, мм
        /// </summary>
        public double TotalThicknessBelowPipe => LayersBelowPipe.Sum(l => l.Thickness);

        /// <summary>
        /// Варианты уровня грунтовых вод
        /// </summary>
        public ObservableCollection<string> GroundwaterLevelOptions { get; } = new ObservableCollection<string>
        {
            "УГВ < 1 м (влажные условия)",
            "УГВ >= 1 м (сухие условия)"
        };

        /// <summary>
        /// Выбранный вариант УГВ
        /// </summary>
        [ObservableProperty]
        private string _selectedGroundwaterOption = "УГВ >= 1 м (сухие условия)";

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения данных конструкции
        /// </summary>
        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Создать ViewModel
        /// </summary>
        public ConstructionViewModel(
            IConstructionService constructionService,
            IMaterialRepository materialRepository,
            IConstructionRepository constructionRepository,
            ICalculationStateService calculationStateService,
            CalculationContext calculationContext,
            IValidator<ConstructionModel> validator,
            ConstructionModel construction,
            IMarkDirtyService markDirtyService,
            IConstructionTemplateRepository templateRepository,
            IDialogService dialogService,
            IEditorDialogService editorDialogService)
        {
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionRepository = constructionRepository ?? throw new ArgumentNullException(nameof(constructionRepository));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _editorDialogService = editorDialogService ?? throw new ArgumentNullException(nameof(editorDialogService));

            // Подписываемся на изменения коллекций
            LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;
            LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;

            // Подписываемся на изменения в модели Construction
            _construction.DataChanged += OnConstructionDataChanged;

            // Подписываемся на изменения шага укладки
            _calculationStateService.PipeSpacingChanged += OnPipeSpacingChanged;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда инициализации (загрузки данных)
        /// </summary>
        [RelayCommand]
        private async Task Initialize()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                // Загружаем материалы и шаблоны
                await RefreshCatalogsAsync();

                // Устанавливаем конструкцию по умолчанию
                ResetToDefault();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Команда добавления слоя над трубой
        /// </summary>
        [RelayCommand]
        private void AddLayerAbovePipe()
        {
            if (_isResetting) return;
            if (AvailableMaterials.Count == 0) return;

            var defaultMaterial = AvailableMaterials.FirstOrDefault(m => m.Id == 5) ?? AvailableMaterials.First();
            var layer = new Layer
            {
                Material = defaultMaterial,
                Thickness = 50,
                CalculatedLambda = defaultMaterial.LambdaA,
                Position = LayerPosition.AbovePipe
            };

            LayersAbovePipe.Insert(0, layer);
            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Команда добавления слоя под трубой
        /// </summary>
        [RelayCommand]
        private void AddLayerBelowPipe()
        {
            if (_isResetting) return;
            if (AvailableMaterials.Count == 0) return;

            var defaultMaterial = AvailableMaterials.FirstOrDefault(m => m.Id == 1) ?? AvailableMaterials.First();
            var lambda = GroundwaterLevel < 1.0 ? defaultMaterial.LambdaB : defaultMaterial.LambdaA;

            var layer = new Layer
            {
                Material = defaultMaterial,
                Thickness = 100,
                CalculatedLambda = lambda,
                Position = LayerPosition.BelowPipe,
                Order = LayersBelowPipe.Count
            };

            LayersBelowPipe.Add(layer);
            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Команда удаления слоя
        /// </summary>
        [RelayCommand]
        private void RemoveLayer(Layer? layer)
        {
            if (_isResetting) return;
            if (layer == null) return;

            if (layer.Position == LayerPosition.AbovePipe)
            {
                LayersAbovePipe.Remove(layer);
            }
            else
            {
                LayersBelowPipe.Remove(layer);
            }

            SelectedLayer = null;
            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Команда применения шаблона
        /// </summary>
        [RelayCommand]
        private async Task ApplyTemplate()
        {
            if (_isResetting) return;
            if (SelectedTemplate == null) return;

            try
            {
                await ApplySelectedTemplateAsync();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка применения шаблона: {ex.Message}";
                IsValid = false;
            }
        }

        /// <summary>
        /// Применяет выбранный шаблон с возможностью импорта отсутствующего материала.
        /// </summary>
        private async Task ApplySelectedTemplateAsync()
        {
            try
            {
                ApplyTemplateCore(SelectedTemplate!);
            }
            catch (MaterialNotFoundException ex) when (ex.Snapshot != null)
            {
                var result = _dialogService.Show(
                    $"Материал '{ex.Snapshot.Name}' (ID {ex.MaterialId}) отсутствует в справочнике. Импортировать из снимка?",
                    "Импорт материала",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _constructionService.ImportMissingMaterialAsync(ex.Snapshot);
                        await RefreshCatalogsAsync();
                        ApplyTemplateCore(SelectedTemplate!);
                    }
                    catch (Exception importEx)
                    {
                        ValidationMessage = $"Ошибка импорта материала: {importEx.Message}";
                        IsValid = false;
                    }
                }
                else
                {
                    _dialogService.ShowError(
                        $"Материал '{ex.Snapshot.Name}' (ID {ex.MaterialId}) не импортирован. Применение шаблона отменено.",
                        "Импорт отменён");
                    ValidationMessage = $"Материал '{ex.Snapshot.Name}' не найден в справочнике";
                    IsValid = false;
                }
            }
            catch (MaterialNotFoundException ex) when (ex.Snapshot == null)
            {
                _dialogService.ShowError(
                    $"Материал с идентификатором {ex.MaterialId} не найден в справочнике и отсутствует снимок для импорта.",
                    "Ошибка применения шаблона");
                ValidationMessage = $"Материал с идентификатором {ex.MaterialId} не найден в справочнике";
                IsValid = false;
            }
        }

        /// <summary>
        /// Ядро применения шаблона: создание конструкции и копирование слоёв.
        /// </summary>
        private void ApplyTemplateCore(ConstructionTemplate template)
        {
            // Создаём новую конструкцию из шаблона
            var newConstruction = _constructionService.CreateFromTemplate(template, AvailableMaterials);

            // Очищаем текущие слои
            LayersAbovePipe.Clear();
            LayersBelowPipe.Clear();

            // Добавляем слои из шаблона
            foreach (var layer in newConstruction.LayersAbovePipe)
            {
                LayersAbovePipe.Add(layer);
            }

            foreach (var layer in newConstruction.Layers)
            {
                LayersBelowPipe.Add(layer);
            }

            // Устанавливаем параметры
            GroundwaterLevel = template.DefaultGroundwaterLevel;
            HasLoads = template.HasLoads;

            // Обновляем УГВ опцию
            SelectedGroundwaterOption = GroundwaterLevel < 1.0
                ? "УГВ < 1 м (влажные условия)"
                : "УГВ >= 1 м (сухие условия)";

            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Команда сохранения конструкции
        /// </summary>
        [RelayCommand]
        private async Task SaveConstruction()
        {
            try
            {
                // Синхронизируем с моделью
                SyncToModel();

                // Сохраняем в файл
                var filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "SnowMeltingCalculator",
                    $"construction_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                await _constructionRepository.SaveConstructionAsync(_construction, filePath);

                HasUnsavedChanges = false;
                ValidationMessage = "Конструкция сохранена успешно";
                IsValid = true;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка сохранения: {ex.Message}";
                IsValid = false;
            }
        }

        /// <summary>
        /// Команда загрузки конструкции
        /// </summary>
        [RelayCommand]
        private async Task LoadConstruction()
        {
            // В реальном приложении здесь должен быть диалог выбора файла
            // Для демонстрации используем файл по умолчанию
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SnowMeltingCalculator",
                "construction_last.json");

            try
            {
                await LoadConstructionCoreAsync(filePath);
            }
            catch (MaterialNotFoundException ex) when (ex.Snapshot != null)
            {
                var result = _dialogService.Show(
                    $"Материал '{ex.Snapshot.Name}' (ID {ex.MaterialId}) отсутствует в справочнике. Импортировать из снимка?",
                    "Импорт материала",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _constructionService.ImportMissingMaterialAsync(ex.Snapshot);
                        await RefreshCatalogsAsync();
                        await LoadConstructionCoreAsync(filePath);
                    }
                    catch (Exception importEx)
                    {
                        ValidationMessage = $"Ошибка импорта материала: {importEx.Message}";
                        IsValid = false;
                    }
                }
                else
                {
                    _dialogService.ShowError(
                        $"Материал '{ex.Snapshot.Name}' (ID {ex.MaterialId}) не импортирован. Загрузка конструкции отменена.",
                        "Импорт отменён");
                    ValidationMessage = $"Материал '{ex.Snapshot.Name}' не найден в справочнике";
                    IsValid = false;
                }
            }
            catch (MaterialNotFoundException ex) when (ex.Snapshot == null)
            {
                _dialogService.ShowError(
                    $"Материал с идентификатором {ex.MaterialId} не найден в справочнике и отсутствует снимок для импорта.",
                    "Ошибка загрузки конструкции");
                ValidationMessage = $"Материал с идентификатором {ex.MaterialId} не найден в справочнике";
                IsValid = false;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка загрузки: {ex.Message}";
                IsValid = false;
            }
        }

        /// <summary>
        /// Загружает конструкцию из файла и копирует данные в текущую модель.
        /// </summary>
        private async Task LoadConstructionCoreAsync(string filePath)
        {
            var loadedConstruction = await _constructionRepository.LoadConstructionAsync(filePath);

            if (loadedConstruction != null)
            {
                // Копируем данные из загруженной конструкции в текущую
                CopyConstructionData(loadedConstruction);
                SyncFromModel();
                HasUnsavedChanges = false;
                ValidationMessage = "Конструкция загружена успешно";
                IsValid = true;
            }
        }

        /// <summary>
        /// Команда открытия редактора материалов
        /// </summary>
        [RelayCommand]
        private async Task OpenMaterialEditor()
        {
            if (_editorDialogService.ShowMaterialEditor() == true)
            {
                await RefreshCatalogsAsync();
            }
        }

        /// <summary>
        /// Команда открытия редактора шаблонов
        /// </summary>
        [RelayCommand]
        private async Task OpenTemplateEditor()
        {
            if (_editorDialogService.ShowTemplateEditor() != null)
            {
                await RefreshCatalogsAsync();
            }
        }

        /// <summary>
        /// Перезагружает материалы и шаблоны из репозиториев и обновляет коллекции.
        /// </summary>
        private async Task RefreshCatalogsAsync()
        {
            var materials = await _materialRepository.LoadMaterialsAsync();
            SynchronizeAvailableMaterials(materials);

            var templates = await _templateRepository.GetAllAsync();
            Templates.Clear();
            if (templates != null)
            {
                foreach (var template in templates)
                {
                    Templates.Add(template);
                }
            }

            RebindLayerMaterials();
            OnPropertyChanged(nameof(AvailableMaterials));
        }

        /// <summary>
        /// Синхронизирует коллекцию доступных материалов с данными репозитория
        /// по идентификатору, сохраняя ссылки на существующие экземпляры.
        /// </summary>
        /// <param name="materials">Актуальный список материалов из репозитория.</param>
        private void SynchronizeAvailableMaterials(IEnumerable<Material> materials)
        {
            var updatedMaterials = materials.ToList();
            var updatedById = updatedMaterials.ToDictionary(m => m.Id);

            // Удаляем материалы, которых больше нет в репозитории.
            for (int i = AvailableMaterials.Count - 1; i >= 0; i--)
            {
                var existing = AvailableMaterials[i];
                if (!updatedById.ContainsKey(existing.Id))
                {
                    AvailableMaterials.RemoveAt(i);
                }
            }

            var existingById = AvailableMaterials.ToDictionary(m => m.Id);
            foreach (var updated in updatedMaterials)
            {
                if (existingById.TryGetValue(updated.Id, out var existing))
                {
                    // Обновляем существующий экземпляр на месте, чтобы сохранить
                    // живые ссылки из слоёв и других коллекций.
                    existing.Name = updated.Name;
                    existing.Category = updated.Category;
                    existing.LambdaA = updated.LambdaA;
                    existing.LambdaB = updated.LambdaB;
                    existing.MaxSupplyTemp = updated.MaxSupplyTemp;
                    existing.MinOutdoorTemp = updated.MinOutdoorTemp;
                    existing.Notes = updated.Notes;
                    existing.IsBuiltIn = updated.IsBuiltIn;
                }
                else
                {
                    AvailableMaterials.Add(updated);
                }
            }
        }

        /// <summary>
        /// Перепривязывает ссылки <see cref="Layer.Material"/> к актуальным
        /// экземплярам из <see cref="AvailableMaterials"/>.
        /// </summary>
        private void RebindLayerMaterials()
        {
            foreach (var layer in LayersAbovePipe.Concat(LayersBelowPipe))
            {
                var currentMaterial = AvailableMaterials.FirstOrDefault(m => m.Id == layer.Material?.Id);
                if (currentMaterial != null)
                {
                    layer.Material = currentMaterial;
                }
                else
                {
                    var previousLambda = layer.CalculatedLambda;
                    var fallback = Material.GetDefaultMaterial();
                    var catalogFallback = AvailableMaterials.FirstOrDefault(m => m.Id == fallback.Id);
                    layer.Material = catalogFallback ?? fallback;
                    layer.CalculatedLambda = previousLambda;
                    layer.IsLambdaOverridden = true;
                }
            }
        }

        /// <summary>
        /// Публичный обёртка для перезагрузки материалов из репозитория.
        /// Используется при открытии проекта для обновления списка доступных материалов.
        /// </summary>
        public async Task ReloadMaterialsAsync()
        {
            await RefreshCatalogsAsync();
        }

        /// <summary>
        /// Сбросить ViewModel к значениям по умолчанию
        /// </summary>
        public void Reset()
        {
            _isResetting = true;
            try
            {
                LayersAbovePipe.Clear();
                LayersBelowPipe.Clear();

                // Добавляем базовые слои
                var concrete = AvailableMaterials.FirstOrDefault(m => m.Id == 5);
                var sand = AvailableMaterials.FirstOrDefault(m => m.Id == 1);
                var soil = AvailableMaterials.FirstOrDefault(m => m.Id == 2);

                if (concrete != null)
                {
                    LayersAbovePipe.Add(new Layer
                    {
                        Material = concrete,
                        Thickness = 100,
                        CalculatedLambda = concrete.LambdaA,
                        Position = LayerPosition.AbovePipe,
                        Order = 0
                    });
                }

                if (sand != null)
                {
                    LayersBelowPipe.Add(new Layer
                    {
                        Material = sand,
                        Thickness = 150,
                        CalculatedLambda = sand.LambdaA,
                        Position = LayerPosition.BelowPipe,
                        Order = 0
                    });
                }

                if (soil != null)
                {
                    LayersBelowPipe.Add(new Layer
                    {
                        Material = soil,
                        Thickness = 200,
                        CalculatedLambda = soil.LambdaA,
                        Position = LayerPosition.BelowPipe,
                        Order = 1
                    });
                }

                GroundwaterLevel = 2.0;
                HasLoads = false;
                SelectedGroundwaterOption = "УГВ >= 1 м (сухие условия)";
                SelectedTemplate = null;
                SelectedLayer = null;

                UpdateCalculations();
                HasUnsavedChanges = false;
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Команда сброса к значениям по умолчанию
        /// </summary>
        [RelayCommand]
        private void ResetToDefault()
        {
            Reset();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обработчик изменения слоя
        /// </summary>
        public void OnLayerChanged(Layer layer)
        {
            if (layer == null) return;

            // Обновляем λ если не переопределена вручную
            if (!layer.IsLambdaOverridden && layer.Material != null)
            {
                if (layer.Position == LayerPosition.AbovePipe)
                {
                    layer.CalculatedLambda = layer.Material.LambdaA;
                }
                else
                {
                    layer.CalculatedLambda = GroundwaterLevel < 1.0
                        ? layer.Material.LambdaB
                        : layer.Material.LambdaA;
                }
            }

            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Обновить расчёты R1, R2, LambdaE
        /// </summary>
        public void UpdateCalculations()
        {
            // Рассчитываем R1
            R1Total = LayersAbovePipe.Sum(l => l.CalculatedR);

            // Рассчитываем R2
            R2Total = LayersBelowPipe.Sum(l => l.CalculatedR);

            // Обновляем свойства для UI
            OnPropertyChanged(nameof(TotalThicknessAbovePipe));
            OnPropertyChanged(nameof(TotalThicknessBelowPipe));

            // Валидация
            Validate();

            // Синхронизируем с моделью перед уведомлением
            SyncToModel();

            // LambdaE берётся из модели; единый источник истины
            LambdaE = _construction.LambdaE;

            // Уведомляем об изменении данных
            OnDataChanged();

            // Публикуем в общий контекст при валидных данных
            if (IsValid)
            {
                _calculationContext.UpdateConstruction(_construction, "Construction");
            }
        }

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        public void Validate()
        {
            // Синхронизируем с моделью для валидации
            SyncToModel();

            var result = _validator.Validate(_construction);

            IsValid = result.IsValid;

            var messages = new System.Collections.Generic.List<string>();
            messages.AddRange(result.Errors.Select(e => e.Message));
            messages.AddRange(result.Warnings);

            ValidationMessage = string.Join("\n", messages);
        }

        /// <summary>
        /// Получить данные конструкции для передачи другим модулям
        /// </summary>
        public ConstructionModel GetConstruction()
        {
            SyncToModel();
            return _construction;
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Обработчик изменения УГВ
        /// </summary>
        partial void OnGroundwaterLevelChanged(double value)
        {
            if (_isResetting) return;

            // Обновляем λ для слоёв под трубой
            foreach (var layer in LayersBelowPipe)
            {
                if (!layer.IsLambdaOverridden && layer.Material != null)
                {
                    layer.CalculatedLambda = value < 1.0
                        ? layer.Material.LambdaB
                        : layer.Material.LambdaA;
                }
            }

            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Обработчик изменения признака нагрузок
        /// </summary>
        partial void OnHasLoadsChanged(bool value)
        {
            if (_isResetting) return;

            UpdateCalculations();
            HasUnsavedChanges = true;
            _markDirtyService.MarkDirty();
        }

        /// <summary>
        /// Обработчик изменения выбора УГВ опции
        /// </summary>
        partial void OnSelectedGroundwaterOptionChanged(string value)
        {
            // Устанавливаем УГВ в зависимости от выбора
            GroundwaterLevel = value.Contains("< 1") ? 0.5 : 2.0;
        }

        /// <summary>
        /// Обработчик изменения выбранного слоя
        /// </summary>
        partial void OnSelectedLayerChanged(Layer? value)
        {
            // Можно добавить логику при выборе слоя
        }

        /// <summary>
        /// Обработчик изменения шага укладки труб
        /// </summary>
        private void OnPipeSpacingChanged(object? sender, int spacing)
        {
            OnPropertyChanged(nameof(PipeSpacing));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обработчик изменения коллекции слоёв
        /// </summary>
        private void OnLayersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Подписываемся на изменения свойств новых слоёв
            if (e.NewItems != null)
            {
                foreach (Layer layer in e.NewItems)
                {
                    layer.PropertyChanged += OnLayerPropertyChanged;
                }
            }

            // Отписываемся от изменений свойств удалённых слоёв
            if (e.OldItems != null)
            {
                foreach (Layer layer in e.OldItems)
                {
                    layer.PropertyChanged -= OnLayerPropertyChanged;
                }
            }

            if (!_isSyncing && !_isResetting)
            {
                _markDirtyService.MarkDirty();
                UpdateCalculations();
            }
        }

        /// <summary>
        /// Обработчик изменения свойств слоя
        /// </summary>
        private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isSyncing || _isResetting) return;

            try
            {
                // При изменении толщины или λ пересчитываем R
                if (e.PropertyName == nameof(Layer.Thickness) ||
                    e.PropertyName == nameof(Layer.CalculatedLambda) ||
                    e.PropertyName == nameof(Layer.Material))
                {
                    _markDirtyService.MarkDirty();
                    // При изменении материала обновляем λ с учётом УГВ
                    if (e.PropertyName == nameof(Layer.Material) && sender is Layer layer)
                    {
                        layer.UpdateLambda(GroundwaterLevel);
                    }
                    UpdateCalculations();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обработке изменения слоя: {ex.Message}");
                ValidationMessage = $"Ошибка: {ex.Message}";
                IsValid = false;
            }
        }

        /// <summary>
        /// Обработчик изменения данных в модели Construction
        /// </summary>
        private void OnConstructionDataChanged(object? sender, ConstructionDataChangedEventArgs e)
        {
            if (!_isSyncing)
            {
                // Синхронизируем данные из модели в ViewModel
                SyncFromModel();
            }
        }

        /// <summary>
        /// Синхронизировать данные из модели в ViewModel
        /// </summary>
        private void SyncFromModel()
        {
            if (_isSyncing) return;

            _isSyncing = true;
            try
            {
                LayersAbovePipe.Clear();
                LayersBelowPipe.Clear();

                foreach (var layer in _construction.LayersAbovePipe)
                {
                    LayersAbovePipe.Add(layer);
                }

                foreach (var layer in _construction.Layers)
                {
                    LayersBelowPipe.Add(layer);
                }

                GroundwaterLevel = _construction.GroundwaterLevel;
                HasLoads = _construction.HasLoads;

                UpdateCalculations();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Синхронизировать данные из ViewModel в модель
        /// </summary>
        private void SyncToModel()
        {
            if (_isSyncing) return;

            _isSyncing = true;
            try
            {
                _construction.LayersAbovePipe.Clear();
                _construction.Layers.Clear();

                foreach (var layer in LayersAbovePipe)
                {
                    _construction.LayersAbovePipe.Add(layer);
                }

                foreach (var layer in LayersBelowPipe)
                {
                    _construction.Layers.Add(layer);
                }

                _construction.GroundwaterLevel = GroundwaterLevel;
                _construction.HasLoads = HasLoads;

                // Единый источник истины для порядка слоёв
                _construction.ReindexLayers();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Копировать данные из другой конструкции в текущую
        /// </summary>
        private void CopyConstructionData(ConstructionModel source)
        {
            _construction.LayersAbovePipe.Clear();
            _construction.Layers.Clear();

            foreach (var layer in source.LayersAbovePipe)
            {
                _construction.LayersAbovePipe.Add(layer);
            }

            foreach (var layer in source.Layers)
            {
                _construction.Layers.Add(layer);
            }

            _construction.GroundwaterLevel = source.GroundwaterLevel;
            _construction.HasLoads = source.HasLoads;
        }

        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
        private void OnDataChanged()
        {
            DataChanged?.Invoke(this, new ConstructionDataChangedEventArgs
            {
                ChangedProperty = "Construction",
                OldValue = null,
                NewValue = _construction,
                IsValid = IsValid
            });
        }

        #endregion
    }
}