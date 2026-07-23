using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Thermal;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Тесты для ValidationPipeline
    /// </summary>
    [TestFixture]
    public class ValidationPipelineTests
    {
        private static IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            return services.BuildServiceProvider();
        }

        private static CalculationContext CreateValidContext(IServiceProvider provider)
        {
            var climate = (ClimateData)provider.GetRequiredService<IClimateData>();
            climate.SelectedCity = "Москва";
            climate.AirTemperature = -20.0;
            climate.WindSpeed = 5.0;
            climate.SnowfallIntensity = 1.0;

            var construction = (ConstructionModel)provider.GetRequiredService<IConstructionData>();
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон");
            var sand = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            construction.AddLayerAbovePipe(concrete, 50);
            construction.AddLayerBelowPipe(sand, 150);

            var context = provider.GetRequiredService<CalculationContext>();
            context.UpdateClimate(climate);
            context.UpdateConstruction(construction);
            context.UpdateThermalInputs(new ThermalInputs());
            context.UpdateHydraulics(new HydraulicInputData());

            return context;
        }

        #region DI Resolution Tests

        [Test]
        public void AddApplicationServices_ResolvesIValidationPipeline()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var pipeline = provider.GetRequiredService<IValidationPipeline>();

            // Assert
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(pipeline, Is.TypeOf<ValidationPipeline>());
        }

        [Test]
        public void AddApplicationServices_ResolvesClimateValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var validator = provider.GetRequiredService<IValidator<IClimateData>>();

            // Assert
            Assert.That(validator, Is.Not.Null);
            Assert.That(validator, Is.TypeOf<ClimateValidator>());
        }

        [Test]
        public void AddApplicationServices_ResolvesConstructionValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var validator = provider.GetRequiredService<IValidator<ConstructionModel>>();

            // Assert
            Assert.That(validator, Is.Not.Null);
            Assert.That(validator, Is.TypeOf<ConstructionValidator>());
        }

        [Test]
        public void AddApplicationServices_ResolvesThermalValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var validator = provider.GetRequiredService<IValidator<ThermalInputs>>();

            // Assert
            Assert.That(validator, Is.Not.Null);
            Assert.That(validator, Is.TypeOf<ThermalValidator>());
        }

        [Test]
        public void AddApplicationServices_ResolvesThermalResultValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var validator = provider.GetRequiredService<IValidator<ThermalCalculationResult>>();

            // Assert
            Assert.That(validator, Is.Not.Null);
            Assert.That(validator, Is.TypeOf<ThermalResultValidator>());
        }

        [Test]
        public void AddApplicationServices_ResolvesHydraulicValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var validator = provider.GetRequiredService<IValidator<HydraulicInputData>>();

            // Assert
            Assert.That(validator, Is.Not.Null);
            Assert.That(validator, Is.TypeOf<HydraulicValidator>());
        }

        [Test]
        public void AddApplicationServices_ResolvesCircuitValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();

            // Act
            var validator = provider.GetRequiredService<IValidator<CircuitRow>>();

            // Assert
            Assert.That(validator, Is.Not.Null);
            Assert.That(validator, Is.TypeOf<CircuitValidator>());
        }

        #endregion

        #region ValidateAll Tests

        [Test]
        public void ValidateAll_NullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pipeline.ValidateAll(null!));
        }

        [Test]
        public void ValidateAll_EmptyContext_ReturnsValid()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();
            var context = provider.GetRequiredService<CalculationContext>();

            // Act
            var result = pipeline.ValidateAll(context);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateAll_ValidContext_ReturnsValid()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();
            var context = CreateValidContext(provider);

            // Act
            var result = pipeline.ValidateAll(context);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateAll_InvalidContext_MergesErrors()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();

            var climate = (ClimateData)provider.GetRequiredService<IClimateData>();
            climate.SelectedCity = "Москва";
            climate.AirTemperature = 100.0; // Недопустимое значение
            climate.WindSpeed = 5.0;
            climate.SnowfallIntensity = 1.0;

            var construction = (ConstructionModel)provider.GetRequiredService<IConstructionData>();
            // Конструкция без слоёв — недопустима

            var context = provider.GetRequiredService<CalculationContext>();
            context.UpdateClimate(climate);
            context.UpdateConstruction(construction);
            // Тепловые и гидравлические данные не заданы — пропускаются

            // Act
            var result = pipeline.ValidateAll(context);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Температура")),
                Is.True, "Ожидалась ошибка климатического валидатора");
            Assert.That(result.Errors.Any(e => e.Message.Contains("хотя бы один слой")),
                Is.True, "Ожидалась ошибка валидатора конструкции");
        }

        [Test]
        public void ValidateAll_NullInputs_SkipsValidators()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();
            var context = CreateValidContext(provider);

            context.UpdateThermalInputs(null!);
            context.UpdateHydraulics((HydraulicInputData)null!);

            // Act
            var result = pipeline.ValidateAll(context);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Validate<T> Tests

        [Test]
        public void Validate_WithValidator_DelegatesToValidator()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();
            var validator = provider.GetRequiredService<IValidator<HydraulicInputData>>();
            var input = new HydraulicInputData { GlycolConcentration = 5.0 };

            // Act
            var result = pipeline.Validate(input, validator);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Концентрация гликоля")), Is.True);
        }

        [Test]
        public void Validate_NullValidator_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = CreateServiceProvider();
            var pipeline = provider.GetRequiredService<IValidationPipeline>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pipeline.Validate(new HydraulicInputData(), null!));
        }

        #endregion
    }
}
