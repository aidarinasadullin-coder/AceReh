using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using CSharpMath.SkiaSharp;
using SkiaSharp;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// LaTeX-вёрстка формул детального отчёта (запрос владельца, 2026-09-07):
    /// «плоская» запись формулы из модели конвертируется в LaTeX и
    /// отрисовывается CSharpMath/SkiaSharp в PNG (трёхкратный масштаб к
    /// точечному размеру PDF, В10 — кириллица в индексах поддерживается).
    /// </summary>
    /// <remarks>
    /// Формулы с прозой (4+ кириллических слова вне subscript'ов — пояснения
    /// вида «Колбрук–Уайт (итерации…)») не конвертируются: рендер оставляет
    /// их текстом. Никаких вычислений — только нотация (AC-5).
    /// </remarks>
    public static class CalculationReportLaTeXFormulaRenderer
    {
        /// <summary>Размер шрифта рендера, px (в PDF размещается 1/3 pt/px —
        /// кегль формулы в документе прежний, растровый запас трёхкратный).</summary>
        private const float RenderFontSize = 30f;

        /// <summary>Пт на пиксель при размещении в документе (рендер 3×, В10).</summary>
        internal const double PointPerPixel = 1d / 3d;

        private static readonly ConcurrentDictionary<string, FormulaImage?> Cache = new();

        /// <summary>Отрендеренная формула: PNG-байты и размер в пикселях.</summary>
        public sealed record FormulaImage(byte[] Bytes, int WidthPx, int HeightPx);

        /// <summary>
        /// Сконвертировать и отрендерить формулу; null — формула остаётся
        /// текстом (проза или ошибка рендера).
        /// </summary>
        public static FormulaImage? TryRenderPng(string? plainFormula)
        {
            if (string.IsNullOrWhiteSpace(plainFormula))
            {
                return null;
            }

            return Cache.GetOrAdd(plainFormula, key =>
            {
                var latex = TryConvertToLaTeX(key);
                return latex == null ? null : TryRenderPngInner(latex);
            });
        }

        /// <summary>
        /// «Плоская» запись → LaTeX; null — формула не пригодна для вёрстки.
        /// </summary>
        public static string? TryConvertToLaTeX(string plain)
        {
            // Пояснительная проза (например, «…Колбрук–Уайт (итерации, старт
            // по Блазиусу); между — линейная интерполяция») остаётся текстом.
            if (Regex.Matches(plain, "(?<![_А-Яа-яЁёA-Za-z])([А-Яа-яЁё][а-яё]{2,})").Count >= 4)
            {
                return null;
            }

            var t = plain;
            // Дробные литеры (¼/½ — размеры коллекторов) → числовые дроби;
            // пробел перед литерой уходит внутрь \text (math-режим пробелы
            // съедает, сами литеры в math-шрифте отсутствуют).
            t = t.Replace(" 1¼", "\\text{ 1/4}")
                 .Replace(" 1½", "\\text{ 1/2}")
                 .Replace(" ¾", "\\text{ 3/4}")
                 .Replace("¼", "\\text{1/4}")
                 .Replace("½", "\\text{1/2}")
                 .Replace("¾", "\\text{3/4}");
            // Нижние индексы: _X → _{X} (защищаются от \text-прохода).
            t = Regex.Replace(t, "_([А-Яа-яЁёA-Za-z0-9]+)", "_{$1}");
            // Десятичный разделитель — запятая (В6): 3.6 → 3,6.
            t = Regex.Replace(t, "(\\d)\\.(\\d)", "$1,$2");
            // Степени: ^tok → ^{tok}.
            t = Regex.Replace(t, @"\^(\([^)]*\)|[^\s^_]+)", "^{$1}");
            // Надстрочные юникод-цифры → степени math-режима (⁵ и соседние
            // в math-фолбэке CSharpMath отсутствуют; после ^-обёртки, чтобы
            // не получить ^{{2}}).
            t = t.Replace("⁰", "^{0}")
                 .Replace("¹", "^{1}")
                 .Replace("²", "^{2}")
                 .Replace("³", "^{3}")
                 .Replace("⁴", "^{4}")
                 .Replace("⁵", "^{5}")
                 .Replace("⁶", "^{6}")
                 .Replace("⁷", "^{7}")
                 .Replace("⁸", "^{8}")
                 .Replace("⁹", "^{9}");
            // Точка над символом: ṁ/V̇ → \dot{...} (комбинируемая диакритика
            // в math-фолбэке отсутствует).
            t = t.Replace("ṁ", "\\dot{m}").Replace("V̇", "\\dot{V}");
            // Операторы: · * → \cdot; типографские минусы; стрелка.
            t = t.Replace("·", " \\cdot ")
                 .Replace("*", " \\cdot ")
                 .Replace("−", " - ")
                 .Replace("—", " - ")
                 .Replace("→", " \\rightarrow ");
            // Смежные индексы «плоской» записи: tП → t_{П}, ηR → η_{R}
            // (qTotal не разбивается — за заглавной идут строчные).
            t = Regex.Replace(t, "([a-zṁ])([А-ЯЁA-Z])(?![a-zа-яё])", "$1_{$2}");
            // Уже свёрстанные группы _{...} не затрагиваются \text-проходом.
            var protectedGroups = new List<string>();
            t = Regex.Replace(t, "_\\{[^}]*\\}", m =>
            {
                protectedGroups.Add(m.Value);
                return "\u0001" + (protectedGroups.Count - 1).ToString(CultureInfo.InvariantCulture) + "\u0001";
            });
            // Корни: √( … )/sqrt( … ) → \sqrt{ … } (В10) — после защиты
            // подстрочных групп, чтобы подкоренные индексы (d_нар) не
            // разъехались; скобки балансируются, вложенность — рекурсией.
            t = ConvertSquareRoots(t);
            // Оставшаяся кириллица → \text{...}; пробелы внутри \text
            // сохраняются текстовым режимом.
            t = Regex.Replace(
                t,
                "\\s*((?:[А-Яа-яЁё][А-Яа-яЁё0-9/]*)(?:\\s+[А-Яа-яЁё0-9/]+)*)",
                m => " \\text{ " + m.Groups[1].Value + " } ");
            t = Regex.Replace(t, "\u0001(\\d+)\u0001", m => protectedGroups[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)]);
            return Regex.Replace(t, "\\s+", " ").Trim();
        }

        /// <summary>
        /// √( … )/sqrt( … ) → \sqrt{ … }: винкула рисуется CSharpMath вместо
        /// голого глифа радикала. Маркер без скобочной группы (√x) не
        /// конвертируется; несбалансированная скобка — остаток остаётся как
        /// есть (рендер откатится к тексту, правило проза-фолбэка).
        /// </summary>
        private static string ConvertSquareRoots(string t)
        {
            var i = 0;
            while (i < t.Length)
            {
                var markerLength = RootMarkerLength(t, i);
                if (markerLength == 0)
                {
                    i++;
                    continue;
                }

                var open = i + markerLength;
                if (open >= t.Length || t[open] != '(')
                {
                    i++;
                    continue;
                }

                var close = FindMatchingParen(t, open);
                if (close < 0)
                {
                    return t;
                }

                var radicand = ConvertSquareRoots(t.Substring(open + 1, close - open - 1));
                t = t.Substring(0, i) + "\\sqrt{" + radicand + "}" + t.Substring(close + 1);
                i += "\\sqrt{".Length + radicand.Length + 1;
            }

            return t;
        }

        /// <summary>Длина маркера корня в позиции <paramref name="i"/>:
        /// «√» либо «sqrt» вне слова (не asqrt, не d_sqrt, не уже-готовый \sqrt).</summary>
        private static int RootMarkerLength(string t, int i)
        {
            if (t[i] == '√')
            {
                return 1;
            }

            if (t[i] != 's' || i + 4 > t.Length || t[i + 1] != 'q' || t[i + 2] != 'r' || t[i + 3] != 't')
            {
                return 0;
            }

            if (i > 0 && (char.IsLetterOrDigit(t[i - 1]) || t[i - 1] == '_' || t[i - 1] == '\\'))
            {
                return 0;
            }

            return 4;
        }

        /// <summary>Позиция скобки, парной к открывающей в <paramref name="open"/>; −1 — не найдена.</summary>
        private static int FindMatchingParen(string t, int open)
        {
            var depth = 0;
            for (var j = open; j < t.Length; j++)
            {
                if (t[j] == '(')
                {
                    depth++;
                }
                else if (t[j] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return j;
                    }
                }
            }

            return -1;
        }

        private static FormulaImage? TryRenderPngInner(string latex)
        {
            try
            {
                var painter = new MathPainter
                {
                    LaTeX = latex,
                    FontSize = RenderFontSize,
                    TextColor = SKColors.Black,
                };
                if (!string.IsNullOrEmpty(painter.ErrorMessage))
                {
                    return null;
                }

                const int pad = 2;
                // Канва 3× с запасом: базовая линия на y = 162px — над ней
                // 162/30 ≈ 5,4em (требование В10: ≥ 5em), под ней ≈ 7,9em —
                // высокие конструкции (\sqrt{…} с дробью) не режутся.
                using var bitmap = new SKBitmap(3600, 400);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    painter.Draw(canvas, new SKPoint(pad + 8, pad + 160));
                    canvas.Flush();
                }

                if (!string.IsNullOrEmpty(painter.ErrorMessage))
                {
                    return null;
                }

                var bounds = FindContentBounds(bitmap);
                if (bounds == default)
                {
                    return null;
                }

                var crop = SKRectI.Inflate(new SKRectI(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom), pad, pad);
                crop.Intersect(new SKRectI(0, 0, bitmap.Width, bitmap.Height));
                using var cropped = new SKBitmap();
                if (!bitmap.ExtractSubset(cropped, crop))
                {
                    return null;
                }

                using var data = cropped.Encode(SKEncodedImageFormat.Png, 90);
                var bytes = data?.ToArray();
                return bytes == null || bytes.Length == 0
                    ? null
                    : new FormulaImage(bytes, cropped.Width, cropped.Height);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LaTeX-рендер формулы: {ex.Message}");
                return null;
            }
        }

        /// <summary>Границы непрозрачного содержимого (alpha &gt; 0).</summary>
        private static SKRectI FindContentBounds(SKBitmap bitmap)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = -1;
            var maxY = -1;
            var bytes = bitmap.Bytes;
            var width = bitmap.Width;
            var height = bitmap.Height;
            for (var y = 0; y < height; y++)
            {
                var rowStart = y * bitmap.RowBytes;
                for (var x = 0; x < width; x++)
                {
                    if (bytes[rowStart + x * 4 + 3] == 0)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX < 0 ? default : new SKRectI(minX, minY, maxX + 1, maxY + 1);
        }
    }
}
