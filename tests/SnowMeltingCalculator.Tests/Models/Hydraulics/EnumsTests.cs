using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для перечислений модуля гидравлики
    /// </summary>
    [TestFixture]
    public class EnumsTests
    {
        #region FlowRegime Tests

        [Test]
        public void FlowRegime_HasCorrectValues()
        {
            // Assert
            Assert.That((int)FlowRegime.Laminar, Is.EqualTo(0));
            Assert.That((int)FlowRegime.Transitional, Is.EqualTo(1));
            Assert.That((int)FlowRegime.Turbulent, Is.EqualTo(2));
        }

        [Test]
        public void FlowRegime_HasThreeValues()
        {
            // Arrange
            var values = System.Enum.GetValues<FlowRegime>();

            // Assert
            Assert.That(values.Length, Is.EqualTo(3));
        }

        [Test]
        public void FlowRegime_NamesAreCorrect()
        {
            // Assert
            Assert.That(FlowRegime.Laminar.ToString(), Is.EqualTo("Laminar"));
            Assert.That(FlowRegime.Transitional.ToString(), Is.EqualTo("Transitional"));
            Assert.That(FlowRegime.Turbulent.ToString(), Is.EqualTo("Turbulent"));
        }

        [Test]
        public void FlowRegime_CanParseFromString()
        {
            // Act & Assert
            Assert.That(System.Enum.Parse<FlowRegime>("Laminar"), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(System.Enum.Parse<FlowRegime>("Transitional"), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(System.Enum.Parse<FlowRegime>("Turbulent"), Is.EqualTo(FlowRegime.Turbulent));
        }

        #endregion

        #region GlycolType Tests

        [Test]
        public void GlycolType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)GlycolType.Ethylene, Is.EqualTo(0));
            Assert.That((int)GlycolType.Propylene, Is.EqualTo(1));
        }

        [Test]
        public void GlycolType_HasTwoValues()
        {
            // Arrange
            var values = System.Enum.GetValues<GlycolType>();

            // Assert
            Assert.That(values.Length, Is.EqualTo(2));
        }

        [Test]
        public void GlycolType_NamesAreCorrect()
        {
            // Assert
            Assert.That(GlycolType.Ethylene.ToString(), Is.EqualTo("Ethylene"));
            Assert.That(GlycolType.Propylene.ToString(), Is.EqualTo("Propylene"));
        }

        [Test]
        public void GlycolType_CanParseFromString()
        {
            // Act & Assert
            Assert.That(System.Enum.Parse<GlycolType>("Ethylene"), Is.EqualTo(GlycolType.Ethylene));
            Assert.That(System.Enum.Parse<GlycolType>("Propylene"), Is.EqualTo(GlycolType.Propylene));
        }

        #endregion

        #region CollectorType Tests

        [Test]
        public void CollectorType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)CollectorType.HKV, Is.EqualTo(0));
            Assert.That((int)CollectorType.IV, Is.EqualTo(1));
        }

        [Test]
        public void CollectorType_HasTwoValues()
        {
            // Arrange
            var values = System.Enum.GetValues<CollectorType>();

            // Assert
            Assert.That(values.Length, Is.EqualTo(2));
        }

        [Test]
        public void CollectorType_NamesAreCorrect()
        {
            // Assert
            Assert.That(CollectorType.HKV.ToString(), Is.EqualTo("HKV"));
            Assert.That(CollectorType.IV.ToString(), Is.EqualTo("IV"));
        }

        [Test]
        public void CollectorType_CanParseFromString()
        {
            // Act & Assert
            Assert.That(System.Enum.Parse<CollectorType>("HKV"), Is.EqualTo(CollectorType.HKV));
            Assert.That(System.Enum.Parse<CollectorType>("IV"), Is.EqualTo(CollectorType.IV));
        }

        #endregion
    }
}