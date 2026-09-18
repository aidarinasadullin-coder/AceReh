using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Architecture
{
    /// <summary>
    /// Сторож актуальности инструкции пользователя (docs/manual/README.html,
    /// решение владельца 2026-09-18 — этап 2 плана пересборки инструкции):
    /// A — версия в шапке инструкции («соответствует версии программы X»)
    ///     равна &lt;Version&gt; из src/SnowMeltingCalculator.csproj;
    /// B — README.html собран из шаблона: в файле нет незаполненных
    ///     плейсхолдеров {{IMG:…}} / {{VERSION}}.
    /// Инструкция пересобирается командой python docs/manual/build_manual.py.
    /// Обнаружение корня репо — по образцу скан-тестов R2/R3/R5.
    /// </summary>
    [TestFixture]
    public class ManualVersionGateTests
    {
        private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

        private static string RepoRoot => RepoRootLazy.Value;

        private static string ManualPath => Path.Combine(RepoRoot, "docs", "manual", "README.html");
        private static string CsprojPath => Path.Combine(RepoRoot, "src", "SnowMeltingCalculator.csproj");

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SnowMeltingCalculator.sln")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null,
                "ManualVersionGate: repository root (SnowMeltingCalculator.sln) not found above "
                + AppContext.BaseDirectory);
            return dir!.FullName;
        }

        // A — версия инструкции совпадает с версией приложения.

        [Test]
        public void Manual_Version_MatchesCsproj()
        {
            var manual = File.ReadAllText(ManualPath);
            var m = Regex.Match(manual, @"соответствует версии программы ([0-9A-Za-z.\-]+)");
            Assert.That(m.Success, Is.True,
                "docs/manual/README.html: в шапке не найдена строка «соответствует версии программы X».");

            var csproj = File.ReadAllText(CsprojPath);
            var v = Regex.Match(csproj, @"<Version>([^<]+)</Version>");
            Assert.That(v.Success, Is.True, $"src/SnowMeltingCalculator.csproj: не найден <Version>.");

            Assert.That(m.Groups[1].Value.Trim(), Is.EqualTo(v.Groups[1].Value.Trim()),
                "Инструкция пользователя отстаёт от версии приложения. Пересоберите её: "
                + "python docs/manual/build_manual.py (docs/manual/src/template.html + src/media).");
        }

        // B — README.html собран, а не редактировался вручную с плейсхолдерами.

        [Test]
        public void Manual_HasNoLeftoverPlaceholders()
        {
            var manual = File.ReadAllText(ManualPath);
            var leftovers = Regex.Matches(manual, @"\{\{IMG:[^}]*\}\}|\{\{VERSION\}\}");
            Assert.That(leftovers, Is.Empty,
                "docs/manual/README.html содержит незаполненные плейсхолдеры шаблона — "
                + "пересоберите: python docs/manual/build_manual.py.");
        }
    }
}
