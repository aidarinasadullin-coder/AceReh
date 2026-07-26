using System;
using System.Collections.Generic;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Тесты для CalculationContext (живой контракт после сужения, этап D2)
    /// </summary>
    [TestFixture]
    public class CalculationContextTests
    {
        #region Тесты обновления данных

        [Test]
        public void UpdateClimate_ValidData_RaisesContextChanged()
        {
            // Arrange
            var context = new CalculationContext();
            var climateData = new ClimateData
            {
                SelectedCity = "Москва",
                AirTemperature = -15.0,
                WindSpeed = 5.0,
                SnowfallIntensity = 0.3
            };

            ContextChangedEventArgs? eventArgs = null;
            context.ContextChanged += (s, e) => eventArgs = e;

            // Act
            context.UpdateClimate(climateData);

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.PropertyName, Is.EqualTo(nameof(CalculationContext.Climate)));
            Assert.That(eventArgs.NewValue, Is.EqualTo(climateData));
        }

        [Test]
        public void UpdateClimate_ResetsThermalAndHydraulicsResults()
        {
            // Arrange
            var context = new CalculationContext();
            var climateData1 = new ClimateData
            {
                SelectedCity = "Москва",
                AirTemperature = -15.0
            };
            var climateData2 = new ClimateData
            {
                SelectedCity = "Санкт-Петербург",
                AirTemperature = -10.0
            };

            context.UpdateClimate(climateData1);
            context.UpdateThermal(new ThermalCalculationResult { IsValid = true });
            context.UpdateHydraulics(new List<CollectorSummary> { new CollectorSummary() });

            // Act
            context.UpdateClimate(climateData2);

            // Assert
            Assert.That(context.ThermalResult, Is.Null);
            Assert.That(context.HydraulicsResults, Is.Null);
        }

        [Test]
        public void UpdateConstruction_ValidData_RaisesContextChanged()
        {
            // Arrange
            var context = new CalculationContext();
            var constructionData = new ConstructionData
            {
                R1Total = 0.05,
                R2Total = 0.1,
                LambdaE = 1.6
            };

            ContextChangedEventArgs? eventArgs = null;
            context.ContextChanged += (s, e) => eventArgs = e;

            // Act
            context.UpdateConstruction(constructionData);

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.PropertyName, Is.EqualTo(nameof(CalculationContext.Construction)));
        }

        [Test]
        public void UpdateThermal_ValidResult_RaisesContextChanged()
        {
            // Arrange
            var context = new CalculationContext();
            var result = new ThermalCalculationResult
            {
                IsValid = true,
                PowerUp = 150.0,
                PowerDown = 30.0,
                PowerTotal = 180.0
            };

            ContextChangedEventArgs? eventArgs = null;
            context.ContextChanged += (s, e) => eventArgs = e;

            // Act
            context.UpdateThermal(result);

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.PropertyName, Is.EqualTo(nameof(CalculationContext.ThermalResult)));
            Assert.That(context.IsThermalValid, Is.True);
        }

        [Test]
        public void UpdateHydraulics_ValidResults_RaisesContextChanged()
        {
            // Arrange
            var context = new CalculationContext();
            var results = new List<CollectorSummary>
            {
                new CollectorSummary { CollectorNumber = 1, IsValid = true }
            };

            ContextChangedEventArgs? eventArgs = null;
            context.ContextChanged += (s, e) => eventArgs = e;

            // Act
            context.UpdateHydraulics(results);

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.PropertyName, Is.EqualTo(nameof(CalculationContext.HydraulicsResults)));
            Assert.That(context.HydraulicsResults, Is.SameAs(results));
        }

        #endregion

        #region Тесты сброса

        [Test]
        public void Reset_ClearsAllData()
        {
            // Arrange
            var context = new CalculationContext();
            var climateData = new ClimateData { SelectedCity = "Москва" };
            var constructionData = new ConstructionData { R1Total = 0.05 };
            var thermalResult = new ThermalCalculationResult { IsValid = true };

            context.UpdateClimate(climateData);
            context.UpdateConstruction(constructionData);
            context.UpdateThermal(thermalResult);

            // Act
            context.Reset();

            // Assert
            Assert.That(context.Climate, Is.Null);
            Assert.That(context.Construction, Is.Null);
            Assert.That(context.ThermalResult, Is.Null);
            Assert.That(context.HydraulicsResults, Is.Null);
        }

        #endregion

        #region Тесты свойств

        [Test]
        public void Properties_ReturnCorrectValues()
        {
            // Arrange
            var context = new CalculationContext();
            var climateData = new ClimateData
            {
                SelectedCity = "Москва",
                AirTemperature = -15.0,
                WindSpeed = 5.0,
                SnowfallIntensity = 0.3
            };
            var thermalResult = new ThermalCalculationResult
            {
                IsValid = true,
                PowerUp = 150.0,
                PowerDown = 30.0,
                PowerTotal = 180.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 40.0,
                DeltaT = 10.0
            };

            context.UpdateClimate(climateData);
            context.UpdateThermal(thermalResult);

            // Assert
            Assert.That(context.AirTemperature, Is.EqualTo(-15.0));
            Assert.That(context.PowerUp, Is.EqualTo(150.0));
            Assert.That(context.PowerDown, Is.EqualTo(30.0));
            Assert.That(context.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(context.ReturnTemperature, Is.EqualTo(40.0));
            Assert.That(context.IsThermalValid, Is.True);
        }

        [Test]
        public void Properties_WhenNull_ReturnDefaults()
        {
            // Arrange
            var context = new CalculationContext();

            // Assert
            Assert.That(context.AirTemperature, Is.EqualTo(0.0));
            Assert.That(context.PowerUp, Is.EqualTo(0.0));
            Assert.That(context.PowerDown, Is.EqualTo(0.0));
            Assert.That(context.SupplyTemperature, Is.EqualTo(0.0));
            Assert.That(context.ReturnTemperature, Is.EqualTo(0.0));
            Assert.That(context.IsThermalValid, Is.False);
        }

        #endregion
    }
}
