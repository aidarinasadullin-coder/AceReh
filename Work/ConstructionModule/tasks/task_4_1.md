# Task 4.1: Создать ConstructionViewModel.cs (базовая структура)

**Этап:** 4. ViewModel  
**Приоритет:** P0 (Критическая)  
**Время:** 2 часа  
**Зависимости:** Task 1.5, Task 2.1, Task 3.1

---

## 1. Цель задачи

Создать `ConstructionViewModel` — MVVM ViewModel для управления конструктором конструкции.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-01 | Добавление слоя материала | AddLayerAbovePipeCommand, AddLayerBelowPipeCommand |
| UC-02 | Выбор материала из справочника | AvailableMaterials |
| UC-03 | Задание толщины слоя | Thickness (в Layer) |
| UC-04 | Удаление слоя | RemoveLayerCommand |
| UC-05 | Учёт уровня грунтовых вод | GroundwaterLevel |
| UC-09 | Интеграция с ThermalViewModel | DataChanged |

---

## 3. Описание изменений

### 3.1. Создать файл ConstructionViewModel.cs

**Путь:** `src/ViewModels/Construction/ConstructionViewModel.cs`

**Код:**

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Construction;

namespace SnowMeltingCalculator.ViewModels.Construction
{
    /// <summary>
    /// ViewModel для модуля "Конструктор конструкции" ("Пирог")
    /// </summary>
    public partial class ConstructionViewModel : ObservableObject
    {
        #region Поля

        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionService _constructionService;
        private readonly IConstructionRepository _constructionRepository;
        private readonly ConstructionValidator _validator;
        private readonly Construction _construction;

        #endregion

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
        /// Уровень грунтовых вод, м
        /// </summary>
        [ObservableProperty]
        private double _groundwaterLevel = 2.0;

        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        [ObservableProperty]
        private bool _hasLoads = false;

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
        /// Признак загрузки данных
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Суммарное термическое сопротивление над трубой, м²·К/Вт
        /// </summary>
        public double R1Total => _construction.R1Total;

        /// <summary>
        /// Суммарное термическое сопротивление под трубой, м²·К/Вт
        /// </summary>
        public double R2Total => _construction.R2Total;

        /// <summary>
        /// Теплопроводность стяжки вокруг трубы, Вт/м·К
        /// </summary>
        public double LambdaE => _construction.LambdaE;

        /// <summary>
        /// Материал по умолчанию
        /// </summary>
        public Material? DefaultMaterial => _availableMaterials.FirstOrDefault();

        #endregion

        #region Конструктор

        /// <summary>
        /// Создать ViewModel
        /// </summary>
        public ConstructionViewModel(
            IMaterialRepository materialRepository,
            IConstructionService constructionService,
            IConstructionRepository constructionRepository,
            ConstructionValidator validator,
            Construction construction)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _constructionRepository = constructionRepository ?? throw new ArgumentNullException(nameof(constructionRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));

            // Подписка на изменения в модели
            _construction.DataChanged += OnConstructionDataChanged;

