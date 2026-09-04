// ================================================================================
// Фаза 1Б редизайна — smoke: чистый старт приложения и навигация по 5 модулям.
// ================================================================================
//
// Проверяет каркас (Фаза 1): окно стартует, статус-бар показывает плашку
// активного модуля (ShellModulePlate), степпер содержит все 5 шагов и переходы
// по ним материализуют модульные вьюхи (якоря — существующие AutomationId
// Thermal*/Hydraulics*/Results*; для Климата и Конструкции якорь — плашка).
//
// ================================================================================

using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

[TestFixture]
public sealed class StartupAndNavigationSmokeTests : UiSmokeFixtureBase
{
    private static readonly (string Step, string Plate)[] Steps =
    {
        ("Климат", "КЛИМАТ"),
        ("Конструкция", "КОНСТРУКЦИЯ"),
        ("Тепловой расчёт", "ТЕПЛОВОЙ"),
        ("Гидравлический расчёт", "ГИДРАВЛИКА"),
        ("Результаты", "РЕЗУЛЬТАТЫ")
    };

    private static readonly (string Step, string AnchorAutomationId)[] ModuleAnchors =
    {
        ("Тепловой расчёт", "ThermalMode"),
        ("Гидравлический расчёт", "HydraulicsGlycolType"),
        ("Результаты", "ResultsThermalPower")
    };

    [Test, Order(1)]
    public void Start_ShellStructureIsPresent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(App.Window.Title, Does.Contain("Калькулятор снеготаяния"),
                "Заголовок окна — брендированный, без открытого проекта.");

            // Стартовый модуль — Климат (плашка статус-бара каркаса)
            Assert.That(App.ModulePlate, Is.EqualTo("КЛИМАТ"),
                "При чистом старте активен шаг «Климат».");

            // Слот валидации активного модуля (контракт ShellValidationMessage)
            Assert.That(App.WaitForElement("ShellValidationMessage"), Is.Not.Null,
                "Слот валидации статус-бара должен присутствовать в каркасе.");
        });
    }

    [Test, Order(2)]
    public void Navigation_AllFiveSteps_AreReachable()
    {
        Assert.Multiple(() =>
        {
            foreach (var (step, plate) in Steps)
            {
                App.NavigateTo(step);
                App.WaitModulePlate(plate);
            }

            // Материализация модульных вьюх — по существующим AutomationId якорям
            foreach (var (step, anchor) in ModuleAnchors)
            {
                App.NavigateTo(step);
                App.WaitModulePlate(plateOf(step));
                Assert.That(App.WaitForElement(anchor), Is.Not.Null,
                    $"После перехода на «{step}» не найден якорьAutomationId {anchor}.");
            }
        });

        static string plateOf(string step) =>
            System.Array.Find(Steps, pair => pair.Step == step).Plate;
    }
}
