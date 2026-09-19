using System.IO;

namespace SnowMeltingCalculator.Services.Logging
{
    /// <summary>
    /// Файловый журнал с ротацией без внешних пакетов (волна 4
    /// hardening-роадмапа, решение владельца D2).
    ///
    /// <para>Политика: %LOCALAPPDATA%\SnowMeltingCalculator\logs\app.log
    /// (не %APPDATA% — логи не синхронизируются по сети), максимум
    /// 3 файла по 5 МБ (суммарно ≤ 15 МБ на диске), flush при каждой записи
    /// (объёмы малы), потокобезопасность — один lock. Телеметрии нет by
    /// construction: файл только пишется на диск.</para>
    ///
    /// <para>Корень логов инъекционный (тесты не пишут в реальный
    /// %LOCALAPPDATA%): конструктор принимает каталог явно, дефолт —
    /// <see cref="CreateDefault"/>. Сбой самого журнала (недоступный диск,
    /// блокировка файла) проглатывается внутри — лог не может быть причиной
    /// сбоя приложения (обоснованное исключение из правила
    /// «глушить можно только с записью»).</para>
    /// </summary>
    public sealed class RollingFileLogger : IAppLog
    {
        private readonly string _rootDir;
        private readonly string _baseName;
        private readonly long _maxFileBytes;
        private readonly int _maxFiles;
        // static: две композиции логгера в одном процессе пишут строго
        // последовательно (кросс-процессная блокировка — забота WAL-аналога,
        // для файла лога не решается; строки при гонке процессов глотаются).
        private static readonly object Lock = new();

        public RollingFileLogger(string rootDir, string baseName = "app",
            long maxFileBytes = 5 * 1024 * 1024, int maxFiles = 3)
        {
            _rootDir = rootDir;
            _baseName = baseName;
            _maxFileBytes = maxFileBytes;
            _maxFiles = maxFiles;
        }

        /// <summary>Дефолтная конфигурация: %LOCALAPPDATA%\SnowMeltingCalculator\logs;
        /// каталог можно переопределить переменной SNOWCALC_LOG_DIR (шов для тестов).</summary>
        public static RollingFileLogger CreateDefault()
        {
            var root = Environment.GetEnvironmentVariable("SNOWCALC_LOG_DIR");
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SnowMeltingCalculator",
                    "logs");
            }

            return new RollingFileLogger(root);
        }

        public void Info(string message) => Write(AppLogLevel.Info, message, null);

        public void Warn(string message) => Write(AppLogLevel.Warn, message, null);

        public void Error(string message, Exception? exception = null) =>
            Write(AppLogLevel.Error, message, exception);

        private void Write(AppLogLevel level, string message, Exception? exception)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz} " +
                           $"{level.ToString().ToUpperInvariant(),5}  {message}";
                if (exception is not null)
                {
                    line += $" | {exception.GetType().Name}: {exception.Message}";
                }

                lock (Lock)
                {
                    Directory.CreateDirectory(_rootDir);
                    var currentPath = Path.Combine(_rootDir, _baseName + ".log");
                    if (File.Exists(currentPath) &&
                        new FileInfo(currentPath).Length >= _maxFileBytes)
                    {
                        Rotate();
                    }

                    File.AppendAllText(currentPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Журнал не может быть причиной сбоя приложения: любой сбой
                // записи (диск, блокировка, права) молча игнорируется.
                // Единственное осознанное «молчаливое» место в кодовой базе —
                // исключено из скан-теста молчаливых catch.
            }
        }

        /// <summary>app.log → app.1.log → app.2.log → (старейший удаляется).</summary>
        private void Rotate()
        {
            var oldest = Path.Combine(_rootDir, $"{_baseName}.{_maxFiles - 1}.log");
            if (File.Exists(oldest))
            {
                File.Delete(oldest);
            }

            for (int i = _maxFiles - 2; i >= 1; i--)
            {
                var from = Path.Combine(_rootDir, $"{_baseName}.{i}.log");
                var to = Path.Combine(_rootDir, $"{_baseName}.{i + 1}.log");
                if (File.Exists(from))
                {
                    File.Move(from, to, overwrite: true);
                }
            }

            var current = Path.Combine(_rootDir, _baseName + ".log");
            if (File.Exists(current))
            {
                File.Move(current, Path.Combine(_rootDir, _baseName + ".1.log"), overwrite: true);
            }
        }
    }
}
