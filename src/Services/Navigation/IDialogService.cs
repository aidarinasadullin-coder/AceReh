namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Тестовый шов для вызова диалоговых окон.
    /// </summary>
    /// <remarks>
    /// Контракт UI-нейтральный: использует <see cref="DialogResult"/>,
    /// <see cref="DialogButtons"/> и <see cref="DialogIcon"/> вместо WPF-типов,
    /// чтобы слой ViewModel не зависел от System.Windows.
    /// </remarks>
    public interface IDialogService
    {
        DialogResult Show(string messageBoxText, string caption, DialogButtons button, DialogIcon icon);

        /// <summary>
        /// Показать диалог сохранения файла.
        /// </summary>
        /// <param name="defaultFileName">Имя файла по умолчанию.</param>
        /// <param name="filter">Фильтр файлов.</param>
        /// <param name="title">Заголовок диалога (null — заголовок по умолчанию).</param>
        /// <param name="defaultExt">Расширение по умолчанию (null — не задано).</param>
        /// <returns>Путь к выбранному файлу или null, если диалог отменён.</returns>
        string? ShowSaveFileDialog(
            string defaultFileName,
            string filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*",
            string? title = null,
            string? defaultExt = null);

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

        /// <summary>
        /// Показать системный диалог печати.
        /// </summary>
        /// <returns>true — пользователь подтвердил печать, false — отменил.</returns>
        bool ShowPrintDialog();
    }
}
