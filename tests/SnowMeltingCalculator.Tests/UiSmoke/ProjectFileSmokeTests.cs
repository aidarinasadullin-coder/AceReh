// ================================================================================
// Фаза 1Б редизайна — smoke: открытие .smc, один тепловой расчёт, сохранение.
// ================================================================================
//
// Проект открывается командной строкой (сценарий двойного клика по .smc в
// проводнике, MainWindow.InitialProjectPath). Фикстура v1-sample.smc
// read-only: для сценария сохранения используется временная копия в Temp.
// Расчёт: правка «Температура грунта» → ThermalCalculate → результат в UI
// изменился. Сохранение: меню «Файл → Сохранить» (CurrentFilePath известен —
// диалог не открывается), файл на диске проверяется по содержимому.
//
// ================================================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

[TestFixture]
public sealed class ProjectFileSmokeTests : UiSmokeFixtureBase
{
    private static readonly TimeSpan CalculationTimeout = TimeSpan.FromSeconds(30);

    private string? _tempProjectPath;
    private string _tempFileName = string.Empty;

    protected override string[] LaunchArguments => new[] { TempProjectPath() };

    private string TempProjectPath()
    {
        if (_tempProjectPath is null)
        {
            _tempFileName = $"v1-sample-uismoke-{Guid.NewGuid():N}.smc";
            _tempProjectPath = Path.Combine(Path.GetTempPath(), _tempFileName);
            File.Copy(UiSmokeApplication.SampleProjectPath(), _tempProjectPath, overwrite: true);

            // Климат копии приводится к реалистичному рабочему режиму снеготаяния:
            // фикстура хранит расчётный холодный сценарий (воздух −28 °C, ветер 5),
            // при котором требуемая мощность делает любой расчёт неосуществимым
            // (подача > 100 °C — нереалистично). Рабочий режим: около нуля, ветер ~3.
            var json = File.ReadAllText(_tempProjectPath);
            json = json.Replace("\"airTemperature\": -28.0", "\"airTemperature\": -2.0");
            json = json.Replace("\"windSpeed\": 5.0", "\"windSpeed\": 3.0");
            File.WriteAllText(_tempProjectPath, json);
        }

        return _tempProjectPath;
    }

