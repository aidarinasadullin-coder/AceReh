using System.Collections.Generic;
using System.Linq;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Генерация метаданных параметров и формул для гидравлического раздела отчёта.
    /// </summary>
    public static class HydraulicsReportMetadataBuilder
    {
        public const string FormulaStatusUnconfirmed = "требуется привязка к существующей формуле";

        public static IReadOnlyList<ReportParameterMetadata> BuildMetadata(HydraulicsSection section)
        {
            var metadata = new List<ReportParameterMetadata>();
            AddScalarMetadata(metadata, section);

            foreach (var collector in section.Collectors)
            {
                AddCollectorMetadata(metadata, collector);
                foreach (var circuit in collector.Circuits)
                {
                    AddCircuitMetadata(metadata, collector, circuit);
                }
            }

            return metadata;
        }

        public static IReadOnlyList<ReportFormula> BuildFormulas()
        {
            return new List<ReportFormula>
            {
                Formula("S", "L_HK * VAHK / 100", "CircuitRow.cs:272", "Hydraulics"),
                Formula("L_total", "L_HK + L_Zul", "CircuitRow.cs / ProjectData", "Hydraulics"),
                Formula("Q_HK", "[L_HK/(100/VAHK) + L_Zul/(100/VAZul) * (qZul/100)] * (q_up + q_down)", "CircuitsCalculator.cs:34-37", "Hydraulics"),
                Formula("V_dot", "Q_HK * 3.6 / (rho * c_p * DeltaT)", "CircuitsCalculator.cs:56-57", "Hydraulics"),
                Formula("v", "V_dot * 4000 / (3600 * pi * d_inner^2)", "CircuitsCalculator.cs:89", "Hydraulics"),
                Formula("d_inner", "d_ext - 2 * s", "PipeTypeProjectData / docs", "Hydraulics"),
                Formula("Re", "1000 * v * d_inner / nu", "CircuitsCalculator.cs:92", "Hydraulics"),
                Formula("lambda_lam", "64 / Re", "FlowRegimeCalculator.cs:89-95", "Hydraulics"),
                Formula("lambda_trans", "interpolation 64/2300 .. Colebrook-White at Re=4000", "FlowRegimeCalculator.cs:106-125", "Hydraulics"),
                Formula("lambda_turb", "Colebrook-White iterative", "FlowRegimeCalculator.cs:136-167", "Hydraulics"),
                Formula("R", "10000 * v^2 * rho * lambda / (2 * d_inner) * 100", "CircuitsCalculator.cs:101-102", "Hydraulics"),
                Formula("DpRohr", "(L_HK + L_Zul) * R", "CircuitsCalculator.cs:105", "Hydraulics"),
                Formula("DpVerteiler_HKV", "(FlowRate/1000/1.2)^2 * 100000 * rho", "CircuitsCalculator.cs:110", "Hydraulics"),
                Formula("DpVent_HKV", "15000 * (rho/2) * v^2", "CircuitsCalculator.cs:111", "Hydraulics"),
                Formula("DpVerteiler_IV", "15000 * (rho/2) * v^2", "CircuitsCalculator.cs:115", "Hydraulics"),
                Formula("DpVent_IV", "(FlowRate/1000/Kv)^2 * 100000 * rho", "CircuitsCalculator.cs:116", "Hydraulics"),
                Formula("DpGesamt", "DpRohr + DpVerteiler + DpVent", "CircuitTemperatureResult.cs / CircuitsCalculator", "Hydraulics"),
                Formula("Dp_max", "max(DpGesamt)", "CircuitsCalculator.cs:265-266", "Hydraulics"),
                Formula("zu_drosseln_HKV", "maxDp - (DpRohr + DpVent)", "CircuitsCalculator.cs:215", "Hydraulics"),
                Formula("zu_drosseln_IV", "maxDp - (DpRohr + DpVerteiler)", "CircuitsCalculator.cs:219", "Hydraulics"),
                Formula("Kv_default", "HKV-D=1.2, IV 1¼=1.45, IV 1½=1.5", "ValveTurnsCalculator.cs:155-163", "Hydraulics"),
                Formula("turns_HKV", "4.2111*Kv^3 - 6.7436*Kv^2 + 4.6613*Kv - 0.712", "ValveTurnsCalculator.cs:257-262", "Hydraulics"),
                Formula("turns_IV_1_25", "5.1818 * Kv - 0.23", "ValveTurnsCalculator.cs:248-251", "Hydraulics"),
                Formula("turns_IV_1_5", "5.122 * Kv - 0.2106", "ValveTurnsCalculator.cs:239-242", "Hydraulics")
            };
        }

        private static void AddScalarMetadata(List<ReportParameterMetadata> metadata, HydraulicsSection section)
        {
            metadata.Add(Meta("Тип гликоля", "-", "Тип используемого гликоля", section.GlycolType));
            metadata.Add(Meta("Концентрация гликоля", "-", "Концентрация гликоля", section.GlycolConcentration));
            metadata.Add(Meta("Плотность", "rho", "Плотность теплоносителя", section.Density));
            metadata.Add(Meta("Удельная теплоёмкость", "c_p", "Удельная теплоёмкость теплоносителя", section.SpecificHeat));
            metadata.Add(Meta("Кинематическая вязкость", "nu", "Кинематическая вязкость теплоносителя", section.KinematicViscosity));
            metadata.Add(Meta("Теплопроводность теплоносителя", "lambda_fluid", "Теплопроводность теплоносителя", "Вт/(м·К)", ReportValueSource.ProgramDatabase, "GlycolProperties.ThermalConductivity", "условно: доступна в GlycolProperties, но не сохраняется в результате", "HydraulicsSection"));
            metadata.Add(Meta("Число Прандтля", "Pr", "Число Прандтля", "-", ReportValueSource.ProgramDatabase, "GlycolProperties.PrandtlNumber", FormulaStatusUnconfirmed, "HydraulicsSection"));
            metadata.Add(Meta("Температура замерзания", "-", "Температура замерзания теплоносителя", "°C", ReportValueSource.ProgramDatabase, "GlycolProperties", "недоступно в текущем коде", "HydraulicsSection"));
        }

        private static void AddCollectorMetadata(List<ReportParameterMetadata> metadata, ReportCollector collector)
        {
            metadata.Add(Meta("Тип коллектора", "-", "Тип коллектора", collector.Summary.CollectorType));
            metadata.Add(Meta("Количество контуров коллектора", "-", "Количество активных контуров", collector.Summary.CircuitCount));
            metadata.Add(Meta("Суммарная мощность коллектора", "-", "Суммарная мощность контуров коллектора", collector.Summary.TotalPower));
            metadata.Add(Meta("Суммарный расход коллектора", "-", "Суммарный расход контуров коллектора", collector.Summary.TotalFlowRate));
            metadata.Add(Meta("Потери давления коллектора", "Dp_max", "Максимальные потери давления в коллекторе", collector.Summary.PressureLoss));
            metadata.Add(Meta("Kv коллектора/клапана", "Kv", "Коэффициент пропускной способности клапана", collector.Summary.Kv));
        }

        private static void AddCircuitMetadata(List<ReportParameterMetadata> metadata, ReportCollector collector, ReportCircuit circuit)
        {
            metadata.Add(Meta("Номер контура", "-", "Номер контура", circuit.CircuitNumber, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Длина греющего участка", "L_HK", "Длина греющего участка контура", circuit.CircuitLength, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Площадь контура", "S", "Площадь контура", circuit.CircuitArea, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Длина подводки", "L_Zul", "Длина подводки", circuit.SupplyLength, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Общая длина контура", "L_total", "Общая длина контура", circuit.TotalLength, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Шаг укладки", "VAHK", "Шаг укладки трубы", circuit.PipeSpacing, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Шаг подводки", "VAZul", "Шаг подводки", circuit.SupplySpacing, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Доля тепла подводки", "qZul", "Доля тепла подводки", circuit.SupplyHeatPercent, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Мощность контура", "Q_HK", "Мощность контура", circuit.Power, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Расход теплоносителя", "V_dot", "Объёмный расход теплоносителя в контуре", circuit.FlowRate, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Скорость потока", "v", "Скорость потока", circuit.Velocity, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Плотность", "rho", "Плотность теплоносителя", circuit.Density, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Кинематическая вязкость", "nu", "Кинематическая вязкость", circuit.KinematicViscosity, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Число Рейнольдса", "Re", "Число Рейнольдса", circuit.ReynoldsNumber, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Коэффициент трения", "lambda", "Коэффициент трения", circuit.FrictionFactor, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Удельные потери давления", "R", "Удельные потери давления", circuit.PressureLossPerMeter, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Потери в трубе", "DpRohr", "Потери давления в трубе", circuit.DpRohr, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Потери в распределителе", "DpVerteiler", "Потери давления в распределителе", circuit.DpVerteiler, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Потери в вентиле", "DpVent", "Потери давления в вентиле", circuit.DpVent, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Суммарные потери контура", "DpGesamt", "Суммарные потери давления контура", circuit.DpGesamt, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Дросселирование", "zu_drosseln", "Дросселирование для балансировки", circuit.Throttling, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("ZuDrosseln", "ZuDrosseln", "Значение ZuDrosseln", circuit.ZuDrosseln, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Обороты клапана", "-", "Обороты балансировочного клапана", circuit.ValveTurns, "HydraulicsSection.Collectors.Circuits"));
            metadata.Add(Meta("Режим течения", "-", "Режим течения теплоносителя", circuit.FlowRegime, "HydraulicsSection.Collectors.Circuits"));
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<double> value)
        {
            return Meta(name, symbol, physicalMeaning, value, "HydraulicsSection");
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<double> value, string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = value.Formula ?? value.FormulaStatus,
                FormulaSource = "HydraulicsSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = whereUsed
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<string> value)
        {
            return Meta(name, symbol, physicalMeaning, value, "HydraulicsSection");
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<string> value, string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = value.Formula ?? value.FormulaStatus,
                FormulaSource = "HydraulicsSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = whereUsed
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, int value, string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = "-",
                Source = ReportValueSource.Project,
                SourceDetail = "CircuitProjectData.CircuitNumber",
                Formula = null,
                FormulaSource = "HydraulicsSectionBuilder",
                WhereCalculated = "CircuitProjectData.CircuitNumber",
                WhereUsed = whereUsed
            };
        }

        private static ReportParameterMetadata Meta(
            string name,
            string symbol,
            string physicalMeaning,
            string unit,
            ReportValueSource source,
            string sourceDetail,
            string formula,
            string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = unit,
                Source = source,
                SourceDetail = sourceDetail,
                Formula = formula,
                FormulaSource = "HydraulicsSectionBuilder",
                WhereCalculated = sourceDetail,
                WhereUsed = whereUsed
            };
        }

        private static ReportFormula Formula(string symbol, string expression, string sourcePath, string section)
        {
            return new ReportFormula
            {
                Symbol = symbol,
                Expression = expression,
                SourcePath = sourcePath,
                Section = section
            };
        }
    }
}
