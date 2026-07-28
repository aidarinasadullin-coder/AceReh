using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Строитель модели данных для PDF-экспорта результатов расчёта.
    /// Собирает <see cref="ResultsPdfData"/> из публичного снимка ResultsViewModel
    /// и состояния модулей (конструкция, гидравлика).
    /// Вынесен из ResultsViewModel (архитектурный долг, этап C2).
    /// </summary>
    public class ResultsPdfDataBuilder
    {
        private readonly IConstructionVisualizationImageService _constructionVisualizationImageService;
        private readonly ICalculationStateService _calculationStateService;
        private readonly ConstructionViewModel _constructionViewModel;
        private readonly CircuitsViewModel _circuitsViewModel;

        /// <summary>
        /// Конструктор строителя PDF-данных
        /// </summary>
        public ResultsPdfDataBuilder(
            IConstructionVisualizationImageService constructionVisualizationImageService,
            ICalculationStateService calculationStateService,
            ConstructionViewModel constructionViewModel,
            CircuitsViewModel circuitsViewModel)
        {
            _constructionVisualizationImageService = constructionVisualizationImageService ?? throw new ArgumentNullException(nameof(constructionVisualizationImageService));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _constructionViewModel = constructionViewModel ?? throw new ArgumentNullException(nameof(constructionViewModel));
            _circuitsViewModel = circuitsViewModel ?? throw new ArgumentNullException(nameof(circuitsViewModel));
        }

        /// <summary>
        /// Собрать данные для PDF экспорта
        /// </summary>
        public ResultsPdfData Build(ResultsViewModel results)
        {
            results.RefreshAll();

            var pdfData = new ResultsPdfData
            {
                // Информация о проекте
                ProjectNumber = results.ProjectNumber,
                ProjectObject = results.ProjectObject,
                ReportDate = DateTime.Now,

                // KPI
                TotalThermalPower_kW = results.TotalThermalPower_kW,
                SystemVolume_L = results.SystemVolume_L,
                PumpFlowRate_m3h = results.PumpFlowRate_m3h,
                PumpHead_kPa = results.PumpHead_kPa,
                ExpansionTankVolume_L = results.ExpansionTankVolume_L,

                // Температуры
                SupplyTemperature = results.SupplyTemperature,
                ReturnTemperature = results.ReturnTemperature,
                OperatingTemperature = results.OperatingTemperature,
                GroundTemperature = results.GroundTemperature,
                SurfaceTemperature = results.SurfaceTemperature,

                // Климат
                City = results.SelectedCity,
                DesignTemperature = results.DesignTemperature,
                WindSpeed = results.WindSpeed,
                SnowfallIntensity = results.SnowfallIntensity,
                ClimateZone = results.ClimateZone,
                ColdPeriodDays = results.ColdPeriodDays,

                // Труба
                PipeType = results.PipeType,
                PipeSpacing = results.PipeSpacing,

                // Режим и теплоноситель
                OperatingMode = results.OperatingMode,
                GlycolType = results.GlycolType,
                GlycolConcentration = results.GlycolConcentration,

                // Конструкция
                R1 = results.R1,
                R2 = results.R2,
                LambdaE = results.LambdaE,
                PowerUp = results.PowerUp,
                PowerDown = results.PowerDown,
                TotalPowerDensity = results.TotalPowerDensity,

                // Оборудование
                TotalPipeLength = results.TotalPipeLength,
                RzsCount = results.RzsCount
            };

            // Слои конструкции
            foreach (var layer in results.Layers)
            {
                pdfData.Layers.Add(new LayerPdfData
                {
                    MaterialName = layer.Material?.Name ?? "Не указан",
                    Thickness = layer.Thickness,
                    Lambda = layer.CalculatedLambda,
                    R = layer.CalculatedR,
                    Position = layer.Position == LayerPosition.AbovePipe ? "Над трубой" : "Под трубой"
                });
            }

            // Изображение схемы конструкции для PDF
            pdfData.ConstructionImageBytes = _constructionVisualizationImageService.GenerateImage(
                new ConstructionVisualizationParameters
                {
                    LayersAbovePipe = _constructionViewModel.LayersAbovePipe,
                    LayersBelowPipe = _constructionViewModel.LayersBelowPipe,
                    PipeSpacing = _calculationStateService.PipeSpacing,
                    CompactMode = true,
                    ShowDimensionLine = true,
                    FixedScaleFactor = 0.25
                },
                width: 400,
                height: 300);

            // Коллекторы и контуры
            if (_circuitsViewModel.Collectors != null)
            {
                foreach (var collector in _circuitsViewModel.Collectors)
                {
                    if (collector == null) continue;

                    var collectorPdf = new CollectorPdfData
                    {
                        Number = collector.CollectorNumber,
                        Type = collector.CollectorTypeDisplayWithCount,
                        Summary = new CollectorSummaryPdfData
                        {
                            CircuitCount = collector.Circuits?.Count ?? 0,
                            TotalPipeLength = collector.Summary?.TotalPipeLength ?? 0,
                            TotalPower = collector.Summary?.TotalPower ?? 0,
                            TotalFlowRate = collector.Summary?.TotalFlowRate ?? 0,
                            PressureLoss_Operating_kPa = (collector.Summary?.PressureLoss_Operating_Pa ?? 0) / 1000.0,
                            PressureLoss_Cold_kPa = (collector.Summary?.PressureLoss_Cold_Pa ?? 0) / 1000.0,
                            Kv = collector.Summary?.Kv ?? 1.2,
                            CollectorType = collector.Summary?.CollectorType ?? "HKV-D"
                        }
                    };

                    // Контуры коллектора
                    if (collector.Circuits != null)
                    {
                        foreach (var circuit in collector.Circuits)
                        {
                            if (circuit == null) continue;

                            // Используем данные для рабочего режима
                            var result = circuit.OperatingResult;

                            // Расчёт удельных потерь (Па/м)
                            double pressureLossPerMeter = 0;
                            if (result?.DpRohr > 0 && circuit.TotalLength > 0)
                            {
                                pressureLossPerMeter = result.DpRohr / circuit.TotalLength;
                            }

                            collectorPdf.Circuits.Add(new CircuitPdfData
                            {
                                CircuitNumber = circuit.CircuitNumber,
                                Length = circuit.TotalLength,
                                Area = circuit.CircuitArea,
                                Power = circuit.Power,
                                FlowRate = circuit.FlowRate,
                                Velocity = circuit.Velocity,
                                FlowRegime = circuit.FlowRegimeDescription,
                                PressureLossPerMeter = pressureLossPerMeter,
                                DpRohr = (result?.DpRohr ?? 0) / 1000.0,        // кПа
                                DpVerteiler = (result?.DpVerteiler ?? 0) / 1000.0, // кПа
                                DpVent = (result?.DpVent ?? 0) / 1000.0,          // кПа
                                DpGesamt = (result?.DpGesamt ?? 0) / 1000.0,      // кПа
                                Throttling = circuit.Throttling / 1000.0,         // кПа
                                ZuDrosseln = circuit.Throttling / 1000.0, // кПа
                                ValveTurns = circuit.ValveTurns
                            });
                        }
                    }

                    pdfData.Collectors.Add(collectorPdf);
                }
            }

            // Спецификации коллекторов
            foreach (var spec in results.CollectorSpecifications)
            {
                pdfData.CollectorSpecifications.Add(new CollectorSpecPdfData
                {
                    Number = spec.Number,
                    Type = spec.Type,
                    CircuitCount = spec.CircuitCount,
                    TotalPower_kW = spec.TotalPower_kW,
                    TotalFlowRate_m3h = spec.TotalFlowRate_m3h,
                    PressureLoss_mbar = spec.PressureLoss_mbar,
                    Kv = spec.Kv
                });
            }

            return pdfData;
        }
    }
}
