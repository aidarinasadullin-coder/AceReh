using SnowMeltingCalculator.Converters;
using NUnit.Framework;
using System;
using System.Globalization;

namespace SnowMeltingCalculator.Tests.Converters
{
    /// <summary>
    /// Тесты для ValveTurnsToFractionConverter
    /// </summary>
    [TestFixture]
    public class ValveTurnsToFractionConverterTests
    {
        private ValveTurnsToFractionConverter _converter = null!;

        [SetUp]
        public void SetUp()
        {
            _converter = new ValveTurnsToFractionConverter();
        }

        [Test]
        public void Convert_Zero_ReturnsZero()
        {
            // Act
            var result = _converter.Convert(0.0, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("0"));
        }

        [Test]
        public void Convert_Quarter_ReturnsQuarterSymbol()
        {
            // Act
            var result = _converter.Convert(0.25, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("¼"));
        }

        [Test]
        public void Convert_Half_ReturnsHalfSymbol()
        {
            // Act
            var result = _converter.Convert(0.5, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("½"));
        }

        [Test]
        public void Convert_ThreeQuarters_ReturnsThreeQuartersSymbol()
        {
            // Act
            var result = _converter.Convert(0.75, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("¾"));
        }

        [Test]
        public void Convert_One_ReturnsOne()
        {
            // Act
            var result = _converter.Convert(1.0, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("1"));
        }

        [Test]
        public void Convert_OneAndQuarter_ReturnsOneAndQuarter()
        {
            // Act
            var result = _converter.Convert(1.25, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("1 ¼"));
        }

        [Test]
        public void Convert_TwoAndHalf_ReturnsTwoAndHalf()
        {
            // Act
            var result = _converter.Convert(2.5, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("2 ½"));
        }

        [Test]
        public void Convert_TwoAndThreeQuarters_ReturnsTwoAndThreeQuarters()
        {
            // Act
            var result = _converter.Convert(2.75, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("2 ¾"));
        }

        [Test]
        public void Convert_Eight_ReturnsEight()
        {
            // Act
            var result = _converter.Convert(8.0, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("8"));
        }

        [Test]
        public void Convert_RoundsToQuarter()
        {
            // Act - 1.24 должно округлиться до 1.25
            var result = _converter.Convert(1.24, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("1 ¼"));
        }

        [Test]
        public void Convert_RoundsToHalf()
        {
            // Act - 1.49 должно округлиться до 1.5
            var result = _converter.Convert(1.49, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("1 ½"));
        }

        [Test]
        public void Convert_Null_ReturnsEmptyString()
        {
            // Act
            var result = _converter.Convert(null!, typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Convert_NonDouble_ReturnsToString()
        {
            // Act
            var result = _converter.Convert("test", typeof(string), null!, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void ConvertBack_ThrowsNotImplemented()
        {
            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack("1 ¼", typeof(double), null!, CultureInfo.InvariantCulture));
        }
    }
}