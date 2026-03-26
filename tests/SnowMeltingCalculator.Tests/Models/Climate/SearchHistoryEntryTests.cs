using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Tests.Models.Climate
{
    /// <summary>
    /// Тесты для модели SearchHistoryEntry
    /// </summary>
    [TestFixture]
    public class SearchHistoryEntryTests
    {
        #region Default Values Tests

        [Test]
        public void SearchHistoryEntry_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var entry = new SearchHistoryEntry();

            // Assert
            Assert.That(entry.Id, Is.EqualTo(0));
            Assert.That(entry.CityId, Is.EqualTo(string.Empty));
            Assert.That(entry.UseCount, Is.EqualTo(0));
            Assert.That(entry.City, Is.Null);
        }

        [Test]
        public void SearchHistoryEntry_LastUsed_DefaultIsMinValue()
        {
            // Arrange & Act
            var entry = new SearchHistoryEntry();

            // Assert
            Assert.That(entry.LastUsed, Is.EqualTo(DateTime.MinValue));
        }

        #endregion

        #region Property Assignment Tests

        [Test]
        public void SearchHistoryEntry_CanSetCityId()
        {
            // Arrange
            var entry = new SearchHistoryEntry();

            // Act
            entry.CityId = "Москва";

            // Assert
            Assert.That(entry.CityId, Is.EqualTo("Москва"));
        }

        [Test]
        public void SearchHistoryEntry_CanSetLastUsed()
        {
            // Arrange
            var entry = new SearchHistoryEntry();
            var testDate = new DateTime(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

            // Act
            entry.LastUsed = testDate;

            // Assert
            Assert.That(entry.LastUsed, Is.EqualTo(testDate));
        }

        [Test]
        public void SearchHistoryEntry_CanSetUseCount()
        {
            // Arrange
            var entry = new SearchHistoryEntry();

            // Act
            entry.UseCount = 5;

            // Assert
            Assert.That(entry.UseCount, Is.EqualTo(5));
        }

        [Test]
        public void SearchHistoryEntry_CanSetId()
        {
            // Arrange
            var entry = new SearchHistoryEntry();

            // Act
            entry.Id = 42;

            // Assert
            Assert.That(entry.Id, Is.EqualTo(42));
        }

        [Test]
        public void SearchHistoryEntry_CanSetCityNavigation()
        {
            // Arrange
            var entry = new SearchHistoryEntry();
            var city = new CityInfo { Name = "Москва", Region = "Московская область" };

            // Act
            entry.City = city;

            // Assert
            Assert.That(entry.City, Is.SameAs(city));
            Assert.That(entry.City.Name, Is.EqualTo("Москва"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void SearchHistoryEntry_CanCreateCompleteEntry()
        {
            // Arrange
            var city = new CityInfo
            {
                Name = "Москва",
                Region = "Московская область",
                T5Days092 = -28
            };

            // Act
            var entry = new SearchHistoryEntry
            {
                Id = 1,
                CityId = "Москва",
                LastUsed = DateTime.UtcNow,
                UseCount = 10,
                City = city
            };

            // Assert
            Assert.That(entry.Id, Is.EqualTo(1));
            Assert.That(entry.CityId, Is.EqualTo("Москва"));
            Assert.That(entry.UseCount, Is.EqualTo(10));
            Assert.That(entry.City, Is.Not.Null);
            Assert.That(entry.City.Name, Is.EqualTo("Москва"));
        }

        [Test]
        public void SearchHistoryEntry_CityId_IsCompatibleWithCityInfoName()
        {
            // Arrange
            var city = new CityInfo { Name = "Санкт-Петербург", Region = "Ленинградская область" };
            var entry = new SearchHistoryEntry();

            // Act
            entry.CityId = city.Name;

            // Assert
            Assert.That(entry.CityId, Is.EqualTo(city.Name));
        }

        #endregion
    }
}