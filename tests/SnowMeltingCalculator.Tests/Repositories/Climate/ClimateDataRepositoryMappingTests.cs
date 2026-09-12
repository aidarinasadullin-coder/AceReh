using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Repositories;

namespace SnowMeltingCalculator.Tests.Repositories.Climate
{
    /// <summary>
    /// Маппинг climate_db.json → CityInfo: источник v_H переключён на
    /// wind_max_jan — «максимальная из средних скоростей по румбам за январь»
    /// (план 2026-09-12, часть A). Null-fallback: город без данных → 0.
    /// </summary>
    [TestFixture]
    public class ClimateDataRepositoryMappingTests
    {
        private static string WriteTempClimateDb(string json)
        {
            var path = Path.Combine(Path.GetTempPath(), $"climate_db_test_{Guid.NewGuid():N}.json");
            File.WriteAllText(path, json);
            return path;
        }

        [Test]
        public async Task MapToCityInfo_PropsWindMaxJan_FromJsonField()
        {
            // Arrange: wind_max_jan прокидывается в WindMaxJan (решение A)
            const string json = """
                {
                  "meta": {},
                  "cities": [
                    {
                      "city": "Майкоп",
                      "region": "Республика Адыгея",
                      "t_5days_092": -15,
                      "wind_avg_t_le_8": 3.1,
                      "wind_max_jan": 5.4,
                      "humidity_15h_cold": 68,
                      "period_0_days": 20
                    }
                  ]
                }
                """;
            var path = WriteTempClimateDb(json);
            try
            {
                var repository = new ClimateDataRepository(path);

                // Act
                var cities = (await repository.LoadCitiesAsync()).ToList();

                // Assert
                Assert.That(cities, Has.Count.EqualTo(1));
                Assert.That(cities[0].Name, Is.EqualTo("Майкоп"));
                Assert.That(cities[0].WindMaxJan, Is.EqualTo(5.4));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task MapToCityInfo_MissingWindMaxJan_FallsBackToZero()
        {
            // Мариуполь/Запорожье/Аскания-Нова: null → 0, валидация климата подсветит (решение A1)
            const string json = """
                {
                  "meta": {},
                  "cities": [
                    {
                      "city": "Мариуполь",
                      "region": "Донецкая область",
                      "t_5days_092": -16,
                      "wind_avg_t_le_8": 5.6,
                      "wind_max_jan": null,
                      "humidity_15h_cold": 80,
                      "period_0_days": 40
                    }
                  ]
                }
                """;
            var path = WriteTempClimateDb(json);
            try
            {
                var repository = new ClimateDataRepository(path);

                // Act
                var cities = (await repository.LoadCitiesAsync()).ToList();

                // Assert
                Assert.That(cities, Has.Count.EqualTo(1));
                Assert.That(cities[0].WindMaxJan, Is.EqualTo(0.0));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void RealClimateDb_MaykopWindMaxJan_Is54_AndNullOnlyInThreeKnownCities()
        {
            // Characterized на реальной базе (план 2026-09-12, кейс 3 части A):
            // Майкоп → 5.4; wind_max_jan отсутствует ровно у трёх известных городов.
            var dbPath = FindRepoClimateDb();
            Assert.That(dbPath, Is.Not.Null, "data/climate_db.json не найден от тестового каталога вверх по дереву");

            var repository = new ClimateDataRepository(dbPath!);
            var cities = repository.LoadCitiesAsync().Result;

            var maykop = cities.SingleOrDefault(c => string.Equals(c.Name, "Майкоп", StringComparison.OrdinalIgnoreCase));
            Assert.That(maykop, Is.Not.Null);
            Assert.That(maykop!.WindMaxJan, Is.EqualTo(5.4));

            var withoutWind = cities.Where(c => c.WindMaxJan == 0.0).Select(c => c.Name).ToList();
            Assert.That(withoutWind, Is.EquivalentTo(new[] { "Мариуполь", "Запорожье", "Аскания-Нова" }));
        }

        private static string? FindRepoClimateDb()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "data", "climate_db.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
