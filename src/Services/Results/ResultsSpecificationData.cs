// ================================================================================
// REHAU Снеготаяние - Read-model экспорта спецификации закупки в Excel
// ================================================================================
//
// Назначение: данные для выгрузки .xlsx из шага «Результаты» (план
// docs/plans/2026-09-13-excel-specification-plan.md)
//
// Соответствует: docs/plans/2026-09-13-excel-specification-plan.md
// - Лист «Спецификация»: секции «Труба», «Резьбозажимные соединения»,
//   «Коллекторы HKV», «Итого по проекту»
// - Лист «Контуры»: наладка по контурам
// Собирается ResultsSpecificationDataBuilder из канонических снимков
// ProjectSession; числовые значения хранятся числами (double/int) —
// строковое форматирование выполняет ExcelExportService.
//
// ================================================================================

using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Строка листа «Спецификация» — позиция закупки (труба, РЗС, коллектор, итого).
    /// </summary>
    public sealed class SpecificationRow
    {
        /// <summary>Имя секции («Труба», «Резьбозажимные соединения», «Коллекторы HKV», «Итого по проекту»).</summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>Наименование позиции.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Артикул; пустая строка — артикул не подтверждён (см. Notes).</summary>
        public string Article { get; set; } = string.Empty;

        /// <summary>Единица измерения («м», «шт», «кВт»).</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>Количество в указанных единицах.</summary>
        public double Quantity { get; set; }

        /// <summary>Примечание (например «артикул уточняется» для РЗС 25×2,3).</summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Строка листа «Контуры» — наладка одного контура. Единицы канонические:
    /// длины в м, мощность Вт, расход л/ч, потери давления и преднастройка в Па
    /// (перевод в отображаемые единицы — забота ExcelExportService).
    /// </summary>
    public sealed class SpecificationCircuitRow
    {
        public int CollectorNumber { get; set; }
        public string CollectorType { get; set; } = string.Empty;
        public int CircuitNumber { get; set; }
        public double LoopLength_m { get; set; }
        public double SupplyLength_m { get; set; }
        public double Area_m2 { get; set; }
        public double Power_W { get; set; }
        public double FlowRate_lh { get; set; }
        public double PressureLoss_Pa { get; set; }
        public double ValveTurns { get; set; }
        public double ZuDrosseln_Pa { get; set; }
    }

    /// <summary>
    /// Данные экспорта спецификации закупки. Итоги дублируются полями
    /// (Total*) для шапки листа «Контуры» и проверки сходимости.
    /// </summary>
    public sealed class ResultsSpecificationData
    {
        public string ProjectNumber { get; set; } = string.Empty;
        public string ProjectObject { get; set; } = string.Empty;
        public List<SpecificationRow> Rows { get; } = new();
        public List<SpecificationCircuitRow> CircuitRows { get; } = new();
        public int TotalCircuits { get; set; }
        public double TotalPipeLength_m { get; set; }
        public double TotalPower_kW { get; set; }
    }
}
