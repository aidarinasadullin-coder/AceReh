using System.Diagnostics;

namespace SnowMeltingCalculator.Services.Printing;

/// <summary>
/// Shell-печать PDF (verb «print») и открытие в системном просмотрщике
/// (verb «open»).
///
/// <para><see cref="ProcessStartInfo.UseShellExecute"/> = <c>true</c>
/// обязателен: начиная с .NET Core умолчание — <c>false</c>, а verb
/// «print» работает только через ShellExecute — на .NET 8 прежний код
/// с дефолтным <c>false</c> падал Win32Exception на каждом вызове
/// (P0-находка ревизии 2026-09-18, решение владельца D1: сервис новый,
/// а не правка inline-блока).</para>
///
/// <para>Реальный акт печати без подключённого принтера юнит-тестом не
/// верифицируется — принятое ограничение D1; пинятся параметры
/// <see cref="ProcessStartInfo"/> через шов <see cref="_processFactory"/>
/// (процесс не запускается). Процесс одноразовый — диспоузится через
/// <c>using</c>; <c>null</c> от <c>Process.Start</c> при успешном
/// shell-старте легален.</para>
/// </summary>
public sealed class PrintService : IPrintService
{
    private readonly Func<ProcessStartInfo, Process?> _processFactory;

    public PrintService(Func<ProcessStartInfo, Process?>? processFactory = null)
    {
        _processFactory = processFactory ?? Process.Start;
    }

    public PrintResult PrintPdf(string pdfPath) => RunShell(pdfPath, "print");

    public PrintResult OpenPdf(string pdfPath) => RunShell(pdfPath, "open");

    private PrintResult RunShell(string pdfPath, string verb)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pdfPath,
            Verb = verb,
            UseShellExecute = true,
            CreateNoWindow = true,
        };

        try
        {
            using var process = _processFactory(startInfo);
            return PrintResult.Ok();
        }
        catch (Exception ex)
        {
            return PrintResult.Fail(ex.Message);
        }
    }
}
