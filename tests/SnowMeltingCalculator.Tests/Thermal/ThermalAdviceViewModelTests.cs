using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Thermal
{
    /// <summary>
    /// Поведение ViewModel при активных советах (план thermal-advice §2,
    /// решение №8 вариант A; ревью реализации P2-3): при активных советах
    /// постоянный хинт «Рекомендуется: …» скрыт, показан хинт совета;
    /// на чистом результате — наоборот.
    /// </summary>
    [TestFixture]
    public class ThermalAdviceViewModelTests
    {
        private ThermalViewModel BuildViewModel()
        {
            var climate = new ClimateData { AirTemperature = -20.0, WindSpeed = 5.0, SnowfallIntensity = 2.0 };
            var construction = new ConstructionData { R1Total = 0.05, R2Total = 0.10, LambdaE = 1.6 };
            var markDirty = new Mock<IMarkDirtyService>();
            var calculationState = new Mock<ICalculationStateService>();
            var context = new CalculationContext();
            var session = new ProjectSession(climate, context);
            var coordinator = new ThermalStateCoordinator(
                session.ThermalState,
                context,
                markDirty.Object,
                new ThermalCalculator(),
                climate,
                construction,
                new ThermalValidator(new ThermalCalculator(), climate, construction),
                new ThermalResultValidator());

            return new ThermalViewModel(
                new ThermalCalculator(),
                climate,
                construction,
                calculationState.Object,
                context,
                new ThermalValidator(new ThermalCalculator(), climate, construction),
                new ThermalResultValidator(),
                markDirty.Object,
                coordinator);
        }

        [Test]
        public void AdviceActive_SupplyTemperatureHintHidden_AndAdviceSupplyHintShown()
        {
            var viewModel = BuildViewModel();
            viewModel.Result = new ThermalCalculationResult
            {
                MeanTemperature = 14.6,
                SupplyTemperature = 50.0,
                ReturnTemperature = -20.8,
                DeltaT = 70.8
            };

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.AdviceList, Has.Count.EqualTo(2));
                // Решение №8 (вариант A): постоянный «Рекомендуется: …» скрыт
                Assert.That(viewModel.SupplyTemperatureHint, Is.Empty);
                Assert.That(viewModel.AdviceSupplyHint, Does.Contain("уменьшите"));
                // Акцент чипа обратки (решение владельца 2026-09-15)
                Assert.That(viewModel.IsReturnTemperatureNegative, Is.True);
            });
        }

        [Test]
        public void CleanResult_RestoresRecommendationHint_AndHidesAdvice()
        {
            var viewModel = BuildViewModel();
            viewModel.Result = new ThermalCalculationResult
            {
                MeanTemperature = 14.6,
                SupplyTemperature = 28.0,
                ReturnTemperature = 1.2,
                DeltaT = 26.8
            };

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.AdviceList, Is.Empty);
                Assert.That(viewModel.SupplyTemperatureHint, Does.Contain("Рекомендуется"));
                Assert.That(viewModel.AdviceSupplyHint, Is.Null);
                Assert.That(viewModel.IsReturnTemperatureNegative, Is.False);
            });
        }

        [Test]
        public void NoResult_NoNegativeReturnAccent()
        {
            var viewModel = BuildViewModel();

            Assert.That(viewModel.IsReturnTemperatureNegative, Is.False,
                "Без результата акцента обратки нет.");
        }
    }
}
