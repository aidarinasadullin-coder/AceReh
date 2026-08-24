using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public sealed class HydraulicsStateLegacyStoreGuardTests
    {
        [Test, Category("NegativeFixture")]
        public void VmWritableStore_GuardRequiresStateMutationEntryPoints()
        {
            var source = ReadSource("src/ViewModels/Hydraulics/CircuitsViewModel.cs");
            Assert.Multiple(() =>
            {
                Assert.That(RejectsVmWritableStore(source), Is.False);
                Assert.That(source, Does.Contain("ApplyGlobalInputs"));
                Assert.That(source, Does.Contain("ReplaceCollectors"));
                Assert.That(RejectsVmWritableStore("_inputData.GlycolConcentration = value;"), Is.True);
            });

            var state = new ProjectSessionHydraulicsState();
            HydraulicsMutationOrigin? origin = null;
            state.Changed += (_, args) => origin = args.Origin;
            state.ApplyGlobalInputs(
                new HydraulicGlobalInputsSnapshot(GlycolType.Ethylene, 55, 5, 10),
                HydraulicsMutationOrigin.User);
            Assert.That(origin, Is.EqualTo(HydraulicsMutationOrigin.User));
        }

        [Test, Category("NegativeFixture")]
        public void ServiceHydraulicsStore_GuardRequiresSnapshotStatusTranslation()
        {
            var source = ReadSource("src/Services/Navigation/CalculationStateService.cs");
            Assert.Multiple(() =>
            {
                Assert.That(FindMatches(source, @"private\s+(?:bool|string)\s+_hydraulics[A-Za-z0-9_]*\b"), Is.Empty);
                Assert.That(RejectsServiceStore("private bool _hydraulicsIsCalculating;"), Is.True);
            });

            var session = new ProjectSession();
            var service = new CalculationStateService(session);
            session.HydraulicsState.BeginCalculation();
            Assert.That(service.HydraulicsIsCalculating, Is.True);
        }

        [Test, Category("NegativeFixture")]
        public void OrchestratorDirectAssign_GuardRequiresCanonicalRestore()
        {
            var source = ReadSource("src/Services/Project/ProjectLoadOrchestrator.cs");
            Assert.Multiple(() =>
            {
                Assert.That(FindDirectHydraulicsAssignments(source), Is.Empty);
                Assert.That(source, Does.Contain("_hydraulicsState.Restore("));
                Assert.That(FindDirectHydraulicsAssignments("_circuitsViewModel.InputData = data;"), Is.EqualTo(new[] { "InputData" }));
            });
        }

        [Test, Category("NegativeFixture")]
        public void ResultsNonCanonicalSave_GuardRequiresSessionSnapshotMapper()
        {
            var source = ReadSource("src/ViewModels/Results/ResultsViewModel.cs");
            var save = ExtractMethod(source, "public ProjectData SaveCurrentProject()", "private bool HasUnsavedData()");
            Assert.Multiple(() =>
            {
                Assert.That(save, Does.Contain("HydraulicsPersistenceMapper.BuildHydraulicsProjectData"));
                Assert.That(save, Does.Contain("_projectSession.HydraulicsState.Snapshot"));
                Assert.That(FindMatches(save, @"_circuitsViewModel\.(?:BuildCanonicalSnapshot|InputData|Collectors)\b"), Is.Empty);
                Assert.That(RejectsNonCanonicalSave("var snapshot = _circuitsViewModel.BuildCanonicalSnapshot();"), Is.True);
            });
        }

        [Test, Category("NegativeFixture")]
        public void ContextUnapprovedWriter_GuardAllowsOnlyApprovedProductionWriters()
        {
            var root = FindRepositoryRoot();
            var writers = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFileNameWithoutExtension(path), "CalculationContext", StringComparison.OrdinalIgnoreCase))
                .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bUpdateHydraulics\s*\("))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(writers, Is.EqualTo(new[] { "HydraulicsStateCoordinator" }));
                Assert.That(FindUnapprovedWriterFiles(new[] { ("Synthetic.cs", "context.UpdateHydraulics(items);") }), Is.EqualTo(new[] { "Synthetic.cs" }));
            });

            var thermalWriters = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFileNameWithoutExtension(path), "CalculationContext", StringComparison.OrdinalIgnoreCase))
                .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bUpdateThermal(?:Inputs|Result)?\s*\("))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.That(thermalWriters, Is.EqualTo(new[] { "ThermalStateCoordinator" }));
        }

        [Test, Category("NegativeFixture")]
        public void SnapshotMutability_GuardDefensivelyCopiesCollections()
        {
            var source = ReadSource("src/Services/Project/HydraulicsStateSnapshots.cs");
            var snapshotTypes = new[]
            {
                "HydraulicGlobalInputsSnapshot", "HydraulicCircuitResultSnapshot", "HydraulicCollectorSummarySnapshot",
                "HydraulicCircuitSnapshot", "HydraulicCollectorSnapshot", "HydraulicsStatusSnapshot", "HydraulicsStateSnapshot"
            };
            Assert.Multiple(() =>
            {
                Assert.That(FindMatches(source, @"public\s+[^\n]+\{\s*get;\s*set;\s*\}"), Is.Empty);
                Assert.That(snapshotTypes.All(type => source.Contains($"sealed class {type}", StringComparison.Ordinal)), Is.True);
                Assert.That(RejectsMutableSnapshot("public List<HydraulicCollectorSnapshot> Collectors { get; set; }"), Is.True);
            });

            var collectors = HydraulicsStateSnapshot.Default.Collectors;
            Assert.That(() => ((IList<HydraulicCollectorSnapshot>)collectors).Add(new HydraulicCollectorSnapshot(1, "", ValveType.HKV_D, null)), Throws.TypeOf<NotSupportedException>());
        }

        [Test, Category("NegativeFixture")]
        public void DuplicateUpstreamSubscriber_GuardRequiresCoordinatorOnlyInProduction()
        {
            var coordinatorSource = ReadSource("src/Services/Project/HydraulicsStateCoordinator.cs");
            var vmSource = ReadSource("src/ViewModels/Hydraulics/CircuitsViewModel.cs");
            Assert.Multiple(() =>
            {
                Assert.That(Count(coordinatorSource, "ContextChanged +="), Is.EqualTo(1));
                Assert.That(Count(coordinatorSource, "PipeSpacingChanged +="), Is.EqualTo(1));
                Assert.That(Count(coordinatorSource, "StateChanged +="), Is.EqualTo(1));
                Assert.That(FindFallbackSubscriptions(vmSource), Is.Zero);
                Assert.That(RejectsDuplicateSubscriber("if (coordinator == null) context.ContextChanged += handler;"), Is.True);
            });

            using var provider = new ServiceCollection().AddApplicationServices().BuildServiceProvider();
            var coordinator = provider.GetRequiredService<IHydraulicsStateCoordinator>();
            var viewModel = provider.GetRequiredService<SnowMeltingCalculator.ViewModels.Hydraulics.CircuitsViewModel>();
            var field = typeof(SnowMeltingCalculator.ViewModels.Hydraulics.CircuitsViewModel)
                .GetField("_coordinator", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field?.GetValue(viewModel), Is.SameAs(coordinator));
        }

        [Test, Category("NegativeFixture")]
        public void DiIndependentStateRegistration_GuardRequiresProjectSessionOwnership()
        {
            var services = new ServiceCollection().AddApplicationServices();
            Assert.Multiple(() =>
            {
                Assert.That(CountIndependentStateDescriptors(services), Is.Zero);
                Assert.That(RejectsIndependentDiRegistration("services.AddSingleton<IProjectSessionHydraulicsState>();"), Is.True);
            });

            using var provider = services.BuildServiceProvider();
            var session = provider.GetRequiredService<ProjectSession>();
            Assert.That(provider.GetRequiredService<IProjectSession>().HydraulicsState, Is.SameAs(session.HydraulicsState));
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

        private static string[] FindDirectHydraulicsAssignments(string source)
        {
            var restoreIndex = source.IndexOf("_hydraulicsState.Restore(", StringComparison.Ordinal);
            var prefix = restoreIndex < 0 ? source : source[..restoreIndex];
            return Regex.Matches(prefix, @"_circuitsViewModel\.(?<property>InputData|Collectors|HydraulicsResults)\s*=(?!=)")
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
            files.Where(item => Regex.IsMatch(item.Source, @"\bUpdateHydraulics\s*\("))
                .Where(item => !item.File.Equals("HydraulicsStateCoordinator.cs", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.File).ToArray();

        private static int Count(string source, string needle) =>
            Regex.Matches(source, Regex.Escape(needle)).Count;

        private static int FindFallbackSubscriptions(string source)
        {
            return Count(source, "ContextChanged +=")
                + Count(source, "PipeSpacingChanged +=")
                + Count(source, "StateChanged +=");
        }

        private static int CountIndependentStateDescriptors(IServiceCollection services) =>
            services.Count(descriptor => descriptor.ServiceType == typeof(IProjectSessionHydraulicsState)
                || descriptor.ServiceType == typeof(ProjectSessionHydraulicsState));

        internal static bool RejectsVmWritableStore(string source) =>
            Regex.IsMatch(source, @"_inputData\.[A-Za-z0-9_]+\s*=(?!=)");

        internal static bool RejectsServiceStore(string source) =>
            Regex.IsMatch(source, @"private\s+(?:bool|string)\s+_hydraulics[A-Za-z0-9_]*\b");

        internal static bool RejectsNonCanonicalSave(string source) =>
            Regex.IsMatch(source, @"_circuitsViewModel\.(?:BuildCanonicalSnapshot|InputData|Collectors)\b");

        internal static bool RejectsMutableSnapshot(string source) =>
            Regex.IsMatch(source, @"(?:List<[^>]+>|[^\s]+\[\])\s+\w+\s*\{[^}]*set;");

        internal static bool RejectsDuplicateSubscriber(string source) =>
            Regex.IsMatch(source, @"ContextChanged\s*\+=");

        internal static bool RejectsIndependentDiRegistration(string source) =>
            Regex.IsMatch(source, @"Add(?:Singleton|Scoped|Transient)<\s*(?:IProjectSessionHydraulicsState|ProjectSessionHydraulicsState)\s*>");
    }
}
