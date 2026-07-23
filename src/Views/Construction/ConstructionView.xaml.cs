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

            LayoutGrid.RowDefinitions.Clear();
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(LayersAboveCard, 0);
            Grid.SetRow(LayersAboveCard, 0);
            LayersAboveCard.Margin = new Thickness(0, 0, 12, 0);
            LayersAboveCard.VerticalAlignment = VerticalAlignment.Top;

            Grid.SetColumn(VisualizationCard, 1);
            Grid.SetRow(VisualizationCard, 0);
            VisualizationCard.Margin = new Thickness(12, 0, 0, 0);

            Grid.SetColumn(LayersBelowCard, 0);
            Grid.SetRow(LayersBelowCard, 1);
            LayersBelowCard.Margin = new Thickness(0, 12, 12, 0);

            Grid.SetColumn(ResultsCard, 0);
            Grid.SetRow(ResultsCard, 2);
            ResultsCard.Margin = new Thickness(0, 12, 12, 0);
        }

        private void SetStackedLayout()
        {
            LayoutGrid.ColumnDefinitions.Clear();
            LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            LayoutGrid.RowDefinitions.Clear();
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(LayersAboveCard, 0);
            Grid.SetRow(LayersAboveCard, 0);
            LayersAboveCard.Margin = new Thickness(0, 0, 0, 12);
            LayersAboveCard.VerticalAlignment = VerticalAlignment.Top;

            Grid.SetColumn(LayersBelowCard, 0);
            Grid.SetRow(LayersBelowCard, 1);
            LayersBelowCard.Margin = new Thickness(0, 0, 0, 12);

            Grid.SetColumn(ResultsCard, 0);
            Grid.SetRow(ResultsCard, 2);
            ResultsCard.Margin = new Thickness(0, 0, 0, 12);

            Grid.SetColumn(VisualizationCard, 0);
            Grid.SetRow(VisualizationCard, 3);
            VisualizationCard.Margin = new Thickness(0);
        }
    }
}