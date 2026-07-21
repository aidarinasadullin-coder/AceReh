namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Шов для отображения редакторских окон материалов и шаблонов.
    /// </summary>
    public interface IEditorDialogService
    {
        /// <summary>
        /// Показать диалог редактора материала.
        /// </summary>
        /// <returns>Результат диалога или null, если диалог не может быть показан.</returns>
        bool? ShowMaterialEditor();

        /// <summary>
        /// Показать диалог редактора шаблона.
        /// </summary>
        /// <returns>Результат диалога или null, если диалог не может быть показан.</returns>
        bool? ShowTemplateEditor();
    }
}
