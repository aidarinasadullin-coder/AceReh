using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Shell;
using ResultsVm = SnowMeltingCalculator.ViewModels.Results.ResultsViewModel;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Контракты каркаса Фазы 1 редизайна: состояния шагов степпера
    /// (✓ готов · ● черновик · ⚠ ошибка · ⟳ пересчёт), слоты статус-бара
    /// и read-only проекции SummaryViewModel. Только чтение состояния —
    /// инварианты R2/R3/R5 не затрагиваются.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ShellPhase1ViewModelTests
    {
        private ServiceProvider? _provider;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application();
            }
        }

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            _provider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            (_provider as IDisposable)?.Dispose();
            _provider = null;
        }

        private MainViewModel CreateMainViewModel() =>
            _provider!.GetRequiredService<MainViewModel>();

        [Test]
        public void ResultsStep_IsDraft_OnFreshProject()
        {
            var viewModel = CreateMainViewModel();

            var results = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Results);

            Assert.That(results.StepStatus, Is.EqualTo(StepStatus.Draft),
                "на чистом проекте данные не готовы — шаг «Результаты» в черновике");
        }

        [Test]
        public void ClimateStep_TurnsError_WhenValidationMessageAppears()
        {
            var viewModel = CreateMainViewModel();
            var climate = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Climate);

            Assert.That(climate.StepStatus, Is.Not.EqualTo(StepStatus.Error));

            viewModel.ClimateViewModel.ValidationMessage = "Проверьте параметры";

            Assert.That(climate.StepStatus, Is.EqualTo(StepStatus.Error),
                "непустая валидация Климата — шаг ⚠ (ревью Ф1, F3)");

            viewModel.ClimateViewModel.ValidationMessage = string.Empty;

            Assert.That(climate.StepStatus, Is.Not.EqualTo(StepStatus.Error),
                "очистка валидации возвращает шаг из ошибки");
        }

        [Test]
        public void ClimateStep_IsDraft_WhenNoCitySelected()
        {
            var viewModel = CreateMainViewModel();
            var climate = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Climate);

            Assert.That(climate.StepStatus, Is.EqualTo(StepStatus.Draft),
                "без выбранного города шаг Климата — черновик");
        }

        [Test]
        public void StatusBar_PlateText_FollowsSelectedModule()
        {
            var viewModel = CreateMainViewModel();

            viewModel.SelectedMenuItem = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Results);
            Assert.That(viewModel.CurrentModulePlateText, Is.EqualTo("РЕЗУЛЬТАТЫ"));
            Assert.That(viewModel.CurrentValidationText, Is.EqualTo(viewModel.ResultsViewModel.StatusMessage),
                "слот статус-бара показывает статус активного модуля");

            viewModel.SelectedMenuItem = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Climate);
            Assert.That(viewModel.CurrentModulePlateText, Is.EqualTo("КЛИМАТ"));
        }

        [Test]
        public void StatusBar_StatusKind_IsError_WhenValidationNonEmpty()
        {
            var viewModel = CreateMainViewModel();
            viewModel.SelectedMenuItem = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Climate);

            viewModel.ClimateViewModel.ValidationMessage = "Проверьте параметры";

            Assert.That(viewModel.CurrentStatusKind, Is.EqualTo(ShellStatusKind.Error));
            Assert.That(viewModel.CurrentValidationText, Is.EqualTo("Проверьте параметры"));

            viewModel.ClimateViewModel.ValidationMessage = string.Empty;

            Assert.That(viewModel.CurrentStatusKind, Is.EqualTo(ShellStatusKind.Success));
        }

        [Test]
        public void StatusBar_RecalcSlot_FollowsThermalNeedsRecalculation()
        {
            var viewModel = CreateMainViewModel();

            Assert.That(viewModel.CurrentRecalcText, Is.Empty,
                "на свежем проекте слот пересчёта пуст");

            // Реальный канонический переключатель состояния теплового модуля
            _provider!.GetRequiredService<ICalculationStateService>()
                .SetThermalNeedsRecalculation("Изменены параметры теплового расчёта");

            Assert.That(viewModel.CurrentRecalcText, Does.Contain("Изменены параметры"),
                "нужен пересчёт теплового — правый слот заполнен");

            var thermal = viewModel.MenuItems.Single(m => m.Target == NavigationTarget.Thermal);
            Assert.That(thermal.StepStatus, Is.Not.EqualTo(StepStatus.Ready),
                "needs-recalculation — шаг теплового не «готов» (черновик или ошибка)");
        }
    }

    /// <summary>
    /// SummaryViewModel — read-only проекция панели «Сводка»: зеркалирует
    /// агрегаты ResultsViewModel и снапшоты сессии, ничего не пишет.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SummaryViewModelTests
    {
        private ServiceProvider? _provider;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application();
            }
        }

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            _provider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            (_provider as IDisposable)?.Dispose();
            _provider = null;
        }

        [Test]
        public void TotalThermalPower_MirrorsResultsViewModel()
        {
            var results = _provider!.GetRequiredService<ResultsVm>();
            var summary = _provider!.GetRequiredService<SummaryViewModel>();

            results.TotalThermalPower_kW = 12.4;

            Assert.That(summary.TotalThermalPowerKw, Is.EqualTo(12.4).Within(1e-9),
                "мощность сводки зеркалит агрегат Results");
        }

        [Test]
        public void ModeText_MirrorsResultsViewModel()
        {
            var results = _provider!.GetRequiredService<ResultsVm>();
            var summary = _provider!.GetRequiredService<SummaryViewModel>();

            Assert.That(summary.ModeText, Is.EqualTo(results.CurrentModeText));
        }

        [Test]
        public void ThermalProjections_AreZero_WhenNoResultYet()
        {
            var summary = _provider!.GetRequiredService<SummaryViewModel>();

            Assert.Multiple(() =>
            {
                Assert.That(summary.PowerUp, Is.EqualTo(0));
                Assert.That(summary.PowerDown, Is.EqualTo(0));
                Assert.That(summary.PowerTotal, Is.EqualTo(0));
                Assert.That(summary.SupplyTemperature, Is.EqualTo(0));
                Assert.That(summary.ReturnTemperature, Is.EqualTo(0));
                Assert.That(summary.City, Is.Empty);
            });
        }
    }
}
