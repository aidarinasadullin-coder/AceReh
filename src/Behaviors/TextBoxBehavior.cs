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
                    textBox.GotKeyboardFocus += OnGotKeyboardFocus;
                    textBox.Loaded += OnLoaded;
                }
                else
                {
                    textBox.GotFocus -= OnGotFocus;
                    textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
                    textBox.Loaded -= OnLoaded;
                }
            }
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SelectAllText(textBox);
            }
        }

        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SelectAllText(textBox);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // При загрузке TextBox в DataGrid, если он уже в фокусе, выделяем текст
                if (textBox.IsFocused || textBox.IsKeyboardFocused)
                {
                    SelectAllText(textBox);
                }
            }
        }

        private static void SelectAllText(TextBox textBox)
        {
            // Используем Dispatcher для отложенного вызова SelectAll
            // Это необходимо для DataGrid, где TextBox создаётся динамически
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Проверяем, что TextBox всё ещё в фокусе
                if (textBox.IsKeyboardFocused && textBox.Text.Length > 0)
                {
                    textBox.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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

            // Разделитель закреплённой культуры биндингов (AppCulture), а не
            // CurrentCulture — иначе на машине с en-US ввод «35,5» разобьётся.
            var decimalSeparator = Core.AppCulture.Culture.NumberFormat.NumberDecimalSeparator;

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
                // Та же закреплённая культура, что и у биндинга (см. AppCulture)
                var decimalSeparator = Core.AppCulture.Culture.NumberFormat.NumberDecimalSeparator;

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

        #region CaretOnFocus Property

        /// <summary>
        /// При значении true позиционирует курсор в конец текста при получении фокуса.
        /// В отличие от SelectAllOnFocus, не выделяет текст.
        /// </summary>
        public static readonly DependencyProperty CaretOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "CaretOnFocus",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnCaretOnFocusChanged));

        public static bool GetCaretOnFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(CaretOnFocusProperty);
        }

        public static void SetCaretOnFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(CaretOnFocusProperty, value);
        }

        private static void OnCaretOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += OnGotFocusForCaret;
                    textBox.GotKeyboardFocus += OnGotKeyboardFocusForCaret;
                }
                else
                {
                    textBox.GotFocus -= OnGotFocusForCaret;
                    textBox.GotKeyboardFocus -= OnGotKeyboardFocusForCaret;
                }
            }
        }

        private static void OnGotFocusForCaret(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Позиционируем курсор в конец текста
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private static void OnGotKeyboardFocusForCaret(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Позиционируем курсор в конец текста
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        #endregion

        #region ClearSelectionOnFirstInput Property

        /// <summary>
        /// При значении true снимает выделение текста после первого ввода символа.
        /// Используется для DataGrid, где при входе в редактирование текст выделяется автоматически.
        /// </summary>
        public static readonly DependencyProperty ClearSelectionOnFirstInputProperty =
            DependencyProperty.RegisterAttached(
                "ClearSelectionOnFirstInput",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnClearSelectionOnFirstInputChanged));

        public static bool GetClearSelectionOnFirstInput(DependencyObject obj)
        {
            return (bool)obj.GetValue(ClearSelectionOnFirstInputProperty);
        }

        public static void SetClearSelectionOnFirstInput(DependencyObject obj, bool value)
        {
            obj.SetValue(ClearSelectionOnFirstInputProperty, value);
        }

        // Флаг для отслеживания первого ввода
        private static readonly System.Collections.Generic.Dictionary<TextBox, bool> _isFirstInput = new();

        private static void OnClearSelectionOnFirstInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += OnGotFocusForFirstInput;
                    textBox.LostFocus += OnLostFocusForFirstInput;
                    textBox.TextChanged += OnTextChangedForFirstInput;
                }
                else
                {
                    textBox.GotFocus -= OnGotFocusForFirstInput;
                    textBox.LostFocus -= OnLostFocusForFirstInput;
                    textBox.TextChanged -= OnTextChangedForFirstInput;
                }
            }
        }

        private static void OnGotFocusForFirstInput(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Сбрасываем флаг при получении фокуса
                _isFirstInput[textBox] = true;
            }
        }

        private static void OnLostFocusForFirstInput(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Удаляем флаг при потере фокуса
                _isFirstInput.Remove(textBox);
            }
        }

        private static void OnTextChangedForFirstInput(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Проверяем, это первый ввод после получения фокуса?
                if (_isFirstInput.TryGetValue(textBox, out var isFirst) && isFirst)
                {
                    // После первого ввода символа при выделенном тексте WPF автоматически заменяет текст.
                    // Нам нужно снять выделение (если оно осталось) и поставить курсор в конец.
                    textBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Снимаем выделение и ставим курсор в конец текста
                        textBox.CaretIndex = textBox.Text.Length;
                        textBox.SelectionLength = 0;
                    }), System.Windows.Threading.DispatcherPriority.Background);

                    // Сбрасываем флаг - последующие изменения не будут обрабатываться
                    _isFirstInput[textBox] = false;
                }
            }
        }

        #endregion
    }
}