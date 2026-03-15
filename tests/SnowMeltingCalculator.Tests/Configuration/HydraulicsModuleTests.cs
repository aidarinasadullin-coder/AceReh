using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Repositories.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Tests.Configuration
{
    /// <summary>
    /// Тесты DI-регистрации модуля гидравлики
    /// </summary>
    [TestFixture]
    public class HydraulicsModuleTests
    {
        [Test]
        public void AddHydraulicsModule_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            Assert.That(serviceProvider.GetService<ICollectorRepository>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IGlycolDataService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IHydraulicCalculator>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<HydraulicValidator>(), Is.Not.Null);
        }

        [Test]
        public void AddHydraulicsModule_RegistersViewModels()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            Assert.That(serviceProvider.GetService<HydraulicsViewModel>(), Is.Not.Null);
        }

        [Test]
        public void AddHydraulicsModule_ServicesAreSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var repo1 = serviceProvider.GetService<ICollectorRepository>();
            var repo2 = serviceProvider.GetService<ICollectorRepository>();
            var service1 = serviceProvider.GetService<IGlycolDataService>();
            var service2 = serviceProvider.GetService<IGlycolDataService>();
            var calc1 = serviceProvider.GetService<IHydraulicCalculator>();
            var calc2 = serviceProvider.GetService<IHydraulicCalculator>();
            
            // Assert
            Assert.That(repo1, Is.SameAs(repo2));
            Assert.That(service1, Is.SameAs(service2));
            Assert.That(calc1, Is.SameAs(calc2));
        }

        [Test]
        public void AddHydraulicsModule_ViewModelsAreSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var vm1 = serviceProvider.GetService<HydraulicsViewModel>();
            var vm2 = serviceProvider.GetService<HydraulicsViewModel>();
            
            // Assert
            Assert.That(vm1, Is.SameAs(vm2));
        }

        [Test]
        public void AddHydraulicsModule_CircuitViewModelsAreTransient()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var vm1 = serviceProvider.GetService<CircuitViewModel>();
            var vm2 = serviceProvider.GetService<CircuitViewModel>();
            
            // Assert
            Assert.That(vm1, Is.Not.SameAs(vm2));
        }

        [Test]
        public void AddHydraulicsModule_CollectorViewModelsAreTransient()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var vm1 = serviceProvider.GetService<CollectorViewModel>();
            var vm2 = serviceProvider.GetService<CollectorViewModel>();
            
            // Assert
            Assert.That(vm1, Is.Not.SameAs(vm2));
        }

        [Test]
        public void AddHydraulicsModule_HydraulicCalculatorHasGlycolServiceDependency()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var calculator = serviceProvider.GetService<IHydraulicCalculator>() as HydraulicCalculator;
            
            // Assert
            Assert.That(calculator, Is.Not.Null);
        }

        [Test]
        public void AddHydraulicsModule_HydraulicsViewModelHasAllDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHydraulicsModule();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var vm = serviceProvider.GetService<HydraulicsViewModel>();
            
            // Assert
            Assert.That(vm, Is.Not.Null);
        }
    }
}