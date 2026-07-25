using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Unit-тесты CircuitsValidator.
    /// После устранения прямых вызовов MessageBox валидатор тестируется
    /// через шов IDialogService без WPF/UI-потока.
    /// </summary>
    [TestFixture]
    public class CircuitsValidatorTests
    {
        private Mock<IDialogService> _dialogServiceMock = null!;
        private CircuitsValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _dialogServiceMock = new Mock<IDialogService>();
            _validator = new CircuitsValidator(_dialogServiceMock.Object);
        }

        private static CollectorData CollectorWithCircuits(int count)
        {
            var collector = new CollectorData(1);
            for (var i = 0; i < count; i++)
            {
                collector.Circuits.Add(new CircuitRow());
            }
            return collector;
        }

        #region CanRemoveCircuit

        [Test]
        public void CanRemoveCircuit_NullCircuit_ReturnsFalse()
        {
            Assert.That(_validator.CanRemoveCircuit(null, CollectorWithCircuits(2)), Is.False);
        }

        [Test]
        public void CanRemoveCircuit_NullCollector_ReturnsFalse()
        {
            Assert.That(_validator.CanRemoveCircuit(new CircuitRow(), null), Is.False);
        }

        [Test]
        public void CanRemoveCircuit_SingleCircuitInCollector_ReturnsFalse()
        {
            var collector = CollectorWithCircuits(1);
            Assert.That(_validator.CanRemoveCircuit(collector.Circuits[0], collector), Is.False,
                "Последний контур коллектора удалять нельзя");
        }

        [Test]
        public void CanRemoveCircuit_TwoCircuitsInCollector_ReturnsTrue()
        {
            var collector = CollectorWithCircuits(2);
            Assert.That(_validator.CanRemoveCircuit(collector.Circuits[0], collector), Is.True);
        }

        #endregion

        #region CanRemoveCollector

        [Test]
        public void CanRemoveCollector_NullCollector_ReturnsFalse()
        {
            Assert.That(_validator.CanRemoveCollector(null, 2), Is.False);
        }

        [Test]
        public void CanRemoveCollector_SingleCollector_ReturnsFalse()
        {
            Assert.That(_validator.CanRemoveCollector(new CollectorData(1), 1), Is.False,
                "Последний коллектор удалять нельзя");
        }

        [Test]
        public void CanRemoveCollector_TwoCollectors_ReturnsTrue()
        {
            Assert.That(_validator.CanRemoveCollector(new CollectorData(1), 2), Is.True);
        }

        #endregion

        #region ConfirmDeleteCircuit

        [Test]
        public void ConfirmDeleteCircuit_UserConfirms_ReturnsTrue()
        {
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Warning))
                .Returns(DialogResult.Yes);

            Assert.That(_validator.ConfirmDeleteCircuit(3), Is.True);
            _dialogServiceMock.Verify(
                d => d.Show(It.Is<string>(s => s.Contains("№3")), "Удаление контура", DialogButtons.YesNo, DialogIcon.Warning),
                Times.Once);
        }

        [Test]
        public void ConfirmDeleteCircuit_UserDeclines_ReturnsFalse()
        {
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Warning))
                .Returns(DialogResult.No);

            Assert.That(_validator.ConfirmDeleteCircuit(3), Is.False);
        }

        #endregion

        #region ConfirmDeleteCollector

        [Test]
        public void ConfirmDeleteCollector_UserConfirms_ReturnsTrue()
        {
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Warning))
                .Returns(DialogResult.Yes);

            Assert.That(_validator.ConfirmDeleteCollector(2), Is.True);
            _dialogServiceMock.Verify(
                d => d.Show(It.Is<string>(s => s.Contains("№2")), "Удаление коллектора", DialogButtons.YesNo, DialogIcon.Warning),
                Times.Once);
        }

        [Test]
        public void ConfirmDeleteCollector_UserDeclines_ReturnsFalse()
        {
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Warning))
                .Returns(DialogResult.No);

            Assert.That(_validator.ConfirmDeleteCollector(2), Is.False);
        }

        #endregion
    }
}
