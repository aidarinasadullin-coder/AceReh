using System;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    /// <summary>
    /// Тесты интеграции HydraulicsViewModel с ThermalModule
    /// </summary>
    [TestFixture]
    public class HydraulicsViewModelThermalIntegrationTests
    {
        [Test]
        public void HydraulicsViewModel_SubscribesToThermalResultChanged()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult();
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                thermalResult);

            // Act & Assert - не должно быть исключений
            Assert.That(viewModel, Is.Not.Null);
        }

        [Test]
        public void HydraulicsViewModel_UpdatesOnThermalResultChanged()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                IsValid = true,
                VolumeFlowRate = 250.5,
                SupplyTemperature = 55.0,
                ReturnTemperature = 35.0
            };
            
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                thermalResult);

            // Начальные значения
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(200)); // значение по умолчанию
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(50)); // значение по умолчанию
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(30)); // значение по умолчанию

            // Act
            thermalResult.RaiseResultChanged();

            // Assert
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(250.5));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(55.0));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(35.0));
        }

        [Test]
        public void HydraulicsViewModel_DoesNotUpdateOnInvalidThermalResult()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                IsValid = false,
                VolumeFlowRate = 250.5,
                SupplyTemperature = 55.0,
                ReturnTemperature = 35.0
            };
            
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                thermalResult);

            double initialFlowRate = viewModel.VolumeFlowRate;
            double initialSupplyTemp = viewModel.SupplyTemperature;
            double initialReturnTemp = viewModel.ReturnTemperature;

            // Act
            thermalResult.RaiseResultChanged();

            // Assert - значения не должны измениться
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(initialFlowRate));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(initialSupplyTemp));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(initialReturnTemp));
        }

        [Test]
        public void HydraulicsViewModel_UnsubscribesOnDispose()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                IsValid = true,
                VolumeFlowRate = 250.5,
                SupplyTemperature = 55.0,
                ReturnTemperature = 35.0
            };
            
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                thermalResult);

            // Act
            viewModel.Dispose();

            // Assert - после Dispose событие не должно вызывать обновление
            thermalResult.VolumeFlowRate = 999.9;
            thermalResult.SupplyTemperature = 99.0;
            thermalResult.ReturnTemperature = 88.0;
            thermalResult.RaiseResultChanged();

            // Значения должны остаться прежними (по умолчанию)
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(200));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(50));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(30));
        }

        [Test]
        public void HydraulicsViewModel_WorksWithoutThermalResult()
        {
            // Arrange & Act
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                null);

            // Assert - не должно быть исключений
            Assert.That(viewModel, Is.Not.Null);
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(200));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(50));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(30));
        }

        [Test]
        public void HydraulicsViewModel_MultipleThermalResultChanges()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                IsValid = true
            };
            
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                thermalResult);

            // Act & Assert - несколько изменений
            thermalResult.VolumeFlowRate = 100;
            thermalResult.SupplyTemperature = 40;
            thermalResult.ReturnTemperature = 30;
            thermalResult.RaiseResultChanged();
            
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(100));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(40));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(30));

            thermalResult.VolumeFlowRate = 200;
            thermalResult.SupplyTemperature = 50;
            thermalResult.ReturnTemperature = 35;
            thermalResult.RaiseResultChanged();
            
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(200));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(50));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(35));

            thermalResult.VolumeFlowRate = 300;
            thermalResult.SupplyTemperature = 60;
            thermalResult.ReturnTemperature = 40;
            thermalResult.RaiseResultChanged();
            
            Assert.That(viewModel.VolumeFlowRate, Is.EqualTo(300));
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(60));
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(40));
        }

        [Test]
        public void HydraulicsViewModel_ImplementsIDisposable()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult();
            var viewModel = new HydraulicsViewModel(
                new HydraulicCalculator(new GlycolDataService()),
                new GlycolDataService(),
                new CollectorRepository(),
                thermalResult);

            // Act & Assert
            Assert.That(viewModel, Is.InstanceOf<IDisposable>());
            
            // Dispose не должен вызывать исключений
            viewModel.Dispose();
            viewModel.Dispose(); // Повторный Dispose тоже не должен вызывать исключений
        }
    }
}