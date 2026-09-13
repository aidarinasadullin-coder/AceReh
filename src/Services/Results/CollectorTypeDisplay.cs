// ================================================================================
// REHAU Снеготаяние - Единый форматтер имени коллектора
// ================================================================================
//
// Назначение: один источник правил отображения типа коллектора (урок №22:
// одна производная — один источник правил). Используется HydraulicSummaryBuilder
// (карточки/спецификации PDF) и ResultsSpecificationDataBuilder (лист
// «Контуры» экспорта спецификации).
//
// ================================================================================

using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Results
{
    public static class CollectorTypeDisplay
    {
        /// <summary>«HKV-D», «IV 1¼"», «IV 1½"».</summary>
        public static string FormatName(ValveType valveType) => valveType switch
        {
            ValveType.HKV_D => "HKV-D",
            ValveType.IV_1_25 => "IV 1¼\"",
            ValveType.IV_1_5 => "IV 1½\"",
            _ => "Unknown"
        };

        /// <summary>«1 контур», «2 контура», … «5 контуров».</summary>
        public static string FormatCircuitCount(int count) => count switch
        {
            1 => "1 контур",
            2 or 3 or 4 => $"{count} контура",
            _ => $"{count} контуров"
        };

        /// <summary>«HKV-D (6 контуров)».</summary>
        public static string Format(ValveType valveType, int circuitCount) =>
            $"{FormatName(valveType)} ({FormatCircuitCount(circuitCount)})";
    }
}
