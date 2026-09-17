using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Общая фикстура отчётных тестов: полный ProjectData с валидным контуром
    /// (канон — CalculationReportWarningTests, копии сверены 2026-09-18).
    /// Историческое расхождение CalculationReportDataBuilderTests — поля
    /// Throttling/ValveTurns на уровне контура — включается флагом.
    /// </summary>
    internal static class CalculationReportTestProjectFactory
    {
        internal static ProjectData CreateValidCircuitProject(
            string projectNumber,
            string projectObject,
            bool withCircuitLevelThrottling = false)
        {
            return new ProjectData
            {
                ProjectNumber = projectNumber,
                ProjectObject = projectObject,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Москва",
                    Region = "Московская область",
                    AirTemperature = -28.0,
                    WindSpeed = 3.5,
                    Humidity = 85.0,
                    SnowfallIntensity = 0.5,
                    SelectedZone = ClimateZone.Zone_M20
                },
                ConstructionData = new ConstructionProjectData
                {
                    GroundwaterLevel = 2.0,
                    R1 = 0.05,
                    R2 = 0.02,
                    LambdaE = 1.6,
                    Layers = new List<LayerProjectData>
                    {
                        new LayerProjectData
                        {
                            Position = LayerPosition.AbovePipe,
                            MaterialName = "Бетон",
                            MaterialLambda = 1.5,
                            CalculatedLambda = 1.5,
                            Thickness = 80.0,
                            CalculatedR = 0.05,
                            Order = 0
                        }
                    }
                },
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    GroundTemperature = 10.0,
                    SupplyTemperature = 55.0,
                    PipeSpacing = 200,
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 20x2.0",
                        OuterDiameter = 20.0,
                        InnerDiameter = 16.0,
                        WallThickness = 2.0
                    },
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 250.0,
                        PowerDown = 50.0,
                        PowerTotal = 300.0,
                        SupplyTemperature = 55.0,
                        ReturnTemperature = 40.0,
                        MeanTemperature = 47.5,
                        DeltaT = 15.0,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    GlycolType = GlycolType.Ethylene,
                    GlycolConcentration = 50.0,
                    SupplySpacingCm = 5.0,
                    SupplyHeatPercent = 10.0,
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            CollectorNumber = 1,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData
                                {
                                    CircuitNumber = 1,
                                    CircuitLength = 80.0,
                                    SupplyLength = 10.0,
                                    SupplySpacingCm = 5.0,
                                    SupplyHeatPercent = 10.0,
                                    PipeSpacingCm = 20.0,
                                    OperatingResult = new CircuitResultProjectData
                                    {
                                        Power = 1200.0,
                                        FlowRate = 100.0,
                                        Velocity = 0.8,
                                        DpRohr = 8000.0,
                                        DpVerteiler = 4000.0,
                                        DpVent = 3000.0,
                                        DpGesamt = 15000.0,
                                        Throttling = 5000.0,
                                        ValveTurns = 2.0,
                                        FlowRegime = "Turbulent",
                                        Density = 1.05,
                                        KinematicViscosity = 1.5,
                                        ReynoldsNumber = 8000.0,
                                        FrictionFactor = 0.03,
                                        PressureLossPerMeter = 166.67
                                    },
                                    DesignResult = new CircuitResultProjectData
                                    {
                                        Power = 2400.0,
                                        FlowRate = 200.0,
                                        Velocity = 1.2,
                                        DpRohr = 16000.0,
                                        DpVerteiler = 8000.0,
                                        DpVent = 6000.0,
                                        DpGesamt = 30000.0,
                                        Throttling = 10000.0,
                                        ValveTurns = 3.0,
                                        FlowRegime = "Turbulent",
                                        Density = 1.08,
                                        KinematicViscosity = 2.5,
                                        ReynoldsNumber = 12000.0,
                                        FrictionFactor = 0.025,
                                        PressureLossPerMeter = 333.33
                                    },
                                    Throttling = withCircuitLevelThrottling ? 5000.0 : default,
                                    ValveTurns = withCircuitLevelThrottling ? 2.0 : default
                                }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 1,
                                TotalPipeLength = 90.0,
                                TotalPower = 1200.0,
                                TotalFlowRate = 100.0,
                                PressureLoss_Operating_Pa = 15000.0,
                                PressureLoss_Cold_Pa = 30000.0,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };
        }
    }
}
