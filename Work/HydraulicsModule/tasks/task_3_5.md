# Task 3.5: CollectorRepository (Репозиторий коллекторов)

**Этап:** 3 - Services  
**Приоритет:** Средний  
**Статус:** Не начато  
**Зависимости:** Task 2.3 (ICollectorRepository)

---

## 1. Цель задачи

Реализовать класс `CollectorRepository` для работы с данными о коллекторах РЕХАУ.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Подбор коллектора РЕХАУ | Все методы |

---

## 3. Создаваемые файлы

### 3.1. CollectorRepository.cs

**Путь:** `src/Repositories/Hydraulics/CollectorRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Repositories.Hydraulics
{
    /// <summary>
    /// Репозиторий для работы с данными о коллекторах РЕХАУ
    /// </summary>
    public class CollectorRepository : ICollectorRepository
    {
        private readonly string _dataFilePath;
        private List<Collector> _cachedCollectors;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Создать экземпляр репозитория с путём к файлу данных по умолчанию
        /// </summary>
        public CollectorRepository() : this("data/rehau_products.json")
        {
        }

        /// <summary>
        /// Создать экземпляр репозитория с указанным путём к файлу данных
        /// </summary>
        public CollectorRepository(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
        }

        /// <summary>
        /// Получить все коллекторы
        /// </summary>
        public async Task<List<Collector>> GetAllAsync()
        {
            return await LoadDataAsync();
        }

        /// <summary>
        /// Получить коллектор по ID
        /// </summary>
        public async Task<Collector> GetByIdAsync(int id)
        {
            var collectors = await LoadDataAsync();
            return collectors.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Получить коллекторы по типу
        /// </summary>
        public async Task<List<Collector>> GetByTypeAsync(CollectorType type)
        {
            var collectors = await LoadDataAsync();
            return collectors.Where(c => c.Type == type).ToList();
        }

        /// <summary>
        /// Получить коллекторы по количеству контуров
        /// </summary>
        public async Task<List<Collector>> GetByCircuitsAsync(int circuitCount)
        {
            var collectors = await LoadDataAsync();
            return collectors.Where(c => c.CircuitCount == circuitCount).ToList();
        }

        /// <summary>
        /// Подобрать коллектор по параметрам
        /// </summary>
        public async Task<Collector> SelectCollectorAsync(CollectorType type, int circuitCount, double totalFlowRate)
        {
            var collectors = await LoadDataAsync();

            // Фильтрация по типу и количеству контуров
            var candidates = collectors
                .Where(c => c.Type == type && c.CircuitCount >= circuitCount)
                .OrderBy(c => c.CircuitCount)
                .ToList();

            if (!candidates.Any())
            {
                return null;
            }

            // Проверка пропускной способности
            foreach (var collector in candidates)
            {
                if (collector.MaxFlowRate >= totalFlowRate)
                {
                    return collector;
                }
            }

            // Если не нашли подходящий по расходу, вернуть первый с достаточным количеством контуров
            return candidates.FirstOrDefault();
        }

        /// <summary>
        /// Получить доступные количества контуров для типа коллектора
        /// </summary>
        public async Task<int[]> GetAvailableCircuitCountsAsync(CollectorType type)
        {
            var collectors = await LoadDataAsync();
            return collectors
                .Where(c => c.Type == type)
                .Select(c => c.CircuitCount)
                .Distinct()
                .OrderBy(c => c)
                .ToArray();
        }

        #region Private Methods

        /// <summary>
        /// Загрузить данные из JSON файла (с кэшированием)
        /// </summary>
        private async Task<List<Collector>> LoadDataAsync()
        {
            lock (_lockObject)
            {
                if (_cachedCollectors != null)
                    return _cachedCollectors;
            }

            if (!File.Exists(_dataFilePath))
            {
                // Если файл не существует, вернуть встроенные данные
                return GetDefaultCollectors();
            }

            string json = await File.ReadAllTextAsync(_dataFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var container = JsonSerializer.Deserialize<CollectorDataContainer>(json, options);

            lock (_lockObject)
            {
                _cachedCollectors = container?.Collectors ?? GetDefaultCollectors();
                return _cachedCollectors;
            }
        }

        /// <summary>
        /// Получить встроенные данные о коллекторах РЕХАУ
        /// </summary>
        private List<Collector> GetDefaultCollectors()
        {
            return new List<Collector>
            {
                // HKV коллекторы (для систем снеготаяния)
                new Collector
                {
                    Id = 1,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 2",
                    Description = "Коллектор РЕХАУ HKV-D на 2 контура",
                    CircuitCount = 2,
                    NominalDiameter = "DN25 (1\")",
                    KvValue = 1.2,
                    MaxFlowRate = 400,
                    MaxPressure = 600,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },
                new Collector
                {
                    Id = 2,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 4",
                    Description = "Коллектор РЕХАУ HKV-D на 4 контура",
                    CircuitCount = 4,
                    NominalDiameter = "DN25 (1\")",
                    KvValue = 1.2,
                    MaxFlowRate = 800,
                    MaxPressure = 600,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },
                new Collector
                {
                    Id = 3,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 6",
                    Description = "Коллектор РЕХАУ HKV-D на 6 контуров",
                    CircuitCount = 6,
                    NominalDiameter = "DN25 (1\")",
                    KvValue = 1.2,
                    MaxFlowRate = 1200,
                    MaxPressure = 600,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },
                new Collector
                {
                    Id = 4,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 8",
                    Description = "Коллектор РЕХАУ HKV-D на 8 контуров",
                    CircuitCount = 8,
                    NominalDiameter = "DN25 (1\")",
                    KvValue = 1.2,
                    MaxFlowRate = 1600,
                    MaxPressure = 600,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },
                new Collector
                {
                    Id = 5,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 10",
                    Description = "Коллектор РЕХАУ HKV-D на 10 контуров",
                    CircuitCount = 10,
                    NominalDiameter = "DN25 (1\")",
                    KvValue = 1.2,
                    MaxFlowRate = 2000,
                    MaxPressure = 600,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },
                new Collector
                {
                    Id = 6,
                    Type = CollectorType.HKV,
                    Name = "HKV-D 12",
                    Description = "Коллектор РЕХАУ HKV-D на 12 контуров",
                    CircuitCount = 12,
                    NominalDiameter = "DN25 (1\")",
                    KvValue = 1.2,
                    MaxFlowRate = 2400,
                    MaxPressure = 600,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },

                // IV коллекторы (промышленные)
                new Collector
                {
                    Id = 7,
                    Type = CollectorType.IV,
                    Name = "IV DN25",
                    Description = "Коллектор РЕХАУ IV DN25 (1¼\")",
                    CircuitCount = 1,
                    NominalDiameter = "DN25 (1¼\")",
                    KvValue = 1.45,
                    MaxFlowRate = 500,
                    MaxPressure = 1000,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                },
                new Collector
                {
                    Id = 8,
                    Type = CollectorType.IV,
                    Name = "IV DN40",
                    Description = "Коллектор РЕХАУ IV DN40 (1½\")",
                    CircuitCount = 1,
                    NominalDiameter = "DN40 (1½\")",
                    KvValue = 2.2,
                    MaxFlowRate = 1000,
                    MaxPressure = 1000,
                    Material = "Латунь",
                    Manufacturer = "REHAU"
                }
            };
        }

        #endregion
    }
}
```

