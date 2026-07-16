using System.Windows;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Реализация диалогового сервиса на основе стандартного MessageBox.
    /// </summary>
    public class MessageBoxService : IDialogService
    {
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }
    }
}
