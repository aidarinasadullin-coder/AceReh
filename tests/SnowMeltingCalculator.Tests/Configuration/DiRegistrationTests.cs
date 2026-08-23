using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Shell;
using SnowMeltingCalculator.ViewModels.Thermal;
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
        public void ConstructionLifecycleDescriptors_HaveOneCanonicalOwnerAndReadProjection()
        {
            var services = CreateApplicationServices();

            Assert.Multiple(() =>
            {
                Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ProjectSessionConstructionState)), Is.False);
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(IProjectSessionConstructionState)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(IConstructionData)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(ConstructionModel)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            });
        }

        [Test]
        public void ConstructionLifecycleConsumers_ObserveCanonicalSessionStateAndProjection()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var concreteSession = provider.GetRequiredService<ProjectSession>();
            var session = provider.GetRequiredService<IProjectSession>();
            var state = provider.GetRequiredService<IProjectSessionConstructionState>();
            var constructionData = provider.GetRequiredService<IConstructionData>();
            var compatibilityModel = provider.GetRequiredService<ConstructionModel>();
            var initializer = provider.GetRequiredService<ConstructionDefaultStateInitializer>();
            var constructionViewModel = provider.GetRequiredService<ConstructionViewModel>();
            var orchestrator = provider.GetRequiredService<ProjectLoadOrchestrator>();
            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            var resultsViewModel = provider.GetRequiredService<ResultsViewModel>();
            var thermalViewModel = provider.GetRequiredService<ThermalViewModel>();
            var initializerState = typeof(ConstructionDefaultStateInitializer)
                .GetField("_constructionState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(initializer);

            Assert.Multiple(() =>
            {
                Assert.That(session, Is.SameAs(concreteSession));
                Assert.That(provider.GetRequiredService<IMarkDirtyService>(), Is.SameAs(concreteSession));
                Assert.That(provider.GetRequiredService<IProjectStateService>(), Is.SameAs(concreteSession));
                Assert.That(state, Is.SameAs(session.ConstructionState));
                Assert.That(constructionData, Is.SameAs(state.CurrentProjection));
                Assert.That(constructionData, Is.Not.SameAs(compatibilityModel));
                Assert.That(initializerState, Is.SameAs(session.ConstructionState));
                Assert.That(provider.GetRequiredService<ConstructionViewModel>(), Is.SameAs(constructionViewModel));
                Assert.That(provider.GetRequiredService<ProjectLoadOrchestrator>(), Is.SameAs(orchestrator));
                Assert.That(provider.GetRequiredService<MainViewModel>(), Is.SameAs(mainViewModel));
                Assert.That(provider.GetRequiredService<ResultsViewModel>(), Is.SameAs(resultsViewModel));
                Assert.That(provider.GetRequiredService<ThermalViewModel>(), Is.SameAs(thermalViewModel));
            });
        }

        [Test]
        public void ConstructionCanonicalMutation_RefreshesThermalProjectionAndCalculationContext()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();
            var session = provider.GetRequiredService<IProjectSession>();
            var constructionData = provider.GetRequiredService<IConstructionData>();
            var context = provider.GetRequiredService<CalculationContext>();
            var layer = new ConstructionLayerSnapshot(
                Guid.NewGuid(), 5, "Concrete", 100.0, 2.0, false, LayerPosition.AbovePipe, 0);

            var result = session.ConstructionState.ApplySnapshot(
                new ConstructionStateSnapshot(2.0, false, new[] { layer }, Array.Empty<ConstructionLayerSnapshot>()),
                ConstructionMutationOrigin.User);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
                Assert.That(constructionData.R1Total, Is.EqualTo(0.05).Within(1e-10));
                Assert.That(constructionData.LambdaE, Is.EqualTo(2.0).Within(1e-10));
                Assert.That(context.Construction, Is.SameAs(constructionData));
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalLifecycleDescriptors_HaveNoIndependentRegistration()
        {
            var services = CreateApplicationServices();

            Assert.Multiple(() =>
            {
                // DEC-T01: the canonical Thermal state is created and owned by
                // ProjectSession; it is never registered in DI by itself.
                Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IProjectSessionThermalState)), Is.False);
                Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ProjectSessionThermalState)), Is.False);

                // The owning session keeps its singleton lifetime.
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(ProjectSession)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(IProjectSession)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalState_ResolvesReferenceIdenticalThroughEverySessionAlias()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var concreteSession = provider.GetRequiredService<ProjectSession>();
            var session = provider.GetRequiredService<IProjectSession>();
            var projectInfo = provider.GetRequiredService<IProjectInfoService>();
            var projectState = provider.GetRequiredService<IProjectStateService>();
            var markDirty = provider.GetRequiredService<IMarkDirtyService>();

            Assert.Multiple(() =>
            {
                // Every legacy lifecycle alias resolves to the one owning session.
                Assert.That(session, Is.SameAs(concreteSession));
                Assert.That(projectInfo, Is.SameAs(concreteSession));
                Assert.That(projectState, Is.SameAs(concreteSession));
                Assert.That(markDirty, Is.SameAs(concreteSession));

                // Repeated accesses return the reference-identical instance.
                Assert.That(session.ThermalState, Is.Not.Null);
                Assert.That(session.ThermalState, Is.SameAs(session.ThermalState));
                Assert.That(concreteSession.ThermalState, Is.SameAs(session.ThermalState));

                // Both resolution paths (concrete type and IProjectSession)
                // expose exactly the same owning state instance.
                Assert.That(((IProjectSession)concreteSession).ThermalState, Is.SameAs(session.ThermalState));

                // The exposed contract is the Todo-3 canonical implementation.
                Assert.That(session.ThermalState, Is.InstanceOf<ProjectSessionThermalState>());
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalState_IsNotResolvableAsIndependentService_FromBuiltProvider()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            Assert.Multiple(() =>
            {
                // Provider self-validation: enumerating descriptors of a built
                // provider yields no independent Thermal-state registration.
                Assert.That(provider.GetServices<IProjectSessionThermalState>(), Is.Empty);

                // Neither the interface nor the concrete type is resolvable.
                Assert.That(provider.GetService<ProjectSessionThermalState>(), Is.Null);

                // The only path to the state is through the owning session.
                Assert.That(provider.GetRequiredService<IProjectSession>().ThermalState, Is.Not.Null);
            });
        }

        [Test]
        [Category("HydraulicsState")]
        public void HydraulicsState_ResolvesReferenceIdenticalThroughEverySessionAlias()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var concreteSession = provider.GetRequiredService<ProjectSession>();
            var session = provider.GetRequiredService<IProjectSession>();
            var markDirty = provider.GetRequiredService<IMarkDirtyService>();

            Assert.Multiple(() =>
            {
                Assert.That(session, Is.SameAs(concreteSession));
                Assert.That(markDirty, Is.SameAs(concreteSession));
                Assert.That(session.HydraulicsState, Is.Not.Null);
                Assert.That(session.HydraulicsState, Is.SameAs(session.HydraulicsState));
                Assert.That(concreteSession.HydraulicsState, Is.SameAs(session.HydraulicsState));
                Assert.That(((IProjectSession)concreteSession).HydraulicsState, Is.SameAs(session.HydraulicsState));
                Assert.That(session.HydraulicsState, Is.InstanceOf<ProjectSessionHydraulicsState>());
            });
        }

        [Test]
        [Category("HydraulicsState")]
        public void HydraulicsState_IsNotResolvableAsIndependentService_FromBuiltProvider()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            Assert.Multiple(() =>
            {
                Assert.That(provider.GetServices<IProjectSessionHydraulicsState>(), Is.Empty);
                Assert.That(provider.GetService<ProjectSessionHydraulicsState>(), Is.Null);
                Assert.That(provider.GetRequiredService<IProjectSession>().HydraulicsState, Is.Not.Null);
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalState_IsOnePerSession_SingletonAcrossScopes_DistinctAcrossSessions()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var rootSession = provider.GetRequiredService<IProjectSession>();

            // Singleton session: any scope resolves the same session and thus
            // the same single Thermal state (one state per application).
            using (var scope = provider.CreateScope())
            {
                var scopedSession = scope.ServiceProvider.GetRequiredService<IProjectSession>();
                Assert.Multiple(() =>
                {
                    Assert.That(scopedSession, Is.SameAs(rootSession));
                    Assert.That(scopedSession.ThermalState, Is.SameAs(rootSession.ThermalState));
                });
            }

            // A separate composition root owns a distinct session whose state
            // is distinct too — still exactly one per session lifetime.
            using var secondProvider = CreateApplicationServices().BuildServiceProvider();
            var secondSession = secondProvider.GetRequiredService<IProjectSession>();
            Assert.Multiple(() =>
            {
                Assert.That(secondSession, Is.Not.SameAs(rootSession));
                Assert.That(secondSession.ThermalState, Is.Not.SameAs(rootSession.ThermalState));
                Assert.That(secondSession.ThermalState, Is.SameAs(secondSession.ThermalState));
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalState_DuplicateIndependentRegistration_IsFlaggedByDescriptorGuard()
        {
            // Canonical composition: zero independent descriptors — guard passes.
            var canonical = CreateApplicationServices();
            Assert.That(CountIndependentThermalStateDescriptors(canonical), Is.EqualTo(0));

            // Synthetic defect model (task-local negative case): an accidental
            // duplicate independent registration of the state produces more
            // than one descriptor and must be flagged by the same guard that
            // the canonical composition satisfies. No service locator involved:
            // the guard inspects registration descriptors only.
            var defective = CreateApplicationServices();
            defective.AddSingleton<IProjectSessionThermalState>(new ProjectSessionThermalState());
            defective.AddSingleton<IProjectSessionThermalState>(new ProjectSessionThermalState());
            Assert.That(CountIndependentThermalStateDescriptors(defective), Is.GreaterThan(1));
        }

        [Test]
        [Category("NegativeFixture")]
        public void ThermalState_IndependentRegistration_IsRejectedByDescriptorAndRuntimeIdentityGuards()
        {
            var services = CreateApplicationServices();
            Assert.That(CountIndependentThermalStateDescriptors(services), Is.Zero);

            services.AddSingleton<IProjectSessionThermalState>(new ProjectSessionThermalState());
            using var defectiveProvider = services.BuildServiceProvider();
            var independentState = defectiveProvider.GetServices<IProjectSessionThermalState>().Single();
            var canonicalState = defectiveProvider.GetRequiredService<IProjectSession>().ThermalState;

            Assert.Multiple(() =>
            {
                Assert.That(CountIndependentThermalStateDescriptors(services), Is.EqualTo(1));
                Assert.That(independentState, Is.Not.SameAs(canonicalState));
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalState_ConstructorCycle_IsRejectedByContainerWithoutServiceLocator()
        {
            // Synthetic cycle model (task-local negative case): the defect being
            // modeled is an INDEPENDENT registration of the Thermal state whose
            // implementation constructor depends on a consumer that itself
            // requires the state. The whole cycle is declared through
            // implementation types, so the container rejects it while building
            // the resolution plan ("A circular dependency was detected") before
            // any instance exists. No service locator or cycle workaround is
            // involved anywhere.
            var services = new ServiceCollection();
            services.AddSingleton<CycleDependentConsumer>();
            services.AddSingleton<IProjectSessionThermalState, CycleStateImplementation>();

            using var provider = services.BuildServiceProvider();

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => provider.GetRequiredService<CycleDependentConsumer>(),
                    Throws.InvalidOperationException.With.Message.Contains("circular").IgnoreCase);
                Assert.That(
                    () => provider.GetRequiredService<IProjectSessionThermalState>(),
                    Throws.InvalidOperationException.With.Message.Contains("circular").IgnoreCase);
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalState_Addition_PreservesClimateAndConstructionIdentities()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var concreteSession = provider.GetRequiredService<ProjectSession>();
            var session = provider.GetRequiredService<IProjectSession>();
            var constructionState = provider.GetRequiredService<IProjectSessionConstructionState>();
            var constructionData = provider.GetRequiredService<IConstructionData>();

            Assert.Multiple(() =>
            {
                // Climate slice: unchanged identities (no independent registration,
                // stable reference across repeated accesses and aliases).
                Assert.That(provider.GetServices<IProjectSessionClimateState>(), Is.Empty);
                Assert.That(session.ClimateState, Is.SameAs(session.ClimateState));
                Assert.That(session.ClimateState, Is.SameAs(concreteSession.ClimateState));

                // Construction slice: unchanged identities.
                Assert.That(constructionState, Is.SameAs(session.ConstructionState));
                Assert.That(constructionData, Is.SameAs(constructionState.CurrentProjection));

                // Legacy lifecycle aliases: unchanged identity with the session.
                Assert.That(provider.GetRequiredService<IMarkDirtyService>(), Is.SameAs(concreteSession));
                Assert.That(provider.GetRequiredService<IProjectStateService>(), Is.SameAs(concreteSession));
                Assert.That(provider.GetRequiredService<IProjectInfoService>(), Is.SameAs(concreteSession));

                // The new Thermal slice follows the same ownership pattern.
                Assert.That(session.ThermalState, Is.SameAs(concreteSession.ThermalState));
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalStateCoordinator_IsSingleton_ReferenceIdenticalWithSessionState()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var first = provider.GetRequiredService<IThermalStateCoordinator>();
            var second = provider.GetRequiredService<IThermalStateCoordinator>();
            var session = provider.GetRequiredService<IProjectSession>();

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.SameAs(second),
                    "Exactly one coordinator instance exists per composition.");
                Assert.That(first.State, Is.SameAs(session.ThermalState),
                    "The coordinator owns the reference-identical canonical state slice.");
                Assert.That(first, Is.InstanceOf<ThermalStateCoordinator>());
            });
        }

        [Test]
        [Category("ThermalState")]
        public void ThermalViewModel_EagerlyMaterializesTheSingleCoordinator()
        {
            using var provider = CreateApplicationServices().BuildServiceProvider();

            var viewModel = provider.GetRequiredService<ThermalViewModel>();
            var viewModelAgain = provider.GetRequiredService<ThermalViewModel>();
            var coordinator = provider.GetRequiredService<IThermalStateCoordinator>();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel, Is.SameAs(viewModelAgain),
                    "ThermalViewModel is an application singleton.");
                Assert.That(viewModel.Coordinator, Is.SameAs(coordinator),
                    "The ViewModel eagerly materialized the one DI coordinator via ctor injection.");
            });
        }

        private static int CountIndependentThermalStateDescriptors(IServiceCollection services)
        {
            return services.Count(descriptor =>
                descriptor.ServiceType == typeof(IProjectSessionThermalState) ||
                descriptor.ServiceType == typeof(ProjectSessionThermalState));
        }

        /// <summary>
        /// Synthetic consumer used only to model a constructor cycle in
        /// <see cref="ThermalState_ConstructorCycle_IsRejectedByContainerWithoutServiceLocator"/>.
        /// </summary>
        private sealed class CycleDependentConsumer
        {
            public CycleDependentConsumer(IProjectSessionThermalState thermalState)
            {
                ThermalState = thermalState;
            }

            public IProjectSessionThermalState ThermalState { get; }
        }

        /// <summary>
        /// Synthetic independent Thermal-state registration used only to close the
        /// constructor cycle modeled in
        /// <see cref="ThermalState_ConstructorCycle_IsRejectedByContainerWithoutServiceLocator"/>.
        /// Its constructor depends back on <see cref="CycleDependentConsumer"/>;
        /// members are never invoked because the container rejects the graph.
        /// </summary>
        private sealed class CycleStateImplementation : IProjectSessionThermalState
        {
            public CycleStateImplementation(CycleDependentConsumer consumer)
            {
                _ = consumer;
            }

#pragma warning disable CS0067
            public event EventHandler<ThermalStateChangedEventArgs>? Changed;
#pragma warning restore CS0067

            public ThermalStateSnapshot Snapshot => throw new NotSupportedException();

            public ThermalMutationResult ApplyInputs(ThermalInputsSnapshot candidate, ThermalMutationOrigin origin) => throw new NotSupportedException();

            public ThermalMutationResult ApplyInputEdit(ThermalInputEdit edit, ThermalMutationOrigin origin) => throw new NotSupportedException();

            public ThermalMutationResult ResetToDefaults(ThermalMutationOrigin origin) => throw new NotSupportedException();

            public ThermalMutationResult BeginCalculation() => throw new NotSupportedException();

            public ThermalMutationResult CompleteCalculation(ThermalInputsSnapshot calculatedInputs, ThermalResultSnapshot result, string validationMessage) => throw new NotSupportedException();

            public ThermalMutationResult FailCalculation(ThermalInputsSnapshot calculatedInputs, string validationMessage, ThermalResultSnapshot? compatibilityInvalidResult = null) => throw new NotSupportedException();

            public ThermalMutationResult Restore(ThermalInputsSnapshot inputs, ThermalResultSnapshot? savedResult) => throw new NotSupportedException();

            public ThermalMutationResult InvalidateFromClimate(string message) => throw new NotSupportedException();

            public ThermalMutationResult InvalidateFromConstruction(string message) => throw new NotSupportedException();

            public ThermalMutationResult ApplyNeedsRecalculation(string recalculationMessage, ThermalMutationOrigin origin) => throw new NotSupportedException();
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
