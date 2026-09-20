// ================================================================================
// REHAU Снеготаяние - Диалог «Что нового» (план 1.3 роадмапа post-1.8)
// ================================================================================

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

using SnowMeltingCalculator.Services.Updates;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Модальный диалог обновления/«Что нового»: заголовок, подзаголовок,
    /// список изменений, опциональная кнопка «Открыть папку выдачи»
    /// (только https — гвард <see cref="UpdateChannelOptions.IsSafeFolderUrl"/>,
    /// значение приходит из сети).
    /// </summary>
    public partial class WhatsNewWindow : Window
    {
        private readonly string? _folderUrl;

        public WhatsNewWindow(
            string title,
            string? subtitle,
            IReadOnlyList<string> items,
            string? folderUrl)
        {
            InitializeComponent();

            TitleText.Text = title;
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                SubtitleText.Text = subtitle;
                SubtitleText.Visibility = Visibility.Visible;
            }

            ItemsList.ItemsSource = items;

            _folderUrl = UpdateChannelOptions.IsSafeFolderUrl(folderUrl) ? folderUrl : null;
            if (_folderUrl is not null)
            {
                FolderButton.Visibility = Visibility.Visible;
            }
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_folderUrl is null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo(_folderUrl) { UseShellExecute = true });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
