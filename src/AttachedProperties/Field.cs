using System.Windows;

namespace SnowMeltingCalculator.AttachedProperties
{
    /// <summary>
    /// Юнит-adornment полей ввода (Фаза 2 редизайна): строка единицы измерения,
    /// отрисовываемая внутри поля справа (эталон tokens.css: <c>.f .in .unit</c>).
    /// Читается шаблоном Controls.TextBox.xaml через RelativeSource TemplatedParent;
    /// при пустом значении колонка юнита схлопывается. UI-only, состояние не
    /// хранит (инварианты R1–R6 не затрагиваются).
    /// </summary>
    public static class Field
    {
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.RegisterAttached(
                "Unit",
                typeof(string),
                typeof(Field),
                new PropertyMetadata(string.Empty));

        public static string GetUnit(DependencyObject obj) =>
            (string)obj.GetValue(UnitProperty);

        public static void SetUnit(DependencyObject obj, string value) =>
            obj.SetValue(UnitProperty, value);
    }
}