### 3.2. Модели данных для коллекторов

**Путь:** `src/Models/Hydraulics/Collector.cs`

```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Тип коллектора РЕХАУ
    /// </summary>
    public enum CollectorType
    {
        /// <summary>
        /// HKV-D — коллектор для систем снеготаяния
        /// </summary>
        HKV = 1,

        /// <summary>
        /// IV — промышленный коллектор
        /// </summary>
        IV = 2
    }

    /// <summary>
    /// Коллектор РЕХАУ
    /// </summary>
    public class Collector
    {
        /// <summary>
        /// Идентификатор коллектора
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Тип коллектора
        /// </summary>
        public CollectorType Type { get; set; }

        /// <summary>
        /// Название коллектора
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание коллектора
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Количество контуров
        /// </summary>
        public int CircuitCount { get; set; }

        /// <summary>
        /// Номинальный диаметр
        /// </summary>
        public string NominalDiameter { get; set; }

        /// <summary>
        /// Коэффициент пропускной способности Kv (м³/ч)
        /// </summary>
        public double KvValue { get; set; }

        /// <summary>
        /// Максимальный расход (л/ч)
        /// </summary>
        public double MaxFlowRate { get; set; }

        /// <summary>
        /// Максимальное рабочее давление (кПа)
        /// </summary>
        public double MaxPressure { get; set; }

        /// <summary>
        /// Материал
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// Производитель
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({CircuitCount} контуров)";
        }
    }

    /// <summary>
    /// Контейнер данных коллекторов из JSON
    /// </summary>
    public class CollectorDataContainer
    {
        public List<Collector> Collectors { get; set; }
    }
}
```

