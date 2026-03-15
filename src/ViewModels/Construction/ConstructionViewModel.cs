using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
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
        private readonly ConstructionValidator _validator;
        private readonly ConstructionModel _construction;
        private bool _isSyncing; // Флаг для предотвращения рекурсии при синхронизации

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
            ConstructionModel construction)
        {
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionRepository = constructionRepository ?? throw new ArgumentNullException(nameof(constructionRepository));
            _validator = new ConstructionValidator();
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));

            // Подписываемся на изменения коллекций
            LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;
            LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;
            
            // Подписываемся на изменения в модели Construction
            _construction.DataChanged += OnConstructionDataChanged;
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
                // Загружаем материалы
                var materials = await _materialRepository.LoadMaterialsAsync();
                AvailableMaterials.Clear();
                foreach (var material in materials)
                {
                    AvailableMaterials.Add(material);
                }

                // Загружаем шаблоны
                var templates = ConstructionTemplate.GetDefaultTemplates();
                Templates.Clear();
                foreach (var template in templates)
                {
                    Templates.Add(template);
                }

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
            if (AvailableMaterials.Count == 0) return;

            var defaultMaterial = AvailableMaterials.FirstOrDefault(m => m.Id == 5) ?? AvailableMaterials.First();
            var layer = new Layer
            {
                Material = defaultMaterial,
                Thickness = 50,
                CalculatedLambda = defaultMaterial.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = LayersAbovePipe.Count
            };

            LayersAbovePipe.Add(layer);
            UpdateCalculations();
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// Команда добавления слоя под трубой
        /// </summary>
        [RelayCommand]
        private void AddLayerBelowPipe()
        {
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
        }

        /// <summary>
        /// Команда удаления слоя
        /// </summary>
        [RelayCommand]
        private void RemoveLayer(Layer? layer)
        {
            if (layer == null) return;

            if (layer.Position == LayerPosition.AbovePipe)
            {
                LayersAbovePipe.Remove(layer);
                // Пересчитываем порядок
                for (int i = 0; i < LayersAbovePipe.Count; i++)
                {
                    LayersAbovePipe[i].Order = i;
                }
            }
            else
            {
                LayersBelowPipe.Remove(layer);
                // Пересчитываем порядок
                for (int i = 0; i < LayersBelowPipe.Count; i++)
                {
                    LayersBelowPipe[i].Order = i;
                }
            }

            SelectedLayer = null;
            UpdateCalculations();
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// Команда применения шаблона
        /// </summary>
        [RelayCommand]
        private void ApplyTemplate()
        {
            if (SelectedTemplate == null) return;

            try
            {
                // Создаём новую конструкцию из шаблона
                var newConstruction = _constructionService.CreateFromTemplate(
                    SelectedTemplate,
                    AvailableMaterials);

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
                GroundwaterLevel = SelectedTemplate.DefaultGroundwaterLevel;
                HasLoads = SelectedTemplate.HasLoads;

                // Обновляем УГВ опцию
                SelectedGroundwaterOption = GroundwaterLevel < 1.0
                    ? "УГВ < 1 м (влажные условия)"
                    : "УГВ >= 1 м (сухие условия)";

                UpdateCalculations();
                HasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка применения шаблона: {ex.Message}";
                IsValid = false;
            }
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
            try
            {
                var filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "SnowMeltingCalculator",
                    "construction_last.json");

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
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка загрузки: {ex.Message}";
                IsValid = false;
            }
        }

        /// <summary>
        /// Команда сброса к значениям по умолчанию
        /// </summary>
        [RelayCommand]
        private void ResetToDefault()
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

            // Рассчитываем LambdaE
            var firstLayerAbove = LayersAbovePipe.FirstOrDefault();
            LambdaE = firstLayerAbove?.Material?.LambdaA ?? 1.6;

            // Обновляем свойства для UI
            OnPropertyChanged(nameof(TotalThicknessAbovePipe));
            OnPropertyChanged(nameof(TotalThicknessBelowPipe));

            // Валидация
            Validate();

            // Уведомляем об изменении данных
            OnDataChanged();
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
            messages.AddRange(result.Errors);
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
        }

        /// <summary>
        /// Обработчик изменения признака нагрузок
        /// </summary>
        partial void OnHasLoadsChanged(bool value)
        {
            UpdateCalculations();
            HasUnsavedChanges = true;
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Обработчик изменения коллекции слоёв
        /// </summary>
        private void OnLayersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_isSyncing)
            {
                UpdateCalculations();
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