    [OneTimeTearDown]
    public void DeleteTempCopy()
    {
        if (_tempProjectPath is not null)
        {
            // Приложение оставляет рядом .bak — убираем временную копию целиком
            foreach (var path in new[] { _tempProjectPath, _tempProjectPath + ".bak" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    [Test, Order(1)]
    public void Open_FromCommandLine_TitleReflectsProjectFile()
    {
        Retry.WhileTrue(
            () => !App.Window.Title.Contains(_tempFileName, StringComparison.Ordinal),
            TimeSpan.FromSeconds(15),
            ignoreException: true);

        Assert.Multiple(() =>
        {
            Assert.That(App.Window.Title, Does.Contain(_tempFileName),
                "Заголовок окна должен отражать открытый файл проекта.");
            Assert.That(App.Window.Title, Does.Contain("Калькулятор снеготаяния"),
                "Заголовок окна остаётся брендированным.");
            Assert.That(App.ModulePlate, Is.EqualTo("КЛИМАТ"),
                "После открытия проекта активен шаг «Климат».");
        });
    }

    [Test, Order(2)]
    public void ThermalCalculation_AndMenuSave_UpdateProjectFile()
    {
        // --- Навигация на Тепловой расчёт
        App.NavigateTo("Тепловой расчёт");
        App.WaitModulePlate("ТЕПЛОВОЙ");
        Assert.That(App.WaitForElement("ThermalMode"), Is.Not.Null,
            "Вьюха теплового расчёта должна быть материализована.");

        // Результат из открытого проекта (v1: powerTotal = 261.0) в русской локали
        var powerBefore = App.ReadText("ThermalPowerTotal");
        Assert.That(powerBefore, Does.Contain(","),
            "Числа в UI отображаются в русской локали (десятичная запятая — Ф0.6).");

        // --- Правка входа: подача 50,0 → 35,0 (валидный и реалистичный диапазон
        // 20–90; рабочие подачи снеготаяния — 34–45 °C). Ожидаемый исход: расчёт
        // сходится, слот валидации пуст, результат пересчитывается.
        var supplyTemperature = App.WaitForElement("ThermalSupplyTemperature")
            ?? throw new InvalidOperationException("ThermalSupplyTemperature не найден.");
        supplyTemperature.AsTextBox().Text = "35,0";

        // --- Один расчёт
        var calculateButton = App.WaitForElement("ThermalCalculate")
            ?? throw new InvalidOperationException("ThermalCalculate не найден.");
        calculateButton.AsButton().Invoke();

        var calculated = Retry.WhileTrue(
            () => !string.IsNullOrEmpty(App.ReadText("ShellValidationMessage"))
                || App.ReadText("ThermalPowerTotal") == powerBefore,
            CalculationTimeout,
            ignoreException: true).Success;

        Assert.Multiple(() =>
        {
            Assert.That(calculated, Is.True,
                $"Тепловой расчёт не сошёлся. Валидация: «{App.ReadText("ShellValidationMessage")}», " +
                $"мощность: «{App.ReadText("ThermalPowerTotal")}» (была «{powerBefore}»).");
            Assert.That(App.ReadText("ShellValidationMessage"), Is.Empty,
                "При реалистичном входе расчёт сходится без ошибок валидации.");
            Assert.That(App.ReadText("ShellValidationMessage"), Does.Not.Contain("Ошибка расчёта"),
                "Калькулятор не должен падать с исключением на входах сценария.");
            Assert.That(App.ReadText("ThermalPowerTotal"), Is.Not.EqualTo(powerBefore),
                "Пересчитанная суммарная мощность отличается от сохранённой в файле.");
        });

        // --- Сохранение через меню «Файл → Сохранить» (без диалога: путь известен).
        // Верхний пункт меню WPF раскрывается (ExpandCollapse), подпункт — Invoke.
        var saveTimeBefore = File.GetLastWriteTimeUtc(TempProjectPath());
        var fileMenu = App.Window.FindFirstDescendant(cf => cf.ByName("Файл"))
            ?? throw new InvalidOperationException("Меню «Файл» не найдено в шапке.");
        fileMenu.AsMenuItem().Expand();

        var saveItem = Retry.WhileNull(
            () => App.Window.FindFirstDescendant(cf => cf.ByName("Сохранить")),
            TimeSpan.FromSeconds(10),
            ignoreException: true).Result
            ?? throw new InvalidOperationException("Пункт «Сохранить» не найден в меню «Файл».");
        ActivateMenuItem(saveItem);

        // Файл на диске: перезаписан и содержит правку температуры грунта
        var saved = Retry.WhileTrue(
            () => File.GetLastWriteTimeUtc(TempProjectPath()) <= saveTimeBefore,
            TimeSpan.FromSeconds(15),
            ignoreException: true).Success;
        Assert.That(saved, Is.True, "Файл проекта не перезаписан после «Файл → Сохранить».");

        var savedJson = File.ReadAllText(TempProjectPath());
        Assert.That(savedJson, Does.Match(new Regex("\"supplyTemperature\"\\s*:\\s*35(\\.0+)?")),
            "Сохранённый файл должен содержать изменённую температуру подачи 35,0.");
    }

    /// <summary>Активировать пункт подменю: Invoke, а при его отсутствии — клик.</summary>
    private static void ActivateMenuItem(FlaUI.Core.AutomationElements.AutomationElement item)
    {
        try
        {
            item.AsMenuItem().Invoke();
        }
        catch (FlaUI.Core.Exceptions.PatternNotSupportedException)
        {
            item.AsMenuItem().Click(false);
        }
    }
}
