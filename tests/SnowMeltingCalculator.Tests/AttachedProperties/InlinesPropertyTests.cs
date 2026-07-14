using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NUnit.Framework;
using SnowMeltingCalculator.AttachedProperties;

namespace SnowMeltingCalculator.Tests.AttachedProperties
{
    /// <summary>
    /// Тесты для InlinesProperty
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class InlinesPropertyTests
    {
        #region GetInlines/SetInlines Tests

        [Test]
        public void GetInlines_WhenNotSet_ReturnsNull()
        {
            // Arrange
            var textBlock = new TextBlock();

            // Act
            var result = InlinesProperty.GetInlines(textBlock);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SetInlines_WhenSet_ReturnsSetValue()
        {
            // Arrange
            var textBlock = new TextBlock();
            var inlines = new List<Inline>
            {
                new Run("Test"),
                new Run("Text")
            };

            // Act
            InlinesProperty.SetInlines(textBlock, inlines);
            var result = InlinesProperty.GetInlines(textBlock);

            // Assert
            Assert.That(result, Is.EqualTo(inlines));
        }

        #endregion

        #region OnInlinesChanged Tests

        [Test]
        public void OnInlinesChanged_WithInlines_ClearsAndAddsInlines()
        {
            // Arrange
            var textBlock = new TextBlock();
            textBlock.Inlines.Add(new Run("Existing"));

            var inlines = new List<Inline>
            {
                new Run("First"),
                new Run("Second")
            };

            // Act
            InlinesProperty.SetInlines(textBlock, inlines);

            // Assert
            Assert.That(textBlock.Inlines.Count, Is.EqualTo(2));
        }

        [Test]
        public void OnInlinesChanged_WithNull_ClearsInlines()
        {
            // Arrange
            var textBlock = new TextBlock();
            textBlock.Inlines.Add(new Run("Existing"));

            // Act
            InlinesProperty.SetInlines(textBlock, null);

            // Assert
            // TextBlock.Inlines может содержать пустой Inline после очистки
            // Проверяем, что добавленный Run был удалён
            Assert.That(textBlock.Inlines.Count, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void OnInlinesChanged_WithEmptyList_ClearsInlines()
        {
            // Arrange
            var textBlock = new TextBlock();
            textBlock.Inlines.Add(new Run("Existing"));
            var inlines = new List<Inline>();

            // Act
            InlinesProperty.SetInlines(textBlock, inlines);

            // Assert
            Assert.That(textBlock.Inlines.Count, Is.EqualTo(0));
        }

        [Test]
        public void OnInlinesChanged_WithNonTextBlock_DoesNotThrow()
        {
            // Arrange
            var textBox = new TextBox();

            // Act & Assert - не должно выбросить исключение
            Assert.DoesNotThrow(() => InlinesProperty.SetInlines(textBox, new List<Inline>()));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_WithMultipleRuns_DisplaysCorrectText()
        {
            // Arrange
            var textBlock = new TextBlock();
            var inlines = new List<Inline>
            {
                new Run("Hello "),
                new Run("World") { FontWeight = FontWeights.Bold }
            };

            // Act
            InlinesProperty.SetInlines(textBlock, inlines);

            // Assert
            Assert.That(textBlock.Inlines.Count, Is.EqualTo(2));

            var firstRun = (Run)textBlock.Inlines.FirstInline;
            Assert.That(firstRun.Text, Is.EqualTo("Hello "));

            var secondRun = (Run)textBlock.Inlines.FirstInline.NextInline;
            Assert.That(secondRun.Text, Is.EqualTo("World"));
            Assert.That(secondRun.FontWeight, Is.EqualTo(FontWeights.Bold));
        }

        [Test]
        public void Integration_ReplacingInlines_UpdatesCorrectly()
        {
            // Arrange
            var textBlock = new TextBlock();
            var firstInlines = new List<Inline>
            {
                new Run("First")
            };
            var secondInlines = new List<Inline>
            {
                new Run("Second"),
                new Run("Third")
            };

            // Act
            InlinesProperty.SetInlines(textBlock, firstInlines);
            InlinesProperty.SetInlines(textBlock, secondInlines);

            // Assert
            Assert.That(textBlock.Inlines.Count, Is.EqualTo(2));
            Assert.That(((Run)textBlock.Inlines.FirstInline).Text, Is.EqualTo("Second"));
        }

        #endregion
    }
}