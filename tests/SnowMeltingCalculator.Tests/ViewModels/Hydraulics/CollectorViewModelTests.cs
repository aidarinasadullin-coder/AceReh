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
    /// <summary>
    /// Тесты для CollectorViewModel
    /// </summary>
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
                .Setup(r => r.SelectCollectorAsync(It.IsAny<int>(), It.IsAny<double>()))
                .ReturnsAsync((int circuits, double flow) =>
                {
                    return GetTestCollectors()
                        .FirstOrDefault(c => c.Circuits >= circuits && c.MaxFlowRate >= flow);
                });

            _viewModel = new CollectorViewModel(_repositoryMock.Object);
        }

        private List<Collector> GetTestCollectors()
        {
            return new List<Collector>
            {
                new Collector
                {
                    Id = "HKV-D-4",
                    Name = "HKV-D 4",
                    FullName = "Коллектор РЕХАУ HKV-D на 4 контура",
                    Type = CollectorType.HKV,
                    Circuits = 4,
                    ConnectionSize = "DN25 (1\")",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320
                },
                new Collector
                {
                    Id = "HKV-D-6",
                    Name = "HKV-D 6",
                    FullName = "Коллектор РЕХАУ HKV-D на 6 контуров",
                    Type = CollectorType.HKV,
                    Circuits = 6,
                    ConnectionSize = "DN25 (1\")",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320
                },
                new Collector
                {
                    Id = "IV-1.25",
                    Name = "IV DN25",
                    FullName = "Коллектор РЕХАУ IV DN25 (1¼\")",
                    Type = CollectorType.IV,
                    Circuits = 1,
                    ConnectionSize = "DN25 (1¼\")",
                    Kv = 1.45,
                    MaxFlowRate = 2.5,
                    MaxPressure = 1000
                }
            };
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            // Assert
            Assert.That(_viewModel.SelectedCollectorType, Is.EqualTo(CollectorType.HKV));
            Assert.That(_viewModel.CircuitCount, Is.EqualTo(4));
            Assert.That(_viewModel.TotalFlowRate, Is.EqualTo(0));
        }

        #endregion

        #region LoadCollectors Tests

        [Test]
        public async Task LoadCollectorsAsync_LoadsCollectors()
        {
            // Act
            await _viewModel.LoadCollectorsCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.AvailableCollectors.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task LoadCollectorsAsync_SetsIsLoading()
        {
            // Arrange
            bool isLoadingDuringExecution = false;
            _repositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() =>
                {
                    isLoadingDuringExecution = _viewModel.IsLoading;
                    return GetTestCollectors();
                });

            // Act
            await _viewModel.LoadCollectorsCommand.ExecuteAsync(null);

            // Assert
            Assert.That(isLoadingDuringExecution, Is.True);
            Assert.That(_viewModel.IsLoading, Is.False);
        }

        #endregion

        #region SelectCollector Tests

        [Test]
        public async Task SelectCollectorAsync_SelectsCorrectCollector()
        {
            // Arrange
            _viewModel.SelectedCollectorType = CollectorType.HKV;
            _viewModel.CircuitCount = 4;
            _viewModel.TotalFlowRate = 600; // л/ч = 0.6 м³/ч

            // Act
            await _viewModel.SelectCollectorCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.SelectedCollector, Is.Not.Null);
            Assert.That(_viewModel.SelectedCollector.Type, Is.EqualTo(CollectorType.HKV));
            Assert.That(_viewModel.SelectedCollector.Circuits, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public async Task SelectCollectorAsync_WhenNoMatch_SetsErrorMessage()
        {
            // Arrange
            _viewModel.CircuitCount = 20; // Больше максимального
            _viewModel.TotalFlowRate = 5000;

            // Act
            await _viewModel.SelectCollectorCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.SelectedCollector, Is.Null);
            Assert.That(_viewModel.ErrorMessage, Is.Not.Empty);
        }

        #endregion

        #region FilterByType Tests

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
        public void FilterByType_WhenIV_FiltersCorrectly()
        {
            // Arrange
            _viewModel.AvailableCollectors = new System.Collections.ObjectModel.ObservableCollection<Collector>(GetTestCollectors());

            // Act
            _viewModel.FilterByTypeCommand.Execute(CollectorType.IV);

            // Assert
            Assert.That(_viewModel.FilteredCollectors.All(c => c.Type == CollectorType.IV), Is.True);
        }

        #endregion

        #region SelectCollectorFromList Tests

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
        public void SelectCollectorFromList_WithNull_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _viewModel.SelectCollectorFromListCommand.Execute(null));
        }

        #endregion

        #region ClearSelection Tests

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
        public void ClearSelection_ClearsErrorMessage()
        {
            // Arrange
            _viewModel.SelectedCollector = GetTestCollectors()[0];
            _viewModel.ErrorMessage = "Test error";

            // Act
            _viewModel.ClearSelectionCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Empty);
        }

        #endregion

        #region SetSelectionParameters Tests

        [Test]
        public void SetSelectionParameters_SetsValues()
        {
            // Act
            _viewModel.SetSelectionParameters(6, 1000);

            // Assert
            Assert.That(_viewModel.CircuitCount, Is.EqualTo(6));
            Assert.That(_viewModel.TotalFlowRate, Is.EqualTo(1000));
        }

        #endregion

        #region IsCollectorCompatible Tests

        [Test]
        public void IsCollectorCompatible_ReturnsTrueForCompatible()
        {
            // Arrange
            var collector = new Collector
            {
                Circuits = 6,
                MaxFlowRate = 1.5 // м³/ч = 1500 л/ч
            };

            // Act
            bool isCompatible = _viewModel.IsCollectorCompatible(collector, 4, 800); // 800 л/ч

            // Assert
            Assert.That(isCompatible, Is.True);
        }

        [Test]
        public void IsCollectorCompatible_ReturnsFalseForIncompatibleCircuits()
        {
            // Arrange
            var collector = new Collector
            {
                Circuits = 4,
                MaxFlowRate = 1.5
            };

            // Act
            bool isCompatible = _viewModel.IsCollectorCompatible(collector, 6, 500);

            // Assert
            Assert.That(isCompatible, Is.False);
        }

        [Test]
        public void IsCollectorCompatible_ReturnsFalseForIncompatibleFlowRate()
        {
            // Arrange
            var collector = new Collector
            {
                Circuits = 6,
                MaxFlowRate = 1.0 // м³/ч = 1000 л/ч
            };

            // Act
            bool isCompatible = _viewModel.IsCollectorCompatible(collector, 4, 1500); // 1500 л/ч

            // Assert
            Assert.That(isCompatible, Is.False);
        }

        [Test]
        public void IsCollectorCompatible_WithNull_ReturnsFalse()
        {
            // Act
            bool isCompatible = _viewModel.IsCollectorCompatible(null, 4, 500);

            // Assert
            Assert.That(isCompatible, Is.False);
        }

        #endregion

        #region GetRecommendation Tests

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
            _viewModel.SelectedCollector = new Collector { MaxFlowRate = 0.5 }; // м³/ч = 500 л/ч
            _viewModel.TotalFlowRate = 600;

            // Act
            var recommendation = _viewModel.GetRecommendation();

            // Assert
            Assert.That(recommendation, Does.Contain("превышает"));
        }

        [Test]
        public void GetRecommendation_ReturnsWarningWhenCircuitsExceeded()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { Circuits = 4, MaxFlowRate = 2.0 };
            _viewModel.CircuitCount = 6;
            _viewModel.TotalFlowRate = 500;

            // Act
            var recommendation = _viewModel.GetRecommendation();

            // Assert
            Assert.That(recommendation, Does.Contain("количество контуров"));
        }

        [Test]
        public void GetRecommendation_ReturnsCorrectWhenOk()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { Circuits = 6, MaxFlowRate = 1.5 };
            _viewModel.CircuitCount = 4;
            _viewModel.TotalFlowRate = 800;

            // Act
            var recommendation = _viewModel.GetRecommendation();

            // Assert
            Assert.That(recommendation, Does.Contain("корректно"));
        }

        #endregion

        #region Computed Properties Tests

        [Test]
        public void SelectedCollectorInfo_ReturnsDescription()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector
            {
                FullName = "Тестовый коллектор",
                Circuits = 4,
                Kv = 1.2,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };

            // Assert
            Assert.That(_viewModel.SelectedCollectorInfo, Does.Contain("Тестовый коллектор"));
        }

        [Test]
        public void SelectedCollectorInfo_WhenNull_ReturnsDefault()
        {
            // Arrange
            _viewModel.SelectedCollector = null;

            // Assert
            Assert.That(_viewModel.SelectedCollectorInfo, Is.EqualTo("Коллектор не выбран"));
        }

        [Test]
        public void SelectedCollectorName_ReturnsName()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { Name = "HKV-D 4" };

            // Assert
            Assert.That(_viewModel.SelectedCollectorName, Is.EqualTo("HKV-D 4"));
        }

        [Test]
        public void SelectedCollectorName_WhenNull_ReturnsDash()
        {
            // Arrange
            _viewModel.SelectedCollector = null;

            // Assert
            Assert.That(_viewModel.SelectedCollectorName, Is.EqualTo("—"));
        }

        [Test]
        public void SelectedCollectorKv_FormatsCorrectly()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { Kv = 1.234 };

            // Assert - проверяем, что формат содержит правильное значение (с учётом локали)
            Assert.That(_viewModel.SelectedCollectorKv, Does.Contain("1.23").Or.Contains("1,23"));
            Assert.That(_viewModel.SelectedCollectorKv, Does.Contain("м³/ч"));
        }

        [Test]
        public void SelectedCollectorMaxFlow_FormatsCorrectly()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { MaxFlowRate = 1.5 }; // м³/ч

            // Assert
            Assert.That(_viewModel.SelectedCollectorMaxFlow, Is.EqualTo("1500 л/ч"));
        }

        [Test]
        public void SelectedCollectorMaxPressure_FormatsCorrectly()
        {
            // Arrange
            _viewModel.SelectedCollector = new Collector { MaxPressure = 320 };

            // Assert
            Assert.That(_viewModel.SelectedCollectorMaxPressure, Is.EqualTo("320 мбар"));
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

        [Test]
        public void AvailableCircuitCountsHKV_ReturnsCorrectValues()
        {
            // Assert
            Assert.That(_viewModel.AvailableCircuitCountsHKV, Is.EqualTo(new[] { 2, 4, 6, 8, 10, 12 }));
        }

        [Test]
        public void AvailableCollectorTypes_ReturnsCorrectValues()
        {
            // Assert
            Assert.That(_viewModel.AvailableCollectorTypes, Is.EqualTo(new[] { CollectorType.HKV, CollectorType.IV }));
        }

        #endregion
    }
}