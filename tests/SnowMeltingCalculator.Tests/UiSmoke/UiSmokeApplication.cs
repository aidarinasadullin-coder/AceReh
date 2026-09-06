// ================================================================================
// Фаза 1Б редизайна — FlaUI smoke-набор (UiSmoke).
// ================================================================================
//
// Инфраструктура запуска реального exe приложения (src/bin/.../win-x64) через
// FlaUI.UIA3 и селекторы по AutomationId, закреплённым
// ThermalAutomationIdSelectorContractTests. Тесты в этой папке помечены
// Category("UiSmoke"): в интерактивной сессии входят в обычный dotnet test,
// в headless/CI исключаются фильтром --filter "Category!=UiSmoke"
// (UIA3 требует интерактивной сессии).
//
// ================================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

/// <summary>
/// Управляемый экземпляр приложения для smoke-прогона: процесс exe, UIA3-автоматизация
/// и главное окно. Не каноническое состояние (R1–R6 не затрагиваются) — только
/// read-only автоматизация поверх реального процесса.
/// </summary>
public sealed class UiSmokeApplication : IDisposable
{
    /// <summary>Таймаут появления главного окна: SelfContained-exe грузит климат и материалы.</summary>
    private static readonly TimeSpan WindowTimeout = TimeSpan.FromSeconds(90);

    /// <summary>Таймаут внутрии-интерфейсных переходов (навигация, обновление статус-бара).</summary>
    private static readonly TimeSpan UiTimeout = TimeSpan.FromSeconds(10);

    private readonly Application _app;
    private readonly UIA3Automation _automation;

    private UiSmokeApplication(Application app, UIA3Automation automation, Window window)
    {
        _app = app;
        _automation = automation;
        Window = window;
    }

    /// <summary>Главное окно приложения.</summary>
    public Window Window { get; }

