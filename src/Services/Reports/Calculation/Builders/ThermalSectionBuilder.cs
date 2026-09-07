using System.Collections.Generic;
using SnowMeltingCalculator.Models.Climate;
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

            var alpha = ReportValueFactory.Create(detail?.Alpha ?? 0.0, "Вт/(м²·К)", ReportValueSource.Calculated, "ThermalCalculationResult.Alpha", decimals: ReportDecimals.For("Вт/(м²·К)"), formula: "2.26 * (t_P - t_H)^0.33 + 2.6 * v_H", formulaStatus: FormulaStatusNotStored);
            var meltingHeat = ReportValueFactory.Create(detail?.MeltingHeat ?? 0.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.MeltingHeat", decimals: ReportDecimals.For("Вт/м²"), formula: "(h/3600) * rho * (c_ice*(0-t_H) + L_melt + c_water*t_P)", formulaStatus: FormulaStatusNotStored);
            var radiationHeat = ReportValueFactory.Create(detail?.RadiationHeat ?? 0.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.RadiationHeat", decimals: ReportDecimals.For("Вт/м²"), formulaStatus: FormulaStatusReference);
            var convectionHeat = ReportValueFactory.Create(detail?.ConvectionHeat ?? 0.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.ConvectionHeat", decimals: ReportDecimals.For("Вт/м²"), formula: "alpha * (t_P - t_H)", formulaStatus: FormulaStatusNotStored);
            var powerUp = ReportValueFactory.Create(result.PowerUp, "Вт/м²", ReportValueSource.Calculated, "ThermalResultProjectData.PowerUp", decimals: ReportDecimals.For("Вт/м²"), formula: "MeltingHeat + ConvectionHeat");
            var powerDown = ReportValueFactory.Create(result.PowerDown, "Вт/м²", ReportValueSource.Calculated, "ThermalResultProjectData.PowerDown", decimals: ReportDecimals.For("Вт/м²"), formulaStatus: FormulaStatusUnconfirmed);
            var totalPowerDensity = ReportValueFactory.Create(result.PowerTotal, "Вт/м²", ReportValueSource.Calculated, "ThermalResultProjectData.PowerTotal", decimals: ReportDecimals.For("Вт/м²"), formula: "PowerUp + PowerDown");
            var rFb = ReportValueFactory.Create(detail?.RFb ?? 0.0, "м²·К/Вт", ReportValueSource.Calculated, "ThermalCalculationResult.RFb", decimals: ReportDecimals.For("м²·К/Вт"), formula: "R1 + 1/alpha", formulaStatus: FormulaStatusNotStored);
            var rD = ReportValueFactory.Create(detail?.RD ?? 0.0, "м²·К/Вт", ReportValueSource.Calculated, "ThermalCalculationResult.RD", decimals: ReportDecimals.For("м²·К/Вт"), formula: "R2 + 1/AlphaBottom", formulaStatus: FormulaStatusNotStored);
            var parameterM = ReportValueFactory.Create(detail?.ParameterM ?? 0.0, "1/м", ReportValueSource.Calculated, "ThermalCalculationResult.ParameterM", decimals: ReportDecimals.For("1/м"), formula: "0.6 * sqrt((1/RFb + 1/RD) / (lambdaE * dE))", formulaStatus: FormulaStatusNotStored);
            var efficiencyEtaR = ReportValueFactory.Create(detail?.EfficiencyEtaR ?? 0.0, "-", ReportValueSource.Calculated, "ThermalCalculationResult.EfficiencyEtaR", decimals: 3, formula: "tanh(x)/x", formulaStatus: FormulaStatusNotStored);
            var excessTemperature = ReportValueFactory.Create(detail?.ExcessTemperature ?? 0.0, "K", ReportValueSource.Calculated, "ThermalCalculationResult.ExcessTemperature", decimals: ReportDecimals.For("K"), formulaStatus: FormulaStatusUnconfirmed);
            var massFlowRate = ReportValueFactory.Create(detail?.MassFlowRate ?? 0.0, "кг/(ч·м²)", ReportValueSource.Calculated, "ThermalCalculationResult.MassFlowRate", decimals: ReportDecimals.For("кг/(ч·м²)"), formula: "PowerTotal / (c_p / 3.6) / DeltaT", formulaStatus: FormulaStatusNotStored);
            var volumeFlowRate = ReportValueFactory.Create(detail?.VolumeFlowRate ?? 0.0, "л/(ч·м²)", ReportValueSource.Calculated, "ThermalCalculationResult.VolumeFlowRate", decimals: ReportDecimals.For("л/(ч·м²)"), formula: "MassFlowRate / rho * 1000", formulaStatus: FormulaStatusNotStored);
            var snowDensity = ReportValueFactory.Create(Core.Constants.ThermalConstants.SnowDensity, "кг/м³", ReportValueSource.Calculated, "ThermalCalculator.SnowDensity", decimals: ReportDecimals.For("кг/м³"), formulaStatus: FormulaStatusConstant);
            var iceHeatCapacity = ReportValueFactory.Create(Core.Constants.ThermalConstants.IceHeatCapacity, "Дж/(кг·К)", ReportValueSource.Calculated, "ThermalCalculator.IceHeatCapacity", decimals: ReportDecimals.For("Дж/(кг·К)"), formulaStatus: FormulaStatusConstant);
            var iceMeltingHeat = ReportValueFactory.Create(Core.Constants.ThermalConstants.IceMeltingHeat, "Дж/кг", ReportValueSource.Calculated, "ThermalCalculator.IceMeltingHeat", decimals: ReportDecimals.For("Дж/кг"), formulaStatus: FormulaStatusConstant);
            var waterHeatCapacity = ReportValueFactory.Create(Core.Constants.ThermalConstants.WaterHeatCapacity, "Дж/(кг·К)", ReportValueSource.Calculated, "ThermalCalculator.WaterHeatCapacity", decimals: ReportDecimals.For("Дж/(кг·К)"), formulaStatus: FormulaStatusConstant);

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
                WaterHeatCapacity = waterHeatCapacity,
                IsDetailAvailable = detail is not null,
                DetailSourceDescription = DescribeDetailSource(thermalDetail),
                DetailNote = thermalDetail?.Note,
                DetailValidationErrors = thermalDetail?.ValidationErrors ?? new List<string>(),
                Steps = BuildSteps(
                    project,
                    detail,
                    alpha,
                    meltingHeat,
                    convectionHeat,
                    powerUp,
                    powerDown,
                    totalPowerDensity,
                    rFb,
                    rD,
                    parameterM,
                    efficiencyEtaR,
                    excessTemperature,
                    massFlowRate,
                    volumeFlowRate),
                Constants = BuildConstants()
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
                Meta("Коэффициент A", "A", "Промежуточный коэффициент A", "-", "A = 1 / etaR", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator", "ThermalSection"),
                Meta("Коэффициент B", "B", "Промежуточный коэффициент B", "-", "B = 1/RFb + 1/RD", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator", "ThermalSection"),
                Meta("Коэффициент C", "C", "Промежуточный коэффициент C", "-", "C = abs(t_H - t_G)", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator", "ThermalSection"),
                Meta("Коэффициент D", "D", "Промежуточный коэффициент D", "-", "D = lR / (pi * lambdaR)", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator", "ThermalSection"),
                Meta("Коэффициент E", "E", "Промежуточный коэффициент E", "-", "E = s / (d - s)", "ThermalCalculator.CalculateExcessTemperature", "ThermalCalculator", "ThermalSection"),
                Meta("Аргумент КПД ребра", "x", "Аргумент для расчёта etaR", "-", "x = m * spacing / 2", "ThermalCalculator.CalculateRodTheory", "ThermalCalculator", "ThermalSection")
            };

            var formulas = new List<ReportFormula>
            {
                Formula("alpha", "2.26 * (t_P - t_H)^0.33 + 2.6 * v_H", "ThermalCalculator", "Thermal"),
                Formula("Q_таяние", "(h/3600) * rho * (c_ice*(0-t_H) + L_melt + c_water*t_P)", "ThermalCalculator", "Thermal"),
                Formula("Q_конв", "alpha * (t_P - t_H)", "ThermalCalculator", "Thermal"),
                Formula("Q_изл", "epsilon * sigma * (273 + t_P)^4", "ThermalCalculator", "Thermal"),
                Formula("PowerUp", "Q_таяние + Q_конв", "ThermalCalculator", "Thermal"),
                Formula("RFb", "R1 + 1/alpha", "ThermalCalculator", "Thermal"),
                Formula("RD", "R2 + 1/AlphaBottom", "ThermalCalculator", "Thermal"),
                Formula("m", "0.6 * sqrt((1/RFb + 1/RD) / (lambdaE * dE))", "ThermalCalculator", "Thermal"),
                Formula("x", "m * spacing / 2", "ThermalCalculator", "Thermal"),
                Formula("etaR", "tanh(x) / x", "ThermalCalculator", "Thermal"),
                Formula("A", "1 / etaR", "ThermalCalculator", "Thermal"),
                Formula("B", "1/RFb + 1/RD", "ThermalCalculator", "Thermal"),
                Formula("C", "abs(t_H - t_G)", "ThermalCalculator", "Thermal"),
                Formula("D", "lR / (pi * lambdaR)", "ThermalCalculator", "Thermal"),
                Formula("E", "s / (d - s)", "ThermalCalculator", "Thermal"),
                Formula("JHmu", "[A + (B - C/(PowerUp * RFb * RD)) * D * E] * PowerUp * RFb", "ThermalCalculator", "Thermal"),
                Formula("PowerDown", "(JHmu_low * RFb + C * D * E) / (RFb * RD * (A + B * D * E))", "ThermalCalculator", "Thermal"),
                Formula("TotalPowerDensity", "PowerUp + PowerDown", "ThermalCalculator", "Thermal"),
                Formula("m_dot", "PowerTotal / (c_p / 3.6) / DeltaT", "ThermalCalculator", "Thermal"),
                Formula("V_dot_m2", "MassFlowRate / rho * 1000", "ThermalCalculator", "Thermal")
            };

            return new SectionBuildResult<ThermalSection>
            {
                Section = section,
                ParameterMetadata = metadata,
                Formulas = formulas
            };
        }

        /// <summary>Описание источника детальных величин (ADR-010).</summary>
        private static string DescribeDetailSource(ThermalReportDetail? detail)
        {
            if (detail is null)
            {
                return "детальные величины не переданы";
            }

            return detail.Source switch
            {
                ThermalReportDetailSource.Snapshot => "канонический снимок сеанса расчёта",
                ThermalReportDetailSource.Recalculated => "контрольный пересчёт по текущим входам проекта",
                ThermalReportDetailSource.RecalculationInvalid => "контрольный пересчёт не дал валидного результата",
                _ => "детальные величины недоступны"
            };
        }

        /// <summary>
        /// Таблица констант расчёта — единственный источник
        /// <see cref="Core.Constants.ThermalConstants"/> (ADR-010 п.3) и
        /// публичные константы <see cref="Hydraulics.FlowRegimeCalculator"/>.
        /// </summary>
        private static List<ReportConstantEntry> BuildConstants()
        {
            return new List<ReportConstantEntry>
            {
                new() { Name = "Плотность снега (компактный)", Symbol = "ρ_снега", Value = Core.Constants.ThermalConstants.SnowDensity, Decimals = 0, Unit = "кг/м³", SourceDetail = "Core.Constants.ThermalConstants.SnowDensity" },
                new() { Name = "Удельная теплоёмкость льда", Symbol = "c_льда", Value = Core.Constants.ThermalConstants.IceHeatCapacity, Decimals = 0, Unit = "Дж/(кг·К)", SourceDetail = "Core.Constants.ThermalConstants.IceHeatCapacity" },
                new() { Name = "Удельная теплота плавления льда", Symbol = "L_плавл", Value = Core.Constants.ThermalConstants.IceMeltingHeat, Decimals = 0, Unit = "Дж/кг", SourceDetail = "Core.Constants.ThermalConstants.IceMeltingHeat" },
                new() { Name = "Удельная теплоёмкость воды", Symbol = "c_воды", Value = Core.Constants.ThermalConstants.WaterHeatCapacity, Decimals = 0, Unit = "Дж/(кг·К)", SourceDetail = "Core.Constants.ThermalConstants.WaterHeatCapacity" },
                new() { Name = "Эмпирический коэффициент формы (мокрая стяжка)", Symbol = "fm", Value = Core.Constants.ThermalConstants.RodCoefficient, Decimals = 1, Unit = "-", SourceDetail = "Core.Constants.ThermalConstants.RodCoefficient" },
                new() { Name = "Постоянная Стефана-Больцмана", Symbol = "σ", Value = Core.Constants.ThermalConstants.StefanBoltzmann, Decimals = 10, Unit = "Вт/(м²·К⁴)", SourceDetail = "Core.Constants.ThermalConstants.StefanBoltzmann" },
                new() { Name = "Коэффициент излучения поверхности", Symbol = "ε", Value = Core.Constants.ThermalConstants.EmissionCoefficient, Decimals = 3, Unit = "-", SourceDetail = "Core.Constants.ThermalConstants.EmissionCoefficient" },
                new() { Name = "Граница ламинарного режима (Re)", Symbol = "Re_лам", Value = Hydraulics.FlowRegimeCalculator.LaminarBoundary, Decimals = 0, Unit = "-", SourceDetail = "FlowRegimeCalculator.LaminarBoundary" },
                new() { Name = "Граница турбулентного режима (Re)", Symbol = "Re_турб", Value = Hydraulics.FlowRegimeCalculator.TurbulentBoundary, Decimals = 0, Unit = "-", SourceDetail = "FlowRegimeCalculator.TurbulentBoundary" },
                new() { Name = "Шероховатость PE-Xa", Symbol = "ε_тр", Value = Hydraulics.FlowRegimeCalculator.PEXaRoughness, Decimals = 3, Unit = "мм", SourceDetail = "FlowRegimeCalculator.PEXaRoughness" }
            };
        }

        /// <summary>
        /// Пошаговый расчёт. Подстановки собираются из входов проекта и
        /// констант кода; результаты — сохранённые величины провайдера.
        /// Новые инженерные вычисления в билдере не выполняются (AC-5):
        /// если промежуточная величина не сохраняется (A–E, Kv контура),
        /// подстановка опускается с примечанием.
        /// </summary>
        private static List<CalculationStep> BuildSteps(
            ProjectData project,
            ThermalReportDetail? detail,
            ReportValue<double> alpha,
            ReportValue<double> meltingHeat,
            ReportValue<double> convectionHeat,
            ReportValue<double> powerUp,
            ReportValue<double> powerDown,
            ReportValue<double> totalPowerDensity,
            ReportValue<double> rFb,
            ReportValue<double> rD,
            ReportValue<double> parameterM,
            ReportValue<double> efficiencyEtaR,
            ReportValue<double> excessTemperature,
            ReportValue<double> massFlowRate,
            ReportValue<double> volumeFlowRate)
        {
            var steps = new List<CalculationStep>();
            if (detail is null || !detail.HasValues)
            {
                return steps;
            }

            var climate = project.ClimateData ?? new ClimateProjectData();
            var thermal = project.ThermalData ?? new ThermalProjectData();
            var result = thermal.Result ?? new ThermalResultProjectData();
            var construction = project.ConstructionData ?? new ConstructionProjectData();

            var tP = (double)(int)thermal.SelectedMode;
            var tH = climate.AirTemperature;
            var vH = climate.WindSpeed;
            var h = climate.SnowfallIntensity;
            var tG = thermal.GroundTemperature;
            var tSupply = thermal.SupplyTemperature;
            var spacingMm = thermal.PipeSpacing;
            var dOuter = thermal.SelectedPipe?.OuterDiameter ?? 0.0;

            ReportValue<double> V(string key, double value, string unit) =>
                ReportValueFactory.Create(value, unit, ReportValueSource.UserInput, key, decimals: ReportDecimals.For(unit));

            steps.Add(new CalculationStep
            {
                Key = "thermal.alpha",
                Title = "1. Коэффициент теплоотдачи α",
                FormulaText = "α = 2,26·(tП − tH)^0,33 + 2,6·vH",
                SubstitutionText = $"α = 2,26·({ReportNumber.Format(tP, 1)} − ({ReportNumber.Format(tH, 1)}))^0,33 + 2,6·{ReportNumber.Format(vH, 1)}",
                Result = alpha,
                Note = "Конвекция с поверхности; ветровая составляющая 2,6·vH часто превышает собственно конвективную.",
                Inputs = new List<ReportValue<double>> { V("t_P", tP, "°C"), V("t_H", tH, "°C"), V("v_H", vH, "м/с") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.melting",
                Title = "2. Мощность на плавление снега Q_таяния",
                FormulaText = "Q_таяния = (h/1000/3600)·ρ_снега·[c_льда·(0 − tH) + L_плавл + c_воды·tП] (h в мм/ч)",
                SubstitutionText = $"Q_таяния = (h = {ReportNumber.Format(h, 2)} мм/ч; константы — таблица ниже) → {ReportNumber.Format(meltingHeat.Value, 1)} Вт/м²",
                Result = meltingHeat,
                Note = "Нагрев льда до 0 °C, плавление и нагрев воды до tП; плотность и теплоёмкости — константы кода (таблица констант).",
                Inputs = new List<ReportValue<double>> { V("h", h, "мм/ч") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.convection",
                Title = "3. Конвективный тепловой поток Q_конв",
                FormulaText = "Q_конв = α·(tП − tH)",
                SubstitutionText = $"Q_конв = {ReportNumber.Format(detail.Alpha, 2)}·({ReportNumber.Format(tP, 1)} − ({ReportNumber.Format(tH, 1)}))",
                Result = convectionHeat,
                Inputs = new List<ReportValue<double>> { V("alpha", detail.Alpha, "Вт/(м²·К)"), V("t_P", tP, "°C"), V("t_H", tH, "°C") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.powerup",
                Title = "4. Полезная мощность вверх PowerUp",
                FormulaText = "PowerUp = Q_таяния + Q_конв",
                SubstitutionText = $"PowerUp = {ReportNumber.Format(detail.MeltingHeat, 1)} + {ReportNumber.Format(detail.ConvectionHeat, 1)} = {ReportNumber.Format(result.PowerUp, 1)} Вт/м²",
                Result = powerUp,
                Inputs = new List<ReportValue<double>> { V("Q_таяния", detail.MeltingHeat, "Вт/м²"), V("Q_конв", detail.ConvectionHeat, "Вт/м²") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.rfb",
                Title = "5. Сопротивление теплопередаче вверх RFb",
                FormulaText = "RFb = R1 + 1/α",
                SubstitutionText = $"RFb = {ReportNumber.Format(construction.R1, 4)} + 1/{ReportNumber.Format(detail.Alpha, 2)} = {ReportNumber.Format(detail.RFb, 4)} м²·К/Вт",
                Result = rFb,
                Inputs = new List<ReportValue<double>> { V("R1", construction.R1, "м²·К/Вт"), V("alpha", detail.Alpha, "Вт/(м²·К)") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.rd",
                Title = "6. Сопротивление вниз RD",
                FormulaText = "RD = R2 + 1/α_низ; α_низ → ∞ (нижняя граница адиабатическая)",
                SubstitutionText = $"RD = {ReportNumber.Format(construction.R2, 4)} + 1/∞ ≈ {ReportNumber.Format(detail.RD, 4)} м²·К/Вт",
                Result = rD,
                Note = "Слои под трубой приняты адиабатической границей снизу.",
                Inputs = new List<ReportValue<double>> { V("R2", construction.R2, "м²·К/Вт") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.m",
                Title = "7. Параметр затухания m (теория стержня)",
                FormulaText = "m = 0,6·√((1/RFb + 1/RD)/(λE·d_нар))",
                SubstitutionText = $"m = fm·√((1/{ReportNumber.Format(detail.RFb, 4)} + 1/{ReportNumber.Format(detail.RD, 4)})/({ReportNumber.Format(construction.LambdaE, 2)}·{ReportNumber.Format(dOuter / 1000.0, 3)})) = {ReportNumber.Format(detail.ParameterM, 2)} 1/м",
                Result = parameterM,
                Note = "fm — эмпирический коэффициент формы «мокрой стяжки» (таблица констант).",
                Inputs = new List<ReportValue<double>> { V("RFb", detail.RFb, "м²·К/Вт"), V("RD", detail.RD, "м²·К/Вт"), V("lambdaE", construction.LambdaE, "Вт/(м·К)"), V("d_нар", dOuter, "мм") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.etaR",
                Title = "8. КПД шага раскладки ηR",
                FormulaText = "ηR = tanh(m·s/2)/(m·s/2)",
                SubstitutionText = $"ηR = tanh({ReportNumber.Format(detail.ParameterM, 2)}·{ReportNumber.Format(spacingMm / 2000.0, 3)})/({ReportNumber.Format(detail.ParameterM, 2)}·{ReportNumber.Format(spacingMm / 2000.0, 3)}) = {ReportNumber.Format(detail.EfficiencyEtaR, 3)}",
                Result = efficiencyEtaR,
                Note = "Доля теплового потенциала, реализуемая при дискретной раскладке труб.",
                Inputs = new List<ReportValue<double>> { V("m", detail.ParameterM, "1/м"), V("s", spacingMm, "мм") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.jhmu",
                Title = "9. Избыточная температура теплоносителя JHmü",
                FormulaText = "JHmü = [A + (B − C/(PowerUp·RFb·RD))·D·E]·PowerUp·RFb",
                SubstitutionText = $"JHmü (при PowerUp = {ReportNumber.Format(result.PowerUp, 1)} Вт/м²) = {ReportNumber.Format(detail.ExcessTemperature, 1)} К",
                Result = excessTemperature,
                Note = "Промежуточные коэффициенты A–E не сохраняются в проекте — подстановка промежуточных значений недоступна; формула и смысл приведены в приложении.",
                Inputs = new List<ReportValue<double>> { V("PowerUp", result.PowerUp, "Вт/м²") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.tmean",
                Title = "10. Средняя температура теплоносителя",
                FormulaText = "T_средняя = JHmü + tH",
                SubstitutionText = $"T_средняя = {ReportNumber.Format(detail.ExcessTemperature, 1)} + ({ReportNumber.Format(tH, 1)}) = {ReportNumber.Format(result.MeanTemperature, 1)} °C",
                Result = ReportValueFactory.Create(result.MeanTemperature, "°C", ReportValueSource.Calculated, "ThermalResultProjectData.MeanTemperature", decimals: ReportDecimals.For("°C"), formula: "JHmü + t_H"),
                Inputs = new List<ReportValue<double>> { V("JHmü", detail.ExcessTemperature, "К"), V("t_H", tH, "°C") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.treturn",
                Title = "11. Температура обратки и перепад ΔT",
                FormulaText = "T_обратки = 2·T_средняя − T_подачи; ΔT = T_подачи − T_обратки",
                SubstitutionText = $"T_обратки = 2·{ReportNumber.Format(result.MeanTemperature, 1)} − {ReportNumber.Format(tSupply, 1)} = {ReportNumber.Format(result.ReturnTemperature, 1)} °C; ΔT = {ReportNumber.Format(result.DeltaT, 1)} К",
                Result = ReportValueFactory.Create(result.DeltaT, "K", ReportValueSource.Calculated, "ThermalResultProjectData.DeltaT", decimals: ReportDecimals.For("K"), formula: "T_подачи − T_обратки"),
                Inputs = new List<ReportValue<double>> { V("T_средняя", result.MeanTemperature, "°C"), V("T_подачи", tSupply, "°C") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.powerdown",
                Title = "12. Мощность вниз qD",
                FormulaText = "qD = (JHmü_низ·RFb + C·D·E)/(RFb·RD·(A + B·D·E)), JHmü_низ = T_средняя − tG",
                SubstitutionText = $"qD = {ReportNumber.Format(result.PowerDown, 1)} Вт/м² (потери вниз через изоляцию)",
                Result = powerDown,
                Note = "Промежуточные коэффициенты A–E не сохраняются в проекте; зависимость qD от толщины утепления видна по величине R2.",
                Inputs = new List<ReportValue<double>> { V("T_средняя", result.MeanTemperature, "°C"), V("t_G", tG, "°C") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.ptotal",
                Title = "13. Суммарная удельная мощность qTotal",
                FormulaText = "qTotal = PowerUp + PowerDown",
                SubstitutionText = $"qTotal = {ReportNumber.Format(result.PowerUp, 1)} + {ReportNumber.Format(result.PowerDown, 1)} = {ReportNumber.Format(result.PowerTotal, 1)} Вт/м²",
                Result = totalPowerDensity,
                Inputs = new List<ReportValue<double>> { V("PowerUp", result.PowerUp, "Вт/м²"), V("PowerDown", result.PowerDown, "Вт/м²") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.massflow",
                Title = "14. Массовый расход на м²",
                FormulaText = "ṁ = qTotal/(c_p/3,6)/ΔT",
                SubstitutionText = $"ṁ = {ReportNumber.Format(result.PowerTotal, 1)}/(c_p/3,6)/{ReportNumber.Format(result.DeltaT, 1)} = {ReportNumber.Format(detail.MassFlowRate, 1)} кг/(ч·м²)",
                Result = massFlowRate,
                Note = "c_p — дефолт пайплайна 3,39 кДж/(кг·К); множитель 3,6 переводит кДж/(кг·К) и Вт в кг/ч.",
                Inputs = new List<ReportValue<double>> { V("qTotal", result.PowerTotal, "Вт/м²"), V("ΔT", result.DeltaT, "K") }
            });

            steps.Add(new CalculationStep
            {
                Key = "thermal.volumeflow",
                Title = "15. Объёмный расход на м²",
                FormulaText = "V̇ = ṁ/ρ·1000",
                SubstitutionText = $"V̇ = {ReportNumber.Format(detail.MassFlowRate, 1)}/ρ·1000 = {ReportNumber.Format(detail.VolumeFlowRate, 2)} л/(ч·м²)",
                Result = volumeFlowRate,
                Note = "ρ — дефолт пайплайна 1053 кг/м³ (теплоноситель по умолчанию).",
                Inputs = new List<ReportValue<double>> { V("ṁ", detail.MassFlowRate, "кг/(ч·м²)") }
            });

            return steps;
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
