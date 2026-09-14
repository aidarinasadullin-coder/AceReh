using NUnit.Framework;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;
using System;
using System.Linq;

namespace SnowMeltingCalculator.Tests.Thermal
{
    /// <summary>
    /// Каталог советов v1 (план 2026-09-13-thermal-advice, §4-§5):
    /// ситуации → советы, устойчивый порядок, сверка с ThermalResultValidator
    /// по веткам обратки/ΔT&gt;30 (ветка ΔT≤0 — ранний возврат невалидного
    /// результата, вне скоупа сверки).
    /// </summary>
    [TestFixture]
    public class ThermalAdviceServiceTests
    {
        private ThermalAdviceService _service = null!;
        private ThermalResultValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _service = new ThermalAdviceService();
            _validator = new ThermalResultValidator();
        }

        private static ThermalCalculationResult Result(double meanTemperature, double supplyTemperature)
        {
            // Т_обратка и ΔT — по канону ThermalCalculator:481-484
            var returnTemperature = 2.0 * meanTemperature - supplyTemperature;
            return new ThermalCalculationResult
            {
                MeanTemperature = meanTemperature,
                SupplyTemperature = supplyTemperature,
                ReturnTemperature = returnTemperature,
                DeltaT = supplyTemperature - returnTemperature
            };
        }

        [Test]
        public void Build_Supply50_Mean146_ReturnsBothAdvicesInStableOrder()
        {
            var advice = _service.Build(Result(14.6, 50.0));

            Assert.Multiple(() =>
            {
                Assert.That(advice, Has.Count.EqualTo(2));
                Assert.That(advice[0].Id, Is.EqualTo("RETURN_NEGATIVE"));
                Assert.That(advice[1].Id, Is.EqualTo("DELTAT_MAX"));
                Assert.That(advice.Select(a => a.Severity),
                    Is.All.EqualTo(ThermalAdviceSeverity.Warning));
                Assert.That(advice.Select(a => a.TargetStep),
                    Is.All.EqualTo(NavigationTarget.Thermal));
                Assert.That(advice.Select(a => a.TargetTitle),
                    Is.All.EqualTo("Тепловой расчёт"));
            });
        }

        [Test]
        public void Build_ReturnNegative_MessageGuidesSupplyDown_WithCanonicalComma()
        {
            var advice = _service.Build(Result(14.6, 50.0));

            var message = advice.Single(a => a.Id == "RETURN_NEGATIVE").Message;

            Assert.Multiple(() =>
            {
                // Лечение — единая цель «для ΔT ≈ 15 К»: 14,6 + 15/2 = 22,1
                // (отзыв владельца 2026-09-14: граничный ориентир 2×T_средняя
                // вёл к блужданию между лимитами)
                Assert.That(message, Does.Contain("уменьшите"));
                Assert.That(message, Does.Contain("22,1"));
                Assert.That(message, Does.Contain("для ΔT ≈ 15 К"));
                // Обратка отформатирована той же культурой
                Assert.That(message, Does.Contain("-20,8"));
                // Перевёрнутый рычаг «увеличьте подачу» исключён (ревью P1-2)
                Assert.That(message, Does.Not.Contain("увеличьте подачу"));
            });
        }

        [Test]
        public void Build_DeltaTMax_MessageGuidesSupplyDown_WithoutGlycol()
        {
            var advice = _service.Build(Result(14.6, 50.0));

            var message = advice.Single(a => a.Id == "DELTAT_MAX").Message;

            Assert.Multiple(() =>
            {
                Assert.That(message, Does.Contain("70,8"));
                Assert.That(message, Does.Contain("30"));
                Assert.That(message, Does.Contain("уменьшите подачу до ≈22,1"));
                Assert.That(message, Does.Contain("для ΔT ≈ 15 К"));
                // Гликоль на ΔT не влияет (ревью P1-3): рычаг исключён
                Assert.That(message, Does.Not.Contain("гликол").And.Not.Contain("гликоль"));
            });
        }

        [Test]
        public void Build_BothAdvices_ShareSingleSupplyTarget()
        {
            // Отзыв владельца 2026-09-14: ориентир в обоих советах один —
            // ввод цели «ΔT ≈ 15 К» снимает оба нарушения за одну итерацию
            var advice = _service.Build(Result(14.6, 50.0));

            Assert.That(advice.Select(a => a.Message),
                Has.All.Contains("22,1 °C (для ΔT ≈ 15 К)"));
        }

        [Test]
        public void Build_Supply295_Mean146_ReturnsOnlyReturnNegative()
        {
            // Обратка -0,3 < 0; ΔT = 29,8 ≤ 30 — одно нарушение
            var advice = _service.Build(Result(14.6, 29.5));

            Assert.Multiple(() =>
            {
                Assert.That(advice, Has.Count.EqualTo(1));
                Assert.That(advice[0].Id, Is.EqualTo("RETURN_NEGATIVE"));
            });
        }

        [Test]
        public void Build_Supply28_Mean146_ReturnsEmpty()
        {
            // Обратка +1,2 ≥ 0; ΔT = 26,8 ≤ 30 — чистый расчёт, карточка скрыта
            var advice = _service.Build(Result(14.6, 28.0));

            Assert.That(advice, Is.Empty);
        }

        [Test]
        public void Build_AdviceCount_MatchesValidatorErrorCount_OnSameInputs()
        {
            double[] supplies = { 50.0, 29.5, 28.0 };

            foreach (var supply in supplies)
            {
                var result = Result(14.6, supply);
                var adviceCount = _service.Build(result).Count;
                var validationResult = _validator.Validate(result);
                var relevantErrors = validationResult.Errors
                    .Count(e => e.PropertyName is "ReturnTemperature" or "DeltaT");

                Assert.That(adviceCount, Is.EqualTo(relevantErrors),
                    $"Подача {supply}: набор советов должен совпадать с ошибками валидатора");
            }
        }

        [Test]
        public void Build_NullResult_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.Build(null!));
        }

        [Test]
        public void Build_TextUsesCanonicalCulture_IgnoringCurrentCulture()
        {
            // Числа советов — по AppCulture (ru-RU, запятая), не по CurrentCulture
            // (урок из 1984477): на en-US-машине разделитель не меняется.
            var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("en-US");

                var message = _service.Build(Result(14.6, 50.0))
                    .Single(a => a.Id == "RETURN_NEGATIVE").Message;

                Assert.Multiple(() =>
                {
                    Assert.That(message, Does.Contain("22,1"));
                    Assert.That(message, Does.Not.Contain("22.1"));
                });
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
