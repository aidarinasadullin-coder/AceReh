using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Строитель read-model итогов гидравлики: карточки по коллекторам,
    /// спецификации и сгруппированное оборудование.
    /// Чистый маппинг состояния коллекторов — без собственного состояния.
    /// Вынесен из ResultsViewModel (архитектурный долг, этап C3).
    /// Phase 9 (ST-026/ST-027): вход — канонические снимки HydraulicsState,
    /// не модель модуля.
    /// </summary>
    public class HydraulicSummaryBuilder
    {
        /// <summary>
        /// Построить канонический read-model карточек итогов гидравлики по всем коллекторам.
        /// </summary>
        public List<CollectorHydraulicSummaryCard> BuildSummaryCards(IReadOnlyList<HydraulicCollectorSnapshot>? collectors)
        {
            var cards = new List<CollectorHydraulicSummaryCard>();
            if (collectors == null) return cards;

            foreach (var collector in collectors)
            {
                if (collector == null) continue;
                cards.Add(BuildCard(collector));
            }

            return cards;
        }

        /// <summary>
        /// Построить спецификации коллекторов
        /// </summary>
        public List<CollectorSpecification> BuildSpecifications(IReadOnlyList<HydraulicCollectorSnapshot>? collectors, bool isOperatingMode)
        {
            var specifications = new List<CollectorSpecification>();
            if (collectors == null) return specifications;

            foreach (var collector in collectors)
            {
                if (collector?.Summary == null) continue;
                var summary = collector.Summary;

                specifications.Add(new CollectorSpecification
                {
                    Number = collector.CollectorNumber,
                    Type = FormatCollectorTypeDisplay(collector.ValveType, collector.Circuits.Count),
                    CircuitCount = collector.Circuits.Count,
                    TotalPower_kW = summary.TotalPower / 1000.0,
                    TotalFlowRate_m3h = summary.TotalFlowRate / 1000.0,
                    PressureLoss_mbar = isOperatingMode
                        ? summary.PressureLoss_Operating_Pa / 100.0
                        : summary.PressureLoss_Cold_Pa / 100.0,
                    Kv = summary.Kv
                });
            }

            return specifications;
        }

        /// <summary>
        /// Построить сгруппированный read-model оборудования коллекторов.
        /// </summary>
        /// <remarks>
        /// Группирует коллекторы по (ValveType, CircuitCount), сохраняя порядок первого появления.
        /// </remarks>
        public List<CollectorEquipmentItem> BuildEquipmentItems(IReadOnlyList<HydraulicCollectorSnapshot>? collectors)
        {
            var items = new List<CollectorEquipmentItem>();
            if (collectors == null) return items;

            var groupMap = new Dictionary<(ValveType ValveType, int CircuitCount), CollectorEquipmentItem>();
            var orderedGroups = new List<CollectorEquipmentItem>();

            foreach (var collector in collectors)
            {
                if (collector == null) continue;

                int circuitCount = collector.Circuits.Count;
                var key = (collector.ValveType, circuitCount);

                if (groupMap.TryGetValue(key, out var existingItem))
                {
                    existingItem.CollectorQuantity++;
                }
                else
                {
                    var newItem = new CollectorEquipmentItem
                    {
                        ValveType = collector.ValveType,
                        CircuitCount = circuitCount,
                        Type = collector.ValveType switch
                        {
                            ValveType.HKV_D => $"HKV-D ({FormatCircuitCount(circuitCount)})",
                            ValveType.IV_1_25 or ValveType.IV_1_5 => $"IV ({FormatCircuitCount(circuitCount)})",
                            _ => FormatCollectorTypeDisplay(collector.ValveType, circuitCount)
                        },
                        CollectorQuantity = 1
                    };

                    groupMap[key] = newItem;
                    orderedGroups.Add(newItem);
                }
            }

            items.AddRange(orderedGroups);
            return items;
        }

        private static CollectorHydraulicSummaryCard BuildCard(HydraulicCollectorSnapshot collector)
        {
            var summary = collector.Summary;
            return new CollectorHydraulicSummaryCard
            {
                CollectorNumber = collector.CollectorNumber,
                CollectorTypeDisplay = FormatCollectorTypeDisplay(collector.ValveType, collector.Circuits.Count),
                CircuitCount = summary?.CircuitCount ?? 0,
                TotalPipeLength = summary?.TotalPipeLength ?? 0,
                TotalPower = summary?.TotalPower ?? 0,
                TotalFlowRate = summary?.TotalFlowRate ?? 0,
                OperatingPressureLossPa = summary?.PressureLoss_Operating_Pa ?? 0,
                ColdPressureLossPa = summary?.PressureLoss_Cold_Pa ?? 0,
                Kv = summary?.Kv ?? 0
            };
        }

        private static string FormatCollectorTypeDisplay(ValveType valveType, int circuitCount) =>
            $"{FormatCollectorTypeName(valveType)} ({FormatCircuitCount(circuitCount)})";

        private static string FormatCollectorTypeName(ValveType valveType) => valveType switch
        {
            ValveType.HKV_D => "HKV-D",
            ValveType.IV_1_25 => "IV 1¼\"",
            ValveType.IV_1_5 => "IV 1½\"",
            _ => "Unknown"
        };

        private static string FormatCircuitCount(int count) => count switch
        {
            1 => "1 контур",
            2 or 3 or 4 => $"{count} контура",
            _ => $"{count} контуров"
        };
    }
}