            // Инициализация
            InitializeAsync().ConfigureAwait(false);
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Асинхронная инициализация
        /// </summary>
        private async Task InitializeAsync()
        {
            IsLoading = true;

            try
            {
                // Загрузка материалов
                var materials = await _materialRepository.LoadMaterialsAsync();
                AvailableMaterials = new ObservableCollection<Material>(materials);

                // Синхронизация с моделью
                SyncWithModel();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка загрузки материалов: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Синхронизация с моделью Construction
        /// </summary>
        private void SyncWithModel()
        {
            // Слои над трубой
            LayersAbovePipe = new ObservableCollection<Layer>(_construction.LayersAbovePipe);
            
            // Слои под трубой
            LayersBelowPipe = new ObservableCollection<Layer>(_construction.LayersBelowPipe);

            // Параметры
            GroundwaterLevel = _construction.GroundwaterLevel;
            HasLoads = _construction.HasLoads;

            // Уведомление об изменении вычисляемых свойств
            OnPropertyChanged(nameof(R1Total));
            OnPropertyChanged(nameof(R2Total));
            OnPropertyChanged(nameof(LambdaE));
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда: Добавить слой над трубой
        /// </summary>
        [RelayCommand]
        private void AddLayerAbovePipe()
        {
            if (DefaultMaterial == null)
            {
                ValidationMessage = "Материалы не загружены";
                return;
            }

            var layer = _constructionService.CreateDefaultLayer(LayerPosition.AbovePipe, GroundwaterLevel);
            _construction.AddLayerAbovePipe(layer.Material, layer.Thickness);
            
            // Синхронизация
            LayersAbovePipe.Add(layer);
            Validate();
        }

        /// <summary>
        /// Команда: Добавить слой под трубой
        /// </summary>
        [RelayCommand]
        private void AddLayerBelowPipe()
        {
            if (DefaultMaterial == null)
            {
                ValidationMessage = "Материалы не загружены";
                return;
            }

            var layer = _constructionService.CreateDefaultLayer(LayerPosition.BelowPipe, GroundwaterLevel);
            _construction.AddLayerBelowPipe(layer.Material, layer.Thickness);
            
            // Синхронизация
            LayersBelowPipe.Add(layer);
            Validate();
        }

        /// <summary>
        /// Команда: Удалить слой
        /// </summary>
        [RelayCommand]
        private void RemoveLayer(Layer layer)
        {
            if (layer == null) return;

            _construction.RemoveLayer(layer);

            // Синхронизация
            if (layer.Position == LayerPosition.AbovePipe)
            {
                LayersAbovePipe.Remove(layer);
            }
            else
            {
                LayersBelowPipe.Remove(layer);
            }

            Validate();
        }

        /// <summary>
        /// Команда: Сохранить конструкцию
        /// </summary>
        [RelayCommand]
        private async Task SaveConstructionAsync()
        {
            // TODO: Реализовать сохранение
            await Task.CompletedTask;
        }

        /// <summary>
        /// Команда: Загрузить конструкцию
        /// </summary>
        [RelayCommand]
        private async Task LoadConstructionAsync()
        {
            // TODO: Реализовать загрузку
            await Task.CompletedTask;
        }

        /// <summary>
        /// Команда: Применить шаблон
        /// </summary>
        [RelayCommand]
        private void ApplyTemplate(ConstructionTemplate template)
        {
            if (template == null) return;

            var newConstruction = _constructionService.ApplyTemplate(template);
            
            // Очистка текущей конструкции
            _construction.Clear();
            
            // Добавление слоёв из шаблона
            foreach (var layer in newConstruction.LayersAbovePipe)
            {
                _construction.AddLayerAbovePipe(layer.Material, layer.Thickness);
            }
            foreach (var layer in newConstruction.LayersBelowPipe)
            {
                _construction.AddLayerBelowPipe(layer.Material, layer.Thickness);
            }

            // Обновление параметров
            _construction.HasLoads = newConstruction.HasLoads;
            _construction.GroundwaterLevel = newConstruction.GroundwaterLevel;

            // Синхронизация
            SyncWithModel();
            Validate();
        }

        #endregion

        #region Обработчики изменений

        /// <summary>
        /// Обработчик изменения УГВ
        /// </summary>
        partial void OnGroundwaterLevelChanged(double value)
        {
            _construction.GroundwaterLevel = value;
            _construction.UpdateLambdaForGroundwater();
            Validate();
        }

        /// <summary>
        /// Обработчик изменения флага нагрузок
        /// </summary>
        partial void OnHasLoadsChanged(bool value)
        {
            _construction.HasLoads = value;
            Validate();
        }

        /// <summary>
        /// Обработчик изменения данных конструкции
        /// </summary>
        private void OnConstructionDataChanged(object? sender, ConstructionDataChangedEventArgs e)
        {
            // Уведомление об изменении вычисляемых свойств
            OnPropertyChanged(nameof(R1Total));
            OnPropertyChanged(nameof(R2Total));
            OnPropertyChanged(nameof(LambdaE));
            OnPropertyChanged(nameof(IsValid));

            // Обновление валидации
            IsValid = e.IsValid;
        }

        #endregion

        #region Валидация

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        private void Validate()
        {
            var result = _validator.Validate(_construction);
            
            IsValid = result.IsValid;
            ValidationMessage = result.IsValid ? string.Empty : result.GetErrorMessage();
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### TC-4.1.1: Инициализация ViewModel

```csharp
[Fact]
public async Task ConstructionViewModel_Initialize_ShouldLoadMaterials()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var constructionService = new ConstructionService(materialRepo);
    var constructionRepo = new ConstructionRepository(materialRepo);
    var validator = new ConstructionValidator();
    var construction = new Construction();

    // Act
    var viewModel = new ConstructionViewModel(
        materialRepo, constructionService, constructionRepo, validator, construction);

    // Ждём инициализации
    await Task.Delay(100);

    // Assert
    Assert.NotEmpty(viewModel.AvailableMaterials);
    Assert.NotNull(viewModel.DefaultMaterial);
}
```

### TC-4.1.2: Добавление слоя над трубой

```csharp
[Fact]
public async Task ConstructionViewModel_AddLayerAbovePipe_ShouldAddLayer()
{
    // Arrange
    var viewModel = CreateViewModel();
    await Task.Delay(100);

    // Act
    viewModel.AddLayerAbovePipeCommand.Execute(null);

    // Assert
    Assert.Single(viewModel.LayersAbovePipe);
}
```

### TC-4.1.3: Удаление слоя

```csharp
[Fact]
public async Task ConstructionViewModel_RemoveLayer_ShouldRemoveLayer()
{
    // Arrange
    var viewModel = CreateViewModel();
    await Task.Delay(100);
    viewModel.AddLayerAbovePipeCommand.Execute(null);
    var layer = viewModel.LayersAbovePipe[0];

    // Act
    viewModel.RemoveLayerCommand.Execute(layer);

    // Assert
    Assert.Empty(viewModel.LayersAbovePipe);
}
```

### TC-4.1.4: Изменение УГВ

```csharp
[Fact]
public async Task ConstructionViewModel_OnGroundwaterLevelChanged_ShouldUpdateLambda()
{
    // Arrange
    var viewModel = CreateViewModel();
    await Task.Delay(100);
    viewModel.AddLayerBelowPipeCommand.Execute(null);
    var layer = viewModel.LayersBelowPipe[0];
    var lambdaBefore = layer.Lambda;

    // Act
    viewModel.GroundwaterLevel = 0.5; // Влажные условия

    // Assert
    Assert.NotEqual(lambdaBefore, layer.Lambda);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/ViewModels/Construction/ConstructionViewModel.cs` создан
- [ ] ViewModel наследует от `ObservableObject`
- [ ] Свойства `LayersAbovePipe`, `LayersBelowPipe`, `AvailableMaterials` — ObservableCollection
- [ ] Команды `AddLayerAbovePipeCommand`, `AddLayerBelowPipeCommand`, `RemoveLayerCommand` работают
- [ ] Обработчики `OnGroundwaterLevelChanged`, `OnHasLoadsChanged` работают
- [ ] Событие `DataChanged` пробрасывается из `Construction`
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Использовать `CommunityToolkit.Mvvm` для MVVM
- `[ObservableProperty]` генерирует свойства с уведомлениями
- `[RelayCommand]` генерирует команды
- Синхронизация с моделью `Construction` через события

---

**Конец документа**