using System.IO;
using System.Threading.Tasks;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;

namespace SnowMeltingCalculator.Tests.Repositories.Hydraulics
{
    /// <summary>
    /// Тесты загрузки данных коллекторов из JSON
    /// </summary>
    [TestFixture]
    public class CollectorRepositoryJsonLoadingTests
    {
        [Test]
        public async Task CollectorRepository_LoadsFromJsonFile()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var collectors = await repository.GetAllAsync();
            
            // Assert
            Assert.That(collectors, Is.Not.Empty);
        }

        [Test]
        public async Task CollectorRepository_ReturnsDefaultDataWhenFileNotFound()
        {
            // Arrange
            var repository = new CollectorRepository("nonexistent_file.json");
            
            // Act
            var collectors = await repository.GetAllAsync();
            
            // Assert - должны вернуться встроенные данные
            Assert.That(collectors, Is.Not.Empty);
            Assert.That(collectors.Any(c => c.Type == CollectorType.HKV), Is.True);
        }

        [Test]
        public async Task CollectorRepository_GetByIdAsync_ReturnsCollector()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var collector = await repository.GetByIdAsync("HKV_4");
            
            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector.Id, Is.EqualTo("HKV_4"));
            Assert.That(collector.Circuits, Is.EqualTo(4));
        }

        [Test]
        public async Task CollectorRepository_GetByTypeAsync_ReturnsCorrectType()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var hkvCollectors = await repository.GetByTypeAsync(CollectorType.HKV);
            var ivCollectors = await repository.GetByTypeAsync(CollectorType.IV);
            
            // Assert
            Assert.That(hkvCollectors.All(c => c.Type == CollectorType.HKV), Is.True);
            Assert.That(ivCollectors.All(c => c.Type == CollectorType.IV), Is.True);
        }

        [Test]
        public async Task CollectorRepository_GetByCircuitsAsync_ReturnsCorrectCircuits()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var collector = await repository.GetByCircuitsAsync(4);
            
            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector.Circuits, Is.EqualTo(4));
        }

        [Test]
        public void CollectorRepository_SelectCollector_ReturnsSuitableCollector()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var collector = repository.SelectCollector(4, 1.0);
            
            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector.Circuits >= 4, Is.True);
            Assert.That(collector.MaxFlowRate >= 1.0, Is.True);
        }

        [Test]
        public void CollectorRepository_SelectCollector_ReturnsNullForTooManyCircuits()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var collector = repository.SelectCollector(20, 1.0);
            
            // Assert
            Assert.That(collector, Is.Null);
        }

        [Test]
        public async Task CollectorRepository_CachesData()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act - несколько вызовов должны использовать кэш
            var collectors1 = await repository.GetAllAsync();
            var collectors2 = await repository.GetAllAsync();
            var collectors3 = await repository.GetAllAsync();
            
            // Assert - количество должно быть одинаковым
            Assert.That(collectors1.Count(), Is.EqualTo(collectors2.Count()));
            Assert.That(collectors1.Count(), Is.EqualTo(collectors3.Count()));
        }

        [Test]
        public void CollectorRepository_IsCollectorSuitable_ReturnsTrueForValidParams()
        {
            // Arrange
            var repository = new CollectorRepository();
            var collector = new Collector
            {
                Id = "TEST",
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            // Act
            var result = repository.IsCollectorSuitable(collector, 4, 1.0, 200);
            
            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CollectorRepository_IsCollectorSuitable_ReturnsFalseForTooManyCircuits()
        {
            // Arrange
            var repository = new CollectorRepository();
            var collector = new Collector
            {
                Id = "TEST",
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            // Act
            var result = repository.IsCollectorSuitable(collector, 6, 1.0, 200);
            
            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CollectorRepository_IsCollectorSuitable_ReturnsFalseForTooHighFlowRate()
        {
            // Arrange
            var repository = new CollectorRepository();
            var collector = new Collector
            {
                Id = "TEST",
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            // Act
            var result = repository.IsCollectorSuitable(collector, 4, 2.0, 200);
            
            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CollectorRepository_IsCollectorSuitable_ReturnsFalseForTooHighPressure()
        {
            // Arrange
            var repository = new CollectorRepository();
            var collector = new Collector
            {
                Id = "TEST",
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            // Act
            var result = repository.IsCollectorSuitable(collector, 4, 1.0, 400);
            
            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CollectorRepository_GetAvailableCircuitCounts_ReturnsCorrectValues()
        {
            // Arrange
            var repository = new CollectorRepository();
            
            // Act
            var counts = repository.GetAvailableCircuitCounts();
            
            // Assert
            Assert.That(counts, Contains.Item(2));
            Assert.That(counts, Contains.Item(4));
            Assert.That(counts, Contains.Item(6));
            Assert.That(counts, Contains.Item(12));
        }

        [Test]
        public void CollectorRepository_GetMaxCircuitsForHKV_Returns12()
        {
            // Arrange
            var repository = new CollectorRepository();
            
            // Act
            var maxCircuits = repository.GetMaxCircuitsForHKV();
            
            // Assert
            Assert.That(maxCircuits, Is.EqualTo(12));
        }

        [Test]
        public void CollectorRepository_GetMaxFlowRateForHKV_ReturnsCorrectValue()
        {
            // Arrange
            var repository = new CollectorRepository();
            
            // Act
            var maxFlow = repository.GetMaxFlowRateForHKV();
            
            // Assert
            Assert.That(maxFlow, Is.EqualTo(1.5));
        }

        [Test]
        public void CollectorRepository_GetMaxPressureForHKV_ReturnsCorrectValue()
        {
            // Arrange
            var repository = new CollectorRepository();
            
            // Act
            var maxPressure = repository.GetMaxPressureForHKV();
            
            // Assert
            Assert.That(maxPressure, Is.EqualTo(320));
        }

        [Test]
        public async Task CollectorRepository_HasBothHKVAndIVCollectors()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var collectors = await repository.GetAllAsync();
            var hasHKV = collectors.Any(c => c.Type == CollectorType.HKV);
            var hasIV = collectors.Any(c => c.Type == CollectorType.IV);
            
            // Assert
            Assert.That(hasHKV, Is.True, "Должны быть HKV коллекторы");
            Assert.That(hasIV, Is.True, "Должны быть IV коллекторы");
        }

        [Test]
        public async Task CollectorRepository_HKVCollectorsHaveCorrectProperties()
        {
            // Arrange
            var repository = new CollectorRepository("data/rehau_products.json");
            
            // Act
            var hkvCollectors = await repository.GetByTypeAsync(CollectorType.HKV);
            
            // Assert
            foreach (var collector in hkvCollectors)
            {
                Assert.That(collector.Type, Is.EqualTo(CollectorType.HKV));
                Assert.That(collector.Circuits, Is.InRange(2, 12));
                Assert.That(collector.MaxFlowRate, Is.EqualTo(1.5));
                Assert.That(collector.MaxPressure, Is.EqualTo(320));
                Assert.That(collector.MaxSetting, Is.EqualTo(8));
            }
        }
    }
}