### 3.3. Интерфейс ICollectorRepository

**Путь:** `src/Repositories/Hydraulics/ICollectorRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Repositories.Hydraulics
{
    /// <summary>
    /// Интерфейс репозитория коллекторов
    /// </summary>
    public interface ICollectorRepository
    {
        /// <summary>
        /// Получить все коллекторы
        /// </summary>
        Task<List<Collector>> GetAllAsync();

        /// <summary>
        /// Получить коллектор по ID
        /// </summary>
        Task<Collector> GetByIdAsync(int id);

        /// <summary>
        /// Получить коллекторы по типу
        /// </summary>
        Task<List<Collector>> GetByTypeAsync(CollectorType type);

        /// <summary>
        /// Получить коллекторы по количеству контуров
        /// </summary>
        Task<List<Collector>> GetByCircuitsAsync(int circuitCount);

        /// <summary>
        /// Подобрать коллектор по параметрам
        /// </summary>
        Task<Collector> SelectCollectorAsync(CollectorType type, int circuitCount, double totalFlowRate);

        /// <summary>
        /// Получить доступные количества контуров для типа коллектора
        /// </summary>
        Task<int[]> GetAvailableCircuitCountsAsync(CollectorType type);
    }
}
```

### 3.4. Пример JSON файла данных

**Путь:** `data/rehau_products.json`

