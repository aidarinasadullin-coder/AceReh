using System;
using System.Windows;
using SnowMeltingCalculator.ViewModels.Construction;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Логика взаимодействия для TemplateEditorView.xaml.
    /// Окно редактора шаблонов конструкций — позволяет добавлять, редактировать
    /// и удалять пользовательские шаблоны. Встроенные шаблоны защищены от изменений.
    /// </summary>
    public partial class TemplateEditorView : Window
    {
        public TemplateEditorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TemplateEditorViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
                await vm.InitializeAsync();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TemplateEditorViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
        }

        private void OnRequestClose()
        {
            Close();
        }
    }
}
