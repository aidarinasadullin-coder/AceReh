using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Architecture
{
    /// <summary>
    /// Скан-сканер молчаливых catch (волна 4 hardening-роадмапа): каждый
    /// catch-блок production-кода обязан либо писать в локальный журнал
    /// (<c>AppLog.</c>), либо пробрасывать исключение наверх (<c>throw</c>).
    /// Правило «глушить можно только с записью»: production-инцидент должен
    /// быть воспроизводим по логу (D2).
    ///
    /// <para>Единственное осознанное исключение —
    /// <c>Services/Logging/RollingFileLogger.cs</c>: сбой самого журнала
    /// не может быть залогирован им же и не должен ронять приложение.</para>
    ///
    /// <para>Новый молчаливый catch в любом файле сделает тест красным —
    /// добавьте <c>AppLog.Warn(ex, «контекст»)</c> или throw.</para>
    /// </summary>
    [TestFixture]
    public class SilentCatchScanTests
    {
        private static string SrcRoot => Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));

        [Test]
        public void EveryCatchBlock_LogsOrRethrows()
        {
            var violations = new List<string>();

            foreach (var file in Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/bin/") || normalized.Contains("/obj/")) continue;
                if (normalized.EndsWith("Services/Logging/RollingFileLogger.cs")) continue;

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    // комментарии (// и ///) не код: слово «catch» в тексте
                    // комментария не является catch-блоком
                    var commentIndex = lines[i].IndexOf("//", StringComparison.Ordinal);
                    var codePart = commentIndex >= 0 ? lines[i][..commentIndex] : lines[i];
                    var catchMatch = Regex.Match(codePart, @"\bcatch\b");
                    if (!catchMatch.Success) continue;

                    // OperationCanceledException — норма потока (отмена
                    // пользователем/таймером), не инцидент: лог не требуется
                    var catchTail = codePart[(catchMatch.Index + catchMatch.Length)..];
                    if (catchTail.Contains("OperationCanceledException")) continue;

                    var body = ExtractBody(lines, i);
                    var hasLog = body.Contains("AppLog.");
                    var rethrows = Regex.IsMatch(body, @"\bthrow\b");
                    if (!hasLog && !rethrows)
                    {
                        violations.Add($"{normalized}:{i + 1}");
                    }
                }
            }

            Assert.That(violations, Is.Empty,
                "Молчаливые catch-блоки запрещены (волна 4, D2): каждый catch обязан " +
                "писать в журнал AppLog.* или пробрасывать исключение наверх. " +
                "Нарушения:\n" + string.Join("\n", violations));
        }

        /// <summary>Собрать тело catch-блока до балансировки фигурных скобок
        /// (или однострочное выражение), до 200 строк.</summary>
        private static string ExtractBody(string[] lines, int catchIndex)
        {
            var collected = new List<string>();
            var depth = 0;
            var started = false;
            for (int j = catchIndex; j < Math.Min(catchIndex + 200, lines.Length); j++)
            {
                var line = lines[j];
                depth += line.Count(ch => ch == '{') - line.Count(ch => ch == '}');
                if (line.Contains('{')) started = true;
                collected.Add(line);
                if (started && depth <= 0) break;
                if (!started && line.TrimEnd().EndsWith(";") && j > catchIndex) break;
            }
            return string.Join("\n", collected);
        }
    }
}
