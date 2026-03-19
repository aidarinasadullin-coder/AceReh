using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.Climate
{
    /// <summary>
    /// Тесты для ClimateDataService
    /// </summary>
    [TestFixture]
    public class ClimateDataServiceTests
    {
        private ClimateDataService _service = null!;

        [SetUp]
        public void Setup()
        {
            // Создаем мок-репозиторий с тестовыми данными
            var mockRepository = new MockClimateDataRepository();
            _service = new ClimateDataService(mockRepository);
        }

        #region SearchCitiesAsync Tests

        [Test]
        public async Task SearchCitiesAsync_ValidQuery_ReturnsFilteredCities()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var results = await _service.SearchCitiesAsync("Моск");

            // Assert
            Assert.That(results.Count(), Is.GreaterThan(0));
            Assert.That(results.All(c => c.Name.Contains("Моск", StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        [Test]
        public async Task SearchCitiesAsync_EmptyQuery_ReturnsEmpty()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var results = await _service.SearchCitiesAsync("");

            // Assert
            Assert.That(results.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task SearchCitiesAsync_ShortQuery_ReturnsEmpty()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var results = await _service.SearchCitiesAsync("М");

            // Assert
            Assert.That(results.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task SearchCitiesAsync_ReturnsMax20Results()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var results = await _service.SearchCitiesAsync("а");

            // Assert
            Assert.That(results.Count(), Is.LessThanOrEqualTo(20));
        }

        #endregion

        #region DetermineZone Tests

        [Test]
        public void DetermineZone_AboveMinus27_ReturnsZoneM10()
        {
            // Arrange
            var temperature = -20.0;

            // Act
            var zone = _service.DetermineZone(temperature);

            // Assert
            Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M10));
        }

        [Test]
        public void DetermineZone_ExactlyMinus27_ReturnsZoneM10()
        {
            // Arrange
            var temperature = -27.0;

            // Act
            var zone = _service.DetermineZone(temperature);

            // Assert
            Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M10));
        }

        [Test]
        public void DetermineZone_BetweenMinus27AndMinus37_ReturnsZoneM15()
        {
            // Arrange
            var temperature = -30.0;

            // Act
            var zone = _service.DetermineZone(temperature);

            // Assert
            Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M15));
        }

        [Test]
        public void DetermineZone_ExactlyMinus37_ReturnsZoneM20()
        {
            // Arrange
            var temperature = -37.0;

            // Act
            var zone = _service.DetermineZone(temperature);

            // Assert
            Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M20));
        }

        [Test]
        public void DetermineZone_BelowMinus37_ReturnsZoneM20()
        {
            // Arrange
            var temperature = -40.0;

            // Act
            var zone = _service.DetermineZone(temperature);

            // Assert
            Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M20));
        }

        [Test]
        public void DetermineZone_HighRequirements_ReturnsZoneM20Plus()
        {
            // Arrange
            var temperature = -20.0;

            // Act
            var zone = _service.DetermineZone(temperature, isHighRequirements: true);

            // Assert
            Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M20_Plus));
        }

        [Test]
        public void DetermineZone_HighRequirements_AlwaysReturnsZoneM20Plus()
        {
            // Arrange
            var temperatures = new[] { -10.0, -20.0, -30.0, -40.0 };

            // Act & Assert
            foreach (var temp in temperatures)
            {
                var zone = _service.DetermineZone(temp, isHighRequirements: true);
                Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M20_Plus), 
                    $"Temperature {temp} with high requirements should return Zone_M20_Plus");
            }
        }

        #endregion

        #region GetCityByName Tests

        [Test]
        public async Task GetCityByName_ExistingCity_ReturnsCity()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var city = _service.GetCityByName("Москва");

            // Assert
            Assert.That(city, Is.Not.Null);
            Assert.That(city!.Name, Is.EqualTo("Москва"));
        }

        [Test]
        public async Task GetCityByName_NonExistingCity_ReturnsNull()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var city = _service.GetCityByName("НесуществующийГород");

            // Assert
            Assert.That(city, Is.Null);
        }

        [Test]
        public async Task GetCityByName_CaseInsensitive_ReturnsCity()
        {
            // Arrange
            await _service.LoadClimateDataAsync();

            // Act
            var city = _service.GetCityByName("МОСКВА");

            // Assert
            Assert.That(city, Is.Not.Null);
        }

        #endregion

        #region LoadClimateDataAsync Tests

        [Test]
        public async Task LoadClimateDataAsync_LoadsDataSuccessfully()
        {
            // Act
            await _service.LoadClimateDataAsync();

            // Assert
            Assert.That(_service.IsLoaded, Is.True);
            Assert.That(_service.CitiesCount, Is.GreaterThan(0));
        }

        [Test]
        public async Task LoadClimateDataAsync_CalledTwice_LoadsOnce()
        {
            // Act
            await _service.LoadClimateDataAsync();
            var count1 = _service.CitiesCount;
            
            await _service.LoadClimateDataAsync();
            var count2 = _service.CitiesCount;

            // Assert
            Assert.That(count1, Is.EqualTo(count2));
        }

        #endregion
    }

    /// <summary>
    /// Мок-репозиторий для тестов
    /// </summary>
    internal class MockClimateDataRepository : IClimateDataRepository
    {
        public Task<IEnumerable<CityInfo>> LoadCitiesAsync()
        {
            var cities = new List<CityInfo>
            {
                new CityInfo { Name = "Москва", Region = "Московская область", T5Days092 = -28, WindAvgTempLe8 = 4.5, Humidity15hCold = 85 },
                new CityInfo { Name = "Санкт-Петербург", Region = "Ленинградская область", T5Days092 = -26, WindAvgTempLe8 = 5.0, Humidity15hCold = 88 },
                new CityInfo { Name = "Сочи", Region = "Краснодарский край", T5Days092 = -5, WindAvgTempLe8 = 6.0, Humidity15hCold = 70 },
                new CityInfo { Name = "Майкоп", Region = "Республика Адыгея", T5Days092 = -15, WindAvgTempLe8 = 5.4, Humidity15hCold = 68 },
                new CityInfo { Name = "Норильск", Region = "Красноярский край", T5Days092 = -42, WindAvgTempLe8 = 7.0, Humidity15hCold = 75 },
                new CityInfo { Name = "Якутск", Region = "Республика Саха (Якутия)", T5Days092 = -48, WindAvgTempLe8 = 2.0, Humidity15hCold = 80 },
                new CityInfo { Name = "Владивосток", Region = "Приморский край", T5Days092 = -24, WindAvgTempLe8 = 8.0, Humidity15hCold = 65 },
                new CityInfo { Name = "Мурманск", Region = "Мурманская область", T5Days092 = -32, WindAvgTempLe8 = 6.5, Humidity15hCold = 82 },
                new CityInfo { Name = "Краснодар", Region = "Краснодарский край", T5Days092 = -19, WindAvgTempLe8 = 5.0, Humidity15hCold = 72 },
                new CityInfo { Name = "Екатеринбург", Region = "Свердловская область", T5Days092 = -32, WindAvgTempLe8 = 5.5, Humidity15hCold = 78 }
            };

            return Task.FromResult<IEnumerable<CityInfo>>(cities);
        }

        public Task<CityInfo?> GetCityByNameAsync(string name)
        {
            var cities = LoadCitiesAsync().Result;
            return Task.FromResult(cities.FirstOrDefault(c => 
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        public IEnumerable<CityInfo> GetAllCities()
        {
            return LoadCitiesAsync().Result;
        }
    }
}