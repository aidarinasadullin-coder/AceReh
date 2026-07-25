using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Оркестратор загрузки проекта: восстанавливает состояние модулей
    /// (климат, конструкция, тепловой расчёт, гидравлика) из модели <see cref="ProjectData"/>
    /// и сбрасывает модули перед загрузкой нового проекта.
    /// Вынесен из ResultsViewModel (архитектурный долг, этап C1).
    /// </summary>
    /// <remarks>
    /// Вызывается под guard <see cref="ICalculationStateService.IsLoadProjectInProgress"/>,
    /// который устанавливает вызывающая сторона (ResultsViewModel.LoadProjectDataAsync).
    /// </remarks>
    public class ProjectLoadOrchestrator
    {
        private readonly ClimateViewModel _climateViewModel;
        private readonly ConstructionViewModel _constructionViewModel;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly CircuitsViewModel _circuitsViewModel;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IConstructionService _constructionService;
        private readonly CalculationContext _calculationContext;

        /// <summary>
        /// Конструктор оркестратора загрузки проекта
        /// </summary>
        public ProjectLoadOrchestrator(
            ClimateViewModel climateViewModel,
            ConstructionViewModel constructionViewModel,
            ThermalViewModel thermalViewModel,
            CircuitsViewModel circuitsViewModel,
            ICalculationStateService calculationStateService,
            IConstructionService constructionService,
            CalculationContext calculationContext)
        {
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));
            _constructionViewModel = constructionViewModel ?? throw new ArgumentNullException(nameof(constructionViewModel));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _circuitsViewModel = circuitsViewModel ?? throw new ArgumentNullException(nameof(circuitsViewModel));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
        }

        /// <summary>
        /// Сбросить все модули перед загрузкой нового проекта,
        /// чтобы избежать "залипания" старых результатов и ошибок.
        /// </summary>
        public void ResetModules()
        {
            _calculationContext.Reset();
            _climateViewModel.Reset();
            _constructionViewModel.Reset();
            _thermalViewModel.Reset();
            _circuitsViewModel.Reset();
        }

        /// <summary>
        /// Восстановить состояние модулей из модели проекта.
        /// Файл — источник истины. Порядок:
        /// restore inputs -> sync climate -> ensure thermal result ->
        /// restore circuit results. Единый refresh снимка Results выполняет
        /// вызывающая сторона после завершения этого метода.
        /// </summary>
        public async Task RestoreModulesFromProjectAsync(ProjectData data)
        {
            if (data == null) return;

            // Восстанавливаем климатические данные и коллекторы под guard загрузки проекта,
            // чтобы восстановление города не перезаписало сохранённые пользовательские параметры.
            _climateViewModel.BeginLoadProject();
            try
            {
                _climateViewModel.SelectedCity = null;
                var city = _climateViewModel.FindCityByName(data.ClimateData.SelectedCity);
                _climateViewModel.SearchQuery = data.ClimateData.SelectedCity;
                _climateViewModel.AirTemperature = data.ClimateData.AirTemperature;
                _climateViewModel.WindSpeed = data.ClimateData.WindSpeed;
                _climateViewModel.Humidity = data.ClimateData.Humidity;
                _climateViewModel.SnowfallIntensity = data.ClimateData.SnowfallIntensity;
                _climateViewModel.SelectedZone = data.ClimateData.SelectedZone;
                _climateViewModel.IsHighRequirements = data.ClimateData.IsHighRequirements;
                _climateViewModel.SelectedCity = city;

                // Импортируем пользовательские материалы проекта перед загрузкой слоёв
                if (data.CustomMaterials.Any())
                {
                    await _constructionService.ImportProjectMaterialsAsync(data.CustomMaterials);
                    await _constructionViewModel.ReloadMaterialsAsync();
                }

                // Импортируем пользовательские шаблоны конструкций проекта
                if (data.CustomTemplates.Any())
                {
                    await _constructionService.ImportProjectTemplatesAsync(data.CustomTemplates);
                    await _constructionViewModel.ReloadMaterialsAsync();
                }

                // Загружаем данные конструкции
                // Сначала восстанавливаем УГВ и признак нагрузок, чтобы UpdateLambda при загрузке слоёв
                // использовал корректный уровень грунтовых вод (λБ при УГВ < 1 м, λА при УГВ >= 1 м).
                _constructionViewModel.GroundwaterLevel = data.ConstructionData.GroundwaterLevel;
                _constructionViewModel.HasLoads = data.ConstructionData.HasLoads;
                if (data.ConstructionData.Layers.Any())
                {
                    LoadLayersFromProjectData(data.ConstructionData.Layers, data.Version);
                }

                // Загружаем данные теплового расчёта
                _thermalViewModel.SelectedMode = data.ThermalData.SelectedMode;
                _thermalViewModel.SupplyTemperature = data.ThermalData.SupplyTemperature;
                _thermalViewModel.GroundTemperature = data.ThermalData.GroundTemperature;
                _calculationStateService.SetPipeSpacing(data.ThermalData.PipeSpacing, "ProjectLoadOrchestrator.RestoreModules");

                // Восстанавливаем выбранную трубу
                var restoredPipe = data.ThermalData.SelectedPipe;
                if (restoredPipe != null)
                {
                    var restoredPipeType = new PipeType
                    {
                        Name = restoredPipe.Name,
                        OuterDiameter = restoredPipe.OuterDiameter,
                        InnerDiameter = restoredPipe.InnerDiameter,
                        WallThickness = restoredPipe.WallThickness
                    };
                    _thermalViewModel.SelectedPipe = _thermalViewModel.AvailablePipes
                        .FirstOrDefault(p => p == restoredPipeType)
                        ?? _thermalViewModel.AvailablePipes.FirstOrDefault();
                }

                // Восстанавливаем результат теплового расчёта
                if (data.ThermalData.Result != null)
                {
                    _thermalViewModel.Result = new ThermalCalculationResult
                    {
                        PowerUp = data.ThermalData.Result.PowerUp,
                        PowerDown = data.ThermalData.Result.PowerDown,
                        PowerTotal = data.ThermalData.Result.PowerTotal,
                        SupplyTemperature = data.ThermalData.Result.SupplyTemperature,
                        ReturnTemperature = data.ThermalData.Result.ReturnTemperature,
                        MeanTemperature = data.ThermalData.Result.MeanTemperature,
                        DeltaT = data.ThermalData.Result.DeltaT,
                        IsValid = data.ThermalData.Result.IsValid
                    };
                }

                // Загружаем коллекторы
                _circuitsViewModel.Collectors.Clear();
                foreach (var collectorData in data.HydraulicsData.Collectors)
                {
                    var collector = new CollectorData(collectorData.CollectorNumber)
                    {
                        CollectorType = collectorData.CollectorType,
                        ValveType = collectorData.ValveType
                    };

                    foreach (var circuitData in collectorData.Circuits)
                    {
                        collector.Circuits.Add(new CircuitRow
                        {
                            CircuitNumber = circuitData.CircuitNumber,
                            CircuitLength = circuitData.CircuitLength,
                            SupplyLength = circuitData.SupplyLength,
                            SupplySpacing_cm = circuitData.SupplySpacingCm,
                            SupplyHeatPercent = circuitData.SupplyHeatPercent,
                            PipeSpacing_cm = circuitData.PipeSpacingCm
                        });
                    }

                    _circuitsViewModel.Collectors.Add(collector);
                }

                // Загружаем данные гидравлики после восстановления коллекторов,
                // чтобы присвоения InputData не пометили проект dirty до завершения загрузки.
                _circuitsViewModel.InputData.GlycolType = data.HydraulicsData.GlycolType;
                _circuitsViewModel.InputData.GlycolConcentration = data.HydraulicsData.GlycolConcentration;
                _circuitsViewModel.InputData.SupplySpacing_cm = data.HydraulicsData.SupplySpacingCm;
                _circuitsViewModel.InputData.SupplyHeatPercent = data.HydraulicsData.SupplyHeatPercent;

                // Выбираем первый загруженный коллектор и обновляем состояние команд
                if (_circuitsViewModel.Collectors.Count > 0)
                {
                    _circuitsViewModel.SelectedCollectorIndex = 0;
                }
                _circuitsViewModel.AddCircuitCommand.NotifyCanExecuteChanged();
                _circuitsViewModel.RemoveCircuitCommand.NotifyCanExecuteChanged();
            }
            finally
            {
                _climateViewModel.EndLoadProject();
                // После загрузки проекта явно синхронизируем singleton IClimateData
                // с параметрами, восстановленными из файла. Иначе ThermalCalculator
                // будет считать по старым/нулевым климатическим данным.
                _climateViewModel.SyncToClimateData();
            }

            // === Детерминированная финализация загрузки проекта ===
            //
            // 1. Тепловой результат: если сохранённый результат валиден —
            //    публикуем его через canonical writer (ThermalViewModel.LoadResult),
            //    иначе выполняем полный расчёт из восстановленных входных данных.
            //    CircuitsViewModel — чистый потребитель контекста: пересчёт
            //    гидравлики срабатывает через OnCalculationContextChanged.
            if (_thermalViewModel.Result != null && _thermalViewModel.Result.IsValid)
            {
                _thermalViewModel.LoadResult(_thermalViewModel.Result);
            }
            else
            {
                // Сохранённого валидного результата нет — считаем из входных данных,
                // чтобы пользователю не пришлось нажимать "Расчёт" вручную.
                await _thermalViewModel.CalculateCommand.ExecuteAsync(null);
            }

            // 2. Восстанавливаем результаты контуров из сохранённых данных
            RestoreCircuitsResults(data.HydraulicsData.Collectors);

            // 3. Климат восстановлен из файла, а не изменён пользователем —
            //    сбрасываем признак ручных правок, выставленный сеттерами при загрузке.
            _climateViewModel.HasUserModifications = false;
        }

        /// <summary>
        /// Восстанавливает результаты контуров из сохранённых данных проекта
        /// </summary>
        private void RestoreCircuitsResults(List<CollectorProjectData> collectorsData)
        {
            if (collectorsData == null || _circuitsViewModel.Collectors == null) return;

            for (int i = 0; i < collectorsData.Count && i < _circuitsViewModel.Collectors.Count; i++)
            {
                var collectorData = collectorsData[i];
                var collector = _circuitsViewModel.Collectors[i];

                // Восстанавливаем Summary.
                // Создаём новый экземпляр, чтобы избежать shared-reference/last-write overwrite
                // между коллекторами, если Summary ранее была перезаписана одним и тем же объектом.
                if (collectorData.Summary != null)
                {
                    collector.Summary = new CollectorSummary
                    {
                        CollectorNumber = collector.CollectorNumber,
                        CollectorType = collectorData.Summary.CollectorType,
                        CircuitCount = collectorData.Summary.CircuitCount,
                        TotalPower = collectorData.Summary.TotalPower,
                        TotalFlowRate = collectorData.Summary.TotalFlowRate,
                        TotalPipeLength = collectorData.Summary.TotalPipeLength,
                        PressureLoss_Operating_Pa = collectorData.Summary.PressureLoss_Operating_Pa,
                        PressureLoss_Cold_Pa = collectorData.Summary.PressureLoss_Cold_Pa,
                        Kv = collectorData.Summary.Kv,
                        ValveType = collector.ValveType
                    };
                }

                // Восстанавливаем результаты контуров
                if (collectorData.Circuits != null && collector.Circuits != null)
                {
                    for (int j = 0; j < collectorData.Circuits.Count && j < collector.Circuits.Count; j++)
                    {
                        var circuitData = collectorData.Circuits[j];
                        var circuit = collector.Circuits[j];

                        circuit.Power = circuitData.Power;
                        circuit.FlowRate = circuitData.FlowRate;
                        circuit.Velocity = circuitData.Velocity;
                        circuit.Throttling = circuitData.Throttling;
                        circuit.ValveTurns = circuitData.ValveTurns;

                        // Восстанавливаем OperatingResult
                        if (circuitData.OperatingResult != null)
                        {
                            if (!Enum.TryParse<FlowRegime>(circuitData.OperatingResult.FlowRegimeString, true, out var operatingFlowRegime) &&
                                !Enum.TryParse<FlowRegime>(circuitData.OperatingResult.FlowRegime, true, out operatingFlowRegime))
                            {
                                operatingFlowRegime = FlowRegime.Laminar;
                            }

                            circuit.OperatingResult = new CircuitTemperatureResult
                            {
                                DpRohr = circuitData.OperatingResult.DpRohr,
                                DpVerteiler = circuitData.OperatingResult.DpVerteiler,
                                DpVent = circuitData.OperatingResult.DpVent,
                                ZuDrosseln = circuitData.OperatingResult.Throttling,
                                FlowRegime = operatingFlowRegime,
                                Density = circuitData.OperatingResult.Density,
                                KinematicViscosity = circuitData.OperatingResult.KinematicViscosity,
                                ReynoldsNumber = circuitData.OperatingResult.ReynoldsNumber,
                                FrictionFactor = circuitData.OperatingResult.FrictionFactor,
                                PressureLossPerMeter = circuitData.OperatingResult.PressureLossPerMeter
                            };
                        }

                        // Восстанавливаем DesignResult
                        if (circuitData.DesignResult != null)
                        {
                            if (!Enum.TryParse<FlowRegime>(circuitData.DesignResult.FlowRegimeString, true, out var designFlowRegime) &&
                                !Enum.TryParse<FlowRegime>(circuitData.DesignResult.FlowRegime, true, out designFlowRegime))
                            {
                                designFlowRegime = FlowRegime.Laminar;
                            }

                            circuit.DesignResult = new CircuitTemperatureResult
                            {
                                DpRohr = circuitData.DesignResult.DpRohr,
                                DpVerteiler = circuitData.DesignResult.DpVerteiler,
                                DpVent = circuitData.DesignResult.DpVent,
                                ZuDrosseln = circuitData.DesignResult.Throttling,
                                FlowRegime = designFlowRegime,
                                Density = circuitData.DesignResult.Density,
                                KinematicViscosity = circuitData.DesignResult.KinematicViscosity,
                                ReynoldsNumber = circuitData.DesignResult.ReynoldsNumber,
                                FrictionFactor = circuitData.DesignResult.FrictionFactor,
                                PressureLossPerMeter = circuitData.DesignResult.PressureLossPerMeter
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузить слои конструкции из данных проекта
        /// </summary>
        private void LoadLayersFromProjectData(List<LayerProjectData> layerDataList, string version)
        {
            // До v1.1 слои AbovePipe сохранялись в хронологическом порядке (Add в конец),
            // т.е. [у трубы, поверхность]. С v1.1 физический top-to-bottom: [поверхность, ..., у трубы].
            var needsAbovePipeReverse = string.Compare(version, "1.1", StringComparison.OrdinalIgnoreCase) < 0;

            var aboveLayers = layerDataList
                .Where(l => l.Position == LayerPosition.AbovePipe)
                .Reverse();
            if (!needsAbovePipeReverse)
                aboveLayers = aboveLayers.Reverse();
            aboveLayers = aboveLayers.ToList();

            var belowLayers = layerDataList
                .Where(l => l.Position == LayerPosition.BelowPipe)
                .ToList(); // порядок below не менялся

            // Clear + Add по мигрированным коллекциям
            _constructionViewModel.LayersAbovePipe.Clear();
            _constructionViewModel.LayersBelowPipe.Clear();

            foreach (var layerData in aboveLayers)
            {
                var material = _constructionViewModel.AvailableMaterials
                    .FirstOrDefault(m => m.Name == layerData.MaterialName)
                    ?? Material.GetDefaultMaterial();

                var layer = new Layer
                {
                    Position = layerData.Position,
                    Material = material,
                    Thickness = layerData.Thickness,
                    CalculatedLambda = layerData.CalculatedLambda,
                    IsLambdaOverridden = layerData.IsLambdaOverridden,
                    Order = layerData.Order
                };

                _constructionViewModel.LayersAbovePipe.Add(layer);
            }

            foreach (var layerData in belowLayers)
            {
                var material = _constructionViewModel.AvailableMaterials
                    .FirstOrDefault(m => m.Name == layerData.MaterialName)
                    ?? Material.GetDefaultMaterial();

                var layer = new Layer
                {
                    Position = layerData.Position,
                    Material = material,
                    Thickness = layerData.Thickness,
                    CalculatedLambda = layerData.CalculatedLambda,
                    IsLambdaOverridden = layerData.IsLambdaOverridden,
                    Order = layerData.Order
                };

                _constructionViewModel.LayersBelowPipe.Add(layer);
            }

            // Обновляем λ для слоёв под трубой в соответствии с восстановленным УГВ.
            // Метод UpdateLambda учитывает флаг IsLambdaOverridden и оставляет ручные значения нетронутыми.
            foreach (var layer in _constructionViewModel.LayersBelowPipe)
            {
                layer.UpdateLambda(_constructionViewModel.GroundwaterLevel);
            }

            // После загрузки проекта сбрасываем флаг ручного переопределения λ.
            // Значение λ сохранено из файла, но дальнейшее изменение УГВ должно
            // пересчитывать λ по каталогу (P0-7).
            foreach (var layer in _constructionViewModel.LayersAbovePipe
                .Concat(_constructionViewModel.LayersBelowPipe))
            {
                layer.IsLambdaOverridden = false;
            }

            _constructionViewModel.UpdateCalculations();
        }
    }
}
