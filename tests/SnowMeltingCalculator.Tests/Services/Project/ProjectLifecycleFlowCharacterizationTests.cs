using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 1 Task 3 RED characterization tests for lifecycle flows, repeated
    /// reset/load cycles, and restore failures. Tests use current public seams
    /// (IProjectStateService, ICalculationStateService, ProjectLoadOrchestrator,
    /// ResultsViewModel) and do not reference the future IProjectSession contract.
    /// </summary>
    [TestFixture]
    public class ProjectLifecycleFlowCharacterizationTests
    {
        #region Lifecycle flow

        [Test]
        public async Task LoadProjectDataAsync_Success_ClearsRestoreGuard()
        {
            var projectState = new ProjectStateService();
            var calcState = new CalculationStateService(projectState.Session);
            var viewModel = CreateResultsViewModel(projectState, calculationStateService: calcState);
            var data = CreateMinimalProjectData("SUCCESS", "Success");

            Assert.That(calcState.IsLoadProjectInProgress, Is.False);

            await viewModel.LoadProjectDataAsync(data);

            Assert.That(calcState.IsLoadProjectInProgress, Is.False,
                "Restore guard must be false after a successful load.");
        }

        [Test]
        public async Task LoadProjectDataAsync_TwiceOnSingletonGraph_ReplacesIdentityWithoutStaleState()
        {
            var projectState = new ProjectStateService();
            var viewModel = CreateResultsViewModel(projectState);
            var projectA = CreateMinimalProjectData("PRJ-A", "Object A");
            var projectB = CreateMinimalProjectData("PRJ-B", "Object B");

            await viewModel.LoadProjectDataAsync(projectA);
            Assert.That(projectState.ProjectNumber, Is.EqualTo("PRJ-A"),
                "Sanity: first load must set identity to A.");

            await viewModel.LoadProjectDataAsync(projectB);

            Assert.That(projectState.ProjectNumber, Is.EqualTo("PRJ-B"));
            Assert.That(projectState.ProjectObject, Is.EqualTo("Object B"));
            Assert.That(projectState.IsDirty, Is.False,
                "Second load must leave the project clean.");
        }

        [Test]
        public async Task LoadProjectDataAsync_ThenEdit_MarksDirtyThroughExistingStateService()
        {
            var projectState = new ProjectStateService();
            var viewModel = CreateResultsViewModel(projectState);
            var data = CreateMinimalProjectData("EDIT", "Edit Test");

            await viewModel.LoadProjectDataAsync(data);
            Assert.That(projectState.IsDirty, Is.False,
                "Sanity: load must leave the project clean.");

            var climateVm = GetField<ClimateViewModel>(viewModel, "_climateViewModel");
            climateVm.AirTemperature = -99.0;

            Assert.That(projectState.IsDirty, Is.True,
                "A post-load edit must mark the project dirty through the existing IProjectStateService/IMarkDirtyService seam.");
        }

        #endregion

        #region Repeated reset/load cycles

        [Test]
        public void RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions()
        {
            var markDirtyMock = new Mock<IMarkDirtyService>();
            var circuitsVm = CreateCircuitsViewModel(markDirtyMock.Object);
            var orchestrator = CreateProjectLoadOrchestrator(circuitsVm);

            var collector = circuitsVm.Collectors[0];
            collector.Circuits.Clear();
            var oldCircuit = new CircuitRow { CircuitNumber = 1, CircuitLength = 10 };
            collector.Circuits.Add(oldCircuit);

            var changedCount = 0;
            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(CircuitRow.CircuitLength))
                    changedCount++;
            };
            oldCircuit.PropertyChanged += handler;

            try
            {
                for (var i = 0; i < 3; i++)
                    orchestrator.ResetModules();

                changedCount = 0;
                oldCircuit.CircuitLength = 50;

                Assert.That(changedCount, Is.EqualTo(1),
                    "After repeated reset cycles the old circuit must notify only our test handler once.");

                markDirtyMock.Invocations.Clear();
                var newCircuit = circuitsVm.Collectors[0].Circuits[0];
                newCircuit.CircuitLength = 100;

                markDirtyMock.Verify(
                    m => m.MarkDirty(),
                    Times.Once,
                    "A new circuit after repeated cycles must mark dirty exactly once — no duplicated VM subscriptions.");
            }
            finally
            {
                oldCircuit.PropertyChanged -= handler;
            }
        }

        #endregion

        #region Restore failure / no rollback

        [Test]
        public async Task LoadProjectDataAsync_EarlyRestoreFailure_LeavesPartialStateAndClearsGuard()
        {
            var projectState = new ProjectStateService
            {
                CurrentFilePath = @"C:\old.smc",
                ProjectNumber = "OLD"
            };
            var calcState = new CalculationStateService(projectState.Session);

            var constructionMock = new Mock<IConstructionService>();
            constructionMock
                .Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Throws(new InvalidOperationException("injected early boundary failure"));

            var viewModel = CreateResultsViewModel(
                projectState,
                constructionService: constructionMock.Object,
                calculationStateService: calcState);

            var data = CreateMinimalProjectData("NEW", "New Project");
            data.CustomMaterials = new List<MaterialSnapshot>
            {
                new MaterialSnapshot { Name = "Custom material" }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await viewModel.LoadProjectDataAsync(data));

            Assert.That(ex!.Message, Does.Contain("injected early boundary failure"));
            Assert.That(calcState.IsLoadProjectInProgress, Is.False,
                "Guard must be false even when restore throws.");

            // Characterize current partial-restore semantics: identity is already mutated,
            // path and dirty state are retained from the prior project — no rollback.
            Assert.That(projectState.ProjectNumber, Is.EqualTo("NEW"),
                "ProjectNumber already mutated before the failure and must stay mutated.");
            Assert.That(projectState.ProjectObject, Is.EqualTo("New Project"));
            Assert.That(projectState.CurrentFilePath, Is.EqualTo(@"C:\old.smc"),
                "CurrentFilePath must remain at its pre-failure value; current behavior does not roll it back.");
            Assert.That(projectState.IsDirty, Is.True,
                "Current partial-restore behavior leaves the project dirty when restore fails before MarkClean.");
        }

        [Test]
        public async Task LoadProjectDataAsync_LateRestoreFailure_LeavesPartialStateAndClearsGuard()
        {
            var projectState = new ProjectStateService
            {
                CurrentFilePath = @"C:\old.smc",
                ProjectNumber = "OLD"
            };
            var calcState = new CalculationStateService(projectState.Session);

            var constructionMock = new Mock<IConstructionService>();
            constructionMock
                .Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);
            constructionMock
                .Setup(s => s.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()))
                .Throws(new InvalidOperationException("injected late boundary failure"));

            var viewModel = CreateResultsViewModel(
                projectState,
                constructionService: constructionMock.Object,
                calculationStateService: calcState);

            var data = CreateMinimalProjectData("LATE", "Late Failure");
            data.ClimateData.AirTemperature = -25.0;
            data.ConstructionData.GroundwaterLevel = 0.5;
            data.CustomTemplates = new List<ConstructionTemplate>
            {
                new ConstructionTemplate { Name = "Custom template" }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await viewModel.LoadProjectDataAsync(data));

            Assert.That(ex!.Message, Does.Contain("injected late boundary failure"));
            Assert.That(calcState.IsLoadProjectInProgress, Is.False,
                "Guard must be false after a late restore failure.");

            var climateVm = GetField<ClimateViewModel>(viewModel, "_climateViewModel");

            // Identity and climate are mutated before the late exception; current behavior keeps them.
            Assert.That(projectState.ProjectNumber, Is.EqualTo("LATE"));
            Assert.That(projectState.ProjectObject, Is.EqualTo("Late Failure"));
            Assert.That(climateVm.AirTemperature, Is.EqualTo(-25.0),
                "Climate restore happened before the late failure and is not rolled back.");
            Assert.That(projectState.CurrentFilePath, Is.EqualTo(@"C:\old.smc"),
                "Path is set only after LoadProjectDataAsync returns; it must remain from the prior project.");
            Assert.That(projectState.IsDirty, Is.True,
                "Current partial-restore behavior leaves the project dirty when restore fails before MarkClean.");

            // Thermal and construction data are restored after the failure point, so they retain their defaults.
            Assert.That(calcState.PipeSpacing, Is.EqualTo(200),
                "PipeSpacing default is unchanged because thermal restore happens after the late failure.");
        }

        #endregion

        #region Helpers

        private static ResultsViewModel CreateResultsViewModel(
            ProjectStateService projectStateService,
            IConstructionService? constructionService = null,
            ICalculationStateService? calculationStateService = null)
        {
            var calcState = calculationStateService ?? new CalculationStateService(projectStateService.Session);
            var calcContext = new CalculationContext();

            var climateVm = CreateClimateViewModel(projectStateService);
            var constructionVm = CreateConstructionViewModel(projectStateService);
            var thermalVm = CreateThermalViewModel(projectStateService);
            var circuitsVm = CreateCircuitsViewModel(projectStateService);

            var constructionSvc = constructionService ?? CreateDefaultConstructionService();

            return new ResultsViewModel(
                projectStateService,
                projectStateService.Session,
                projectStateService,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                new Mock<IProjectFileService>().Object,
                calcState,
                new Mock<IMaterialRepository>().Object,
                constructionSvc,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm,
                new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calcState,
                    constructionSvc,
                    calcContext),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calcState,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        private static ProjectLoadOrchestrator CreateProjectLoadOrchestrator(CircuitsViewModel circuitsVm)
        {
            var calcState = new CalculationStateService();
            var calcContext = new CalculationContext();

            return new ProjectLoadOrchestrator(
                CreateClimateViewModel(new Mock<IMarkDirtyService>().Object),
                CreateConstructionViewModel(new Mock<IMarkDirtyService>().Object),
                CreateThermalViewModel(new Mock<IMarkDirtyService>().Object),
                circuitsVm,
                calcState,
                CreateDefaultConstructionService(),
                calcContext);
        }

        private static ClimateViewModel CreateClimateViewModel(IMarkDirtyService markDirtyService)
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(new List<CityInfo>());
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns(ClimateZone.Zone_M15);

            return new ClimateViewModel(
                climateServiceMock.Object,
                new ClimateData(),
                new ClimateValidator(),
                markDirtyService,
                new CalculationContext());
        }

        private static ConstructionViewModel CreateConstructionViewModel(IMarkDirtyService markDirtyService)
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            };

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                markDirtyService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object);
        }

        private static ThermalViewModel CreateThermalViewModel(
            IMarkDirtyService markDirtyService,
            IThermalCalculator? thermalCalculator = null)
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            return new ThermalViewModel(
                thermalCalculator ?? new Mock<IThermalCalculator>().Object,
                climateData,
                constructionData,
                new CalculationStateService(),
                new CalculationContext(),
                new ThermalValidator(new ThermalCalculator(), climateData, constructionData),
                new ThermalResultValidator(),
                markDirtyService);
        }

        private static CircuitsViewModel CreateCircuitsViewModel(IMarkDirtyService markDirtyService)
        {
            var calculatorMock = new Mock<ICircuitsCalculator>();
            calculatorMock.Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>())).Returns(new CollectorSummary());
            calculatorMock.Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>())).Returns(new CircuitTemperatureResult());
            calculatorMock.Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>())).Returns((List<CircuitRow> circuits, ValveType _) => circuits);

            var glycolMock = new Mock<IGlycolDataService>();
            glycolMock.Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });

            var selectorMock = new Mock<ICollectorTypeSelector>();
            selectorMock.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>())).Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                new CalculationStateService(),
                new Mock<ICircuitsValidator>().Object,
                selectorMock.Object,
                new CalculationContext(),
                markDirtyService);
        }

        private static IConstructionService CreateDefaultConstructionService()
        {
            var mock = new Mock<IConstructionService>();
            mock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>())).Returns(Task.CompletedTask);
            mock.Setup(s => s.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>())).Returns(Task.CompletedTask);
            return mock.Object;
        }

        private static ProjectData CreateMinimalProjectData(string projectNumber, string projectObject)
        {
            return new ProjectData
            {
                ProjectNumber = projectNumber,
                ProjectObject = projectObject,
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData
                {
                    Result = new ThermalResultProjectData { IsValid = true }
                },
                HydraulicsData = new HydraulicsProjectData()
            };
        }

        private static T GetField<T>(object instance, string fieldName) where T : class
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)(field?.GetValue(instance) ?? throw new InvalidOperationException($"Field {fieldName} not found."));
        }

        #endregion
    }
}
