using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Services.Logging;

namespace SnowMeltingCalculator.Tests.Services.Logging
{
    /// <summary>
    /// Тесты файлового журнала (волна 4, D2): ротация ≤3×5 МБ, форматы
    /// строк, фолбэк-поведение при недоступном носителе. Корень логов —
    /// временный (инъекция, без записи в реальный %LOCALAPPDATA%).
    /// </summary>
    [TestFixture]
    public class RollingFileLoggerTests
    {
        private string _rootDir = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _rootDir = Path.Combine(Path.GetTempPath(), "snowcalc-log-tests", Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_rootDir))
            {
                Directory.Delete(_rootDir, recursive: true);
            }
        }

        [Test]
        public void Write_CreatesFileAndDirectory_WithLevelAndTimestamp()
        {
            var logger = new RollingFileLogger(_rootDir);

            logger.Info("старт приложения");
            logger.Warn("предупреждение");
            logger.Error("ошибка", new InvalidOperationException("причина"));

            var path = Path.Combine(_rootDir, "app.log");
            Assert.That(File.Exists(path), Is.True);

            var content = File.ReadAllText(path);
            Assert.Multiple(() =>
            {
                Assert.That(content, Does.Contain("INFO"), "уровень Info в строке.");
                Assert.That(content, Does.Contain("WARN"));
                Assert.That(content, Does.Contain("ERROR"));
                Assert.That(content, Does.Contain("старт приложения"));
                Assert.That(content, Does.Contain("InvalidOperationException: причина"),
                    "исключение попадает в строку журнала.");
                Assert.That(content, Does.Match(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"),
                    "локальная метка времени.");
            });
        }

        [Test]
        public void Write_RotatesWhenSizeLimitExceeded_AndDropsOldest()
        {
            // maxFileBytes=200, maxFiles=3: несколько записей → ротация app.log
            // → app.1.log → app.2.log, старейший вытесняется.
            var logger = new RollingFileLogger(_rootDir, maxFileBytes: 200, maxFiles: 3);
            for (var i = 0; i < 30; i++)
            {
                logger.Info($"запись {i:00} — " + new string('x', 60));
            }

            var files = Directory.GetFiles(_rootDir, "app*.log");
            Assert.That(files.Length, Is.LessThanOrEqualTo(3),
                "ротация держит не больше maxFiles файлов.");
            Assert.That(new FileInfo(Path.Combine(_rootDir, "app.log")).Length,
                Is.LessThanOrEqualTo(200 + 200),
                "текущий файл не превышает лимит (плюс последняя запись).");
        }

        [Test]
        public async Task Write_FromMultipleThreads_ProducesAllLines()
        {
            var logger = new RollingFileLogger(_rootDir);
            var tasks = new List<Task>();
            for (var t = 0; t < 8; t++)
            {
                var worker = t;
                tasks.Add(Task.Run(() =>
                {
                    for (var i = 0; i < 25; i++)
                    {
                        logger.Info($"поток {worker} запись {i}");
                    }
                }));
            }
            await Task.WhenAll(tasks);

            var lines = File.ReadAllLines(Path.Combine(_rootDir, "app.log"));
            Assert.That(lines.Length, Is.EqualTo(200),
                "все записи из всех потоков дошли до файла (потокобезопасность).");
        }

        [Test]
        public void Write_ToUnavailableRoot_DoesNotThrow()
        {
            // Лог не может быть причиной сбоя приложения: недоступный корень
            // (несуществующий диск) проглатывается внутри логгера.
            var logger = new RollingFileLogger(@"Q:\nonexistent\drive\logs");

            Assert.DoesNotThrow(() => logger.Warn("не должно упасть"));
        }
    }

    /// <summary>
    /// Статический фасад AppLog: до регистрации молчит, после — делегирует.
    /// Тесты регистрируют свой фейк и снимают регистрацию, чтобы не влиять
    /// на другие тесты (фасад — процессное состояние).
    /// </summary>
    [TestFixture]
    public class AppLogFacadeTests
    {
        private sealed class RecordingLogger : IAppLog
        {
            public List<string> Lines { get; } = new();

            public void Info(string message) => Lines.Add("INFO " + message);

            public void Warn(string message) => Lines.Add("WARN " + message);

            public void Error(string message, Exception? exception = null) =>
                Lines.Add("ERROR " + message + (exception is null ? "" : " | " + exception.Message));
        }

        [Test]
        public void Facade_BeforeRegistration_DoesNotThrow()
        {
            AppLog.Register(null!);
            try
            {
                Assert.DoesNotThrow(() =>
                {
                    AppLog.Info("x");
                    AppLog.Warn("x");
                    AppLog.Error("x", new InvalidOperationException());
                });
            }
            finally
            {
                AppLog.Register(null!);
            }
        }

        [Test]
        public void Facade_AfterRegistration_DelegatesAllLevels()
        {
            var recorder = new RecordingLogger();
            AppLog.Register(recorder);
            try
            {
                AppLog.Info("i");
                AppLog.Warn("w");
                AppLog.Error("e", new InvalidOperationException("причина"));

                Assert.Multiple(() =>
                {
                    Assert.That(recorder.Lines, Has.Count.EqualTo(3));
                    Assert.That(recorder.Lines[0], Does.StartWith("INFO i"));
                    Assert.That(recorder.Lines[1], Does.StartWith("WARN w"));
                    Assert.That(recorder.Lines[2], Does.StartWith("ERROR e").And.Contains("причина"));
                });
            }
            finally
            {
                AppLog.Register(null!);
            }
        }
    }
}
