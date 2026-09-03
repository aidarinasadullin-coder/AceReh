using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;

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
        private readonly IProjectLoadClimateAdapter _climateViewModel;
        private readonly IProjectLoadConstructionAdapter _constructionViewModel;
        private readonly IProjectLoadThermalAdapter _thermalViewModel;
        private readonly IProjectLoadHydraulicsAdapter _circuitsViewModel;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IConstructionService _constructionService;
        private readonly CalculationContext _calculationContext;
        private readonly IProjectSessionClimateState _climateState;
        private readonly IProjectSessionConstructionState _constructionState;
        private readonly IProjectSessionThermalState _thermalState;
        private readonly IProjectSessionHydraulicsState _hydraulicsState;
        private readonly ConstructionDefaultStateInitializer _constructionDefaultStateInitializer;

        /// <summary>
        /// Конструктор оркестратора загрузки проекта
        /// </summary>
        public ProjectLoadOrchestrator(
            IProjectLoadClimateAdapter climateViewModel,
            IProjectLoadConstructionAdapter constructionViewModel,
            IProjectLoadThermalAdapter thermalViewModel,
            IProjectLoadHydraulicsAdapter circuitsViewModel,
            ICalculationStateService calculationStateService,
            IConstructionService constructionService,
            CalculationContext calculationContext,
            IProjectSession? projectSession = null,
            ConstructionDefaultStateInitializer? constructionDefaultStateInitializer = null)
        {
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));
            _constructionViewModel = constructionViewModel ?? throw new ArgumentNullException(nameof(constructionViewModel));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _circuitsViewModel = circuitsViewModel ?? throw new ArgumentNullException(nameof(circuitsViewModel));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _constructionService = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            var session = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _climateState = session.ClimateState;
            _constructionState = session.ConstructionState;
            _thermalState = session.ThermalState;
            _hydraulicsState = session.HydraulicsState;
            _constructionDefaultStateInitializer = constructionDefaultStateInitializer
                ?? throw new ArgumentNullException(nameof(constructionDefaultStateInitializer));
        }

        /// <summary>
        /// Сбросить все модули перед загрузкой нового проекта,
        /// чтобы избежать "залипания" старых результатов и ошибок.
        /// </summary>
        public void ResetModules()
        {
            var constructionResult = _constructionDefaultStateInitializer.Apply(
                _constructionState.Snapshot.GroundwaterLevel,
                ConstructionMutationOrigin.Reset);

            _calculationContext.Reset();
            _climateState.ResetToDefaults(ClimateMutationOrigin.ProjectLoadReset);
            _climateViewModel.SearchQuery = string.Empty;
            _constructionViewModel.ApplyLifecycleSnapshotToAdapter(constructionResult.After);
            // Канонический сброс теплового состояния жизненным циклом проекта
            // (не пользователем): результат/статус очищаются без user-dirty
            // (DEC-T08, Todo 9); адаптер ниже зеркалит дефолты без мутаций.
            _thermalState.ResetToDefaults(ThermalMutationOrigin.ProjectLoadReset);
            _thermalViewModel.Reset();
            _hydraulicsState.ResetToDefaults(HydraulicsMutationOrigin.ProjectLoadReset);
            _circuitsViewModel.Reset();
        }

        /// <summary>
        /// Восстановить состояние модулей из модели проекта.
        /// Файл — источник истины. Порядок:
        /// restore inputs -> sync climate -> ensure thermal result ->
        /// restore circuit results. Единый refresh снимка Results выполняет
        /// вызывающая сторона после завершения этого метода.
        /// </summary>
        /// <returns>true, если все модули восстановлены; false, если preflight отклонён.</returns>
        public async Task<bool> RestoreModulesFromProjectAsync(ProjectData data)
        {
            if (data == null) return false;

            var thermalCandidate = ThermalPersistenceMapper.BuildInputsCandidate(
                data.ThermalData,
                _thermalViewModel.AvailablePipes);
            if (IsEmptyThermalData(data.ThermalData))
            {
                thermalCandidate = ThermalInputsSnapshot.Default;
            }
            else if (HasUnsetThermalScalarInputs(data.ThermalData))
            {
                // Legacy-partial .smc: scalar thermal inputs were never persisted
                // (CLR defaults). Canonical DEC-T01 defaults apply to the scalars
                // while the persisted pipe selection is preserved, so the adapter
                // keeps the user's pipe and the open flow continues (no early abort).
                thermalCandidate = new ThermalInputsSnapshot(
                    ThermalInputsSnapshot.Default.Mode,
                    ThermalInputsSnapshot.Default.SupplyTemperature,
                    ThermalInputsSnapshot.Default.GroundTemperature,
                    thermalCandidate.Pipe,
                    ThermalInputsSnapshot.Default.PipeSpacing);
            }
            var savedThermalResult = ThermalPersistenceMapper.BuildSavedResult(data.ThermalData?.Result);
            var thermalPreflight = new ProjectSessionThermalState().Restore(
                thermalCandidate,
                savedThermalResult);
            if (thermalPreflight.IsRejected)
            {
                return false;
            }

            var hydraulicsCandidate = HydraulicsPersistenceMapper.BuildRestoreCandidate(data.HydraulicsData);
            if (IsEmptyHydraulicsData(data.HydraulicsData))
            {
                hydraulicsCandidate = HydraulicsStateSnapshot.Default;
            }
            var hydraulicsPreflight = new ProjectSessionHydraulicsState().Restore(
                hydraulicsCandidate,
                HydraulicsMutationOrigin.ProjectLoad);
            if (hydraulicsPreflight.IsRejected)
            {
                return false;
            }

            var city = _climateViewModel.FindCityByName(data.ClimateData.SelectedCity);
            _climateViewModel.SearchQuery = data.ClimateData.SelectedCity;
            _climateState.ApplyProjectSnapshot(data.ClimateData, city, ClimateMutationOrigin.Load);

            // Каталоги материалов и шаблонов являются глобальными read-only
            // источниками при открытии проекта. Пользовательские записи из
            // ProjectData остаются project-local и не импортируются в каталоги.

            // Загружаем данные конструкции
                // Сначала восстанавливаем УГВ и признак нагрузок, чтобы UpdateLambda при загрузке слоёв
                // использовал корректный уровень грунтовых вод (λБ при УГВ < 1 м, λА при УГВ >= 1 м).
                var result = _constructionState.ApplySnapshot(
                    BuildConstructionSnapshotFromProjectData(data),
                    ConstructionMutationOrigin.ProjectLoad);
                _constructionViewModel.ApplyLifecycleSnapshotToAdapter(result.After);

                // Загружаем данные теплового расчёта через канонический Restore
                // (DEC-T08, Todo 9): маппер строит кандидата входов и сохранённого
                // результата из DTO; Restore атомарно заменяет ВСЕ компоненты
                // состояния предыдущего проекта (входы/результат/статус) — вторая
                // загрузка не оставляет stale-значений проекта A.
                var restoreMutation = _thermalState.Restore(
                    thermalCandidate,
                    savedThermalResult);
                if (restoreMutation.IsRejected)
                {
                    // Повреждённый/вне-диапазона кандидат отклонён атомарно
                    // (замороженная валидация не ослабляется). Гарантируем ноль
                    // stale-значений проекта A (DEC-T08) повторным Restore с
                    // каноническими дефолтами входов; валидный сохранённый
                    // результат файла при этом сохраняется (legacy-наблюдаемое:
                    // файловый результат публикуется без пересчёта), а при
                    // отсутствии/невалидности результата финализация выполнит
                    // ровно один fallback-расчёт.
                    _thermalState.Restore(
                        ThermalInputsSnapshot.Default,
                        savedThermalResult);
                }

                // Совместимая поверхность шага укладки: после канонического Restore
                // значение совпадает, вызов гарантированно no-op (ноль событий).
                _calculationStateService.SetPipeSpacing(thermalCandidate.PipeSpacing, "ProjectLoadOrchestrator.RestoreModules");

                // Обновляем адаптер ViewModel из канонического кандидата (привязки UI);
                // под guard загрузки присвоения не создают пользовательских мутаций.
                _thermalViewModel.SelectedMode = thermalCandidate.Mode;
                _thermalViewModel.SupplyTemperature = thermalCandidate.SupplyTemperature;
                _thermalViewModel.GroundTemperature = thermalCandidate.GroundTemperature;
                _thermalViewModel.SelectedPipe = ThermalPersistenceMapper.ResolveStandardPipe(
                    thermalCandidate.Pipe,
                    _thermalViewModel.AvailablePipes);
                _thermalViewModel.PipeSpacing = thermalCandidate.PipeSpacing;

                // Restore the complete hydraulics slice atomically, then mirror it into the adapter.
                _hydraulicsState.Restore(hydraulicsCandidate, HydraulicsMutationOrigin.ProjectLoad);
                _circuitsViewModel.ApplyLifecycleSnapshotToAdapter(_hydraulicsState.Snapshot);

            // === Детерминированная финализация загрузки проекта ===
            //
            // 1. Тепловой результат читается ТОЛЬКО из канонического состояния
            //    (DEC-T08, Todo 9): валидный сохранённый результат уже восстановлен
            //    через Restore — публикуем его ровно один раз через адаптер
            //    (LoadResult); иначе выполняем РОВНО ОДИН полный расчёт из
            //    восстановленных входных данных (fallback для отсутствующего или
            //    невалидного сохранённого результата). CircuitsViewModel — чистый
            //    потребитель контекста: пересчёт гидравлики срабатывает через
            //    OnCalculationContextChanged.
            if (_thermalState.Snapshot.Result is { IsValid: true })
            {
                _thermalViewModel.LoadResult(
                    ThermalPersistenceMapper.ToDomainResult(_thermalState.Snapshot.Result));
            }
            else
            {
                // Сохранённого валидного результата нет — считаем из входных данных,
                // чтобы пользователю не пришлось нажимать "Расчёт" вручную.
                await _thermalViewModel.CalculateFromRestoreAsync();
            }

            // Thermal result publication can trigger the normal hydraulics
            // recalculation path. Restore the file's hydraulics result last so
            // a valid persisted project remains a lossless round-trip.
            _hydraulicsState.Restore(hydraulicsCandidate, HydraulicsMutationOrigin.ProjectLoad);
            _circuitsViewModel.ApplyLifecycleSnapshotToAdapter(_hydraulicsState.Snapshot);

            return true;
        }

        private ConstructionStateSnapshot BuildConstructionSnapshotFromProjectData(ProjectData data)
        {
            var needsAbovePipeReverse = string.Compare(
                data.Version,
                "1.1",
                StringComparison.OrdinalIgnoreCase) < 0;

            IEnumerable<LayerProjectData> aboveLayers = data.ConstructionData.Layers
                .Where(layer => layer.Position == LayerPosition.AbovePipe);
            if (needsAbovePipeReverse)
            {
                aboveLayers = aboveLayers.Reverse();
            }

            var belowLayers = data.ConstructionData.Layers
                .Where(layer => layer.Position == LayerPosition.BelowPipe);

            return new ConstructionStateSnapshot(
                data.ConstructionData.GroundwaterLevel,
                data.ConstructionData.HasLoads,
                BuildLayerSnapshots(aboveLayers, data.ConstructionData.GroundwaterLevel),
                BuildLayerSnapshots(belowLayers, data.ConstructionData.GroundwaterLevel));
        }

        private static bool IsEmptyThermalData(ThermalProjectData? data)
        {
            return data is not null
                && (data.SelectedMode == default || data.SelectedMode == OperatingMode.Melting)
                && data.SupplyTemperature == 0.0
                && data.GroundTemperature == 0.0
                && data.SelectedPipe is null;
        }

        private static bool HasUnsetThermalScalarInputs(ThermalProjectData? data)
        {
            // Legacy-partial .smc: the scalar thermal inputs were never persisted
            // (CLR defaults) while a pipe selection is present. Treat the scalars as
            // unset so the open flow applies canonical DEC-T01 defaults and keeps the
            // user's pipe, instead of aborting the whole restore on a rejected candidate.
            return data is not null
                && (data.SelectedMode == default || data.SelectedMode == OperatingMode.Melting)
                && data.SupplyTemperature == 0.0
                && data.GroundTemperature == 0.0;
        }

        private static bool IsEmptyHydraulicsData(HydraulicsProjectData? data)
        {
            return data is not null
                && data.GlycolType == GlycolType.Ethylene
                && data.GlycolConcentration == 0.0
                && data.SupplySpacingCm == 0.0
                && data.SupplyHeatPercent == 0.0
                && (data.Collectors is null || data.Collectors.Count == 0);
        }

        private List<ConstructionLayerSnapshot> BuildLayerSnapshots(
            IEnumerable<LayerProjectData> layerDataList,
            double groundwaterLevel)
        {
            return layerDataList.Select((layerData, index) =>
            {
                var material = _constructionViewModel.AvailableMaterials
                    .FirstOrDefault(candidate => candidate.Name == layerData.MaterialName)
                    ?? Material.GetDefaultMaterial();
                var calculatedLambda = layerData.Position == LayerPosition.BelowPipe
                    && !layerData.IsLambdaOverridden
                        ? groundwaterLevel < 1.0 ? material.LambdaB : material.LambdaA
                        : layerData.CalculatedLambda;

                return new ConstructionLayerSnapshot(
                    Guid.NewGuid(),
                    material.Id,
                    material.Name,
                    layerData.Thickness,
                    calculatedLambda,
                    false,
                    layerData.Position,
                    index);
            }).ToList();
        }


    }
}
