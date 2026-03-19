using System.Globalization;
using NUnit.Framework;
using SnowMeltingCalculator.Converters;

namespace SnowMeltingCalculator.Tests.Converters
{
    /// <summary>
    /// Тесты для SidebarTooltipConverter
    /// </summary>
    [TestFixture]
    public class SidebarTooltipConverterTests
    {
        private SidebarTooltipConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new SidebarTooltipConverter();
        }

        [Test]
        public void Convert_WhenCollapsed_ReturnsExpandText()
        {
            // Arrange
            var value = true;

            // Act
            var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("Развернуть панель (Ctrl+B)"));
        }

        [Test]
        public void Convert_WhenExpanded_ReturnsCollapseText()
        {
            // Arrange
            var value = false;

            // Act
            var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("Свернуть панель (Ctrl+B)"));
        }

        [Test]
        public void Convert_WhenNull_ReturnsCollapseText()
        {
            // Arrange
            object value = null!;

            // Act
            var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("Свернуть панель (Ctrl+B)"));
        }

        [Test]
        public void Convert_WhenNotBool_ReturnsCollapseText()
        {
            // Arrange
            var value = "not a bool";

            // Act
            var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.EqualTo("Свернуть панель (Ctrl+B)"));
        }

        [Test]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            var value = "Свернуть панель (Ctrl+B)";

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => 
                _converter.ConvertBack(value, typeof(bool), null, CultureInfo.InvariantCulture));
        }
    }
}