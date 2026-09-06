using PdfSharp.Fonts;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Однократная инициализация шрифтового резолвера PDFsharp перед первым
    /// рендером детального отчёта (урок №9 Ф8): в Core-сборке 6.2 «из коробки»
    /// шрифты не резолвятся — под Windows включается штатный
    /// <see cref="GlobalFontSettings.UseWindowsFontsUnderWindows"/>
    /// (Arial содержит кириллицу, эмбеддинг подмножеством штатный).
    /// </summary>
    /// <remarks>
    /// Флаг идемпотентен; установка не бросает исключений даже если
    /// шрифтовые операции в процессе уже выполнялись (краткий PDF).
    /// Резолвер Inter добавляется на шаге PDF-2 (брендбук), здесь — база.
    /// </remarks>
    public static class CalculationReportPdfFontBootstrapper
    {
        private static bool _initialized;

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Инициализация шрифтов PDFsharp: {ex.Message}");
            }
        }
    }
}
