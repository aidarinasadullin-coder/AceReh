using System.ComponentModel;
using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    /// <summary>
    /// Тесты утечки обработчиков событий в CircuitsViewModel.
    /// </summary>
    [TestFixture]
    public class CircuitsViewModelEventLeakTests
    {
        private Mock<ICircuitsCalculator> _circuitsCalculatorMock = null!;
        private Mock<IGlycolDataService> _glycolServiceMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<ICircuitsValidator> _validatorMock = null!;
        private Mock<ICollectorTypeSelector> _collectorTypeSelectorMock = null!;
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        private CalculationContext _calculationContext = null!;
        private CircuitsViewModel _viewModel = null!;
        private ProjectSession _canonicalSession = null!;

        [SetUp]
        public void Setup()
        {
            _circuitsCalculatorMock = new Mock<ICircuitsCalculator>();
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            _validatorMock = new Mock<ICircuitsValidator>();
            _collectorTypeSelectorMock = new Mock<ICollectorTypeSelector>();
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();

            _calculationContext = new CalculationContext();
            _calculationContext.UpdateClimate(new ClimateData(), "Climate");

            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            _validatorMock
                .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow?>(c => c == null), It.IsAny<CollectorData?>()))
                .Returns(false);
            _validatorMock
                .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow?>(c => c != null), It.Is<CollectorData?>(c => c == null)))
                .Returns(false);
            _validatorMock
                .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow?>(c => c != null), It.Is<CollectorData?>(c => c != null)))
                .Returns((CircuitRow? circuit, CollectorData? collector) => collector!.Circuits.Count > 1);
            _validatorMock
                .Setup(v => v.CanRemoveCollector(It.Is<CollectorData?>(c => c == null), It.IsAny<int>()))
                .Returns(false);
            _validatorMock
                .Setup(v => v.CanRemoveCollector(It.Is<CollectorData?>(c => c != null), It.IsAny<int>()))
                .Returns((CollectorData? collector, int count) => count > 1);
            _validatorMock
                .Setup(v => v.ConfirmDeleteCircuit(It.IsAny<int>()))
                .Returns(true);
            _validatorMock
                .Setup(v => v.ConfirmDeleteCollector(It.IsAny<int>()))
                .Returns(true);

            _collectorTypeSelectorMock
                .Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>()))
                .Returns((CollectorData collector) => new CollectorSelectionResult
                {
                    CollectorType = "HKV-D (2-12 контуров)",
                    ValveType = ValveType.HKV_D
                });

            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(_calculationStateServiceMock.Object, _calculationContext);
            _canonicalSession = (ProjectSession)hydraulicsDependencies.Session;
            _viewModel = new CircuitsViewModel(
                _circuitsCalculatorMock.Object,
                _glycolServiceMock.Object,
                _calculationStateServiceMock.Object,
                _validatorMock.Object,
                _collectorTypeSelectorMock.Object,
                _calculationContext,
                 _markDirtyServiceMock.Object,
                 hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session
            );
        }

        [Test]
        public void CircuitsViewModel_EventLeak_ResetDoesNotDuplicateHandlers()
        {
            // Arrange: коллектор с двумя контурами
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            var circuit1 = new CircuitRow { CircuitNumber = 1, CircuitLength = 10 };
            var circuit2 = new CircuitRow { CircuitNumber = 2, CircuitLength = 20 };
            collector.Circuits.Add(circuit1);
            collector.Circuits.Add(circuit2);

            int propertyChangedCount = 0;
            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(CircuitRow.CircuitLength))
                {
                    propertyChangedCount++;
                }
            };
            circuit1.PropertyChanged += handler;

            try
            {
                // Act: сброс ViewModel очищает коллекцию и добавляет новый коллектор
                _viewModel.Reset();

                // Assert: изменение старого контура не должно приводить к дублированию
                // или утечке обработчиков — PropertyChanged срабатывает ровно один раз
                // (только наш тестовый обработчик, ViewModel уже отписался).
                _markDirtyServiceMock.Invocations.Clear();
                circuit1.CircuitLength = 50;

                Assert.That(propertyChangedCount, Is.EqualTo(1),
                    "PropertyChanged для CircuitLength должен вызываться ровно один раз");
                _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never,
                    "После Reset изменение старого контура не должно помечать проект изменённым");

                // Assert: новый контур после Reset получает ровно одну подписку ViewModel.
                // Канонический dirty-контракт (миграция phase 5): dirty-владение живёт в
                // ProjectSession.HydraulicsState — срез поднимает MarkDirty на aggregate
                // root, а не на IMarkDirtyService, инжектированном во ViewModel. Ровно
                // один переход IsDirty false->true доказывает отсутствие дублей подписок.
                var newCircuit = _viewModel.Collectors[0].Circuits[0];
                _canonicalSession.MarkClean();
                _markDirtyServiceMock.Invocations.Clear();

                var isDirtyTransitions = 0;
                PropertyChangedEventHandler dirtyHandler = (_, e) =>
                {
                    if (e.PropertyName == nameof(ProjectSession.IsDirty))
                    {
                        isDirtyTransitions++;
                    }
                };
                _canonicalSession.PropertyChanged += dirtyHandler;
                try
                {
                    newCircuit.CircuitLength = 100;

                    Assert.That(isDirtyTransitions, Is.EqualTo(1),
                        "Изменение нового контура после Reset должно поднять ровно один переход IsDirty на canonical session");
                    Assert.That(_canonicalSession.IsDirty, Is.True,
                        "После изменения нового контура canonical session должна быть помечена изменённой");
                }
                finally
                {
                    _canonicalSession.PropertyChanged -= dirtyHandler;
                }

                _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never,
                    "VM-инжектированный IMarkDirtyService больше не канал dirty — владение перенесено в canonical slice");
            }
            finally
            {
                circuit1.PropertyChanged -= handler;
            }
        }
    }
}
