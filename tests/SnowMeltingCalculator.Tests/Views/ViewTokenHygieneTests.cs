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
    /// Что сканируется: src/Views/**/*.xaml, src/Controls/**/*.xaml
    /// (расширение Ф7.0) и src/MainWindow.xaml. Themes/*.xaml сознательно
    /// вне зоны (там живут примитивы и легаси-словари, приёмка Ф7 —
    /// «вьюхи + контролы»).
    /// Что нарушает правило: литеральные HEX-цвета (#RGB/#RRGGBB/#AARRGGBB)
    /// и «сырые» числовые FontSize вместо токенов шкалы (Tokens.Typography) —
    /// в обеих формах: атрибутной FontSize="18" и сеттерной
    /// Property="FontSize" Value="18" (расширение Ф7.0).
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

        /// <summary>Сеттерная форма литерального размера: &lt;Setter Property="FontSize" Value="18"/&gt;.</summary>
        private static readonly Regex SetterFontSizeRegex =
            new(@"Property\s*=\s*""(?:TextElement\.)?FontSize""\s+Value\s*=\s*""[0-9]+(?:[.,][0-9]+)?""",
                RegexOptions.Compiled);

        /// <summary>
        /// Разрешённое количество нарушений на файл на момент Фазы 0.
        /// Файлы, не указанные здесь, обязаны быть чистыми (0, 0).
        /// Фаза 6 (план Ф6): ResultsView переработана под компоненты Ф2 —
        /// литералы устранены полностью, запись удалена (было (0, 92)).
        /// Фаза 7 (план Ф7.0): сканер распространён на сеттерную форму
        /// FontSize и на src/Controls; CircuitsResultsView/CircuitInputView
        /// токенизированы — записи уменьшены, новых нарушений нет.
        /// </summary>
        private static readonly Dictionary<string, (int Hex, int FontSize)> Allowlist = new()
        {
            // Обновлено в Фазе 1: каркас окна (MainWindow) и удалённые
            // валидационные карточки вьюх убрали большинство литералов.
            ["MainWindow.xaml"] = (0, 0),
            // Фаза 5 редизайна: вьюхи переработаны под компоненты Ф2 —
            // литералы устранены полностью (было (1, 7) и (0, 41)).
            ["Views/Climate/ClimateView.xaml"] = (0, 0),
            // Фаза 4 редизайна: пир конструкции переработан, литералов 7 (было 12;
            // отсчёт 8 был завышен на один литерал, точный счёт сканера — 7).
            ["Views/Construction/ConstructionView.xaml"] = (0, 7),
            ["Views/Construction/MaterialEditorView.xaml"] = (3, 10),
            ["Views/Construction/TemplateEditorView.xaml"] = (3, 6),
            ["Views/Hydraulics/CircuitInputView.xaml"] = (0, 0),
            // Фаза 7: сеттерные литералы токенизированы (было (0, 1) —
            // атрибутная форма); файл чист.
            ["Views/Hydraulics/CircuitsResultsView.xaml"] = (0, 0),
            // Фаза 3 редизайна: вьюха переработана под компоненты Ф2 —
            // литералы устранены полностью (было (5, 68)).
            ["Views/Hydraulics/CircuitsView.xaml"] = (0, 0),
            // Фаза 6 редизайна: ResultsView чистая (было (0, 92)) — файла
            // нет в allowlist, правило (0, 0).
            ["Views/Shared/ConstructionVisualizationView.xaml"] = (0, 0),
            ["Views/Thermal/ThermalView.xaml"] = (0, 0),
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
                                    <Setter Property="FontSize" Value="24"/>
                                    <Setter Property="TextElement.FontSize" Value="9"/>
                                    <TextBlock Text="чистая строка" FontSize="{StaticResource Font.Size.Body}"/>
                                </UserControl>
                                """;

            var result = Scan(xaml);

            Assert.Multiple(() =>
            {
                Assert.That(result.Hex, Is.EqualTo(1), "сканер должен ловить подставленный HEX");
                Assert.That(result.FontSize, Is.EqualTo(3),
                    "сканер должен ловить сырой FontSize в атрибутной и обеих сеттерных формах, но не токен-значение");
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
                fontSizes += RawFontSizeRegex.Matches(line).Count
                    + SetterFontSizeRegex.Matches(line).Count;
            }

            return (hex, fontSizes);
        }

        private static IEnumerable<string> ViewXamlFiles()
        {
            var srcRoot = FindSrcRoot();
            var viewsDir = Path.Combine(srcRoot, "Views");
            var controlsDir = Path.Combine(srcRoot, "Controls");
            var files = Directory.EnumerateFiles(viewsDir, "*.xaml", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(controlsDir, "*.xaml", SearchOption.AllDirectories))
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
