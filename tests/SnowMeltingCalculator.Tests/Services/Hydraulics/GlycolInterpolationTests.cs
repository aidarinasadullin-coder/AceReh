using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для проверки корректности интерполяции свойств гликолей.
    /// 
    /// Физические законы:
    /// - Вязкость УМЕНЬШАЕТСЯ с ростом температуры (при низкой температуре вязкость выше)
    /// - Плотность УМЕНЬШАЕТСЯ с ростом температуры (при низкой температуре плотность выше)
    /// - Теплоёмкость УВЕЛИЧИВАЕТСЯ с ростом температуры (при низкой температуре теплоёмкость ниже)
    /// 
    /// Эти тесты проверяют, что интерполяция не нарушает физические законы.
    /// </summary>
    [TestFixture]
    public class GlycolInterpolationTests
    {
        private GlycolDataService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new GlycolDataService("data/glycol_data.json");
        }

        #region Кинематическая вязкость

        /// <summary>
        /// Проверка: вязкость при -15°C должна быть ВЫШЕ, чем при +40°C.
        /// Это следует из физики: при понижении температуры вязкость увеличивается.
        /// </summary>
        [Test]
        public void KinematicViscosity_AtMinus15_HigherThanAtPlus40()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;    // Низкая температура
            double tempHigh = 40;    // Высокая температура

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.KinematicViscosity, Is.GreaterThan(propsAtPlus40.KinematicViscosity),
                $"Вязкость при -15°C ({propsAtMinus15.KinematicViscosity:F2} мм²/с) должна быть ВЫШЕ, " +
                $"чем при +40°C ({propsAtPlus40.KinematicViscosity:F2} мм²/с). " +
                $"Физика: вязкость уменьшается с ростом температуры.");
        }

        [Test]
        public void KinematicViscosity_AtMinus15_HigherThanAtPlus40_Propylene()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Propylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Propylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.KinematicViscosity, Is.GreaterThan(propsAtPlus40.KinematicViscosity),
                $"Вязкость пропиленгликоля при -15°C ({propsAtMinus15.KinematicViscosity:F2} мм²/с) должна быть ВЫШЕ, " +
                $"чем при +40°C ({propsAtPlus40.KinematicViscosity:F2} мм²/с).");
        }

        [Test]
        public void KinematicViscosity_DecreasesWithTemperature_Ethylene()
        {
            // Arrange - проверяем монотонное уменьшение вязкости с ростом температуры
            double concentration = 50;
            double[] temperatures = { -15, -5, 5, 15, 25, 35, 45, 55, 65, 75, 85 };

            // Act
            var viscosities = temperatures.Select(t =>
                _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, t)).ToArray();

            // Assert - каждое последующее значение должно быть меньше предыдущего
            for (int i = 1; i < temperatures.Length; i++)
            {
                Assert.That(viscosities[i], Is.LessThan(viscosities[i - 1]),
                    $"Вязкость при {temperatures[i]}°C ({viscosities[i]:F2} мм²/с) должна быть МЕНЬШЕ, " +
                    $"чем при {temperatures[i - 1]}°C ({viscosities[i - 1]:F2} мм²/с). " +
                    $"Физика: вязкость уменьшается с ростом температуры.");
            }
        }

        [Test]
        public void KinematicViscosity_DecreasesWithTemperature_Propylene()
        {
            // Arrange
            double concentration = 50;
            double[] temperatures = { -15, -5, 5, 15, 25, 35, 45, 55, 65, 75, 85 };

            // Act
            var viscosities = temperatures.Select(t =>
                _service.GetKinematicViscosity(GlycolType.Propylene, concentration, t)).ToArray();

            // Assert
            for (int i = 1; i < temperatures.Length; i++)
            {
                Assert.That(viscosities[i], Is.LessThan(viscosities[i - 1]),
                    $"Вязкость пропиленгликоля при {temperatures[i]}°C ({viscosities[i]:F2} мм²/с) должна быть МЕНЬШЕ, " +
                    $"чем при {temperatures[i - 1]}°C ({viscosities[i - 1]:F2} мм²/с).");
            }
        }

        [Test]
        public void KinematicViscosity_VariousConcentrations_LowerTempHigherViscosity()
        {
            // Arrange - проверяем для разных концентраций
            double[] concentrations = { 20, 30, 40, 50, 60, 70, 80 };
            double tempLow = -15;
            double tempHigh = 40;

            foreach (var concentration in concentrations)
            {
                // Act
                var propsLow = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
                var propsHigh = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

                // Assert
                Assert.That(propsLow.KinematicViscosity, Is.GreaterThan(propsHigh.KinematicViscosity),
                    $"При концентрации {concentration}%: вязкость при -15°C ({propsLow.KinematicViscosity:F2} мм²/с) " +
                    $"должна быть ВЫШЕ, чем при +40°C ({propsHigh.KinematicViscosity:F2} мм²/с).");
            }
        }

        /// <summary>
        /// Тест для конкретного случая, сообщённого пользователем.
        /// Пользователь видит в UI, что кинематическая вязкость при -15°C отображается ниже, чем при +40°C.
        /// Это противоречит физике: вязкость должна УМЕНЬШАТЬСЯ с ростом температуры.
        /// </summary>
        [Test]
        public void KinematicViscosity_UserReportedCase_Concentration50_TemperaturesMinus15AndPlus40()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act - концентрация 50%, температуры -15°C и +40°C
            var propsAtMinus15 = service.GetProperties(GlycolType.Ethylene, 50.0, -15.0);
            var propsAtPlus40 = service.GetProperties(GlycolType.Ethylene, 50.0, 40.0);

            // Assert
            Console.WriteLine($"При -15°C: вязкость = {propsAtMinus15.KinematicViscosity:F2} мм²/с, плотность = {propsAtMinus15.Density:F1} кг/м³");
            Console.WriteLine($"При +40°C: вязкость = {propsAtPlus40.KinematicViscosity:F2} мм²/с, плотность = {propsAtPlus40.Density:F1} кг/м³");

            Assert.That(propsAtMinus15.KinematicViscosity, Is.GreaterThan(propsAtPlus40.KinematicViscosity),
                $"Вязкость при -15°C ({propsAtMinus15.KinematicViscosity:F2}) должна быть ВЫШЕ, чем при +40°C ({propsAtPlus40.KinematicViscosity:F2})");

            Assert.That(propsAtMinus15.Density, Is.GreaterThan(propsAtPlus40.Density),
                $"Плотность при -15°C ({propsAtMinus15.Density:F1}) должна быть ВЫШЕ, чем при +40°C ({propsAtPlus40.Density:F1})");
        }

        #endregion

        #region Плотность

        /// <summary>
        /// Проверка: плотность при -15°C должна быть ВЫШЕ, чем при +40°C.
        /// Это следует из физики: при понижении температуры плотность увеличивается.
        /// </summary>
        [Test]
        public void Density_AtMinus15_HigherThanAtPlus40()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.Density, Is.GreaterThan(propsAtPlus40.Density),
                $"Плотность при -15°C ({propsAtMinus15.Density:F1} кг/м³) должна быть ВЫШЕ, " +
                $"чем при +40°C ({propsAtPlus40.Density:F1} кг/м³). " +
                $"Физика: плотность уменьшается с ростом температуры.");
        }

        [Test]
        public void Density_AtMinus15_HigherThanAtPlus40_Propylene()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Propylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Propylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.Density, Is.GreaterThan(propsAtPlus40.Density),
                $"Плотность пропиленгликоля при -15°C ({propsAtMinus15.Density:F1} кг/м³) должна быть ВЫШЕ, " +
                $"чем при +40°C ({propsAtPlus40.Density:F1} кг/м³).");
        }

        [Test]
        public void Density_DecreasesWithTemperature_Ethylene()
        {
            // Arrange
            double concentration = 50;
            double[] temperatures = { -15, -5, 5, 15, 25, 35, 45, 55, 65, 75, 85 };

            // Act
            var densities = temperatures.Select(t =>
                _service.GetDensity(GlycolType.Ethylene, concentration, t)).ToArray();

            // Assert - каждое последующее значение должно быть меньше предыдущего
            for (int i = 1; i < temperatures.Length; i++)
            {
                Assert.That(densities[i], Is.LessThan(densities[i - 1]),
                    $"Плотность при {temperatures[i]}°C ({densities[i]:F1} кг/м³) должна быть МЕНЬШЕ, " +
                    $"чем при {temperatures[i - 1]}°C ({densities[i - 1]:F1} кг/м³). " +
                    $"Физика: плотность уменьшается с ростом температуры.");
            }
        }

        #endregion

        #region Удельная теплоёмкость

        /// <summary>
        /// Проверка: теплоёмкость при -15°C должна быть НИЖЕ, чем при +40°C.
        /// Это следует из физики: теплоёмкость гликолей увеличивается с ростом температуры.
        /// </summary>
        [Test]
        public void SpecificHeat_AtMinus15_LowerThanAtPlus40()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.SpecificHeat, Is.LessThan(propsAtPlus40.SpecificHeat),
                $"Теплоёмкость при -15°C ({propsAtMinus15.SpecificHeat:F2} кДж/(кг·К)) должна быть НИЖЕ, " +
                $"чем при +40°C ({propsAtPlus40.SpecificHeat:F2} кДж/(кг·К)). " +
                $"Физика: теплоёмкость гликолей увеличивается с ростом температуры.");
        }

        [Test]
        public void SpecificHeat_AtMinus15_LowerThanAtPlus40_Propylene()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Propylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Propylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.SpecificHeat, Is.LessThan(propsAtPlus40.SpecificHeat),
                $"Теплоёмкость пропиленгликоля при -15°C ({propsAtMinus15.SpecificHeat:F2} кДж/(кг·К)) должна быть НИЖЕ, " +
                $"чем при +40°C ({propsAtPlus40.SpecificHeat:F2} кДж/(кг·К)).");
        }

        [Test]
        public void SpecificHeat_IncreasesWithTemperature_Ethylene()
        {
            // Arrange - используем температуры из fallback данных ASHRAE
            // При положительных температурах теплоёмкость увеличивается с ростом температуры
            // Fallback данные: -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
            double concentration = 50;
            double[] temperatures = { 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };

            // Act
            var specificHeats = temperatures.Select(t =>
                _service.GetSpecificHeat(GlycolType.Ethylene, concentration, t)).ToArray();

            // Assert - каждое последующее значение должно быть больше предыдущего
            for (int i = 1; i < temperatures.Length; i++)
            {
                Assert.That(specificHeats[i], Is.GreaterThan(specificHeats[i - 1]),
                    $"Теплоёмкость при {temperatures[i]}°C ({specificHeats[i]:F2} кДж/(кг·К)) должна быть БОЛЬШЕ, " +
                    $"чем при {temperatures[i - 1]}°C ({specificHeats[i - 1]:F2} кДж/(кг·К)). " +
                    $"Физика: теплоёмкость гликолей увеличивается с ростом температуры.");
            }
        }

        #endregion

        #region Теплопроводность

        /// <summary>
        /// Проверка: теплопроводность при -15°C должна быть НИЖЕ, чем при +40°C.
        /// Это следует из физики: теплопроводность гликолей увеличивается с ростом температуры.
        /// </summary>
        [Test]
        public void ThermalConductivity_AtMinus15_LowerThanAtPlus40()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsAtMinus15 = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
            var propsAtPlus40 = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

            // Assert
            Assert.That(propsAtMinus15.ThermalConductivity, Is.LessThan(propsAtPlus40.ThermalConductivity),
                $"Теплопроводность при -15°C ({propsAtMinus15.ThermalConductivity:F3} Вт/(м·К)) должна быть НИЖЕ, " +
                $"чем при +40°C ({propsAtPlus40.ThermalConductivity:F3} Вт/(м·К)). " +
                $"Физика: теплопроводность гликолей увеличивается с ростом температуры.");
        }

        [Test]
        public void ThermalConductivity_IncreasesWithTemperature_Ethylene()
        {
            // Arrange - используем температуры из fallback данных ASHRAE
            // При положительных температурах теплопроводность увеличивается с ростом температуры
            // Fallback данные: -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
            double concentration = 50;
            double[] temperatures = { 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };

            // Act
            var conductivities = temperatures.Select(t =>
                _service.GetThermalConductivity(GlycolType.Ethylene, concentration, t)).ToArray();

            // Assert - каждое последующее значение должно быть больше предыдущего
            for (int i = 1; i < temperatures.Length; i++)
            {
                Assert.That(conductivities[i], Is.GreaterThan(conductivities[i - 1]),
                    $"Теплопроводность при {temperatures[i]}°C ({conductivities[i]:F3} Вт/(м·К)) должна быть БОЛЬШЕ, " +
                    $"чем при {temperatures[i - 1]}°C ({conductivities[i - 1]:F3} Вт/(м·К)). " +
                    $"Физика: теплопроводность гликолей увеличивается с ростом температуры.");
            }
        }

        #endregion

        #region Комплексные тесты

        /// <summary>
        /// Проверка всех свойств при экстремальных температурах.
        /// </summary>
        [Test]
        public void AllProperties_PhysicallyCorrect_AtExtremeTemperatures()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -30;    // Близко к минимуму (-34.4°C)
            double tempHigh = 80;    // Близко к максимуму (90°C)

            // Act
            var propsLow = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
            var propsHigh = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

            // Assert - все свойства должны соответствовать физическим законам
            Assert.Multiple(() =>
            {
                // Вязкость: низкая температура = высокая вязкость
                Assert.That(propsLow.KinematicViscosity, Is.GreaterThan(propsHigh.KinematicViscosity),
                    $"Вязкость при -30°C ({propsLow.KinematicViscosity:F2}) должна быть > чем при +80°C ({propsHigh.KinematicViscosity:F2})");

                // Плотность: низкая температура = высокая плотность
                Assert.That(propsLow.Density, Is.GreaterThan(propsHigh.Density),
                    $"Плотность при -30°C ({propsLow.Density:F1}) должна быть > чем при +80°C ({propsHigh.Density:F1})");

                // Теплоёмкость: низкая температура = низкая теплоёмкость
                Assert.That(propsLow.SpecificHeat, Is.LessThan(propsHigh.SpecificHeat),
                    $"Теплоёмкость при -30°C ({propsLow.SpecificHeat:F2}) должна быть < чем при +80°C ({propsHigh.SpecificHeat:F2})");

                // Теплопроводность: низкая температура = низкая теплопроводность
                Assert.That(propsLow.ThermalConductivity, Is.LessThan(propsHigh.ThermalConductivity),
                    $"Теплопроводность при -30°C ({propsLow.ThermalConductivity:F3}) должна быть < чем при +80°C ({propsHigh.ThermalConductivity:F3})");
            });
        }

        /// <summary>
        /// Проверка интерполяции между точками данных ASHRAE.
        /// </summary>
        [Test]
        public void Interpolation_BetweenASHRAEPoints_PreservesPhysicalLaws()
        {
            // Arrange - используем температуры между точками данных ASHRAE
            // Точки ASHRAE: -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2
            double concentration = 50;
            double[] interpolatedTemps = { -10, 0, 10, 20, 30, 40, 50, 60, 70, 80 };

            // Act & Assert - проверяем монотонность для каждого свойства
            var viscosities = interpolatedTemps.Select(t =>
                _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, t)).ToArray();

            for (int i = 1; i < interpolatedTemps.Length; i++)
            {
                Assert.That(viscosities[i], Is.LessThan(viscosities[i - 1]),
                    $"Интерполяция нарушила физику: вязкость при {interpolatedTemps[i]}°C ({viscosities[i]:F2}) " +
                    $"должна быть меньше, чем при {interpolatedTemps[i - 1]}°C ({viscosities[i - 1]:F2})");
            }
        }

        /// <summary>
        /// Проверка граничных значений температур.
        /// </summary>
        [Test]
        public void BoundaryTemperatures_ViscosityOrderCorrect()
        {
            // Arrange - проверяем граничные температуры
            double concentration = 50;
            double tempMinus17_8 = -17.8;  // Точка данных ASHRAE
            double tempMinus1_1 = -1.1;    // Точка данных ASHRAE
            double temp15_6 = 15.6;        // Точка данных ASHRAE
            double temp32_2 = 32.2;        // Точка данных ASHRAE
            double temp48_9 = 48.9;        // Точка данных ASHRAE

            // Act
            var v1 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, tempMinus17_8);
            var v2 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, tempMinus1_1);
            var v3 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temp15_6);
            var v4 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temp32_2);
            var v5 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temp48_9);

            // Assert - вязкость должна монотонно уменьшаться
            Assert.Multiple(() =>
            {
                Assert.That(v1, Is.GreaterThan(v2), $"Вязкость при -17.8°C ({v1:F2}) должна быть > чем при -1.1°C ({v2:F2})");
                Assert.That(v2, Is.GreaterThan(v3), $"Вязкость при -1.1°C ({v2:F2}) должна быть > чем при 15.6°C ({v3:F2})");
                Assert.That(v3, Is.GreaterThan(v4), $"Вязкость при 15.6°C ({v3:F2}) должна быть > чем при 32.2°C ({v4:F2})");
                Assert.That(v4, Is.GreaterThan(v5), $"Вязкость при 32.2°C ({v4:F2}) должна быть > чем при 48.9°C ({v5:F2})");
            });
        }

        #endregion

        #region Тесты с разными концентрациями

        [Test]
        public void Viscosity_PhysicallyCorrect_VariousConcentrations()
        {
            // Arrange
            double[] concentrations = { 20, 30, 40, 50, 60, 70, 80 };
            double tempLow = -15;
            double tempHigh = 40;

            foreach (var concentration in concentrations)
            {
                // Act
                var propsLow = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
                var propsHigh = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

                // Assert
                Assert.That(propsLow.KinematicViscosity, Is.GreaterThan(propsHigh.KinematicViscosity),
                    $"[{concentration}%] Вязкость при -15°C ({propsLow.KinematicViscosity:F2}) должна быть > чем при +40°C ({propsHigh.KinematicViscosity:F2})");
            }
        }

        [Test]
        public void Density_PhysicallyCorrect_VariousConcentrations()
        {
            // Arrange
            double[] concentrations = { 20, 30, 40, 50, 60, 70, 80 };
            double tempLow = -15;
            double tempHigh = 40;

            foreach (var concentration in concentrations)
            {
                // Act
                var propsLow = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
                var propsHigh = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

                // Assert
                Assert.That(propsLow.Density, Is.GreaterThan(propsHigh.Density),
                    $"[{concentration}%] Плотность при -15°C ({propsLow.Density:F1}) должна быть > чем при +40°C ({propsHigh.Density:F1})");
            }
        }

        [Test]
        public void SpecificHeat_PhysicallyCorrect_VariousConcentrations()
        {
            // Arrange
            double[] concentrations = { 20, 30, 40, 50, 60, 70, 80 };
            double tempLow = -15;
            double tempHigh = 40;

            foreach (var concentration in concentrations)
            {
                // Act
                var propsLow = _service.GetProperties(GlycolType.Ethylene, concentration, tempLow);
                var propsHigh = _service.GetProperties(GlycolType.Ethylene, concentration, tempHigh);

                // Assert
                Assert.That(propsLow.SpecificHeat, Is.LessThan(propsHigh.SpecificHeat),
                    $"[{concentration}%] Теплоёмкость при -15°C ({propsLow.SpecificHeat:F2}) должна быть < чем при +40°C ({propsHigh.SpecificHeat:F2})");
            }
        }

        #endregion

        #region Тесты для пропиленгликоля

        [Test]
        public void PropyleneGlycol_AllProperties_PhysicallyCorrect()
        {
            // Arrange
            double concentration = 50;
            double tempLow = -15;
            double tempHigh = 40;

            // Act
            var propsLow = _service.GetProperties(GlycolType.Propylene, concentration, tempLow);
            var propsHigh = _service.GetProperties(GlycolType.Propylene, concentration, tempHigh);

            // Assert
            Assert.Multiple(() =>
            {
                // Вязкость: низкая температура = высокая вязкость
                Assert.That(propsLow.KinematicViscosity, Is.GreaterThan(propsHigh.KinematicViscosity),
                    $"Пропиленгликоль: вязкость при -15°C ({propsLow.KinematicViscosity:F2}) должна быть > чем при +40°C ({propsHigh.KinematicViscosity:F2})");

                // Плотность: низкая температура = высокая плотность
                Assert.That(propsLow.Density, Is.GreaterThan(propsHigh.Density),
                    $"Пропиленгликоль: плотность при -15°C ({propsLow.Density:F1}) должна быть > чем при +40°C ({propsHigh.Density:F1})");

                // Теплоёмкость: низкая температура = низкая теплоёмкость
                Assert.That(propsLow.SpecificHeat, Is.LessThan(propsHigh.SpecificHeat),
                    $"Пропиленгликоль: теплоёмкость при -15°C ({propsLow.SpecificHeat:F2}) должна быть < чем при +40°C ({propsHigh.SpecificHeat:F2})");

                // Теплопроводность: низкая температура = низкая теплопроводность
                Assert.That(propsLow.ThermalConductivity, Is.LessThan(propsHigh.ThermalConductivity),
                    $"Пропиленгликоль: теплопроводность при -15°C ({propsLow.ThermalConductivity:F3}) должна быть < чем при +40°C ({propsHigh.ThermalConductivity:F3})");
            });
        }

        #endregion

        #region Тесты для проверки данных JSON

        /// <summary>
        /// Проверка данных JSON: вязкость уменьшается с ростом температуры
        /// </summary>
        [Test]
        public void JsonData_ViscosityDecreasesWithTemperature()
        {
            // Arrange - проверяем точки данных ASHRAE из JSON
            // Для этиленгликоля 60%: -17.8°C = 40.8 мм²/с, 48.9°C = 1.1 мм²/с
            double concentration = 60;

            // Act
            var viscosityAtMinus17_8 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, -17.8);
            var viscosityAt48_9 = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, 48.9);

            // Assert
            Assert.That(viscosityAtMinus17_8, Is.GreaterThan(viscosityAt48_9),
                $"Данные JSON: вязкость при -17.8°C ({viscosityAtMinus17_8:F2}) должна быть > чем при 48.9°C ({viscosityAt48_9:F2})");

            // Проверяем порядок величин по данным ASHRAE
            Assert.That(viscosityAtMinus17_8, Is.GreaterThan(30),
                $"Вязкость при -17.8°C должна быть ~40.8 мм²/с, получено {viscosityAtMinus17_8:F2}");
            Assert.That(viscosityAt48_9, Is.LessThan(2),
                $"Вязкость при 48.9°C должна быть ~1.1 мм²/с, получено {viscosityAt48_9:F2}");
        }

        [Test]
        public void JsonData_DensityDecreasesWithTemperature()
        {
            // Arrange
            double concentration = 50;

            // Act
            var densityAtMinus17_8 = _service.GetDensity(GlycolType.Ethylene, concentration, -17.8);
            var densityAt48_9 = _service.GetDensity(GlycolType.Ethylene, concentration, 48.9);

            // Assert
            Assert.That(densityAtMinus17_8, Is.GreaterThan(densityAt48_9),
                $"Данные JSON: плотность при -17.8°C ({densityAtMinus17_8:F1}) должна быть > чем при 48.9°C ({densityAt48_9:F1})");
        }

        [Test]
        public void JsonData_SpecificHeatIncreasesWithTemperature()
        {
            // Arrange
            double concentration = 50;

            // Act
            var specificHeatAtMinus17_8 = _service.GetSpecificHeat(GlycolType.Ethylene, concentration, -17.8);
            var specificHeatAt48_9 = _service.GetSpecificHeat(GlycolType.Ethylene, concentration, 48.9);

            // Assert
            Assert.That(specificHeatAt48_9, Is.GreaterThan(specificHeatAtMinus17_8),
                $"Данные JSON: теплоёмкость при 48.9°C ({specificHeatAt48_9:F2}) должна быть > чем при -17.8°C ({specificHeatAtMinus17_8:F2})");
        }

        #endregion
    }
}