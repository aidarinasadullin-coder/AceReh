using System;
using NUnit.Framework;
using SnowMeltingCalculator.Core.Extensions;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Тесты для DoubleExtensions
    /// </summary>
    [TestFixture]
    public class DoubleExtensionsTests
    {
        #region Тесты округления

        [Test]
        public void RoundTo_RoundsCorrectly()
        {
            // Arrange
            double value = 123.456789;

            // Act & Assert
            Assert.That(value.RoundTo(3), Is.EqualTo(123.457));
            Assert.That(value.RoundTo(2), Is.EqualTo(123.46));
            Assert.That(value.RoundTo(1), Is.EqualTo(123.5));
            Assert.That(value.RoundTo(0), Is.EqualTo(123.0));
        }

        [Test]
        public void RoundTo1_RoundsCorrectly()
        {
            // Arrange
            double value = 123.456;

            // Act
            double result = value.RoundTo1();

            // Assert
            Assert.That(result, Is.EqualTo(123.5));
        }

        [Test]
        public void RoundTo2_RoundsCorrectly()
        {
            // Arrange
            double value = 123.4567;

            // Act
            double result = value.RoundTo2();

            // Assert
            Assert.That(result, Is.EqualTo(123.46));
        }

        [Test]
        public void RoundTo3_RoundsCorrectly()
        {
            // Arrange
            double value = 123.45678;

            // Act
            double result = value.RoundTo3();

            // Assert
            Assert.That(result, Is.EqualTo(123.457));
        }

        [Test]
        public void RoundToInt_RoundsCorrectly()
        {
            // Arrange
            double value1 = 123.456;
            double value2 = 123.556;

            // Act
            int result1 = value1.RoundToInt();
            int result2 = value2.RoundToInt();

            // Assert
            Assert.That(result1, Is.EqualTo(123));
            Assert.That(result2, Is.EqualTo(124));
        }

        [Test]
        public void RoundTo_HandlesMidpointRounding()
        {
            // Arrange
            double value = 123.455;

            // Act
            double result = value.RoundTo(2);

            // Assert - MidpointRounding.AwayFromZero
            Assert.That(result, Is.EqualTo(123.46));
        }

        #endregion

        #region Тесты сравнения

        [Test]
        public void IsEqual_ReturnsTrueForEqualValues()
        {
            // Arrange
            double value1 = 123.456;
            double value2 = 123.456;

            // Act
            bool result = value1.IsEqual(value2);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEqual_ReturnsTrueForCloseValues()
        {
            // Arrange
            double value1 = 123.456;
            double value2 = 123.4560001;

            // Act
            bool result = value1.IsEqual(value2, 1e-6);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEqual_ReturnsFalseForDifferentValues()
        {
            // Arrange
            double value1 = 123.456;
            double value2 = 123.457;

            // Act
            bool result = value1.IsEqual(value2);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsZero_ReturnsTrueForZero()
        {
            // Arrange
            double value = 0.0;

            // Act
            bool result = value.IsZero();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsZero_ReturnsTrueForVerySmallValue()
        {
            // Arrange
            double value = 1e-10;

            // Act
            bool result = value.IsZero();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsZero_ReturnsFalseForNonZero()
        {
            // Arrange
            double value = 0.001;

            // Act
            bool result = value.IsZero();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsPositive_ReturnsTrueForPositive()
        {
            // Arrange
            double value = 123.456;

            // Act
            bool result = value.IsPositive();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsPositive_ReturnsFalseForZero()
        {
            // Arrange
            double value = 0.0;

            // Act
            bool result = value.IsPositive();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsPositive_ReturnsFalseForNegative()
        {
            // Arrange
            double value = -123.456;

            // Act
            bool result = value.IsPositive();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsNegative_ReturnsTrueForNegative()
        {
            // Arrange
            double value = -123.456;

            // Act
            bool result = value.IsNegative();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsNonNegative_ReturnsTrueForPositive()
        {
            // Arrange
            double value = 123.456;

            // Act
            bool result = value.IsNonNegative();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsNonNegative_ReturnsTrueForZero()
        {
            // Arrange
            double value = 0.0;

            // Act
            bool result = value.IsNonNegative();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsNonNegative_ReturnsFalseForNegative()
        {
            // Arrange
            double value = -123.456;

            // Act
            bool result = value.IsNonNegative();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsInRange_ReturnsTrueForValueInRange()
        {
            // Arrange
            double value = 50.0;

            // Act
            bool result = value.IsInRange(0.0, 100.0);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsInRange_ReturnsTrueForValueAtBoundary()
        {
            // Arrange
            double value1 = 0.0;
            double value2 = 100.0;

            // Act
            bool result1 = value1.IsInRange(0.0, 100.0);
            bool result2 = value2.IsInRange(0.0, 100.0);

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.True);
        }

        [Test]
        public void IsInRange_ReturnsFalseForValueOutOfRange()
        {
            // Arrange
            double value1 = -1.0;
            double value2 = 101.0;

            // Act
            bool result1 = value1.IsInRange(0.0, 100.0);
            bool result2 = value2.IsInRange(0.0, 100.0);

            // Assert
            Assert.That(result1, Is.False);
            Assert.That(result2, Is.False);
        }

        #endregion

        #region Тесты конвертации

        [Test]
        public void MmToM_ConvertsCorrectly()
        {
            // Arrange
            double mm = 1000.0;

            // Act
            double m = mm.MmToM();

            // Assert
            Assert.That(m, Is.EqualTo(1.0));
        }

        [Test]
        public void MToMm_ConvertsCorrectly()
        {
            // Arrange
            double m = 1.0;

            // Act
            double mm = m.MToMm();

            // Assert
            Assert.That(mm, Is.EqualTo(1000.0));
        }

        [Test]
        public void PaToMbar_ConvertsCorrectly()
        {
            // Arrange
            double pa = 32000.0;

            // Act
            double mbar = pa.PaToMbar();

            // Assert
            Assert.That(mbar, Is.EqualTo(320.0));
        }

        [Test]
        public void MbarToPa_ConvertsCorrectly()
        {
            // Arrange
            double mbar = 320.0;

            // Act
            double pa = mbar.MbarToPa();

            // Assert
            Assert.That(pa, Is.EqualTo(32000.0));
        }

        [Test]
        public void LhToM3h_ConvertsCorrectly()
        {
            // Arrange
            double lh = 1000.0;

            // Act
            double m3h = lh.LhToM3h();

            // Assert
            Assert.That(m3h, Is.EqualTo(1.0));
        }

        [Test]
        public void M3hToLh_ConvertsCorrectly()
        {
            // Arrange
            double m3h = 1.0;

            // Act
            double lh = m3h.M3hToLh();

            // Assert
            Assert.That(lh, Is.EqualTo(1000.0));
        }

        [Test]
        public void CelsiusToKelvin_ConvertsCorrectly()
        {
            // Arrange
            double celsius = 0.0;

            // Act
            double kelvin = celsius.CelsiusToKelvin();

            // Assert
            Assert.That(kelvin, Is.EqualTo(273.15).Within(0.001));
        }

        [Test]
        public void KelvinToCelsius_ConvertsCorrectly()
        {
            // Arrange
            double kelvin = 273.15;

            // Act
            double celsius = kelvin.KelvinToCelsius();

            // Assert
            Assert.That(celsius, Is.EqualTo(0.0).Within(0.001));
        }

        #endregion

        #region Тесты ограничения

        [Test]
        public void Clamp_ReturnsValueInRange()
        {
            // Arrange
            double value = 50.0;

            // Act
            double result = value.Clamp(0.0, 100.0);

            // Assert
            Assert.That(result, Is.EqualTo(50.0));
        }

        [Test]
        public void Clamp_ReturnsMinForValueBelowRange()
        {
            // Arrange
            double value = -10.0;

            // Act
            double result = value.Clamp(0.0, 100.0);

            // Assert
            Assert.That(result, Is.EqualTo(0.0));
        }

        [Test]
        public void Clamp_ReturnsMaxForValueAboveRange()
        {
            // Arrange
            double value = 110.0;

            // Act
            double result = value.Clamp(0.0, 100.0);

            // Assert
            Assert.That(result, Is.EqualTo(100.0));
        }

        [Test]
        public void ClampMin_ReturnsValueIfAboveMin()
        {
            // Arrange
            double value = 50.0;

            // Act
            double result = value.ClampMin(0.0);

            // Assert
            Assert.That(result, Is.EqualTo(50.0));
        }

        [Test]
        public void ClampMin_ReturnsMinIfBelowMin()
        {
            // Arrange
            double value = -10.0;

            // Act
            double result = value.ClampMin(0.0);

            // Assert
            Assert.That(result, Is.EqualTo(0.0));
        }

        [Test]
        public void ClampMax_ReturnsValueIfBelowMax()
        {
            // Arrange
            double value = 50.0;

            // Act
            double result = value.ClampMax(100.0);

            // Assert
            Assert.That(result, Is.EqualTo(50.0));
        }

        [Test]
        public void ClampMax_ReturnsMaxIfAboveMax()
        {
            // Arrange
            double value = 110.0;

            // Act
            double result = value.ClampMax(100.0);

            // Assert
            Assert.That(result, Is.EqualTo(100.0));
        }

        #endregion
    }
}