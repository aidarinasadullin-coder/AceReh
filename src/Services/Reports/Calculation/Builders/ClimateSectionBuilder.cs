using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела климатических данных и входных тепловых параметров.
    /// </summary>
    public sealed class ClimateSectionBuilder : IReportSectionBuilder<ClimateSection>
    {
        public SectionBuildResult<ClimateSection> Build(ProjectData project, CalculationReportMode mode, ThermalReportDetail? thermalDetail = null)
        {
            var climate = project.ClimateData ?? new ClimateProjectData();
            var thermal = project.ThermalData ?? new ThermalProjectData();
            var result = thermal.Result ?? new ThermalResultProjectData();

            var city = ReportValueFactory.Create(climate.SelectedCity ?? string.Empty, "-", ReportValueSource.ProgramDatabase, "ProjectData.ClimateData.SelectedCity");
            var region = ReportValueFactory.Create(climate.Region ?? string.Empty, "-", ReportValueSource.ProgramDatabase, "ProjectData.ClimateData.Region");
            // Доменно-валидные нули входов (ревью P1–P2, В2-семантика):
            // t_H = 0 °C (−50..+10), ветер 0 м/с (ValidationConstants.MinWindSpeed = 0),
            // влажность 0 % (не валидируется), снег 0 мм/ч (0..20),
            // t_G = 0 °C (−10..30) — хранимые значения, а не заглушки.
            var airTemperature = ReportValueFactory.Create(climate.AirTemperature, "°C", ReportValueSource.ProgramDatabase, "ProjectData.ClimateData.AirTemperature", decimals: ReportDecimals.For("°C"), zeroIsValid: true);
            var windSpeed = ReportValueFactory.Create(climate.WindSpeed, "м/с", ReportValueSource.ProgramDatabase, "ProjectData.ClimateData.WindSpeed", decimals: ReportDecimals.For("м/с"), zeroIsValid: true);
            var humidity = ReportValueFactory.Create(climate.Humidity, "%", ReportValueSource.ProgramDatabase, "ProjectData.ClimateData.Humidity", decimals: ReportDecimals.For("%"), zeroIsValid: true);
            var snowfallIntensity = ReportValueFactory.Create(climate.SnowfallIntensity, "мм/ч", ReportValueSource.UserInput, "ProjectData.ClimateData.SnowfallIntensity", decimals: ReportDecimals.For("мм/ч"), zeroIsValid: true);
            var climateZone = ReportValueFactory.Create(climate.SelectedZone.ToString(), "-", ReportValueSource.ProgramDatabase, "ProjectData.ClimateData.SelectedZone");
            var coldPeriodDays = ReportValueFactory.Create(0.0, "дней", ReportValueSource.ProgramDatabase, "ClimateViewModel.SelectedCity.Period_0_Days", decimals: ReportDecimals.For("дней"), formulaStatus: "условно: данные отсутствуют в ProjectData");
            var surfaceTemperature = ReportValueFactory.Create((double)(int)thermal.SelectedMode, "°C", ReportValueSource.UserInput, "ProjectData.ThermalData.SelectedMode", decimals: ReportDecimals.For("°C"), formula: "(int)OperatingMode");
            var groundTemperature = ReportValueFactory.Create(thermal.GroundTemperature, "°C", ReportValueSource.UserInput, "ProjectData.ThermalData.GroundTemperature", decimals: ReportDecimals.For("°C"), zeroIsValid: true);
            var supplyTemperature = ReportValueFactory.Create(thermal.SupplyTemperature, "°C", ReportValueSource.UserInput, "ProjectData.ThermalData.SupplyTemperature", decimals: ReportDecimals.For("°C"));
            var returnTemperature = ReportValueFactory.Create(result.ReturnTemperature, "°C", ReportValueSource.Calculated, "ThermalResultProjectData.ReturnTemperature", decimals: ReportDecimals.For("°C"), formula: "2 * MeanTemperature - SupplyTemperature");
            var meanTemperature = ReportValueFactory.Create(result.MeanTemperature, "°C", ReportValueSource.Calculated, "ThermalResultProjectData.MeanTemperature", decimals: ReportDecimals.For("°C"), formula: "ExcessTemperature + AirTemperature");
            var deltaT = ReportValueFactory.Create(result.DeltaT, "K", ReportValueSource.Calculated, "ThermalResultProjectData.DeltaT", decimals: ReportDecimals.For("K"), formula: "SupplyTemperature - ReturnTemperature");

            var section = new ClimateSection
            {
                City = city,
                Region = region,
                AirTemperature = airTemperature,
                WindSpeed = windSpeed,
                Humidity = humidity,
                SnowfallIntensity = snowfallIntensity,
                ClimateZone = climateZone,
                ColdPeriodDays = coldPeriodDays,
                SurfaceTemperature = surfaceTemperature,
                GroundTemperature = groundTemperature,
                SupplyTemperature = supplyTemperature,
                ReturnTemperature = returnTemperature,
                MeanTemperature = meanTemperature,
                DeltaT = deltaT
            };

            var metadata = new List<ReportParameterMetadata>
            {
                Meta("Город", "-", "Выбранный город", city),
                Meta("Регион", "-", "Регион города", region),
                Meta("Расчётная температура воздуха", "t_H", "Расчётная температура наружного воздуха", airTemperature),
                Meta("Скорость ветра", "v_H", "Скорость ветра", windSpeed),
                Meta("Влажность", "phi", "Относительная влажность (условно, не участвует в расчёте)", humidity, formula: "условно: не используется в расчёте"),
                Meta("Интенсивность снегопада", "h", "Интенсивность снегопада", snowfallIntensity),
                Meta("Климатическая зона", "-", "Климатическая зона", climateZone),
                Meta("Холодный период", "-", "Количество дней холодного периода", coldPeriodDays),
                Meta("Температура поверхности", "t_P", "Температура поверхности по выбранному режиму", surfaceTemperature),
                Meta("Температура грунта", "t_G", "Температура грунта", groundTemperature),
                Meta("Температура подачи", "T_supply", "Температура подачи теплоносителя", supplyTemperature),
                Meta("Температура обратки", "T_return", "Температура обратки теплоносителя", returnTemperature),
                Meta("Средняя температура", "T_mean", "Средняя температура теплоносителя", meanTemperature),
                Meta("Температурный перепад", "DeltaT", "Температурный перепад", deltaT)
            };

            var formulas = new List<ReportFormula>
            {
                Formula("T_return", "2 * MeanTemperature - SupplyTemperature", "ThermalCalculator", "Climate"),
                Formula("T_mean", "ExcessTemperature + AirTemperature", "ThermalCalculator", "Climate"),
                Formula("DeltaT", "SupplyTemperature - ReturnTemperature", "ThermalCalculator", "Climate"),
                Formula("t_P", "(int)OperatingMode", "ThermalCalculator", "Climate")
            };

            return new SectionBuildResult<ClimateSection>
            {
                Section = section,
                ParameterMetadata = metadata,
                Formulas = formulas
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<double> value, string? formula = null)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = formula ?? value.Formula ?? value.FormulaStatus,
                FormulaSource = "ClimateSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = "ClimateSection"
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<string> value, string? formula = null)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = formula ?? value.Formula ?? value.FormulaStatus,
                FormulaSource = "ClimateSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = "ClimateSection"
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
