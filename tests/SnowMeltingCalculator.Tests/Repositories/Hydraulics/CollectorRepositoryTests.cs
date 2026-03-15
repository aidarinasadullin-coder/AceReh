using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using System.Linq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.Repositories.Hydraulics
{
    /// <summary>
    /// Тесты для CollectorRepository
    /// </summary>
    [TestFixture]
    public class CollectorRepositoryTests
    {
        private CollectorRepository _repository = null!;

        [SetUp]
        public void Setup()
        {
            _repository = new CollectorRepository("data/rehau_products.json");
        }

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ReturnsAllCollectors()
        {
            // Act
            var collectors = await _repository.GetAllAsync();

            // Assert
            Assert.That(collectors, Is.Not.Null);
            Assert.That(collectors.Count(), Is.GreaterThan(0));
        }

        #endregion

        #region GetByIdAsync Tests

        [Test]
        public async Task GetByIdAsync_ExistingId_ReturnsCollector()
        {
            // Act - используем ID из встроенных данных
            var collectors = await _repository.GetAllAsync();
            var firstCollector = collectors.FirstOrDefault();
            
            // Assert
            Assert.That(firstCollector, Is.Not.Null);
            
            // Act - получаем по ID
            var collector = await _repository.GetByIdAsync(firstCollector!.Id);
            
            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector!.Id, Is.EqualTo(firstCollector.Id));
        }

        [Test]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var collector = await _repository.GetByIdAsync("NON_EXISTENT");

            // Assert
            Assert.That(collector, Is.Null);
        }

        #endregion

        #region GetByTypeAsync Tests

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

        #endregion

        #region GetByCircuitsAsync Tests

        [Test]
        public async Task GetByCircuitsAsync_4Circuits_ReturnsCorrectCollector()
        {
            // Act
            var collector = await _repository.GetByCircuitsAsync(4);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector!.Circuits, Is.EqualTo(4));
        }

        [Test]
        public async Task GetByCircuitsAsync_12Circuits_ReturnsCorrectCollector()
        {
            // Act
            var collector = await _repository.GetByCircuitsAsync(12);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector!.Circuits, Is.EqualTo(12));
        }

        #endregion

        #region SelectCollector Tests

        [Test]
        public void SelectCollector_HKV4Circuits_ReturnsCorrectCollector()
        {
            // Arrange
            int circuits = 4;
            double totalFlowRate = 0.6; // м³/ч

            // Act
            var collector = _repository.SelectCollector(circuits, totalFlowRate);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector!.Type, Is.EqualTo(CollectorType.HKV));
            Assert.That(collector.Circuits, Is.GreaterThanOrEqualTo(circuits));
        }

        [Test]
        public void SelectCollector_HighFlowRate_ReturnsCollectorWithSufficientCapacity()
        {
            // Arrange
            int circuits = 6;
            double totalFlowRate = 1.5; // м³/ч

            // Act
            var collector = _repository.SelectCollector(circuits, totalFlowRate);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector!.MaxFlowRate, Is.GreaterThanOrEqualTo(totalFlowRate));
        }

        [Test]
        public void SelectCollector_ManyCircuits_ReturnsSuitableCollector()
        {
            // Arrange
            int circuits = 10;
            double totalFlowRate = 1.0; // м³/ч

            // Act
            var collector = _repository.SelectCollector(circuits, totalFlowRate);

            // Assert
            Assert.That(collector, Is.Not.Null);
            Assert.That(collector!.Circuits, Is.GreaterThanOrEqualTo(circuits));
        }

        #endregion

        #region GetAvailableCircuitCounts Tests

        [Test]
        public void GetAvailableCircuitCounts_ReturnsCorrectArray()
        {
            // Act
            var counts = _repository.GetAvailableCircuitCounts();

            // Assert
            Assert.That(counts, Is.Not.Null);
            Assert.That(counts, Does.Contain(2));
            Assert.That(counts, Does.Contain(4));
            Assert.That(counts, Does.Contain(6));
            Assert.That(counts, Does.Contain(8));
            Assert.That(counts, Does.Contain(10));
            Assert.That(counts, Does.Contain(12));
        }

        #endregion

        #region IsCollectorSuitable Tests

        [Test]
        public void IsCollectorSuitable_SuitableParameters_ReturnsTrue()
        {
            // Arrange
            var collector = new Collector
            {
                Id = "TEST",
                Type = CollectorType.HKV,
                Circuits = 6,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            int circuits = 4;
            double flowRate = 1.0;
            double pressure = 200;

            // Act
            bool result = _repository.IsCollectorSuitable(collector, circuits, flowRate, pressure);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsCollectorSuitable_TooManyCircuits_ReturnsFalse()
        {
            // Arrange
            var collector = new Collector
            {
                Id = "TEST",
                Type = CollectorType.HKV,
                Circuits = 6,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            int circuits = 8; // Больше, чем у коллектора
            double flowRate = 1.0;
            double pressure = 200;

            // Act
            bool result = _repository.IsCollectorSuitable(collector, circuits, flowRate, pressure);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsCollectorSuitable_TooHighFlowRate_ReturnsFalse()
        {
            // Arrange
            var collector = new Collector
            {
                Id = "TEST",
                Type = CollectorType.HKV,
                Circuits = 6,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            int circuits = 4;
            double flowRate = 2.0; // Больше, чем максимальный расход
            double pressure = 200;

            // Act
            bool result = _repository.IsCollectorSuitable(collector, circuits, flowRate, pressure);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsCollectorSuitable_TooHighPressure_ReturnsFalse()
        {
            // Arrange
            var collector = new Collector
            {
                Id = "TEST",
                Type = CollectorType.HKV,
                Circuits = 6,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            int circuits = 4;
            double flowRate = 1.0;
            double pressure = 400; // Больше, чем максимальное давление

            // Act
            bool result = _repository.IsCollectorSuitable(collector, circuits, flowRate, pressure);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsCollectorSuitable_NullCollector_ReturnsFalse()
        {
            // Act
            bool result = _repository.IsCollectorSuitable(null!, 4, 1.0, 200);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region GetMaxCircuitsForHKV Tests

        [Test]
        public void GetMaxCircuitsForHKV_ReturnsCorrectValue()
        {
            // Act
            int maxCircuits = _repository.GetMaxCircuitsForHKV();

            // Assert
            Assert.That(maxCircuits, Is.EqualTo(12));
        }

        #endregion

        #region GetMaxFlowRateForHKV Tests

        [Test]
        public void GetMaxFlowRateForHKV_ReturnsCorrectValue()
        {
            // Act
            double maxFlowRate = _repository.GetMaxFlowRateForHKV();

            // Assert
            Assert.That(maxFlowRate, Is.EqualTo(1.5).Within(0.01));
        }

        #endregion

        #region GetMaxPressureForHKV Tests

        [Test]
        public void GetMaxPressureForHKV_ReturnsCorrectValue()
        {
            // Act
            double maxPressure = _repository.GetMaxPressureForHKV();

            // Assert
            Assert.That(maxPressure, Is.EqualTo(320).Within(0.01));
        }

        #endregion
    }
}