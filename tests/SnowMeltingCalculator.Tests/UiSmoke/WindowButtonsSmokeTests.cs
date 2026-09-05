// ================================================================================
// Фаза 3Б редизайна — smoke: кнопки управления окном.
// ================================================================================
//
// Развернуть/восстановить и свернуть через AutomationId WindowMaximizeButton /
// WindowMinimizeButton (план Ф3Б п.3); состояние проверяется по UIA WindowPattern.
// Закрытие в smoke не проверяется — завершает процесс.
// Минимизация выполняется последним тестом фикстуры: приложение запускается
// один раз (UiSmokeFixtureBase), свернутое окно не должно сломать остальные.
//
// ================================================================================

using FlaUI.Core.Definitions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

[TestFixture]
public sealed class WindowButtonsSmokeTests : UiSmokeFixtureBase
{
    [Test, Order(1)]
    public void WindowButtons_PresentInHeader()
    {
        Assert.Multiple(() =>
        {
            Assert.That(App.WaitForElement("WindowMinimizeButton"), Is.Not.Null,
                "Кнопка «Свернуть» присутствует в шапке (Фаза 3Б).");
            Assert.That(App.WaitForElement("WindowMaximizeButton"), Is.Not.Null,
                "Кнопка «Развернуть/Восстановить» присутствует в шапке (Фаза 3Б).");
            Assert.That(App.WaitForElement("WindowCloseButton"), Is.Not.Null,
                "Кнопка «Закрыть» присутствует в шапке (Фаза 3Б).");
        });
    }

    [Test, Order(2)]
    public void WindowButtons_MaximizeAndRestore_Roundtrip()
    {
        var windowPattern = App.Window.Patterns.Window.Pattern;

        Assert.That(windowPattern.WindowVisualState, Is.EqualTo(WindowVisualState.Normal),
            "Тест предполагает нормальное состояние окна на старте фикстуры.");

        App.WaitForElement("WindowMaximizeButton")!.Click();
        Assert.That(windowPattern.WindowVisualState, Is.EqualTo(WindowVisualState.Maximized),
            "Клик по кнопке разворачивает окно.");

        App.WaitForElement("WindowMaximizeButton")!.Click();
        Assert.That(windowPattern.WindowVisualState, Is.EqualTo(WindowVisualState.Normal),
            "Повторный клик восстанавливает окно.");
    }

    [Test, Order(99)]
    public void WindowButtons_Minimize_LastStepOfFixture()
    {
        var windowPattern = App.Window.Patterns.Window.Pattern;

        App.WaitForElement("WindowMinimizeButton")!.Click();
        Assert.That(windowPattern.WindowVisualState, Is.EqualTo(WindowVisualState.Minimized),
            "Клик по кнопке сворачивает окно.");
    }
}
