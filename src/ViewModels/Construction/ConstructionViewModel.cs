using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using SnowMeltingCalculator.Services.Project;

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
        private readonly IValidator<ConstructionModel> _validator;
        private readonly ConstructionModel _construction;
        private readonly IConstructionTemplateRepository _templateRepository;
        private readonly IDialogService _dialogService;
        private readonly IEditorDialogService _editorDialogService;
        private readonly IProjectSessionConstructionState _constructionState;
        private readonly ConstructionDefaultStateInitializer _defaultStateInitializer;
        private bool _isSyncing; // Флаг для предотвращения рекурсии при синхронизации
        private bool _isResetting;
        private bool _isRefreshing; // Флаг для предотвращения рекурсии при обновлении из state
        private readonly HashSet<Layer> _subscribedLayers = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<Layer> _pendingMaterialLambdaUpdates = new(ReferenceEqualityComparer.Instance);

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
        /// Признак развёрнутости аккордеона предпросмотра шаблона
        /// </summary>
        [ObservableProperty]
        private bool _isTemplatePreviewExpanded = false;

        /// <summary>
        /// Слои над трубой для предпросмотра шаблона
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Layer> _templatePreviewLayersAbovePipe = new();

        /// <summary>
        /// Слои под трубой для предпросмотра шаблона
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Layer> _templatePreviewLayersBelowPipe = new();

        /// <summary>
        /// Признак возможности применения выбранного шаблона
        /// </summary>
        [ObservableProperty]
        private bool _canApplySelectedTemplate = false;

        /// <summary>
        /// Сообщение об ошибке предпросмотра шаблона
        /// </summary>
        [ObservableProperty]
        private string _templatePreviewErrorMessage = string.Empty;

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
            IEditorDialogService editorDialogService,
            IProjectSessionConstructionState constructionState,
            ConstructionDefaultStateInitializer defaultStateInitializer)
        {
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionRepository = constructionRepository ?? throw new ArgumentNullException(nameof(constructionRepository));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            ArgumentNullException.ThrowIfNull(calculationContext);
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
            ArgumentNullException.ThrowIfNull(markDirtyService);
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _editorDialogService = editorDialogService ?? throw new ArgumentNullException(nameof(editorDialogService));
            ArgumentNullException.ThrowIfNull(constructionState);
            ArgumentNullException.ThrowIfNull(defaultStateInitializer);
            _constructionState = constructionState;
            _defaultStateInitializer = defaultStateInitializer;

            // Подписываемся на изменения коллекций
            LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;
            LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;

            // Подписываемся на изменения в модели Construction
            _construction.DataChanged += OnConstructionDataChanged;

            // Подписываемся на изменения шага укладки
            _calculationStateService.PipeSpacingChanged += OnPipeSpacingChanged;

            // Подписываемся на изменения состояния конструкции
            _constructionState.Changed += OnConstructionStateChanged;
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

                var result = _defaultStateInitializer.Apply(
                    GroundwaterLevel,
                    ConstructionMutationOrigin.Initialization);
                ApplyLifecycleSnapshotToAdapter(result.After);
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

            _isSyncing = true;
            try
            {
                LayersAbovePipe.Insert(0, layer);
                for (int index = 0; index < LayersAbovePipe.Count; index++)
                {
                    LayersAbovePipe[index].Order = index;
                }
            }
            finally
            {
                _isSyncing = false;
            }
            UpdateCalculations();
            HasUnsavedChanges = true;
            SyncStateFromCollections(ConstructionMutationOrigin.User);
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

            _isSyncing = true;
            try
            {
                LayersBelowPipe.Add(layer);
            }
            finally
            {
                _isSyncing = false;
            }
            UpdateCalculations();
            HasUnsavedChanges = true;
            SyncStateFromCollections(ConstructionMutationOrigin.User);
        }

        /// <summary>
        /// Команда удаления слоя
        /// </summary>
        [RelayCommand]
        private void RemoveLayer(Layer? layer)
        {
            if (_isResetting) return;
            if (layer == null) return;

            _isSyncing = true;
            try
            {
                if (layer.Position == LayerPosition.AbovePipe)
                {
                    LayersAbovePipe.Remove(layer);
                }
                else
                {
                    LayersBelowPipe.Remove(layer);
                }

                var layers = layer.Position == LayerPosition.AbovePipe
                    ? LayersAbovePipe
                    : LayersBelowPipe;
                for (int index = 0; index < layers.Count; index++)
                {
                    layers[index].Order = index;
                }
            }
            finally
            {
                _isSyncing = false;
            }

            SelectedLayer = null;
            UpdateCalculations();
            HasUnsavedChanges = true;
            SyncStateFromCollections(ConstructionMutationOrigin.User);
        }

        /// <summary>
        /// Команда применения шаблона
        /// </summary>
        [RelayCommand]
        private async Task ApplyTemplate()
        {
            if (_isResetting) return;
            if (SelectedTemplate == null) return;
            if (!CanApplySelectedTemplate) return;

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
                    DialogButtons.YesNo,
                    DialogIcon.Question);

                if (result == DialogResult.Yes)
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
            var newConstruction = _constructionService.CreateFromTemplate(template, AvailableMaterials);
            var candidate = new ConstructionStateSnapshot(
                GroundwaterLevel,
                template.HasLoads,
                newConstruction.LayersAbovePipe.Select((layer, index) => new ConstructionLayerSnapshot(
                    layer.Id,
                    layer.Material?.Id ?? 0,
                    layer.Material?.Name ?? string.Empty,
                    layer.Thickness,
                    layer.CalculatedLambda,
                    layer.IsLambdaOverridden,
                    LayerPosition.AbovePipe,
                    index)).ToArray(),
                newConstruction.Layers.Select((layer, index) => new ConstructionLayerSnapshot(
                    layer.Id,
                    layer.Material?.Id ?? 0,
                    layer.Material?.Name ?? string.Empty,
                    layer.Thickness,
                    layer.CalculatedLambda,
                    layer.IsLambdaOverridden,
                    LayerPosition.BelowPipe,
                    index)).ToArray());

            _constructionState.ApplySnapshot(candidate, ConstructionMutationOrigin.Template);

            _isSyncing = true;
            try
            {
                LayersAbovePipe.Clear();
                LayersBelowPipe.Clear();

                foreach (var layer in newConstruction.LayersAbovePipe)
                {
                    LayersAbovePipe.Add(layer);
                }

                foreach (var layer in newConstruction.Layers)
                {
                    LayersBelowPipe.Add(layer);
                }

                HasLoads = template.HasLoads;
                SelectedGroundwaterOption = GroundwaterLevel < 1.0
                    ? "УГВ < 1 м (влажные условия)"
                    : "УГВ >= 1 м (сухие условия)";
            }
            finally
            {
                _isSyncing = false;
            }

            UpdateCalculations();
            HasUnsavedChanges = true;
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
                    DialogButtons.YesNo,
                    DialogIcon.Question);

                if (result == DialogResult.Yes)
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
                var result = _defaultStateInitializer.Apply(
                    GroundwaterLevel,
                    ConstructionMutationOrigin.Reset);
                ApplyLifecycleSnapshotToAdapter(result.After);
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

        /// <summary>
        /// Команда переключения развёрнутости аккордеона предпросмотра шаблона
        /// </summary>
        [RelayCommand]
        private void ToggleTemplatePreview()
        {
            IsTemplatePreviewExpanded = !IsTemplatePreviewExpanded;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обработчик изменения слоя
        /// </summary>
        public void OnLayerChanged(Layer layer)
        {
            if (layer == null || _isRefreshing) return;

            _constructionState.Apply(
                new ConstructionMutation.EditLayer(
                    layer.Id,
                    layer.Material?.Id ?? 0,
                    layer.Material?.Name ?? string.Empty,
                    layer.Thickness,
                    layer.CalculatedLambda,
                    layer.IsLambdaOverridden),
                ConstructionMutationOrigin.User);

            HasUnsavedChanges = true;
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

            LambdaE = LayersAbovePipe.LastOrDefault()?.CalculatedLambda ?? 1.6;
        }

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        public void Validate()
        {
            var result = _validator.Validate(CreateValidationModel());

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

        /// <summary>
        /// Применить уже канонический снапшот состояния конструкции к адаптерным
        /// коллекциям и скалярам VM без повторного вызова канонического состояния.
        /// Используется при сбросе/восстановлении проекта (Task 8).
        /// </summary>
        public void ApplyLifecycleSnapshotToAdapter(ConstructionStateSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            _isRefreshing = true;
            try
            {
                GroundwaterLevel = snapshot.GroundwaterLevel;
                HasLoads = snapshot.HasLoads;

                LayersAbovePipe.Clear();
                for (int i = 0; i < snapshot.LayersAbovePipe.Count; i++)
                {
                    LayersAbovePipe.Add(CreateLayerFromSnapshot(snapshot.LayersAbovePipe[i], i));
                }

                LayersBelowPipe.Clear();
                for (int i = 0; i < snapshot.LayersBelowPipe.Count; i++)
                {
                    LayersBelowPipe.Add(CreateLayerFromSnapshot(snapshot.LayersBelowPipe[i], i));
                }

                SelectedLayer = null;
                SelectedTemplate = null;
            }
            finally
            {
                _isRefreshing = false;
            }

            UpdateCalculations();
            HasUnsavedChanges = false;
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Обработчик изменения УГВ
        /// </summary>
        partial void OnGroundwaterLevelChanged(double value)
        {
            if (_isResetting || _isRefreshing) return;

            _isSyncing = true;
            try
            {
                foreach (var layer in LayersBelowPipe)
                {
                    if (!layer.IsLambdaOverridden && layer.Material != null)
                    {
                        layer.CalculatedLambda = value < 1.0
                            ? layer.Material.LambdaB
                            : layer.Material.LambdaA;
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }

            UpdateCalculations();
            HasUnsavedChanges = true;
            SyncStateFromCollections(ConstructionMutationOrigin.User);
        }

        /// <summary>
        /// Обработчик изменения признака нагрузок
        /// </summary>
        partial void OnHasLoadsChanged(bool value)
        {
            if (_isResetting || _isRefreshing) return;

            UpdateCalculations();
            HasUnsavedChanges = true;
            SyncStateFromCollections(ConstructionMutationOrigin.User);
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
        /// Обработчик изменения выбранного шаблона — генерирует предпросмотр слоёв
        /// </summary>
        partial void OnSelectedTemplateChanged(ConstructionTemplate? value)
        {
            if (value == null)
            {
                TemplatePreviewLayersAbovePipe.Clear();
                TemplatePreviewLayersBelowPipe.Clear();
                IsTemplatePreviewExpanded = false;
                CanApplySelectedTemplate = false;
                TemplatePreviewErrorMessage = string.Empty;
                return;
            }

            try
            {
                var previewConstruction = _constructionService.CreateFromTemplate(value, AvailableMaterials);

                TemplatePreviewLayersAbovePipe.Clear();
                foreach (var layer in previewConstruction.LayersAbovePipe)
                {
                    TemplatePreviewLayersAbovePipe.Add(layer);
                }

                TemplatePreviewLayersBelowPipe.Clear();
                foreach (var layer in previewConstruction.Layers)
                {
                    TemplatePreviewLayersBelowPipe.Add(layer);
                }

                CanApplySelectedTemplate = true;
                TemplatePreviewErrorMessage = string.Empty;
            }
            catch (MaterialNotFoundException ex)
            {
                CanApplySelectedTemplate = false;
                TemplatePreviewErrorMessage = $"Материал '{ex.MaterialId}' не найден в справочнике. Применение шаблона невозможно.";
                TemplatePreviewLayersAbovePipe.Clear();
                TemplatePreviewLayersBelowPipe.Clear();
            }
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
            ReconcileLayerSubscriptions();

            if (!_isSyncing && !_isResetting && !_isRefreshing)
            {
                UpdateCalculations();
                HasUnsavedChanges = true;
                SyncStateFromCollections(ConstructionMutationOrigin.User);
            }
        }

        private void ReconcileLayerSubscriptions()
        {
            var currentLayers = new HashSet<Layer>(LayersAbovePipe, ReferenceEqualityComparer.Instance);
            currentLayers.UnionWith(LayersBelowPipe);

            foreach (var staleLayer in _subscribedLayers.Except(currentLayers).ToArray())
            {
                staleLayer.PropertyChanged -= OnSubscribedLayerPropertyChanged;
                _subscribedLayers.Remove(staleLayer);
            }

            foreach (var currentLayer in currentLayers.Except(_subscribedLayers))
            {
                currentLayer.PropertyChanged += OnSubscribedLayerPropertyChanged;
                _subscribedLayers.Add(currentLayer);
            }
        }

        private void OnSubscribedLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnLayerPropertyChanged(sender, e);
        }

        /// <summary>
        /// Обработчик изменения свойств слоя
        /// </summary>
        private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isSyncing || _isResetting || _isRefreshing) return;

            try
            {
                if (sender is not Layer layer)
                {
                    return;
                }

                if (e.PropertyName == nameof(Layer.Material))
                {
                    _pendingMaterialLambdaUpdates.Add(layer);
                    return;
                }

                if (_pendingMaterialLambdaUpdates.Contains(layer))
                {
                    if (e.PropertyName == nameof(Layer.IsLambdaOverridden))
                    {
                        return;
                    }

                    if (e.PropertyName == nameof(Layer.CalculatedLambda))
                    {
                        _pendingMaterialLambdaUpdates.Remove(layer);
                        var previousLambda = layer.CalculatedLambda;
                        layer.UpdateLambda(GroundwaterLevel);
                        if (Math.Abs(previousLambda - layer.CalculatedLambda) > 1e-10)
                        {
                            return;
                        }
                    }
                }

                if (e.PropertyName == nameof(Layer.Thickness) ||
                    e.PropertyName == nameof(Layer.CalculatedLambda) ||
                    e.PropertyName == nameof(Layer.IsLambdaOverridden))
                {
                    UpdateCalculations();
                    HasUnsavedChanges = true;
                    SyncStateFromCollections(ConstructionMutationOrigin.User);
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

        private ConstructionModel CreateValidationModel()
        {
            var candidate = new ConstructionModel
            {
                GroundwaterLevel = GroundwaterLevel,
                HasLoads = HasLoads
            };
            foreach (var layer in LayersAbovePipe)
            {
                candidate.LayersAbovePipe.Add(layer);
            }

            foreach (var layer in LayersBelowPipe)
            {
                candidate.Layers.Add(layer);
            }

            return candidate;
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

        /// <summary>
        /// Обработчик изменений состояния конструкции
        /// </summary>
        private void OnConstructionStateChanged(object? sender, EventArgs e)
        {
            // User edits already originate in this adapter; lifecycle snapshots
            // refresh it explicitly through ApplyLifecycleSnapshotToAdapter.
        }

        /// <summary>
        /// Compatibility seam for explicitly synchronizing the current adapter
        /// collections/scalars to canonical ConstructionState.
        /// </summary>
        public void SyncToCanonicalState()
        {
            SyncStateFromCollections(ConstructionMutationOrigin.SystemApply);
        }

        /// <summary>
        /// Copies current VM collections into canonical state. The origin determines
        /// authoritative dirty and downstream completion semantics.
        /// </summary>
        private void SyncStateFromCollections(ConstructionMutationOrigin origin)
        {
            if (_isRefreshing || _isSyncing || _isResetting) return;

            var above = LayersAbovePipe.Select((l, i) => new ConstructionLayerSnapshot(
                l.Id,
                l.Material?.Id ?? 0,
                l.Material?.Name ?? string.Empty,
                l.Thickness,
                l.CalculatedLambda,
                l.IsLambdaOverridden,
                LayerPosition.AbovePipe,
                i)).ToArray();

            var below = LayersBelowPipe.Select((l, i) => new ConstructionLayerSnapshot(
                l.Id,
                l.Material?.Id ?? 0,
                l.Material?.Name ?? string.Empty,
                l.Thickness,
                l.CalculatedLambda,
                l.IsLambdaOverridden,
                LayerPosition.BelowPipe,
                i)).ToArray();

            var candidate = new ConstructionStateSnapshot(GroundwaterLevel, HasLoads, above, below);
            _constructionState.ApplySnapshot(candidate, origin);
        }

        /// <summary>
        /// Создаёт адаптерный <see cref="Layer"/> из канонического снапшота слоя,
        /// сохраняя данные и нормализуя порядок. Материал резолвится из каталога
        /// по Id, затем по имени; при недоступности используется дефолтный материал
        /// с сохранением λ и форсированным переопределением (стиль RebindLayerMaterials).
        /// </summary>
        private Layer CreateLayerFromSnapshot(ConstructionLayerSnapshot snap, int normalizedOrder)
        {
            var material = AvailableMaterials.FirstOrDefault(m => m.Id == snap.MaterialId)
                ?? AvailableMaterials.FirstOrDefault(m => string.Equals(m.Name, snap.MaterialName, StringComparison.OrdinalIgnoreCase));

            if (material == null)
            {
                var fallback = Material.GetDefaultMaterial();
                material = AvailableMaterials.FirstOrDefault(m => m.Id == fallback.Id) ?? fallback;
            }

            var layer = new Layer
            {
                Id = snap.Id,
                Material = material,
                Thickness = snap.Thickness,
                CalculatedLambda = snap.CalculatedLambda,
                IsLambdaOverridden = snap.IsLambdaOverridden,
                Position = snap.Position,
                Order = normalizedOrder
            };

            // Материал снапшота недоступен в каталоге: сохраняем λ и форсируем
            // переопределение, чтобы данные не потерялись.
            if (material.Id != snap.MaterialId)
            {
                layer.CalculatedLambda = snap.CalculatedLambda;
                layer.IsLambdaOverridden = true;
            }

            return layer;
        }

        #endregion
    }
}
