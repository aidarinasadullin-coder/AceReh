namespace SnowMeltingCalculator.Services.Printing;

/// <summary>
/// Результат shell-операции печати/открытия PDF: исключение не выходит
/// наружу, вызывающая сторона получает Success/ErrorMessage (волна 2
/// hardening-роадмапа, решения владельца D1/D8).
/// </summary>
public readonly record struct PrintResult(bool Success, string? ErrorMessage)
{
    public static PrintResult Ok() => new(true, null);

    public static PrintResult Fail(string message) => new(false, message);
}

/// <summary>
/// Печать PDF и фолбэк-открытие в системном просмотрщике (D1: печать
/// вынесена из <c>ResultsViewModel</c> в отдельный сервис; D8: при отказе
/// печати пользователь может открыть PDF системным просмотрщиком).
/// </summary>
public interface IPrintService
{
    /// <summary>Отправить PDF системному принтеру через shell verb «print».</summary>
    PrintResult PrintPdf(string pdfPath);

    /// <summary>Открыть PDF в системном просмотрщике через shell verb «open».</summary>
    PrintResult OpenPdf(string pdfPath);
}
