using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Services.Reports.Calculation;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Shell;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Tests.Services.Project;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Тесты главного ViewModel и обработчика закрытия окна
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelTests
    {
        private ProjectStateService _projectStateService = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<IProjectFileService> _projectFileServiceMock = null!;
        private CalculationContext _calculationContext = null!;
        private MainViewModel _viewModel = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (System.Windows.Application.Current == null)
            {
                new Application();
            }
        }

        [SetUp]
        public void SetUp()
        {
            ResetAppSettingsSingleton();

            _projectStateService = new ProjectStateService();
            _dialogServiceMock = new Mock<IDialogService>();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            _projectFileServiceMock = new Mock<IProjectFileService>();
            _calculationContext = new CalculationContext();

            var climateVm = CreateClimateViewModel(_projectStateService);
            var constructionVm = CreateConstructionViewModel(
                _projectStateService,
                _calculationContext,
                out var constructionDefaultStateInitializer);
            var thermalVm = CreateThermalViewModel(_projectStateService);
            var circuitsVm = CreateCircuitsViewModel(_projectStateService);
            var resultsVm = CreateResultsViewModel(_projectStateService, _projectFileServiceMock.Object, _dialogServiceMock.Object);

            _viewModel = new MainViewModel(
                climateVm,
                thermalVm,
                constructionVm,
                circuitsVm,
                resultsVm,
                _calculationStateServiceMock.Object,
                _projectStateService,
                _dialogServiceMock.Object,
                _calculationContext,
                _projectStateService.Session,
                constructionDefaultStateInitializer);
        }

        [TearDown]
        public void TearDown()
        {
            ResetAppSettingsSingleton();
        }

        /// <summary>
        /// Сбрасывает статический singleton <see cref="AppSettings"/> и удаляет файл настроек,
        /// чтобы тесты были детерминированы относительно состояния свёрнутой боковой панели.
        /// </summary>
        private static void ResetAppSettingsSingleton()
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnowMeltingCalculator",
                "settings.json");
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            var field = typeof(AppSettings).GetField("_instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        #region NewCalculationCommand

        [Test]
        public async Task NewCalculation_WhenClean_DoesNotShowDialog_AndResets()
        {
            _projectStateService.MarkClean();
            _calculationContext.UpdateClimate(new ClimateData { AirTemperature = -20 });

            await _viewModel.NewCalculationCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButtons>(), It.IsAny<DialogIcon>()),
                Times.Never);
            Assert.That(_calculationContext.Climate, Is.Null);
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task NewCalculation_WhenDirtyAndCancel_DoesNotReset()
        {
            _projectStateService.MarkDirty();
            _calculationContext.UpdateClimate(new ClimateData { AirTemperature = -20 });
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.Cancel);

            await _viewModel.NewCalculationCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question),
                Times.Once);
            Assert.That(_projectStateService.IsDirty, Is.True);
            Assert.That(_calculationContext.Climate, Is.Not.Null);
        }

        [Test]
        public async Task NewCalculation_WhenDirtyAndNo_Resets()
        {
            _projectStateService.MarkDirty();
            _calculationContext.UpdateClimate(new ClimateData { AirTemperature = -20 });
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.No);

            await _viewModel.NewCalculationCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question),
                Times.Once);
            Assert.That(_calculationContext.Climate, Is.Null);
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task NewCalculation_WhenDirtyAndYesAndSaveSucceeds_Resets()
        {
            _projectStateService.MarkDirty();
            _calculationContext.UpdateClimate(new ClimateData { AirTemperature = -20 });
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.Yes);
            _dialogServiceMock
                .Setup(d => d.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(@"C:\temp\project.smc");
            _projectFileServiceMock
                .Setup(p => p.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Success(null));

            await _viewModel.NewCalculationCommand.ExecuteAsync(null);

            _projectFileServiceMock.Verify(p => p.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(_projectStateService.IsDirty, Is.False);
            Assert.That(_calculationContext.Climate, Is.Null);
        }

        [Test]
        public async Task NewCalculation_WhenDirtyAndYesButSaveCancelled_DoesNotReset()
        {
            _projectStateService.MarkDirty();
            _calculationContext.UpdateClimate(new ClimateData { AirTemperature = -20 });
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.Yes);
            await _viewModel.NewCalculationCommand.ExecuteAsync(null);

            _projectFileServiceMock.Verify(p => p.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(_projectStateService.IsDirty, Is.True);
            Assert.That(_calculationContext.Climate, Is.Not.Null);
        }

        [Test]
        public async Task NewCalculation_AfterLoadedHydraulics_ClearsResultsHydraulicSummaryCards()
        {
            // Arrange: загружаем в ResultsViewModel проект с двумя коллекторами,
            // который заполняет HydraulicSummaryCards и проставляет ненулевые
            // legacy-скаляры (TotalCircuits / TotalFlowRate / MaxPressureLoss).
            // LoadProjectDataAsync вызывает MarkClean в конце, поэтому NewCalculation
            // пойдёт по «clean»-ветке без диалога и сразу выполнит PerformNewCalculationReset.
            var projectData = new ProjectData
            {
                ProjectNumber = "P-NewCalcClear",
                ProjectObject = "New Calc Clear Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            CollectorNumber = 1,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 2, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 2,
                                TotalPipeLength = 220,
                                TotalPower = 12000,
                                TotalFlowRate = 600.5,
                                PressureLoss_Operating_Pa = 18000,
                                PressureLoss_Cold_Pa = 60000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        },
                        new CollectorProjectData
                        {
                            CollectorNumber = 2,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 100, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 1,
                                TotalPipeLength = 100,
                                TotalPower = 5000,
                                TotalFlowRate = 250.25,
                                PressureLoss_Operating_Pa = 9000,
                                PressureLoss_Cold_Pa = 30000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };

            var resultsVm = _viewModel.ResultsViewModel;

            // Reflection-доступ к HydraulicSummaryCards, чтобы тест оставался
            // compile-clean независимо от точной сигнатуры read-model.
            var cardsProperty = typeof(ResultsViewModel).GetProperty(
                "HydraulicSummaryCards",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(cardsProperty, Is.Not.Null,
                "ResultsViewModel должен экспонировать публичное свойство 'HydraulicSummaryCards'.");

            // Act 1: загружаем проект и обновляем гидравлические данные,
            // как это делает UI при переходе на вкладку Results.
            await resultsVm.LoadProjectDataAsync(projectData);
            resultsVm.LoadHydraulicsDataOnNavigate();

            // Assert 1: после загрузки коллекция карточек непустая и legacy-скаляры
            // отражают данные выбранного коллектора. Sanity-проверка, что мы
            // действительно «зашли» в состояние с заполненной read-model.
            var cardsBefore = ((System.Collections.IEnumerable)cardsProperty!.GetValue(resultsVm)!)
                .Cast<object>().ToList();
            Assert.That(cardsBefore.Count, Is.EqualTo(2),
                "Sanity: после загрузки проекта HydraulicSummaryCards должен содержать 2 карточки.");
            Assert.That(resultsVm.TotalCircuits, Is.GreaterThan(0),
                "Sanity: TotalCircuits должен быть > 0 после загрузки.");
            Assert.That(resultsVm.TotalFlowRate, Is.GreaterThan(0.0),
                "Sanity: TotalFlowRate должен быть > 0 после загрузки.");
            Assert.That(resultsVm.MaxPressureLoss, Is.GreaterThan(0.0),
                "Sanity: MaxPressureLoss должен быть > 0 после загрузки.");

            // Sanity: после LoadProjectDataAsync проект помечается Clean,
            // иначе NewCalculation пошёл бы по dialog-ветке и тест потерял бы детерминированность.
            Assert.That(_projectStateService.IsDirty, Is.False,
                "Sanity: после LoadProjectDataAsync проект должен быть Clean.");

            // Act 2: NewCalculation (clean-ветка → без диалога → PerformNewCalculationReset).
            await _viewModel.NewCalculationCommand.ExecuteAsync(null);

            // Assert 2: после NewCalculation коллекция карточек должна быть пустой,
            // и все legacy-скаляры должны быть нулевыми. Если хоть один из этих
            // инвариантов нарушен — значит PerformNewCalculationReset оставил
            // в ResultsViewModel «залипшее» состояние из предыдущего расчёта.
            var cardsAfter = ((System.Collections.IEnumerable)cardsProperty.GetValue(resultsVm)!)
                .Cast<object>().ToList();
            Assert.That(cardsAfter, Is.Empty,
                "HydraulicSummaryCards должен быть пустым после NewCalculation.");
            Assert.That(resultsVm.TotalCircuits, Is.EqualTo(0),
                "TotalCircuits должен быть 0 после NewCalculation.");
            Assert.That(resultsVm.TotalFlowRate, Is.EqualTo(0.0),
                "TotalFlowRate должен быть 0 после NewCalculation.");
            Assert.That(resultsVm.MaxPressureLoss, Is.EqualTo(0.0),
                "MaxPressureLoss должен быть 0 после NewCalculation.");
        }

        [Test]
        public async Task NewCalculation_ReplacesEditedConstructionWithCanonicalDefaultsAndStaysClean()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            using var provider = services.BuildServiceProvider();
            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            var constructionViewModel = provider.GetRequiredService<ConstructionViewModel>();
            var session = provider.GetRequiredService<IProjectSession>();
            var state = provider.GetRequiredService<IProjectSessionConstructionState>();
            var context = provider.GetRequiredService<CalculationContext>();
            var materials = provider.GetRequiredService<IMaterialRepository>();

            await constructionViewModel.InitializeCommand.ExecuteAsync(null);
            var catalog = materials.GetAllMaterials().ToDictionary(material => material.Id);
            var staleLayerId = Guid.NewGuid();
            var customSnapshot = new ConstructionStateSnapshot(
                2.0,
                true,
                new[]
                {
                    new ConstructionLayerSnapshot(
                        staleLayerId, 5, catalog[5].Name, 333.0, catalog[5].LambdaA, false,
                        LayerPosition.AbovePipe, 0)
                },
                Array.Empty<ConstructionLayerSnapshot>());
            Assert.That(state.ApplySnapshot(customSnapshot, ConstructionMutationOrigin.User).IsChanged, Is.True);
            session.MarkClean();

            var origins = new List<ConstructionMutationOrigin>();
            var constructionPublications = 0;
            state.Changed += (_, args) => origins.Add(args.Origin);
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    constructionPublications++;
                }
            };

            await mainViewModel.NewCalculationCommand.ExecuteAsync(null);

            CanonicalDefaultConstructionLifecycleTests.AssertDefaultSnapshot(state.Snapshot, catalog);
            CanonicalDefaultConstructionLifecycleTests.AssertAdapterParity(constructionViewModel, state.Snapshot);
            Assert.Multiple(() =>
            {
                Assert.That(origins, Is.EqualTo(new[] { ConstructionMutationOrigin.Reset }));
                Assert.That(state.Snapshot.LayersAbovePipe.Concat(state.Snapshot.LayersBelowPipe)
                    .Any(layer => layer.Id == staleLayerId), Is.False);
                Assert.That(session.IsDirty, Is.False);
                Assert.That(constructionPublications, Is.Zero);
            });
            CanonicalDefaultConstructionLifecycleTests.AssertDefaultProjectData(
                mainViewModel.ResultsViewModel.SaveCurrentProject(), catalog);
        }

        [Test]
        public async Task NewCalculation_ChangedClimateReset_SynchronizesOnceWithoutCompatibilityThermalOrDirty()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            using var provider = services.BuildServiceProvider();
            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            var climateViewModel = provider.GetRequiredService<ClimateViewModel>();
            var session = provider.GetRequiredService<IProjectSession>();
            var state = session.ClimateState;
            var climateData = (ClimateData)provider.GetRequiredService<IClimateData>();
            var context = provider.GetRequiredService<CalculationContext>();
            var calculationState = provider.GetRequiredService<ICalculationStateService>();
            var thermalViewModel = provider.GetRequiredService<ThermalViewModel>();
            var constructionViewModel = provider.GetRequiredService<ConstructionViewModel>();

            await constructionViewModel.InitializeCommand.ExecuteAsync(null);

            state.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
                ClimateMutationOrigin.SystemApply);
            thermalViewModel.LoadResult(new ThermalCalculationResult { IsValid = true });
            session.MarkClean();

            var completions = 0;
            var compatibilityEvents = 0;
            var contextUpdates = 0;
            var thermalStates = 0;
            state.Changed += (_, _) => completions++;
            climateData.DataChanged += (_, _) => compatibilityEvents++;
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Climate))
                {
                    contextUpdates++;
                }
            };
            calculationState.StateChanged += (_, args) =>
            {
                if (args.Module == "Thermal")
                {
                    thermalStates++;
                }
            };

            await mainViewModel.NewCalculationCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(state.Snapshot.AirTemperature, Is.EqualTo(-15.0));
                Assert.That(climateViewModel.AirTemperature, Is.EqualTo(-15.0));
                Assert.That(completions, Is.EqualTo(1));
                Assert.That(contextUpdates, Is.EqualTo(1));
                Assert.That(compatibilityEvents, Is.Zero);
                Assert.That(thermalStates, Is.Zero);
                Assert.That(session.IsDirty, Is.False);
            });
        }

        #endregion

        #region Navigation

        [Test]
        public void CurrentNavigationTarget_DefaultsToClimate()
        {
            Assert.That(_viewModel.CurrentNavigationTarget, Is.EqualTo(NavigationTarget.Climate));
        }

        [Test]
        public void SelectedMenuItem_WhenSetToEachItem_UpdatesCurrentNavigationTarget()
        {
            foreach (var menuItem in _viewModel.MenuItems)
            {
                _viewModel.SelectedMenuItem = menuItem;

                Assert.That(_viewModel.CurrentNavigationTarget, Is.EqualTo(menuItem.Target));
            }
        }

        [TestCase(NavigationTarget.Climate, "Климатические данные")]
        [TestCase(NavigationTarget.Construction, "Конструкция")]
        [TestCase(NavigationTarget.Thermal, "Тепловой расчёт")]
        [TestCase(NavigationTarget.Hydraulics, "Гидравлический расчёт")]
        [TestCase(NavigationTarget.Results, "Результаты расчёта")]
        public void SelectedMenuItem_WhenSetToEachTarget_UpdatesCurrentTitle(NavigationTarget target, string expectedTitle)
        {
            var menuItem = _viewModel.MenuItems.Single(item => item.Target == target);

            _viewModel.SelectedMenuItem = menuItem;

            Assert.That(_viewModel.CurrentTitle, Is.EqualTo(expectedTitle));
        }

        [Test]
        public void SelectedMenuItem_WhenSetToResults_DoesNotLoadHydraulicsDataOnNavigate()
        {
            var resultsMenuItem = _viewModel.MenuItems.Single(item => item.Target == NavigationTarget.Results);

            _viewModel.SelectedMenuItem = resultsMenuItem;

            var cardsProperty = typeof(ResultsViewModel).GetProperty(
                "HydraulicSummaryCards",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(cardsProperty, Is.Not.Null,
                "ResultsViewModel должен экспонировать публичное свойство 'HydraulicSummaryCards'.");
            var cards = ((System.Collections.IEnumerable)cardsProperty!.GetValue(_viewModel.ResultsViewModel)!)
                .Cast<object>()
                .ToList();

            Assert.That(cards, Is.Empty);
        }

        [Test]
        public void MainViewModel_DoesNotExposeCurrentViewProperty()
        {
            var currentViewProperty = typeof(MainViewModel).GetProperty(
                "CurrentView",
                BindingFlags.Public | BindingFlags.Instance);

            Assert.That(currentViewProperty, Is.Null);
        }

        #endregion

        #region WindowTitle

        [Test]
        public void WindowTitle_DirtyNoPath_ShowsAsteriskAndNewCalculation()
        {
            _projectStateService.MarkDirty();
            _projectStateService.CurrentFilePath = null;

            Assert.That(_viewModel.WindowTitle, Is.EqualTo("*Новый расчёт — Калькулятор снеготаяния REHAU"));
        }

        [Test]
        public void WindowTitle_DirtyWithPath_ShowsAsteriskAndFileName()
        {
            _projectStateService.MarkDirty();
            _projectStateService.CurrentFilePath = @"C:\temp\project.smc";

            Assert.That(_viewModel.WindowTitle, Is.EqualTo("*project.smc — Калькулятор снеготаяния REHAU"));
        }

        [Test]
        public void WindowTitle_CleanWithPath_ShowsFileName()
        {
            _projectStateService.MarkClean();
            _projectStateService.CurrentFilePath = @"C:\temp\project.smc";

            Assert.That(_viewModel.WindowTitle, Is.EqualTo("project.smc — Калькулятор снеготаяния REHAU"));
        }

        [Test]
        public void WindowTitle_CleanNoPath_ShowsDefaultTitle()
        {
            _projectStateService.MarkClean();
            _projectStateService.CurrentFilePath = null;

            Assert.That(_viewModel.WindowTitle, Is.EqualTo("Калькулятор снеготаяния REHAU"));
        }

        #endregion

        #region Closing

        [Test]
        public async Task Closing_WhenDirtyAndCancel_SetsCancelTrue()
        {
            _projectStateService.MarkDirty();
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.Cancel);

            var args = await InvokeClosingAsync(_dialogServiceMock.Object, _projectStateService, _viewModel);

            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question),
                Times.Once);
            Assert.That(args.Cancel, Is.True);
        }

        [Test]
        public async Task Closing_WhenDirtyAndNo_DoesNotCancel()
        {
            _projectStateService.MarkDirty();
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.No);

            var args = await InvokeClosingAsync(_dialogServiceMock.Object, _projectStateService, _viewModel);

            Assert.That(args.Cancel, Is.False);
        }

        [Test]
        public async Task Closing_WhenDirtyAndYes_SetsCancelTrueAndReinvokesClose()
        {
            _projectStateService.MarkDirty();
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel, DialogIcon.Question))
                .Returns(DialogResult.Yes);
            _dialogServiceMock
                .Setup(d => d.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(@"C:\temp\project.smc");
            _projectFileServiceMock
                .Setup(p => p.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Success(null));

            var window = CreateUninitializedMainWindow();
            SetField(window, "_projectStateService", _projectStateService);
            SetField(window, "_dialogService", _dialogServiceMock.Object);
            SetField(window, "_viewModel", _viewModel);

            var args1 = await InvokeClosingAsync(window);
            Assert.That(args1.Cancel, Is.True);
            Assert.That(GetField<bool>(window, "_isClosingAfterSave"), Is.True);

            var args2 = await InvokeClosingAsync(window);
            Assert.That(args2.Cancel, Is.False);
            Assert.That(GetField<bool>(window, "_isClosingAfterSave"), Is.False);
        }

        #endregion

        #region Sidebar

        [Test]
        public void IsSidebarCollapsed_DefaultsToFalse_OnFreshAppSettings()
        {
            // Sanity: после SetUp singleton AppSettings сброшен, MainViewModel создан заново
            // и должен подхватить значение по умолчанию (false) из настроек.
            Assert.That(_viewModel.IsSidebarCollapsed, Is.False);
            Assert.That(_viewModel.IsSidebarExpanded, Is.True);
        }

        [Test]
        public void IsSidebarCollapsed_WhenSetToTrue_ChangesValue_AndMarksSidebarCollapsed()
        {
            _viewModel.IsSidebarCollapsed = true;

            Assert.That(_viewModel.IsSidebarCollapsed, Is.True);
            Assert.That(_viewModel.IsSidebarExpanded, Is.False,
                "IsSidebarExpanded — обратное свойство, должно стать false при свёрнутой панели.");
        }

        [Test]
        public void IsSidebarCollapsed_WhenSetToTrue_PersistsToAppSettings()
        {
            // Capture текущее состояние singleton, чтобы не оставлять side-effect на другие тесты
            var capturedInitial = AppSettings.Instance.IsSidebarCollapsed;

            try
            {
                _viewModel.IsSidebarCollapsed = true;

                Assert.That(AppSettings.Instance.IsSidebarCollapsed, Is.True,
                    "Свёрнутое состояние боковой панели должно сохраняться в AppSettings.Instance.");
            }
            finally
            {
                AppSettings.Instance.IsSidebarCollapsed = capturedInitial;
            }
        }

        [Test]
        public void IsSidebarCollapsed_WhenToggled_NotifiesIsSidebarExpanded()
        {
            var expandedNotifications = new List<string?>();
            _viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.IsSidebarExpanded))
                {
                    expandedNotifications.Add(args.PropertyName);
                }
            };

            _viewModel.IsSidebarCollapsed = !_viewModel.IsSidebarCollapsed;

            Assert.That(expandedNotifications, Is.Not.Empty,
                "Сеттер IsSidebarCollapsed должен уведомлять об изменении IsSidebarExpanded для XAML-триггеров.");
        }

        [Test]
        public void ToggleSidebarCommand_FlipsIsSidebarCollapsed()
        {
            var initial = _viewModel.IsSidebarCollapsed;

            _viewModel.ToggleSidebarCommand.Execute(null);

            Assert.That(_viewModel.IsSidebarCollapsed, Is.EqualTo(!initial));
        }

        [Test]
        public void MainViewModel_Constructor_ReadsInitialCollapseStateFromAppSettings()
        {
            // Arrange: подготовим AppSettings со свёрнутым состоянием
            AppSettings.Instance.IsSidebarCollapsed = true;

            try
            {
                var constructionViewModel = CreateConstructionViewModel(
                    _projectStateService,
                    _calculationContext,
                    out var constructionDefaultStateInitializer);
                var viewModel = new MainViewModel(
                    CreateClimateViewModel(_projectStateService),
                    CreateThermalViewModel(_projectStateService),
                    constructionViewModel,
                    CreateCircuitsViewModel(_projectStateService),
                    CreateResultsViewModel(_projectStateService, _projectFileServiceMock.Object, _dialogServiceMock.Object),
                    _calculationStateServiceMock.Object,
                    _projectStateService,
                    _dialogServiceMock.Object,
                    _calculationContext,
                    _projectStateService.Session,
                    constructionDefaultStateInitializer);

                Assert.That(viewModel.IsSidebarCollapsed, Is.True,
                    "MainViewModel должен подхватывать сохранённое состояние свёрнутой панели при конструировании.");
                Assert.That(viewModel.IsSidebarExpanded, Is.False);
            }
            finally
            {
                // TearDown повторно сбросит singleton, но на всякий случай вернём исходное значение
                AppSettings.Instance.IsSidebarCollapsed = false;
            }
        }

        #endregion

        #region Helpers

        private async Task<CancelEventArgs> InvokeClosingAsync(IDialogService dialogService, IProjectStateService projectStateService, MainViewModel viewModel)
        {
            var window = CreateUninitializedMainWindow();
            SetField(window, "_projectStateService", projectStateService);
            SetField(window, "_dialogService", dialogService);
            SetField(window, "_viewModel", viewModel);
            return await InvokeClosingAsync(window);
        }

        private async Task<CancelEventArgs> InvokeClosingAsync(MainWindow window)
        {
            var method = typeof(MainWindow).GetMethod("MainWindow_ClosingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var args = new CancelEventArgs();
            var task = (Task?)method?.Invoke(window, new object[] { args });
            if (task != null)
            {
                await task;
            }
            return args;
        }

        private static MainWindow CreateUninitializedMainWindow()
        {
#pragma warning disable SYSLIB0050 // Formatter-based serialization is obsolete
            return (MainWindow)FormatterServices.GetUninitializedObject(typeof(MainWindow));
#pragma warning restore SYSLIB0050
        }

        private static void SetField(object target, string fieldName, object? value)
        {
            var field = typeof(MainWindow).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            var field = typeof(MainWindow).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)(field?.GetValue(target) ?? default(T)!);
        }

        private static ClimateViewModel CreateClimateViewModel(ProjectStateService projectStateService)
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns((double t, bool high) =>
                {
                    if (high) return ClimateZone.Zone_M20_Plus;
                    if (t >= -27) return ClimateZone.Zone_M10;
                    if (t > -37) return ClimateZone.Zone_M15;
                    return ClimateZone.Zone_M20;
                });

            return new ClimateViewModel(
                climateServiceMock.Object,
                new ClimateData(),
                new ClimateValidator(),
                projectStateService,
                new CalculationContext());
        }

        private static ConstructionViewModel CreateConstructionViewModel(ProjectStateService projectStateService)
        {
            return CreateConstructionViewModel(
                projectStateService,
                new CalculationContext(),
                out _);
        }

        private static ConstructionViewModel CreateConstructionViewModel(
            ProjectStateService projectStateService,
            CalculationContext calculationContext,
            out ConstructionDefaultStateInitializer constructionDefaultStateInitializer)
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 },
                new Material { Id = 6, Name = "Base", LambdaA = 1.2, LambdaB = 1.3 },
                new Material { Id = 10, Name = "Insulation", LambdaA = 0.04, LambdaB = 0.05 },
                new Material { Id = 13, Name = "Fill", LambdaA = 0.7, LambdaB = 0.8 }
            };

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materials.FirstOrDefault(material => material.Id == id));
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(materials);
            constructionDefaultStateInitializer = new ConstructionDefaultStateInitializer(
                materialRepositoryMock.Object,
                projectStateService.Session.ConstructionState);

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                calculationContext,
                new ConstructionValidator(),
                new ConstructionModel(),
                projectStateService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                projectStateService.Session.ConstructionState,
                constructionDefaultStateInitializer);
        }

        private static ThermalViewModel CreateThermalViewModel(ProjectStateService projectStateService)
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            return new ThermalViewModel(
                new Mock<IThermalCalculator>().Object,
                climateData,
                constructionData,
                new CalculationStateService(),
                new CalculationContext(),
                new ThermalValidator(new ThermalCalculator(), climateData, constructionData),
                new ThermalResultValidator(),
                projectStateService);
        }

        private static CircuitsViewModel CreateCircuitsViewModel(ProjectStateService projectStateService)
        {
            var calculatorMock = new Mock<ICircuitsCalculator>();
            calculatorMock.Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>())).Returns(new CollectorSummary());
            calculatorMock.Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>())).Returns(new CircuitTemperatureResult());
            calculatorMock.Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>())).Returns((List<CircuitRow> circuits, ValveType _) => circuits);

            var glycolMock = new Mock<IGlycolDataService>();
            glycolMock.Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>())).Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });

            var selectorMock = new Mock<ICollectorTypeSelector>();
            selectorMock.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>())).Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

            var calculationStateService = new CalculationStateService();
            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationStateService, calculationContext);
            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                 calculationStateService,
                new Mock<ICircuitsValidator>().Object,
                selectorMock.Object,
                  calculationContext,
                  hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session);
        }

        private static ResultsViewModel CreateResultsViewModel(ProjectStateService projectStateService, IProjectFileService projectFileService, IDialogService dialogService)
        {
            var climateVm = CreateClimateViewModel(projectStateService);
            var constructionVm = CreateConstructionViewModel(projectStateService);
            var thermalVm = CreateThermalViewModel(projectStateService);
            var circuitsVm = CreateCircuitsViewModel(projectStateService);

            var materials = Material.GetDefaultMaterials();
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materials.FirstOrDefault(material => material.Id == id));
            var constructionDefaultStateInitializer = new ConstructionDefaultStateInitializer(
                materialRepositoryMock.Object,
                projectStateService.Session.ConstructionState);

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            var calculationStateService = new CalculationStateService(projectStateService.Session);
            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                projectStateService,
                projectStateService.Session,
                projectStateService,
                dialogService,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                projectFileService,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm,
                new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calculationStateService,
                    constructionServiceMock.Object,
                    calculationContext,
                    projectStateService.Session,
                    constructionDefaultStateInitializer),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        #endregion
    }
}
