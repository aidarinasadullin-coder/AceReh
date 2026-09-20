// ================================================================================
// Тесты проверки обновлений (план 1.3) — фейк-канал, сети в тестах нет
// ================================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SnowMeltingCalculator.Services.Updates;

namespace SnowMeltingCalculator.Tests.Services.Updates
{
    /// <summary>Фейк-канал: отдаёт заготовленный результат без сети.</summary>
    internal sealed class FakeUpdateChannel : IUpdateChannel
    {
        public ChannelResult Result { get; set; } = new(false, null, "не вызван");
        public int Calls { get; private set; }

        public Task<ChannelResult> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    [TestFixture]
    public class UpdateCheckServiceTests
    {
        private static string CurrentVersionString()
        {
            var current = Assembly.GetEntryAssembly()!.GetName().Version!;
            return $"{current.Major}.{current.Minor}.{current.Build}";
        }

        private static UpdateManifest Manifest(string version)
        {
            return new UpdateManifest(version, "2026-09-21", new List<string>(), null);
        }

        [Test]
        public void NewerManifest_UpdateAvailable()
        {
            var channel = new FakeUpdateChannel { Result = new ChannelResult(true, Manifest("99.0.0"), null) };
            var service = new UpdateCheckService(channel);

            var outcome = service.CheckAsync().Result;

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Kind, Is.EqualTo(UpdateCheckKind.UpdateAvailable));
                Assert.That(outcome.Manifest!.Version, Is.EqualTo("99.0.0"));
            });
        }

        [Test]
        public void EqualManifest_UpToDate()
        {
            var channel = new FakeUpdateChannel { Result = new ChannelResult(true, Manifest(CurrentVersionString()), null) };
            var service = new UpdateCheckService(channel);

            var outcome = service.CheckAsync().Result;

            Assert.That(outcome.Kind, Is.EqualTo(UpdateCheckKind.UpToDate));
        }

        [Test]
        public void OlderManifest_UpToDate_NoDowngrade()
        {
            var channel = new FakeUpdateChannel { Result = new ChannelResult(true, Manifest("0.0.1"), null) };
            var service = new UpdateCheckService(channel);

            var outcome = service.CheckAsync().Result;

            Assert.That(outcome.Kind, Is.EqualTo(UpdateCheckKind.UpToDate), "даунгрейд не предлагаем (U5)");
        }

        [Test]
        public void BadVersionInManifest_Unavailable()
        {
            var channel = new FakeUpdateChannel { Result = new ChannelResult(true, Manifest("x.y.z"), null) };
            var service = new UpdateCheckService(channel);

            var outcome = service.CheckAsync().Result;

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Kind, Is.EqualTo(UpdateCheckKind.Unavailable));
                Assert.That(outcome.Reason, Does.Contain("версия"));
            });
        }

        [Test]
        public void ChannelFailure_Unavailable()
        {
            var channel = new FakeUpdateChannel { Result = new ChannelResult(false, null, "Сеть недоступна") };
            var service = new UpdateCheckService(channel);

            var outcome = service.CheckAsync().Result;

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Kind, Is.EqualTo(UpdateCheckKind.Unavailable));
                Assert.That(outcome.Reason, Is.EqualTo("Сеть недоступна"));
            });
        }

        [Test]
        public void ChannelNotConfigured_Unavailable()
        {
            // Деградация «канал не настроен» — это контракт DriveUpdateChannel
            // (пустая константа → failure БЕЗ сетевого вызова): проверяем сам
            // класс, HttpClient не дергается (ранний return до сети).
            var channel = new DriveUpdateChannel();
            var service = new UpdateCheckService(channel);

            var outcome = service.CheckAsync().Result;

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Kind, Is.EqualTo(UpdateCheckKind.Unavailable));
                Assert.That(outcome.Reason, Does.Contain("не настроен"));
            });
        }
    }
}
