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
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            var calculationStateService = new CalculationStateService(projectStateService.Session);
            var calculationContext = new CalculationContext();
            var climateVm = CreateClimateViewModel(projectStateService.Session);
            var constructionVm = CreateConstructionViewModel();
            var thermalVm = CreateThermalViewModel();

            return new ResultsViewModel(
                projectStateService,
                projectStateService.Session,
                projectStateService,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                new Mock<IProjectFileService>().Object,
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
                    projectStateService.Session),
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
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
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
                new Mock<IMarkDirtyService>().Object,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object);
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

            var viewModel = new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                new CalculationStateService(),
                new Mock<ICircuitsValidator>().Object,
                selectorMock.Object,
                new CalculationContext(),
                new Mock<IMarkDirtyService>().Object);

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
                ConstructionData = new ConstructionProjectData(),
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
    }
}
