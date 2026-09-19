using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Services.Time;

namespace SnowMeltingCalculator.Tests.Fixtures
{
    /// <summary>
    /// Мгновенный планировщик для тестов (волна 5): задержки завершаются
    /// сразу, отмена честно бросает OperationCanceledException — поведенческие
    /// тесты VM идут без реальных 3-секундных ожиданий UI-статусов.
    /// </summary>
    public sealed class ImmediateDelayScheduler : IDelayScheduler
    {
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }
    }
}
