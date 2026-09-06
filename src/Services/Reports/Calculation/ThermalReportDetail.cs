using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Источник детальных тепловых величин отчёта (ADR-010).
    /// </summary>
    public enum ThermalReportDetailSource
    {
        /// <summary>Полный набор величин из канонического снимка (расчёт в этой сессии).</summary>
        Snapshot,

        /// <summary>Величины получены контрольным пересчётом по текущим входам.</summary>
        Recalculated,

        /// <summary>Контрольный пересчёт не дал валидного результата (IsValid=false или исключение).</summary>
        RecalculationInvalid,

        /// <summary>Детальные величины недоступны (резервное состояние).</summary>
        Missing
    }

    /// <summary>
    /// Детальные тепловые величины для отчёта: α, составляющие мощности,
    /// RFb/RD, m, ηR, JHmü, расходы — всё, что не сохраняется в wire-наборе
    /// <c>.smc</c> (DEC-T08). Источник фиксируется явно (снимок сессии или
    /// контрольный пересчёт, ADR-010 п.1).
    /// </summary>
    public sealed class ThermalReportDetail
    {
        /// <summary>Откуда получены величины.</summary>
        public ThermalReportDetailSource Source { get; init; }

        /// <summary>
        /// Результат сохранён, но входы изменились (фаза NeedsRecalculation) —
        /// предупреждение REPORT_INPUTS_STALE. При успешном контрольном
        /// пересчёте по текущим входам не выставляется (ADR-010, приоритет правил).
        /// </summary>
        public bool IsStale { get; init; }

        /// <summary>Примечание к тепловому разделу (пересчёт, расхождение, теплоноситель).</summary>
        public string? Note { get; init; }

        /// <summary>Ошибки валидации пересчёта (для RecalculationInvalid) или последнего расчёта.</summary>
        public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

        /// <summary>Величины пригодны для вывода (Snapshot или успешный Recalculated).</summary>
        public bool HasValues =>
            Source == ThermalReportDetailSource.Snapshot ||
            Source == ThermalReportDetailSource.Recalculated;

        /// <summary>Коэффициент теплоотдачи α, Вт/(м²·К).</summary>
        public double Alpha { get; init; }

        /// <summary>Мощность на плавление снега, Вт/м².</summary>
        public double MeltingHeat { get; init; }

        /// <summary>Лучистый тепловой поток (справочно), Вт/м².</summary>
        public double RadiationHeat { get; init; }

        /// <summary>Конвективный тепловой поток, Вт/м².</summary>
        public double ConvectionHeat { get; init; }

        /// <summary>Избыточная температура JHmü, К.</summary>
        public double ExcessTemperature { get; init; }

        /// <summary>Полное сопротивление вверх RFb, м²·К/Вт.</summary>
        public double RFb { get; init; }

        /// <summary>Полное сопротивление вниз RD, м²·К/Вт.</summary>
        public double RD { get; init; }

        /// <summary>Параметр затухания m, 1/м.</summary>
        public double ParameterM { get; init; }

        /// <summary>КПД ребра ηR, —.</summary>
        public double EfficiencyEtaR { get; init; }

        /// <summary>Массовый расход на м², кг/(ч·м²).</summary>
        public double MassFlowRate { get; init; }

        /// <summary>Объёмный расход на м², л/(ч·м²).</summary>
        public double VolumeFlowRate { get; init; }
    }
}
