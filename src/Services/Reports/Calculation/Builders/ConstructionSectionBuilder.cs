using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела конструкции.
    /// </summary>
    public sealed class ConstructionSectionBuilder : IReportSectionBuilder<ConstructionSection>
    {
        public SectionBuildResult<ConstructionSection> Build(ProjectData project, CalculationReportMode mode, ThermalReportDetail? thermalDetail = null, HydraulicsReportDetail? hydraulicsDetail = null)
        {
            var construction = project.ConstructionData ?? new ConstructionProjectData();

            var groundwaterLevel = ReportValueFactory.Create(construction.GroundwaterLevel, "м", ReportValueSource.UserInput, "ProjectData.ConstructionData.GroundwaterLevel", decimals: ReportDecimals.For("м"), zeroIsValid: true);
            var r1 = ReportValueFactory.Create(construction.R1, "м²·К/Вт", ReportValueSource.Calculated, "ProjectData.ConstructionData.R1", decimals: ReportDecimals.For("м²·К/Вт"), formula: "sum(R_i) above pipe");
            var r2 = ReportValueFactory.Create(construction.R2, "м²·К/Вт", ReportValueSource.Calculated, "ProjectData.ConstructionData.R2", decimals: ReportDecimals.For("м²·К/Вт"), formula: "sum(R_i) below pipe");
            var lambdaE = ReportValueFactory.Create(construction.LambdaE, "Вт/(м·К)", ReportValueSource.UserInput, "ProjectData.ConstructionData.LambdaE", decimals: ReportDecimals.For("Вт/(м·К)"));

            var layers = (construction.Layers ?? new List<LayerProjectData>())
                .Select(layer => new ReportConstructionLayer
                {
                    Position = layer.Position.ToString(),
                    MaterialName = ReportValueFactory.Create(layer.MaterialName ?? string.Empty, "-", ReportValueSource.Project, "LayerProjectData.MaterialName"),
                    Thickness = ReportValueFactory.Create(layer.Thickness, "мм", ReportValueSource.UserInput, "LayerProjectData.Thickness", decimals: ReportDecimals.For("мм")),
                    Lambda = ReportValueFactory.Create(layer.CalculatedLambda, "Вт/(м·К)", ReportValueSource.Project, "LayerProjectData.CalculatedLambda", decimals: ReportDecimals.For("Вт/(м·К)"), formula: layer.IsLambdaOverridden ? "ручное переопределение" : "материал БД"),
                    ThermalResistance = ReportValueFactory.Create(layer.CalculatedR, "м²·К/Вт", ReportValueSource.Calculated, "LayerProjectData.CalculatedR", decimals: ReportDecimals.For("м²·К/Вт"), formula: "(d / 1000) / lambda")
                })
                .ToList();

            var section = new ConstructionSection
            {
                GroundwaterLevel = groundwaterLevel,
                R1 = r1,
                R2 = r2,
                LambdaE = lambdaE,
                LambdaRuleNote = BuildLambdaRuleNote(construction),
                Steps = BuildSteps(construction, layers, r1, r2),
                Layers = layers
            };

            var metadata = new List<ReportParameterMetadata>
            {
                Meta("Уровень грунтовых вод", "УГВ", "Уровень грунтовых вод", groundwaterLevel),
                Meta("Сопротивление над трубой", "R1", "Суммарное термическое сопротивление слоёв над трубой", r1),
                Meta("Сопротивление под трубой", "R2", "Суммарное термическое сопротивление слоёв под трубой", r2),
                Meta("Эквивалентная теплопроводность", "lambdaE", "Эквивалентная теплопроводность материала вокруг трубы", lambdaE),
                Meta("Выбор сухой/влажной lambda", "lambdaA / lambdaB", "Логика выбора теплопроводности в зависимости от УГВ", "-", ReportValueSource.ProgramDatabase, "LayerProjectData.CalculatedLambda", "УГВ < 1 м -> lambdaB, иначе lambdaA", "ConstructionViewModel", "ConstructionSection.Layers")
            };

            foreach (var layer in layers)
            {
                metadata.Add(Meta("Материал слоя", "-", "Материал слоя конструкции", layer.MaterialName, "ConstructionSection.Layers"));
                metadata.Add(Meta("Толщина слоя", "d_i", "Толщина слоя", layer.Thickness, "ConstructionSection.Layers"));
                metadata.Add(Meta("Теплопроводность слоя", "lambda_i", "Коэффициент теплопроводности слоя", layer.Lambda, "ConstructionSection.Layers"));
                metadata.Add(Meta("Термическое сопротивление слоя", "R_i", "Термическое сопротивление слоя", layer.ThermalResistance, "ConstructionSection.Layers"));
            }

            var formulas = new List<ReportFormula>
            {
                Formula("R_i", "(d_i / 1000) / lambda_i", "ThermalCalculator.CalculateThermalResistance / ProjectData", "Construction"),
                Formula("R1", "sum(R_i) above pipe", "ProjectData.ConstructionData.R1", "Construction"),
                Formula("R2", "sum(R_i) below pipe", "ProjectData.ConstructionData.R2", "Construction"),
                Formula("lambdaA/lambdaB", "УГВ < 1 м -> lambdaB, иначе lambdaA", "ConstructionViewModel / docs/Formulas_Snegotayanie.md", "Construction")
            };

            return new SectionBuildResult<ConstructionSection>
            {
                Section = section,
                ParameterMetadata = metadata,
                Formulas = formulas
            };
        }

        /// <summary>
        /// Правило выбора λА/λБ по уровню грунтовых вод (docs/Formulas_Snegotayanie.md)
        /// с фактическим значением УГВ проекта.
        /// </summary>
        private static string BuildLambdaRuleNote(ConstructionProjectData construction)
        {
            var condition = construction.GroundwaterLevel < 1.0 ? "λБ (влажные условия)" : "λА (сухие условия)";
            return $"УГВ < 1 м → λБ (влажные условия), УГВ ≥ 1 м → λА (сухие условия); правило касается только слоёв ПОД трубой (над трубой всегда λА). " +
                $"В проекте УГВ = {ReportNumber.Format(construction.GroundwaterLevel, 1)} м → слои под трубой считаются по {condition}.";
        }

        /// <summary>
        /// Шаги расчёта R1/R2 с подстановкой по слоям: слагаемые
        /// «(d_i/1000)/λ_i» берутся из сохранённых слоёв, результат —
        /// сохранённое суммарное сопротивление. Новых вычислений нет (AC-5):
        /// при отсутствии слоёв шаги не строятся.
        /// </summary>
        private static List<CalculationStep> BuildSteps(
            ConstructionProjectData construction,
            IReadOnlyList<ReportConstructionLayer> layers,
            ReportValue<double> r1,
            ReportValue<double> r2)
        {
            var steps = new List<CalculationStep>();
            AddResistanceStep(steps, "construction.r1", "R1 — сопротивление слоёв НАД трубой",
                layers.Where(l => l.Position == LayerPosition.AbovePipe.ToString()).ToList(), construction.R1, r1);
            AddResistanceStep(steps, "construction.r2", "R2 — сопротивление слоёв ПОД трубой",
                layers.Where(l => l.Position == LayerPosition.BelowPipe.ToString()).ToList(), construction.R2, r2);
            return steps;
        }

        private static void AddResistanceStep(
            List<CalculationStep> steps,
            string key,
            string title,
            IReadOnlyList<ReportConstructionLayer> layerRows,
            double total,
            ReportValue<double> resultValue)
        {
            if (layerRows.Count == 0)
            {
                return;
            }

            var terms = layerRows
                .Select(l => $"{ReportNumber.Format(l.Thickness.Value / 1000.0, 3)}/{ReportNumber.Format(l.Lambda.Value, 2)}");
            var substitution = $"{title.Split(' ')[0]} = {string.Join(" + ", terms)} = {ReportNumber.Format(total, 4)} м²·К/Вт";

            steps.Add(new CalculationStep
            {
                Key = key,
                Title = title,
                FormulaText = "R = Σ (d_i/1000)/λ_i",
                SubstitutionText = substitution,
                Result = resultValue,
                Note = "Отсчёт — от оси трубы: слои над трубой к поверхности, под трубой — к грунту.",
                Inputs = layerRows
                    .SelectMany(l => new[]
                    {
                        ReportValueFactory.Create(l.Thickness.Value, "мм", ReportValueSource.UserInput, "LayerProjectData.Thickness", decimals: ReportDecimals.For("мм")),
                        ReportValueFactory.Create(l.Lambda.Value, "Вт/(м·К)", ReportValueSource.Project, "LayerProjectData.CalculatedLambda", decimals: ReportDecimals.For("Вт/(м·К)"))
                    })
                    .ToList()
            });
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<double> value)
        {
            return Meta(name, symbol, physicalMeaning, value, "ConstructionSection");
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
                FormulaSource = "ConstructionSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = whereUsed
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<string> value)
        {
            return Meta(name, symbol, physicalMeaning, value, "ConstructionSection");
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
                FormulaSource = "ConstructionSectionBuilder",
                WhereCalculated = value.SourceDetail,
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
            string? formula,
            string whereCalculated,
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
                FormulaSource = formula == null ? string.Empty : "ConstructionSectionBuilder",
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
