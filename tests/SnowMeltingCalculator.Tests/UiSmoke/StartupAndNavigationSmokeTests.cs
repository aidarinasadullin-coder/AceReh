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
        ("Гидравлика", "ГИДРАВЛИКА"),
        ("Результаты", "РЕЗУЛЬТАТЫ")
    };

    private static readonly (string Step, string AnchorAutomationId)[] ModuleAnchors =
    {
        ("Тепловой расчёт", "ThermalMode"),
        ("Гидравлика", "HydraulicsGlycolType"),
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

    [Test, Order(3)]
    public void CitySelection_CompletesInDropdown_PopupDoesNotReopen()
    {
        // Решение владельца (журнал п.5, обратная связь приёмки Ф7): выбор
        // города завершается в дропдауне — после выбора список не возвращается
        // ни debounce-хвостом, ни повторным фокусом в поле.
        App.NavigateTo("Климат");
        App.WaitModulePlate("КЛИМАТ");

        var cityField = App.WaitForElement("ClimateCitySearch")
            ?? throw new AssertionException("Поле города (ClimateCitySearch) не найдено.");
        var cityEdit = cityField.FindFirstDescendant(
                cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit))
            ?? throw new AssertionException("TextBox города внутри ClimateCitySearch не найден.");

        cityEdit.Patterns.Value.Pattern.SetValue("Сургут");

        var popupList = FlaUI.Core.Tools.Retry.WhileNull(
                () => App.FindInAnyWindow("CitySuggestionsList"),
                System.TimeSpan.FromSeconds(5),
                ignoreException: true).Result;
        Assert.That(popupList, Is.Not.Null,
            "После ввода названия города должен открыться список подсказок.");

        var item = popupList!.FindFirstDescendant(
                cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem))
            ?? throw new AssertionException("Список подсказок города пуст.");
        item.Click(); // реальный клик мышью — канал выбора контрола (MouseLeftButtonUp)

        // Выбор состоялся: debounce-окно, которое раньше переоткрывало popup.
        System.Threading.Thread.Sleep(600);
        Assert.That(App.FindInAnyWindow("CitySuggestionsList"), Is.Null,
            "После выбора города список подсказок не должен переоткрываться.");

        // Повторный фокус в поле — тоже не возвращает список.
        cityEdit.Focus();
        System.Threading.Thread.Sleep(1200);
        Assert.That(App.FindInAnyWindow("CitySuggestionsList"), Is.Null,
            "Возврат фокуса в поле с выбранным городом не должен переоткрывать список.");
    }

    [Test, Order(4)]
    public void HeaderCalculate_FromForeignStep_ShowsThermalValidationInsteadOfSilentNoOp()
    {
        // Решение владельца (2026-09-06, «молчаливую ошибку надо поправить»):
        // шапочная «Рассчитать» считает тепловое; если тепловые входы невалидны,
        // сообщение валидации не попадало в статус-бар другого шага (no-op).
        // Теперь шелл переводит на Тепловой шаг, где ошибка видна.
        // Внимание (ревью): пин опирается на то, что инвалидация без результата
        // — NoChange и фаза теплового остаётся Default (роутинг статус-бара
        // при NeedsRecalculation показал бы RecalcMessage вместо валидации).
        App.NavigateTo("Климат");
        App.WaitModulePlate("КЛИМАТ");

        var button = App.WaitForElement("ThermalCalculate")
            ?? throw new AssertionException("Шапочная кнопка «Рассчитать» (ThermalCalculate) не найдена.");
        button.Patterns.Invoke.Pattern.Invoke();

        // Свежий старт: тип трубы не выбран → валидация теплового модуля,
        // шелл ведёт на Тепловой шаг, где она отображается в статус-баре.
        App.WaitModulePlate("ТЕПЛОВОЙ");

        var message = App.ReadText("ShellValidationMessage");
        Assert.That(message, Does.Contain("Тип трубы не задан"),
            "После шапочного «Рассчитать» с невалидным тепловым входом статус-бар должен показать ошибку.");
    }
}
