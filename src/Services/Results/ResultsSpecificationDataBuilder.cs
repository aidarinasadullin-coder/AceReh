// ================================================================================
// REHAU Снеготаяние - Строитель данных спецификации закупки (экспорт .xlsx)
// ================================================================================
//
// Назначение: сборка ResultsSpecificationData из канонических снимков
// ProjectSession (план docs/plans/2026-09-13-excel-specification-plan.md).
// Не зависит от ViewModels (R4); мутаций не делает (R2/R5).
//
// Состав:
// - Труба: ThermalState.Snapshot.Inputs.Pipe (Name/Article), метраж =
//   Σ(CircuitLength + SupplyLength) — как ResultsKpiPresenter
// - Резьбозажимные соединения: 2 шт на каждый контур (подача + обратка),
//   типоразмер — по трубе проекта; артикул из секции «fittings»
//   rehau_products.json; для 25×2,3 артикул пуст + примечание (ОВ-5)
// - Коллекторы HKV: группировка по (ValveType, число контуров), артикул —
//   резолв из каталога ICollectorRepository
// - Контуры: наладка из HydraulicCircuitSnapshot + OperatingResult
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Fittings;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Fittings;
using SnowMeltingCalculator.Repositories.Hydraulics;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Results
{
    public class ResultsSpecificationDataBuilder
    {
        private readonly ICollectorRepository _collectorRepository;
        private readonly IFittingsRepository _fittingsRepository;

        public ResultsSpecificationDataBuilder(
            ICollectorRepository collectorRepository,
            IFittingsRepository fittingsRepository)
        {
            _collectorRepository = collectorRepository ?? throw new ArgumentNullException(nameof(collectorRepository));
            _fittingsRepository = fittingsRepository ?? throw new ArgumentNullException(nameof(fittingsRepository));
        }

        /// <summary>
        /// Собрать данные спецификации из канонических снимков сессии.
        /// </summary>
        public async Task<ResultsSpecificationData> BuildAsync(
            IProjectSession session,
            string projectNumber,
            string projectObject,
            CancellationToken cancellationToken = default)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var data = new ResultsSpecificationData
            {
                ProjectNumber = projectNumber ?? string.Empty,
                ProjectObject = projectObject ?? string.Empty
            };

            var collectors = session.HydraulicsState.Snapshot.Collectors ?? Array.Empty<HydraulicCollectorSnapshot>();
            var pipe = session.ThermalState.Snapshot.Inputs.Pipe;

            // Итоги — тот же расчёт, что ResultsKpiPresenter: длина контура =
            // петля + подача, мощность из рабочего режима.
            var totalCircuits = 0;
            double totalPipeLength = 0;
            double totalPower = 0;

            foreach (var collector in collectors)
            {
                foreach (var circuit in collector.Circuits)
                {
                    totalCircuits++;
                    totalPipeLength += circuit.CircuitLength + circuit.SupplyLength;
                    totalPower += circuit.OperatingResult?.Power ?? 0;
                }
            }

            data.TotalCircuits = totalCircuits;
            data.TotalPipeLength_m = totalPipeLength;
            data.TotalPower_kW = totalPower / 1000.0;

            // Труба
            if (pipe is not null)
            {
                data.Rows.Add(new SpecificationRow
                {
                    Section = "Труба",
                    Name = pipe.Name,
                    Article = pipe.Article,
                    Unit = "м",
                    Quantity = totalPipeLength
                });
            }

            // Резьбозажимные соединения: 2 шт на каждый контур (подача + обратка)
            if (pipe is not null && totalCircuits > 0)
            {
                var fitting = await FindFittingAsync(pipe, cancellationToken).ConfigureAwait(false);
                data.Rows.Add(new SpecificationRow
                {
                    Section = "Резьбозажимные соединения",
                    Name = fitting?.FullName ?? $"Резьбозажимное соединение для трубы {pipe.Name}",
                    Article = fitting?.ArticleNumber ?? string.Empty,
                    Unit = "шт",
                    Quantity = totalCircuits * 2,
                    Notes = fitting?.Notes
                });
            }

            // Коллекторы: группировка по (ValveType, число контуров) с сохранением
            // порядка первого появления — как BuildEquipmentItems.
            if (collectors.Count > 0)
            {
                var catalog = await _collectorRepository.GetAllAsync().ConfigureAwait(false);
                var groupMap = new Dictionary<(ValveType ValveType, int CircuitCount), List<HydraulicCollectorSnapshot>>();
                var orderedGroups = new List<(ValveType ValveType, int CircuitCount)>();

                foreach (var collector in collectors)
                {
                    var key = (collector.ValveType, collector.Circuits.Count);
                    if (!groupMap.ContainsKey(key))
                    {
                        groupMap[key] = new List<HydraulicCollectorSnapshot>();
                        orderedGroups.Add(key);
                    }

                    groupMap[key].Add(collector);
                }

                foreach (var key in orderedGroups)
                {
                    var article = FindCollectorArticle(catalog, key.ValveType, key.CircuitCount);
                    data.Rows.Add(new SpecificationRow
                    {
                        Section = "Коллекторы HKV",
                        Name = $"{CollectorTypeDisplay.Format(key.ValveType, key.CircuitCount)}",
                        Article = article ?? string.Empty,
                        Unit = "шт",
                        Quantity = groupMap[key].Count
                    });
                }
            }

            // Итого по проекту
            data.Rows.Add(new SpecificationRow
            {
                Section = "Итого по проекту",
                Name = "Контуры",
                Unit = "шт",
                Quantity = totalCircuits
            });
            data.Rows.Add(new SpecificationRow
            {
                Section = "Итого по проекту",
                Name = "Труба",
                Unit = "м",
                Quantity = totalPipeLength
            });
            data.Rows.Add(new SpecificationRow
            {
                Section = "Итого по проекту",
                Name = "Мощность",
                Unit = "кВт",
                Quantity = data.TotalPower_kW
            });

            // Контуры (наладка)
            foreach (var collector in collectors)
            {
                var typeDisplay = CollectorTypeDisplay.Format(collector.ValveType, collector.Circuits.Count);
                foreach (var circuit in collector.Circuits)
                {
                    var result = circuit.OperatingResult;
                    data.CircuitRows.Add(new SpecificationCircuitRow
                    {
                        CollectorNumber = collector.CollectorNumber,
                        CollectorType = typeDisplay,
                        CircuitNumber = circuit.CircuitNumber,
                        LoopLength_m = circuit.CircuitLength,
                        SupplyLength_m = circuit.SupplyLength,
                        // Та же формула, что HydraulicCircuitRowProjection/CircuitRow:
                        // площадь = длина петли × шаг трубы / 100
                        Area_m2 = circuit.PipeSpacingCm > 0 ? circuit.CircuitLength * circuit.PipeSpacingCm / 100.0 : 0,
                        Power_W = result?.Power ?? 0,
                        FlowRate_lh = result?.FlowRate ?? 0,
                        PressureLoss_Pa = result?.DpGesamt ?? 0,
                        ValveTurns = result?.ValveTurns ?? 0,
                        ZuDrosseln_Pa = result?.Throttling ?? 0
                    });
                }
            }

            return data;
        }

        private async Task<RzsFitting?> FindFittingAsync(ThermalPipeSnapshot pipe, CancellationToken ct)
        {
            var fittings = await _fittingsRepository.GetAllAsync(ct).ConfigureAwait(false);
            return fittings.FirstOrDefault(f =>
                Math.Abs(f.PipeOuterDiameterMm - pipe.OuterDiameter) < 0.01 &&
                Math.Abs(f.PipeWallThicknessMm - pipe.WallThickness) < 0.01);
        }

        private static string? FindCollectorArticle(
            IEnumerable<Models.Hydraulics.Collector> catalog,
            ValveType valveType,
            int circuitCount)
        {
            var collectorType = valveType == ValveType.HKV_D
                ? Models.Hydraulics.CollectorType.HKV
                : Models.Hydraulics.CollectorType.IV;

            return catalog.FirstOrDefault(c =>
                c.Type == collectorType &&
                c.Circuits == circuitCount &&
                !string.IsNullOrWhiteSpace(c.ArticleNumber))?.ArticleNumber;
        }
    }
}
