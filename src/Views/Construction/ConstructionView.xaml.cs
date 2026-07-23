using System;
using System.Windows;
using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Логика взаимодействия для ConstructionView.xaml.
    /// Адаптивное переключение раскладки LayoutGrid между двумя колонками
    /// (2*/1*) и одной колонкой-стэком по событию SizeChanged.
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
            LayoutGrid.SizeChanged += OnLayoutGridSizeChanged;
            // Установить начальную раскладку сразу, чтобы избежать мерцания
            // дефолтных 2-col колонок из XAML до первого SizeChanged.
            ApplyLayout(LayoutGrid.ActualWidth);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            LayoutGrid.SizeChanged -= OnLayoutGridSizeChanged;
        }

        private void OnLayoutGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (Math.Abs(e.NewSize.Width - _lastWidth) < 1) return;
            _lastWidth = e.NewSize.Width;

            bool twoColumn = e.NewSize.Width >= TwoColumnThreshold;
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
            LayoutGrid.ColumnDefinitions.Clear();
            LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Перемещаем визуализацию из левого StackPanel в правую колонку Grid
            if (VisualizationCard.Parent == LeftColumnPanel)
            {
                LeftColumnPanel.Children.Remove(VisualizationCard);
                LayoutGrid.Children.Add(VisualizationCard);
            }

            Grid.SetColumn(VisualizationCard, 1);
            Grid.SetRow(VisualizationCard, 0);
            VisualizationCard.Margin = new Thickness(12, 0, 0, 0);
            VisualizationCard.VerticalAlignment = VerticalAlignment.Top;

            LeftColumnPanel.Margin = new Thickness(0, 0, 12, 0);
            LayersAboveCard.Margin = new Thickness(0, 0, 0, 12);
            LayersBelowCard.Margin = new Thickness(0, 0, 0, 12);
            ResultsCard.Margin = new Thickness(0);
        }

        private void SetStackedLayout()
        {
            LayoutGrid.ColumnDefinitions.Clear();
            LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Перемещаем визуализацию в конец левого StackPanel
            if (VisualizationCard.Parent == LayoutGrid)
            {
                LayoutGrid.Children.Remove(VisualizationCard);
                LeftColumnPanel.Children.Add(VisualizationCard);
            }

            LeftColumnPanel.Margin = new Thickness(0);
            LayersAboveCard.Margin = new Thickness(0, 0, 0, 12);
            LayersBelowCard.Margin = new Thickness(0, 0, 0, 12);
            ResultsCard.Margin = new Thickness(0, 0, 0, 12);
            VisualizationCard.Margin = new Thickness(0);
        }
    }
}