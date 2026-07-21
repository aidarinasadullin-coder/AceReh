using System;
using System.Windows;
using SnowMeltingCalculator.ViewModels.Construction;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Логика взаимодействия для MaterialEditorView.xaml.
    /// Окно редактора материалов — позволяет добавлять, редактировать и удалять
    /// пользовательские материалы. Встроенные материалы защищены от изменений.
    /// </summary>
    public partial class MaterialEditorView : Window
    {
        public MaterialEditorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MaterialEditorViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
                await vm.InitializeAsync();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MaterialEditorViewModel vm)
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