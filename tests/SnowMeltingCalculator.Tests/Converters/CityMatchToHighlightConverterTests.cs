using System.Globalization;
using System.Windows.Documents;
using System.Windows.Media;
using NUnit.Framework;
using SnowMeltingCalculator.Converters;

namespace SnowMeltingCalculator.Tests.Converters
{
    /// <summary>
    /// Тесты для CityMatchToHighlightConverter
    /// </summary>
    [TestFixture]
    public class CityMatchToHighlightConverterTests
    {
        private CityMatchToHighlightConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new CityMatchToHighlightConverter();
        }

        #region Convert Tests

        [Test]
        public void Convert_WithHighlightedText_ReturnsInlines()
        {
            // Arrange
            var text = "Мос**ква**";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.That(result, Is.InstanceOf<System.Collections.Generic.List<Inline>>());
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(2));
        }

        [Test]
        public void Convert_WithHighlightedMiddle_ReturnsCorrectInlines()
        {
            // Arrange
            var text = "Мос**ква**";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(2));

            // Первая часть - обычный текст
            var firstRun = (Run)inlines[0];
            Assert.That(firstRun.Text, Is.EqualTo("Мос"));
            Assert.That(firstRun.FontWeight, Is.Not.EqualTo(System.Windows.FontWeights.Bold));

            // Вторая часть - подсвеченный текст
            var secondRun = (Run)inlines[1];
            Assert.That(secondRun.Text, Is.EqualTo("ква"));
            Assert.That(secondRun.FontWeight, Is.EqualTo(System.Windows.FontWeights.Bold));
        }

        [Test]
        public void Convert_WithHighlightedStart_ReturnsCorrectInlines()
        {
            // Arrange
            var text = "**Мос**ква";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(2));

            // Первая часть - подсвеченный текст
            var firstRun = (Run)inlines[0];
            Assert.That(firstRun.Text, Is.EqualTo("Мос"));
            Assert.That(firstRun.FontWeight, Is.EqualTo(System.Windows.FontWeights.Bold));

            // Вторая часть - обычный текст
            var secondRun = (Run)inlines[1];
            Assert.That(secondRun.Text, Is.EqualTo("ква"));
        }

        [Test]
        public void Convert_WithMultipleHighlights_ReturnsCorrectInlines()
        {
            // Arrange
            // Формат: "**Мос**ква** - столица**"
            // Разбор: ["", "Мос", "ква", " - столица", ""]
            // Нечётные индексы (1, 3) - подсвеченные: "Мос", " - столица"
            // Чётные индексы (0, 2, 4) - обычные: "", "ква", ""
            // Пустые строки пропускаются, остаются: "Мос" (bold), "ква" (normal), " - столица" (bold)
            var text = "**Мос**ква** - столица**";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(3));

            // Чередование: подсвеченный, обычный, подсвеченный
            Assert.That(((Run)inlines[0]).FontWeight, Is.EqualTo(System.Windows.FontWeights.Bold));
            Assert.That(((Run)inlines[0]).Text, Is.EqualTo("Мос"));
            Assert.That(((Run)inlines[1]).FontWeight, Is.Not.EqualTo(System.Windows.FontWeights.Bold));
            Assert.That(((Run)inlines[1]).Text, Is.EqualTo("ква"));
            Assert.That(((Run)inlines[2]).FontWeight, Is.EqualTo(System.Windows.FontWeights.Bold));
            Assert.That(((Run)inlines[2]).Text, Is.EqualTo(" - столица"));
        }

        [Test]
        public void Convert_WithNoHighlight_ReturnsSingleInline()
        {
            // Arrange
            var text = "Москва";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(1));

            var run = (Run)inlines[0];
            Assert.That(run.Text, Is.EqualTo("Москва"));
            Assert.That(run.FontWeight, Is.Not.EqualTo(System.Windows.FontWeights.Bold));
        }

        [Test]
        public void Convert_WithEmptyString_ReturnsEmptyList()
        {
            // Arrange
            var text = "";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(0));
        }

        [Test]
        public void Convert_WithNull_ReturnsEmptyList()
        {
            // Arrange
            string text = null!;

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(0));
        }

        [Test]
        public void Convert_WithHighlightedText_HasCorrectColors()
        {
            // Arrange
            var text = "Мос**ква**";

            // Act
            var result = _converter.Convert(text, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;

            // Обычный текст - чёрный REHAU (#1D1D1B)
            var normalRun = (Run)inlines[0];
            Assert.That(normalRun.Foreground, Is.InstanceOf<SolidColorBrush>());
            var normalBrush = (SolidColorBrush)normalRun.Foreground;
            Assert.That(normalBrush.Color.R, Is.EqualTo((byte)0x1D));
            Assert.That(normalBrush.Color.G, Is.EqualTo((byte)0x1D));
            Assert.That(normalBrush.Color.B, Is.EqualTo((byte)0x1B));

            // Подсвеченный текст - бирюзовый REHAU (#4FC7B5)
            var highlightRun = (Run)inlines[1];
            Assert.That(highlightRun.Foreground, Is.InstanceOf<SolidColorBrush>());
            var highlightBrush = (SolidColorBrush)highlightRun.Foreground;
            Assert.That(highlightBrush.Color.R, Is.EqualTo((byte)0x4F));
            Assert.That(highlightBrush.Color.G, Is.EqualTo((byte)0xC7));
            Assert.That(highlightBrush.Color.B, Is.EqualTo((byte)0xB5));
        }

        [Test]
        public void Convert_WithNonString_ReturnsEmptyList()
        {
            // Arrange
            var value = 123;

            // Act
            var result = _converter.Convert(value, typeof(object), null, CultureInfo.InvariantCulture);

            // Assert
            var inlines = (System.Collections.Generic.List<Inline>)result;
            Assert.That(inlines.Count, Is.EqualTo(0));
        }

        #endregion

        #region ConvertBack Tests

        [Test]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            var value = new System.Collections.Generic.List<Inline>();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(value, typeof(string), null, CultureInfo.InvariantCulture));
        }

        #endregion

        #region CreateInlines Tests

        [Test]
        public void CreateInlines_WithHighlightedText_ReturnsCorrectInlines()
        {
            // Arrange
            var text = "Мос**ква**";

            // Act
            var inlines = CityMatchToHighlightConverter.CreateInlines(text);

            // Assert
            Assert.That(inlines.Count, Is.EqualTo(2));
            Assert.That(((Run)inlines[0]).Text, Is.EqualTo("Мос"));
            Assert.That(((Run)inlines[1]).Text, Is.EqualTo("ква"));
        }

        [Test]
        public void CreateInlines_WithEmptyText_ReturnsEmptyList()
        {
            // Arrange
            var text = "";

            // Act
            var inlines = CityMatchToHighlightConverter.CreateInlines(text);

            // Assert
            Assert.That(inlines.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateInlines_WithNullText_ReturnsEmptyList()
        {
            // Arrange
            string text = null!;

            // Act
            var inlines = CityMatchToHighlightConverter.CreateInlines(text);

            // Assert
            Assert.That(inlines.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateInlines_WithCustomBrushes_UsesCustomBrushes()
        {
            // Arrange
            var text = "Мос**ква**";
            var highlightBrush = new SolidColorBrush(Colors.Red);
            var normalBrush = new SolidColorBrush(Colors.Blue);

            // Act
            var inlines = CityMatchToHighlightConverter.CreateInlines(text, highlightBrush, normalBrush);

            // Assert
            Assert.That(inlines.Count, Is.EqualTo(2));

            var normalRun = (Run)inlines[0];
            Assert.That(normalRun.Foreground, Is.EqualTo(normalBrush));

            var highlightRun = (Run)inlines[1];
            Assert.That(highlightRun.Foreground, Is.EqualTo(highlightBrush));
        }

        #endregion
    }
}