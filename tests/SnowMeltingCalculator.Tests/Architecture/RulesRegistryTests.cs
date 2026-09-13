using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Architecture
{
    /// <summary>
    /// Верификатор реестра «правило → проверка» (docs/agents/rules-registry.md,
    /// план docs/plans/2026-09-12-process-gates-plan.md, решение 6):
    /// A — каждый test-токен колонки «Как проверить» существует в тестовой
    ///     сборке (reflection по именам типов и методов);
    /// B — каждая ADR-запись docs/architecture/README.md упомянута в реестре;
    /// C — каждый урок lessons.md, у которого заголовок содержит
    ///     «правило владельца» или маркер «Урок №N» (а также каждый номер
    ///     №N, встречающийся в заголовках), упомянут в реестре.
    /// Формат таблицы проверяется простыми регекспами, без markdown-парсера.
    /// Обнаружение корня репо — по образцу скан-тестов R2/R3/R5.
    /// </summary>
    [TestFixture]
    public class RulesRegistryTests
    {
        private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

        private static string RepoRoot => RepoRootLazy.Value;

        private static string RegistryPath => Path.Combine(RepoRoot, "docs", "agents", "rules-registry.md");
        private static string LessonsPath => Path.Combine(RepoRoot, "docs", "agents", "lessons.md");
        private static string AdrLogPath => Path.Combine(RepoRoot, "docs", "architecture", "README.md");

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SnowMeltingCalculator.sln")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null,
                "RulesRegistry: repository root (SnowMeltingCalculator.sln) not found above "
                + AppContext.BaseDirectory);
            return dir!.FullName;
        }

        private sealed record RegistryRow(string Id, string Rule, string Source, string CheckKind, string CheckHow, int Line);

        private static List<RegistryRow> ParseRegistry()
        {
            var lines = File.ReadAllLines(RegistryPath);
            var rows = new List<RegistryRow>();
            var headerSeen = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!line.StartsWith("|", StringComparison.Ordinal))
                    continue;
                if (line.Contains("---"))
                {
                    headerSeen = true;
                    continue;
                }
                if (!headerSeen)
                    continue; // колонка-заголовок таблицы до разделителя
                var cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                if (cells.Length != 5)
                    Assert.Fail(
                        $"rules-registry.md:{i + 1}: строка таблицы должна иметь 5 колонок "
                        + $"(ID/Правило/Первоисточник/Проверка/Как проверить), найдено {cells.Length}: {line}");
                if (cells[0].Length == 0)
                    Assert.Fail($"rules-registry.md:{i + 1}: пустой ID правила.");
                if (cells[3] is not ("test" or "script" or "review-step"))
                    Assert.Fail(
                        $"rules-registry.md:{i + 1} ({cells[0]}): колонка «Проверка» = '{cells[3]}', "
                        + "ожидается test | script | review-step.");
                if (cells[3] == "test" && !Regex.IsMatch(cells[4], @"^[A-Za-z0-9_,\s]+$"))
                    Assert.Fail(
                        $"rules-registry.md:{i + 1} ({cells[0]}): при типе test колонка «Как проверить» "
                        + $"содержит только имена тестов через запятую, без прозы: '{cells[4]}'");
                rows.Add(new RegistryRow(cells[0], cells[1], cells[2], cells[3], cells[4], i + 1));
            }

            Assert.That(headerSeen, Is.True, "rules-registry.md: таблица реестра не найдена.");
            Assert.That(rows, Is.Not.Empty, "rules-registry.md: реестр пуст.");
            return rows;
        }

        // A — каждый заявленный тест существует в тестовой сборке.

        [Test]
        public void Registry_TestChecks_ResolveInTestAssembly()
        {
            var rows = ParseRegistry();
            var types = typeof(RulesRegistryTests).Assembly.GetTypes();
            var typeNames = new HashSet<string>(types.Select(t => t.Name));
            var methodNames = new HashSet<string>(
                types.SelectMany(t => t.GetMethods(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    .Select(m => m.Name));

            var missing = new List<string>();
            foreach (var row in rows.Where(r => r.CheckKind == "test"))
            {
                foreach (var raw in row.CheckHow.Split(','))
                {
                    var token = raw.Trim();
                    if (token.Length == 0)
                        continue;
                    if (!typeNames.Contains(token) && !methodNames.Contains(token))
                        missing.Add(
                            $"rules-registry.md:{row.Line} ({row.Id}): '{token}' не найден ни как "
                            + "тест-класс, ни как тест-метод тестовой сборки");
                }
            }

            Assert.That(missing, Is.Empty,
                "Реестр ссылается на несуществующие проверки:\n  " + string.Join("\n  ", missing));
        }

        // B — каждая ADR-запись журнала упомянута в реестре.

        [Test]
        public void Registry_Adrs_AllMentioned()
        {
            ParseRegistry();
            var registryText = File.ReadAllText(RegistryPath);
            var adrText = File.ReadAllText(AdrLogPath);
            var adrIds = Regex.Matches(adrText, @"^### (ADR-\d+)", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            Assert.That(adrIds, Is.Not.Empty, "docs/architecture/README.md: ADR-записи не найдены.");
            var uncovered = adrIds.Where(id => !registryText.Contains(id, StringComparison.Ordinal)).ToList();
            Assert.That(uncovered, Is.Empty,
                "ADR-записи без строки в реестре (правило: строка в том же коммите, что и ADR):\n  "
                + string.Join(", ", uncovered));
        }

        // C — правила владельца и пронумерованные уроки покрыты реестром.

        [Test]
        public void Registry_OwnerRulesAndLessons_AllMentioned()
        {
            ParseRegistry();
            var registryText = File.ReadAllText(RegistryPath);
            var lessonsLines = File.ReadAllLines(LessonsPath);

            var problems = new List<string>();
            for (var i = 0; i < lessonsLines.Length; i++)
            {
                var trimmed = lessonsLines[i].TrimStart();
                if (!trimmed.StartsWith("## ", StringComparison.Ordinal))
                    continue;
                var header = trimmed.Substring(3).Trim();
                var nums = Regex.Matches(header, @"№(\d+)")
                    .Select(m => m.Groups[1].Value)
                    .Distinct()
                    .ToList();
                var isOwnerRule = header.Contains("правило владельца", StringComparison.Ordinal);
                var isNumberedLesson = Regex.IsMatch(header, @"Урок №\d+");
                if (!isOwnerRule && !isNumberedLesson && nums.Count == 0)
                    continue;

                var covered =
                    nums.Any(n => Regex.IsMatch(registryText, @"№\s*" + n + @"\b"))
                    || (isOwnerRule
                        && OwnerRuleKeyPhrase(header) is { } phrase
                        && registryText.Contains(phrase, StringComparison.Ordinal));
                if (!covered)
                    problems.Add(
                        $"lessons.md:{i + 1}: заголовок «{header}» не покрыт реестром "
                        + $"(номера: {(nums.Count > 0 ? string.Join(", ", nums.Select(n => "№" + n)) : "нет")}).");
            }

            Assert.That(problems, Is.Empty,
                "Правила владельца / уроки без строки в реестре:\n  " + string.Join("\n  ", problems));
        }

        // «правило владельца: каждый новый инсталл — новая версия)» →
        // «каждый новый инсталл — новая версия» — различительная фраза заголовка,
        // по которой реестр связывает строку с уроком без номера в заголовке.
        private static string? OwnerRuleKeyPhrase(string header)
        {
            var start = header.IndexOf("правило владельца:", StringComparison.Ordinal);
            if (start < 0)
                return null;
            var phrase = header.Substring(start + "правило владельца:".Length).Trim();
            var closing = phrase.LastIndexOf(')');
            if (closing >= 0)
                phrase = phrase.Substring(0, closing).Trim();
            return phrase.Length == 0 ? null : phrase;
        }
    }
}
