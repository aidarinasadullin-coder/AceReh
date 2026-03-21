using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using System;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для ValveTurnsCalculator
    /// </summary>
    [TestFixture]
    public class ValveTurnsCalculatorTests
    {
        #region CalculateTurns Tests

        [Test]
        public void CalculateTurns_HKV_D_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.2; // Kv для HKV-D

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.HKV_D);

            // Assert
            // Формула: 4.2111×1.2³ - 6.7436×1.2² + 4.6613×1.2 - 0.712
            // Ожидаемое значение: ~2.5 оборота
            Assert.That(turns, Is.GreaterThan(2.0));
            Assert.That(turns, Is.LessThan(3.0));
        }

        [Test]
        public void CalculateTurns_IV_1_25_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.45; // Kv для IV 1¼"

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_25);

            // Assert
            // Формула: 5.1818 × 1.45 - 0.23 = 7.29361
            // Округление до 0.25: Math.Round(7.29361 * 4) / 4 = 7.25
            Assert.That(turns, Is.EqualTo(7.25).Within(0.01));
        }

        [Test]
        public void CalculateTurns_IV_1_5_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.5; // Kv для IV 1½"

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_5);

            // Assert
            // Формула: 5.122 × 1.5 - 0.2106 = 7.4724
            // Округление до 0.25: Math.Round(7.4724 * 4) / 4 = 7.5
            Assert.That(turns, Is.EqualTo(7.5).Within(0.01));
        }

