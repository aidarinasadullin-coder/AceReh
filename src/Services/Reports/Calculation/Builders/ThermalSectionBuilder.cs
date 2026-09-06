using System.Collections.Generic;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела теплотехнического расчёта.
    /// </summary>
    public sealed class ThermalSectionBuilder : IReportSectionBuilder<ThermalSection>
    {
        private const string FormulaStatusNotStored = "не сохраняется в ProjectData";
        private const string FormulaStatusReference = "справочно, не включается в PowerUp";
        private const string FormulaStatusUnconfirmed = "требуется привязка к существующей формуле";
        private const string FormulaStatusConstant = "кодовое значение";

        public SectionBuildResult<ThermalSection> Build(ProjectData project, CalculationReportMode mode, ThermalReportDetail? thermalDetail = null)
        {
            var thermal = project.ThermalData ?? new ThermalProjectData();
            var result = thermal.Result ?? new ThermalResultProjectData();
            var detail = thermalDetail is { HasValues: true } ? thermalDetail : null;

            var alpha = ReportValueFactory.Create(detail?.Alpha ?? 0.0, "Вт/(м²·К)", ReportValueSource.Calculated, "ThermalCalculationResult.Alpha", formula: "2.26 * (t_P - t_H)^0.33 + 2.6 * v_H", formulaStatus: FormulaStatusNotStored);
            var meltingHeat = ReportValueFactory.Create(detail?.MeltingHeat ?? 0.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.MeltingHeat", formula: "(h/3600) * rho * (c_ice*(0-t_H) + L_melt + c_water*t_P)", formulaStatus: FormulaStatusNotStored);
            var radiationHeat = ReportValueFactory.Create(detail?.RadiationHeat ?? 0.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.RadiationHeat", formulaStatus: FormulaStatusReference);
            var convectionHeat = ReportValueFactory.Create(detail?.ConvectionHeat ?? 0.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.ConvectionHeat", formula: "alpha * (t_P - t_H)", formulaStatus: FormulaStatusNotStored);
            var powerUp = ReportValueFactory.Create(result.PowerUp, "Вт/м²", ReportValueSource.Calculated, "ThermalResultProjectData.PowerUp", formula: "MeltingHeat + ConvectionHeat");
            var powerDown = ReportValueFactory.Create(result.PowerDown, "Вт/м²", ReportValueSource.Calculated, "ThermalResultProjectData.PowerDown", formulaStatus: FormulaStatusUnconfirmed);
            var totalPowerDensity = ReportValueFactory.Create(result.PowerTotal, "Вт/м²", ReportValueSource.Calculated, "ThermalResultProjectData.PowerTotal", formula: "PowerUp + PowerDown");
            var rFb = ReportValueFactory.Create(detail?.RFb ?? 0.0, "м²·К/Вт", ReportValueSource.Calculated, "ThermalCalculationResult.RFb", formula: "R1 + 1/alpha", formulaStatus: FormulaStatusNotStored);
            var rD = ReportValueFactory.Create(detail?.RD ?? 0.0, "м²·К/Вт", ReportValueSource.Calculated, "ThermalCalculationResult.RD", formula: "R2 + 1/AlphaBottom", formulaStatus: FormulaStatusNotStored);
            var parameterM = ReportValueFactory.Create(detail?.ParameterM ?? 0.0, "1/м", ReportValueSource.Calculated, "ThermalCalculationResult.ParameterM", formula: "0.6 * sqrt((1/RFb + 1/RD) / (lambdaE * dE))", formulaStatus: FormulaStatusNotStored);
            var efficiencyEtaR = ReportValueFactory.Create(detail?.EfficiencyEtaR ?? 0.0, "-", ReportValueSource.Calculated, "ThermalCalculationResult.EfficiencyEtaR", formula: "tanh(x)/x", formulaStatus: FormulaStatusNotStored);
            var excessTemperature = ReportValueFactory.Create(detail?.ExcessTemperature ?? 0.0, "K", ReportValueSource.Calculated, "ThermalCalculationResult.ExcessTemperature", formulaStatus: FormulaStatusUnconfirmed);
            var massFlowRate = ReportValueFactory.Create(detail?.MassFlowRate ?? 0.0, "кг/(ч·м²)", ReportValueSource.Calculated, "ThermalCalculationResult.MassFlowRate", formula: "PowerTotal / (c_p / 3.6) / DeltaT", formulaStatus: FormulaStatusNotStored);
            var volumeFlowRate = ReportValueFactory.Create(detail?.VolumeFlowRate ?? 0.0, "л/(ч·м²)", ReportValueSource.Calculated, "ThermalCalculationResult.VolumeFlowRate", formula: "MassFlowRate / rho * 1000", formulaStatus: FormulaStatusNotStored);
            var snowDensity = ReportValueFactory.Create(900.0, "кг/м³", ReportValueSource.Calculated, "ThermalCalculator.SnowDensity", formulaStatus: FormulaStatusConstant);
            var iceHeatCapacity = ReportValueFactory.Create(2100.0, "Дж/(кг·К)", ReportValueSource.Calculated, "ThermalCalculator.IceHeatCapacity", formulaStatus: FormulaStatusConstant);
            var iceMeltingHeat = ReportValueFactory.Create(330000.0, "Дж/кг", ReportValueSource.Calculated, "ThermalCalculator.IceMeltingHeat", formulaStatus: FormulaStatusConstant);
            var waterHeatCapacity = ReportValueFactory.Create(4200.0, "Дж/(кг·К)", ReportValueSource.Calculated, "ThermalCalculator.WaterHeatCapacity", formulaStatus: FormulaStatusConstant);

            var section = new ThermalSection
            {
                Alpha = alpha,
                MeltingHeat = meltingHeat,
                RadiationHeat = radiationHeat,
                ConvectionHeat = convectionHeat,
                PowerUp = powerUp,
                PowerDown = powerDown,
                TotalPowerDensity = totalPowerDensity,
                RFb = rFb,
                RD = rD,
                ParameterM = parameterM,
                EfficiencyEtaR = efficiencyEtaR,
                ExcessTemperature = excessTemperature,
                MassFlowRate = massFlowRate,
                VolumeFlowRate = volumeFlowRate,
                SnowDensity = snowDensity,
                IceHeatCapacity = iceHeatCapacity,
                IceMeltingHeat = iceMeltingHeat,
                WaterHeatCapacity = waterHeatCapacity
            };

            var metadata = new List<ReportParameterMetadata>
            {
                Meta("Коэффициент теплоотдачи", "alpha", "Коэффициент теплоотдачи на поверхности", alpha),
                Meta("Мощность на плавление снега", "Q_таяние", "Мощность, затрачиваемая на плавление снега", meltingHeat),
                Meta("Лучистый тепловой поток", "Q_изл", "Лучистый тепловой поток (справочно, не включается в PowerUp)", radiationHeat),
                Meta("Конвективный тепловой поток", "Q_конв", "Конвективный тепловой поток", convectionHeat),
                Meta("Полезная мощность вверх", "PowerUp", "Суммарная мощность вверх", powerUp),
                Meta("Мощность вниз", "PowerDown", "Мощность вниз (потери)", powerDown),
                Meta("Суммарная удельная мощность", "TotalPowerDensity", "Суммарная удельная мощность", totalPowerDensity),
                Meta("Сопротивление вверх", "RFb", "Полное тепловое сопротивление вверх", rFb),
                Meta("Сопротивление вниз", "RD", "Полное тепловое сопротивление вниз", rD),
                Meta("Параметр затухания", "m", "Параметр затухания теории стержня", parameterM),
                Meta("КПД ребра", "etaR", "КПД ребра", efficiencyEtaR),
                Meta("Избыточная температура", "JHmu", "Избыточная температура теплоносителя", excessTemperature),
                Meta("Массовый расход на м²", "m_dot", "Массовый расход теплоносителя на м²", massFlowRate),
                Meta("Объёмный расход на м²", "V_dot_m2", "Объёмный расход теплоносителя на м²", volumeFlowRate),
                Meta("Плотность снега", "rho_snow", "Плотность снега", snowDensity),
                Meta("Теплоёмкость льда", "c_ice", "Удельная теплоёмкость льда", iceHeatCapacity),
                Meta("Теплота плавления льда", "L_melt", "Удельная теплота плавления льда", iceMeltingHeat),
                Meta("Теплоёмкость воды", "c_water", "Удельная теплоёмкость воды", waterHeatCapacity),
                Meta("Коэффициент A", "A", "Промежуточный коэффициент A", "-", "A = 1 / etaR", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator.cs:329", "ThermalSection"),
                Meta("Коэффициент B", "B", "Промежуточный коэффициент B", "-", "B = 1/RFb + 1/RD", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator.cs:332", "ThermalSection"),
                Meta("Коэффициент C", "C", "Промежуточный коэффициент C", "-", "C = abs(t_H - t_G)", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator.cs:335", "ThermalSection"),
                Meta("Коэффициент D", "D", "Промежуточный коэффициент D", "-", "D = lR / (pi * lambdaR)", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator.cs:342", "ThermalSection"),
                Meta("Коэффициент E", "E", "Промежуточный коэффициент E", "-", "E = s / (d - s)", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator.cs:349", "ThermalSection"),
                Meta("Аргумент КПД ребра", "x", "Аргумент для расчёта etaR", "-", "x = m * spacing / 2", "ThermalCalculator.CalculateRodTheory", "ThermalCalculator.cs:240", "ThermalSection")
            };

            var formulas = new List<ReportFormula>
            {
                Formula("alpha", "2.26 * (t_P - t_H)^0.33 + 2.6 * v_H", "ThermalCalculator.cs:94", "Thermal"),
                Formula("Q_таяние", "(h/3600) * rho * (c_ice*(0-t_H) + L_melt + c_water*t_P)", "ThermalCalculator.cs:135-139", "Thermal"),
                Formula("Q_конв", "alpha * (t_P - t_H)", "ThermalCalculator.cs:143", "Thermal"),
                Formula("Q_изл", "epsilon * sigma * (273 + t_P)^4", "ThermalCalculator.cs:534-535", "Thermal"),
                Formula("PowerUp", "Q_таяние + Q_конв", "ThermalCalculator.cs:147", "Thermal"),
                Formula("RFb", "R1 + 1/alpha", "ThermalCalculator.cs:182", "Thermal"),
                Formula("RD", "R2 + 1/AlphaBottom", "ThermalCalculator.cs:185", "Thermal"),
                Formula("m", "0.6 * sqrt((1/RFb + 1/RD) / (lambdaE * dE))", "ThermalCalculator.cs:237", "Thermal"),
                Formula("x", "m * spacing / 2", "ThermalCalculator.cs:240", "Thermal"),
                Formula("etaR", "tanh(x) / x", "ThermalCalculator.cs:244-256", "Thermal"),
                Formula("A", "1 / etaR", "ThermalCalculator.cs:329", "Thermal"),
                Formula("B", "1/RFb + 1/RD", "ThermalCalculator.cs:332", "Thermal"),
                Formula("C", "abs(t_H - t_G)", "ThermalCalculator.cs:335", "Thermal"),
                Formula("D", "lR / (pi * lambdaR)", "ThermalCalculator.cs:342", "Thermal"),
                Formula("E", "s / (d - s)", "ThermalCalculator.cs:349", "Thermal"),
                Formula("JHmu", "[A + (B - C/(PowerUp * RFb * RD)) * D * E] * PowerUp * RFb", "ThermalCalculator.cs:354", "Thermal"),
                Formula("PowerDown", "(JHmu_low * RFb + C * D * E) / (RFb * RD * (A + B * D * E))", "ThermalCalculator.cs:418-422", "Thermal"),
                Formula("TotalPowerDensity", "PowerUp + PowerDown", "ThermalCalculator.cs:568", "Thermal"),
                Formula("m_dot", "PowerTotal / (c_p / 3.6) / DeltaT", "ThermalCalculator.cs:577", "Thermal"),
                Formula("V_dot_m2", "MassFlowRate / rho * 1000", "ThermalCalculator.cs:580", "Thermal")
            };

            return new SectionBuildResult<ThermalSection>
            {
                Section = section,
                ParameterMetadata = metadata,
                Formulas = formulas
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<double> value)
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
                FormulaSource = "ThermalSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = "ThermalSection"
            };
        }

        private static ReportParameterMetadata Meta(
            string name,
            string symbol,
            string physicalMeaning,
            string unit,
            string formula,
            string whereCalculated,
            string formulaSource,
            string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = unit,
                Source = ReportValueSource.Calculated,
                SourceDetail = whereCalculated,
                Formula = formula,
                FormulaSource = formulaSource,
                WhereCalculated = whereCalculated,
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
