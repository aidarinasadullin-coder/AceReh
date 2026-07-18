using System.Windows;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Тестовый шов для вызова диалоговых окон.
    /// </summary>
    public interface IDialogService
    {
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);

        /// <summary>
        /// Показать диалог сохранения файла.
        /// </summary>
        /// <param name="defaultFileName">Имя файла по умолчанию.</param>
        /// <param name="filter">Фильтр файлов.</param>
        /// <returns>Путь к выбранному файлу или null, если диалог отменён.</returns>
        string? ShowSaveFileDialog(string defaultFileName, string filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*");

        /// <summary>
        /// Показать диалог открытия файла.
        /// </summary>
        /// <param name="filter">Фильтр файлов.</param>
        /// <returns>Путь к выбранному файлу или null, если диалог отменён.</returns>
        string? ShowOpenFileDialog(string filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*");

        /// <summary>
        /// Показать модальное окно с сообщением об ошибке.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        /// <param name="title">Заголовок окна.</param>
        void ShowError(string message, string title);
    }
}
