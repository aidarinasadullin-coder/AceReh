using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using PdfSharp.Fonts;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Композитный шрифтовый резолвер PDFsharp (брендбук, спека §7.2):
    /// фирменный Inter подаётся из TTF, встроенных в сборку
    /// (<c>Assets/Fonts/Inter-*.ttf</c>), остальные семейства делегируются
    /// штатному платформенному резолверу (урок №9 Ф8) — вывод краткого PDF
    /// (Arial) и прочих шрифтов не меняется.
    /// </summary>
    /// <remarks>
    /// Inter не установлен в системе — без встраивания рендер с Inter падает
    /// («No appropriate font found»), поэтому бренд-шрифт отдаётся байтами.
    /// Курсив брендбуком не допускается; запрос курсива обслуживается
    /// прямым начертанием с симуляцией наклона.
    /// </remarks>
    public sealed class CalculationReportInterFontResolver : IFontResolver
    {
        /// <summary>Имя семейства, отдаваемое резолвером.</summary>
        public const string FamilyName = "Inter";

        private const string ResourceRoot = "assets/fonts/";
        private const string PackUriRoot = "pack://application:,,,/SnowMeltingCalculator;component/Assets/Fonts/inter-";

        private static readonly Dictionary<string, byte[]> FontCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Проверить, что встроенный Inter доступен, не устанавливая резолвер.</summary>
        public static bool CanLoadFonts()
        {
            return TryLoadFontBytes("Regular").Length > 0;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (!string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                // Делегация вне резолвера разрешена: платформенный резолвер
                // сам регистрирует байты шрифта, GetFont для его граней не зовётся.
                // Null (неизвестное семейство) — штатное поведение PDFsharp:
                // стандартная ошибка «No appropriate font found».
                return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic)!;
            }

            var faceName = isBold ? "Inter-Bold" : "Inter-Regular";
            return new FontResolverInfo(faceName, mustSimulateBold: false, mustSimulateItalic: isItalic);
        }

        public byte[] GetFont(string faceName)
        {
            var embedded = faceName switch
            {
                "Inter-Regular" => "Regular",
                "Inter-Bold" => "Bold",
                _ => null,
            };

            if (embedded is null)
            {
                // Грани чужих семейств резолвер не возвращает — платформенный
                // резолвер регистрирует их байты сам (проверено рендером Ф8).
                return null!;
            }

            return TryLoadFontBytes(embedded);
        }

        private static byte[] TryLoadFontBytes(string styleName)
        {
            if (FontCache.TryGetValue(styleName, out var cached))
            {
                return cached;
            }

            // Ключ ресурса: assets/fonts/inter-<начертание в нижнем регистре>.ttf.
            var fileName = $"inter-{styleName.ToLowerInvariant()}.ttf";
            var bytes = LoadViaResourceManager(fileName)
                        ?? LoadViaPackUri($"Inter-{styleName}.ttf")
                        ?? Array.Empty<byte>();
            FontCache[styleName] = bytes;
            return bytes;
        }

        /// <summary>
        /// WPF-ресурсы лежат в <c>AssemblyName.g.resources</c> — читаются
        /// без работающего <see cref="System.Windows.Application"/> (тесты).
        /// </summary>
        private static byte[]? LoadViaResourceManager(string fileName)
        {
            try
            {
                var assembly = typeof(CalculationReportInterFontResolver).Assembly;
                // WPF кладёт ресурсы в <Assembly>.g.resources; ResourceManager
                // сам добавляет суффикс «.resources», поэтому корень — «.g».
                var manager = new ResourceManager(assembly.GetName().Name + ".g", assembly);
                using var stream = manager.GetStream(ResourceRoot + fileName) as Stream;
                if (stream == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static byte[]? LoadViaPackUri(string fileName)
        {
            try
            {
                var uri = new Uri(PackUriRoot + fileName, UriKind.Absolute);
                var info = System.Windows.Application.GetResourceStream(uri);
                if (info?.Stream == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