```json
{
  "collectors": [
    {
      "id": 1,
      "type": 1,
      "name": "HKV-D 2",
      "description": "Коллектор РЕХАУ HKV-D на 2 контура",
      "circuitCount": 2,
      "nominalDiameter": "DN25 (1\")",
      "kvValue": 1.2,
      "maxFlowRate": 400,
      "maxPressure": 600,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 2,
      "type": 1,
      "name": "HKV-D 4",
      "description": "Коллектор РЕХАУ HKV-D на 4 контура",
      "circuitCount": 4,
      "nominalDiameter": "DN25 (1\")",
      "kvValue": 1.2,
      "maxFlowRate": 800,
      "maxPressure": 600,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 3,
      "type": 1,
      "name": "HKV-D 6",
      "description": "Коллектор РЕХАУ HKV-D на 6 контуров",
      "circuitCount": 6,
      "nominalDiameter": "DN25 (1\")",
      "kvValue": 1.2,
      "maxFlowRate": 1200,
      "maxPressure": 600,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 4,
      "type": 1,
      "name": "HKV-D 8",
      "description": "Коллектор РЕХАУ HKV-D на 8 контуров",
      "circuitCount": 8,
      "nominalDiameter": "DN25 (1\")",
      "kvValue": 1.2,
      "maxFlowRate": 1600,
      "maxPressure": 600,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 5,
      "type": 1,
      "name": "HKV-D 10",
      "description": "Коллектор РЕХАУ HKV-D на 10 контуров",
      "circuitCount": 10,
      "nominalDiameter": "DN25 (1\")",
      "kvValue": 1.2,
      "maxFlowRate": 2000,
      "maxPressure": 600,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 6,
      "type": 1,
      "name": "HKV-D 12",
      "description": "Коллектор РЕХАУ HKV-D на 12 контуров",
      "circuitCount": 12,
      "nominalDiameter": "DN25 (1\")",
      "kvValue": 1.2,
      "maxFlowRate": 2400,
      "maxPressure": 600,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 7,
      "type": 2,
      "name": "IV DN25",
      "description": "Коллектор РЕХАУ IV DN25 (1¼\")",
      "circuitCount": 1,
      "nominalDiameter": "DN25 (1¼\")",
      "kvValue": 1.45,
      "maxFlowRate": 500,
      "maxPressure": 1000,
      "material": "Латунь",
      "manufacturer": "REHAU"
    },
    {
      "id": 8,
      "type": 2,
      "name": "IV DN40",
      "description": "Коллектор РЕХАУ IV DN40 (1½\")",
      "circuitCount": 1,
      "nominalDiameter": "DN40 (1½\")",
      "kvValue": 2.2,
      "maxFlowRate": 1000,
      "maxPressure": 1000,
      "material": "Латунь",
      "manufacturer": "REHAU"
    }
  ]
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Repositories/Hydraulics/CollectorRepositoryTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.Repositories.Hydraulics
{
    [TestFixture]
    public class CollectorRepositoryTests
    {
        private CollectorRepository _repository;

        [SetUp]
        public void Setup()
        {
            _repository = new CollectorRepository();
        }

        [Test]
        public async Task GetAllAsync_ReturnsAllCollectors()
        {
            // Act
            var collectors = await _repository.GetAllAsync();

            // Assert
            Assert.That(collectors, Is.Not.Null);
            Assert.That(collectors.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetByIdAsync_ExistingId_ReturnsCollector()
        {
            // Act
            var collector = await _repository.GetByIdAsync(1);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector.Id, Is.EqualTo(1));
            Assert.That(collector.Name, Does.Contain("HKV"));
        }

        [Test]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var collector = await _repository.GetByIdAsync(999);

            // Assert
            Assert.That(collector, Is.Null);
        }

        [Test]
        public async Task GetByTypeAsync_HKV_ReturnsHKVCollectors()
        {
            // Act
            var collectors = await _repository.GetByTypeAsync(CollectorType.HKV);

            // Assert
            Assert.That(collectors, Is.Not.Null);
            Assert.That(collectors.All(c => c.Type == CollectorType.HKV), Is.True);
        }

        [Test]
        public async Task GetByTypeAsync_IV_ReturnsIVCollectors()
        {
            // Act
            var collectors = await _repository.GetByTypeAsync(CollectorType.IV);

            // Assert
            Assert.That(collectors, Is.Not.Null);
            Assert.That(collectors.All(c => c.Type == CollectorType.IV), Is.True);
        }

        [Test]
        public async Task GetByCircuitsAsync_4Circuits_ReturnsCorrectCollectors()
        {
            // Act
            var collectors = await _repository.GetByCircuitsAsync(4);

            // Assert
            Assert.That(collectors, Is.Not.Null);
            Assert.That(collectors.All(c => c.CircuitCount == 4), Is.True);
        }

        [Test]
        public async Task SelectCollectorAsync_HKV4Circuits_ReturnsCorrectCollector()
        {
            // Arrange
            int circuitCount = 4;
            double totalFlowRate = 600; // л/ч

            // Act
            var collector = await _repository.SelectCollectorAsync(CollectorType.HKV, circuitCount, totalFlowRate);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector.Type, Is.EqualTo(CollectorType.HKV));
            Assert.That(collector.CircuitCount, Is.GreaterThanOrEqualTo(circuitCount));
        }

        [Test]
        public async Task SelectCollectorAsync_HighFlowRate_ReturnsCollectorWithSufficientCapacity()
        {
            // Arrange
            int circuitCount = 6;
            double totalFlowRate = 1500; // л/ч

            // Act
            var collector = await _repository.SelectCollectorAsync(CollectorType.HKV, circuitCount, totalFlowRate);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector.MaxFlowRate, Is.GreaterThanOrEqualTo(totalFlowRate));
        }

        [Test]
        public async Task GetAvailableCircuitCountsAsync_HKV_ReturnsCorrectArray()
        {
            // Act
            var counts = await _repository.GetAvailableCircuitCountsAsync(CollectorType.HKV);

            // Assert
            Assert.That(counts, Is.Not.Null);
            Assert.That(counts, Does.Contain(2));
            Assert.That(counts, Does.Contain(4));
            Assert.That(counts, Does.Contain(6));
            Assert.That(counts, Does.Contain(8));
            Assert.That(counts, Does.Contain(10));
            Assert.That(counts, Does.Contain(12));
        }

        [Test]
        public async Task Collector_HasCorrectKvValue()
        {
            // Act
            var collectors = await _repository.GetAllAsync();
            var hkvCollector = collectors.First(c => c.Type == CollectorType.HKV);

            // Assert
            Assert.That(hkvCollector.KvValue, Is.EqualTo(1.2).Within(0.01));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `CollectorRepository.cs` создан
- [ ] Файл `Collector.cs` создан
- [ ] Файл `ICollectorRepository.cs` создан
- [ ] Реализован интерфейс `ICollectorRepository`
- [ ] Данные загружаются из `data/rehau_products.json`
- [ ] Встроенные данные используются при отсутствии файла
- [ ] Кэширование данных работает
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов

---

## 6. Примечания

- HKV-D: 2, 4, 6, 8, 10, 12 контуров (для систем снеготаяния)
- IV: DN25 (1¼"), DN40 (1½") (промышленные)
- Kv для HKV: 1.2 м³/ч
- Kv для IV DN25: 1.45 м³/ч
- Kv для IV DN40: 2.2 м³/ч
- Максимальное давление: 600 кПа (HKV), 1000 кПа (IV)