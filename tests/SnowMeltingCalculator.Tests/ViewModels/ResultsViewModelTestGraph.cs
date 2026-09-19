using Moq;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
using SnowMeltingCalculator.Repositories.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;
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
using SnowMeltingCalculator.Tests.Fixtures;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Общий фабрикатор тестового графа ResultsViewModel (план 2026-09-18
    /// «чистка раздутых тестов», волна A; ревью R-2026-09-18-02, находка №3:
    /// граф вызовов фабрик кросс-темен — единственные копии живут здесь,
    /// fixture-файлы держат только тонкие instance-адаптеры над своими полями).
    /// Только перенос кода из ResultsViewModelOpenProjectTests.cs дословно.
    /// </summary>
    internal static class ResultsViewModelTestGraph
    {
        public const string TestFilePath = @"C:\temp\test-project.smc";

        private static ConstructionDefaultStateInitializer CreateDefaultConstructionInitializer(IProjectSession session)
        {
            var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            var repository = new Mock<IMaterialRepository>();
            repository.Setup(candidate => candidate.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materials.GetValueOrDefault(id));
            return new ConstructionDefaultStateInitializer(repository.Object, session.ConstructionState);
        }

        public static ResultsViewModel CreateViewModel(
            IProjectSession session,
            Mock<IDialogService> dialogServiceMock,
            Mock<IProjectFileService> projectFileServiceMock,
            Mock<IConstructionService> constructionServiceMock,
            ClimateViewModel climateVm,
            ConstructionViewModel constructionVm,
            ThermalViewModel thermalVm,
            CircuitsViewModel circuitsVm)
        {
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());

            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);
            constructionServiceMock.Setup(s => s.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()))
                .Returns(Task.CompletedTask);

            var calculationStateService = new CalculationStateService(session);
            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                session,
                dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                projectFileServiceMock.Object,
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
                    session,
                    CreateDefaultConstructionInitializer(session)),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        public static ResultsViewModel CreateViewModel(
            IProjectSession session,
            Mock<IDialogService> dialogServiceMock,
            Mock<IProjectFileService> projectFileServiceMock,
            ClimateViewModel climateVm,
            ConstructionViewModel constructionVm,
            ThermalViewModel thermalVm,
            CircuitsViewModel circuitsVm,
            CalculationStateService calculationStateService)
        {
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                session,
                dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                projectFileServiceMock.Object,
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
                    session,
                    CreateDefaultConstructionInitializer(session)),
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
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());
        }

        public static ClimateViewModel CreateClimateViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService)
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
                markDirtyService,
                new CalculationContext());
        }

        public static ClimateViewModel CreateClimateViewModelWithCity(
            string cityName,
            string region,
            double t5Days,
            double windAvg,
            double humidity,
            IProjectSession? projectSession = null)
        {
            return CreateClimateViewModelWithCityAndSingleton(
                new ClimateData(),
                cityName,
                region,
                t5Days,
                windAvg,
                humidity,
                projectSession);
        }

        public static ClimateViewModel CreateClimateViewModelWithCityAndSingleton(
            IClimateData climateData,
            string cityName,
            string region,
            double t5Days,
            double windAvg,
            double humidity,
            IProjectSession? projectSession = null)
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.GetCityByName(cityName)).Returns(new CityInfo
            {
                Name = cityName,
                Region = region,
                T5Days092 = t5Days,
                WindMaxJan = windAvg,
                Humidity15hCold = humidity
            });
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns((double t, bool high) =>
                {
                    if (high) return ClimateZone.Zone_M20_Plus;
                    if (t >= -27) return ClimateZone.Zone_M10;
                    if (t > -37) return ClimateZone.Zone_M15;
                    return ClimateZone.Zone_M20;
                });

            if (projectSession != null)
            {
                return new ClimateViewModel(
                    climateServiceMock.Object,
                    climateData,
                    new ClimateValidator(),
                    projectSession);
            }

            return new ClimateViewModel(
                climateServiceMock.Object,
                climateData,
                new ClimateValidator(),
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());
        }

        public static ConstructionViewModel CreateConstructionViewModel(IProjectSession? projectSession = null)
        {
            var materialsById = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            foreach (var material in new[]
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            })
            {
                materialsById[material.Id] = material;
            }

            var materials = materialsById.Values.ToList();

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materialsById.GetValueOrDefault(id));

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            var calculationContext = new CalculationContext();
            projectSession ??= new ProjectSession(calculationContext: calculationContext);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
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
                new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, projectSession.ConstructionState));
        }

        public static ConstructionViewModel CreateConstructionViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService,
            IProjectSession? projectSession = null)
        {
            var materialsById = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            foreach (var material in new[]
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            })
            {
                materialsById[material.Id] = material;
            }
            var materials = materialsById.Values.ToList();

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materialsById.GetValueOrDefault(id));

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            var calculationContext = new CalculationContext();
            projectSession ??= new ProjectSession(calculationContext: calculationContext);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                calculationStateService,
                calculationContext,
                new ConstructionValidator(),
                new ConstructionModel(),
                markDirtyService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                projectSession.ConstructionState,
                new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, projectSession.ConstructionState));
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

        public static ThermalViewModel CreateThermalViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService)
        {
            return CreateThermalViewModel(
                calculationStateService,
                markDirtyService,
                new Mock<IThermalCalculator>().Object);
        }

        public static ThermalViewModel CreateThermalViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService,
            IThermalCalculator thermalCalculator)
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            var thermalValidatorMock = new Mock<IValidator<ThermalInputs>>();
            thermalValidatorMock
                .Setup(validator => validator.Validate(It.IsAny<ThermalInputs>()))
                .Returns(ValidationResult.Success());
            return new ThermalViewModel(
                thermalCalculator,
                climateData,
                constructionData,
                calculationStateService,
                new CalculationContext(),
                thermalValidatorMock.Object,
                new ThermalResultValidator(),
                markDirtyService);
        }

        public static CircuitsViewModel CreateCircuitsViewModel(
            bool allowRemoveCircuit = false,
            GlycolProperties? glycolProperties = null)
        {
            var calculatorMock = new Mock<ICircuitsCalculator>();
            calculatorMock.Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>())).Returns(new CollectorSummary());
            calculatorMock.Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>())).Returns(new CircuitTemperatureResult());
            calculatorMock.Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>())).Returns((List<CircuitRow> circuits, ValveType _) => circuits);

            var glycolMock = new Mock<IGlycolDataService>();
            glycolMock.Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(glycolProperties ?? new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });
            glycolMock.Setup(g => g.GetMinValidConcentration(It.IsAny<GlycolType>(), It.IsAny<double>()))
                .Returns(30.0);

            var selectorMock = new Mock<ICollectorTypeSelector>();
            selectorMock.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>())).Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

            var validatorMock = new Mock<ICircuitsValidator>();
            if (allowRemoveCircuit)
            {
                validatorMock
                    .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow>(c => c != null), It.IsAny<CollectorData>()))
                    .Returns(true);
            }

            var calculationStateService = new CalculationStateService();
            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationStateService, calculationContext);
            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                calculationStateService,
                validatorMock.Object,
                selectorMock.Object,
                calculationContext,
                hydraulicsDependencies.Coordinator,
                hydraulicsDependencies.Session);
        }

        public static CircuitsViewModel CreateCircuitsViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService,
            bool allowRemoveCircuit = false,
            IProjectSession? projectSession = null)
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

            var validatorMock = new Mock<ICircuitsValidator>();
            if (allowRemoveCircuit)
            {
                validatorMock
                    .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow>(c => c != null), It.IsAny<CollectorData>()))
                    .Returns(true);
            }

            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(
                calculationStateService,
                calculationContext,
                projectSession);
            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                calculationStateService,
                validatorMock.Object,
                selectorMock.Object,
                calculationContext,
                hydraulicsDependencies.Coordinator,
                hydraulicsDependencies.Session);
        }

        public static CollectorData CreateCollectorForLifecycle(
            int collectorNumber,
            ValveType valveType,
            int circuitCount,
            double totalPower,
            double totalLength)
        {
            var collector = ResultsViewModelTestHelpers.CreateCollector(collectorNumber, valveType, circuitCount);
            collector.Summary = new CollectorSummary
            {
                CollectorNumber = collectorNumber,
                CircuitCount = circuitCount,
                TotalPipeLength = totalLength,
                TotalPower = totalPower,
                TotalFlowRate = totalPower / 20,
                PressureLoss_Operating_Pa = totalPower * 2,
                PressureLoss_Cold_Pa = totalPower * 4,
                Kv = 1.2,
                CollectorType = valveType == ValveType.HKV_D ? "HKV-D" : "IV"
            };
            return collector;
        }

        public static T GetField<T>(object instance, string fieldName) where T : class
        {
            return (T)instance.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(instance)!;
        }
    }
}
