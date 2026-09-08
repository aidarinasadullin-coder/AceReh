using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Tests.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.History
{
    /// <summary>
    /// Этапы 2–3 плана undo/redo (ADR-014): группировка событийного
    /// memento-дневника, тотальное подавление при загрузке, гейт расчёта,
    /// точка чистоты, лимит истории и интеграция зеркал адаптеров —
    /// на production-shaped графе ReactiveGraph.
    /// </summary>
    [TestFixture]
    public class UndoRedoServiceJournalTests
    {
        private ReactiveSubscriptionLifecycleTests.ReactiveGraph _graph = null!;

        [SetUp]
        public void SetUp()
        {
            ReactiveSubscriptionLifecycleTests.ResetAppSettingsSingleton();
            _graph = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateProductionShaped();
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            ReactiveSubscriptionLifecycleTests.ResetAppSettingsSingleton();
        }

        [Test]
        public async Task CitySelection_GroupsIntoOneEntry_WithClimateThermalAndHydraulics()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));
            Assert.That(_graph.UndoRedo.CanUndo, Is.False, "Sanity: load under the guard records nothing (P0-1).");

            var city = new CityInfo { Name = "Тестоград", Region = "Тест", T5Days092 = -30.0, WindAvgTempLe8 = 4.0, Humidity15hCold = 75.0, Period_0_Days = 220 };
            _graph.Session.ClimateState.ApplyCitySelection(city, isHighRequirements: false, ClimateMutationOrigin.User);
            _graph.UndoRedo.FlushPendingForTests();

            Assert.That(_graph.UndoRedo.UndoStackForTests, Has.Count.EqualTo(1),
                "One user action (city selection) = exactly one journal entry.");
            Assert.That(_graph.UndoRedo.UndoDescription, Is.EqualTo("Выбор города"));
            var entry = _graph.UndoRedo.UndoStackForTests[0];
            Assert.That(entry.Slices.Keys, Is.EquivalentTo(new[]
            {
                SnowMeltingCalculator.Services.History.UndoSliceKind.Climate,
                SnowMeltingCalculator.Services.History.UndoSliceKind.Thermal,
                SnowMeltingCalculator.Services.History.UndoSliceKind.Hydraulics
            }), "The entry must cover climate, thermal invalidation and the hydraulics cascade (plan §1.11).");
        }

        [Test]
        public void PerCharacterEdits_AreStitchedIntoSingleEntry()
        {
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 3.0), ClimateMutationOrigin.User);
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 4.0), ClimateMutationOrigin.User);
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 5.0), ClimateMutationOrigin.User);

            Assert.That(_graph.UndoRedo.UndoStackForTests, Has.Count.EqualTo(0),
                "The group is still open (silence window) — nothing pushed yet.");

            _graph.UndoRedo.FlushPendingForTests();

            Assert.That(_graph.UndoRedo.UndoStackForTests, Has.Count.EqualTo(1),
                "Consecutive user edits within the silence window stitch into one entry.");
            Assert.That(_graph.UndoRedo.UndoDescription, Is.EqualTo("Изменение климатических данных"));
        }

        [Test]
        public void HeaderCalculate_OpensStandaloneCalculationEntry_AndClosesOnSilence()
        {
            var before = _graph.Session.ThermalState.Snapshot;
            _graph.Session.ThermalState.BeginCalculation();
            Assert.That(_graph.Session.ThermalState.Snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Calculating));

            var result = ThermalResultSnapshot.FromResult(new ThermalCalculationResult { PowerTotal = 42.5, IsValid = true })!;
            _graph.Session.ThermalState.CompleteCalculation(before.Inputs, result, string.Empty);

            Assert.That(_graph.UndoRedo.CanUndo, Is.False, "The entry is still open — no push yet.");

            _graph.UndoRedo.FlushPendingForTests();

            Assert.That(_graph.UndoRedo.UndoDescription, Is.EqualTo("Расчёт"));

            _graph.UndoRedo.Undo();
            Assert.That(_graph.Session.ThermalState.Snapshot.Result, Is.Null,
                "Undoing the calculation entry restores the pre-calculation thermal state.");
        }

        [Test]
        public async Task LoadProject_ClearsJournal_IncludingPhantomFallbackEntries()
        {
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 3.0), ClimateMutationOrigin.User);
            _graph.UndoRedo.FlushPendingForTests();
            Assert.That(_graph.UndoRedo.CanUndo, Is.True);
            _graph.Session.MarkClean(); // не dirtу: диалог подтверждения замены пропускается моком

            // Production-путь открытия: LoadProjectFromPathAsync →
            // ApplyLoadedProjectAsync (Clear ДО загрузки).
            _graph.FileServiceMock
                .Setup(f => f.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(SnowMeltingCalculator.Core.Results.OperationResult<ProjectData>.Success(
                    ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200)));
            await _graph.ResultsVm.LoadProjectFromPathAsync("загрузка.smc");

            Assert.That(_graph.UndoRedo.CanUndo, Is.False,
                "Open clears the journal; the load-time fallback/cascade must not create phantom entries (P0-1).");
        }

        [Test]
        public void UndoDuringCalculation_IsBlocked_CompletesAfterCalculation()
        {
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 3.0), ClimateMutationOrigin.User);
            _graph.UndoRedo.FlushPendingForTests();
            Assert.That(_graph.UndoRedo.CanUndo, Is.True);

            // Гейт расчёта (P1-3): фаза Calculating гасит CanUndo/CanRedo.
            _graph.Session.ThermalState.BeginCalculation();
            Assert.That(_graph.UndoRedo.CanUndo, Is.False, "Ctrl+Z mid-calculation must be blocked.");
            Assert.That(_graph.UndoRedo.CanRedo, Is.False);

            var result = ThermalResultSnapshot.FromResult(new ThermalCalculationResult { PowerTotal = 42.5, IsValid = true })!;
            _graph.Session.ThermalState.CompleteCalculation(_graph.Session.ThermalState.Snapshot.Inputs, result, string.Empty);
            _graph.UndoRedo.FlushPendingForTests();

            Assert.That(_graph.UndoRedo.CanUndo, Is.True, "After the calculation completes, the gate opens again.");
        }

        [Test]
        public async Task UndoThenEdit_KillsRedo()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            _graph.ThermalVm.SupplyTemperature = 61.0;
            _graph.UndoRedo.FlushPendingForTests();
            _graph.UndoRedo.Undo();
            Assert.That(_graph.UndoRedo.CanRedo, Is.True, "Sanity: undo makes redo available.");

            _graph.ThermalVm.SupplyTemperature = 63.0;
            Assert.That(_graph.UndoRedo.CanRedo, Is.False, "A new user edit kills the redo branch (Word-style).");
        }

        [Test]
        public async Task Save_SetsCleanPoint_UndoBackToSavedState_ClearsDirty()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "undo-clean-point.smc");
            _graph.Session.CurrentFilePath = filePath;
            _graph.FileServiceMock
                .Setup(f => f.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(SnowMeltingCalculator.Core.Results.OperationResult<object?>.Success(null!));

            // Правка A → сохранение (точка чистоты на позиции 1).
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 3.0), ClimateMutationOrigin.User);
            _graph.UndoRedo.FlushPendingForTests();
            await _graph.ResultsVm.SaveProjectCommand.ExecuteAsync(null);
            Assert.That(_graph.Session.IsDirty, Is.False, "Sanity: saved.");

            // Правка B поверх сохранённого — проект снова грязный.
            _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 4.0), ClimateMutationOrigin.User);
            Assert.That(_graph.Session.IsDirty, Is.True);
            _graph.UndoRedo.FlushPendingForTests();

            _graph.UndoRedo.Undo();

            Assert.That(_graph.Session.IsDirty, Is.False,
                "Undoing exactly back to the saved state must clear the dirty star (acceptance #3).");
        }

        [Test]
        public void HistoryLimit_TenEntries_EvictsOldest()
        {
            // 12 отдельных правок с закрытием группы после каждой.
            for (var i = 1; i <= 12; i++)
            {
                _graph.Session.ClimateState.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.WindSpeed, 1.0 + i), ClimateMutationOrigin.User);
                _graph.UndoRedo.FlushPendingForTests();
            }

            Assert.That(_graph.UndoRedo.UndoStackForTests, Has.Count.EqualTo(10),
                "The journal keeps at most 10 entries (owner decision §1.3).");

            for (var i = 0; i < 10; i++)
            {
                _graph.UndoRedo.Undo();
            }

            Assert.That(_graph.UndoRedo.CanUndo, Is.False, "The 12th and 11th actions are gone forever.");
        }

        [Test]
        public void Undo_DoesNotRecordItself_AndKeepsRedoIntact()
        {
            _graph.ThermalVm.SupplyTemperature = 61.0;
            _graph.UndoRedo.FlushPendingForTests();

            var undoCountBefore = _graph.UndoRedo.UndoStackForTests.Count;
            _graph.UndoRedo.Undo();

            Assert.That(_graph.UndoRedo.UndoStackForTests.Count, Is.EqualTo(undoCountBefore - 1),
                "The undo application must not append new entries (triple suppression).");
            Assert.That(_graph.UndoRedo.CanRedo, Is.True);

            _graph.UndoRedo.Redo();
            Assert.That(_graph.UndoRedo.UndoStackForTests.Count, Is.EqualTo(undoCountBefore),
                "Redo restores the entry without recording itself.");
        }

        [Test]
        public async Task Undo_CitySelection_RestoresClimateThermalResultAndMirrors()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));
            var climateBefore = _graph.Session.ClimateState.Snapshot;
            var thermalBefore = _graph.Session.ThermalState.Snapshot;
            Assert.That(thermalBefore.Result, Is.Not.Null, "Sanity: loaded project has a thermal result.");

            var city = new CityInfo { Name = "Тестоград", Region = "Тест", T5Days092 = -30.0, WindAvgTempLe8 = 4.0, Humidity15hCold = 75.0, Period_0_Days = 220 };
            _graph.Session.ClimateState.ApplyCitySelection(city, isHighRequirements: false, ClimateMutationOrigin.User);
            _graph.UndoRedo.FlushPendingForTests();
            Assert.That(_graph.Session.ThermalState.Snapshot.Result, Is.Null, "Sanity: the user edit invalidated the thermal result.");

            _graph.UndoRedo.Undo();

            Assert.That(_graph.Session.ClimateState.Snapshot, Is.EqualTo(climateBefore), "Climate is restored.");
            Assert.That(_graph.Session.ThermalState.Snapshot, Is.EqualTo(thermalBefore), "Thermal inputs, result and status are restored in one action.");
            Assert.That(_graph.ThermalVm.Result, Is.Not.Null, "The ThermalViewModel adapter mirror restored its Result binding.");
            Assert.That(_graph.ResultsVm.SelectedCity, Is.EqualTo(climateBefore.SelectedCity), "The open Results tab refreshes on undo.");
        }

        [Test]
        public async Task Undo_CircuitEdit_RestoresOnlyHydraulics()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            // Пользователь добавляет коллектор с контуром и правит длину.
            _graph.CircuitsVm.AddCollectorCommand.Execute(null);
            var added = _graph.CircuitsVm.Collectors[^1];
            Assert.That(added.Circuits, Is.Not.Empty, "Sanity: fresh collector carries default circuits.");
            _graph.UndoRedo.FlushPendingForTests(); // AddCollector — отдельная запись

            var hydraulicsBefore = _graph.Session.HydraulicsState.Snapshot;
            var thermalBefore = _graph.Session.ThermalState.Snapshot;

            added.Circuits[0].CircuitLength = 77.0;
            Assert.That(_graph.Session.HydraulicsState.Snapshot, Is.Not.EqualTo(hydraulicsBefore), "Sanity: the edit changed hydraulics.");
            _graph.UndoRedo.FlushPendingForTests();

            Assert.That(_graph.UndoRedo.UndoDescription, Is.EqualTo("Изменение коллекторов/контуров"));
            var thermalCompletions = 0;
            _graph.Session.ThermalState.Changed += (_, _) => thermalCompletions++;

            _graph.UndoRedo.Undo();

            Assert.That(_graph.Session.HydraulicsState.Snapshot, Is.EqualTo(hydraulicsBefore), "The hydraulics edit is undone.");
            Assert.That(thermalCompletions, Is.Zero, "Undoing a hydraulics-only edit must not touch the thermal slice.");
            Assert.That(_graph.Session.ThermalState.Snapshot, Is.EqualTo(thermalBefore));
            Assert.That(_graph.CircuitsVm.Collectors[^1].Circuits[0].CircuitLength,
                Is.EqualTo(hydraulicsBefore.Collectors.Last().Circuits[0].CircuitLength),
                "The CircuitsViewModel adapter mirror restored the grid binding.");
        }

        [Test]
        public async Task ConstructionUndo_MirrorsAdapterCollections()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            var layer = new ConstructionLayerSnapshot(Guid.NewGuid(), 1, "Пеноплекс", 40.0, 0.031, false, LayerPosition.AbovePipe, 0);
            var target = new ConstructionStateSnapshot(2.0, new[] { layer }, Array.Empty<ConstructionLayerSnapshot>());
            _graph.Session.ConstructionState.ApplySnapshot(target, ConstructionMutationOrigin.User);
            _graph.UndoRedo.FlushPendingForTests();
            Assert.That(_graph.Session.ConstructionState.Snapshot.LayersAbovePipe, Has.Count.EqualTo(1),
                "Sanity: the canonical edit is in place (User edits originate in the adapter and do not mirror).");

            _graph.UndoRedo.Undo();

            Assert.That(_graph.ConstructionVm.LayersAbovePipe, Is.Empty,
                "The ConstructionViewModel adapter mirror restored the collection.");
        }
    }
}
