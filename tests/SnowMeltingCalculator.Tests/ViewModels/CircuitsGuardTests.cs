using System.Threading;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Гварды честности гидравлического расчёта (волна 3 hardening-роадмапа,
    /// решение владельца D9): нулевой шаг подводки и NaN-свойства
    /// теплоносителя останавливают расчёт валидационным сообщением вместо
    /// тихого Infinity/NaN в результатах; fallback-допущения 35/30 °C
    /// показываются в InfoMessage и не блокируют публикацию.
    /// Тесты работают со штатным коллектором, создаваемым конструктором VM
    /// (AddCollector создаёт 2 контура с нулевой длиной).
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class CircuitsGuardTests
    {
        [Test]
        public void Calculate_ZeroSupplySpacing_StopsWithValidationMessage()
        {
            var viewModel = ResultsViewModelTestGraph.CreateCircuitsViewModel();
            var collector = viewModel.Collectors[0];
            collector.Circuits[0].CircuitLength = 50;
            collector.Circuits[0].SupplySpacing_cm = 0;

            viewModel.CalculateCommand.Execute(null);

            var hydraulicsState = ResultsViewModelTestGraph.GetField<
                IProjectSessionHydraulicsState>(viewModel, "_hydraulicsState");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ValidationMessage, Does.Contain("Шаг подводки"));
                Assert.That(viewModel.ValidationMessage, Does.Contain("контур 1"));
                Assert.That(viewModel.ValidationMessage, Does.Contain("пересчитайте"));
                Assert.That(hydraulicsState.Snapshot.IsCalculated(), Is.False,
                    "Расчёт откатывается: канон не получает результатов для контура с " +
                    "нулевым шагом подводки (UI-объект Summary контура может оставаться " +
                    "от прошлого расчёта — принятая семантика ADR-012 note).");
                // Волна 3.5: Error-статус модуля + красная заливка чипов —
                // индикация не выглядит «успешной» при откате. В тестовой
                // композиции Error читается с того же канона (Status
                // проставлен FailCalculation'ом после null-возврата расчёта).
                Assert.That(hydraulicsState.Snapshot.Status.ValidationMessage,
                    Does.Contain("Шаг подводки"),
                    "Канон в Error-статусе с текстом гварда: вкладка Error, не зелёная.");
                Assert.That(viewModel.HasCalculationError, Is.True,
                    "Чипы сводки залиты красным тинтом при ошибке расчёта.");
                Assert.That(viewModel.HasCalculationNotice, Is.False);
            });
        }

        [Test]
        public void Calculate_NaNGlycolProperties_StopsWithRecommendation()
        {
            var nanGlycol = new GlycolProperties
            {
                Density = double.NaN,
                SpecificHeat = double.NaN,
                KinematicViscosity = double.NaN
            };
            var viewModel = ResultsViewModelTestGraph.CreateCircuitsViewModel(glycolProperties: nanGlycol);
            var collector = viewModel.Collectors[0];
            collector.Circuits[0].CircuitLength = 50;

            viewModel.CalculateCommand.Execute(null);

            var hydraulicsState = ResultsViewModelTestGraph.GetField<
                IProjectSessionHydraulicsState>(viewModel, "_hydraulicsState");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ValidationMessage, Does.Contain("вне диапазона базы"));
                Assert.That(viewModel.ValidationMessage, Does.Contain("требуется концентрация ≥ 30 %"),
                    "Динамический порог (волна 3.6): для расчётной температуры называется " +
                    "конкретная минимальная концентрация из матрицы.");
                Assert.That(viewModel.ValidationMessage, Does.Contain("повысьте"),
                    "Направление рекомендации: при замерзании концентрацию повышают (D9), " +
                    "а не снижают.");
                Assert.That(viewModel.ValidationMessage, Does.Not.Contain("снизьте"));
                Assert.That(viewModel.ValidationMessage, Does.Contain("Этиленгликоль"),
                    "Имя типа — на русском, как в UI.");
                Assert.That(hydraulicsState.Snapshot.IsCalculated(), Is.False,
                    "Расчёт откатывается: канон не получает результатов от NaN-свойств " +
                    "теплоносителя (UI-объект Summary контура может оставаться от " +
                    "прошлого расчёта — принятая семантика ADR-012 note).");
                Assert.That(viewModel.HasCalculationError, Is.True,
                    "Чипы сводки залиты красным тинтом при ошибке расчёта.");
            });
        }

        [Test]
        public void Calculate_WithoutThermalResult_ShowsFallbackNotice()
        {
            var viewModel = ResultsViewModelTestGraph.CreateCircuitsViewModel();
            var collector = viewModel.Collectors[0];
            collector.Circuits[0].CircuitLength = 50;

            viewModel.CalculateCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.InfoMessage, Does.Contain("Тепловой результат недоступен"));
                Assert.That(viewModel.InfoMessage, Does.Contain("35/30"));
                Assert.That(viewModel.ValidationMessage, Is.Empty,
                    "Fallback — не ошибка: расчёт от допущений валиден и публикуется.");
                Assert.That(collector.Summary, Is.Not.Null,
                    "Fallback-расчёт публикует сводку, как и прежде.");
                // Волна 3.5: янтарная заливка чипов при допущениях.
                Assert.That(viewModel.HasCalculationNotice, Is.True,
                    "Чипы сводки залиты янтарным при допущениях 35/30.");
                Assert.That(viewModel.HasCalculationError, Is.False);
            });
        }

        [Test]
        public void Calculate_GuardRollback_ClearsStaleTableRowFields()
        {
            // Вариант «а» (волна 3.6): таблица не держит числа прошлого
            // успешного расчёта после гвард-отката. Прогрев даёт расчётные
            // поля (мок-результаты), битый шаг — откат с очисткой.
            var viewModel = ResultsViewModelTestGraph.CreateCircuitsViewModel();
            var collector = viewModel.Collectors[0];
            collector.Circuits[0].CircuitLength = 50;

            viewModel.CalculateCommand.Execute(null);
            Assert.That(collector.Circuits[0].OperatingResult, Is.Not.Null,
                "guard: прогрев заполнил расчётные поля строки.");

            collector.Circuits[0].SupplySpacing_cm = 0;
            viewModel.CalculateCommand.Execute(null);

            var cleared = collector.Circuits[0];
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ValidationMessage, Does.Contain("Шаг подводки"));
                Assert.That(cleared.OperatingResult, Is.Null);
                Assert.That(cleared.DesignResult, Is.Null);
                Assert.That(cleared.Power, Is.Zero);
                Assert.That(cleared.FlowRate, Is.Zero);
                Assert.That(cleared.Velocity, Is.Zero);
                Assert.That(cleared.Throttling, Is.Zero);
                Assert.That(cleared.ValveTurns, Is.Zero);
                Assert.That(cleared.ValveTurnsWarning, Is.Null);
                Assert.That(cleared.IsReferenceCircuit, Is.False);
            });
        }

        [Test]
        public void Calculate_WithValidThermalResult_ClearsFallbackNotice()
        {
            var viewModel = ResultsViewModelTestGraph.CreateCircuitsViewModel();
            var collector = viewModel.Collectors[0];
            collector.Circuits[0].CircuitLength = 50;
            var context = ResultsViewModelTestGraph.GetField<CalculationContext>(
                viewModel, "_calculationContext");
            context.ThermalResult = new ThermalCalculationResult
            {
                IsValid = true,
                SupplyTemperature = 40,
                ReturnTemperature = 32,
                PowerUp = 120,
                PowerDown = 60,
                DeltaT = 8,
                MeanTemperature = 36
            };

            viewModel.CalculateCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.InfoMessage, Is.Empty,
                    "При валидном тепловом результате допущения не упоминаются.");
                Assert.That(viewModel.ValidationMessage, Is.Empty);
                Assert.That(collector.Summary, Is.Not.Null);
            });
        }
    }
}
