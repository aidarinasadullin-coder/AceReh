using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Architecture
{
    /// <summary>
    /// Пин версионирования (урок №20 «каждый новый инсталл — новая версия»,
    /// план docs/plans/2026-09-12-process-gates-plan.md, решение 7): канон —
    /// одна строка из <Version> в src/SnowMeltingCalculator.csproj, она
    /// сверяется по девяти местам — csproj (канон, место 1), .iss ×3
    /// (шапочный комментарий литералом «v<канон>»; #define MyAppVersion и
    /// OutputBaseFilename — макро-места, литерала версии не содержат и
    /// проверяются структурно), INSTALL.md ×3 (имя сетапа — ровно 2
    /// вхождения, подвал «Версия: …»), README.md подвал, CHANGELOG.md секция.
    /// Расхождение любого места — падение с диагностикой всех проблемных
    /// мест и канона.
    /// </summary>
    [TestFixture]
    public class VersionSyncTests
    {
        private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

        private static string RepoRoot => RepoRootLazy.Value;

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SnowMeltingCalculator.sln")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null,
                "VersionSync: repository root (SnowMeltingCalculator.sln) not found above "
                + AppContext.BaseDirectory);
            return dir!.FullName;
        }

        [Test]
        public void Version_IsConsistent_AcrossNinePlaces()
        {
            var csproj = File.ReadAllText(Path.Combine(RepoRoot, "src", "SnowMeltingCalculator.csproj"));
            var csprojMatch = Regex.Match(csproj, @"<Version>([^<]+)</Version>");
            Assert.That(csprojMatch.Success, Is.True,
                "Место 1 (канон): тег <Version> не найден в src/SnowMeltingCalculator.csproj.");
            var version = csprojMatch.Groups[1].Value.Trim();

            var iss = File.ReadAllText(Path.Combine(RepoRoot, "installer", "SnowMeltingCalculator.iss"));
            var install = File.ReadAllText(Path.Combine(RepoRoot, "INSTALL.md"));
            var readme = File.ReadAllText(Path.Combine(RepoRoot, "README.md"));
            var changelog = File.ReadAllText(Path.Combine(RepoRoot, "CHANGELOG.md"));

            var problems = new List<string>();

            // Место 2 — шапочный комментарий .iss, литерал «v<канон>».
            if (!iss.Contains("v" + version, StringComparison.Ordinal))
                problems.Add(
                    $"Место 2: installer/SnowMeltingCalculator.iss — шапочный комментарий не содержит "
                    + $"литерал \"v{version}\".");

            // Место 3 — #define MyAppVersion (макро-место, проверка структурная).
            if (!Regex.IsMatch(iss, @"^#define MyAppVersion\b", RegexOptions.Multiline))
                problems.Add(
                    "Место 3: installer/SnowMeltingCalculator.iss — не найдено ни одного "
                    + "'#define MyAppVersion'.");

            // Место 4 — OutputBaseFilename (макро-место, шаблон с макросом).
            if (!Regex.IsMatch(
                    iss,
                    @"^OutputBaseFilename=SnowMeltingCalculator-v\{#MyAppVersion\}-Setup\s*$",
                    RegexOptions.Multiline))
                problems.Add(
                    "Место 4: installer/SnowMeltingCalculator.iss — OutputBaseFilename не соответствует "
                    + "шаблону 'OutputBaseFilename=SnowMeltingCalculator-v{#MyAppVersion}-Setup'.");

            // Места 5–6 — INSTALL.md, имя сетапа: ровно два вхождения.
            var setupName = $"SnowMeltingCalculator-v{version}-Setup.exe";
            var setupCount = Regex.Matches(install, Regex.Escape(setupName)).Count;
            if (setupCount != 2)
                problems.Add(
                    $"Места 5–6: INSTALL.md — имя сетапа '{setupName}' встречается {setupCount} раз, "
                    + "ожидается ровно 2 (шаг установки и блок ручной сборки).");

            // Место 7 — INSTALL.md, подвал «Версия: <канон>».
            if (!install.Contains("Версия: " + version, StringComparison.Ordinal))
                problems.Add($"Место 7: INSTALL.md — подвал не содержит «Версия: {version}».");

            // Место 8 — README.md, подвал «Версия: <канон>».
            if (!readme.Contains("Версия: " + version, StringComparison.Ordinal))
                problems.Add($"Место 8: README.md — подвал не содержит «Версия: {version}».");

            // Место 9 — CHANGELOG.md, секция «## [<канон>]».
            if (!Regex.IsMatch(changelog, @"^## \[" + Regex.Escape(version) + @"\]", RegexOptions.Multiline))
                problems.Add($"Место 9: CHANGELOG.md — нет секции «## [{version}]».");

            Assert.That(problems, Is.Empty,
                $"Канон версии: «{version}» (src/SnowMeltingCalculator.csproj, место 1). "
                + "Рассинхрон мест версионирования:\n  " + string.Join("\n  ", problems));
        }
    }
}
