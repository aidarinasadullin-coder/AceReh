# Task 2.3: ICollectorRepository (Интерфейс репозитория коллекторов)

**Этап:** 2 - Interfaces  
**Приоритет:** Средний  
**Статус:** Completed  
**Зависимости:** Task 1.1, Task 1.4

---

## 1. Цель задачи

Создать интерфейс `ICollectorRepository` — контракт для репозитория коллекторов РЕХАУ.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Подбор коллектора РЕХАУ | Все методы интерфейса |

---

## 3. Создаваемые файлы

### 3.1. ICollectorRepository.cs

**Путь:** `src/Repositories/Hydraulics/ICollectorRepository.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Repositories.Hydraulics
{
    /// <summary>
    /// Интерфейс репозитория коллекторов РЕХАУ
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для работы с данными о коллекторах:
    /// - Получение списка коллекторов
    /// - Поиск по идентификатору
    /// - Фильтрация по типу
    /// - Подбор по количеству контуров
    /// 
    /// Данные загружаются из data/rehau_products.json
    /// 
    /// Поддерживаемые коллекторы:
    /// - HKV-D (бытовой): 2, 4, 6, 8, 10, 12 контуров
    /// - IV (промышленный): DN25 (1¼"), DN40 (1½")
    /// </remarks>
    public interface ICollectorRepository
    {
        /// <summary>
        /// Получить все коллекторы
        /// </summary>
        /// <returns>Список всех коллекторов</returns>
        /// <remarks>
        /// Загружает данные из data/rehau_products.json
        /// </remarks>
        Task<IEnumerable<Collector>> GetAllAsync();
        
        /// <summary>
        /// Получить коллектор по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор коллектора</param>
        /// <returns>Коллектор или null, если не найден</returns>
        /// <remarks>
        /// Идентификаторы:
        /// - "HKV-D-2", "HKV-D-4", ..., "HKV-D-12"
        /// - "IV-1.25", "IV-1.5"
        /// </remarks>
        Task<Collector?> GetByIdAsync(string id);
        
        /// <summary>
        /// Получить коллекторы по типу
        /// </summary>
        /// <param name="type">Тип коллектора (HKV или IV)</param>
        /// <returns>Список коллекторов указанного типа</returns>
        /// <remarks>
        /// Фильтрация по CollectorType:
        /// - CollectorType.HKV — бытовые коллекторы
        /// - CollectorType.IV — промышленные коллекторы
        /// </remarks>
        Task<IEnumerable<Collector>> GetByTypeAsync(CollectorType type);
        
        /// <summary>
        /// Получить коллектор по количеству контуров
        /// </summary>
        /// <param name="circuits">Количество контуров</param>
        /// <returns>Коллектор или null, если не найден</returns>
        /// <remarks>
        /// Для HKV-D:
        /// - 2 контура → HKV-D-2
        /// - 4 контура → HKV-D-4
        /// - и т.д.
        /// 
        /// Для IV: возвращает первый доступный промышленный коллектор
        /// </remarks>
        Task<Collector?> GetByCircuitsAsync(int circuits);
        
        /// <summary>
        /// Подобрать коллектор для заданного количества контуров и расхода
        /// </summary>
        /// <param name="circuits">Количество контуров</param>
        /// <param name="totalFlowRate_m3_h">Суммарный расход, м³/ч</param>
        /// <returns>Рекомендуемый коллектор или null, если не найден</returns>
        /// <remarks>
        /// Алгоритм подбора:
        /// 1. Если circuits ≤ 12: подобрать HKV-D
        /// 2. Проверить ограничение по расходу (≤ MaxFlowRate)
        /// 3. Если не подходит: предложить IV
        /// 
        /// Ограничения:
        /// - HKV-D: макс. 12 контуров, макс. 1.5 м³/ч, макс. 320 мбар
        /// </remarks>
        Collector? SelectCollector(int circuits, double totalFlowRate_m3_h);
        
        /// <summary>
        /// Получить список доступных количеств контуров для HKV-D
        /// </summary>
        /// <returns>Список количеств контуров: 2, 4, 6, 8, 10, 12</returns>
        IEnumerable<int> GetAvailableCircuitCounts();
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданных параметров
        /// </summary>
        /// <param name="collector">Коллектор</param>
        /// <param name="circuits">Количество контуров</param>
        /// <param name="totalFlowRate_m3_h">Суммарный расход, м³/ч</param>
        /// <param name="pressure_mbar">Давление, мбар</param>
        /// <returns>true, если коллектор подходит</returns>
        /// <remarks>
        /// Проверка ограничений:
        /// - Количество контуров ≤ Circuits
        /// - Расход ≤ MaxFlowRate
        /// - Давление ≤ MaxPressure
        /// </remarks>
        bool IsCollectorSuitable(
            Collector collector, 
            int circuits, 
            double totalFlowRate_m3_h, 
            double pressure_mbar);
        
        /// <summary>
        /// Получить максимальное количество контуров для HKV-D
        /// </summary>
        /// <returns>Максимальное количество контуров (12)</returns>
        int GetMaxCircuitsForHKV();
        
        /// <summary>
        /// Получить максимальный расход для HKV-D
        /// </summary>
        /// <returns>Максимальный расход, м³/ч (1.5)</returns>
        double GetMaxFlowRateForHKV();
        
        /// <summary>
        /// Получить максимальное давление для HKV-D
        /// </summary>
        /// <returns>Максимальное давление, мбар (320)</returns>
        double GetMaxPressureForHKV();
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты (интерфейс)

**Файл:** `tests/Repositories/Hydraulics/ICollectorRepositoryTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Repositories.Hydraulics
{
    [TestFixture]
    public class ICollectorRepositoryTests
    {
        private Mock<ICollectorRepository> _repositoryMock;
        
        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ICollectorRepository>();
        }
        
        [Test]
        public async Task GetAllAsync_ReturnsAllCollectors()
        {
            // Arrange
            var collectors = new List<Collector>
            {
                new Collector { Id = "HKV-D-2", Type = CollectorType.HKV, Circuits = 2 },
                new Collector { Id = "HKV-D-4", Type = CollectorType.HKV, Circuits = 4 },
                new Collector { Id = "IV-1.25", Type = CollectorType.IV, Circuits = 12 }
            };
            
            _repositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(collectors);
            
            // Act
            var result = await _repositoryMock.Object.GetAllAsync();
            
            // Assert
            Assert.That(result.Count(), Is.EqualTo(3));
        }
        
        [Test]
        public async Task GetByIdAsync_ReturnsCollector()
        {
            // Arrange
            var collector = new Collector { Id = "HKV-D-4", Circuits = 4 };
            
            _repositoryMock
                .Setup(r => r.GetByIdAsync("HKV-D-4"))
                .ReturnsAsync(collector);
            
            // Act
            var result = await _repositoryMock.Object.GetByIdAsync("HKV-D-4");
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo("HKV-D-4"));
        }
        
        [Test]
        public async Task GetByIdAsync_ReturnsNullForUnknownId()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync("UNKNOWN"))
                .ReturnsAsync((Collector?)null);
            
            // Act
            var result = await _repositoryMock.Object.GetByIdAsync("UNKNOWN");
            
            // Assert
            Assert.That(result, Is.Null);
        }
        
        [Test]
        public async Task GetByTypeAsync_ReturnsCollectorsOfType()
        {
            // Arrange
            var hkvCollectors = new List<Collector>
            {
                new Collector { Id = "HKV-D-2", Type = CollectorType.HKV },
                new Collector { Id = "HKV-D-4", Type = CollectorType.HKV }
            };
            
            _repositoryMock
                .Setup(r => r.GetByTypeAsync(CollectorType.HKV))
                .ReturnsAsync(hkvCollectors);
            
            // Act
            var result = await _repositoryMock.Object.GetByTypeAsync(CollectorType.HKV);
            
            // Assert
            Assert.That(result.All(c => c.Type == CollectorType.HKV), Is.True);
        }
        
        [Test]
        public async Task GetByCircuitsAsync_ReturnsCollector()
        {
            // Arrange
            var collector = new Collector { Id = "HKV-D-4", Circuits = 4 };
            
            _repositoryMock
                .Setup(r => r.GetByCircuitsAsync(4))
                .ReturnsAsync(collector);
            
            // Act
            var result = await _repositoryMock.Object.GetByCircuitsAsync(4);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Circuits, Is.EqualTo(4));
        }
        
        [Test]
        public void SelectCollector_ReturnsSuitableCollector()
        {
            // Arrange
            var collector = new Collector
            {
                Id = "HKV-D-4",
                Type = CollectorType.HKV,
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            _repositoryMock
                .Setup(r => r.SelectCollector(4, 1.0))
                .Returns(collector);
            
            // Act
            var result = _repositoryMock.Object.SelectCollector(4, 1.0);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Circuits, Is.EqualTo(4));
        }
        
        [Test]
        public void GetAvailableCircuitCounts_ReturnsCorrectList()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetAvailableCircuitCounts())
                .Returns(new[] { 2, 4, 6, 8, 10, 12 });
            
            // Act
            var result = _repositoryMock.Object.GetAvailableCircuitCounts();
            
            // Assert
            Assert.That(result, Is.EqualTo(new[] { 2, 4, 6, 8, 10, 12 }));
        }
        
        [Test]
        public void IsCollectorSuitable_ReturnsTrueForValidParameters()
        {
            // Arrange
            var collector = new Collector
            {
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            _repositoryMock
                .Setup(r => r.IsCollectorSuitable(collector, 4, 1.0, 200))
                .Returns(true);
            
            // Act
            var result = _repositoryMock.Object.IsCollectorSuitable(collector, 4, 1.0, 200);
            
            // Assert
            Assert.That(result, Is.True);
        }
        
        [Test]
        public void GetMaxCircuitsForHKV_Returns12()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetMaxCircuitsForHKV())
                .Returns(12);
            
            // Act
            var result = _repositoryMock.Object.GetMaxCircuitsForHKV();
            
            // Assert
            Assert.That(result, Is.EqualTo(12));
        }
        
        [Test]
        public void GetMaxFlowRateForHKV_ReturnsCorrectValue()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetMaxFlowRateForHKV())
                .Returns(1.5);
            
            // Act
            var result = _repositoryMock.Object.GetMaxFlowRateForHKV();
            
            // Assert
            Assert.That(result, Is.EqualTo(1.5));
        }
        
        [Test]
        public void GetMaxPressureForHKV_ReturnsCorrectValue()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetMaxPressureForHKV())
                .Returns(320.0);
            
            // Act
            var result = _repositoryMock.Object.GetMaxPressureForHKV();
            
            // Assert
            Assert.That(result, Is.EqualTo(320.0));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `ICollectorRepository.cs` создан
- [ ] Интерфейс содержит все методы из ТЗ
- [ ] Все методы имеют XML-документацию
- [ ] Интерфейс ссылается на `Collector` и `CollectorType`
- [ ] Unit-тесты с Mock проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Интерфейс должен быть асинхронным (Task<>) для загрузки данных из JSON
- Методы SelectCollector и IsCollectorSuitable синхронные (работа с памятью)
- Данные о коллекторах должны загружаться при инициализации