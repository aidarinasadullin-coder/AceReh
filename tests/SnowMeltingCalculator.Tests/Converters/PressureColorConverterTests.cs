using System.Globalization;
using System.Windows;
using System.Windows.Media;
using NUnit.Framework;
using SnowMeltingCalculator.Converters;

namespace SnowMeltingCalculator.Tests.Converters
{
    /// <summary>
    /// Тесты для PressureColorConverter (контракт Фазы 3 редизайна, ADR-007):
    /// порог задаётся ConverterParameter'ом в единицах значения;
    /// превышение → красный Brand.Red.Dark (#B60034), иначе UnsetValue
    /// (нейтральный цвет ячейки — эталон renders/03b).
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
        public void Convert_WhenAboveParameterLimit_ReturnsBrandRedDark()
        {
            // Удельные потери: предел 300 Па/м
            var result = _converter.Convert(312.0, typeof(Brush), "300", CultureInfo.InvariantCulture);

            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
            var brush = (SolidColorBrush)result;
            Assert.That(brush.Color.R, Is.EqualTo((byte)0xB6));
            Assert.That(brush.Color.G, Is.EqualTo((byte)0x00));
            Assert.That(brush.Color.B, Is.EqualTo((byte)0x34));
        }

        [Test]
        public void Convert_WhenBelowParameterLimit_ReturnsUnsetValue()
        {
            var result = _converter.Convert(196.0, typeof(Brush), "300", CultureInfo.InvariantCulture);

            Assert.That(result, Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void Convert_WhenAtParameterLimit_ReturnsUnsetValue()
        {
            var result = _converter.Convert(300.0, typeof(Brush), "300", CultureInfo.InvariantCulture);

            Assert.That(result, Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void Convert_TotalPressureLimitInPascals_ReturnsRedOnlyAboveLimit()
        {
            // Суммарные Δp (Па): предел 32000 Па = 320 мбар (паспортный предел HKV)
            Assert.That(_converter.Convert(31999.0, typeof(Brush), "32000", CultureInfo.InvariantCulture),
                Is.EqualTo(DependencyProperty.UnsetValue));
            Assert.That(_converter.Convert(32000.0, typeof(Brush), "32000", CultureInfo.InvariantCulture),
                Is.EqualTo(DependencyProperty.UnsetValue));
            Assert.That(_converter.Convert(32001.0, typeof(Brush), "32000", CultureInfo.InvariantCulture),
                Is.InstanceOf<SolidColorBrush>());
        }

        [Test]
        public void Convert_WhenParameterMissing_ReturnsUnsetValue()
        {
            Assert.That(_converter.Convert(400.0, typeof(Brush), null, CultureInfo.InvariantCulture),
                Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void Convert_WhenParameterNotANumber_ReturnsUnsetValue()
        {
            Assert.That(_converter.Convert(400.0, typeof(Brush), "not a number", CultureInfo.InvariantCulture),
                Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void Convert_WhenValueNull_ReturnsUnsetValue()
        {
            Assert.That(_converter.Convert(null!, typeof(Brush), "300", CultureInfo.InvariantCulture),
                Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void Convert_WhenValueNotDouble_ReturnsUnsetValue()
        {
            Assert.That(_converter.Convert("not a double", typeof(Brush), "300", CultureInfo.InvariantCulture),
                Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void Convert_ParameterWithInvariantDecimalPoint_IsParsed()
        {
            // Параметр парсится по InvariantCulture независимо от локали ОС
            var result = _converter.Convert(26.0, typeof(Brush), "25.5", CultureInfo.InvariantCulture);
            Assert.That(result, Is.InstanceOf<SolidColorBrush>());
        }

        [Test]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var value = new SolidColorBrush(Colors.Red);

            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(value, typeof(double), null, CultureInfo.InvariantCulture));
        }
    }
}
