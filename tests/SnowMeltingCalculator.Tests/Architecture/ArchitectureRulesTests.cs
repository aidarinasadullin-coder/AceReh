using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Architecture
{
    /// <summary>
    /// Пост-миграционные архитектурные правила R1–R6 (шесть инвариантов из
    /// корневого AGENTS.md). R1 и R4-reflect — структурные проверки через
    /// reflection; R2/R3/R5 и R4-using — сканирование исходников, дословно
    /// переносящее принятый evidence writer-inventory фазы 10 (8/8 PASS,
    /// ADR-003 в docs/architecture/README.md). R6 (.smc wire compatibility)
    /// здесь не переизобретается: его держат persistence fixture- и
    /// hash-наборы (ProjectSnapshotContractTests и связанные).
    /// Изменение списков санкционированных writers = изменение правила —
    /// только через запись в журнал docs/architecture/README.md.
    /// </summary>
    [TestFixture]
    public class ArchitectureRulesTests
    {
        private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

        private static string RepoRoot => RepoRootLazy.Value;

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SnowMeltingCalculator.sln")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null,
                "Architecture scan: repository root (SnowMeltingCalculator.sln) not found above "
                + AppContext.BaseDirectory);
            return dir!.FullName;
        }

        private static List<string> SourceFiles() =>
            Directory.EnumerateFiles(Path.Combine(RepoRoot, "src"), "*.cs", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .ToList();

        private static List<string> CallSites(List<string> files, string pattern, bool ignoreCase, string? onlyFileName = null)
        {
            var rx = new Regex(pattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            var sites = new List<string>();
            foreach (var file in files)
            {
                if (onlyFileName is not null && !file.EndsWith("/" + onlyFileName, StringComparison.Ordinal))
                    continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                        continue;
                    if (rx.IsMatch(line))
                        sites.Add($"{file}:{i + 1}");
                }
            }
            return sites;
        }

        private static void AssertSanctioned(string ruleId, List<string> sites, params string[] allowedFiles)
        {
            var bad = sites.Where(site =>
            {
                var path = site.Substring(0, site.LastIndexOf(':'));
                var name = path.Substring(path.LastIndexOf('/') + 1);
                return !allowedFiles.Contains(name);
            }).ToList();
            Assert.That(bad, Is.Empty,
                $"{ruleId} violated (root AGENTS.md invariants). Non-sanctioned writers:\n  "
                + string.Join("\n  ", bad));
        }

        // R1 — ProjectSession is the aggregate root with explicit slices.

        [Test]
        public void R1_ProjectSession_IsAggregateRoot_WithFourExplicitSlices()
        {
            var session = typeof(ProjectSession);
            Assert.That(session.GetInterfaces(), Does.Contain(typeof(IProjectSession)),
                "R1: ProjectSession must implement IProjectSession.");
            Assert.That(session.GetInterfaces(), Does.Contain(typeof(IMarkDirtyService)),
                "R1: ProjectSession must implement IMarkDirtyService (canonical dirty boundary).");
            AssertSlice(session, "ClimateState", typeof(IProjectSessionClimateState));
            AssertSlice(session, "ConstructionState", typeof(IProjectSessionConstructionState));
            AssertSlice(session, "ThermalState", typeof(IProjectSessionThermalState));
            AssertSlice(session, "HydraulicsState", typeof(IProjectSessionHydraulicsState));
        }

        private static void AssertSlice(Type session, string propertyName, Type sliceInterface)
        {
            var property = session.GetProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"R1: ProjectSession.{propertyName} slice is missing.");
            Assert.That(property!.PropertyType, Is.EqualTo(sliceInterface),
                $"R1: ProjectSession.{propertyName} must expose exactly {sliceInterface.Name}.");
        }

        // R2 — each value has exactly one writable canonical owner.
        // Patterns and sanction lists are ported verbatim from the accepted
        // Phase 10 writer-inventory evidence (WI-1..WI-7).

        [Test]
        public void R2_ClimateState_MutatedOnlyBySanctionedWriters()
        {
            var sites = CallSites(SourceFiles(),
                @"climateState\.(ApplyCitySelection|ApplyIndividualEdit|ApplyProjectSnapshot|ResetToCityData)\(", true);
            AssertSanctioned("R2/WI-1 (ClimateState)", sites,
                "ProjectSessionClimateState.cs", "ClimateViewModel.cs", "ProjectLoadOrchestrator.cs",
                "MainViewModel.cs", "ResultsViewModel.cs", "ProjectSession.cs");
        }

        [Test]
        public void R2_ConstructionState_MutatedOnlyBySanctionedWriters()
        {
            var sites = CallSites(SourceFiles(),
                @"constructionState\.(Apply|ApplySnapshot|ResetToDefaults)\(", true);
            // ConstructionStateLegacyStoreGuardTests.cs is inert under the
            // src-only scan; kept for verbatim parity with the evidence.
            AssertSanctioned("R2/WI-2 (ConstructionState)", sites,
                "ProjectSessionConstructionState.cs", "ConstructionViewModel.cs", "ProjectLoadOrchestrator.cs",
                "MainViewModel.cs", "ConstructionDefaultStateInitializer.cs", "ConstructionStateLegacyStoreGuardTests.cs");
        }

        [Test]
        public void R2_ThermalState_MutatedOnlyBySanctionedWriters()
        {
            var files = SourceFiles();
            var sites = CallSites(files,
                @"thermalState\.(ApplyInputs|ApplyInputEdit|ApplyNeedsRecalculation|BeginCalculation|CompleteCalculation|FailCalculation|Restore|InvalidateFromClimate|InvalidateFromConstruction)\(", true);
            // The coordinator holds its slice as a generic `_state` field;
            // that receiver is scoped to the owning coordinator file.
            sites.AddRange(CallSites(files,
                @"_state\.(ApplyInputs|ApplyInputEdit|ApplyNeedsRecalculation|BeginCalculation|CompleteCalculation|FailCalculation|Restore|InvalidateFromClimate|InvalidateFromConstruction)\(",
                true, "ThermalStateCoordinator.cs"));
            AssertSanctioned("R2/WI-3 (ThermalState)", sites,
                "ProjectSessionThermalState.cs", "ThermalStateCoordinator.cs", "CalculationStateService.cs",
                "ProjectLoadOrchestrator.cs", "ThermalViewModel.cs");
        }

        [Test]
        public void R2_HydraulicsState_MutatedOnlyBySanctionedWriters()
        {
            var files = SourceFiles();
            var sites = CallSites(files,
                @"hydraulicsState\.(ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ApplySnapshot)\(", true);
            sites.AddRange(CallSites(files,
                @"_state\.(ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ApplySnapshot)\(",
                true, "HydraulicsStateCoordinator.cs"));
            AssertSanctioned("R2/WI-4 (HydraulicsState)", sites,
                "ProjectSessionHydraulicsState.cs", "HydraulicsStateCoordinator.cs", "CircuitsViewModel.cs",
                "ProjectLoadOrchestrator.cs", "CalculationStateService.cs");
        }

        [Test]
        public void R2_SessionDirtyAndIdentity_MutatedOnlyBySanctionedWriters()
        {
            var files = SourceFiles();
            var dirty = CallSites(files, @"\.MarkDirty\(\)", false);
            AssertSanctioned("R2/WI-5 (MarkDirty)", dirty,
                "ProjectSession.cs", "ProjectSessionClimateState.cs", "ProjectSessionConstructionState.cs",
                "ProjectSessionThermalState.cs", "ProjectSessionHydraulicsState.cs",
                "ThermalStateCoordinator.cs", "HydraulicsStateCoordinator.cs", "ResultsViewModel.cs");
            var clean = CallSites(files, @"\.MarkClean\(\)", false);
            AssertSanctioned("R2/WI-6 (MarkClean)", clean,
                "ProjectSession.cs", "ResultsViewModel.cs", "MainViewModel.cs");
        }

        [Test]
        public void R2_CalculationContextProjection_WrittenOnlyBySanctionedWriters()
        {
            // ST-020..ST-022 compatibility projection, DEC-001 = A: exactly
            // the four sanctioned projection writers plus the load/save shell.
            var sites = CallSites(SourceFiles(),
                @"calculationContext\.(UpdateClimate|UpdateConstruction|UpdateThermal|UpdateThermalInputs|UpdateHydraulics|Reset)\(", true);
            AssertSanctioned("R2/WI-7 (CalculationContext projection)", sites,
                "ProjectSessionClimateState.cs", "ProjectSessionConstructionState.cs", "ThermalStateCoordinator.cs",
                "HydraulicsStateCoordinator.cs", "MainViewModel.cs", "ProjectLoadOrchestrator.cs");
        }

        // R3 — ViewModels are WPF adapters, not canonical state stores.

        [Test]
        public void R3_ViewModels_MutateOnlyTheirOwnSlice()
        {
            var vmSlices = new Dictionary<string, string[]>
            {
                ["ClimateViewModel.cs"] = new[] { "ClimateState" },
                ["ConstructionViewModel.cs"] = new[] { "ConstructionState" },
                ["ThermalViewModel.cs"] = new[] { "ThermalState" },
                ["CircuitsViewModel.cs"] = new[] { "HydraulicsState" },
            };
            var foreign = new List<string>();
            foreach (var file in SourceFiles())
            {
                var name = file.Substring(file.LastIndexOf('/') + 1);
                if (!vmSlices.ContainsKey(name))
                    continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                        continue;
                    foreach (var pair in vmSlices)
                    {
                        if (pair.Key == name)
                            continue;
                        foreach (var slice in pair.Value)
                        {
                            if (Regex.IsMatch(line, slice + @"\.\s*(Apply|Replace|Reset|Begin|Complete|Fail|Invalidate|Restore)"))
                                foreign.Add($"{name}:{i + 1} writes {slice}");
                        }
                    }
                }
            }
            Assert.That(foreign, Is.Empty,
                "R3 violated: a ViewModel wrote a foreign slice (WI-8):\n  " + string.Join("\n  ", foreign));
        }

        // R4 — Services do not depend on concrete ViewModels.
        // The single sanctioned exception is ADR-002: the two Results
        // builders read ViewModels.Results read-model records.

        [Test]
        public void R4_Services_DoNotDependOnViewModels()
        {
            var violations = new List<string>();
            foreach (var type in typeof(ProjectSession).Assembly.GetTypes())
            {
                var ns = type.Namespace ?? string.Empty;
                if (!ns.StartsWith("SnowMeltingCalculator.Services", StringComparison.Ordinal))
                    continue;
                foreach (var dependency in ReferencedTypes(type))
                {
                    var depNs = dependency.Namespace ?? string.Empty;
                    if (!depNs.StartsWith("SnowMeltingCalculator.ViewModels", StringComparison.Ordinal))
                        continue;
                    if (depNs == "SnowMeltingCalculator.ViewModels.Results")
                        continue; // ADR-002 sanctioned exception.
                    violations.Add($"{type.FullName} -> {dependency.FullName}");
                }
            }
            Assert.That(violations, Is.Empty,
                "R4 violated: Services depend on ViewModels outside the ADR-002 exception:\n  "
                + string.Join("\n  ", violations));
        }

        [Test]
        public void R4_Services_ViewModelsUsings_OnlySanctionedResultsBuilders()
        {
            var rx = new Regex(@"^\s*using\s+SnowMeltingCalculator\.ViewModels");
            var bad = new List<string>();
            var servicesDir = Path.Combine(RepoRoot, "src", "Services");
            foreach (var file in Directory.EnumerateFiles(servicesDir, "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                        continue;
                    if (rx.IsMatch(line))
                        bad.Add($"{file.Replace('\\', '/')}:{i + 1}");
                }
            }
            var sanctioned = new[] { "ResultsPdfDataBuilder.cs", "HydraulicSummaryBuilder.cs" };
            var violations = bad.Where(site =>
            {
                var path = site.Substring(0, site.LastIndexOf(':'));
                var name = path.Substring(path.LastIndexOf('/') + 1);
                return !sanctioned.Contains(name);
            }).ToList();
            Assert.That(violations, Is.Empty,
                "R4 violated: only ResultsPdfDataBuilder.cs and HydraulicSummaryBuilder.cs may use "
                + "ViewModels namespaces in src/Services (ADR-002):\n  " + string.Join("\n  ", violations));
        }

        private static IEnumerable<Type> ReferencedTypes(Type type)
        {
            if (type.BaseType is not null)
                foreach (var t in Expand(type.BaseType))
                    yield return t;
            foreach (var iface in type.GetInterfaces())
                foreach (var t in Expand(iface))
                    yield return t;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var field in type.GetFields(flags))
                foreach (var t in Expand(field.FieldType))
                    yield return t;
            foreach (var property in type.GetProperties(flags))
                foreach (var t in Expand(property.PropertyType))
                    yield return t;
            foreach (var @event in type.GetEvents(flags))
                if (@event.EventHandlerType is not null)
                    foreach (var t in Expand(@event.EventHandlerType))
                        yield return t;
            foreach (var ctor in type.GetConstructors(flags))
                foreach (var parameter in ctor.GetParameters())
                    foreach (var t in Expand(parameter.ParameterType))
                        yield return t;
            foreach (var method in type.GetMethods(flags))
            {
                foreach (var t in Expand(method.ReturnType))
                    yield return t;
                foreach (var parameter in method.GetParameters())
                    foreach (var t in Expand(parameter.ParameterType))
                        yield return t;
            }
        }

        private static IEnumerable<Type> Expand(Type type)
        {
            yield return type;
            if (type.IsArray)
            {
                foreach (var t in Expand(type.GetElementType()!))
                    yield return t;
                yield break;
            }
            if (type.IsGenericType)
            {
                foreach (var argument in type.GetGenericArguments())
                    foreach (var t in Expand(argument))
                        yield return t;
            }
        }

        // R5 — Results is derived and does not own module inputs.
        // ResultsViewModel's sanctioned footprint is the identity-adapter
        // MarkDirty/MarkClean and the Phase 10-sanctioned ClimateState
        // Apply sites (WI-1/WI-5/WI-6 allowlists).

        [Test]
        public void R5_Results_IsDerivedProjection_DoesNotOwnModuleInputs()
        {
            var file = SourceFiles().Single(f => f.EndsWith("/ResultsViewModel.cs", StringComparison.Ordinal));
            var violations = new List<string>();
            void Check(string label, string pattern)
            {
                var sites = CallSites(new List<string> { file }, pattern, true);
                if (sites.Count > 0)
                    violations.Add($"{label}: {string.Join(", ", sites)}");
            }
            Check("ConstructionState write",
                @"constructionState\.(Apply|ApplySnapshot|ResetToDefaults)\(");
            Check("ThermalState write",
                @"thermalState\.(ApplyInputs|ApplyInputEdit|ApplyNeedsRecalculation|BeginCalculation|CompleteCalculation|FailCalculation|Restore|InvalidateFromClimate|InvalidateFromConstruction)\(");
            Check("HydraulicsState write",
                @"hydraulicsState\.(ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ApplySnapshot)\(");
            Check("CalculationContext write",
                @"calculationContext\.(UpdateClimate|UpdateConstruction|UpdateThermal|UpdateThermalInputs|UpdateHydraulics|Reset)\(");
            Assert.That(violations, Is.Empty,
                "R5 violated: Results must not own module inputs:\n  " + string.Join("\n  ", violations));
        }

        // R6 — .smc wire compatibility: enforced by the persistence
        // fixture/hash-pin suites (ProjectSnapshotContractTests and
        // related), not re-implemented here.
    }
}
