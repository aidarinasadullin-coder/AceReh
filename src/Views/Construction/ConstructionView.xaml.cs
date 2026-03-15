using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.ViewModels.Construction;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Логика взаимодействия для ConstructionView.xaml
    /// </summary>
    public partial class ConstructionView : UserControl
    {
        private ConstructionViewModel? _viewModel;
        private const double PipeRadius = 15;
        private const double ScaleFactor = 0.5; // Масштаб для визуализации

        public ConstructionView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
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

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                UnsubscribeFromViewModelEvents(_viewModel);
            }

            if (e.NewValue is ConstructionViewModel viewModel)
            {
                _viewModel = viewModel;
                SubscribeToViewModelEvents();
                DrawConstruction();
            }
        }

        private void SubscribeToViewModelEvents()
        {
            if (_viewModel == null) return;

            _viewModel.LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;
            _viewModel.LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void UnsubscribeFromViewModelEvents(ConstructionViewModel viewModel)
        {
            viewModel.LayersAbovePipe.CollectionChanged -= OnLayersCollectionChanged;
            viewModel.LayersBelowPipe.CollectionChanged -= OnLayersCollectionChanged;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
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
                Dispatcher.BeginInvoke(new Action(DrawConstruction));
            }
        }

        /// <summary>
        /// Отрисовка визуализации "Пирога" конструкции
        /// </summary>
        private void DrawConstruction()
        {
            if (_viewModel == null || ConstructionCanvas == null) return;

            // Очищаем Canvas
            ConstructionCanvas.Children.Clear();

            var canvasWidth = ConstructionCanvas.ActualWidth;
            var canvasHeight = ConstructionCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                // Canvas ещё не отрисован, отложим отрисовку
                Dispatcher.BeginInvoke(new Action(DrawConstruction), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            // Центр Canvas
            var centerX = canvasWidth / 2;

            // Масштабирование: если слоёв много, уменьшаем масштаб
            var totalThickness = _viewModel.LayersAbovePipe.Sum(l => l.Thickness) + 
                                 _viewModel.LayersBelowPipe.Sum(l => l.Thickness);
            var scaleFactor = totalThickness > 500 ? 0.3 : 0.5;

            // Рисуем слои снизу вверх (от грунта к поверхности)
            double currentY = canvasHeight - 20; // Отступ снизу

            // === Слои под трубой ===
            // Рисуем в обратном порядке: первый в списке - ближайший к трубе
            var layersBelowReversed = _viewModel.LayersBelowPipe.Reverse().ToList();
            foreach (var layer in layersBelowReversed)
            {
                var layerHeight = layer.Thickness * scaleFactor;
                if (layerHeight < 5) layerHeight = 5; // Минимальная высота для видимости

                var color = GetMaterialColor(layer.Material);
                var rect = new Rectangle
                {
                    Width = canvasWidth - 40,
                    Height = layerHeight,
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, 20);
                Canvas.SetTop(rect, currentY - layerHeight);
                ConstructionCanvas.Children.Add(rect);

                // Подпись слоя
                var label = new TextBlock
                {
                    Text = $"{layer.Material?.Name ?? "Не указан"}\n{layer.Thickness:F0} мм",
                    FontSize = 9,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(label, 25);
                Canvas.SetTop(label, currentY - layerHeight + 2);
                ConstructionCanvas.Children.Add(label);

                currentY -= layerHeight;
            }

            // === Труба внутри первого слоя над трубой ===
            // Первый слой над трубой - это стяжка, труба внутри неё
            var layersAbove = _viewModel.LayersAbovePipe.ToList();
            
            // Рисуем половину первого слоя (под трубой)
            if (layersAbove.Count > 0)
            {
                var firstLayer = layersAbove[0];
                var halfThickness = firstLayer.Thickness / 2.0;
                var layerHeight = halfThickness * scaleFactor;
                if (layerHeight < 5) layerHeight = 5;

                var color = GetMaterialColor(firstLayer.Material);
                var rect = new Rectangle
                {
                    Width = canvasWidth - 40,
                    Height = layerHeight,
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, 20);
                Canvas.SetTop(rect, currentY - layerHeight);
                ConstructionCanvas.Children.Add(rect);

                currentY -= layerHeight;
            }

            // Труба
            var pipeY = currentY - PipeRadius;
            
            var pipe = new Ellipse
            {
                Width = PipeRadius * 2,
                Height = PipeRadius * 2,
                Fill = Brushes.OrangeRed,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };

            Canvas.SetLeft(pipe, centerX - PipeRadius);
            Canvas.SetTop(pipe, pipeY);
            ConstructionCanvas.Children.Add(pipe);

            // Подпись трубы
            var pipeLabel = new TextBlock
            {
                Text = "Труба",
                FontSize = 8,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(pipeLabel, centerX - 15);
            Canvas.SetTop(pipeLabel, pipeY + PipeRadius / 2 - 5);
            ConstructionCanvas.Children.Add(pipeLabel);

            currentY = pipeY - PipeRadius;

            // Вторая половина первого слоя (над трубой)
            if (layersAbove.Count > 0)
            {
                var firstLayer = layersAbove[0];
                var halfThickness = firstLayer.Thickness / 2.0;
                var layerHeight = halfThickness * scaleFactor;
                if (layerHeight < 5) layerHeight = 5;

                var color = GetMaterialColor(firstLayer.Material);
                var rect = new Rectangle
                {
                    Width = canvasWidth - 40,
                    Height = layerHeight,
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, 20);
                Canvas.SetTop(rect, currentY - layerHeight);
                ConstructionCanvas.Children.Add(rect);

                // Подпись первого слоя (только один раз)
                var label = new TextBlock
                {
                    Text = $"{firstLayer.Material?.Name ?? "Не указан"}\n{firstLayer.Thickness:F0} мм",
                    FontSize = 9,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(label, 25);
                Canvas.SetTop(label, currentY - layerHeight + 2);
                ConstructionCanvas.Children.Add(label);

                currentY -= layerHeight;
            }

            // Остальные слои над трубой (начиная со второго)
            for (int i = 1; i < layersAbove.Count; i++)
            {
                var layer = layersAbove[i];
                var layerHeight = layer.Thickness * scaleFactor;
                if (layerHeight < 5) layerHeight = 5;

                var color = GetMaterialColor(layer.Material);
                var rect = new Rectangle
                {
                    Width = canvasWidth - 40,
                    Height = layerHeight,
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, 20);
                Canvas.SetTop(rect, currentY - layerHeight);
                ConstructionCanvas.Children.Add(rect);

                // Подпись слоя
                var label = new TextBlock
                {
                    Text = $"{layer.Material?.Name ?? "Не указан"}\n{layer.Thickness:F0} мм",
                    FontSize = 9,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(label, 25);
                Canvas.SetTop(label, currentY - layerHeight + 2);
                ConstructionCanvas.Children.Add(label);

                currentY -= layerHeight;
            }

            // Поверхность
            var surfaceLabel = new TextBlock
            {
                Text = "← Поверхность",
                FontSize = 10,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(surfaceLabel, canvasWidth - 100);
            Canvas.SetTop(surfaceLabel, currentY - 15);
            ConstructionCanvas.Children.Add(surfaceLabel);

            // Грунт
            var groundLabel = new TextBlock
            {
                Text = "← Грунт",
                FontSize = 10,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(groundLabel, canvasWidth - 70);
            Canvas.SetTop(groundLabel, canvasHeight - 35);
            ConstructionCanvas.Children.Add(groundLabel);
        }

        /// <summary>
        /// Получить цвет материала
        /// </summary>
        private Color GetMaterialColor(Material? material)
        {
            if (material == null)
                return Colors.Gray;

            return material.Category switch
            {
                MaterialCategory.Concrete => Color.FromRgb(128, 128, 128),    // Серый
                MaterialCategory.Soil => Color.FromRgb(139, 69, 19),          // Коричневый
                MaterialCategory.Insulation => Color.FromRgb(255, 215, 0),   // Жёлтый
                MaterialCategory.Coating => Color.FromRgb(50, 50, 50),       // Тёмно-серый
                MaterialCategory.Subbase => Color.FromRgb(160, 160, 160),    // Светло-серый
                MaterialCategory.Screed => Color.FromRgb(192, 192, 192),     // Серебристый
                _ => Colors.Gray
            };
        }

        /// <summary>
        /// Обработчик изменения размера Canvas
        /// </summary>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (_viewModel != null)
            {
                Dispatcher.BeginInvoke(new Action(DrawConstruction), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}