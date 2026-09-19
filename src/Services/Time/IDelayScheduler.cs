using System.Threading;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Services.Time
{
    /// <summary>
    /// Абстракция ожидания для ViewModel (волна 5, план §7.1): позволяет
    /// тестам подставлять мгновенный планировщик вместо реальных задержек
    /// UI-статусов (−36 с прогона, пин «12 трёхсекундных тестов»).
    /// </summary>
    public interface IDelayScheduler
    {
        /// <summary>Подождать заданный интервал; отмена — выброс
        /// <see cref="OperationCanceledException"/>.</summary>
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }

    /// <summary>Продакшн-реализация: реальная задержка Task.Delay.</summary>
    public sealed class TaskDelayScheduler : IDelayScheduler
    {
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken) =>
            Task.Delay(delay, cancellationToken);
    }
}
