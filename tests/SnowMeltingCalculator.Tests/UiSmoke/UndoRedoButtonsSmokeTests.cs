// ================================================================================
// ADR-014 — smoke: кнопки «Отменить / Вернуть» в шапке.
// ================================================================================
//
// Приёмка плана undo/redo (2026-09-08) §10.1: на заставке кнопок нет;
// края истории гасят кнопки; правка данных включает «Отменить», клик
// возвращает значение и гасит кнопку обратно. AutomationId — контракт
// ShellUndoButton/ShellRedoButton.
//
// ================================================================================

using System;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

[TestFixture]
public sealed class UndoRedoButtonsSmokeTests : UiSmokeFixtureBase
{
    [Test, Order(1)]
    public void Buttons_HiddenOnWelcomeScreen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(App.FindInAnyWindow("ShellUndoButton"), Is.Null,
                "На welcome-заставке кнопки «Отменить» нет (ADR-014 §1.10).");
            Assert.That(App.FindInAnyWindow("ShellRedoButton"), Is.Null,
                "На welcome-заставке кнопки «Вернуть» нет (ADR-014 §1.10).");
        });
    }

    [Test, Order(2)]
    public void Buttons_VisibleButDisabled_AfterNavigationWithoutHistory()
    {
        App.NavigateTo("Тепловой расчёт");
        App.WaitModulePlate("ТЕПЛОВОЙ");

        var undo = App.WaitForElement("ShellUndoButton")
            ?? throw new InvalidOperationException("ShellUndoButton не найден после закрытия заставки.");
        var redo = App.WaitForElement("ShellRedoButton")
            ?? throw new InvalidOperationException("ShellRedoButton не найден после закрытия заставки.");

        Assert.Multiple(() =>
        {
            Assert.That(undo.IsEnabled, Is.False, "Пустой дневник гасит «Отменить».");
            Assert.That(redo.IsEnabled, Is.False, "Пустой дневник гасит «Вернуть».");
        });
    }

    [Test, Order(3)]
    public void EditThermalInput_EnablesUndo_ClickRestoresValueAndDisables()
    {
        // Правка теплового входа — user-мутация, открывающая запись дневника.
        var supplyTemperature = App.WaitForElement("ThermalSupplyTemperature")
            ?? throw new InvalidOperationException("ThermalSupplyTemperature не найден.");
        var textBefore = supplyTemperature.AsTextBox().Text;
        supplyTemperature.AsTextBox().Text = "35,0";

        // Окно тишины (400 мс) закрывает группу — кнопка включается.
        var undoEnabled = Retry.WhileFalse(
            () => App.WaitForElement("ShellUndoButton", TimeSpan.FromSeconds(1))?.IsEnabled == true,
            TimeSpan.FromSeconds(10),
            ignoreException: true).Success;
        Assert.That(undoEnabled, Is.True, "Правка данных включает кнопку «Отменить».");

        App.WaitForElement("ShellUndoButton")!.AsButton().Invoke();

        var restored = Retry.WhileTrue(
            () => App.WaitForElement("ThermalSupplyTemperature", TimeSpan.FromSeconds(1))?.AsTextBox().Text != textBefore,
            TimeSpan.FromSeconds(10),
            ignoreException: true).Success;

        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.True,
                $"Клик по «Отменить» возвращает вход ({textBefore}), а не «{supplyTemperature.AsTextBox().Text}».");
            Assert.That(App.WaitForElement("ShellUndoButton")!.IsEnabled, Is.False,
                "После отката дневник пуст — кнопка гаснет.");
            Assert.That(App.WaitForElement("ShellRedoButton")!.IsEnabled, Is.True,
                "Отменённое действие доступно «Вернуть».");
        });

        // Возврат отменённого — история снова пуста.
        App.WaitForElement("ShellRedoButton")!.AsButton().Invoke();

        var redoDisabled = Retry.WhileFalse(
            () => App.WaitForElement("ShellRedoButton", TimeSpan.FromSeconds(1))?.IsEnabled == false,
            TimeSpan.FromSeconds(10),
            ignoreException: true).Success;

        Assert.Multiple(() =>
        {
            Assert.That(redoDisabled, Is.True, "После «Вернуть» ветка возврата исчерпана — кнопка гаснет.");
            Assert.That(
                App.WaitForElement("ThermalSupplyTemperature")!.AsTextBox().Text,
                Is.EqualTo("35,0"),
                "«Вернуть» восстанавливает правку 35,0.");
        });
    }
}
