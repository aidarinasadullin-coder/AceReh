using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.Services.Climate;
using System;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.Climate
{
    /// <summary>
    /// Тесты для ClimateViewModel
    /// </summary>
    [TestFixture]
    public class ClimateViewModelTests
    {
        private ClimateViewModel _viewModel = null!;
        private MockClimateDataService _mockService = null!;
        private ClimateData _climateData = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new MockClimateDataService();
            _climateData = new ClimateData();
            _viewModel = new ClimateViewModel(_mockService, _climateData);
        }

        #region SelectCity Tests

        [Test]
        public async Task SelectCity_AutoFillsParameters()
        {
            // Arrange
            await _viewModel.LoadDataAsync();
            var city = new CityInfo
            {
                Name = "Москва",
                Region = "Московская область",
                T5Days092 = -28,
                WindMaxJan = 4.5,
                Humidity15hCold = 85
            };

            // Act
            _viewModel.SelectedCity = city;

            // Assert
            Assert.That(_viewModel.AirTemperature, Is.EqualTo(-28));
            Assert.That(_viewModel.WindSpeed, Is.EqualTo(4.5));
            Assert.That(_viewModel.Humidity, Is.EqualTo(85));
        }

        [Test]
        public async Task SelectCity_DeterminesCorrectZone()
        {
            // Arrange
            await _viewModel.LoadDataAsync();

            // Act - Zone_M10 (t >= -27)
            _viewModel.SelectedCity = new CityInfo { Name = "Сочи", T5Days092 = -5 };
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M10));

            // Act - Zone_M15 (-37 < t < -27)
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28 };
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));

            // Act - Zone_M20 (t <= -37)
            _viewModel.SelectedCity = new CityInfo { Name = "Норильск", T5Days092 = -42 };
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M20));
        }

        #endregion

        #region HighRequirements Tests

        [Test]
        public void SetHighRequirements_ChangesZone()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28 };
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));

            // Act
            _viewModel.IsHighRequirements = true;

            // Assert
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M20_Plus));
        }

        [Test]
        public void UnsetHighRequirements_RestoresZone()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28 };
            _viewModel.IsHighRequirements = true;

            // Act
            _viewModel.IsHighRequirements = false;

            // Assert
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void Validate_InvalidTemperature_ReturnsFalse()
        {
            // Arrange
            _viewModel.AirTemperature = -60; // Ниже минимума

            // Act & Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура"));
        }

        [Test]
        public void Validate_InvalidWindSpeed_ReturnsFalse()
        {
            // Arrange
            _viewModel.WindSpeed = 50; // Выше максимума

            // Act & Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("ветра"));
        }

        [Test]
        public void Validate_InvalidHumidity_ReturnsFalse()
        {
            // Arrange
            _viewModel.Humidity = 150; // Выше максимума

            // Act & Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Влажность"));
        }

        [Test]
        public void Validate_InvalidSnowfallIntensity_ReturnsFalse()
        {
            // Arrange
            _viewModel.SnowfallIntensity = 10; // Выше максимума

            // Act & Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Интенсивность"));
        }

        [Test]
        public void Validate_ZeroSnowfallIntensity_ReturnsTrue()
        {
            // Arrange - граничное значение (отсутствие снегопада)
            _viewModel.SnowfallIntensity = 0;

            // Act & Assert
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(_viewModel.ValidationMessage, Is.Empty);
        }

        [Test]
        public void Validate_ValidData_ReturnsTrue()
        {
            // Arrange
            _viewModel.AirTemperature = -15;
            _viewModel.WindSpeed = 5;
            _viewModel.Humidity = 70;
            _viewModel.SnowfallIntensity = 0.3;

            // Act & Assert
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(_viewModel.ValidationMessage, Is.Empty);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void ResetToDefaults_ClearsAllFields()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28 };
            _viewModel.AirTemperature = -28;
            _viewModel.WindSpeed = 4.5;
            _viewModel.Humidity = 85;

            // Act
            _viewModel.ResetToDefaultsCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.SelectedCity, Is.Null);
            Assert.That(_viewModel.AirTemperature, Is.EqualTo(-15.0));
            Assert.That(_viewModel.WindSpeed, Is.EqualTo(5.0));
            Assert.That(_viewModel.Humidity, Is.EqualTo(70.0));
            Assert.That(_viewModel.SnowfallIntensity, Is.EqualTo(0.3));
            Assert.That(_viewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));
        }

        [Test]
        public void ResetToCityData_RestoresCityValues()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28, WindMaxJan = 4.5, Humidity15hCold = 85 };
            _viewModel.AirTemperature = -20; // Изменено пользователем
            _viewModel.WindSpeed = 10;
            _viewModel.Humidity = 90;

            // Act
            _viewModel.ResetToCityDataCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.AirTemperature, Is.EqualTo(-28));
            Assert.That(_viewModel.WindSpeed, Is.EqualTo(4.5));
            Assert.That(_viewModel.Humidity, Is.EqualTo(85));
        }

        #endregion

        #region GetClimateData Tests

        [Test]
        public void GetClimateData_ReturnsCorrectData()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", Region = "Московская область", T5Days092 = -28 };
            _viewModel.AirTemperature = -28;
            _viewModel.WindSpeed = 4.5;
            _viewModel.Humidity = 85;
            _viewModel.SnowfallIntensity = 0.5;

            // Act
            var data = _viewModel.GetClimateData();

            // Assert
            Assert.That(data.SelectedCity, Is.EqualTo("Москва"));
            Assert.That(data.SelectedRegion, Is.EqualTo("Московская область"));
            Assert.That(data.AirTemperature, Is.EqualTo(-28));
            Assert.That(data.WindSpeed, Is.EqualTo(4.5));
            Assert.That(data.Humidity, Is.EqualTo(85));
            Assert.That(data.SnowfallIntensity, Is.EqualTo(0.5));
        }

        #endregion

        #region SyncToClimateData Tests

        [Test]
        public void SelectCity_SyncsToClimateData()
        {
            // Arrange
            var city = new CityInfo
            {
                Name = "Москва",
                Region = "Московская область",
                T5Days092 = -28,
                WindMaxJan = 4.5,
                Humidity15hCold = 85
            };

            // Act
            _viewModel.SelectedCity = city;

            // Assert - singleton IClimateData должен быть обновлён
            Assert.That(_climateData.SelectedCity, Is.EqualTo("Москва"));
            Assert.That(_climateData.SelectedRegion, Is.EqualTo("Московская область"));
            Assert.That(_climateData.AirTemperature, Is.EqualTo(-28));
            Assert.That(_climateData.WindSpeed, Is.EqualTo(4.5));
            Assert.That(_climateData.Humidity, Is.EqualTo(85));
        }

        [Test]
        public void ChangeAirTemperature_SyncsToClimateData()
        {
            // Arrange
            _viewModel.AirTemperature = -20;

            // Act
            _viewModel.AirTemperature = -25;

            // Assert
            Assert.That(_climateData.AirTemperature, Is.EqualTo(-25));
        }

        [Test]
        public void ChangeWindSpeed_SyncsToClimateData()
        {
            // Arrange
            _viewModel.WindSpeed = 5.0;

            // Act
            _viewModel.WindSpeed = 10.0;

            // Assert
            Assert.That(_climateData.WindSpeed, Is.EqualTo(10.0));
        }

        [Test]
        public void ChangeSnowfallIntensity_SyncsToClimateData()
        {
            // Arrange
            _viewModel.SnowfallIntensity = 0.3;

            // Act
            _viewModel.SnowfallIntensity = 0.5;

            // Assert
            Assert.That(_climateData.SnowfallIntensity, Is.EqualTo(0.5));
        }

        [Test]
        public void ResetToDefaults_SyncsToClimateData()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28 };
            _viewModel.AirTemperature = -28;
            _viewModel.WindSpeed = 4.5;

            // Act
            _viewModel.ResetToDefaultsCommand.Execute(null);

            // Assert
            Assert.That(_climateData.SelectedCity, Is.EqualTo(string.Empty));
            Assert.That(_climateData.AirTemperature, Is.EqualTo(-15.0));
            Assert.That(_climateData.WindSpeed, Is.EqualTo(5.0));
        }

        [Test]
        public void SetHighRequirements_SyncsZoneToClimateData()
        {
            // Arrange
            _viewModel.SelectedCity = new CityInfo { Name = "Москва", T5Days092 = -28 };

            // Act
            _viewModel.IsHighRequirements = true;

            // Assert
            Assert.That(_climateData.Zone, Is.EqualTo(ClimateZone.Zone_M20_Plus));
        }

        #endregion
    }

    /// <summary>
    /// Мок-сервис для тестов ViewModel
    /// </summary>
    internal class MockClimateDataService : IClimateDataService
    {
        public bool IsLoaded => true;
        public int CitiesCount => 10;

        public Task LoadClimateDataAsync() => Task.CompletedTask;

        public Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
        {
            var cities = new List<CityInfo>
            {
                new CityInfo { Name = "Москва", Region = "Московская область", T5Days092 = -28 },
                new CityInfo { Name = "Санкт-Петербург", Region = "Ленинградская область", T5Days092 = -26 },
                new CityInfo { Name = "Сочи", Region = "Краснодарский край", T5Days092 = -5 }
            };

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Task.FromResult(Enumerable.Empty<CityInfo>());

            return Task.FromResult(cities.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).AsEnumerable());
        }

        public CityInfo? GetCityByName(string name)
        {
            return new CityInfo { Name = name, Region = "Тестовый регион", T5Days092 = -25 };
        }

        public IEnumerable<CityInfo> GetAllCities()
        {
            return new List<CityInfo>
            {
                new CityInfo { Name = "Москва", Region = "Московская область", T5Days092 = -28 },
                new CityInfo { Name = "Санкт-Петербург", Region = "Ленинградская область", T5Days092 = -26 }
            };
        }

        public ClimateZone DetermineZone(double t5days, bool isHighRequirements = false)
        {
            if (isHighRequirements)
                return ClimateZone.Zone_M20_Plus;

            if (t5days >= -27)
                return ClimateZone.Zone_M10;

            if (t5days > -37)
                return ClimateZone.Zone_M15;

            return ClimateZone.Zone_M20;
        }
    }
}