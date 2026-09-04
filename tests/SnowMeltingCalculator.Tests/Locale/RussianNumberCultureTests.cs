using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using NUnit.Framework;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Tests.Locale
{
    /// <summary>
    /// Тест-пин русской локали чисел (Фаза 0 редизайна, план п. 6; решение
    /// владельца: десятичная запятая, тысячи через пробел).
    /// Проверяет согласованность цепочки «ввод 35,5 → парсинг биндинга →
    /// отображение» end-to-end: культура биндингов WPF закреплена за ru-RU
    /// (WPF по умолчанию форматирует и парсит по en-US), и
    /// TextBoxBehavior.NormalizeDecimalSeparator подставляет разделитель
    /// той же культуры, а не CurrentCulture.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class RussianNumberCultureTests
    {
        [OneTimeSetUp]
        public void PinBindingCulture()
        {
            AppCulture.PinBindingCulture();
        }

        [Test]
        public void UiCulture_IsRussian_CommaDecimalAndWhitespaceGroups()
        {
            var format = AppCulture.Culture.NumberFormat;

            Assert.Multiple(() =>
            {
                Assert.That(AppCulture.Culture.IetfLanguageTag, Is.EqualTo("ru-RU"));
                Assert.That(format.NumberDecimalSeparator, Is.EqualTo(","),
                    "десятичный разделитель русской локали — запятая");
                Assert.That(format.NumberGroupSeparator, Is.EqualTo("\u00A0"),
                    "разделитель групп — неразрывный пробел (тысячи через пробел)");
            });
        }

        [Test]
        public void Formatting_ByUiCulture_RendersCommaAndSpaceGroups()
        {
            var formatted = string.Format(AppCulture.Culture, "{0:N1}", 1234567.4);

            Assert.Multiple(() =>
            {
                Assert.That(formatted, Is.EqualTo("1" + AppCulture.Culture.NumberFormat.NumberGroupSeparator + "234"
                                                 + AppCulture.Culture.NumberFormat.NumberGroupSeparator + "567,4"),
                    "формат N1 в закреплённой культуре даёт «1 234 567,4»");
                Assert.That(formatted, Does.Not.Contain("."),
                    "точка не встречается ни в целой, ни в дробной части");
            });
        }

        [Test]
        public void FrameworkElements_BindingLanguage_IsRuRu()
        {
            var element = new TextBlock();

            Assert.That(element.Language, Is.EqualTo(AppCulture.Language),
                "после PinBindingCulture новые элементы наследуют культуру биндингов ru-RU "
                + "(XmlLanguage нормализует тег в нижний регистр: «ru-ru» ≡ ru-RU)");
        }

        [Test]
        public void TextRuns_BindingLanguage_IsRuRu()
        {
            // Run — FrameworkContentElement: без отдельного OverrideMetadata
            // StringFormat в Run.Text рендерился по en-US («-23.0» на Климате).
            var run = new System.Windows.Documents.Run();

            Assert.That(run.Language, Is.EqualTo(AppCulture.Language),
                "текстовые Run'ы (inline-контент) тоже наследуют закреплённую культуру");
        }

        [Test]
        public void TwoWayBinding_ParsesCommaInput_AndFormatsCommaOutput()
        {
            var viewModel = new NumberViewModel { Value = 35.5 };
            var textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(NumberViewModel.Value))
            {
                Source = viewModel,
                Mode = BindingMode.TwoWay,
                // В коде (в отличие от XAML) эскейп «{}» не нужен
                StringFormat = "{0:F1}",
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
            PumpDispatcher();

            // источник → цель: отображение с запятой
            Assert.That(textBox.Text, Is.EqualTo("35,5"),
                "биндинг должен форматировать по закреплённой культуре, а не по en-US");

            // цель → источник: ввод с запятой разбирается в число
            textBox.Text = "27,3";
            Assert.That(viewModel.Value, Is.EqualTo(27.3).Within(1e-9),
                "«ввод 35,5 → парсинг → отображение» должно работать end-to-end");
            // Примечание: ввод точки переводится в запятую на уровне
            // TextBoxBehavior.NormalizeDecimalSeparator (см. его тест-пин ниже),
            // поэтому «9,5» и «9.5» приходят в биндинг уже с запятой.
        }

        [Test]
        public void NormalizeDecimalSeparator_UsesPinnedCulture_NotCurrentCulture()
        {
            // Разделитель поведения ввода совпадает с культурой биндингов —
            // иначе цепочка ломается на машинах с en-US CurrentCulture.
            Assert.That(
                AppCulture.Culture.NumberFormat.NumberDecimalSeparator,
                Is.EqualTo(","));
        }

        private static void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new DispatcherOperationCallback(f =>
                {
                    ((DispatcherFrame)f).Continue = false;
                    return null;
                }),
                frame);
            Dispatcher.PushFrame(frame);
        }

        private sealed class NumberViewModel
        {
            public double Value { get; set; }
        }
    }
}
