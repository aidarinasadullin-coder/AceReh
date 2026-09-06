using PdfSharp.Fonts;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Однократная инициализация шрифтового резолвера PDFsharp перед первым
    /// рендером детального отчёта (урок №9 Ф8): в Core-сборке 6.2 «из коробки»
    /// шрифты не резолвятся — под Windows включается штатный
    /// <see cref="GlobalFontSettings.UseWindowsFontsUnderWindows"/>,
    /// поверх ставится композитный резолвер бренд-шрифта Inter
    /// (<see cref="CalculationReportInterFontResolver"/>, спека §7.2).
    /// </summary>
    /// <remarks>
    /// PDFsharp допускает установку глобального резолвера только до первой
    /// шрифтовой операции процесса. Если она уже произошла (например, краткий
    /// PDF отрендерился раньше), установка невозможна — тогда отчёт
    /// печатается резервным Arial (допустим по гайдлайну, §3.2 гайда).
    /// Вызов идемпотентен.
    /// </remarks>
    public static class CalculationReportPdfFontBootstrapper
    {
        private static bool _initialized;

        /// <summary>Резолвер Inter установлен — рендер может использовать Inter.</summary>
        public static bool InterAvailable { get; private set; }

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            try
            {
                GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Инициализация шрифтов PDFsharp: {ex.Message}");
            }

            try
            {
                if (CalculationReportInterFontResolver.CanLoadFonts())
                {
                    GlobalFontSettings.FontResolver = new CalculationReportInterFontResolver();
                    InterAvailable = true;
                }
            }
            catch (System.Exception ex)
            {
                // Резолвер нельзя менять после шрифтовых операций — откат на Arial.
                InterAvailable = false;
                System.Diagnostics.Debug.WriteLine($"Резолвер Inter не установлен: {ex.Message}");
            }
        }
    }
}
