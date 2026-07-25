using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Реализация диалогового сервиса на основе стандартных WPF-диалогов.
    /// </summary>
    /// <remarks>
    /// Единственное место приложения, где UI-нейтральные контракты диалогов
    /// (<see cref="DialogResult"/>, <see cref="DialogButtons"/>, <see cref="DialogIcon"/>)
    /// отображаются на WPF-типы MessageBox.
    /// </remarks>
    public class MessageBoxService : IDialogService
    {
        public DialogResult Show(string messageBoxText, string caption, DialogButtons button, DialogIcon icon)
        {
            var wpfResult = MessageBox.Show(
                messageBoxText,
                caption,
                ToMessageBoxButton(button),
                ToMessageBoxImage(icon));

            return ToDialogResult(wpfResult);
        }

        public string? ShowSaveFileDialog(
            string defaultFileName,
            string filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*",
            string? title = null,
            string? defaultExt = null)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName,
                Title = title ?? "Сохранить проект",
                DefaultExt = defaultExt ?? "smc"
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

        public bool ShowPrintDialog()
        {
            var printDialog = new PrintDialog();
            return printDialog.ShowDialog() == true;
        }

        private static MessageBoxButton ToMessageBoxButton(DialogButtons button) => button switch
        {
            DialogButtons.OK => MessageBoxButton.OK,
            DialogButtons.OKCancel => MessageBoxButton.OKCancel,
            DialogButtons.YesNo => MessageBoxButton.YesNo,
            DialogButtons.YesNoCancel => MessageBoxButton.YesNoCancel,
            _ => MessageBoxButton.OK
        };

        private static MessageBoxImage ToMessageBoxImage(DialogIcon icon) => icon switch
        {
            DialogIcon.None => MessageBoxImage.None,
            DialogIcon.Information => MessageBoxImage.Information,
            DialogIcon.Question => MessageBoxImage.Question,
            DialogIcon.Warning => MessageBoxImage.Warning,
            DialogIcon.Error => MessageBoxImage.Error,
            _ => MessageBoxImage.None
        };

        private static DialogResult ToDialogResult(MessageBoxResult result) => result switch
        {
            MessageBoxResult.OK => DialogResult.OK,
            MessageBoxResult.Cancel => DialogResult.Cancel,
            MessageBoxResult.Yes => DialogResult.Yes,
            MessageBoxResult.No => DialogResult.No,
            _ => DialogResult.None
        };
    }
}
