using System.Globalization;
using System.Windows.Media;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Converters;

namespace SnowMeltingCalculator.Tests.Views.Hydraulics
{
    /// <summary>
    /// Тесты для конвертера FlowRegimeToColorConverter
    /// </summary>
    [TestFixture]
    public class FlowRegimeToColorConverterTests
    {
        private FlowRegimeToColorConverter _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new FlowRegimeToColorConverter();
        }

        [Test]
        public void Convert_Laminar_ReturnsGreen()
        {
            // Arrange
            var regime = FlowRegime.Laminar;

            // Act
            var result = _converter.Convert(regime, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(46, 125, 50))); // Зелёный
        }

        [Test]
        public void Convert_Transitional_ReturnsOrange()
        {
            // Arrange
            var regime = FlowRegime.Transitional;

            // Act
            var result = _converter.Convert(regime, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(255, 152, 0))); // Оранжевый
        }

        [Test]
        public void Convert_Turbulent_ReturnsBlue()
        {
            // Arrange
            var regime = FlowRegime.Turbulent;

            // Act
            var result = _converter.Convert(regime, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(33, 150, 243))); // Синий
        }

        [Test]
        public void Convert_Null_ReturnsBlack()
        {
            // Arrange
            object regime = null;

            // Act
            var result = _converter.Convert(regime, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Colors.Black));
        }

        [Test]
        public void Convert_InvalidValue_ReturnsBlack()
        {
            // Arrange
            var invalidValue = "InvalidString";

            // Act
            var result = _converter.Convert(invalidValue, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Colors.Black));
        }

        [Test]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<System.NotImplementedException>(() =>
                _converter.ConvertBack(null, typeof(FlowRegime), null, CultureInfo.InvariantCulture));
        }
    }
}