    /// <summary>Путь к собранному exe приложения (конфигурация — как у тестовой сборки).</summary>
    public static string AppExePath
    {
        get
        {
            // tests/bin/<Configuration>/net8.0-windows/ → корень репозитория
            var testBin = new DirectoryInfo(AppContext.BaseDirectory);
            var configuration = testBin.Parent?.Name
                ?? throw new InvalidOperationException($"Не определена конфигурация сборки тестов: {testBin}");
            return Path.Combine(
                RepoRoot, "src", "bin", configuration, "net8.0-windows", "win-x64",
                "SnowMeltingCalculator.exe");
        }
    }

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SnowMeltingCalculator.sln")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName
                ?? throw new InvalidOperationException("Корень репозитория не найден от " + AppContext.BaseDirectory);
        }
    }

    private static string FixtureSmcPath => Path.Combine(
        RepoRoot, "tests", "SnowMeltingCalculator.Tests", "Fixtures", "v1-sample.smc");

    /// <summary>
    /// Запустить exe (опционально с аргументами командной строки, например путь к .smc)
    /// и дождаться главного окна. Рабочая директория — папка exe.
    /// Окно выбирается по заголовку «Калькулятор снеготаяния REHAU», а не по
    /// process.MainWindowHandle: сплэш Ф7.2 (первое видимое окно процесса)
    /// перехватил бы MainWindowHandle (P2-4 ревью Ф7).
    /// </summary>
    public static UiSmokeApplication Launch(params string[] arguments)
    {
        var exePath = AppExePath;
        Assert.That(File.Exists(exePath), Is.True,
            $"Приложение не собрано: {exePath} не найден. Выполните 'dotnet build' перед прогоном UiSmoke.");

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = string.Join(' ', arguments.Select(a => $"\"{a}\"")),
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не удалось запустить процесс: {exePath}");
        var app = new Application(process);
        UIA3Automation? automation = null;

        try
        {
            // Создание автоматизации под try: сбой COM/UIA (например, запуск
            // не в интерактивной сессии) не должен оставлять процесс висеть.
            automation = new UIA3Automation();
            var window = Retry.WhileNull(
                    () => FindMainWindow(app, automation),
                    WindowTimeout,
                    ignoreException: true).Result
                ?? throw new TimeoutException(
                    $"Главное окно не появилось за {WindowTimeout.TotalSeconds} с (процесс HasExited={app.HasExited}).");

            return new UiSmokeApplication(app, automation, window);
        }
        catch
        {
            automation?.Dispose();
            app.Kill();
            throw;
        }
    }

    /// <summary>
    /// Главное окно среди top-level окон процесса: заголовок заканчивается
    /// на «Калькулятор снеготаяния REHAU» (MainViewModel.WindowTitle).
    /// Сплэш (Title=«Загрузка») и служебные окна не совпадают.
    /// </summary>
    private static Window? FindMainWindow(Application app, UIA3Automation automation)
    {
        return app.GetAllTopLevelWindows(automation)
            .FirstOrDefault(w => (w.Title ?? string.Empty)
                .EndsWith("Калькулятор снеготаяния REHAU", StringComparison.Ordinal));
    }

    /// <summary>Путь к read-only фикстуре проекта v1-sample.smc (мутировать запрещено).</summary>
    public static string SampleProjectPath()
    {
        Assert.That(File.Exists(FixtureSmcPath), Is.True, $"Фикстура не найдена: {FixtureSmcPath}");
        return FixtureSmcPath;
    }

    /// <summary>
    /// Дождаться элемента по AutomationId в окне и вернуть его (или null по таймауту).
    /// </summary>
    public AutomationElement? WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        return Retry.WhileNull(
            () => Window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            timeout ?? UiTimeout,
            ignoreException: true).Result;
    }

    /// <summary>
    /// Найти элемент по AutomationId в любом top-level окне процесса (WPF Popup
    /// живёт в отдельном HWND и не виден среди потомков главного окна).
    /// Null, если элемента нет нигде.
    /// </summary>
    public AutomationElement? FindInAnyWindow(string automationId)
    {
        foreach (var window in _app.GetAllTopLevelWindows(_automation))
        {
            try
            {
                var element = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (element is not null)
                {
                    return element;
                }
            }
            catch
            {
                // Окно могло закрыться в момент обхода — пропускаем.
            }
        }

        return null;
    }

    /// <summary>Прочитать текст элемента по AutomationId (пустая строка, если элемент не найден).</summary>
    public string ReadText(string automationId, TimeSpan? timeout = null)
    {
        return WaitForElement(automationId, timeout)?.Name ?? string.Empty;
    }

    /// <summary>
    /// Текст скошенной плашки статус-бара («КЛИМАТ», «КОНСТРУКЦИЯ», …) — канона-
    /// льный признак текущего модуля каркаса (ShellModulePlate).
    /// </summary>
    public string ModulePlate => ReadText("ShellModulePlate");

    /// <summary>
    /// Дождаться плашки модуля — синхронизация перехода после выбора шага степпера.
    /// Чтение — прямой поиск без внутреннего Retry, чтобы не каскадировать таймауты.
    /// </summary>
    public void WaitModulePlate(string expectedPlate, TimeSpan? timeout = null)
    {
        var plateNow = string.Empty;
        var reached = Retry.WhileTrue(
            () =>
            {
                plateNow = Window.FindFirstDescendant(cf => cf.ByAutomationId("ShellModulePlate"))
                    ?.Name ?? string.Empty;
                return !string.Equals(plateNow, expectedPlate, StringComparison.Ordinal);
            },
            timeout ?? UiTimeout,
            ignoreException: true).Success;

        Assert.That(reached, Is.True,
            $"Статус-бар не показал «{expectedPlate}»; текущий текст: «{plateNow}».");
    }

    /// <summary>
    /// Перейти на шаг сценария по русскому заголовку степпера
    /// («Климат», «Конструкция», «Тепловой расчёт», «Гидравлика», «Результаты»;
    /// короткие названия — по эталону 01, Фаза 3Б).
    /// Список адресуется по контракту ShellStepperList (план Ф2.6, снятие
    /// техдолга Ф1Б); эвристика «первый List в окне» оставлена фолбэком.
    /// </summary>
    public void NavigateTo(string stepTitle)
    {
        AutomationElement? FindStepInList(Func<AutomationElement?> findList)
        {
            var list = findList();
            return list?
                .FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .FirstOrDefault(candidate => candidate
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                    .Any(text => text.Name == stepTitle));
        }

        AutomationElement? ByStepperListId() =>
            Window.FindFirstDescendant(cf => cf.ByAutomationId("ShellStepperList"));

        AutomationElement? ByFirstListHeuristic() =>
            Window.FindFirstDescendant(cf => cf.ByControlType(ControlType.List));

        var item = Retry.WhileNull(
            () => FindStepInList(ByStepperListId) ?? FindStepInList(ByFirstListHeuristic),
            UiTimeout,
            ignoreException: true).Result;

        Assert.That(item, Is.Not.Null, $"Шаг степпера «{stepTitle}» не найден.");
        item!.AsListBoxItem().Select();
    }

    /// <summary>Грейсфул-закрытие приложения; Close(true) добивает процесс при сбое.</summary>
    public void Dispose()
    {
        try
        {
            _automation.Dispose();
        }
        catch
        {
            // автоматизация могла быть уже освобождена — не мешаем закрытию процесса
        }

        if (!_app.HasExited)
        {
            _app.Close();
        }
    }
}
