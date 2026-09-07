using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
using SnowMeltingCalculator.Repositories.Construction;
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
using SnowMeltingCalculator.ViewModels.Thermal;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    internal static class ResultsViewModelTestHelpers
    {
        public static ResultsViewModel CreateResultsViewModel(
            ProjectStateService projectStateService,
            CircuitsViewModel circuitsVm)
        {
            return CreateResultsViewModel(
                projectStateService,
                circuitsVm,
                out _,
                out _,
                out _);
        }

        public static ResultsViewModel CreateResultsViewModel(
            ProjectStateService projectStateService,
            CircuitsViewModel circuitsVm,
            out ClimateViewModel climateVmOut,
            out ConstructionViewModel constructionVmOut,
            out ThermalViewModel thermalVmOut)
        {
            var materials = Material.GetDefaultMaterials().ToList();
            var materialsById = materials.ToDictionary(material => material.Id);
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materialsById.GetValueOrDefault(id));

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            var calculationStateService = new CalculationStateService(projectStateService.Session);
            var calculationContext = new CalculationContext();
            var climateVm = CreateClimateViewModel(projectStateService.Session);
            var constructionVm = CreateConstructionViewModel(
                projectStateService.Session,
                materialRepositoryMock.Object);
            var thermalVm = CreateThermalViewModel();
            climateVmOut = climateVm;
            constructionVmOut = constructionVm;
            thermalVmOut = thermalVm;
            var constructionDefaultStateInitializer = CreateDefaultConstructionInitializer(
                projectStateService.Session,
                materialRepositoryMock.Object);

            return new ResultsViewModel(
                projectStateService.Session,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<IProjectFileService>().Object,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
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

        public static ClimateViewModel CreateClimateViewModel()
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.GetCityByName("Тестовый город")).Returns(new CityInfo
            {
                Name = "Тестовый город",
                Region = "Тестовый регион",
                T5Days092 = -25,
                WindAvgTempLe8 = 3,
                Humidity15hCold = 70
            });
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns(ClimateZone.Zone_M10);

            return new ClimateViewModel(
                climateServiceMock.Object,
                new ClimateData(),
                new ClimateValidator(),
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());
        }

        public static ClimateViewModel CreateClimateViewModel(IProjectSession projectSession)
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.GetCityByName("Тестовый город")).Returns(new CityInfo
            {
                Name = "Тестовый город",
                Region = "Тестовый регион",
                T5Days092 = -25,
                WindAvgTempLe8 = 3,
                Humidity15hCold = 70
            });
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns(ClimateZone.Zone_M10);

            return new ClimateViewModel(
                climateServiceMock.Object,
                new ClimateData(),
                new ClimateValidator(),
                projectSession);
        }

        public static ConstructionViewModel CreateConstructionViewModel()
        {
            return CreateConstructionViewModel(null);
        }

        public static ConstructionViewModel CreateConstructionViewModel(IProjectSession? projectSession)
        {
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            return CreateConstructionViewModel(projectSession, materialRepositoryMock.Object);
        }

        public static ConstructionDefaultStateInitializer CreateDefaultConstructionInitializer(
            IProjectSession projectSession)
        {
            var materialsById = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(repository => repository.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materialsById.GetValueOrDefault(id));

            return CreateDefaultConstructionInitializer(projectSession, materialRepositoryMock.Object);
        }

        private static ConstructionDefaultStateInitializer CreateDefaultConstructionInitializer(
            IProjectSession projectSession,
            IMaterialRepository materialRepository)
        {
            return new ConstructionDefaultStateInitializer(
                materialRepository,
                projectSession.ConstructionState);
        }

        private static ConstructionViewModel CreateConstructionViewModel(
            IProjectSession? projectSession,
            IMaterialRepository materialRepository)
        {
            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            var calculationContext = new CalculationContext();
            projectSession ??= new ProjectSession(calculationContext: calculationContext);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepository,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(projectSession),
                calculationContext,
                new ConstructionValidator(),
                new ConstructionModel(),
                new Mock<IMarkDirtyService>().Object,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                projectSession.ConstructionState,
                new ConstructionDefaultStateInitializer(materialRepository, projectSession.ConstructionState));
        }

        public static ThermalViewModel CreateThermalViewModel()
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
                new Mock<IMarkDirtyService>().Object);
        }

        public static CircuitsViewModel CreateCircuitsViewModelWithCollectors(params CollectorData[] collectors)
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
            selectorMock.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>()))
                .Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

            var calculationStateService = new CalculationStateService();
            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationStateService, calculationContext);
            var viewModel = new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                 calculationStateService,
                new Mock<ICircuitsValidator>().Object,
                selectorMock.Object,
                  calculationContext,
                  hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session);

            viewModel.Collectors.Clear();
            foreach (var collector in collectors)
            {
                viewModel.Collectors.Add(collector);
            }

            return viewModel;
        }

        public static CollectorData CreateCollector(int collectorNumber, ValveType valveType, int circuitCount)
        {
            var collector = new CollectorData(collectorNumber)
            {
                ValveType = valveType,
                CollectorType = valveType switch
                {
                    ValveType.IV_1_25 => "IV 1¼\" (2-12 контуров)",
                    ValveType.IV_1_5 => "IV 1½\" (2-12 контуров)",
                    _ => "HKV-D (2-12 контуров)"
                },
                Circuits = new ObservableCollection<CircuitRow>(
                    Enumerable.Range(1, circuitCount).Select(number => new CircuitRow
                    {
                        CircuitNumber = number,
                        CircuitLength = 50,
                        SupplyLength = 10,
                        SupplySpacing_cm = 5,
                        SupplyHeatPercent = 10,
                        PipeSpacing_cm = 20
                    }))
            };

            return collector;
        }

        public static void ReplaceCollectors(CircuitsViewModel viewModel, params CollectorData[] collectors)
        {
            viewModel.Collectors.Clear();
            foreach (var collector in collectors)
            {
                viewModel.Collectors.Add(collector);
            }
        }

        /// <summary>
        /// Phase 8: зеркалит сеяние коллекторов в канонический HydraulicsState
        /// (готовность/KPI читаются из канона, карточки оборудования — из VM).
        /// </summary>
        public static void ReplaceCollectorsCanonical(
            IProjectSession session,
            CircuitsViewModel viewModel,
            params CollectorData[] collectors)
        {
            ReplaceCollectors(viewModel, collectors);

            session.HydraulicsState.ReplaceCollectors(
                collectors.Where(c => c != null).Select(c => new HydraulicCollectorSnapshot(
                    c.CollectorNumber,
                    c.CollectorType,
                    c.ValveType,
                    c.Circuits?.Select(row => new HydraulicCircuitSnapshot(
                        row.CircuitNumber,
                        row.CircuitLength,
                        row.SupplyLength,
                        row.SupplySpacing_cm,
                        row.SupplyHeatPercent,
                        row.PipeSpacing_cm)) ?? System.Array.Empty<HydraulicCircuitSnapshot>(),
                    c.Summary == null
                        ? null
                        : new HydraulicCollectorSummarySnapshot(
                            c.Summary.CircuitCount,
                            c.Summary.TotalPipeLength,
                            c.Summary.TotalPower,
                            c.Summary.TotalFlowRate,
                            c.Summary.PressureLoss_Operating_Pa,
                            c.Summary.PressureLoss_Cold_Pa,
                            c.Summary.Kv,
                            c.Summary.CollectorType))),
                HydraulicsMutationOrigin.Calculation);
        }

        public static Task LoadReadyModulesAsync(ResultsViewModel viewModel)
        {
            return viewModel.LoadProjectDataAsync(CreateReadyProjectData());
        }

        public static ProjectData CreateReadyProjectData()
        {
            return new ProjectData
            {
                ProjectNumber = "P-CollectorEquipmentItems",
                ProjectObject = "Collector Equipment Items Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Тестовый город",
                    Region = "Тестовый регион",
                    AirTemperature = -20,
                    WindSpeed = 3,
                    Humidity = 70,
                    SnowfallIntensity = 1,
                    SelectedZone = ClimateZone.Zone_M10
                },
                ConstructionData = new ConstructionProjectData
                {
                    R1 = 0.057,
                    R2 = 39.32,
                    LambdaE = 1.74,
                    GroundwaterLevel = 2.0,
                    Layers = new List<LayerProjectData>
                    {
                        CreateLayer(LayerPosition.AbovePipe, "Бетон", 1.74, 100, 0),
                        CreateLayer(LayerPosition.BelowPipe, "Бетон", 1.74, 10, 0),
                        CreateLayer(LayerPosition.BelowPipe, "Бетон с арматурной сеткой", 1.69, 10, 1),
                        CreateLayer(LayerPosition.BelowPipe, "Пенополистирол ЭППС", 0.035, 80, 2),
                        CreateLayer(LayerPosition.BelowPipe, "ПГС", 1.0, 200, 3),
                        CreateLayer(LayerPosition.BelowPipe, "Грунт", 0.5, 1000, 4),
                        CreateLayer(LayerPosition.BelowPipe, "Грунт", 0.5, 570, 5)
                    }
                },
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 20x2,0",
                        OuterDiameter = 20,
                        InnerDiameter = 16,
                        WallThickness = 2
                    },
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 50,
                        PowerDown = 50,
                        PowerTotal = 100,
                        SupplyTemperature = 45,
                        ReturnTemperature = 35,
                        MeanTemperature = 40,
                        DeltaT = 10,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData()
            };
        }

        private static LayerProjectData CreateLayer(
            LayerPosition position,
            string materialName,
            double lambda,
            double thickness,
            int order)
        {
            return new LayerProjectData
            {
                Position = position,
                MaterialName = materialName,
                MaterialLambda = lambda,
                Thickness = thickness,
                CalculatedR = thickness / 1000 / lambda,
                CalculatedLambda = lambda,
                Order = order
            };
        }
    }
}
