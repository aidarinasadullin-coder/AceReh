using System.ComponentModel;
using System.Diagnostics;
using NUnit.Framework;
using SnowMeltingCalculator.Services.Printing;

namespace SnowMeltingCalculator.Tests.Services.Printing
{
    /// <summary>
    /// Пины shell-конфигурации печати (волна 2 hardening-роадмапа, решения
    /// владельца D1/D8). Реальный акт печати без подключённого принтера
    /// юнит-тестом неверифицируем (принятое ограничение D1) — пинится
    /// <see cref="ProcessStartInfo"/> через шов фабрики процесса: регрессия
    /// «UseShellExecute по умолчанию» (P0 ревизии 2026-09-18) ловится здесь.
    /// </summary>
    [TestFixture]
    public class PrintServiceTests
    {
        private sealed class TrackingProcess : Process
        {
            public bool DisposeCalled { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCalled = true;
                base.Dispose(disposing);
            }
        }

        private static (PrintService Service, TrackingProcess Process, List<ProcessStartInfo> Started)
            CreateCapturingService()
        {
            var process = new TrackingProcess();
            var started = new List<ProcessStartInfo>();
            var service = new PrintService(info =>
            {
                started.Add(info);
                return process;
            });
            return (service, process, started);
        }

        [Test]
        public void PrintPdf_UsesShellExecutePrintVerb()
        {
            var (service, _, started) = CreateCapturingService();

            var result = service.PrintPdf(@"C:\temp\report.pdf");

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(started, Has.Count.EqualTo(1));
                var info = started.Single();
                Assert.That(info.FileName, Is.EqualTo(@"C:\temp\report.pdf"));
                Assert.That(info.Verb, Is.EqualTo("print"),
                    "Печать PDF идёт shell-verb «print», а не запуском файла как программы.");
                Assert.That(info.UseShellExecute, Is.True,
                    "На .NET 8 умолчание UseShellExecute=false: verb «print» без ShellExecute " +
                    "падает Win32Exception на каждом вызове (P0 ревизии 2026-09-18).");
                Assert.That(info.CreateNoWindow, Is.True);
            });
        }

        [Test]
        public void PrintPdf_DisposesStartedProcess()
        {
            var (service, process, _) = CreateCapturingService();

            service.PrintPdf(@"C:\temp\report.pdf");

            Assert.That(process.DisposeCalled, Is.True,
                "Процесс одноразовый: незакрытый handle утечки недопустим.");
        }

        [Test]
        public void PrintPdf_ProcessFactoryReturningNull_IsSuccess()
        {
            // UseShellExecute=true: Process.Start легально возвращает null
            // при успешном shell-старте без полученного handle — это успех.
            var service = new PrintService(_ => null);

            var result = service.PrintPdf(@"C:\temp\report.pdf");

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void PrintPdf_ShellFailure_ReturnsFailureWithoutThrowing()
        {
            var service = new PrintService(_ =>
                throw new Win32Exception(3, "Нет ассоциированного приложения для печати"));

            var result = service.PrintPdf(@"C:\temp\missing.pdf");

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("Нет ассоциированного приложения"));
            });
        }

        [Test]
        public void OpenPdf_UsesShellOpenVerb()
        {
            var (service, _, started) = CreateCapturingService();

            var result = service.OpenPdf(@"C:\temp\report.pdf");

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(started, Has.Count.EqualTo(1));
                Assert.That(started.Single().Verb, Is.EqualTo("open"),
                    "Фолбэк D8 открывает PDF системным просмотрщиком, а не печатает повторно.");
                Assert.That(started.Single().UseShellExecute, Is.True);
            });
        }
    }
}
