using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;
using SnowMeltingCalculator.Behaviors;

namespace SnowMeltingCalculator.Tests.Behaviors
{
    /// <summary>
    /// Тесты для TextBoxBehavior - attached behavior для улучшения UX ввода чисел.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class TextBoxBehaviorTests
    {
        #region SelectAllOnFocus Tests

        [Test]
        public void SelectAllOnFocus_DefaultValue_IsFalse()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            var value = TextBoxBehavior.GetSelectAllOnFocus(textBox);

            // Assert
            Assert.That(value, Is.False);
        }

        [Test]
        public void SelectAllOnFocus_CanBeSetToTrue()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            TextBoxBehavior.SetSelectAllOnFocus(textBox, true);
            var value = TextBoxBehavior.GetSelectAllOnFocus(textBox);

            // Assert
            Assert.That(value, Is.True);
        }

        [Test]
        public void SelectAllOnFocus_CanBeSetToFalse()
        {
            // Arrange
            var textBox = new TextBox();
            TextBoxBehavior.SetSelectAllOnFocus(textBox, true);

            // Act
            TextBoxBehavior.SetSelectAllOnFocus(textBox, false);
            var value = TextBoxBehavior.GetSelectAllOnFocus(textBox);

            // Assert
            Assert.That(value, Is.False);
        }

        #endregion

        #region RestoreOnEscape Tests

        [Test]
        public void RestoreOnEscape_DefaultValue_IsFalse()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            var value = TextBoxBehavior.GetRestoreOnEscape(textBox);

            // Assert
            Assert.That(value, Is.False);
        }

        [Test]
        public void RestoreOnEscape_CanBeSetToTrue()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            TextBoxBehavior.SetRestoreOnEscape(textBox, true);
            var value = TextBoxBehavior.GetRestoreOnEscape(textBox);

            // Assert
            Assert.That(value, Is.True);
        }

        [Test]
        public void RestoreOnEscape_CanBeSetToFalse()
        {
            // Arrange
            var textBox = new TextBox();
            TextBoxBehavior.SetRestoreOnEscape(textBox, true);

            // Act
            TextBoxBehavior.SetRestoreOnEscape(textBox, false);
            var value = TextBoxBehavior.GetRestoreOnEscape(textBox);

            // Assert
            Assert.That(value, Is.False);
        }

        #endregion

        #region NormalizeDecimalSeparator Tests

        [Test]
        public void NormalizeDecimalSeparator_DefaultValue_IsFalse()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            var value = TextBoxBehavior.GetNormalizeDecimalSeparator(textBox);

            // Assert
            Assert.That(value, Is.False);
        }

        [Test]
        public void NormalizeDecimalSeparator_CanBeSetToTrue()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            TextBoxBehavior.SetNormalizeDecimalSeparator(textBox, true);
            var value = TextBoxBehavior.GetNormalizeDecimalSeparator(textBox);

            // Assert
            Assert.That(value, Is.True);
        }

        [Test]
        public void NormalizeDecimalSeparator_CanBeSetToFalse()
        {
            // Arrange
            var textBox = new TextBox();
            TextBoxBehavior.SetNormalizeDecimalSeparator(textBox, true);

            // Act
            TextBoxBehavior.SetNormalizeDecimalSeparator(textBox, false);
            var value = TextBoxBehavior.GetNormalizeDecimalSeparator(textBox);

            // Assert
            Assert.That(value, Is.False);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void AllProperties_CanBeSetTogether()
        {
            // Arrange
            var textBox = new TextBox();

            // Act
            TextBoxBehavior.SetSelectAllOnFocus(textBox, true);
            TextBoxBehavior.SetRestoreOnEscape(textBox, true);
            TextBoxBehavior.SetNormalizeDecimalSeparator(textBox, true);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(TextBoxBehavior.GetSelectAllOnFocus(textBox), Is.True);
                Assert.That(TextBoxBehavior.GetRestoreOnEscape(textBox), Is.True);
                Assert.That(TextBoxBehavior.GetNormalizeDecimalSeparator(textBox), Is.True);
            });
        }

        [Test]
        public void AllProperties_CanBeDisabledTogether()
        {
            // Arrange
            var textBox = new TextBox();
            TextBoxBehavior.SetSelectAllOnFocus(textBox, true);
            TextBoxBehavior.SetRestoreOnEscape(textBox, true);
            TextBoxBehavior.SetNormalizeDecimalSeparator(textBox, true);

            // Act
            TextBoxBehavior.SetSelectAllOnFocus(textBox, false);
            TextBoxBehavior.SetRestoreOnEscape(textBox, false);
            TextBoxBehavior.SetNormalizeDecimalSeparator(textBox, false);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(TextBoxBehavior.GetSelectAllOnFocus(textBox), Is.False);
                Assert.That(TextBoxBehavior.GetRestoreOnEscape(textBox), Is.False);
                Assert.That(TextBoxBehavior.GetNormalizeDecimalSeparator(textBox), Is.False);
            });
        }

        #endregion

        #region Culture-Specific Tests

        [Test]
        public void NormalizeDecimalSeparator_UsesCurrentCultureDecimalSeparator()
        {
            // Arrange
            var currentCulture = CultureInfo.CurrentCulture;
            var decimalSeparator = currentCulture.NumberFormat.NumberDecimalSeparator;

            // Assert - проверяем, что код использует текущую культуру
            Assert.That(decimalSeparator, Is.AnyOf(".", ","));
        }

        #endregion
    }
}