using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Controls.Climate
{
    /// <summary>
    /// Кастомный контрол для автозаполнения при выборе города
    /// </summary>
    public partial class CityAutoCompleteBox : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Текст поиска
        /// </summary>
        public static readonly DependencyProperty SearchQueryProperty =
            DependencyProperty.Register(
                nameof(SearchQuery),
                typeof(string),
                typeof(CityAutoCompleteBox),
                new PropertyMetadata(string.Empty, OnSearchQueryChanged));

        /// <summary>
        /// Выбранный город
        /// </summary>
        public static readonly DependencyProperty SelectedCityProperty =
            DependencyProperty.Register(
                nameof(SelectedCity),
                typeof(CityInfo),
                typeof(CityAutoCompleteBox),
                new PropertyMetadata(null, OnSelectedCityChanged));

        /// <summary>
        /// Отфильтрованный список городов
        /// </summary>
        public static readonly DependencyProperty FilteredCitiesProperty =
            DependencyProperty.Register(
                nameof(FilteredCities),
                typeof(IEnumerable<CityMatchResult>),
                typeof(CityAutoCompleteBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Признак открытого popup
        /// </summary>
        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register(
                nameof(IsPopupOpen),
                typeof(bool),
                typeof(CityAutoCompleteBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Индекс выбранного предложения
        /// </summary>
        public static readonly DependencyProperty SelectedSuggestionIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedSuggestionIndex),
                typeof(int),
                typeof(CityAutoCompleteBox),
                new PropertyMetadata(-1));

        /// <summary>
        /// Текст плейсхолдера
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(CityAutoCompleteBox),
                new PropertyMetadata("Введите город..."));

        #endregion

        #region Private Fields

        private CancellationTokenSource? _debounceCts;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(300);
        private bool _isNavigating = false;
        private bool _isFocused = false;

        #endregion

        #region Events

        /// <summary>
        /// Событие выбора города
        /// </summary>
        public event EventHandler<CitySelectedEventArgs>? CitySelected;

        #endregion

        #region Constructor

        public CityAutoCompleteBox()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        #endregion

        #region Public Properties

        public string SearchQuery
        {
            get => (string)GetValue(SearchQueryProperty);
            set => SetValue(SearchQueryProperty, value);
        }

        public CityInfo? SelectedCity
        {
            get => (CityInfo?)GetValue(SelectedCityProperty);
            set => SetValue(SelectedCityProperty, value);
        }

        public IEnumerable<CityMatchResult> FilteredCities
        {
            get => (IEnumerable<CityMatchResult>)GetValue(FilteredCitiesProperty);
            set => SetValue(FilteredCitiesProperty, value);
        }

        public bool IsPopupOpen
        {
            get => (bool)GetValue(IsPopupOpenProperty);
            set => SetValue(IsPopupOpenProperty, value);
        }

        public int SelectedSuggestionIndex
        {
            get => (int)GetValue(SelectedSuggestionIndexProperty);
            set => SetValue(SelectedSuggestionIndexProperty, value);
        }

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        #endregion

        #region Property Changed Handlers

        private static void OnSearchQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CityAutoCompleteBox)d;
            control.OnSearchQueryChanged((string)e.NewValue);
        }

        private static void OnSelectedCityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CityAutoCompleteBox)d;
            control.OnSelectedCityChanged((CityInfo?)e.OldValue, (CityInfo?)e.NewValue);
        }

        private void OnSearchQueryChanged(string value)
        {
            UpdatePlaceholderVisibility();
            if (string.IsNullOrEmpty(value))
            {
                ClosePopup();
                return;
            }

            // Debounce
            DebounceSearch();
        }

        private void OnSelectedCityChanged(CityInfo? oldValue, CityInfo? newValue)
        {
            if (newValue != null && !_isNavigating)
            {
                SearchQuery = newValue.Name;
                ClosePopup();
            }
        }

        #endregion

        #region Event Handlers

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    MoveSelection(1);
                    e.Handled = true;
                    break;

                case Key.Up:
                    MoveSelection(-1);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    SelectCurrentItem();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    ClosePopup();
                    e.Handled = true;
                    break;

                case Key.Tab:
                    ClosePopup();
                    // Tab обрабатывается стандартно
                    break;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            _isFocused = true;
            UpdatePlaceholderVisibility();
            // Открыть popup при фокусе, если есть текст
            if (!string.IsNullOrEmpty(SearchQuery) && FilteredCities != null)
            {
                var collection = FilteredCities as ICollection<CityMatchResult>;
                if (collection != null && collection.Count > 0)
                {
                    OpenPopup();
                }
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            _isFocused = false;
            UpdatePlaceholderVisibility();
            // Закрыть popup при потере фокуса
            // Задержка для обработки клика по элементу списка
            Task.Delay(100).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!SuggestionsList.IsMouseOver)
                    {
                        ClosePopup();
                    }
                });
            });
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            // Фокус на TextBox при открытии popup
            SearchTextBox.Focus();
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            // Сброс индекса при закрытии
            SelectedSuggestionIndex = -1;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка изменения выбора
        }

        private void OnSuggestionClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = (ListBox)sender;
            var item = listBox.SelectedItem as CityMatchResult;
            
            if (item != null)
            {
                SelectItem(item);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Debounce поиска
        /// </summary>
        private void DebounceSearch()
        {
            // Отмена предыдущего таймера
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            var token = _debounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_debounceDelay, token);

                    if (!token.IsCancellationRequested)
                    {
                        // Выполнить поиск в UI потоке
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // Вызвать событие для ViewModel
                            OnSearchTriggered();
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // Отменено — это нормально
                }
            }, token);
        }

        /// <summary>
        /// Вызывается при срабатывании поиска после debounce
        /// </summary>
        private void OnSearchTriggered()
        {
            // Открыть popup, если есть результаты
            if (FilteredCities != null)
            {
                var collection = FilteredCities as ICollection<CityMatchResult>;
                if (collection != null && collection.Count > 0)
                {
                    OpenPopup();
                }
            }
        }

        /// <summary>
        /// Переместить выбор в списке
        /// </summary>
        private void MoveSelection(int delta)
        {
            if (FilteredCities == null) return;

            var collection = FilteredCities as ICollection<CityMatchResult>;
            if (collection == null) return;

            var count = collection.Count;
            if (count == 0) return;

            var newIndex = SelectedSuggestionIndex + delta;

            if (newIndex < 0)
            {
                newIndex = count - 1;
            }
            else if (newIndex >= count)
            {
                newIndex = 0;
            }

            SelectedSuggestionIndex = newIndex;
            SuggestionsList.ScrollIntoView(SuggestionsList.Items[newIndex]);
        }

        /// <summary>
        /// Выбрать текущий элемент
        /// </summary>
        private void SelectCurrentItem()
        {
            if (SelectedSuggestionIndex < 0) return;

            var collection = FilteredCities as IList<CityMatchResult>;
            if (collection == null || SelectedSuggestionIndex >= collection.Count) return;

            var item = collection[SelectedSuggestionIndex];
            SelectItem(item);
        }

        /// <summary>
        /// Выбрать элемент
        /// </summary>
        private void SelectItem(CityMatchResult item)
        {
            _isNavigating = true;
            
            SelectedCity = item.City;
            SearchQuery = item.City.Name;
            ClosePopup();
            
            CitySelected?.Invoke(this, new CitySelectedEventArgs { City = item.City });
            
            _isNavigating = false;
        }

        /// <summary>
        /// Открыть popup
        /// </summary>
        private void OpenPopup()
        {
            IsPopupOpen = true;
        }

        /// <summary>
        /// Закрыть popup
        /// </summary>
        private void ClosePopup()
        {
            IsPopupOpen = false;
        }

        /// <summary>
        /// Обновить видимость placeholder
        /// </summary>
        private void UpdatePlaceholderVisibility()
        {
            if (PlaceholderTextBlock != null)
            {
                // Placeholder виден только когда:
                // 1. TextBox не в фокусе
                // 2. SearchQuery пустой
                PlaceholderTextBlock.Visibility = (!_isFocused && string.IsNullOrEmpty(SearchQuery))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        #endregion
    }

    /// <summary>
    /// Аргументы события выбора города
    /// </summary>
    public class CitySelectedEventArgs : EventArgs
    {
        public CityInfo City { get; set; } = null!;
    }
}