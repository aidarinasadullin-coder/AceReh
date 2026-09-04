using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Views
{
    /// <summary>
    /// Верификация name-table шрифтов Inter (Фаза 0 редизайна, план п. 1).
    /// Причина: до Фазы 0 в Assets/Fonts не было Regular и SemiBold, и запрос
    /// Regular(400) матчился на ближайший вес — Medium(500): весь «обычный»
    /// текст рендерился Medium. Добавленные TTF попадают в семью «Inter»
    /// только если name-table согласована с существующими файлами:
    /// typographic family (ID16) или family (ID1) = «Inter», а вес задаёт
    /// OS/2 usWeightClass. Если новый TTF назовёт семью иначе, WPF заведёт
    /// отдельную семью и Regular снова промахнётся — этот тест это ловит.
    /// </summary>
    [TestFixture]
    public class FontNameTableTests
    {
        private static readonly IReadOnlyDictionary<string, ushort> ExpectedWeights =
            new Dictionary<string, ushort>
            {
                ["Inter-Light.ttf"] = 300,
                ["Inter-Regular.ttf"] = 400,
                ["Inter-Medium.ttf"] = 500,
                ["Inter-SemiBold.ttf"] = 600,
                ["Inter-Bold.ttf"] = 700,
                ["Inter-ExtraBold.ttf"] = 800,
                ["Inter-Black.ttf"] = 900,
            };

        [Test]
        public void AllInterWeights_ArePresent()
        {
            var missing = ExpectedWeights.Keys
                .Where(name => !File.Exists(Path.Combine(FontsDirectory(), name)))
                .ToList();

            Assert.That(missing, Is.Empty,
                "Отсутствуют TTF набора Inter (glob csproj подхватывает файлы автоматически): "
                + string.Join(", ", missing));
        }

        [Test]
        public void EveryFont_JoinsInterFamily_WithExpectedWeight()
        {
            var problems = new List<string>();
            string? commonVersion = null;

            foreach (var (fileName, expectedWeight) in ExpectedWeights)
            {
                var path = Path.Combine(FontsDirectory(), fileName);
                if (!File.Exists(path))
                {
                    continue; // покрывается AllInterWeights_ArePresent
                }

                var table = TtfNameTable.Read(path);

                var family = table.TypographicFamily ?? table.Family;
                if (family != "Inter")
                {
                    problems.Add($"{fileName}: семья «{family}» ≠ «Inter» "
                                 + "(ID16=" + (table.TypographicFamily ?? "—")
                                 + ", ID1=" + table.Family + ")");
                }

                if (table.WeightClass != expectedWeight)
                {
                    problems.Add($"{fileName}: usWeightClass {table.WeightClass} ≠ {expectedWeight}");
                }

                if (commonVersion is null)
                {
                    commonVersion = table.Version;
                }
                else if (table.Version != commonVersion)
                {
                    problems.Add($"{fileName}: версия «{table.Version}» не совпадает с остальным набором «{commonVersion}»");
                }
            }

            Assert.That(problems, Is.Empty,
                "Name-table набора Inter рассогласована:\n  " + string.Join("\n  ", problems));
        }

        [Test]
        public void InterFamily_ResolvesRegularAndSemiBoldWeights()
        {
            // Семья загружается с диска: csproj-глоб «Assets\Fonts\*.ttf»
            // включает в ресурс приложения ровно эти файлы, а проверка через
            // pack URI в тестах не работает (нет WPF-окружения приложения).
            var family = new System.Windows.Media.FontFamily(FontsDirectory() + "/#Inter");

            var weights = family.GetTypefaces().Select(face => face.Weight.ToOpenTypeWeight()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(weights, Does.Contain(400),
                    "семья «Inter» должна содержать Regular(400) — иначе он матчится на Medium(500)");
                Assert.That(weights, Does.Contain(600),
                    "семья «Inter» должна содержать SemiBold(600) — вес подзаголовков брендбука");
            });
        }

        private static string FontsDirectory()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir is not null
                   && !File.Exists(Path.Combine(dir.FullName, "src", "Assets", "Fonts", "Inter-Regular.ttf")))
            {
                dir = dir.Parent;
            }

            Assert.That(dir, Is.Not.Null,
                "Font scan: src/Assets/Fonts not found above " + TestContext.CurrentContext.TestDirectory);
            return Path.Combine(dir!.FullName, "src", "Assets", "Fonts");
        }

        /// <summary>Минимальный читатель name-table (ID 1/2/5/16/17) и OS/2 usWeightClass.</summary>
        private sealed class TtfNameTable
        {
            public string Family { get; init; } = string.Empty;
            public string? TypographicFamily { get; init; }
            public string Version { get; init; } = string.Empty;
            public ushort WeightClass { get; init; }

            public static TtfNameTable Read(string path)
            {
                var b = File.ReadAllBytes(path);
                var numTables = Be16(b, 4);
                var nameOffset = 0;
                var os2Offset = 0;
                for (var i = 0; i < numTables; i++)
                {
                    var record = 12 + i * 16;
                    var tag = Encoding.ASCII.GetString(b, record, 4);
                    var offset = Be32(b, record + 8);
                    if (tag == "name")
                    {
                        nameOffset = offset;
                    }
                    else if (tag == "OS/2")
                    {
                        os2Offset = offset;
                    }
                }

                Assert.That(nameOffset, Is.Not.Zero, $"{Path.GetFileName(path)}: нет таблицы name");
                Assert.That(os2Offset, Is.Not.Zero, $"{Path.GetFileName(path)}: нет таблицы OS/2");

                var count = Be16(b, nameOffset + 2);
                var storageOffset = nameOffset + Be16(b, nameOffset + 4);
                string? family = null, typographic = null, version = null;
                for (var i = 0; i < count; i++)
                {
                    var record = nameOffset + 6 + i * 12;
                    var platform = Be16(b, record);
                    var encoding = Be16(b, record + 2);
                    var language = Be16(b, record + 4);
                    var nameId = Be16(b, record + 6);
                    var length = Be16(b, record + 8);
                    var offset = Be16(b, record + 10);
                    if (platform != 3 || language != 0x409 || nameId is not (1 or 2 or 5 or 16 or 17))
                    {
                        continue;
                    }

                    var raw = b[(storageOffset + offset)..(storageOffset + offset + length)];
                    var value = encoding == 1 || encoding == 0
                        ? Encoding.BigEndianUnicode.GetString(raw)
                        : Encoding.ASCII.GetString(raw);

                    switch (nameId)
                    {
                        case 1: family ??= value; break;
                        case 5: version ??= value; break;
                        case 16: typographic ??= value; break;
                    }
                }

                return new TtfNameTable
                {
                    Family = family ?? string.Empty,
                    TypographicFamily = typographic,
                    Version = version ?? string.Empty,
                    WeightClass = Be16(b, os2Offset + 4),
                };
            }

            private static ushort Be16(byte[] b, int o) => (ushort)((b[o] << 8) | b[o + 1]);

            private static int Be32(byte[] b, int o) =>
                (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
        }
    }
}
