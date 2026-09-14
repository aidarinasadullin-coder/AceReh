// ================================================================================
// План 2026-09-13 thermal-advice, фаза C — UiSmoke-сценарии советов.
// Редакция 2026-09-15 (решение владельца): карточки «Рекомендации» больше нет —
// советы это тёплые подписи под полями подачи (ThermalAdviceSupplyHint) и шага
// (ThermalAdviceSpacingHint); чип обратки акцентируется при < 0.
// ================================================================================
//
// Пины поверх реального exe (FlaUI), один запуск на фикстуру:
//  1) чистый старт → Result null → подписей советов нет;
//  2) выбор трубы клавиатурой (канал из урока п.17: Expand → Down → Enter) +
//    подача 90 °C на дефолтных климате/конструкции → ΔT = 2×(90 − T_средняя) > 30
//    при T_средняя < 45 °C → подписи советов видны («уменьшите подачу»),
//    карточка ThermalAdviceCard не существует.

// ================================================================================

using System;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

[Category("UiSmoke")]
[Apartment(ApartmentState.STA)]
public sealed class ThermalAdviceSmokeTests : UiSmokeFixtureBase
{
    [Test, Order(1)]
    public void CleanStart_ThermalStep_NoAdviceHintsAndNoCard()
    {
        App.NavigateTo("Тепловой расчёт");
        App.WaitModulePlate("ТЕПЛОВОЙ");

        Assert.Multiple(() =>
        {
            Assert.That(App.FindInAnyWindow("ThermalAdviceSupplyHint"), Is.Null,
                "Без результата подписи совета под подачей быть не должно.");
            Assert.That(App.FindInAnyWindow("ThermalAdviceSpacingHint"), Is.Null,
                "Без результата подписи совета под шагом быть не должно.");
            Assert.That(App.FindInAnyWindow("ThermalAdviceCard"), Is.Null,
                "Карточка «Рекомендации» удалена (решение владельца 2026-09-15).");
        });
    }

    [Test, Order(2)]
    public void ExcessiveSupply90_ShowsAdviceHints_WithoutCard()
    {
        App.NavigateTo("Тепловой расчёт");
        App.WaitModulePlate("ТЕПЛОВОЙ");

        // Тип трубы: keyboard-канал (раскрывающий список + первый элемент) —
        // SelectionItemPattern ненадёжен для WPF-биндинга (п.17).
        var pipeCombo = App.WaitForElement("ThermalPipe")?.AsComboBox()
            ?? throw new AssertionException("ComboBox «Тип трубы» не найден.");
        pipeCombo.Focus();
        pipeCombo.Expand();
        Keyboard.PressVirtualKeyCode((ushort)VirtualKeyShort.DOWN);
        Keyboard.PressVirtualKeyCode((ushort)VirtualKeyShort.ENTER);

        var supplyEdit = App.WaitForElement("ThermalSupplyTemperature")?.AsTextBox()
            ?? throw new AssertionException("Поле «Температура подачи» не найдено.");
        supplyEdit.Text = "90";

        var calculateButton = App.WaitForElement("ThermalCalculate")
            ?? throw new AssertionException("Шапочная кнопка «Рассчитать» не найдена.");
        calculateButton.Patterns.Invoke.Pattern.Invoke();

        var supplyHint = App.WaitForElement("ThermalAdviceSupplyHint", TimeSpan.FromSeconds(15));
        if (supplyHint is null)
        {
            // Диагностика: что показывают статус-бар и ΔT после расчёта
            var validation = App.ReadText("ShellValidationMessage");
            var deltaT = App.ReadText("ThermalDeltaT");
            throw new AssertionException(
                $"После расчёта с подачей 90 °C подпись совета под подачей не появилась. " +
                $"Статус-бар: '{validation}'; ΔT: '{deltaT}'.");
        }

        Assert.That(supplyHint.Name, Does.Contain("уменьшите"),
            "Подпись под подачей должна предлагать уменьшить подачу.");

        var spacingHint = App.WaitForElement("ThermalAdviceSpacingHint")
            ?? throw new AssertionException("Подпись совета под шагом не найдена.");
        Assert.That(spacingHint.Name, Does.Contain("шаг"),
            "Подпись под шагом должна предлагать увеличить шаг.");

        // Карточки быть не должно (решение владельца 2026-09-15).
        Assert.That(App.FindInAnyWindow("ThermalAdviceCard"), Is.Null,
            "Карточка «Рекомендации» удалена — не должна рендериться.");
    }
}
