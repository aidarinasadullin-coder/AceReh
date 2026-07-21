using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Реализация сервиса редакторских диалогов.
    /// Создаёт окна-редакторы из DI, устанавливает владельца и отображает их как модальные диалоги.
    /// </summary>
    public class EditorDialogService : IEditorDialogService
    {
        private const string MaterialEditorViewTypeName = "SnowMeltingCalculator.Views.Construction.MaterialEditorView";
        private const string TemplateEditorViewTypeName = "SnowMeltingCalculator.Views.Construction.TemplateEditorView";
        private const string MaterialEditorViewModelTypeName = "SnowMeltingCalculator.ViewModels.Construction.MaterialEditorViewModel";
        private const string TemplateEditorViewModelTypeName = "SnowMeltingCalculator.ViewModels.Construction.TemplateEditorViewModel";

        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Создать сервис редакторских диалогов.
        /// </summary>
        /// <param name="serviceProvider">Провайдер сервисов для разрешения окон.</param>
        public EditorDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Показать диалог редактора материала.
        /// </summary>
        public bool? ShowMaterialEditor()
        {
            return ShowEditor(MaterialEditorViewTypeName, MaterialEditorViewModelTypeName);
        }

        /// <summary>
        /// Показать диалог редактора шаблона.
        /// </summary>
        public bool? ShowTemplateEditor()
        {
            return ShowEditor(TemplateEditorViewTypeName, TemplateEditorViewModelTypeName);
        }

        private bool? ShowEditor(string viewTypeName, string? viewModelTypeName)
        {
            var owner = Application.Current?.MainWindow;
            if (owner == null)
                return null;

            var viewType = Type.GetType(viewTypeName);
            if (viewType == null)
                return null;

            var view = _serviceProvider.GetService(viewType) as Window;
            if (view == null)
                return null;

            if (view.DataContext == null && viewModelTypeName != null)
            {
                var viewModelType = Type.GetType(viewModelTypeName);
                if (viewModelType != null)
                {
                    var viewModel = _serviceProvider.GetService(viewModelType);
                    if (viewModel != null)
                        view.DataContext = viewModel;
                }
            }

            view.Owner = owner;
            return view.ShowDialog();
        }
    }
}
