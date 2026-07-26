using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Строитель read-model итогов гидравлики: карточки по коллекторам,
    /// спецификации и сгруппированное оборудование.
    /// Чистый маппинг состояния коллекторов — без собственного состояния.
    /// Вынесен из ResultsViewModel (архитектурный долг, этап C3).
    /// </summary>
    public class HydraulicSummaryBuilder
    {
        /// <summary>
        /// Построить канонический read-model карточек итогов гидравлики по всем коллекторам.
        /// </summary>
        public List<CollectorHydraulicSummaryCard> BuildSummaryCards(IEnumerable<CollectorData>? collectors)
        {
            var cards = new List<CollectorHydraulicSummaryCard>();
            if (collectors == null) return cards;

            foreach (var collector in collectors)
            {
                if (collector == null) continue;
                cards.Add(new CollectorHydraulicSummaryCard(collector));
            }

            return cards;
        }

        /// <summary>
        /// Построить спецификации коллекторов
        /// </summary>
        public List<CollectorSpecification> BuildSpecifications(IEnumerable<CollectorData>? collectors, bool isOperatingMode)
        {
            var specifications = new List<CollectorSpecification>();
            if (collectors == null) return specifications;

            foreach (var collector in collectors)
            {
                if (collector?.Summary == null) continue;

                specifications.Add(new CollectorSpecification
                {
                    Number = collector.CollectorNumber,
                    Type = collector.CollectorTypeDisplayWithCount,
                    CircuitCount = collector.Circuits?.Count ?? 0,
                    TotalPower_kW = collector.Summary.TotalPower / 1000.0,
                    TotalFlowRate_m3h = collector.Summary.TotalFlowRate_m3h,
                    PressureLoss_mbar = isOperatingMode
                        ? collector.Summary.PressureLoss_Operating_mbar
                        : collector.Summary.PressureLoss_Cold_mbar,
                    Kv = collector.Summary.Kv
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
        public List<CollectorEquipmentItem> BuildEquipmentItems(IEnumerable<CollectorData>? collectors)
        {
            var items = new List<CollectorEquipmentItem>();
            if (collectors == null) return items;

            var groupMap = new Dictionary<(ValveType ValveType, int CircuitCount), CollectorEquipmentItem>();
            var orderedGroups = new List<CollectorEquipmentItem>();

            foreach (var collector in collectors)
            {
                if (collector == null) continue;

                int circuitCount = collector.Circuits?.Count ?? 0;
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
                        Type = collector.CollectorTypeDisplayWithCount,
                        CollectorQuantity = 1
                    };

                    groupMap[key] = newItem;
                    orderedGroups.Add(newItem);
                }
            }

            items.AddRange(orderedGroups);
            return items;
        }
    }
}
