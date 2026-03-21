using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для класса GlycolProperties
    /// </summary>
    [TestFixture]
    public class GlycolPropertiesTests
    {
        #region Вычисляемые свойства

        [Test]
        public void KinematicViscosity_m2_s_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties { KinematicViscosity = 2.16 }; // мм²/с
            
            // Act & Assert
            Assert.That(props.KinematicViscosity_m2_s, Is.EqualTo(2.16e-6).Within(1e-10));
        }
        
        [Test]
        public void KinematicViscosity_m2_s_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var props = new GlycolProperties { KinematicViscosity = 0 };
            
            // Act & Assert
            Assert.That(props.KinematicViscosity_m2_s, Is.EqualTo(0));
        }
        
        [Test]
        public void DynamicViscosity_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053, // кг/м³
                KinematicViscosity = 2.16 // мм²/с
            };
            
            // Act
            var mu = props.DynamicViscosity;
            
            // Assert
            // μ = ρ × ν = 1053 × 2.16e-6 = 0.00227 Па·с
            Assert.That(mu, Is.EqualTo(0.00227448).Within(0.00001));
        }
        
        [Test]
        public void DynamicViscosity_WithZeroDensity_ReturnsZero()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 0,
                KinematicViscosity = 2.16
            };
            
            // Act & Assert
            Assert.That(props.DynamicViscosity, Is.EqualTo(0));
        }
        
        [Test]
        public void ThermalDiffusivity_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                SpecificHeat = 3.39, // кДж/(кг·К)
                ThermalConductivity = 0.42 // Вт/(м·К)
            };
            
            // Act
            var a = props.ThermalDiffusivity;
            
            // Assert
            // a = λ / (ρ × c_p × 1000) = 0.42 / (1053 × 3.39 × 1000)
            Assert.That(a, Is.GreaterThan(0));
            Assert.That(a, Is.EqualTo(1.176e-7).Within(1e-9));
        }
        
        [Test]
        public void ThermalDiffusivity_WithZeroConductivity_ReturnsZero()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                SpecificHeat = 3.39,
                ThermalConductivity = 0
            };
            
            // Act & Assert
            Assert.That(props.ThermalDiffusivity, Is.EqualTo(0));
        }
        
        [Test]
        public void PrandtlNumber_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                SpecificHeat = 3.39,
                KinematicViscosity = 2.16,
                ThermalConductivity = 0.42
            };
            
            // Act
            var pr = props.PrandtlNumber;
            
            // Assert
            // Pr = ν / a
            Assert.That(pr, Is.GreaterThan(0));
            Assert.That(pr, Is.EqualTo(18.37).Within(0.5));
        }
        
        [Test]
        public void PrandtlNumber_WithTypicalWaterValues_CalculatesCorrectly()
        {
            // Arrange - типичные значения для воды при 20°C
            var props = new GlycolProperties
            {
                Density = 998,
                SpecificHeat = 4.18,
                KinematicViscosity = 1.0,
                ThermalConductivity = 0.6
            };
            
            // Act
            var pr = props.PrandtlNumber;
            
            // Assert
            // Для воды Pr ≈ 7 при 20°C
            Assert.That(pr, Is.GreaterThan(5));
            Assert.That(pr, Is.LessThan(10));
        }

        #endregion

        #region Water

        [Test]
        public void Water_CreatesWaterProperties()
        {
            // Arrange & Act
            var water = GlycolProperties.Water(20);
            
            // Assert
            Assert.That(water.Density, Is.GreaterThan(990).And.LessThan(1000));
            Assert.That(water.SpecificHeat, Is.EqualTo(4.18).Within(0.1));
            Assert.That(water.Concentration, Is.EqualTo(0));
        }
        
        [Test]
        public void Water_WithDifferentTemperatures_ReturnsDifferentValues()
        {
            // Arrange & Act
            var water20 = GlycolProperties.Water(20);
            var water60 = GlycolProperties.Water(60);
            
            // Assert
            Assert.That(water20.Density, Is.GreaterThan(water60.Density));
            Assert.That(water20.KinematicViscosity, Is.Not.EqualTo(water60.KinematicViscosity));
        }
        
        [Test]
        public void Water_SetsCorrectGlycolType()
        {
            // Arrange & Act
            var water = GlycolProperties.Water(20);
            
            // Assert
            Assert.That(water.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }
        
        [Test]
        public void Water_SetsCorrectTemperature()
        {
            // Arrange & Act
            var water = GlycolProperties.Water(40);
            
            // Assert
            Assert.That(water.Temperature, Is.EqualTo(40));
        }
        
        [Test]
        public void Water_WithZeroTemperature_ReturnsValidProperties()
        {
            // Arrange & Act
            var water = GlycolProperties.Water(0);
            
            // Assert
            Assert.That(water.Density, Is.GreaterThan(900));
            Assert.That(water.KinematicViscosity, Is.GreaterThan(0));
        }
        
        [Test]
        public void Water_WithNegativeTemperature_ReturnsValidProperties()
        {
            // Arrange & Act
            var water = GlycolProperties.Water(-10);
            
            // Assert
            Assert.That(water.Density, Is.GreaterThan(900));
            Assert.That(water.KinematicViscosity, Is.GreaterThan(0));
        }

        #endregion

        #region ToString

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39
            };
            
            // Act
            var str = props.ToString();
            
            // Assert
            Assert.That(str, Does.Contain("1053"));
            Assert.That(str, Does.Contain("2.16").Or.Contain("2,16")); // Поддержка разных культур
            Assert.That(str, Does.Contain("3.39").Or.Contain("3,39")); // Поддержка разных культур
        }
        
        [Test]
        public void ToString_ContainsUnits()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39
            };
            
            // Act
            var str = props.ToString();
            
            // Assert
            Assert.That(str, Does.Contain("кг/м³"));
            Assert.That(str, Does.Contain("мм²/с"));
            Assert.That(str, Does.Contain("кДж/(кг·К)"));
        }

        #endregion

        #region GetDetailedDescription

        [Test]
        public void GetDetailedDescription_ReturnsCorrectFormat()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39,
                ThermalConductivity = 0.42,
                Temperature = 40,
                Concentration = 50,
                GlycolType = GlycolType.Ethylene
            };
            
            // Act
            var desc = props.GetDetailedDescription();
            
            // Assert
            Assert.That(desc, Does.Contain("Этиленгликоль"));
            Assert.That(desc, Does.Contain("50%"));
            Assert.That(desc, Does.Contain("40").And.Contains("°C")); // Поддержка разных форматов (40°C или 40,0°C)
            Assert.That(desc, Does.Contain("Плотность"));
            Assert.That(desc, Does.Contain("Вязкость"));
            Assert.That(desc, Does.Contain("Теплоёмкость"));
            Assert.That(desc, Does.Contain("Теплопроводность"));
            Assert.That(desc, Does.Contain("Число Прандтля"));
        }
        
        [Test]
        public void GetDetailedDescription_ForPropylene_ReturnsCorrectName()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1050,
                KinematicViscosity = 2.5,
                SpecificHeat = 3.5,
                ThermalConductivity = 0.4,
                Temperature = 30,
                Concentration = 40,
                GlycolType = GlycolType.Propylene
            };
            
            // Act
            var desc = props.GetDetailedDescription();
            
            // Assert
            Assert.That(desc, Does.Contain("Пропиленгликоль"));
            Assert.That(desc, Does.Contain("40%"));
        }

        #endregion

        #region Empty

        [Test]
        public void Empty_CreatesEmptyProperties()
        {
            // Act
            var props = GlycolProperties.Empty;
            
            // Assert
            Assert.That(props.Density, Is.EqualTo(0));
            Assert.That(props.KinematicViscosity, Is.EqualTo(0));
            Assert.That(props.SpecificHeat, Is.EqualTo(0));
            Assert.That(props.ThermalConductivity, Is.EqualTo(0));
        }

        #endregion

        #region Значения по умолчанию

        [Test]
        public void Default_TemperatureIsZero()
        {
            // Arrange & Act
            var props = new GlycolProperties();
            
            // Assert
            Assert.That(props.Temperature, Is.EqualTo(0));
        }
        
        [Test]
        public void Default_ConcentrationIsZero()
        {
            // Arrange & Act
            var props = new GlycolProperties();
            
            // Assert
            Assert.That(props.Concentration, Is.EqualTo(0));
        }
        
        [Test]
        public void Default_GlycolTypeIsEthylene()
        {
            // Arrange & Act
            var props = new GlycolProperties();
            
            // Assert
            Assert.That(props.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }

        #endregion

        #region Типы гликоля

        [Test]
        public void GlycolProperties_CanBeCreatedWithEthyleneType()
        {
            // Arrange & Act
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39,
                ThermalConductivity = 0.42,
                Temperature = 40,
                Concentration = 50,
                GlycolType = GlycolType.Ethylene
            };
            
            // Assert
            Assert.That(props.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }
        
        [Test]
        public void GlycolProperties_CanBeCreatedWithPropyleneType()
        {
            // Arrange & Act
            var props = new GlycolProperties
            {
                Density = 1050,
                KinematicViscosity = 2.5,
                SpecificHeat = 3.5,
                ThermalConductivity = 0.4,
                Temperature = 30,
                Concentration = 40,
                GlycolType = GlycolType.Propylene
            };
            
            // Assert
            Assert.That(props.GlycolType, Is.EqualTo(GlycolType.Propylene));
        }

        #endregion

        #region Граничные значения

        [Test]
        public void KinematicViscosity_m2_s_WithVerySmallValue_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties { KinematicViscosity = 0.001 };
            
            // Act & Assert
            Assert.That(props.KinematicViscosity_m2_s, Is.EqualTo(1e-9).Within(1e-15));
        }
        
        [Test]
        public void KinematicViscosity_m2_s_WithLargeValue_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties { KinematicViscosity = 100 };
            
            // Act & Assert
            Assert.That(props.KinematicViscosity_m2_s, Is.EqualTo(1e-4).Within(1e-12));
        }
        
        [Test]
        public void DynamicViscosity_WithTypicalValues_CalculatesCorrectly()
        {
            // Arrange - типичные значения для 50% этиленгликоля при 40°C
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var mu = props.DynamicViscosity;
            
            // Assert
            // μ = 1053 × 2.16e-6 ≈ 0.00227 Па·с
            Assert.That(mu, Is.EqualTo(0.00227).Within(0.00001));
        }

        #endregion
    }
}