using System.Globalization;
using System.Windows.Media;
using NUnit.Framework;
using SnowMeltingCalculator.Converters;

namespace SnowMeltingCalculator.Tests.Converters
{
    /// <summary>
    /// Тесты для PressureColorConverter
    /// </summary>
    [TestFixture]
    public class PressureColorConverterTests
    {
        private PressureColorConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new PressureColorConverter();
        }

        [Test]
        public void Convert_WhenPressureBelowLimit_ReturnsGreen()
        {
            // Arrange
            var pressure = 100.0; // мбар (< 320)

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)46));  // #2E7D32
            Assert.That(brush.Color.G, Is.EqualTo((byte)125));
            Assert.That(brush.Color.B, Is.EqualTo((byte)50));
        }

        [Test]
        public void Convert_WhenPressureAtLimit_ReturnsGreen()
        {
            // Arrange
            var pressure = 320.0; // мбар (= 320, граница)

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)46));  // #2E7D32
            Assert.That(brush.Color.G, Is.EqualTo((byte)125));
            Assert.That(brush.Color.B, Is.EqualTo((byte)50));
        }

        [Test]
        public void Convert_WhenPressureAboveLimit_ReturnsRed()
        {
            // Arrange
            var pressure = 400.0; // мбар (> 320)

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)211));  // #D32F2F
            Assert.That(brush.Color.G, Is.EqualTo((byte)47));
            Assert.That(brush.Color.B, Is.EqualTo((byte)47));
        }

        [Test]
        public void Convert_WhenPressureJustAboveLimit_ReturnsRed()
        {
            // Arrange
            var pressure = 320.1; // мбар (чуть выше границы)

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)211));  // #D32F2F
            Assert.That(brush.Color.G, Is.EqualTo((byte)47));
            Assert.That(brush.Color.B, Is.EqualTo((byte)47));
        }

        [Test]
        public void Convert_WhenPressureZero_ReturnsGreen()
        {
            // Arrange
            var pressure = 0.0; // мбар

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)46));  // #2E7D32
            Assert.That(brush.Color.G, Is.EqualTo((byte)125));
            Assert.That(brush.Color.B, Is.EqualTo((byte)50));
        }

        [Test]
        public void Convert_WhenPressureNegative_ReturnsGreen()
        {
            // Arrange
            var pressure = -10.0; // мбар (отрицательное значение)

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)46));  // #2E7D32
            Assert.That(brush.Color.G, Is.EqualTo((byte)125));
            Assert.That(brush.Color.B, Is.EqualTo((byte)50));
        }

        [Test]
        public void Convert_WhenPressureVeryHigh_ReturnsRed()
        {
            // Arrange
            var pressure = 1000.0; // мбар (очень высокое)

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)211));  // #D32F2F
            Assert.That(brush.Color.G, Is.EqualTo((byte)47));
            Assert.That(brush.Color.B, Is.EqualTo((byte)47));
        }

        [Test]
        public void Convert_WhenNull_ReturnsBlack()
        {
            // Arrange
            object value = null!;

            // Act
            var result = _converter.Convert(value, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Colors.Black));
        }

        [Test]
        public void Convert_WhenNotDouble_ReturnsBlack()
        {
            // Arrange
            var value = "not a double";

            // Act
            var result = _converter.Convert(value, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Colors.Black));
        }

        [Test]
        public void Convert_WhenInt_ReturnsCorrectColor()
        {
            // Arrange
            var pressure = 100; // int, не double

            // Act
            var result = _converter.Convert(pressure, typeof(Brush), null, CultureInfo.InvariantCulture);

            // Assert
            // int не является double, поэтому должен вернуть чёрный
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color, Is.EqualTo(Colors.Black));
        }

        [Test]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            var value = new SolidColorBrush(Colors.Green);

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(value, typeof(double), null, CultureInfo.InvariantCulture));
        }
    }
}