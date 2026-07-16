using System.Windows;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Тестовый шов для вызова MessageBox.
    /// </summary>
    public interface IDialogService
    {
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}
