using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Shell;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 10 (INV-016): consolidated mutation-boundary acceptance across all
    /// migrated slices. Every user-visible mutation crosses a public
    /// state/application boundary and produces exactly one identifiable
    /// logical-change completion boundary; load/reset/restore/system-apply
    /// origins are distinguishable and create no user dirty or history
    /// candidate. Public surfaces only — no ViewModel internals, no
    /// interception of setters or commands, no undo/redo implementation.
    /// </summary>
    [TestFixture]
    public class MutationBoundaryConsolidationTests
    {
        private ReactiveSubscriptionLifecycleTests.ReactiveGraph _graph = null!;

        [SetUp]
        public void SetUp()
        {
            ResetAppSettingsSingleton();
            _graph = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateProductionShaped();
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            ResetAppSettingsSingleton();
        }

        #region Climate — multi-field single action through the adapter boundary

        [Test]
        public void Climate_UserCitySelection_ChangesManyFieldsThroughExactlyOneCompletionBoundary()
        {
            var completions = new List<ClimateStateChangedEventArgs>();
            _graph.Session.ClimateState.Changed += (_, args) => completions.Add(args);

            var city = new CityInfo { Name = "Норильск", T5Days092 = -45 };
            _graph.ClimateVm.SelectedCity = city;

            Assert.That(completions, Has.Count.EqualTo(1),
                "A single city selection is one logical user action and must complete exactly once.");
            Assert.That(completions[0].Origin, Is.EqualTo(ClimateMutationOrigin.User),
                "The completion args carry the user origin — the distinguishable boundary record.");

            var after = completions[0].NewSnapshot;
            var before = completions[0].OldSnapshot;
            Assert.That(after.SelectedCity, Is.Not.EqualTo(before.SelectedCity), "City identity changed…");
            Assert.That(after.ColdFiveDayTemperature, Is.Not.EqualTo(before.ColdFiveDayTemperature), "…cold five-day temperature changed…");
            Assert.That(after.AirTemperature, Is.Not.EqualTo(before.AirTemperature), "…and the derived air temperature changed — many fields, one commit.");

            Assert.That(_graph.Counters.ContextClimate, Is.EqualTo(1),
                "Exactly one CalculationContext.Climate projection publication accompanies the commit.");
            Assert.That(_graph.Counters.DirtyRaised, Is.EqualTo(1), "One dirty intent for one changed user action.");
        }

        #endregion

        #region Construction — one layer action, one commit

        [Test]
        public void Construction_UserAddLayer_OneCanonicalCommitForTheWholeLayerChange()
        {
            var completions = new List<ConstructionStateChangedEventArgs>();
            _graph.Session.ConstructionState.Changed += (_, args) => completions.Add(args);

            _graph.ConstructionVm.AddLayerAbovePipeCommand.Execute(null);

            Assert.That(completions, Has.Count.EqualTo(1),
                "Adding one layer mutates collection, order and thickness data through exactly one completion boundary.");
            Assert.That(completions[0].Origin, Is.EqualTo(ConstructionMutationOrigin.User));
            Assert.That(completions[0].After.LayersAbovePipe.Count,
                Is.EqualTo(completions[0].Before.LayersAbovePipe.Count + 1),
                "The commit carries the whole layer-list change, not per-field fragments.");

            Assert.That(_graph.Counters.ContextConstruction, Is.EqualTo(1),
                "Exactly one CalculationContext.Construction publication for the commit (RE-009).");
            Assert.That(_graph.Counters.DirtyRaised, Is.EqualTo(1));
        }

        #endregion

        #region Thermal — mode + temperatures + spacing in one commit

        [Test]
        public void Thermal_UserApplyInputs_ModeTemperatureSpacingInOneCommit()
        {
            var completions = new List<ThermalStateChangedEventArgs>();
            _graph.Session.ThermalState.Changed += (_, args) => completions.Add(args);

            var candidate = new ThermalInputsSnapshot(
                OperatingMode.Intensive,
                supplyTemperature: 70.0,
                groundTemperature: 12.0,
                pipe: null,
                pipeSpacing: 300);
            var result = _graph.Session.ThermalState.ApplyInputs(candidate, ThermalMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True, "Sanity: the candidate differs from defaults.");
            Assert.That(completions, Has.Count.EqualTo(1),
                "Mode, temperatures and spacing are many internal fields but ONE logical user commit.");
            Assert.That(completions[0].Mutation.Origin, Is.EqualTo(ThermalMutationOrigin.User));

            var after = completions[0].Mutation.After;
            Assert.That(after.Inputs.Mode, Is.EqualTo(OperatingMode.Intensive));
            Assert.That(after.Inputs.SupplyTemperature, Is.EqualTo(70.0));
            Assert.That(after.Inputs.GroundTemperature, Is.EqualTo(12.0));
            Assert.That(after.Inputs.PipeSpacing, Is.EqualTo(300));

            // DEC-T03 dirty ownership: the canonical slice completes the change;
            // the coordinator adapter is the explicit dirty-intent owner. The
            // dirty intent is proven on the adapter boundary:
            _graph.Session.MarkClean();
            var adapterDirty = new List<bool>();
            _graph.Session.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IProjectSession.IsDirty))
                {
                    adapterDirty.Add(_graph.Session.IsDirty);
                }
            };
            _graph.ThermalVm.SupplyTemperature = 66.0;
            Assert.That(adapterDirty, Is.EqualTo(new[] { true }),
                "One adapter-level user edit = exactly one dirty-intent transition.");
        }

        #endregion

        #region Hydraulics — user circuit edit through the adapter boundary

        [Test]
        public void Hydraulics_UserCircuitEdit_CrossesOneCanonicalBoundaryWithOneCommit()
        {
            _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                    OperatingMode.Melting, 55.0, 8.0, 200)).GetAwaiter().GetResult();
            _graph.CircuitsVm.AddCollectorCommand.Execute(null);
            var added = _graph.CircuitsVm.Collectors[^1];
            _graph.Session.MarkClean();
            var dirtyBefore = _graph.Counters.DirtyRaised;

            var completions = new List<HydraulicsStateChangedEventArgs>();
            _graph.Session.HydraulicsState.Changed += (_, args) => completions.Add(args);

            added.Circuits[0].CircuitLength = 42.0;

            Assert.That(completions.Count(c => c.Origin == HydraulicsMutationOrigin.User), Is.EqualTo(1),
                "One circuit-length edit is one logical user action with exactly one User-origin canonical commit.");
            TestContext.Out.WriteLine($"[phase-10 consolidation] hydraulics origins: {string.Join(",", completions.Select(c => c.Origin))}");
            Assert.That(_graph.Counters.DirtyRaised, Is.EqualTo(dirtyBefore + 1));
        }

        #endregion

        #region Results — derived consumption is downstream-only

        [Test]
        public async Task Results_ProjectionReadsCanonicalStateWithoutCanonicalMutations()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                    OperatingMode.Melting, 55.0, 8.0, 200));

            var rebuilds = 0;
            _graph.ResultsVm.HydraulicSummaryCards.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    rebuilds++;
                }
            };

            var dirtyBefore = _graph.Counters.DirtyRaised;
            var climateCompletionsBefore = _graph.Counters.ClimateCompletions;

            _graph.ResultsVm.RefreshAll();

            Assert.That(_graph.ResultsVm.PipeSpacing, Is.EqualTo(200),
                "The Results projection reads the canonical ThermalState inputs (Phase 8 derived projection).");
            Assert.That(_graph.ResultsVm.OperatingMode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(_graph.Counters.DirtyRaised, Is.EqualTo(dirtyBefore),
                "A projection rebuild must not create dirty.");
            Assert.That(_graph.Counters.ClimateCompletions, Is.EqualTo(climateCompletionsBefore),
                "A projection rebuild must not mutate canonical state.");
            Assert.That(rebuilds, Is.EqualTo(1),
                "One RefreshAll = exactly one observable summary-cards rebuild.");
        }

        #endregion

        #region Shell/Save — one save action, one clean transition

        [Test]
        public async Task Shell_UserSaveCommand_ProducesOneBoundaryWriteAndOneCleanTransition()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                    OperatingMode.Melting, 55.0, 8.0, 200));
            _graph.Session.MarkDirty();
            var dirtyBefore = _graph.Counters.DirtyRaised;
            var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ph10-consolidation.smc");
            _graph.Session.CurrentFilePath = filePath;
            _graph.FileServiceMock
                .Setup(f => f.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Success(null!));
            Assert.That(_graph.Session.IsDirty, Is.True, "Sanity: the project is dirty before save.");

            await _graph.ResultsVm.SaveProjectCommand.ExecuteAsync(null);

            Assert.That(_graph.Session.IsDirty, Is.False,
                "A successful save transitions the project clean through the existing boundary.");
            Assert.That(_graph.Counters.CleanTransitions, Is.EqualTo(1),
                "Exactly one clean transition for one save action (Phase 6 save boundary).");
            Assert.That(_graph.Counters.DirtyRaised, Is.EqualTo(dirtyBefore),
                "The save itself must not raise new dirty beyond the deliberate pre-save dirty.");
        }

        #endregion

        #region Lifecycle and system origins — distinguishable, never user dirty

        [Test]
        public async Task LoadResetRestoreAndSystemApply_AreDistinguishableAndCreateNoUserDirty()
        {
            var projectA = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                OperatingMode.Melting, 55.0, 8.0, 200);
            var projectB = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                OperatingMode.Intensive, 60.0, 5.0, 300);

            var loadOrigins = new List<ClimateMutationOrigin>();
            var thermalOrigins = new List<ThermalMutationOrigin>();
            _graph.Session.ClimateState.Changed += (_, args) => loadOrigins.Add(args.Origin);
            _graph.Session.ThermalState.Changed += (_, args) => thermalOrigins.Add(args.Mutation.Origin);

            // load
            await _graph.ResultsVm.LoadProjectDataAsync(projectA);
            Assert.That(_graph.Session.IsDirty, Is.False);
            Assert.That(loadOrigins, Does.Contain(ClimateMutationOrigin.Load),
                "The load origin is visible on the public completion args (the future-recorder hook is a boundary property).");

            // reset
            _graph.Orchestrator.ResetModules();
            Assert.That(_graph.Session.IsDirty, Is.False);

            // restore
            using (_graph.Session.BeginProjectRestore())
            {
                await _graph.Orchestrator.RestoreModulesFromProjectAsync(projectB);
            }
            Assert.That(_graph.Session.IsDirty, Is.False);

            // system apply (the exact seam CalculationStateService.ResetThermalState uses)
            var systemApply = _graph.Session.ThermalState.ApplyInputs(
                _graph.Session.ThermalState.Snapshot.Inputs, ThermalMutationOrigin.SystemApply);
            Assert.That(systemApply.IsChanged, Is.False,
                "A same-value system apply is a no-change completion, not a user mutation.");
            Assert.That(_graph.Session.IsDirty, Is.False,
                "No lifecycle or system origin created a user dirty transition.");
            Assert.That(_graph.Counters.DirtyRaised, Is.EqualTo(0),
                "The whole lifecycle traffic raised zero user-dirty transitions.");
        }

        #endregion

        #region Plumbing

        private static void ResetAppSettingsSingleton()
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnowMeltingCalculator",
                "settings.json");
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            var field = typeof(AppSettings).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        #endregion
    }
}
