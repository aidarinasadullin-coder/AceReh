using System;
using System.Windows;
using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Логика взаимодействия для ConstructionView.xaml.
    /// Адаптивная раскладка «Пира конструкции» (Фаза 4): две колонки
    /// (таблицы | схема) на широкой области, схема под таблицами на узкой.
    /// </summary>
    public partial class ConstructionView : UserControl
    {
        private bool _isUpdating;
        private double _lastWidth;
        private const double TwoColumnThreshold = 880.0;

        public ConstructionView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PieLayoutGrid.SizeChanged += OnPieLayoutGridSizeChanged;
            // Установить начальную раскладку сразу, чтобы избежать мерцания
            // дефолтных 2-col колонок из XAML до первого SizeChanged.
            ApplyLayout(PieLayoutGrid.ActualWidth);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            PieLayoutGrid.SizeChanged -= OnPieLayoutGridSizeChanged;
        }

        private void OnPieLayoutGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (Math.Abs(e.NewSize.Width - _lastWidth) < 1) return;
            _lastWidth = e.NewSize.Width;

            ApplyLayout(e.NewSize.Width);
        }

        private void ApplyLayout(double width)
        {
            _lastWidth = width;
            bool twoColumn = width >= TwoColumnThreshold;
            _isUpdating = true;
            try
            {
                if (twoColumn) SetTwoColumnLayout();
                else SetStackedLayout();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void SetTwoColumnLayout()
        {
            PieLayoutGrid.ColumnDefinitions.Clear();
            PieLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            PieLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(SchemaPanel, 1);
            Grid.SetRow(SchemaPanel, 0);
            SchemaPanel.Margin = new Thickness(16, 0, 0, 0);
            SchemaPanel.MinWidth = 280;
            TablesPanel.Margin = new Thickness(0);
        }

        private void SetStackedLayout()
        {
            PieLayoutGrid.ColumnDefinitions.Clear();
            PieLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Схема под таблицами на узкой области
            Grid.SetColumn(SchemaPanel, 0);
            Grid.SetRow(SchemaPanel, 1);
            SchemaPanel.Margin = new Thickness(0, 16, 0, 0);
            SchemaPanel.MinWidth = 0;
            TablesPanel.Margin = new Thickness(0);
        }

        /// <summary>
        /// Меню «⋯» — управление справочниками (Фаза 4, п.2).
        /// </summary>
        private void TemplatesMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu is { } menu)
            {
                menu.PlacementTarget = button;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }
    }
}
