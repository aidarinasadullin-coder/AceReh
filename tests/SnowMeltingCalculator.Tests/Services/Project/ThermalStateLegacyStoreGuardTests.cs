using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Final Phase 4 source and runtime guards. These tests are deliberately
    /// NegativeFixture tests: each synthetic defect must be rejected by the
    /// same predicate used against production source.
    /// </summary>
    [TestFixture]
    public sealed class ThermalStateLegacyStoreGuardTests
    {
        // AMZ-1 is the only accepted transitional write. Its production caller
        // is CalculationStateService; the other references are immutable tests.
        internal static readonly IReadOnlyDictionary<string, string[]> AmzExpected =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["ApplyNeedsRecalculation"] = new[]
                {
                    "src/Services/Project/ProjectSessionThermalState.cs",
                    "src/Services/Navigation/CalculationStateService.cs"
                },
                ["LegacyInterfaceWriters"] = new[]
                {
                    "src/Services/Navigation/CalculationStateService.cs"
                },
                ["ProjectLoadResetTranslation"] = new[]
                {
                    "src/Services/Navigation/CalculationStateService.cs"
                }
            };

        [Test, Category("NegativeFixture")]
        public void VmWritableStore_GuardRejectsThermalStatusBackingFields()
        {
            var source = ReadSource("src/ViewModels/Thermal/ThermalViewModel.cs");
            Assert.That(FindMatches(source, @"private\s+(?:bool|string)\s+_thermal(?:NeedsRecalculation|IsCalculating|ValidationMessage)\b"), Is.Empty);
            Assert.That(RejectsVmWritableStore("private bool _thermalNeedsRecalculation;"), Is.True);
        }

        [Test, Category("NegativeFixture")]
        public void ServiceThermalStore_GuardRejectsThermalAndSpacingBackingFields()
        {
            var source = ReadSource("src/Services/Navigation/CalculationStateService.cs");
            Assert.That(FindMatches(source, @"private\s+(?:bool|string|int)\s+_(?:thermal|pipeSpacing)[A-Za-z0-9_]*\b"), Is.Empty);
            Assert.That(RejectsServiceStore("private int _pipeSpacing = 200;"), Is.True);
        }

        [Test, Category("NegativeFixture")]
        public void OrchestratorDirectAssign_GuardRequiresRestoreBeforeAdapterProjection()
        {
            var source = ReadSource("src/Services/Project/ProjectLoadOrchestrator.cs");
            Assert.That(FindDirectThermalAssignmentsBeforeRestore(source), Is.Empty);
            Assert.That(FindDirectThermalAssignmentsBeforeRestore("_thermalViewModel.SelectedPipe = pipe;"), Is.EqualTo(new[] { "SelectedPipe" }));
        }

        [Test, Category("NegativeFixture")]
        public void ResultsNonCanonicalSave_GuardRequiresCanonicalMapperInput()
        {
            var source = ReadSource("src/ViewModels/Results/ResultsViewModel.cs");
            var save = ExtractMethod(source, "public ProjectData SaveCurrentProject()", "public ");
            Assert.That(save, Does.Contain("ThermalPersistenceMapper.BuildThermalProjectData"));
            Assert.That(FindMatches(save, @"_thermalViewModel\.(?:SelectedMode|SupplyTemperature|GroundTemperature|PipeSpacing)\b"), Is.Empty);
            Assert.That(FindMatches(save, @"_calculationStateService\.PipeSpacing\b"), Is.Empty);
            Assert.That(RejectsNonCanonicalSave("data.ThermalData = Build(_thermalViewModel.PipeSpacing);"), Is.True);
        }

        [Test, Category("NegativeFixture")]
        public void ContextUnapprovedWriter_GuardAllowsOnlyCoordinatorProductionWriter()
        {
            var root = FindRepositoryRoot();
            var writers = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFileNameWithoutExtension(path), "CalculationContext", StringComparison.OrdinalIgnoreCase))
                .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bUpdateThermal(?:Inputs|Result)?\s*\("))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.That(writers, Is.EqualTo(new[] { "ThermalStateCoordinator" }));
            Assert.That(FindUnapprovedWriterFiles(new[] { ("Synthetic.cs", "context.UpdateThermal(result);") }), Is.EqualTo(new[] { "Synthetic.cs" }));
        }

        [Test, Category("NegativeFixture")]
        public void SnapshotMutability_GuardDefensivelyCopiesEscapingMutableValues()
        {
            var errors = new[] { "original" };
            var snapshot = ThermalResultSnapshot.FromResult(new ThermalCalculationResult { ValidationErrors = errors })!;
            errors[0] = "changed";
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ValidationErrors, Is.EqualTo(new[] { "original" }));
                Assert.That(() => ((IList<string>)snapshot.ValidationErrors)[0] = "escape", Throws.TypeOf<NotSupportedException>());
                Assert.That(RejectsMutableSnapshot("public List<string> ValidationErrors { get; set; }"), Is.True);
            });
        }

        [Test, Category("NegativeFixture")]
        public void DuplicateUpstreamSubscriber_GuardRequiresOneCoordinatorAttachPerSurface()
        {
            var source = ReadSource("src/Services/Project/ThermalStateCoordinator.cs");
            Assert.That(Count(source, "_climateDataImpl.DataChanged += _climateUpstreamHandler;"), Is.EqualTo(1));
            Assert.That(Count(source, "_constructionData.DataChanged += _constructionUpstreamHandler;"), Is.EqualTo(1));
            Assert.That(CountUpstreamSubscriptions("x.DataChanged += handler;\nx.DataChanged += handler;"), Is.EqualTo(2));
        }

        [Test, Category("NegativeFixture")]
        public void DiIndependentStateRegistration_GuardRejectsIndependentDescriptorsAndInstances()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            Assert.That(CountIndependentThermalDescriptors(services), Is.Zero);
            services.AddSingleton<IProjectSessionThermalState>(new ProjectSessionThermalState());
            Assert.That(CountIndependentThermalDescriptors(services), Is.EqualTo(1));
            Assert.That(RejectsIndependentDiRegistration("services.AddSingleton<IProjectSessionThermalState>();"), Is.True);
        }

        private static string ReadSource(string relativePath) =>
            File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string FindRepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "SnowMeltingCalculator.sln")))
                directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
            Assert.That(directory, Is.Not.Empty, "Could not locate repository root.");
            return directory;
        }

        private static string[] FindMatches(string source, string pattern) =>
            Regex.Matches(source, pattern).Select(match => match.Value).ToArray();

        private static string[] FindDirectThermalAssignmentsBeforeRestore(string source)
        {
            var restoreIndex = source.IndexOf("_thermalState.Restore(", StringComparison.Ordinal);
            var prefix = restoreIndex < 0 ? source : source[..restoreIndex];
            return Regex.Matches(prefix, @"_thermalViewModel\.(?<property>SelectedMode|SupplyTemperature|GroundTemperature|SelectedPipe|PipeSpacing)\s*=(?!=)")
                .Select(match => match.Groups["property"].Value).ToArray();
        }

        private static string ExtractMethod(string source, string start, string nextDeclaration)
        {
            var startIndex = source.IndexOf(start, StringComparison.Ordinal);
            Assert.That(startIndex, Is.GreaterThanOrEqualTo(0));
            var endIndex = source.IndexOf(nextDeclaration, startIndex + start.Length, StringComparison.Ordinal);
            return endIndex < 0 ? source[startIndex..] : source[startIndex..endIndex];
        }

        private static string[] FindUnapprovedWriterFiles(IEnumerable<(string File, string Source)> files) =>
            files.Where(item => Regex.IsMatch(item.Source, @"\bUpdateThermal(?:Inputs|Result)?\s*\("))
                .Where(item => !item.File.Equals("ThermalStateCoordinator.cs", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.File).ToArray();

        private static int Count(string source, string needle) =>
            Regex.Matches(source, Regex.Escape(needle)).Count;

        private static int CountIndependentThermalDescriptors(IServiceCollection services) =>
            services.Count(descriptor => descriptor.ServiceType == typeof(IProjectSessionThermalState) || descriptor.ServiceType == typeof(ProjectSessionThermalState));

        internal static bool RejectsVmWritableStore(string source) =>
            Regex.IsMatch(source, @"private\s+(?:bool|string)\s+_thermal(?:NeedsRecalculation|IsCalculating|ValidationMessage)\b");

        internal static bool RejectsServiceStore(string source) =>
            Regex.IsMatch(source, @"private\s+(?:bool|string|int)\s+_(?:thermal|pipeSpacing)[A-Za-z0-9_]*\b");

        internal static bool RejectsNonCanonicalSave(string source) =>
            Regex.IsMatch(source, @"_thermalViewModel\.(?:SelectedMode|SupplyTemperature|GroundTemperature|SelectedPipe|PipeSpacing)\b|_calculationStateService\.PipeSpacing\b");

        internal static bool RejectsMutableSnapshot(string source) =>
            Regex.IsMatch(source, @"(?:List(?:<[^>]+>)?|string\[\]|ThermalCalculationResult)\s+ValidationErrors\s*\{[^}]*set;");

        internal static int CountUpstreamSubscriptions(string source) =>
            Regex.Matches(source, @"\b[A-Za-z_][A-Za-z0-9_]*\.DataChanged\s*\+=").Count;

        internal static bool RejectsIndependentDiRegistration(string source) =>
            Regex.IsMatch(source, @"Add(?:Singleton|Scoped|Transient)<\s*(?:IProjectSessionThermalState|ProjectSessionThermalState)\s*>");
    }
}
