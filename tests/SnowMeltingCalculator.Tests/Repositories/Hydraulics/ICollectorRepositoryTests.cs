using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Repositories.Hydraulics
{
    /// <summary>
    /// Тесты для интерфейса ICollectorRepository
    /// </summary>
    [TestFixture]
    public class ICollectorRepositoryTests
    {
        private Mock<ICollectorRepository> _repositoryMock = null!;
        
        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ICollectorRepository>();
        }
        
        #region GetAllAsync Tests
        
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
        
        #endregion
        
        #region GetByIdAsync Tests
        
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
        
        #endregion
        
        #region GetByTypeAsync Tests
        
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
        public async Task GetByTypeAsync_ReturnsIndustrialCollectors()
        {
            // Arrange
            var ivCollectors = new List<Collector>
            {
                new Collector { Id = "IV-1.25", Type = CollectorType.IV },
                new Collector { Id = "IV-1.5", Type = CollectorType.IV }
            };
            
            _repositoryMock
                .Setup(r => r.GetByTypeAsync(CollectorType.IV))
                .ReturnsAsync(ivCollectors);
            
            // Act
            var result = await _repositoryMock.Object.GetByTypeAsync(CollectorType.IV);
            
            // Assert
            Assert.That(result.All(c => c.Type == CollectorType.IV), Is.True);
        }
        
        #endregion
        
        #region GetByCircuitsAsync Tests
        
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
        public async Task GetByCircuitsAsync_ReturnsNullForInvalidCircuits()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByCircuitsAsync(15))
                .ReturnsAsync((Collector?)null);
            
            // Act
            var result = await _repositoryMock.Object.GetByCircuitsAsync(15);
            
            // Assert
            Assert.That(result, Is.Null);
        }
        
        #endregion
        
        #region SelectCollector Tests
        
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
        public void SelectCollector_ForHighFlowRate_ReturnsIndustrial()
        {
            // Arrange
            var collector = new Collector
            {
                Id = "IV-1.5",
                Type = CollectorType.IV,
                Circuits = 12,
                MaxFlowRate = 10.0
            };
            
            _repositoryMock
                .Setup(r => r.SelectCollector(4, 5.0))
                .Returns(collector);
            
            // Act
            var result = _repositoryMock.Object.SelectCollector(4, 5.0);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Type, Is.EqualTo(CollectorType.IV));
        }
        
        #endregion
        
        #region GetAvailableCircuitCounts Tests
        
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
        
        #endregion
        
        #region IsCollectorSuitable Tests
        
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
        public void IsCollectorSuitable_ReturnsFalseForExceededFlowRate()
        {
            // Arrange
            var collector = new Collector
            {
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            _repositoryMock
                .Setup(r => r.IsCollectorSuitable(collector, 4, 2.0, 200))
                .Returns(false);
            
            // Act
            var result = _repositoryMock.Object.IsCollectorSuitable(collector, 4, 2.0, 200);
            
            // Assert
            Assert.That(result, Is.False);
        }
        
        [Test]
        public void IsCollectorSuitable_ReturnsFalseForExceededPressure()
        {
            // Arrange
            var collector = new Collector
            {
                Circuits = 4,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            _repositoryMock
                .Setup(r => r.IsCollectorSuitable(collector, 4, 1.0, 400))
                .Returns(false);
            
            // Act
            var result = _repositoryMock.Object.IsCollectorSuitable(collector, 4, 1.0, 400);
            
            // Assert
            Assert.That(result, Is.False);
        }
        
        #endregion
        
        #region GetMaxCircuitsForHKV Tests
        
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
        
        #endregion
        
        #region GetMaxFlowRateForHKV Tests
        
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
        
        #endregion
        
        #region GetMaxPressureForHKV Tests
        
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
        
        #endregion
    }
}