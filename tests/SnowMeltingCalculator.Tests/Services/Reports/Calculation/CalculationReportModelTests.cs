using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Фокусные тесты чистой модели детального расчётного отчёта.
    /// </summary>
    [TestFixture]
    public class CalculationReportModelTests
    {
        private static readonly string[] ForbiddenNamespaces = new[]
        {
            "SnowMeltingCalculator.Services.Results",
            "SnowMeltingCalculator.ViewModels",
            "QuestPDF",
            "System.Windows",
            "System.Windows.Media",
            "System.Windows.Controls"
        };

        private static readonly string[] ForbiddenTypeNames = new[]
        {
            "ResultsPdfData",
            "ResultsPdfDataBuilder",
            "PdfExportService",
            "QuestPDF",
            "Markdown"
        };

        private static readonly string[] ForbiddenPropertyTokens = new[]
        {
            "Markdown",
            "Pdf",
            "Xaml",
            "ViewModel",
            "ImageBytes",
            "Font",
            "Renderer",
            "Document"
        };

        // Orchestration types in the same namespace: services, renderers, builders and
        // their interfaces. They are NOT pure model DTOs and are expected to reference
        // the Markdown renderer interface. They are covered by their own fixtures
        // (CalculationReportMarkdownRendererTests, CalculationReportExportServiceTests,
        // CalculationReportDataBuilderTests), so the pure-model guard must skip them
        // by name regardless of the StartsWith/EndsWith rules below.
        private static readonly HashSet<string> ExcludedReportTypeNames = new(StringComparer.Ordinal)
        {
            "CalculationReportExportService",
            "ICalculationReportExportService",
            "CalculationReportMarkdownRenderer",
            "ICalculationReportMarkdownRenderer",
            "CalculationReportDataBuilder",
            "ICalculationReportDataBuilder"
        };

        [Test]
        public void ReportValue_double_HoldsUnitSourceSourceDetailAndFormulaStatus()
        {
            var value = new ReportValue<double>
            {
                Value = 275.0,
                Unit = "Вт/м²",
                Source = ReportValueSource.Calculated,
                SourceDetail = "ThermalCalculationResult.PowerUp",
                Formula = "Q_таяние + Q_конв",
                FormulaStatus = "требуется привязка к существующей формуле"
            };

            Assert.That(value.Value, Is.EqualTo(275.0));
            Assert.That(value.Unit, Is.EqualTo("Вт/м²"));
            Assert.That(value.Source, Is.EqualTo(ReportValueSource.Calculated));
            Assert.That(value.SourceDetail, Is.EqualTo("ThermalCalculationResult.PowerUp"));
            Assert.That(value.Formula, Is.EqualTo("Q_таяние + Q_конв"));
            Assert.That(value.FormulaStatus, Is.EqualTo("требуется привязка к существующей формуле"));
        }

        [Test]
        public void CalculationReportData_ContainsRequiredSections()
        {
            var report = CreateMinimalReport();

            Assert.That(report.ProjectSection, Is.Not.Null);
            Assert.That(report.ClimateSection, Is.Not.Null);
            Assert.That(report.ConstructionSection, Is.Not.Null);
            Assert.That(report.ThermalSection, Is.Not.Null);
            Assert.That(report.HydraulicsSection, Is.Not.Null);
            Assert.That(report.EquipmentSection, Is.Not.Null);
            Assert.That(report.Warnings, Is.Not.Null);
            Assert.That(report.SourcesAppendix, Is.Not.Null);
            Assert.That(report.FormulasAppendix, Is.Not.Null);
        }

        [Test]
        public void CalculationReportData_DoesNotContainMarkdownOrPdfOrWpfSpecificProperties()
        {
            var reportType = typeof(CalculationReportData);
            var properties = reportType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var name = property.Name;
                Assert.That(
                    ForbiddenPropertyTokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase)),
                    Is.False,
                    $"Property {name} on {reportType.Name} contains a renderer-specific token.");

                var propertyTypeName = property.PropertyType.Name;
                Assert.That(
                    ForbiddenTypeNames.Any(token => propertyTypeName.Contains(token, StringComparison.OrdinalIgnoreCase)),
                    Is.False,
                    $"Property {name} type {propertyTypeName} looks renderer-specific.");
            }
        }

        [Test]
        public void NamespaceTypes_DoNotReferenceResultsPdfDataOrQuestPDFOrWpf()
        {
            var assembly = typeof(CalculationReportData).Assembly;
            var reportTypes = assembly.GetTypes()
                .Where(t => t.Namespace == "SnowMeltingCalculator.Services.Reports.Calculation"
                            && IsPureReportModelType(t))
                .ToList();

            Assert.That(reportTypes, Is.Not.Empty);

            foreach (var type in reportTypes)
            {
                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    Type? memberType = null;
                    if (member is PropertyInfo property)
                    {
                        memberType = property.PropertyType;
                    }
                    else if (member is FieldInfo field)
                    {
                        memberType = field.FieldType;
                    }
                    else if (member is MethodInfo method && method.ReturnType != typeof(void))
                    {
                        memberType = method.ReturnType;
                    }

                    if (memberType is null)
                        continue;

                    foreach (var candidate in ExpandType(memberType))
                    {
                        var candidateNamespace = candidate.Namespace ?? string.Empty;
                        var candidateName = candidate.Name;

                        Assert.That(
                            ForbiddenNamespaces.Any(ns => candidateNamespace.StartsWith(ns, StringComparison.Ordinal)),
                            Is.False,
                            $"Type {type.Name} references forbidden namespace {candidateNamespace} via member {member.Name}.");

                        Assert.That(
                            ForbiddenTypeNames.Any(name => candidateName.Contains(name, StringComparison.OrdinalIgnoreCase)),
                            Is.False,
                            $"Type {type.Name} references forbidden type {candidateName} via member {member.Name}.");
                    }
                }
            }
        }

        [Test]
        public void CalculationReportMode_HasExactlyOperatingAndDesignCold()
        {
            var values = Enum.GetValues(typeof(CalculationReportMode)).Cast<CalculationReportMode>().ToList();

            Assert.That(values, Is.EquivalentTo(new[] { CalculationReportMode.Operating, CalculationReportMode.DesignCold }));
        }

        [Test]
        public void ReportValueFactory_CreatesValueWithRequiredMetadata()
        {
            var value = ReportValueFactory.Create(275.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.PowerUp");

            Assert.That(value.Value, Is.EqualTo(275.0));
            Assert.That(value.Unit, Is.EqualTo("Вт/м²"));
            Assert.That(value.Source, Is.EqualTo(ReportValueSource.Calculated));
            Assert.That(value.SourceDetail, Is.EqualTo("ThermalCalculationResult.PowerUp"));
        }

        [Test]
        public void CalculationReportData_CollectionsAreReadOnlyLists()
        {
            var report = CreateMinimalReport();

            Assert.That(report.Warnings, Is.InstanceOf<IReadOnlyList<CalculationReportWarning>>());
            Assert.That(report.ConstructionSection.Layers, Is.InstanceOf<IReadOnlyList<ReportConstructionLayer>>());
            Assert.That(report.HydraulicsSection.Collectors, Is.InstanceOf<IReadOnlyList<ReportCollector>>());
            Assert.That(report.EquipmentSection.CollectorSpecifications, Is.InstanceOf<IReadOnlyList<ReportCollectorSpecification>>());
            Assert.That(report.SourcesAppendix.Entries, Is.InstanceOf<IReadOnlyList<ReportParameterMetadata>>());
            Assert.That(report.FormulasAppendix.Formulas, Is.InstanceOf<IReadOnlyList<ReportFormula>>());
        }

        private static CalculationReportData CreateMinimalReport()
        {
            return new CalculationReportData
            {
                Mode = CalculationReportMode.Operating,
                ReportDate = new DateTime(2026, 7, 27),
                Methodology = "Расчёт по методике REHAU",
                ProjectSection = new ProjectSection
                {
                    ProjectNumber = "P-001",
                    ProjectObject = "Тестовая площадка"
                },
                ClimateSection = new ClimateSection(),
                ConstructionSection = new ConstructionSection(),
                ThermalSection = new ThermalSection(),
                HydraulicsSection = new HydraulicsSection(),
                EquipmentSection = new EquipmentSection(),
                Warnings = new List<CalculationReportWarning>(),
                SourcesAppendix = new SourcesAppendix(),
                FormulasAppendix = new FormulasAppendix()
            };
        }

        /// <summary>
        /// True только для чистых DTO/модели/значения/метаданных/формул/варнингов/перечислений
        /// расчётного отчёта. Сервисы, рендереры, builder-ы и соответствующие интерфейсы
        /// перечислены в <see cref="ExcludedReportTypeNames"/> и исключаются по имени —
        /// их ожидаемая зависимость от Markdown-интерфейсов тестируется в собственных
        /// фикстурах. Префиксы/суффиксы подобраны так, чтобы захватить Report*
        /// (значения, метаданные, формулы, DTO коллектора/контура/слоя),
        /// CalculationReport* (Data, Mode, Warning) и секционные классы
        /// (*Section, *Appendix).
        /// </summary>
        private static bool IsPureReportModelType(Type type)
        {
            if (ExcludedReportTypeNames.Contains(type.Name))
            {
                return false;
            }

            var name = type.Name;
            return name.StartsWith("Report", StringComparison.Ordinal)
                || name.StartsWith("CalculationReport", StringComparison.Ordinal)
                || name.EndsWith("Section", StringComparison.Ordinal)
                || name.EndsWith("Appendix", StringComparison.Ordinal);
        }

        private static IEnumerable<Type> ExpandType(Type type)
        {
            if (type.IsGenericType)
            {
                yield return type.GetGenericTypeDefinition();
                foreach (var arg in type.GetGenericArguments())
                {
                    foreach (var expanded in ExpandType(arg))
                    {
                        yield return expanded;
                    }
                }
            }
            else
            {
                yield return type;
            }

            if (type.IsArray)
            {
                foreach (var expanded in ExpandType(type.GetElementType()!))
                {
                    yield return expanded;
                }
            }
        }
    }
}
