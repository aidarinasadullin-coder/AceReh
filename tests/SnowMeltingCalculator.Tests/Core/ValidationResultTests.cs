using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Тесты для унифицированного результата валидации
    /// </summary>
    [TestFixture]
    public class ValidationResultTests
    {
        #region Success / Failure

        [Test]
        public void Success_ReturnsValidResult()
        {
            // Act
            var result = ValidationResult.Success();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void Failure_WithSingleError_ReturnsInvalidResult()
        {
            // Act
            var result = ValidationResult.Failure("Ошибка валидации");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("Ошибка валидации"));
            Assert.That(result.Errors[0].PropertyName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Failure_WithMultipleErrors_ReturnsInvalidResult()
        {
            // Act
            var result = ValidationResult.Failure(new[] { "Ошибка 1", "Ошибка 2" });

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Any(e => e.Message == "Ошибка 1"), Is.True);
            Assert.That(result.Errors.Any(e => e.Message == "Ошибка 2"), Is.True);
        }

        [Test]
        public void Failure_WithValidationErrors_PreservesPropertyName()
        {
            // Arrange
            var errors = new[]
            {
                new ValidationError("Width", "Слишком узко"),
                new ValidationError("Height", "Слишком низко")
            };

            // Act
            var result = ValidationResult.Failure(errors);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Any(e => e.PropertyName == "Width" && e.Message == "Слишком узко"), Is.True);
            Assert.That(result.Errors.Any(e => e.PropertyName == "Height" && e.Message == "Слишком низко"), Is.True);
        }

        #endregion

        #region AddError / AddWarning

        [Test]
        public void AddError_String_SetsInvalid()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            result.AddError("Ошибка");

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("Ошибка"));
        }

        [Test]
        public void AddError_WithPropertyName_PreservesPropertyName()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            result.AddError("Length", "Длина превышена");

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].PropertyName, Is.EqualTo("Length"));
            Assert.That(result.Errors[0].Message, Is.EqualTo("Длина превышена"));
        }

        [Test]
        public void AddWarning_DoesNotSetInvalid()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            result.AddWarning("Предупреждение");

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
            Assert.That(result.HasWarnings, Is.True);
        }

        #endregion

        #region Merge

        [Test]
        public void Merge_SuccessAndFailure_MakesResultInvalid()
        {
            // Arrange
            var success = ValidationResult.Success();
            var failure = ValidationResult.Failure("Ошибка");

            // Act
            success.Merge(failure);

            // Assert
            Assert.That(success.IsValid, Is.False);
            Assert.That(success.Errors.Count, Is.EqualTo(1));
            Assert.That(success.Errors[0].Message, Is.EqualTo("Ошибка"));
        }

        [Test]
        public void Merge_FailureAndFailure_AggregatesErrors()
        {
            // Arrange
            var first = ValidationResult.Failure("Ошибка 1");
            var second = ValidationResult.Failure("Ошибка 2");

            // Act
            first.Merge(second);

            // Assert
            Assert.That(first.IsValid, Is.False);
            Assert.That(first.Errors.Count, Is.EqualTo(2));
            Assert.That(first.Errors.Any(e => e.Message == "Ошибка 1"), Is.True);
            Assert.That(first.Errors.Any(e => e.Message == "Ошибка 2"), Is.True);
        }

        [Test]
        public void Merge_FailureAndSuccess_KeepsResultInvalid()
        {
            // Arrange
            var failure = ValidationResult.Failure("Ошибка");
            var success = ValidationResult.Success();

            // Act
            failure.Merge(success);

            // Assert
            Assert.That(failure.IsValid, Is.False);
            Assert.That(failure.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void Merge_MergesWarnings()
        {
            // Arrange
            var first = ValidationResult.Success();
            first.AddWarning("Предупреждение 1");
            var second = ValidationResult.Success();
            second.AddWarning("Предупреждение 2");

            // Act
            first.Merge(second);

            // Assert
            Assert.That(first.IsValid, Is.True);
            Assert.That(first.Warnings.Count, Is.EqualTo(2));
            Assert.That(first.Warnings.Any(w => w == "Предупреждение 1"), Is.True);
            Assert.That(first.Warnings.Any(w => w == "Предупреждение 2"), Is.True);
        }

        [Test]
        public void Merge_Null_DoesNothing()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            result.Merge(null!);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void Merge_BugFix_WhenOtherIsInvalid_ExplicitlySetsIsValidFalse()
        {
            // Regression: Hydraulics.ValidationResult.Merge did not set IsValid = false
            // when merging an invalid result whose Errors list happened to be empty.
            // The unified implementation must copy Construction semantics:
            // result is invalid if any merged result is invalid.

            // Arrange
            var target = ValidationResult.Success();
            var invalidOther = new ValidationResult
            {
                IsValid = false
            };

            // Act
            target.Merge(invalidOther);

            // Assert
            Assert.That(target.IsValid, Is.False);
        }

        #endregion

        #region GetAllMessages / ToString

        [Test]
        public void GetAllMessages_ReturnsErrorsAndWarnings()
        {
            // Arrange
            var result = ValidationResult.Failure("Ошибка");
            result.AddWarning("Предупреждение");

            // Act
            var messages = result.GetAllMessages();

            // Assert
            Assert.That(messages.Count, Is.EqualTo(2));
            Assert.That(messages.Any(m => m == "Ошибка"), Is.True);
            Assert.That(messages.Any(m => m == "Предупреждение"), Is.True);
        }

        [Test]
        public void ToString_Success_ReturnsSuccessText()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act & Assert
            Assert.That(result.ToString(), Does.Contain("успешно"));
        }

        [Test]
        public void ToString_Failure_ReturnsErrorCount()
        {
            // Arrange
            var result = ValidationResult.Failure("Ошибка");

            // Act & Assert
            Assert.That(result.ToString(), Does.Contain("Ошибки"));
        }

        #endregion
    }
}
