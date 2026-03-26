using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Tests.Models.Climate
{
    /// <summary>
    /// Тесты для модели CityMatchResult
    /// </summary>
    [TestFixture]
    public class CityMatchResultTests
    {
        #region TemperatureDisplay Tests

        [Test]
        public void TemperatureDisplay_ForNegativeTemperature_ReturnsCorrectFormat()
        {
            // Arrange
            var result = new CityMatchResult
            {
                City = new CityInfo { Name = "Москва", T5Days092 = -28 },
                MatchType = MatchType.StartsWith
            };

            // Act
            var display = result.TemperatureDisplay;

            // Assert
            Assert.That(display, Is.EqualTo("t = -28°C"));
        }

        [Test]
        public void TemperatureDisplay_ForTemperatureWithDecimals_RoundsCorrectly()
        {
            // Arrange
            var result = new CityMatchResult
            {
                City = new CityInfo { Name = "Тест", T5Days092 = -15.5 },
                MatchType = MatchType.Contains
            };

            // Act
            var display = result.TemperatureDisplay;

            // Assert
            Assert.That(display, Is.EqualTo("t = -16°C"));
        }

        [Test]
        public void TemperatureDisplay_ForPositiveTemperature_ReturnsCorrectFormat()
        {
            // Arrange
            var result = new CityMatchResult
            {
                City = new CityInfo { Name = "Сочи", T5Days092 = 5 },
                MatchType = MatchType.Region
            };

            // Act
            var display = result.TemperatureDisplay;

            // Assert
            Assert.That(display, Is.EqualTo("t = 5°C"));
        }

        [Test]
        public void TemperatureDisplay_ForZeroTemperature_ReturnsCorrectFormat()
        {
            // Arrange
            var result = new CityMatchResult
            {
                City = new CityInfo { Name = "Тест", T5Days092 = 0 },
                MatchType = MatchType.StartsWith
            };

            // Act
            var display = result.TemperatureDisplay;

            // Assert
            Assert.That(display, Is.EqualTo("t = 0°C"));
        }

        #endregion

        #region Property Tests

        [Test]
        public void CityMatchResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new CityMatchResult();

            // Assert
            Assert.That(result.HighlightedName, Is.EqualTo(string.Empty));
            Assert.That(result.HighlightedRegion, Is.EqualTo(string.Empty));
            Assert.That(result.ZoneDisplay, Is.EqualTo(string.Empty));
            Assert.That(result.MatchIndex, Is.EqualTo(0));
            Assert.That(result.MatchLength, Is.EqualTo(0));
        }

        [Test]
        public void CityMatchResult_CanSetAllProperties()
        {
            // Arrange
            var city = new CityInfo { Name = "Москва", Region = "Московская область", T5Days092 = -28 };

            // Act
            var result = new CityMatchResult
            {
                City = city,
                HighlightedName = "**Мос**ква",
                HighlightedRegion = "Московская **область**",
                MatchType = MatchType.StartsWith,
                ZoneDisplay = "Зона M15",
                MatchIndex = 0,
                MatchLength = 3
            };

            // Assert
            Assert.That(result.City, Is.SameAs(city));
            Assert.That(result.HighlightedName, Is.EqualTo("**Мос**ква"));
            Assert.That(result.HighlightedRegion, Is.EqualTo("Московская **область**"));
            Assert.That(result.MatchType, Is.EqualTo(MatchType.StartsWith));
            Assert.That(result.ZoneDisplay, Is.EqualTo("Зона M15"));
            Assert.That(result.MatchIndex, Is.EqualTo(0));
            Assert.That(result.MatchLength, Is.EqualTo(3));
        }

        #endregion

        #region MatchType Tests

        [Test]
        public void MatchType_StartsWith_HasValueZero()
        {
            // Assert
            Assert.That((int)MatchType.StartsWith, Is.EqualTo(0));
        }

        [Test]
        public void MatchType_Contains_HasValueOne()
        {
            // Assert
            Assert.That((int)MatchType.Contains, Is.EqualTo(1));
        }

        [Test]
        public void MatchType_Region_HasValueTwo()
        {
            // Assert
            Assert.That((int)MatchType.Region, Is.EqualTo(2));
        }

        #endregion
    }
}