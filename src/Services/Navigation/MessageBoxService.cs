using System.Windows;
using Microsoft.Win32;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Реализация диалогового сервиса на основе стандартных WPF-диалогов.
    /// </summary>
    public class MessageBoxService : IDialogService
    {
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }

        public string? ShowSaveFileDialog(string defaultFileName, string filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*")
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = "smc",
                FileName = defaultFileName,
                Title = "Сохранить проект"
            };

            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
        }

        public string? ShowOpenFileDialog(string filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*")
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                DefaultExt = "smc",
                Title = "Открыть проект"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
