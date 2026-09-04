using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Каноническая культура чисел приложения — русская локаль
    /// (решение владельца, 2026-09-04: десятичная запятая, тысячи через пробел).
    /// </summary>
    /// <remarks>
    /// WPF по умолчанию форматирует и парсит биндинги по en-US (Language
    /// элемента наследует xml:lang="en-US"), независимо от культуры ОС.
    /// <see cref="PinBindingCulture"/> переопределяет метаданные
    /// <see cref="FrameworkElement.LanguageProperty"/>, чтобы все биндинги
    /// (StringFormat, ConvertBack, конвертеры) работали по ru-RU.
    /// <see cref="Behaviors.TextBoxBehavior.NormalizeDecimalSeparator"/> использует
    /// ту же культуру, поэтому цепочка «ввод 35,5 → парсинг → отображение»
    /// согласована end-to-end. Пин тест-пином: RussianNumberCultureTests.
    /// </remarks>
    public static class AppCulture
    {
        /// <summary>Каноническая культура UI и биндингов.</summary>
        public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("ru-RU");

        /// <summary>То же значение в виде XmlLanguage для WPF-биндингов.</summary>
        public static XmlLanguage Language { get; } = XmlLanguage.GetLanguage(Culture.IetfLanguageTag);

        private static readonly object PinLock = new();
        private static bool _pinned;

        /// <summary>
        /// Закрепляет культуру биндингов WPF за всеми элементами.
        /// Идемпотентен: повторный вызов (приложение/тесты) не дублирует
        /// OverrideMetadata, который не допускает повторного переопределения.
        /// </summary>
        public static void PinBindingCulture()
        {
            lock (PinLock)
            {
                if (_pinned)
                {
                    return;
                }

                // FrameworkElement покрывает визуальные контролы, но StringFormat
                // в текстовых Run'ах (например, «пятидневка: -23,0 °C» на Климате)
                // живёт на FrameworkContentElement. Метаданные самого
                // FrameworkContentElement уже переопределены внутри WPF (повторный
                // OverrideMetadata бросает ArgumentException), поэтому пин ставится
                // на конкретный тип Run — единственный FCE с биндингами в вьюхах.
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(Language));
                System.Windows.Documents.Run.LanguageProperty.OverrideMetadata(
                    typeof(System.Windows.Documents.Run),
                    new FrameworkPropertyMetadata(Language));
                _pinned = true;
            }
        }
    }
}
