using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Construction;

namespace SnowMeltingCalculator.Views.Shared
{
    /// <summary>
    /// UserControl для визуализации конструкции (схема "пирога")
    /// </summary>
    public partial class ConstructionVisualizationView : UserControl
    {
        private ConstructionViewModel? _viewModel;
        private bool _isSubscribed;
        private bool _isDrawing;
        private readonly ConstructionVisualizationRenderer _renderer = new();

        #region CompactMode Dependency Property

        public bool CompactMode
        {
            get => (bool)GetValue(CompactModeProperty);
            set => SetValue(CompactModeProperty, value);
        }

        public static readonly DependencyProperty CompactModeProperty =
            DependencyProperty.Register(
                nameof(CompactMode),
                typeof(bool),
                typeof(ConstructionVisualizationView),
                new PropertyMetadata(false, OnCompactModeChanged));

        private static void OnCompactModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ConstructionVisualizationView)d;
            view.DrawConstruction();
        }

        #endregion

        public ConstructionVisualizationView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
            SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConstructionViewModel viewModel)
            {
                _viewModel = viewModel;
                SubscribeToViewModelEvents();
                DrawConstruction();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                UnsubscribeFromViewModelEvents(_viewModel);
                _viewModel = null;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                UnsubscribeFromViewModelEvents(_viewModel);
                _viewModel = null;
            }

            if (e.NewValue is ConstructionViewModel viewModel)
            {
                _viewModel = viewModel;
                SubscribeToViewModelEvents();
                DrawConstruction();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawConstruction();
        }

        private void SubscribeToViewModelEvents()
        {
            if (_viewModel == null || _isSubscribed) return;
            _isSubscribed = true;

            _viewModel.LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;
            _viewModel.LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void UnsubscribeFromViewModelEvents(ConstructionViewModel viewModel)
        {
            if (!_isSubscribed) return;
            _isSubscribed = false;

            viewModel.LayersAbovePipe.CollectionChanged -= OnLayersCollectionChanged;
            viewModel.LayersBelowPipe.CollectionChanged -= OnLayersCollectionChanged;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DrawConstruction();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConstructionViewModel.R1Total) ||
                e.PropertyName == nameof(ConstructionViewModel.R2Total) ||
                e.PropertyName == nameof(ConstructionViewModel.LambdaE) ||
                e.PropertyName == nameof(ConstructionViewModel.GroundwaterLevel) ||
                e.PropertyName == nameof(ConstructionViewModel.PipeSpacing))
            {
                DrawConstruction();
            }
        }

        /// <summary>
        /// Отрисовка визуализации "Пирога" конструкции
        /// </summary>
        private void DrawConstruction()
        {
            if (_isDrawing) return;
            if (_viewModel == null || ConstructionCanvas == null) return;

            _isDrawing = true;
            try
            {
                var parameters = new ConstructionVisualizationParameters
                {
                    LayersAbovePipe = _viewModel.LayersAbovePipe,
                    LayersBelowPipe = _viewModel.LayersBelowPipe,
                    PipeSpacing = _viewModel.PipeSpacing,
                    CompactMode = CompactMode,
                    ShowDimensionLine = !CompactMode,
                    CanvasAvailableHeight = ActualHeight > 0 ? ActualHeight : null
                };

                _renderer.Render(ConstructionCanvas, parameters);
            }
            finally
            {
                _isDrawing = false;
            }
        }
    }
}
