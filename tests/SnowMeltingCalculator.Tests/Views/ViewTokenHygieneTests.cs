using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Views
{
    /// <summary>
    /// Гигиена дизайн-токенов во вьюхах (Фаза 0 редизайна, план п. 7).
    /// Отдельный fixture — сознательно НЕ в ArchitectureRulesTests: это
    /// визуальные, не архитектурные правила.
    ///
    /// Что сканируется: src/Views/**/*.xaml и src/MainWindow.xaml.
    /// Что нарушает правило: литеральные HEX-цвета (#RGB/#RRGGBB/#AARRGGBB)
    /// и «сырые» числовые FontSize вместо токенов шкалы (Tokens.Typography).
    /// Что пропускается: XAML-комментарии и design-time атрибуты d:*.
    ///
    /// Решение по named-цветам (зафиксировано): именованные цвета WPF
    /// (White, Black, Transparent, Red, …) считаются токенами и ratchet не
    /// учитывает; пересмотр решения — Фаза 2 (компонентная библиотека).
    ///
    /// Ratchet-allowlist зафиксирован на состоянии Фазы 0. Рост счётчика
    /// запрещён; уменьшение поощряется — после него уменьшите allowlist.
    /// Подстановка любого нового хардкода ломает тест (приёмка Фазы 0).
    /// </summary>
    [TestFixture]
    public class ViewTokenHygieneTests
    {
        private static readonly Regex CommentRegex =
            new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex DesignTimeAttributeRegex =
            new(@"\bd:[A-Za-z]+\s*=", RegexOptions.Compiled);

        private static readonly Regex LiteralHexRegex =
            new(@"#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})(?![0-9A-Fa-f])",
                RegexOptions.Compiled);

        private static readonly Regex RawFontSizeRegex =
            new(@"(?:TextElement\.)?FontSize\s*=\s*""[0-9]+(?:[.,][0-9]+)?""",
                RegexOptions.Compiled);

        /// <summary>
        /// Разрешённое количество нарушений на файл на момент Фазы 0.
        /// Файлы, не указанные здесь, обязаны быть чистыми (0, 0).
        /// Фаза 2 (план Ф2.5): HEX ResultsView устранён (стиль режима → токен);
        /// FontSize 92/68 — литералы в разметке body, вне объёма Ф2.5 (ADR-006).
        /// </summary>
        private static readonly Dictionary<string, (int Hex, int FontSize)> Allowlist = new()
        {
            // Обновлено в Фазе 1: каркас окна (MainWindow) и удалённые
            // валидационные карточки вьюх убрали большинство литералов.
            ["MainWindow.xaml"] = (0, 0),
            ["Views/Climate/ClimateView.xaml"] = (1, 7),
            ["Views/Construction/ConstructionView.xaml"] = (0, 12),
            ["Views/Construction/MaterialEditorView.xaml"] = (3, 10),
            ["Views/Construction/TemplateEditorView.xaml"] = (3, 6),
            ["Views/Hydraulics/CircuitInputView.xaml"] = (0, 0),
            ["Views/Hydraulics/CircuitsResultsView.xaml"] = (0, 1),
            ["Views/Hydraulics/CircuitsView.xaml"] = (5, 68),
            ["Views/Results/ResultsView.xaml"] = (0, 92),
            ["Views/Shared/ConstructionVisualizationView.xaml"] = (0, 0),
            ["Views/Thermal/ThermalView.xaml"] = (0, 41),
        };

        [Test]
        public void AllViews_StayWithinRatchetAllowlist()
        {
            var overruns = new List<string>();
            var scanned = new List<string>();

            foreach (var path in ViewXamlFiles())
            {
                var key = RelativeKey(path);
                scanned.Add(key);
                var actual = Scan(File.ReadAllText(path));
                var (allowedHex, allowedFontSize) = Allowlist.TryGetValue(key, out var allowed)
                    ? allowed
                    : (0, 0);

                if (actual.Hex > allowedHex || actual.FontSize > allowedFontSize)
                {
                    overruns.Add(
                        $"{key}: HEX {actual.Hex}/{allowedHex}, FontSize {actual.FontSize}/{allowedFontSize}");
                }
                else if (actual.Hex < allowedHex || actual.FontSize < allowedFontSize)
                {
                    TestContext.Out.WriteLine(
                        $"{key} стал чище (HEX {actual.Hex}/{allowedHex}, FontSize {actual.FontSize}/{allowedFontSize}) — уменьшите allowlist.");
                }
            }

            var vanished = Allowlist.Keys.Except(scanned).ToList();
            Assert.That(vanished, Is.Empty,
                "Ratchet-allowlist ссылается на несуществующие файлы (уменьшите/удалите записи): "
                + string.Join(", ", vanished));

            Assert.That(overruns, Is.Empty,
                "Во вьюхах появились литеральные HEX или «сырые» FontSize — используйте токены "
                + "(Themes/Tokens.*.xaml) либо осознанно обновите ratchet через ревью:\n  "
                + string.Join("\n  ", overruns));
        }

        [Test]
        public void Scanner_FlagsInjectedHexAndRawFontSize()
        {
            const string xaml = """
                                <UserControl xmlns:d="http://schemas.microsoft.com/expression/blend/2008">
                                    <TextBlock Text="инъекция" Foreground="#FF0000" FontSize="18"/>
                                </UserControl>
                                """;

            var result = Scan(xaml);

            Assert.Multiple(() =>
            {
                Assert.That(result.Hex, Is.EqualTo(1), "сканер должен ловить подставленный HEX");
                Assert.That(result.FontSize, Is.EqualTo(1), "сканер должен ловить сырой FontSize");
            });
        }

        [Test]
        public void Scanner_SkipsCommentsAndDesignTimeAttributes()
        {
            const string xaml = """
                                <UserControl
                                    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                                    d:DesignWidth="1920"
                                    d:DataContext="{Binding Path=Mock}">
                                    <!-- <TextBlock Foreground="#00FF00" FontSize="24"/> -->
                                    <TextBlock Text="чистая строка" Foreground="{DynamicResource Color.Text.Primary}"/>
                                </UserControl>
                                """;

            var result = Scan(xaml);

            Assert.Multiple(() =>
            {
                Assert.That(result.Hex, Is.EqualTo(0), "комментарии не должны учитываться");
                Assert.That(result.FontSize, Is.EqualTo(0), "комментарии не должны учитываться");
            });
        }

        private static (int Hex, int FontSize) Scan(string xaml)
        {
            var withoutComments = CommentRegex.Replace(xaml, string.Empty);
            int hex = 0, fontSizes = 0;
            foreach (var line in withoutComments.Split('\n'))
            {
                if (DesignTimeAttributeRegex.IsMatch(line))
                {
                    continue;
                }

                hex += LiteralHexRegex.Matches(line).Count;
                fontSizes += RawFontSizeRegex.Matches(line).Count;
            }

            return (hex, fontSizes);
        }

        private static IEnumerable<string> ViewXamlFiles()
        {
            var srcRoot = FindSrcRoot();
            var viewsDir = Path.Combine(srcRoot, "Views");
            var files = Directory.EnumerateFiles(viewsDir, "*.xaml", SearchOption.AllDirectories)
                .Concat(new[] { Path.Combine(srcRoot, "MainWindow.xaml") });
            return files.OrderBy(p => p, StringComparer.Ordinal).ToList();
        }

        private static string RelativeKey(string path)
        {
            var srcRoot = FindSrcRoot();
            return Path.GetRelativePath(srcRoot, path).Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string FindSrcRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "MainWindow.xaml")))
            {
                dir = dir.Parent;
            }

            Assert.That(dir, Is.Not.Null,
                "View token scan: src/MainWindow.xaml not found above " + TestContext.CurrentContext.TestDirectory);
            return Path.Combine(dir!.FullName, "src");
        }
    }
}
