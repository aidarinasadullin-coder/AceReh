// ================================================================================
// REHAU Снеготаяние - Тесты защиты записи шага укладки
// ================================================================================

using NUnit.Framework;
using SnowMeltingCalculator.Services.Navigation;

namespace SnowMeltingCalculator.Tests.Services.Navigation
{
    /// <summary>
    /// Тесты защиты записи PipeSpacing в CalculationStateService
    /// </summary>
    [TestFixture]
    public class CalculationStateServiceGuardTests
    {
        private CalculationStateService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new CalculationStateService();
        }

        [Test]
        public void BadSource_Throws()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _service.SetPipeSpacing(42, "BadSource"));
            Assert.That(ex!.Message, Is.EqualTo("SetPipeSpacing called from non-canonical source: BadSource"));
        }

        [Test]
        public void CanonicalSource_SetsAndRaisesEvent()
        {
            // Arrange
            var eventFired = false;
            var eventValue = 0;
            _service.PipeSpacingChanged += (sender, value) =>
            {
                eventFired = true;
                eventValue = value;
            };

            // Act
            _service.SetPipeSpacing(42, "ThermalViewModel");

            // Assert
            Assert.That(_service.PipeSpacing, Is.EqualTo(42));
            Assert.That(eventFired, Is.True);
            Assert.That(eventValue, Is.EqualTo(42));
        }

        [Test]
        public void ResultsViewModelLoadProjectSource_RequiresFlag()
        {
            // Arrange
            var eventFired = false;
            _service.PipeSpacingChanged += (sender, value) => eventFired = true;

            // Act & Assert - without flag throws
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _service.SetPipeSpacing(42, "ResultsViewModel.LoadProject"));
            Assert.That(ex!.Message, Is.EqualTo("SetPipeSpacing called from non-canonical source: ResultsViewModel.LoadProject"));
            Assert.That(_service.PipeSpacing, Is.Not.EqualTo(42));

            // Act - with flag succeeds
            _service.IsLoadProjectInProgress = true;
            _service.SetPipeSpacing(42, "ResultsViewModel.LoadProject");

            // Assert
            Assert.That(_service.PipeSpacing, Is.EqualTo(42));
            Assert.That(eventFired, Is.True);
        }
    }
}
