# Task 4.3: CollectorViewModel (ViewModel коллектора)

**Этап:** 4 - ViewModels  
**Приоритет:** Средний  
**Статус:** Не начато  
**Зависимости:** Task 4.1

---

## 1. Цель задачи

Создать ViewModel для выбора и отображения коллектора.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Подбор коллектора РЕХАУ | Выбор коллектора |

---

## 3. Создаваемые файлы

### 3.1. CollectorViewModel.cs

**Путь:** `src/ViewModels/Hydraulics/CollectorViewModel.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// ViewModel для выбора и отображения коллектора РЕХАУ
    /// </summary>
    public partial class CollectorViewModel : ObservableObject
    {
        #region Services

        private readonly ICollectorRepository _collectorRepository;

        #endregion

        #region Observable Properties

        /// <summary>
        /// Список доступных коллекторов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Collector> _availableCollectors = new ObservableCollection<Collector>();

        /// <summary>
        /// Выбранный коллектор
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorInfo))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorName))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorDiameter))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorKv))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorMaxFlow))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorMaxPressure))]
        [NotifyPropertyChangedFor(nameof(CanShowDetails))]
        private Collector _selectedCollector;

        /// <summary>
        /// Тип коллектора для фильтрации
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredCollectors))]
        private CollectorType _selectedCollectorType = CollectorType.HKV;

        /// <summary>
        /// Количество контуров для подбора
        /// </summary>
        [ObservableProperty]
        private int _circuitCount = 4;

        /// <summary>
        /// Общий расход (л/ч)
        /// </summary>
        [ObservableProperty]
        private double _totalFlowRate;

        /// <summary>
        /// Признак загрузки
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        [ObservableProperty]
        private string _errorMessage;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Отфильтрованный список коллекторов по типу
        /// </summary>
        public ObservableCollection<Collector> FilteredCollectors
        {
            get
            {
                var filtered = AvailableCollectors
                    .Where(c => c.Type == SelectedCollectorType)
                    .ToList();

                return new ObservableCollection<Collector>(filtered);
            }
        }

        /// <summary>
        /// Признак возможности отображения деталей
        /// </summary>
        public bool CanShowDetails => SelectedCollector != null;

        /// <summary>
        /// Информация о выбранном коллекторе
        /// </summary>
        public string SelectedCollectorInfo => SelectedCollector?.Description ?? "Коллектор не выбран";

        /// <summary>
        /// Название выбранного коллектора
        /// </summary>
        public string SelectedCollectorName => SelectedCollector?.Name ?? "—";

        /// <summary>
        /// Диаметр выбранного коллектора
        /// </summary>
        public string SelectedCollectorDiameter => SelectedCollector?.NominalDiameter ?? "—";

        /// <summary>
        /// Kv выбранного коллектора
        /// </summary>
        public string SelectedCollectorKv => SelectedCollector != null 
            ? $"{SelectedCollector.KvValue:F2} м³/ч" 
            : "—";

        /// <summary>
        /// Максимальный расход выбранного коллектора
        /// </summary>
        public string SelectedCollectorMaxFlow => SelectedCollector != null 
            ? $"{SelectedCollector.MaxFlowRate:F0} л/ч" 
            : "—";

        /// <summary>
        /// Максимальное давление выбранного коллектора
        /// </summary>
        public string SelectedCollectorMaxPressure => SelectedCollector != null 
            ? $"{SelectedCollector.MaxPressure:F0} кПа" 
            : "—";

        /// <summary>
        /// Доступные количества контуров для HKV
        /// </summary>
        public int[] AvailableCircuitCountsHKV => new[] { 2, 4, 6, 8, 10, 12 };

        /// <summary>
        /// Доступные типы коллекторов
        /// </summary>
        public CollectorType[] AvailableCollectorTypes => new[] { CollectorType.HKV, CollectorType.IV };

        #endregion

        #region Commands

        /// <summary>
        /// Команда загрузки коллекторов
        /// </summary>
        [RelayCommand]
        private async Task LoadCollectorsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var collectors = await _collectorRepository.GetAllAsync();

                AvailableCollectors.Clear();
                foreach (var collector in collectors)
                {
                    AvailableCollectors.Add(collector);
                }

                OnPropertyChanged(nameof(FilteredCollectors));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки коллекторов: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Команда подбора коллектора
        /// </summary>
        [RelayCommand]
        private async Task SelectCollectorAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var collector = await _collectorRepository.SelectCollectorAsync(
                    SelectedCollectorType,
                    CircuitCount,
                    TotalFlowRate);

                if (collector != null)
                {
                    SelectedCollector = collector;
                }
                else
                {
                    ErrorMessage = "Не найден подходящий коллектор для заданных параметров";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка подбора коллектора: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Команда выбора коллектора из списка
        /// </summary>
        [RelayCommand]
        private void SelectCollectorFromList(Collector collector)
        {
            if (collector != null)
            {
                SelectedCollector = collector;
            }
        }

        /// <summary>
        /// Команда сброса выбора
        /// </summary>
        [RelayCommand]
        private void ClearSelection()
        {
            SelectedCollector = null;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Команда фильтрации по типу
        /// </summary>
        [RelayCommand]
        private void FilterByType(CollectorType type)
        {
            SelectedCollectorType = type;
            OnPropertyChanged(nameof(FilteredCollectors));
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор для дизайнера
        /// </summary>
        public CollectorViewModel() : this(null)
        {
        }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        public CollectorViewModel(ICollectorRepository collectorRepository)
        {
            _collectorRepository = collectorRepository ?? new CollectorRepository();

            // Загрузка коллекторов при создании
            _ = LoadCollectorsAsync();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Установить параметры для подбора
        /// </summary>
        public void SetSelectionParameters(int circuitCount, double totalFlowRate)
        {
            CircuitCount = circuitCount;
            TotalFlowRate = totalFlowRate;
        }

        /// <summary>
        /// Получить выбранный коллектор
        /// </summary>
        public Collector GetSelectedCollector()
        {
            return SelectedCollector;
        }

        /// <summary>
        /// Проверить совместимость коллектора с параметрами
        /// </summary>
        public bool IsCollectorCompatible(Collector collector, int circuitCount, double totalFlowRate)
        {
            if (collector == null)
                return false;

            // Проверка количества контуров
            if (collector.CircuitCount < circuitCount)
                return false;

            // Проверка расхода
            if (collector.MaxFlowRate < totalFlowRate)
                return false;

            return true;
        }

        /// <summary>
        /// Получить рекомендацию по коллектору
        /// </summary>
        public string GetRecommendation()
        {
            if (SelectedCollector == null)
                return "Выполните подбор коллектора";

            if (TotalFlowRate > SelectedCollector.MaxFlowRate)
                return $"Внимание: расход ({TotalFlowRate:F0} л/ч) превышает максимальный для коллектора ({SelectedCollector.MaxFlowRate:F0} л/ч)";

            if (CircuitCount > SelectedCollector.CircuitCount)
                return $"Внимание: количество контуров ({CircuitCount}) превышает количество выходов коллектора ({SelectedCollector.CircuitCount})";

            double utilizationRate = TotalFlowRate / SelectedCollector.MaxFlowRate * 100;

            if (utilizationRate < 30)
                return $"Рекомендация: загрузка коллектора {utilizationRate:F0}% — рассмотрите коллектор меньшего размера";

            if (utilizationRate > 80)
                return $"Рекомендация: загрузка коллектора {utilizationRate:F0}% — рассмотрите коллектор большего размера";

            return $"Коллектор подобран корректно. Загрузка: {utilizationRate:F0}%";
        }

        #endregion

        #region PropertyChanged Handlers

        /// <summary>
        /// Обработчик изменения типа коллектора
        /// </summary>
        partial void OnSelectedCollectorTypeChanged(CollectorType value)
        {
            OnPropertyChanged(nameof(FilteredCollectors));
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/ViewModels/Hydraulics/CollectorViewModelTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    [TestFixture]
    public class CollectorViewModelTests
    {
        private Mock<ICollectorRepository> _repositoryMock;
        private CollectorViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ICollectorRepository>();

            // Настройка мока для возврата тестовых данных
            _repositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(GetTestCollectors());

            _repositoryMock
                .Setup(r => r.SelectCollectorAsync(It.IsAny<CollectorType>(), It.IsAny<int>(), It.IsAny<double>()))
                .ReturnsAsync((CollectorType type, int circuits, double flow) =>
                {
                    return GetTestCollectors()
                        .FirstOrDefault(c => c.Type == type && c.CircuitCount >= circuits && c.MaxFlowRate >= flow);
                });

            _viewModel = new CollectorViewModel(_repositoryMock.Object);
        }

        private List<Collector> GetTestCollectors()
        {
            return new List<Collector>
            {
                new Collector
                {
                    Id = 1,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 4",
                    CircuitCount = 4,
                    KvValue = 1.2,
                    MaxFlowRate = 800,
                    MaxPressure = 600
                },
                new Collector
                {
                    Id = 2,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 6",
                    CircuitCount = 6,
                    KvValue = 1.2,
                    MaxFlowRate = 1200,
                    MaxPressure = 600
                },
                new Collector
                {
                    Id = 3,
                    Type = CollectorType.IV,
                    Name = "IV DN25",
                    CircuitCount = 1,
                    KvValue = 1.45,
                    MaxFlowRate = 500,
                    MaxPressure = 1000
                }
            };
        }

        [Test]
        public async Task LoadCollectorsAsync_LoadsCollectors()
        {
            // Act
            await _viewModel.LoadCollectorsCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.AvailableCollectors.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task SelectCollectorAsync_SelectsCorrectCollector()
        {
            // Arrange
            _viewModel.SelectedCollectorType = CollectorType.HKV;
            _viewModel.CircuitCount = 4;
            _viewModel.TotalFlowRate = 600;

            // Act
            await _viewModel.SelectCollectorCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.SelectedCollector, Is.Not.Null);
            Assert.That(_viewModel.SelectedCollector.Type, Is.EqualTo(CollectorType.HKV));
            Assert.That(_viewModel.SelectedCollector.CircuitCount, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void FilterByType_FiltersCorrectly()
        {
            // Arrange
            _viewModel.AvailableCollectors = new System.Collections.ObjectModel.ObservableCollection<Collector>(GetTestCollectors());

            // Act
            _viewModel.FilterByTypeCommand.Execute(CollectorType.HKV);

            // Assert
            Assert.That(_viewModel.FilteredCollectors.All(c => c.Type == CollectorType.HKV), Is.True);
        }

        [Test]
        public void SelectCollectorFromList_SetsSelectedCollector()
        {
            // Arrange
            var collector = GetTestCollectors()[0];

            // Act
            _viewModel.SelectCollectorFromListCommand.Execute(collector);

            // Assert
            Assert.That(_viewModel.SelectedCollector, Is.EqualTo(collector));
        }

        [Test]
        public void ClearSelection_ClearsSelectedCollector()
        {
            // Arrange
            _viewModel.SelectedCollector = GetTestCollectors()[0];

            // Act
            _viewModel.ClearSelectionCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.SelectedCollector, Is.Null);
        }

        [Test]
        public void SetSelectionParameters_SetsValues()
        {
            // Act
            _viewModel.SetSelectionParameters(6, 1000);

            // Assert
            Assert.That(_viewModel.CircuitCount, Is.EqualTo(6));
            Assert.That(_viewModel.TotalFlowRate, Is.EqualTo(1000));
        }

        [Test]
        public void IsCollectorCompatible_ReturnsTrueForCompatible()
        {
            // Arrange
            var collector = new Collector
            {
                CircuitCount = 6,
                MaxFlowRate = 1200
            };

            // Act
            bool isCompatible = _viewModel.IsCollectorCompatible(collector, 4, 800);

            // Assert
            Assert.That(isCompatible, Is.True);
        }

        [Test]
        public void IsCollectorCompatible_ReturnsFalseForIncompatible()
        {
            // Arrange
            var collector = new Collector
            {
                CircuitCount = 4,
                MaxFlowRate = 800
            };

            // Act
            bool isCompatible = _viewModel.IsCollectorCompatible(collector, 6, 1000);

            // Assert
            Assert.That(isCompatible, Is.False);
        }

        [Test]
        public void GetRecommendation_ReturnsMessageWhenNoSelection()
        {
            // Arrange
            _viewModel.SelectedCollector = null;

            // Act
            var recommendation = _viewModel.GetRecommendation();

            // Assert
            Assert.That(recommendation, Does.Contain("подбор"));
        }

        [Test]
        public void GetRecommendation_ReturnsWarningWhenFlowExceeded()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { MaxFlowRate = 500 };
            _viewModel.TotalFlowRate = 600;

            // Act
            var recommendation = _viewModel.GetRecommendation();

            // Assert
            Assert.That(recommendation, Does.Contain("превышает"));
        }

        [Test]
        public void SelectedCollectorInfo_ReturnsDescription()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector
            {
                Description = "Тестовый коллектор"
            };

            // Assert
            Assert.That(_viewModel.SelectedCollectorInfo, Is.EqualTo("Тестовый коллектор"));
        }

        [Test]
        public void SelectedCollectorKv_FormatsCorrectly()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { KvValue = 1.234 };

            // Assert
            Assert.That(_viewModel.SelectedCollectorKv, Is.EqualTo("1.23 м³/ч"));
        }

        [Test]
        public void SelectedCollectorMaxFlow_FormatsCorrectly()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { MaxFlowRate = 800 };

            // Assert
            Assert.That(_viewModel.SelectedCollectorMaxFlow, Is.EqualTo("800 л/ч"));
        }

        [Test]
        public void CanShowDetails_ReturnsFalseWhenNoSelection()
        {
            // Arrange
            _viewModel.SelectedCollector = null;

            // Assert
            Assert.That(_viewModel.CanShowDetails, Is.False);
        }

        [Test]
        public void CanShowDetails_ReturnsTrueWhenSelected()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector();

            // Assert
            Assert.That(_viewModel.CanShowDetails, Is.True);
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `CollectorViewModel.cs` создан
- [ ] MVVM паттерн реализован (CommunityToolkit.Mvvm)
- [ ] Все свойства реализованы
- [ ] Команды LoadCollectors, SelectCollector, ClearSelection работают
- [ ] Фильтрация по типу работает
- [ ] Подбор коллектора по параметрам работает
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов

---

## 6. Примечания

- Используется CommunityToolkit.Mvvm для MVVM
- Интеграция с CollectorRepository через DI
- Автоматическая загрузка коллекторов при создании
- Вычисляемые свойства для отображения в UI
- Рекомендации по загрузке коллектора