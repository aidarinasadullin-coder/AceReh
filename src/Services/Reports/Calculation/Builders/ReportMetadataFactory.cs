namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Общая фабрика метаданных параметров и формул секций отчёта.
    /// Литералы FormulaSource/WhereUsed/WhereCalculated попадают в метаданные
    /// отчёта — семантика каждой перегрузки сохранена байт-в-байт за секциями.
    /// </summary>
    internal static class ReportMetadataFactory
    {
        /// <summary>
        /// Метаданные параметра из сохранённого значения: unit/source/sourceDetail
        /// берутся из значения, формула — override ?? Formula ?? FormulaStatus,
        /// FormulaSource — имя билдера, WhereCalculated — sourceDetail значения.
        /// </summary>
        public static ReportParameterMetadata Create(
            string name,
            string symbol,
            string physicalMeaning,
            ReportValue<double> value,
            string formulaSource,
            string whereUsed,
            string? formulaOverride = null)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = formulaOverride ?? value.Formula ?? value.FormulaStatus,
                FormulaSource = formulaSource,
                WhereCalculated = value.SourceDetail,
                WhereUsed = whereUsed
            };
        }

        /// <summary>
        /// Метаданные параметра из строкового сохранённого значения —
        /// семантика та же, что для числового.
        /// </summary>
        public static ReportParameterMetadata Create(
            string name,
            string symbol,
            string physicalMeaning,
            ReportValue<string> value,
            string formulaSource,
            string whereUsed,
            string? formulaOverride = null)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = formulaOverride ?? value.Formula ?? value.FormulaStatus,
                FormulaSource = formulaSource,
                WhereCalculated = value.SourceDetail,
                WhereUsed = whereUsed
            };
        }

        /// <summary>
        /// Метаданные со всеми полями явно; FormulaSource передаётся как есть
        /// (в отличие от value-вариантов, пустая формула не обнуляет источник).
        /// </summary>
        public static ReportParameterMetadata CreateExplicit(
            string name,
            string symbol,
            string physicalMeaning,
            string unit,
            ReportValueSource source,
            string sourceDetail,
            string? formula,
            string formulaSource,
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
                FormulaSource = formulaSource,
                WhereCalculated = whereCalculated,
                WhereUsed = whereUsed
            };
        }

        /// <summary>
        /// Формула секции отчёта.
        /// </summary>
        public static ReportFormula CreateFormula(string symbol, string expression, string sourcePath, string section)
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
