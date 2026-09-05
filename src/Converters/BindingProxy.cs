using System.Windows;

namespace SnowMeltingCalculator.Converters
{
    /// <summary>
    /// Freezable-прокси биндинга: переносит DataContext в ресурсы вьюхи для
    /// элементов вне визуального/логического дерева.
    /// </summary>
    /// <remarks>
    /// Классический паттерн для <c>DataGridColumn.Visibility</c>: колонка —
    /// голый DependencyObject, DataContext не наследует и
    /// <c>RelativeSource AncestorType</c> на ней молча не разрешается.
    /// Объявляется в ресурсах вьюхи: <c>&lt;converters:BindingProxy
    /// x:Key="VmProxy" Data="{Binding}"/&gt;</c>, далее колонка биндит
    /// <c>Visibility="{Binding Data.IsFullMode, Source={StaticResource VmProxy}, …}"</c>.
    /// Фаза 3 редизайна (двухрежимная таблица контуров), ADR-007 п.1.
    /// </remarks>
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() => new BindingProxy();

        /// <summary>Значение контекста, пробрасываемое в биндинги колонок.</summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