[Test]
        public void CalculateTurns_HKV_D_FormulaCalculation()
        {
            // Arrange
            double kv = 2.0;

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.HKV_D);

            // Assert
            // Формула: 4.2111×2³ - 6.7436×2² + 4.6613×2 - 0.712
            // = 4.2111×8 - 6.7436×4 + 9.3226 - 0.712 = 15.325
            // Но ограничение: максимум 8 оборотов
            // Округление до 0.25: Math.Round(8 * 4) / 4 = 8
            Assert.That(turns, Is.EqualTo(8.0).Within(0.01));
        }

        [Test]
        public void CalculateTurns_IV_1_25_FormulaCalculation()
        {
            // Arrange
            double kv = 2.0;

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_25);

            // Assert
            // Формула: 5.1818 × 2 - 0.23 = 10.1336
            // Но ограничение: максимум 8 оборотов
            // Округление до 0.25: Math.Round(8 * 4) / 4 = 8
            Assert.That(turns, Is.EqualTo(8.0).Within(0.01));
        }

        [Test]
        public void CalculateTurns_IV_1_5_FormulaCalculation()
        {
            // Arrange
            double kv = 2.0;

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_5);

            // Assert
            // Формула: 5.122 × 2 - 0.2106 = 10.0334
            // Но ограничение: максимум 8 оборотов
            // Округление до 0.25: Math.Round(8 * 4) / 4 = 8
            Assert.That(turns, Is.EqualTo(8.0).Within(0.01));
        }

        [Test]
        public void CalculateTurns_RoundsToQuarter()
        {
            // Arrange
            double kv = 1.5;

            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_5);

            // Assert - проверяем, что результат округлён до 0.25
            // Результат должен быть кратен 0.25
            double remainder = turns * 4 % 1;
            Assert.That(remainder, Is.EqualTo(0).Within(0.001), "Результат должен быть округлён до 0.25");
        }

        [Test]
        public void CalculateTurns_InvalidValveType_ThrowsException()
        {
            // Arrange
            double kv = 1.0;
            var invalidType = (ValveType)999;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                ValveTurnsCalculator.CalculateTurns(kv, invalidType));
        }

        #endregion

        #region GetDefaultKv Tests

        [Test]
        public void GetDefaultKv_HKV_D_ReturnsCorrectValue()
        {
            // Act
            double kv = ValveTurnsCalculator.GetDefaultKv(ValveType.HKV_D);

            // Assert
            Assert.That(kv, Is.EqualTo(1.2));
        }

        [Test]
        public void GetDefaultKv_IV_1_25_ReturnsCorrectValue()
        {
            // Act
            double kv = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_25);

            // Assert
            Assert.That(kv, Is.EqualTo(1.45));
        }

        [Test]
        public void GetDefaultKv_IV_1_5_ReturnsCorrectValue()
        {
            // Act
            double kv = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_5);

            // Assert
            Assert.That(kv, Is.EqualTo(1.5));
        }

        [Test]
        public void GetDefaultKv_InvalidValveType_ThrowsException()
        {
            // Arrange
            var invalidType = (ValveType)999;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                ValveTurnsCalculator.GetDefaultKv(invalidType));
        }

        [Test]
        public void GetDefaultKv_MatchesConstants()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.GetDefaultKv(ValveType.HKV_D), Is.EqualTo(ValveTurnsCalculator.KV_HKV_D));
            Assert.That(ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_25), Is.EqualTo(ValveTurnsCalculator.KV_IV_1_25));
            Assert.That(ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_5), Is.EqualTo(ValveTurnsCalculator.KV_IV_1_5));
        }

        #endregion

        #region GetValveTypeName Tests

        [Test]
        public void GetValveTypeName_HKV_D_ReturnsCorrectName()
        {
            // Act
            string name = ValveTurnsCalculator.GetValveTypeName(ValveType.HKV_D);

            // Assert
            Assert.That(name, Does.Contain("HKV-D"));
            Assert.That(name, Does.Contain("бытовой"));
        }

        [Test]
        public void GetValveTypeName_IV_1_25_ReturnsCorrectName()
        {
            // Act
            string name = ValveTurnsCalculator.GetValveTypeName(ValveType.IV_1_25);

            // Assert
            Assert.That(name, Does.Contain("IV 1¼"));
            Assert.That(name, Does.Contain("промышленный"));
        }

        [Test]
        public void GetValveTypeName_IV_1_5_ReturnsCorrectName()
        {
            // Act
            string name = ValveTurnsCalculator.GetValveTypeName(ValveType.IV_1_5);

            // Assert
            Assert.That(name, Does.Contain("IV 1½"));
            Assert.That(name, Does.Contain("промышленный"));
        }

        [Test]
        public void GetValveTypeName_InvalidValveType_ReturnsUnknown()
        {
            // Arrange
            var invalidType = (ValveType)999;

            // Act
            string name = ValveTurnsCalculator.GetValveTypeName(invalidType);

            // Assert
            Assert.That(name, Is.EqualTo("Неизвестный тип"));
        }

        #endregion

        #region IsValidKv Tests

        [Test]
        public void IsValidKv_HKV_D_ValidRange_ReturnsTrue()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.8, ValveType.HKV_D), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(2.0, ValveType.HKV_D), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(4.0, ValveType.HKV_D), Is.True);
        }

        [Test]
        public void IsValidKv_HKV_D_InvalidRange_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.7, ValveType.HKV_D), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(4.1, ValveType.HKV_D), Is.False);
        }

        [Test]
        public void IsValidKv_IV_1_25_ValidRange_ReturnsTrue()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.5, ValveType.IV_1_25), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(1.5, ValveType.IV_1_25), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(3.0, ValveType.IV_1_25), Is.True);
        }

        [Test]
        public void IsValidKv_IV_1_25_InvalidRange_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.4, ValveType.IV_1_25), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(3.1, ValveType.IV_1_25), Is.False);
        }

        [Test]
        public void IsValidKv_IV_1_5_ValidRange_ReturnsTrue()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.5, ValveType.IV_1_5), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(2.0, ValveType.IV_1_5), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(3.5, ValveType.IV_1_5), Is.True);
        }

        [Test]
        public void IsValidKv_IV_1_5_InvalidRange_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.4, ValveType.IV_1_5), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(3.6, ValveType.IV_1_5), Is.False);
        }

        [Test]
        public void IsValidKv_InvalidValveType_ReturnsFalse()
        {
            // Arrange
            var invalidType = (ValveType)999;

            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(1.0, invalidType), Is.False);
        }

        [Test]
        public void IsValidKv_NegativeKv_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(-1.0, ValveType.HKV_D), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(-1.0, ValveType.IV_1_25), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(-1.0, ValveType.IV_1_5), Is.False);
        }

        [Test]
        public void IsValidKv_ZeroKv_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.0, ValveType.HKV_D), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(0.0, ValveType.IV_1_25), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(0.0, ValveType.IV_1_5), Is.False);
        }

        #endregion

        #region Constants Tests

        [Test]
        public void Constants_HaveCorrectValues()
        {
            // Assert
            Assert.That(ValveTurnsCalculator.KV_HKV_D, Is.EqualTo(1.2));
            Assert.That(ValveTurnsCalculator.KV_IV_1_25, Is.EqualTo(1.45));
            Assert.That(ValveTurnsCalculator.KV_IV_1_5, Is.EqualTo(1.5));
        }

        [Test]
        public void MaxTurns_IsEight()
        {
            // Assert
            Assert.That(ValveTurnsCalculator.MaxTurns, Is.EqualTo(8.0));
        }

        #endregion

        #region CalculateTurnsWithWarning Tests

        [Test]
        public void CalculateTurnsWithWarning_NormalValue_ReturnsNoWarning()
        {
            // Arrange
            double kv = 1.5;

            // Act
            var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.IV_1_5);

            // Assert
            Assert.That(turns, Is.EqualTo(7.5).Within(0.01));
            Assert.That(warning, Is.Null);
        }

        [Test]
        public void CalculateTurnsWithWarning_ExceedsMaxTurns_ReturnsWarning()
        {
            // Arrange
            double kv = 2.0; // Даст ~10 оборотов, что превышает максимум

            // Act
            var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.IV_1_5);

            // Assert
            Assert.That(turns, Is.EqualTo(8.0));
            Assert.That(warning, Is.Not.Null);
            Assert.That(warning, Does.Contain("превышают"));
            Assert.That(warning, Does.Contain("8"));
        }

        [Test]
        public void CalculateTurnsWithWarning_HKV_D_ExceedsMaxTurns_ReturnsWarning()
        {
            // Arrange
            double kv = 2.0; // Даст ~15 оборотов, что превышает максимум

            // Act
            var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.HKV_D);

            // Assert
            Assert.That(turns, Is.EqualTo(8.0));
            Assert.That(warning, Is.Not.Null);
        }

        [Test]
        public void CalculateTurnsWithWarning_InvalidValveType_ThrowsException()
        {
            // Arrange
            double kv = 1.0;
            var invalidType = (ValveType)999;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                ValveTurnsCalculator.CalculateTurnsWithWarning(kv, invalidType));
        }

        [Test]
        public void CalculateTurnsWithWarning_RoundsToQuarter()
        {
            // Arrange
            double kv = 1.45;

            // Act
            var (turns, _) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.IV_1_25);

            // Assert - проверяем, что результат округлён до 0.25
            double remainder = turns * 4 % 1;
            Assert.That(remainder, Is.EqualTo(0).Within(0.001), "Результат должен быть округлён до 0.25");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CalculateTurns_WithDefaultKv_ReturnsValidTurns()
        {
            // Arrange - используем Kv по умолчанию для каждого типа клапана
            double kvHKV_D = ValveTurnsCalculator.GetDefaultKv(ValveType.HKV_D);
            double kvIV_1_25 = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_25);
            double kvIV_1_5 = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_5);

            // Act
            double turnsHKV_D = ValveTurnsCalculator.CalculateTurns(kvHKV_D, ValveType.HKV_D);
            double turnsIV_1_25 = ValveTurnsCalculator.CalculateTurns(kvIV_1_25, ValveType.IV_1_25);
            double turnsIV_1_5 = ValveTurnsCalculator.CalculateTurns(kvIV_1_5, ValveType.IV_1_5);

            // Assert - все обороты должны быть положительными
            Assert.That(turnsHKV_D, Is.GreaterThan(0));
            Assert.That(turnsIV_1_25, Is.GreaterThan(0));
            Assert.That(turnsIV_1_5, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateTurns_DefaultKv_IsValidForAllTypes()
        {
            // Arrange
            double kvHKV_D = ValveTurnsCalculator.GetDefaultKv(ValveType.HKV_D);
            double kvIV_1_25 = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_25);
            double kvIV_1_5 = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_5);

            // Act & Assert - Kv по умолчанию должны быть валидными
            Assert.That(ValveTurnsCalculator.IsValidKv(kvHKV_D, ValveType.HKV_D), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(kvIV_1_25, ValveType.IV_1_25), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(kvIV_1_5, ValveType.IV_1_5), Is.True);
        }

        [Test]
        public void CalculateTurns_BoundaryValues_HKV_D()
        {
            // Arrange - граничные значения Kv для HKV-D
            double kvMin = 0.8;
            double kvMax = 4.0;

            // Act
            double turnsMin = ValveTurnsCalculator.CalculateTurns(kvMin, ValveType.HKV_D);
            double turnsMax = ValveTurnsCalculator.CalculateTurns(kvMax, ValveType.HKV_D);

            // Assert - обороты должны быть валидными (положительными)
            Assert.That(turnsMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(turnsMax, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CalculateTurns_BoundaryValues_IV_1_25()
        {
            // Arrange - граничные значения Kv для IV 1¼"
            double kvMin = 0.5;
            double kvMax = 3.0;

            // Act
            double turnsMin = ValveTurnsCalculator.CalculateTurns(kvMin, ValveType.IV_1_25);
            double turnsMax = ValveTurnsCalculator.CalculateTurns(kvMax, ValveType.IV_1_25);

            // Assert - обороты должны быть валидными
            Assert.That(turnsMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(turnsMax, Is.GreaterThan(turnsMin));
        }

        [Test]
        public void CalculateTurns_BoundaryValues_IV_1_5()
        {
            // Arrange - граничные значения Kv для IV 1½"
            double kvMin = 0.5;
            double kvMax = 3.5;

            // Act
            double turnsMin = ValveTurnsCalculator.CalculateTurns(kvMin, ValveType.IV_1_5);
            double turnsMax = ValveTurnsCalculator.CalculateTurns(kvMax, ValveType.IV_1_5);

            // Assert - обороты должны быть валидными
            Assert.That(turnsMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(turnsMax, Is.GreaterThan(turnsMin));
        }

        #endregion
    }
}