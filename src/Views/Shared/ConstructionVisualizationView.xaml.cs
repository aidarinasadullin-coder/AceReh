using System;
using System.Collections.ObjectModel;
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
        private ObservableCollection<Layer>? _subscribedAbove;
        private ObservableCollection<Layer>? _subscribedBelow;
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

        public bool? ShowDimensionLine
        {
            get => (bool?)GetValue(ShowDimensionLineProperty);
            set => SetValue(ShowDimensionLineProperty, value);
        }

        public static readonly DependencyProperty ShowDimensionLineProperty =
            DependencyProperty.Register(
                nameof(ShowDimensionLine),
                typeof(bool?),
                typeof(ConstructionVisualizationView),
                new PropertyMetadata(null, OnVisualizationOverrideChanged));

        public double? FixedScaleFactor
        {
            get => (double?)GetValue(FixedScaleFactorProperty);
            set => SetValue(FixedScaleFactorProperty, value);
        }

        public static readonly DependencyProperty FixedScaleFactorProperty =
            DependencyProperty.Register(
                nameof(FixedScaleFactor),
                typeof(double?),
                typeof(ConstructionVisualizationView),
                new PropertyMetadata(null, OnVisualizationOverrideChanged));

        private static void OnVisualizationOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ConstructionVisualizationView)d;
            view.DrawConstruction();
        }

        #endregion

        #region Source Layers Dependency Properties

        /// <summary>
        /// Явная коллекция слоёв над трубой. Если задана, используется вместо коллекции ViewModel.
        /// </summary>
        public ObservableCollection<Layer>? SourceLayersAbovePipe
        {
            get => (ObservableCollection<Layer>?)GetValue(SourceLayersAbovePipeProperty);
            set => SetValue(SourceLayersAbovePipeProperty, value);
        }

        public static readonly DependencyProperty SourceLayersAbovePipeProperty =
            DependencyProperty.Register(
                nameof(SourceLayersAbovePipe),
                typeof(ObservableCollection<Layer>),
                typeof(ConstructionVisualizationView),
                new PropertyMetadata(null, OnSourceLayersChanged));

        /// <summary>
        /// Явная коллекция слоёв под трубой. Если задана, используется вместо коллекции ViewModel.
        /// </summary>
        public ObservableCollection<Layer>? SourceLayersBelowPipe
        {
            get => (ObservableCollection<Layer>?)GetValue(SourceLayersBelowPipeProperty);
            set => SetValue(SourceLayersBelowPipeProperty, value);
        }

        public static readonly DependencyProperty SourceLayersBelowPipeProperty =
            DependencyProperty.Register(
                nameof(SourceLayersBelowPipe),
                typeof(ObservableCollection<Layer>),
                typeof(ConstructionVisualizationView),
                new PropertyMetadata(null, OnSourceLayersChanged));

        private static void OnSourceLayersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ConstructionVisualizationView)d;
            view.UnsubscribeFromViewModelEvents();
            view.SubscribeToViewModelEvents();
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
            UnsubscribeFromViewModelEvents();
            _viewModel = null;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnsubscribeFromViewModelEvents();
            _viewModel = null;

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

        private ObservableCollection<Layer>? GetLayersAbovePipe() =>
            SourceLayersAbovePipe ?? _viewModel?.LayersAbovePipe;

        private ObservableCollection<Layer>? GetLayersBelowPipe() =>
            SourceLayersBelowPipe ?? _viewModel?.LayersBelowPipe;

        private void SubscribeToViewModelEvents()
        {
            if (_isSubscribed) return;

            _subscribedAbove = GetLayersAbovePipe();
            _subscribedBelow = GetLayersBelowPipe();
            if (_subscribedAbove == null || _subscribedBelow == null) return;

            _isSubscribed = true;
            _subscribedAbove.CollectionChanged += OnLayersCollectionChanged;
            _subscribedBelow.CollectionChanged += OnLayersCollectionChanged;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void UnsubscribeFromViewModelEvents()
        {
            if (!_isSubscribed) return;
            _isSubscribed = false;

            if (_subscribedAbove != null)
            {
                _subscribedAbove.CollectionChanged -= OnLayersCollectionChanged;
            }
            if (_subscribedBelow != null)
            {
                _subscribedBelow.CollectionChanged -= OnLayersCollectionChanged;
            }
            _subscribedAbove = null;
            _subscribedBelow = null;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
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
            if (ConstructionCanvas == null) return;

            var above = GetLayersAbovePipe();
            var below = GetLayersBelowPipe();
            if (above == null || below == null) return;

            _isDrawing = true;
            try
            {
                var parameters = new ConstructionVisualizationParameters
                {
                    LayersAbovePipe = above,
                    LayersBelowPipe = below,
                    PipeSpacing = _viewModel?.PipeSpacing ?? 200,
                    CompactMode = CompactMode,
                    ShowDimensionLine = ShowDimensionLine ?? !CompactMode,
                    FixedScaleFactor = FixedScaleFactor,
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
