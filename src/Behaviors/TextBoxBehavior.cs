using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnowMeltingCalculator.Behaviors
{
    /// <summary>
    /// Attached behavior для улучшения UX ввода чисел в TextBox.
    /// </summary>
    /// <remarks>
    /// Функционал:
    /// 1. Выделение всего текста при получении фокуса
    /// 2. Выделение всего текста при клике (если ещё не в фокусе)
    /// 3. Обработка Escape для возврата исходного значения
    /// 4. Обработка точки и запятой как десятичного разделителя
    /// </remarks>
    public static class TextBoxBehavior
    {
        #region SelectAllOnFocus Property

        /// <summary>
        /// При значении true выделяет весь текст при получении фокуса.
        /// </summary>
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        public static bool GetSelectAllOnFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(SelectAllOnFocusProperty);
        }

        public static void SetSelectAllOnFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectAllOnFocusProperty, value);
        }

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += OnGotFocus;
                    textBox.PreviewMouseDown += OnPreviewMouseDown;
                }
                else
                {
                    textBox.GotFocus -= OnGotFocus;
                    textBox.PreviewMouseDown -= OnPreviewMouseDown;
                }
            }
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Используем Dispatcher для отложенного вызова SelectAll
                // Это необходимо для DataGrid, где TextBox создаётся динамически
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsFocused)
            {
                textBox.Focus();
                textBox.SelectAll();
                e.Handled = true;
            }
        }

        #endregion

        #region RestoreOnEscape Property

        /// <summary>
        /// При значении true восстанавливает исходное значение при нажатии Escape.
        /// </summary>
        public static readonly DependencyProperty RestoreOnEscapeProperty =
            DependencyProperty.RegisterAttached(
                "RestoreOnEscape",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnRestoreOnEscapeChanged));

        public static bool GetRestoreOnEscape(DependencyObject obj)
        {
            return (bool)obj.GetValue(RestoreOnEscapeProperty);
        }

        public static void SetRestoreOnEscape(DependencyObject obj, bool value)
        {
            obj.SetValue(RestoreOnEscapeProperty, value);
        }

        private static void OnRestoreOnEscapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += OnGotFocusForRestore;
                    textBox.LostFocus += OnLostFocusForRestore;
                    textBox.PreviewKeyDown += OnPreviewKeyDownForRestore;
                }
                else
                {
                    textBox.GotFocus -= OnGotFocusForRestore;
                    textBox.LostFocus -= OnLostFocusForRestore;
                    textBox.PreviewKeyDown -= OnPreviewKeyDownForRestore;
                }
            }
        }

        // Используем WeakReference для хранения исходного значения, чтобы избежать утечек памяти
        private static readonly System.Collections.Generic.Dictionary<TextBox, string> _originalValues = new();

        private static void OnGotFocusForRestore(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Сохраняем исходное значение при получении фокуса
                _originalValues[textBox] = textBox.Text;
            }
        }

        private static void OnLostFocusForRestore(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Удаляем сохранённое значение при потере фокуса
                _originalValues.Remove(textBox);
            }
        }

        private static void OnPreviewKeyDownForRestore(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && sender is TextBox textBox)
            {
                // Восстанавливаем исходное значение
                if (_originalValues.TryGetValue(textBox, out var originalValue))
                {
                    textBox.Text = originalValue;
                    textBox.SelectAll();
                }
                e.Handled = true;
                // Перемещаем фокус на следующий элемент
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        #endregion

        #region NormalizeDecimalSeparator Property

        /// <summary>
        /// При значении true заменяет точку и запятую на системный десятичный разделитель.
        /// </summary>
        public static readonly DependencyProperty NormalizeDecimalSeparatorProperty =
            DependencyProperty.RegisterAttached(
                "NormalizeDecimalSeparator",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnNormalizeDecimalSeparatorChanged));

        public static bool GetNormalizeDecimalSeparator(DependencyObject obj)
        {
            return (bool)obj.GetValue(NormalizeDecimalSeparatorProperty);
        }

        public static void SetNormalizeDecimalSeparator(DependencyObject obj, bool value)
        {
            obj.SetValue(NormalizeDecimalSeparatorProperty, value);
        }

        private static void OnNormalizeDecimalSeparatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += OnPreviewTextInputForDecimal;
                    DataObject.AddPastingHandler(textBox, OnPastingForDecimal);
                }
                else
                {
                    textBox.PreviewTextInput -= OnPreviewTextInputForDecimal;
                    DataObject.RemovePastingHandler(textBox, OnPastingForDecimal);
                }
            }
        }

        private static void OnPreviewTextInputForDecimal(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
                return;

            // Получаем системный десятичный разделитель
            var decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            
            // Если введена точка или запятая
            if (e.Text == "." || e.Text == ",")
            {
                if (sender is TextBox textBox)
                {
                    // Заменяем на системный разделитель
                    e.Handled = true;
                    
                    // Проверяем, нет ли уже разделителя в тексте
                    if (!textBox.Text.Contains(decimalSeparator))
                    {
                        // Вставляем системный разделитель в текущую позицию курсора
                        var caretIndex = textBox.CaretIndex;
                        textBox.Text = textBox.Text.Insert(caretIndex, decimalSeparator);
                        textBox.CaretIndex = caretIndex + 1;
                    }
                }
            }
        }

        private static void OnPastingForDecimal(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                var decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                
                // Заменяем все точки и запятые на системный разделитель
                var normalizedText = text.Replace(".", decimalSeparator).Replace(",", decimalSeparator);
                
                if (text != normalizedText)
                {
                    var dataObject = new DataObject();
                    dataObject.SetData(DataFormats.Text, normalizedText);
                    e.DataObject = dataObject;
                }
            }
        }

        #endregion
    }
}