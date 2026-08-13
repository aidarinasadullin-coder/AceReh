using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.Views.Construction;

namespace SnowMeltingCalculator.Tests.Configuration
{
    /// <summary>
    /// Tests verifying that all editor-related services, ViewModels and views are resolvable from DI.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class DiRegistrationTests
    {
        private ServiceProvider _provider = null!;

        [SetUp]
        public void Setup()
        {
            EnsureApplicationResources();

            var services = new ServiceCollection();
            services.AddApplicationServices();
            _provider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
        }

        [Test]
        public void ClimateLifecycleDescriptors_HaveNoTransientSecondOwner()
        {
            var services = CreateApplicationServices();

            Assert.Multiple(() =>
            {
                Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IProjectSessionClimateState)), Is.False);
                Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ProjectSessionClimateState)), Is.False);
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(ProjectSession)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(IClimateData)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(CalculationContext)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(ClimateViewModel)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(ProjectLoadOrchestrator)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(ResultsViewModel)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            });
        }

        [Test]
        public void ClimateLifecycleConsumers_ObserveCanonicalProjectionChain()
        {
            var provider = CreateApplicationServices().BuildServiceProvider();
            using (provider)
            {
                var session = provider.GetRequiredService<IProjectSession>();
                var climateViewModel = provider.GetRequiredService<ClimateViewModel>();
                var climateData = provider.GetRequiredService<IClimateData>();
                var calculationContext = provider.GetRequiredService<CalculationContext>();
                var projectLoadOrchestrator = provider.GetRequiredService<ProjectLoadOrchestrator>();
                var resultsViewModel = provider.GetRequiredService<ResultsViewModel>();
                var markDirty = provider.GetRequiredService<IMarkDirtyService>();

                Assert.Multiple(() =>
                {
                    Assert.That(session, Is.Not.Null);
                    Assert.That(climateViewModel, Is.Not.Null);
                    Assert.That(climateData, Is.Not.Null);
                    Assert.That(calculationContext, Is.Not.Null);
                    Assert.That(projectLoadOrchestrator, Is.Not.Null);
                    Assert.That(resultsViewModel, Is.Not.Null);
                    Assert.That(markDirty, Is.SameAs(session));
                });

                var result = session.ClimateState.ApplyIndividualEdit(
                    new ClimateEdit(ClimateEditField.AirTemperature, -12.5),
                    ClimateMutationOrigin.User);

                Assert.Multiple(() =>
                {
                    Assert.That(result.IsChanged, Is.True);
                    Assert.That(result.IsValid, Is.True);
                    Assert.That(climateData.AirTemperature, Is.EqualTo(-12.5));
                    Assert.That(calculationContext.Climate, Is.SameAs(climateData));
                    Assert.That(climateViewModel.AirTemperature, Is.EqualTo(-12.5));
                    Assert.That(session.IsDirty, Is.True);
                });
            }
        }

        [Test]
        public void MaterialEditorViewModel_ResolvesFromProvider()
        {
            // Act
            var viewModel = _provider.GetService<MaterialEditorViewModel>();

            // Assert
            Assert.That(viewModel, Is.Not.Null);
        }

        [Test]
        public void TemplateEditorViewModel_ResolvesFromProvider()
        {
            // Act
            var viewModel = _provider.GetService<TemplateEditorViewModel>();

            // Assert
            Assert.That(viewModel, Is.Not.Null);
        }

        [Test]
        public void MaterialEditorView_ResolvesFromProvider()
        {
            // Act
            var view = _provider.GetService<MaterialEditorView>();

            // Assert
            Assert.That(view, Is.Not.Null);
        }

        [Test]
        public void TemplateEditorView_ResolvesFromProvider()
        {
            // Act
            var view = _provider.GetService<TemplateEditorView>();

            // Assert
            Assert.That(view, Is.Not.Null);
        }

        [Test]
        public void EditorDialogService_ResolvesFromProvider()
        {
            // Act
            var service = _provider.GetService<IEditorDialogService>();

            // Assert
            Assert.That(service, Is.Not.Null);
            Assert.That(service, Is.InstanceOf<EditorDialogService>());
        }

        [Test]
        public void ConstructionViewModel_ResolvesFromProvider()
        {
            // Act
            var viewModel = _provider.GetService<ConstructionViewModel>();

            // Assert
            Assert.That(viewModel, Is.Not.Null);
        }

        [Test]
        public void MaterialCrudValidator_ResolvesAsSelfAndInterface()
        {
            // Act
            var concrete = _provider.GetService<SnowMeltingCalculator.Services.Construction.MaterialCrudValidator>();
            var iface = _provider.GetService<SnowMeltingCalculator.Core.IValidator<SnowMeltingCalculator.Models.Construction.Material>>();

            // Assert
            Assert.That(concrete, Is.Not.Null);
            Assert.That(iface, Is.Not.Null);
        }

        [Test]
        public void ConstructionTemplateValidator_ResolvesAsSelfAndInterface()
        {
            // Act
            var concrete = _provider.GetService<SnowMeltingCalculator.Services.Construction.ConstructionTemplateValidator>();
            var iface = _provider.GetService<SnowMeltingCalculator.Core.IValidator<SnowMeltingCalculator.Models.Construction.ConstructionTemplate>>();

            // Assert
            Assert.That(concrete, Is.Not.Null);
            Assert.That(iface, Is.Not.Null);
        }

        private static ServiceCollection CreateApplicationServices()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            return services;
        }

        private static void EnsureApplicationResources()
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            var resources = Application.Current!.Resources;
            if (resources.MergedDictionaries.Count == 0)
            {
                resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/SnowMeltingCalculator;component/Themes/PrimitiveSpacing.xaml")
                });
                resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/SnowMeltingCalculator;component/Themes/PrimitiveRadius.xaml")
                });
                resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/SnowMeltingCalculator;component/Themes/RecalcIndicators.xaml")
                });
                resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/SnowMeltingCalculator;component/Resources/Dictionary.xaml")
                });

                AddInlineBrushes(resources);
            }
        }

        private static void AddInlineBrushes(ResourceDictionary resources)
        {
            // Colors from App.xaml that views and styles depend on.
            resources["RehauRed"] = Color.FromRgb(0xE5, 0x00, 0x40);
            resources["RehauTeal"] = Color.FromRgb(0x4F, 0xC7, 0xB5);
            resources["RehauGray"] = Color.FromRgb(0x57, 0x57, 0x56);
            resources["RehauBlack"] = Color.FromRgb(0x1D, 0x1D, 0x1B);
            resources["RehauWhite"] = Color.FromRgb(0xFF, 0xFF, 0xFF);
            resources["RehauRedLight"] = Color.FromRgb(0xFF, 0x6B, 0x6B);
            resources["RehauRedDark"] = Color.FromRgb(0xB3, 0x00, 0x2E);
            resources["RehauTealLight"] = Color.FromRgb(0x6B, 0xD3, 0xC8);
            resources["RehauTealDark"] = Color.FromRgb(0x2E, 0x9B, 0x8E);
            resources["RehauBackgroundLight"] = Color.FromRgb(0xFA, 0xFA, 0xFA);
            resources["RehauBackgroundGray"] = Color.FromRgb(0xF5, 0xF5, 0xF5);
            resources["RehauCardBackground"] = Color.FromRgb(0xFF, 0xFF, 0xFF);
            resources["ErrorRed"] = Color.FromRgb(0xD3, 0x2F, 0x2F);
            resources["SuccessGreen"] = Color.FromRgb(0x4F, 0xC7, 0xB5);
            resources["Gray50"] = Color.FromRgb(0xFA, 0xFA, 0xFA);
            resources["Gray100"] = Color.FromRgb(0xF5, 0xF5, 0xF5);
            resources["Gray200"] = Color.FromRgb(0xEE, 0xEE, 0xEE);
            resources["Gray300"] = Color.FromRgb(0xE0, 0xE0, 0xE0);
            resources["Gray400"] = Color.FromRgb(0xBD, 0xBD, 0xBD);
            resources["Gray500"] = Color.FromRgb(0x9E, 0x9E, 0x9E);
            resources["Gray600"] = Color.FromRgb(0x75, 0x75, 0x75);
            resources["Gray700"] = Color.FromRgb(0x61, 0x61, 0x61);
            resources["Gray800"] = Color.FromRgb(0x42, 0x42, 0x42);
            resources["Gray900"] = Color.FromRgb(0x21, 0x21, 0x21);

            // Brushes from App.xaml.
            resources["RehauRedBrush"] = new SolidColorBrush((Color)resources["RehauRed"]);
            resources["RehauTealBrush"] = new SolidColorBrush((Color)resources["RehauTeal"]);
            resources["RehauGrayBrush"] = new SolidColorBrush((Color)resources["RehauGray"]);
            resources["RehauBlackBrush"] = new SolidColorBrush((Color)resources["RehauBlack"]);
            resources["RehauWhiteBrush"] = new SolidColorBrush((Color)resources["RehauWhite"]);
            resources["RehauRedLightBrush"] = new SolidColorBrush((Color)resources["RehauRedLight"]);
            resources["RehauRedDarkBrush"] = new SolidColorBrush((Color)resources["RehauRedDark"]);
            resources["RehauTealLightBrush"] = new SolidColorBrush((Color)resources["RehauTealLight"]);
            resources["RehauTealDarkBrush"] = new SolidColorBrush((Color)resources["RehauTealDark"]);
            resources["RehauBackgroundBrush"] = new SolidColorBrush((Color)resources["RehauBackgroundLight"]);
            resources["RehauBackgroundGrayBrush"] = new SolidColorBrush((Color)resources["RehauBackgroundGray"]);
            resources["RehauCardBrush"] = new SolidColorBrush((Color)resources["RehauCardBackground"]);
            resources["Color.Border.Default"] = new SolidColorBrush((Color)resources["Gray300"]);
            resources["Color.Border.Error"] = new SolidColorBrush((Color)resources["ErrorRed"]);
            resources["Color.Border.Success"] = new SolidColorBrush((Color)resources["SuccessGreen"]);
            resources["Color.Border.Warning"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x00));
            resources["Color.Border.Processing"] = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
            resources["Color.Text.Primary"] = new SolidColorBrush((Color)resources["Gray900"]);
            resources["Color.Text.Secondary"] = new SolidColorBrush((Color)resources["Gray600"]);
            resources["Color.Text.Disabled"] = new SolidColorBrush((Color)resources["Gray400"]);
            resources["Color.Text.Brand"] = new SolidColorBrush((Color)resources["RehauRed"]);
            resources["Color.Text.OnBrand"] = new SolidColorBrush((Color)resources["RehauWhite"]);
            resources["RehauResultCardBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8));
            resources["PrimaryHueLightBrush"] = new SolidColorBrush((Color)resources["RehauRedLight"]);
            resources["PrimaryHueLightForegroundBrush"] = Brushes.White;
            resources["PrimaryHueMidBrush"] = new SolidColorBrush((Color)resources["RehauRed"]);
            resources["PrimaryHueMidForegroundBrush"] = Brushes.White;
            resources["PrimaryHueDarkBrush"] = new SolidColorBrush((Color)resources["RehauRedDark"]);
            resources["PrimaryHueDarkForegroundBrush"] = Brushes.White;
            resources["SecondaryHueLightBrush"] = new SolidColorBrush((Color)resources["RehauTealLight"]);
            resources["SecondaryHueLightForegroundBrush"] = Brushes.Black;
            resources["SecondaryHueMidBrush"] = new SolidColorBrush((Color)resources["RehauTeal"]);
            resources["SecondaryHueMidForegroundBrush"] = Brushes.White;
            resources["SecondaryHueDarkBrush"] = new SolidColorBrush((Color)resources["RehauTealDark"]);
            resources["SecondaryHueDarkForegroundBrush"] = Brushes.White;
        }
    }
}
