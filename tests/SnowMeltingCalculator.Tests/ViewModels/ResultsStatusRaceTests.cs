using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Time;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Гонка StatusMessage (волна 5): DelayStatusWindowAsync с ручным
    /// планировщиком — старая задержка очистки, проснувшись после появления
    /// более свежего статуса, возвращает «отменено» (false), и владелец
    /// свежего окна НЕ затирает статус (прежний код с Task.Delay(3000)
    /// всегда доходил до StatusMessage = string.Empty и гасил свежий
    /// статус). Механика вызывается напрямую — рефлексия приватного метода
    /// (публичный сценарий невозможен: AsyncRelayCommand не пускает вторую
    /// команду поверх работающей). Очистку статуса выполняет сайт-владелец
    /// окна — как в проде.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ResultsStatusRaceTests
    {
        /// <summary>Планировщик с ручным выпуском задержек по очереди.</summary>
        private sealed class ManualDelayScheduler : IDelayScheduler
        {
            private readonly ConcurrentQueue<TaskCompletionSource<bool>> _gates = new();

            public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
            {
                var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _gates.Enqueue(gate);
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() => gate.TrySetCanceled(cancellationToken));
                }
                return gate.Task;
            }

            /// <summary>Завершить старейшую незавершённую задержку.</summary>
            public void ReleaseOldest()
            {
                while (_gates.TryDequeue(out var gate))
                {
                    if (gate.TrySetResult(true)) return;
                }
            }
        }

        private static Task<bool> InvokeDelayStatusWindowAsync(
            ResultsViewModel viewModel, double seconds)
        {
            var method = typeof(ResultsViewModel).GetMethod(
                "DelayStatusWindowAsync",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Task<bool>)method.Invoke(viewModel, new object[] { seconds })!;
        }

        private static ResultsViewModel CreateViewModel(IDelayScheduler scheduler)
        {
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(
                new ProjectStateService(),
                ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                    ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2)),
                out _,
                out _,
                out _,
                delayScheduler: scheduler);
            SetSchedulerByReflection(viewModel, scheduler);
            return viewModel;
        }

        [Test]
        public async Task FreshStatus_SurvivesStaleDelayCompletion()
        {
            var scheduler = new ManualDelayScheduler();
            var viewModel = CreateViewModel(scheduler);

            // Окно статуса A: висит (gate не выпускается)
            viewModel.StatusMessage = "A";
            var holdA = InvokeDelayStatusWindowAsync(viewModel, 3.0);
            Assert.That(viewModel.StatusMessage, Is.EqualTo("A"));

            // Свежий статус B: Hold отменяет окно A (CancelStatusReset)
            viewModel.StatusMessage = "B";
            var holdB = InvokeDelayStatusWindowAsync(viewModel, 3.0);

            // Просыпается СТАРАЯ задержка (отменённое окно A): Hold вернул
            // «отменено», очистки нет — свежий статус «B» выжил
            scheduler.ReleaseOldest();
            await Task.Delay(50);
            Assert.That(viewModel.StatusMessage, Is.EqualTo("B"),
                "Старое окно очистки отменено свежим статусом — гонка StatusMessage устранена.");

            // Окно B истекает: завершено БЕЗ отмены (очистку выполняет
            // сайт-владелец — как в проде)
            scheduler.ReleaseOldest();
            Assert.That(await holdB, Is.True);

            // Окно A завершено отменой
            Assert.That(await holdA, Is.False);
        }

        [Test]
        public async Task ImmediateScheduler_WindowCompletesWithoutCancellation()
        {
            var viewModel = CreateViewModel(new ImmediateDelayScheduler());

            viewModel.StatusMessage = "Проект сохранён";
            var cleared = await InvokeDelayStatusWindowAsync(viewModel, 3.0);

            Assert.That(cleared, Is.True,
                "Immediate-окно завершается без отмены; очистку выполняет сайт-владелец.");
        }

        [Test]
        public void Scheduler_IsInjectable_ViaReflectionField()
        {
            var manual = new ManualDelayScheduler();
            var viewModel = CreateViewModel(new ImmediateDelayScheduler());
            SetSchedulerByReflection(viewModel, manual);

            var field = typeof(ResultsViewModel).GetField(
                "_delayScheduler",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

            Assert.That(ReferenceEquals(field.GetValue(viewModel), manual), Is.True,
                "инъекция планировщика в собранную VM работает (шов для тестов).");
        }

        private static void SetSchedulerByReflection(ResultsViewModel viewModel, IDelayScheduler scheduler)
        {
            typeof(ResultsViewModel).GetField(
                "_delayScheduler",
                BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(viewModel, scheduler);
        }
    }
}
