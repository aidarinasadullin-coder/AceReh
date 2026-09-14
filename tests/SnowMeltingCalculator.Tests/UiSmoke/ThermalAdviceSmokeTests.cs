// ================================================================================
// План 2026-09-13 thermal-advice, фаза C — UiSmoke-сценарии советов.
// ================================================================================
//
// Пины поверх реального exe (FlaUI), один запуск на фикстуру:
//  1) чистый старт → Result null → карточка «Рекомендации» Collapsed (UIA не видит);
//  2) выбор трубы клавиатурой (канал из урока п.17: Expand → Down → Enter) +
//    подача 90 °C на дефолтных климате/конструкции → ΔT = 2×(90 − T_средняя) > 30
//    при T_средняя < 45 °C → карточка рендерится с советом DELTAT_MAX
//    («уменьшите подачу»); кнопки перехода нет (решение владельца 2026-09-14).
//
// Item-советы генерируются ItemsControl в рантайме и не пиннятся
// ThermalAutomationIdSelectorContractTests (ревью P2-8) — здесь их контракт.

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
    public void CleanStart_ThermalStep_AdviceCardIsCollapsed()
    {
        App.NavigateTo("Тепловой расчёт");
        App.WaitModulePlate("ТЕПЛОВОЙ");

        Assert.That(App.FindInAnyWindow("ThermalAdviceCard"), Is.Null,
            "Без результата расчёта карточка «Рекомендации» не должна рендериться.");
    }

    [Test, Order(2)]
    public void ExcessiveSupply90_ShowsAdviceCard_WithoutGoToButton()
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

        var card = App.WaitForElement("ThermalAdviceCard", TimeSpan.FromSeconds(15));
        if (card is null)
        {
            // Диагностика: что показывают статус-бар и ΔT после расчёта
            var validation = App.ReadText("ShellValidationMessage");
            var deltaT = App.ReadText("ThermalDeltaT");
            throw new AssertionException(
                $"После расчёта с подачей 90 °C карточка «Рекомендации» не появилась. " +
                $"Статус-бар: '{validation}'; ΔT: '{deltaT}'.");
        }

        Assert.That(card.Name, Does.Contain("Рекомендации"),
            "Заголовок карточки советов — «Рекомендации».");

        // Контракт текстов: совет перепада с лечением «уменьшите подачу».
        // Кнопки перехода в v1 нет (решение владельца 2026-09-14: советы v1
        // ведут на текущий шаг) — каркас проверки текстов остаётся пином.
        var texts = App.Window.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
            .Select(e => e.Name)
            .ToArray();
        Assert.That(texts, Has.Some.Contains("Перепад"),
            "Совет DELTAT_MAX не найден в карточке.");
        Assert.That(texts, Has.Some.Contains("уменьшите подачу"),
            "Совет перепада должен предлагать уменьшить подачу (ревью P1-3).");
        Assert.That(App.FindInAnyWindow("ThermalAdviceGoToButton"), Is.Null,
            "Кнопки перехода в v1 нет — совет не должен рендерить кнопку.");
    }
}
