using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Tests.Fixtures;

namespace SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics
{
    /// <summary>
    /// Интеграционные тесты проверки отсутствия двойного расчёта
    /// </summary>
    /// <remarks>
    /// Проверяет, что Calculate() вызывается только один раз при одновременном изменении данных.
    /// Критические связи:
    /// - MultiplePropertyChanges_TriggersSingleCalculate
    /// - UpdateFromThermalModule_TriggersSingleCalculate
    /// - UpdateFromClimateModule_TriggersSingleCalculate
    /// </remarks>
    [TestFixture]
    public class DoubleCalculationPreventionTests : HydraulicsIntegrationTestBase
    {
        /// <summary>
        /// Публикует результат теплового расчёта в единый контекст (T15 contract)
        /// </summary>
        private void PushThermalResultToContext(ThermalCalculationResult result, PipeType? pipe = null)
        {
            _thermalViewModel.Result = result;
            var inputs = _thermalViewModel.BuildThermalInputs();
            if (pipe != null)
            {
                inputs = inputs with { Pipe = pipe };
            }
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");
            _calculationContext.UpdateThermal(result, "Thermal");
        }

        #region Single Calculation Tests

        [Test]
        public void UpdateFromThermalModule_TriggersSingleCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act - напрямую через публичный метод UpdateFromThermalModule (T15 contract)
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Assert - GetProperties вызывается дважды: для рабочей и расчётной температуры
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Exactly(2),
                "GetProperties должен быть вызван дважды (для рабочей и расчётной температуры) при изменении Result");
        }

        [Test]
        public void UpdateFromClimateModule_TriggersSingleCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _climateViewModel.AirTemperature = -28.0;

            // Assert - GetProperties вызывается дважды: для рабочей и расчётной температуры
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Exactly(2),
                "GetProperties должен быть вызван дважды (для рабочей и расчётной температуры) при изменении AirTemperature");
        }

        [Test]
        public void CanonicalClimateMutation_WithThermalAndCircuitsConsumers_PublishesOneProjectionAndOneDownstreamUpdate()
        {
            // Given
            var projectSession = new ProjectSession(_climateData, _calculationContext);
            var projectionUpdates = 0;
            var climateContextUpdates = 0;
            _climateData.DataChanged += (_, _) => projectionUpdates++;
            _calculationContext.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Climate))
                {
                    climateContextUpdates++;
                }
            };
            _thermalViewModel.Result = new ThermalCalculationResult { IsValid = true };
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(service => service.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // When
            var result = projectSession.ClimateState.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.AirTemperature, -28.0),
                ClimateMutationOrigin.User);

            // Then
            Assert.Multiple(() =>
            {
                Assert.That(result.IsChanged, Is.True);
                Assert.That(projectionUpdates, Is.EqualTo(1));
                Assert.That(climateContextUpdates, Is.EqualTo(1));
                _glycolServiceMock.Verify(
                    service => service.GetProperties(
                        It.IsAny<GlycolType>(),
                        It.IsAny<double>(),
                        It.IsAny<double>()),
                    Times.Exactly(2));
            });
        }

        [Test]
        public void OnGlycolTypeChanged_TriggersSingleCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Propylene;

            // Assert - GetProperties вызывается дважды: для рабочей и расчётной температуры
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    GlycolType.Propylene,
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Exactly(2),
                "GetProperties должен быть вызван дважды (для рабочей и расчётной температуры) при изменении GlycolType");
        }

        [Test]
        public void OnGlycolConcentrationChanged_TriggersSingleCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert - GetProperties вызывается дважды: для рабочей и расчётной температуры
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    40.0,
                    It.IsAny<double>()),
                Times.Exactly(2),
                "GetProperties должен быть вызван дважды (для рабочей и расчётной температуры) при изменении GlycolConcentration");
        }

        #endregion

        #region Multiple Property Changes Tests

        [Test]
        public void MultiplePropertyChanges_ThermalAndClimate_TriggersSeparateCalculates()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act - изменяем Thermal
            _thermalViewModel.Result = thermalResult;
            var thermalCallCount = _glycolServiceMock.Invocations.Count;

            // Act - изменяем Climate
            _climateViewModel.AirTemperature = -28.0;
            var totalCallCount = _glycolServiceMock.Invocations.Count;

            // Assert - Calculate должен быть вызван дважды для каждого изменения (2 вызова = 2 температуры)
            Assert.That(totalCallCount, Is.EqualTo(thermalCallCount + 2),
                "Calculate должен быть вызван дважды для каждого изменения (рабочая и расчётная температура)");
        }

        [Test]
        public void MultiplePropertyChanges_GlycolTypeAndConcentration_TriggersSeparateCalculates()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - изменяем тип гликоля
            _viewModel.InputData.GlycolType = GlycolType.Propylene;
            var typeCallCount = _glycolServiceMock.Invocations.Count;

            // Act - изменяем концентрацию
            _viewModel.InputData.GlycolConcentration = 40.0;
            var totalCallCount = _glycolServiceMock.Invocations.Count;

            // Assert - Calculate должен быть вызван дважды для каждого изменения
            Assert.That(totalCallCount, Is.EqualTo(typeCallCount + 2),
                "Calculate должен быть вызван дважды для каждого изменения (рабочая и расчётная температура)");
        }

        [Test]
        public void SequentialThermalChanges_TriggersSeparateCalculates()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - первое изменение через контекст (T15 contract)
            var thermalResult1 = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };
            PushThermalResultToContext(thermalResult1);
            var firstCallCount = _glycolServiceMock.Invocations.Count;

            // Act - второе изменение через контекст
            var thermalResult2 = new ThermalCalculationResult
            {
                PowerUp = 300.0,
                PowerDown = 10.0,
                SupplyTemperature = 60.0,
                ReturnTemperature = 40.0,
                IsValid = true
            };
            PushThermalResultToContext(thermalResult2);
            var totalCallCount = _glycolServiceMock.Invocations.Count;

            // Assert - Calculate должен быть вызван дважды для каждого изменения
            Assert.That(totalCallCount, Is.EqualTo(firstCallCount + 2),
                "Calculate должен быть вызван дважды для каждого изменения Result (рабочая и расчётная температура)");
        }

        #endregion

        #region No Double Calculation Tests

        [Test]
        public void SameThermalResult_DoesNotTriggerDuplicateCalculate()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            _thermalViewModel.Result = thermalResult;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - устанавливаем тот же результат
            _thermalViewModel.Result = thermalResult;

            // Assert - Calculate не должен быть вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never,
                "Calculate не должен вызываться при установке того же результата");
        }

        [Test]
        public void SameClimateValue_DoesNotTriggerDuplicateCalculate()
        {
            // Arrange
            _climateViewModel.AirTemperature = -28.0;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - устанавливаем то же значение
            _climateViewModel.AirTemperature = -28.0;

            // Assert - Calculate не должен быть вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never,
                "Calculate не должен вызываться при установке того же значения");
        }

        [Test]
        public void SameGlycolType_DoesNotTriggerDuplicateCalculate()
        {
            // Arrange
            _viewModel.InputData.GlycolType = GlycolType.Propylene;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - устанавливаем тот же тип
            _viewModel.InputData.GlycolType = GlycolType.Propylene;

            // Assert - Calculate не должен быть вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never,
                "Calculate не должен вызываться при установке того же типа гликоля");
        }

        [Test]
        public void SameGlycolConcentration_DoesNotTriggerDuplicateCalculate()
        {
            // Arrange
            _viewModel.InputData.GlycolConcentration = 40.0;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - устанавливаем ту же концентрацию
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert - Calculate не должен быть вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never,
                "Calculate не должен вызываться при установке той же концентрации");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act - последовательность изменений
            // T15: изменение Thermal публикуется в контексте, Climate сбрасывает ThermalResult
            // и всегда вызывает пересчёт, InputData продолжает вызывать пересчёт.
            PushThermalResultToContext(thermalResult); // 2 вызова (рабочая + расчётная температура)
            _climateViewModel.AirTemperature = -28.0; // 2 вызова
            _viewModel.InputData.GlycolType = GlycolType.Propylene; // 2 вызова
            _viewModel.InputData.GlycolConcentration = 40.0; // 2 вызова

            // Assert - всего 8 вызовов: каждое изменение приводит к одному Calculate,
            // а каждый Calculate вызывает GetProperties дважды (рабочая и расчётная температура).
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Exactly(8),
                "Calculate должен быть вызван 4 раза (по 2 GetProperties каждый): Thermal, Climate, GlycolType, GlycolConcentration");
        }

        [Test]
        public void RapidChanges_OnlyTriggersNecessaryCalculates()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - быстрые изменения
            _viewModel.InputData.GlycolType = GlycolType.Propylene; // 2 вызова
            _viewModel.InputData.GlycolConcentration = 40.0; // 2 вызова
            _viewModel.InputData.GlycolConcentration = 45.0; // 2 вызова
            _viewModel.InputData.GlycolConcentration = 50.0; // 2 вызова

            // Assert - 8 вызовов (по 2 для каждого уникального изменения)
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Exactly(8),
                "Calculate должен быть вызван дважды для каждого уникального изменения (рабочая и расчётная температура)");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void NullThermalResult_DoesNotTriggerCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _thermalViewModel.Result = null;

            // Assert - Calculate не должен быть вызван (или только для сброса)
            // Примечание: поведение зависит от реализации
            // В данном случае ожидаем, что Calculate не будет вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtMost(1),
                "Calculate не должен вызываться многократно при null результате");
        }

        [Test]
        public void InvalidThermalResult_DoesNotTriggerCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            var invalidResult = new ThermalCalculationResult
            {
                IsValid = false,
                ValidationErrors = new[] { "Ошибка валидации" }
            };

            // Act
            _thermalViewModel.Result = invalidResult;

            // Assert - Calculate не должен быть вызван для невалидного результата
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtMost(1),
                "Calculate не должен вызываться многократно для невалидного результата");
        }

        #endregion
    }
}
