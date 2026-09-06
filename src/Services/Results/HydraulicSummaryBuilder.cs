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
    /// DE-3: дополнительно строки переключателя коллекторов и сводка
    /// выбранного коллектора (бывшие UpdateCollectorsList/CreateCollectorSummary
    /// из ResultsViewModel).
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

        /// <summary>
        /// Построить строки переключателя коллекторов (DE-3: вынос из ResultsViewModel).
        /// </summary>
        /// <remarks>
        /// <c>IsSelected</c> при построении всегда назначается первой строке;
        /// фактический выбор восстанавливает ResultsViewModel после перестроения.
        /// </remarks>
        public List<CollectorInfo> BuildCollectorInfos(IReadOnlyList<HydraulicCollectorSnapshot>? collectors)
        {
            var collectorInfos = new List<CollectorInfo>();
            if (collectors == null) return collectorInfos;

            for (int i = 0; i < collectors.Count; i++)
            {
                var collectorData = collectors[i];
                if (collectorData == null) continue;

                collectorInfos.Add(new CollectorInfo
                {
                    Number = collectorData.CollectorNumber,
                    DisplayName = $"Коллектор №{collectorData.CollectorNumber} ({collectorData.Circuits?.Count ?? 0} {GetContourWord(collectorData.Circuits?.Count ?? 0)})",
                    CircuitCount = collectorData.Circuits?.Count ?? 0,
                    TotalFlowRate = (collectorData.Summary?.TotalFlowRate ?? 0) / 1000.0,
                    IsSelected = (i == 0) // Первый коллектор выбран по умолчанию
                });
            }

            return collectorInfos;
        }

        /// <summary>
        /// Собрать сводку выбранного коллектора из канонического снапшота
        /// (DE-3: вынос CreateCollectorSummary из ResultsViewModel).
        /// </summary>
        public CollectorSummary? BuildCollectorSummary(HydraulicCollectorSnapshot collector)
        {
            var summary = collector.Summary;
            if (summary == null) return null;

            return new CollectorSummary
            {
                CollectorNumber = collector.CollectorNumber,
                CollectorType = summary.CollectorType,
                CircuitCount = summary.CircuitCount,
                TotalPipeLength = summary.TotalPipeLength,
                TotalPower = summary.TotalPower,
                TotalFlowRate = summary.TotalFlowRate,
                PressureLoss_Operating_Pa = summary.PressureLoss_Operating_Pa,
                PressureLoss_Cold_Pa = summary.PressureLoss_Cold_Pa,
                Kv = summary.Kv
            };
        }

        /// <summary>
        /// Получить правильное склонение слова "контур" (перенос из ResultsViewModel
        /// дословно; рядом существует FormatCircuitCount — объединение отложено:
        /// формы для 21–24, 31–34 и т.п. у них различаются).
        /// </summary>
        private static string GetContourWord(int count)
        {
            if (count % 100 >= 11 && count % 100 <= 19)
                return "контуров";
            int lastDigit = count % 10;
            return lastDigit switch
            {
                1 => "контур",
                2 or 3 or 4 => "контура",
                _ => "контуров"
            };
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
