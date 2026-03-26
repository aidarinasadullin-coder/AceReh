using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SnowMeltingCalculator.Behaviors
{
    /// <summary>
    /// Attached behavior для автоматического входа в режим редактирования при клике на ячейку DataGrid.
    /// </summary>
    /// <remarks>
    /// В стандартном WPF DataGrid для входа в режим редактирования требуется:
    /// - Одинарный клик для выделения ячейки
    /// - Ещё один клик для входа в редактирование
    /// 
    /// Это поведение позволяет войти в редактирование при первом же клике на ячейку.
    /// 
    /// Использование:
    /// behaviors:DataGridBehavior.SingleClickEdit="True"
    /// </remarks>
    public static class DataGridBehavior
    {
        #region SingleClickEdit Property

        /// <summary>
        /// При значении true включает режим редактирования при однократном клике на ячейку.
        /// </summary>
        public static readonly DependencyProperty SingleClickEditProperty =
            DependencyProperty.RegisterAttached(
                "SingleClickEdit",
                typeof(bool),
                typeof(DataGridBehavior),
                new PropertyMetadata(false, OnSingleClickEditChanged));

        public static bool GetSingleClickEdit(DependencyObject obj)
        {
            return (bool)obj.GetValue(SingleClickEditProperty);
        }

        public static void SetSingleClickEdit(DependencyObject obj, bool value)
        {
            obj.SetValue(SingleClickEditProperty, value);
        }

        private static void OnSingleClickEditChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.PreviewMouseDown += OnDataGridPreviewMouseDown;
                }
                else
                {
                    dataGrid.PreviewMouseDown -= OnDataGridPreviewMouseDown;
                }
            }
        }

        private static void OnDataGridPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
                return;

            // Обрабатываем только левую кнопку мыши
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Получаем позицию клика
            var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
            if (hitTestResult == null)
                return;

            // Находим ячейку под курсором
            var cell = FindVisualParent<DataGridCell>(hitTestResult.VisualHit);
            if (cell == null)
                return;

            // Проверяем, что ячейка не readonly
            if (cell.IsReadOnly)
                return;

            // Проверяем, что ячейка ещё не в режиме редактирования
            if (cell.IsEditing)
                return;

            // Находим строку
            var row = FindVisualParent<DataGridRow>(cell);
            if (row == null)
                return;

            // Выделяем строку
            dataGrid.SelectedItem = row.Item;

            // Фокусируем ячейку
            cell.Focus();

            // Входим в режим редактирования
            dataGrid.BeginEdit();

            // Отмечаем событие как обработанное
            e.Handled = true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Находит родительский элемент указанного типа в визуальном дереве.
        /// </summary>
        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                
                child = VisualTreeHelper.GetParent(child);
            }
            
            return null;
        }

        #endregion
    }
}