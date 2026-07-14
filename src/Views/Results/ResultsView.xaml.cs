using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Views.Results
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private ConstructionViewModel? _constructionViewModel;
        private ICalculationStateService? _calculationStateService;
        private bool _isSubscribed;
        private bool _isPipeSpacingSubscribed;
        private bool _isDrawing;
        private readonly ConstructionVisualizationRenderer _renderer = new();

        public ResultsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ResultsViewModel viewModel)
            {
                _constructionViewModel = viewModel.ConstructionViewModel;
                DrawConstruction();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_constructionViewModel != null)
            {
                UnsubscribeFromViewModelEvents();
                _constructionViewModel = null;
            }

            if (_calculationStateService != null)
            {
                UnsubscribeFromPipeSpacingEvents();
                _calculationStateService = null;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (_constructionViewModel != null)
            {
                UnsubscribeFromViewModelEvents();
                _constructionViewModel = null;
            }

            if (_calculationStateService != null)
            {
                UnsubscribeFromPipeSpacingEvents();
                _calculationStateService = null;
            }

            // Get new ViewModel from ResultsViewModel
            if (e.NewValue is ResultsViewModel viewModel)
            {
                _constructionViewModel = viewModel.ConstructionViewModel;
                _calculationStateService = viewModel.CalculationStateService;
                SubscribeToViewModelEvents();
                SubscribeToPipeSpacingEvents();
                DrawConstruction();
            }
        }

        private void SubscribeToViewModelEvents()
        {
            if (_constructionViewModel == null || _isSubscribed) return;

            _constructionViewModel.LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;
            _constructionViewModel.LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;
            _constructionViewModel.PropertyChanged += OnViewModelPropertyChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeFromViewModelEvents()
        {
            if (_constructionViewModel == null || !_isSubscribed) return;

            _constructionViewModel.LayersAbovePipe.CollectionChanged -= OnLayersCollectionChanged;
            _constructionViewModel.LayersBelowPipe.CollectionChanged -= OnLayersCollectionChanged;
            _constructionViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _isSubscribed = false;
        }

        private void SubscribeToPipeSpacingEvents()
        {
            if (_calculationStateService == null || _isPipeSpacingSubscribed) return;

            _calculationStateService.PipeSpacingChanged += OnPipeSpacingChanged;
            _isPipeSpacingSubscribed = true;
        }

        private void UnsubscribeFromPipeSpacingEvents()
        {
            if (_calculationStateService == null || !_isPipeSpacingSubscribed) return;

            _calculationStateService.PipeSpacingChanged -= OnPipeSpacingChanged;
            _isPipeSpacingSubscribed = false;
        }

        private void OnPipeSpacingChanged(object? sender, int e)
        {
            DrawConstruction();
        }

        private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(DrawConstruction));
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConstructionViewModel.R1Total) ||
                e.PropertyName == nameof(ConstructionViewModel.R2Total) ||
                e.PropertyName == nameof(ConstructionViewModel.LambdaE) ||
                e.PropertyName == nameof(ConstructionViewModel.GroundwaterLevel))
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
            if (_constructionViewModel == null || ConstructionCanvas == null) return;

            _isDrawing = true;
            try
            {
                var parameters = new ConstructionVisualizationParameters
                {
                    LayersAbovePipe = _constructionViewModel.LayersAbovePipe,
                    LayersBelowPipe = _constructionViewModel.LayersBelowPipe,
                    PipeSpacing = _constructionViewModel.PipeSpacing,
                    CompactMode = true,
                    ShowDimensionLine = true,
                    FixedScaleFactor = 0.25
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
