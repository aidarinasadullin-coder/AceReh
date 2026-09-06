using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// DE-3 (characterization-гейт): KPI-блок и строки Calculation Methods
    /// ResultsViewModel зафиксированы ДО выноса презентера «канонический
    /// snapshot + режим → KPI/строки». Формулы и обнуления пинятся через
    /// публичную поверхность VM; после выноса тесты обязаны остаться
    /// зелёными без правок (поведение 1:1).
    /// Дыры, не покрытые ResultsStabilizationPhase1BehaviorContractsTests
    /// (мощность/длины/количества там уже пинятся): объём системы,
    /// расширительный бак, насос (расход + макс. потери по режиму),
    /// строки селектора коллекторов, сводка выбранного коллектора.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ResultsKpiCharacterizationTests
    {
        private ProjectStateService _projectStateService = null!;

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
        }

        private static HydraulicCollectorSnapshot CreateCanonicalCollector(
            int number,
            int circuitCount,
            HydraulicCollectorSummarySnapshot? summary = null)
        {
            var circuits = new HydraulicCircuitSnapshot[circuitCount];
            for (var i = 0; i < circuitCount; i++)
            {
                circuits[i] = new HydraulicCircuitSnapshot(i + 1, 50, 10, 5, 10, 20);
            }

            return new HydraulicCollectorSnapshot(
                number, "HKV-D (2-12 контуров)", ValveType.HKV_D, circuits, summary);
        }

        private async Task<ResultsViewModel> CreateReadyViewModelWithCollectorsAsync(
            params HydraulicCollectorSnapshot[] collectors)
        {
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(
                _projectStateService, circuitsVm);

            // Ready-фикстура несёт трубу RAUTHERM S 20x2,0 (внутренний диаметр 16 мм)
            // и валидный тепловой результат — канонический ThermalState заполнен.
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);

            _projectStateService.Session.HydraulicsState.ReplaceCollectors(
                collectors, HydraulicsMutationOrigin.Calculation);
            viewModel.RefreshAll();
            return viewModel;
        }

        [Test]
        public async Task RefreshAll_SystemVolumeAndExpansionTank_UseCanonicalPipeAndCircuitLengths()
        {
            // Длины контуров: (50+10) + (60+12) = 132 м; труба фикстуры — 16 мм.
            var collector = new HydraulicCollectorSnapshot(
                1, "HKV-D (2-12 контуров)", ValveType.HKV_D,
                new[]
                {
                    new HydraulicCircuitSnapshot(1, 50, 10, 5, 10, 20),
                    new HydraulicCircuitSnapshot(2, 60, 12, 5, 10, 20)
                },
                new HydraulicCollectorSummarySnapshot(2, 132, 2600, 650, 2500, 2700, 4.5, "HKV-D"));

            var viewModel = await CreateReadyViewModelWithCollectorsAsync(collector);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsDataReady, Is.True,
                    "guard: ready-фикстура обязана дать валидные модули, включая трубу в каноне");
                Assert.That(viewModel.TotalThermalPower_kW, Is.EqualTo(2.6).Within(1e-9));
                Assert.That(viewModel.TotalPipeLength, Is.EqualTo(132).Within(1e-9));

                // V = π × d²/4 × L × 1000 (л), d = 16 мм = 0.016 м
                var expectedVolume = Math.PI * Math.Pow(0.016, 2) / 4.0 * 132.0 * 1000.0;
                Assert.That(viewModel.SystemVolume_L, Is.EqualTo(expectedVolume).Within(1e-9));

                // Бак: V × β(0.034) × 1.2, дублируется в ExpansionTankV
                Assert.That(viewModel.ExpansionTankVolume_L,
                    Is.EqualTo(expectedVolume * 0.034 * 1.2).Within(1e-9));
                Assert.That(viewModel.ExpansionTankV,
                    Is.EqualTo(viewModel.ExpansionTankVolume_L).Within(1e-9));
            });
        }

        [Test]
        public async Task PumpKpi_SummarizesFlowAndSelectsMaxLossByMode()
        {
            var first = CreateCanonicalCollector(1, 1,
                new HydraulicCollectorSummarySnapshot(1, 60, 5000, 720, 24000, 22000, 2.2, "HKV-D"));
            var second = CreateCanonicalCollector(2, 1,
                new HydraulicCollectorSummarySnapshot(1, 60, 4000, 360, 30000, 28000, 3.1, "HKV-D"));

            var viewModel = await CreateReadyViewModelWithCollectorsAsync(first, second);

            Assert.Multiple(() =>
            {
                // Расход: (720 + 360) л/ч → 1.08 м³/ч; напор: max потерь = 30000 Па → 30 кПа
                Assert.That(viewModel.PumpFlowRate_m3h, Is.EqualTo(1.08).Within(1e-9));
                Assert.That(viewModel.PumpQ, Is.EqualTo(viewModel.PumpFlowRate_m3h).Within(1e-9));
                Assert.That(viewModel.PumpHead_kPa, Is.EqualTo(30).Within(1e-9));
                Assert.That(viewModel.PumpH, Is.EqualTo(viewModel.PumpHead_kPa).Within(1e-9));
            });

            // Расчётный холодный режим: max потерь = 28000 Па → 28 кПа; расход не меняется
            viewModel.ToggleModeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsOperatingMode, Is.False);
                Assert.That(viewModel.PumpHead_kPa, Is.EqualTo(28).Within(1e-9));
                Assert.That(viewModel.PumpH, Is.EqualTo(viewModel.PumpHead_kPa).Within(1e-9));
                Assert.That(viewModel.PumpFlowRate_m3h, Is.EqualTo(1.08).Within(1e-9));
            });

            // Обратное переключение возвращает рабочий напор
            viewModel.ToggleModeCommand.Execute(null);
            Assert.That(viewModel.PumpHead_kPa, Is.EqualTo(30).Within(1e-9));
        }

        [Test]
        public async Task RefreshAll_EmptyCanonicalCollectors_ZerosKpiWithoutStaleValues()
        {
            var collector = CreateCanonicalCollector(1, 2,
                new HydraulicCollectorSummarySnapshot(2, 120, 9000, 800, 21000, 23000, 4.0, "HKV-D"));

            var viewModel = await CreateReadyViewModelWithCollectorsAsync(collector);
            Assert.That(viewModel.TotalThermalPower_kW, Is.EqualTo(9).Within(1e-9),
                "guard: до сброса канона KPI ненулевые");

            _projectStateService.Session.HydraulicsState.ReplaceCollectors(
                Array.Empty<HydraulicCollectorSnapshot>(), HydraulicsMutationOrigin.Calculation);
            viewModel.RefreshAll();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.TotalThermalPower_kW, Is.Zero);
                Assert.That(viewModel.SystemVolume_L, Is.Zero);
                Assert.That(viewModel.TotalPipeLength, Is.Zero);
                Assert.That(viewModel.PumpFlowRate_m3h, Is.Zero);
                Assert.That(viewModel.PumpQ, Is.Zero);
                Assert.That(viewModel.PumpHead_kPa, Is.Zero);
                Assert.That(viewModel.PumpH, Is.Zero);
                Assert.That(viewModel.ExpansionTankVolume_L, Is.Zero);
                Assert.That(viewModel.ExpansionTankV, Is.Zero);

                Assert.That(viewModel.Collectors, Is.Empty);
                Assert.That(viewModel.Circuits, Is.Empty);
                Assert.That(viewModel.CollectorSpecifications, Is.Empty);
                Assert.That(viewModel.CollectorSummary, Is.Null);
                Assert.That(viewModel.RzsCount, Is.Zero);
                Assert.That(viewModel.SelectedCollectorIndex, Is.EqualTo(-1));
            });
        }

        [Test]
        public async Task RefreshAll_CollectorSelectorRows_ContourWordFlowAndSelectionRestore()
        {
            var first = CreateCanonicalCollector(1, 1,
                new HydraulicCollectorSummarySnapshot(1, 60, 2000, 500, 12000, 13000, 1.1, "HKV-D"));
            var second = CreateCanonicalCollector(2, 3,
                new HydraulicCollectorSummarySnapshot(3, 180, 6000, 1500, 14000, 15000, 2.2, "HKV-D"));
            var third = CreateCanonicalCollector(3, 5,
                new HydraulicCollectorSummarySnapshot(5, 300, 10000, 2500, 16000, 17000, 3.3, "HKV-D"));
            var fourth = CreateCanonicalCollector(4, 12,
                new HydraulicCollectorSummarySnapshot(12, 720, 24000, 6000, 18000, 19000, 4.4, "HKV-D"));

            var viewModel = await CreateReadyViewModelWithCollectorsAsync(
                first, second, third, fourth);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Collectors, Has.Count.EqualTo(4));
                Assert.That(viewModel.Collectors[0].DisplayName, Is.EqualTo("Коллектор №1 (1 контур)"));
                Assert.That(viewModel.Collectors[1].DisplayName, Is.EqualTo("Коллектор №2 (3 контура)"));
                Assert.That(viewModel.Collectors[2].DisplayName, Is.EqualTo("Коллектор №3 (5 контуров)"));
                // 11–19 → «контуров»
                Assert.That(viewModel.Collectors[3].DisplayName, Is.EqualTo("Коллектор №4 (12 контуров)"));

                // Расход в строке селектора — в м³/ч (канонический л/ч / 1000)
                Assert.That(viewModel.Collectors[0].TotalFlowRate, Is.EqualTo(0.5).Within(1e-9));
                Assert.That(viewModel.Collectors[1].TotalFlowRate, Is.EqualTo(1.5).Within(1e-9));
                Assert.That(viewModel.Collectors[2].TotalFlowRate, Is.EqualTo(2.5).Within(1e-9));
                Assert.That(viewModel.Collectors[3].TotalFlowRate, Is.EqualTo(6.0).Within(1e-9));

                Assert.That(viewModel.RzsCount, Is.EqualTo(4));
                Assert.That(viewModel.SelectedCollectorIndex, Is.EqualTo(0));
                Assert.That(viewModel.Collectors[0].IsSelected, Is.True);
                Assert.That(viewModel.Collectors[1].IsSelected, Is.False);
            });

            // Выбор третьего коллектора переживает RefreshAll.
            // Характеризация семантики: команда принимает НОМЕР коллектора
            // (не индекс) — №4 живёт на индексе 3.
            viewModel.SelectCollectorCommand.Execute(4);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedCollectorIndex, Is.EqualTo(3));
                Assert.That(viewModel.Collectors[3].IsSelected, Is.True);
                Assert.That(viewModel.Collectors[0].IsSelected, Is.False);
            });

            viewModel.RefreshAll();

            // Характеризация предсуществующего дефекта (не чинится в DE-3, гейт
            // «поведение 1:1»): UpdateCollectorsList пересоздаёт строки с
            // IsSelected=(i==0), а восстановление SelectedCollectorIndex=3 —
            // no-op сеттера (значение не изменилось → хук не вызывается →
            // UpdateCollectorSelectionState не выполняется). Итог: индекс
            // указывает на №4, флаг подсветки остаётся на №1. Кандидат на
            // решение владельца вне DE-серии.
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedCollectorIndex, Is.EqualTo(3),
                    "предыдущий выбор сохраняется при перестроении списка");
                Assert.That(viewModel.Collectors[3].IsSelected, Is.False,
                    "стейл-флаг: новая строка не получает подсветку сохранённого выбора");
                Assert.That(viewModel.Collectors[0].IsSelected, Is.True,
                    "подсветка остаётся на первой строке (дефолт конструктора строки)");
            });
        }

        [Test]
        public async Task RefreshAll_CollectorSummary_MapsCanonicalSnapshotAndNullForMissingSummary()
        {
            var withSummary = new HydraulicCollectorSnapshot(
                7, "IV 1¼\" (2-12 контуров)", ValveType.IV_1_25,
                new[]
                {
                    new HydraulicCircuitSnapshot(1, 60, 0, 5, 10, 15),
                    new HydraulicCircuitSnapshot(2, 60, 0, 5, 10, 15),
                    new HydraulicCircuitSnapshot(3, 60, 0, 5, 10, 15)
                },
                new HydraulicCollectorSummarySnapshot(3, 180, 12000, 720, 24000, 0, 1.45, "IV"));
            var withoutSummary = CreateCanonicalCollector(8, 1);

            var viewModel = await CreateReadyViewModelWithCollectorsAsync(withSummary, withoutSummary);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.CollectorSummary, Is.Not.Null);
                Assert.That(viewModel.CollectorSummary!.CollectorNumber, Is.EqualTo(7));
                Assert.That(viewModel.CollectorSummary.CollectorType, Is.EqualTo("IV"));
                Assert.That(viewModel.CollectorSummary.CircuitCount, Is.EqualTo(3));
                Assert.That(viewModel.CollectorSummary.TotalPipeLength, Is.EqualTo(180).Within(1e-9));
                Assert.That(viewModel.CollectorSummary.TotalPower, Is.EqualTo(12000).Within(1e-9));
                Assert.That(viewModel.CollectorSummary.TotalFlowRate, Is.EqualTo(720).Within(1e-9));
                Assert.That(viewModel.CollectorSummary.PressureLoss_Operating_Pa, Is.EqualTo(24000).Within(1e-9));
                Assert.That(viewModel.CollectorSummary.PressureLoss_Cold_Pa, Is.Zero);
                Assert.That(viewModel.CollectorSummary.Kv, Is.EqualTo(1.45).Within(1e-9));
                Assert.That(viewModel.TotalCircuits, Is.EqualTo(3));
                Assert.That(viewModel.TotalFlowRate, Is.EqualTo(720).Within(1e-9));
                Assert.That(viewModel.MaxPressureLoss, Is.EqualTo(24000).Within(1e-9));
            });

            // Коллектор без сводки в каноне → null-сводка, а не нули
            viewModel.SelectCollectorCommand.Execute(8);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedCollectorIndex, Is.EqualTo(1));
                Assert.That(viewModel.CollectorSummary, Is.Null);
                Assert.That(viewModel.MaxPressureLoss, Is.Zero);
            });
        }
    }
}
