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
    /// отрисовывается CSharpMath/SkiaSharp в PNG (двукратный масштаб к
    /// точечному размеру PDF — кириллица в индексах поддерживается).
    /// </summary>
    /// <remarks>
    /// Формулы с прозой (4+ кириллических слова вне subscript'ов — пояснения
    /// вида «Колбрук–Уайт (итерации…)») не конвертируются: рендер оставляет
    /// их текстом. Никаких вычислений — только нотация (AC-5).
    /// </remarks>
    public static class CalculationReportLaTeXFormulaRenderer
    {
        /// <summary>Размер шрифта рендера, px (в PDF размещается 0,5 pt/px).</summary>
        private const float RenderFontSize = 20f;

        /// <summary>Пт на пиксель при размещении в документе.</summary>
        public const double PointPerPixel = 0.5;

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
            // Оставшаяся кириллица → \text{...}; пробелы внутри \text
            // сохраняются текстовым режимом.
            t = Regex.Replace(
                t,
                "\\s*((?:[А-Яа-яЁё][А-Яа-яЁё0-9/]*)(?:\\s+[А-Яа-яЁё0-9/]+)*)",
                m => " \\text{ " + m.Groups[1].Value + " } ");
            t = Regex.Replace(t, "\u0001(\\d+)\u0001", m => protectedGroups[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)]);
            return Regex.Replace(t, "\\s+", " ").Trim();
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
                // Рендер на запасной канве с последующей обрезкой по
                // непрозрачным пикселям (Display до Draw не заполнен).
                using var bitmap = new SKBitmap(2400, 240);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    painter.Draw(canvas, new SKPoint(pad + 8, pad + 80));
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
