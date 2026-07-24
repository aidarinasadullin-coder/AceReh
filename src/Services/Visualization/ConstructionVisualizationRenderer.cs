using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Visualization
{
    /// <summary>
    /// Режим обработки переполнения высоты визуализации
    /// </summary>
    public enum ScaleOverflowMode
    {
        /// <summary>
        /// Без ограничения — поведение как раньше (неограниченная высота canvas)
        /// </summary>
        None,

        /// <summary>
        /// Сжимать нижние слои при превышении MaxVisualizationHeight
        /// </summary>
        CompressLowerLayers
    }

    /// <summary>
    /// Параметры визуализации конструкции
    /// </summary>
    public class ConstructionVisualizationParameters
    {
        /// <summary>
        /// Слои над трубой (от поверхности к трубе)
        /// </summary>
        public IEnumerable<Layer> LayersAbovePipe { get; set; } = Enumerable.Empty<Layer>();

        /// <summary>
        /// Слои под трубой (от трубы к грунту)
        /// </summary>
        public IEnumerable<Layer> LayersBelowPipe { get; set; } = Enumerable.Empty<Layer>();

        /// <summary>
        /// Шаг укладки труб, мм
        /// </summary>
        public int PipeSpacing { get; set; } = 200;

        /// <summary>
        /// Компактный режим (меньшие размеры и шрифты)
        /// </summary>
        public bool CompactMode { get; set; }

        /// <summary>
        /// Рисовать размерную линию шага трубы
        /// </summary>
        public bool ShowDimensionLine { get; set; } = true;

        /// <summary>
        /// Рисовать подпись "Поверхность"
        /// </summary>
        public bool ShowSurfaceLabel { get; set; } = true;

        /// <summary>
        /// Рисовать подпись "Грунт"
        /// </summary>
        public bool ShowGroundLabel { get; set; } = true;

        /// <summary>
        /// Фиксированный масштаб. Если null — масштаб вычисляется автоматически по высоте.
        /// </summary>
        public double? FixedScaleFactor { get; set; }

        /// <summary>
        /// Доступная высота для автомасштабирования. Используется, если FixedScaleFactor не задан.
        /// </summary>
        public double? CanvasAvailableHeight { get; set; }

        /// <summary>
        /// Максимальная высота визуализации в пикселях. Если задана и OverflowMode = CompressLowerLayers,
        /// нижние слои сжимаются чтобы уложиться в лимит. Верхние слои и зона трубы не сжимаются.
        /// </summary>
        public double? MaxVisualizationHeight { get; set; }

        /// <summary>
        /// Режим обработки переполнения высоты. По умолчанию None — поведение как раньше.
        /// </summary>
        public ScaleOverflowMode OverflowMode { get; set; } = ScaleOverflowMode.None;
    }

    /// <summary>
    /// Универсальный рендерер визуализации конструкции (схема "пирога")
    /// </summary>
    /// <remarks>
    /// Физическая модель: граница между слоями "над трубой" и "под трубой"
    /// проходит через оси труб. Труба рисуется центрированной относительно этой границы.
    /// </remarks>
    public class ConstructionVisualizationRenderer
    {
        private readonly MaterialTextureProvider _textureProvider = new();

        // === Размеры для обычного режима ===
        private const double NormalPipeRadius = 12.0;
        private const double NormalPipeWallThickness = 3.0;
        private const double NormalMinLayerHeight = 8.0;
        private const double NormalCanvasMargin = 20.0;
        private const double NormalMinWidthForLabels = 250.0;
        private const double NormalMinWidthForTwoPipes = 200.0;
        private const double NormalMaxScale = 0.5;
        private const double NormalLabelsHeight = 40.0;

        // === Размеры для компактного режима ===
        private const double CompactPipeRadius = 8.0;
        private const double CompactPipeWallThickness = 2.0;
        private const double CompactMinLayerHeight = 6.0;
        private const double CompactCanvasMargin = 10.0;
        private const double CompactMinWidthForLabels = 200.0;
        private const double CompactMinWidthForTwoPipes = 150.0;
        private const double CompactMaxScale = 0.25;
        private const double CompactLabelsHeight = 30.0;

        private double PipeRadius(bool compact) => compact ? CompactPipeRadius : NormalPipeRadius;
        private double PipeWallThickness(bool compact) => compact ? CompactPipeWallThickness : NormalPipeWallThickness;
        private double MinLayerHeight(bool compact) => compact ? CompactMinLayerHeight : NormalMinLayerHeight;
        private double CanvasMargin(bool compact) => compact ? CompactCanvasMargin : NormalCanvasMargin;
        private double MinWidthForLabels(bool compact) => compact ? CompactMinWidthForLabels : NormalMinWidthForLabels;
        private double MinWidthForTwoPipes(bool compact) => compact ? CompactMinWidthForTwoPipes : NormalMinWidthForTwoPipes;
        private double MaxScale(bool compact) => compact ? CompactMaxScale : NormalMaxScale;
        private double LabelsHeight(bool compact) => compact ? CompactLabelsHeight : NormalLabelsHeight;

        /// <summary>
        /// Информация о распределении высоты для одного слоя при bounded-режиме
        /// </summary>
        private struct LayerRenderInfo
        {
            public Layer Layer;
            public double RenderedHeight;
            public double Thickness;
            public bool IsCompressed;
        }

        /// <summary>
        /// Отрисовать конструкцию на Canvas
        /// </summary>
        /// <param name="canvas">Целевой Canvas</param>
        /// <param name="parameters">Параметры визуализации</param>
        /// <param name="canvasWidth">Ширина canvas. Если null, используется ActualWidth.</param>
        public void Render(Canvas canvas, ConstructionVisualizationParameters parameters, double? canvasWidth = null)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            canvas.Children.Clear();

            var width = canvasWidth ?? canvas.ActualWidth;
            if (width <= 0)
                return;

            var compact = parameters.CompactMode;
            var minLayerHeight = MinLayerHeight(compact);
            var margin = CanvasMargin(compact);
            var labelsHeight = LabelsHeight(compact);

            var layersAbove = parameters.LayersAbovePipe.OrderByDescending(l => l.Order).ToList();
            var layersBelow = parameters.LayersBelowPipe.OrderBy(l => l.Order).ToList();

            // Defensive: reject non-finite or non-positive FixedScaleFactor — treat as null (no-op)
            var fixedScale = parameters.FixedScaleFactor;
            if (fixedScale.HasValue &&
                (double.IsNaN(fixedScale.Value) || double.IsInfinity(fixedScale.Value) || fixedScale.Value <= 0))
            {
                fixedScale = null;
            }

            // Preferred base scale — FixedScaleFactor is only a starting point;
            // bounded height policy can still override it (task 4).
            var baseScale = fixedScale
                ?? CalculateScaleFactor(layersAbove, layersBelow, parameters.CanvasAvailableHeight, margin, labelsHeight, compact);

            // Defensive: reject non-finite or non-positive MaxVisualizationHeight — treat as null (no-op)
            var maxVisHeight = parameters.MaxVisualizationHeight;
            if (maxVisHeight.HasValue &&
                (double.IsNaN(maxVisHeight.Value) || double.IsInfinity(maxVisHeight.Value) || maxVisHeight.Value <= 0))
            {
                maxVisHeight = null;
            }

            // Determine if bounded mode is active
            var boundedMode = maxVisHeight is not null
                              && parameters.OverflowMode == ScaleOverflowMode.CompressLowerLayers;

            // Allocate layer heights
            List<LayerRenderInfo> aboveInfos;
            List<LayerRenderInfo> belowInfos;
            double totalAbove;
            double totalBelow;
            bool anyCompressed;

            if (maxVisHeight is not null && boundedMode)
            {
                AllocateBoundedHeights(layersAbove, layersBelow, baseScale, minLayerHeight,
                    margin, labelsHeight, maxVisHeight.Value,
                    out aboveInfos, out belowInfos, out totalAbove, out totalBelow, out anyCompressed);
            }
            else
            {
                aboveInfos = layersAbove.Select(l => new LayerRenderInfo
                {
                    Layer = l,
                    RenderedHeight = Math.Max(l.Thickness * baseScale, minLayerHeight),
                    Thickness = l.Thickness,
                    IsCompressed = false
                }).ToList();
                belowInfos = layersBelow.Select(l => new LayerRenderInfo
                {
                    Layer = l,
                    RenderedHeight = Math.Max(l.Thickness * baseScale, minLayerHeight),
                    Thickness = l.Thickness,
                    IsCompressed = false
                }).ToList();
                totalAbove = aboveInfos.Sum(i => i.RenderedHeight);
                totalBelow = belowInfos.Sum(i => i.RenderedHeight);
                anyCompressed = false;
            }

            var totalHeight = totalAbove + totalBelow + 2 * margin + labelsHeight;

            canvas.Height = totalHeight;
            if (canvasWidth.HasValue)
            {
                canvas.Width = width;
            }

            var centerX = width / 2;

            // === Система координат ===
            // y = 0 на оси труб (граница между "над трубой" и "под трубой")
            // y > 0 - над трубой (вверх от оси)
            // y < 0 - под трубой (вниз от оси)
            // В Canvas: yCanvas = totalAbove + margin - y

            var baseY = totalAbove + margin; // Позиция y=0 (ось труб) в координатах Canvas

            // === Фаза 1: Слои над трубой (всегда нормальный масштаб) ===
            var currentY = baseY;
            foreach (var info in aboveInfos)
            {
                DrawLayer(canvas, width, currentY - info.RenderedHeight, info.RenderedHeight,
                    info.Layer.Material, info.Layer.Thickness, compact);
                currentY -= info.RenderedHeight;
            }

            // Подпись "Поверхность"
            if (parameters.ShowSurfaceLabel && width >= MinWidthForLabels(compact) && !compact)
            {
                var surfaceLabel = new TextBlock
                {
                    Text = "← Поверхность",
                    FontSize = 10,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(surfaceLabel, width - 100);
                Canvas.SetTop(surfaceLabel, currentY - 5);
                canvas.Children.Add(surfaceLabel);
            }

            // === Фаза 2: Слои под трубой (возможно сжатие нижних) ===
            currentY = baseY;
            var firstCompressedY = -1.0;
            foreach (var info in belowInfos)
            {
                if (info.IsCompressed && firstCompressedY < 0)
                {
                    firstCompressedY = currentY;
                }
                DrawLayer(canvas, width, currentY, info.RenderedHeight,
                    info.Layer.Material, info.Layer.Thickness, compact);
                currentY += info.RenderedHeight;
            }

            // Маркер сжатия "не в масштабе"
            if (anyCompressed && firstCompressedY >= 0)
            {
                DrawCompressionMarker(canvas, width, firstCompressedY, compact);
            }

            // Подпись "Грунт"
            if (parameters.ShowGroundLabel && width >= MinWidthForLabels(compact) && !compact)
            {
                var groundLabel = new TextBlock
                {
                    Text = "← Грунт",
                    FontSize = 10,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(groundLabel, width - 70);
                Canvas.SetTop(groundLabel, currentY + 5);
                canvas.Children.Add(groundLabel);
            }

            // === Фаза 3: Трубы и размерная линия (нормальный масштаб) ===
            DrawPipesAndDimension(canvas, width, centerX, baseY, parameters.PipeSpacing, baseScale, compact, parameters.ShowDimensionLine);
        }

        private double CalculateScaleFactor(
            List<Layer> layersAbove,
            List<Layer> layersBelow,
            double? availableHeight,
            double margin,
            double labelsHeight,
            bool compact)
        {
            var maxScale = MaxScale(compact);

            if (availableHeight is not > 0)
                return maxScale;

            var desiredAbove = layersAbove.Sum(l => l.Thickness * maxScale);
            var desiredBelow = layersBelow.Sum(l => l.Thickness * maxScale);
            var desiredHeight = desiredAbove + desiredBelow + 2 * margin + labelsHeight;

            if (desiredHeight <= availableHeight.Value)
                return maxScale;

            return maxScale * (availableHeight.Value / desiredHeight);
        }

        /// <summary>
        /// Детерминированное распределение высот слоёв при ограниченной высоте canvas.
        /// Верхние слои — всегда нормальный масштаб. Нижние слои — нормальный масштаб
        /// пока хватает бюджета, затем сжатие до MinLayerHeight.
        /// </summary>
        private void AllocateBoundedHeights(
            List<Layer> layersAbove, List<Layer> layersBelow,
            double baseScale, double minLayerHeight,
            double margin, double labelsHeight, double maxHeight,
            out List<LayerRenderInfo> aboveInfos, out List<LayerRenderInfo> belowInfos,
            out double totalAbove, out double totalBelow, out bool anyCompressed)
        {
            // Верхние слои — всегда нормальный масштаб
            aboveInfos = layersAbove.Select(l => new LayerRenderInfo
            {
                Layer = l,
                RenderedHeight = Math.Max(l.Thickness * baseScale, minLayerHeight),
                Thickness = l.Thickness,
                IsCompressed = false
            }).ToList();
            totalAbove = aboveInfos.Sum(i => i.RenderedHeight);

            // Нормальные высоты нижних слоёв
            var normalBelow = layersBelow.Select(l => Math.Max(l.Thickness * baseScale, minLayerHeight)).ToList();
            var normalTotalBelow = normalBelow.Sum();
            var normalTotalHeight = totalAbove + normalTotalBelow + 2 * margin + labelsHeight;

            if (normalTotalHeight <= maxHeight)
            {
                // Сжатие не требуется
                belowInfos = layersBelow.Select((l, i) => new LayerRenderInfo
                {
                    Layer = l,
                    RenderedHeight = normalBelow[i],
                    Thickness = l.Thickness,
                    IsCompressed = false
                }).ToList();
                totalBelow = normalTotalBelow;
                anyCompressed = false;
                return;
            }

            // Сжатие требуется — найти точку раздела k:
            // слои 0..k-1 нормальный масштаб, слои k..n-1 сжатые (MinLayerHeight)
            var compressedHeight = minLayerHeight;
            var lowerBudget = maxHeight - totalAbove - 2 * margin - labelsHeight;
            var n = normalBelow.Count;

            // Найти наибольшее k, где sum(normalBelow[0..k-1]) + (n-k)*compressedHeight <= lowerBudget
            int bestK = 0;
            double prefixSum = 0;
            for (int k = 0; k <= n; k++)
            {
                var total = prefixSum + (n - k) * compressedHeight;
                if (total <= lowerBudget)
                    bestK = k;
                if (k < n)
                    prefixSum += normalBelow[k];
            }

            // Построить belowInfos: первые bestK слоёв нормальные, остальные сжатые
            belowInfos = new List<LayerRenderInfo>();
            for (int i = 0; i < n; i++)
            {
                var isCompressed = i >= bestK;
                belowInfos.Add(new LayerRenderInfo
                {
                    Layer = layersBelow[i],
                    RenderedHeight = isCompressed ? compressedHeight : normalBelow[i],
                    Thickness = layersBelow[i].Thickness,
                    IsCompressed = isCompressed
                });
            }
            totalBelow = belowInfos.Sum(i => i.RenderedHeight);
            anyCompressed = bestK < n;
        }

        /// <summary>
        /// Нарисовать маркер "не в масштабе" на границе первого сжатого нижнего слоя
        /// </summary>
        private void DrawCompressionMarker(Canvas canvas, double canvasWidth, double y, bool compact)
        {
            var margin = CanvasMargin(compact);
            var fontSize = compact ? 7 : 9;

            // Пунктирная линия разрыва на границе сжатия
            var breakLine = new Line
            {
                X1 = margin,
                Y1 = y,
                X2 = canvasWidth - margin,
                Y2 = y,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 3, 2 }
            };
            canvas.Children.Add(breakLine);

            // Подпись "не в масштабе"
            var markerText = new TextBlock
            {
                Text = "не в масштабе",
                FontSize = fontSize,
                Foreground = Brushes.DarkRed,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Padding = new Thickness(3, 1, 3, 1)
            };

            Canvas.SetLeft(markerText, margin + 2);
            Canvas.SetTop(markerText, y + 1);
            canvas.Children.Add(markerText);
        }

        private void DrawLayer(Canvas canvas, double canvasWidth, double y, double layerHeight,
            Material? material, double thickness, bool compact)
        {
            var texture = material != null
                ? _textureProvider.GetTexture(material.Id)
                : _textureProvider.GetTexture(5); // По умолчанию бетон

            var margin = CanvasMargin(compact);
            var rectWidth = Math.Max(canvasWidth - 2 * margin, 1);

            // Основной слой с текстурой
            var rect = new Rectangle
            {
                Width = rectWidth,
                Height = layerHeight,
                Fill = texture,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 1
            };

            Canvas.SetLeft(rect, margin);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);

            // Эффект 2.5D - блик сверху
            var highlightHeight = Math.Min(compact ? 6.0 : 8.0, layerHeight / 3);
            var topGrad = new LinearGradientBrush();
            topGrad.GradientStops.Add(new GradientStop(Color.FromArgb(50, 255, 255, 255), 0.0));
            topGrad.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.0));

            var highlightRect = new Rectangle
            {
                Width = rectWidth,
                Height = highlightHeight,
                Fill = topGrad
            };

            Canvas.SetLeft(highlightRect, margin);
            Canvas.SetTop(highlightRect, y);
            canvas.Children.Add(highlightRect);

            // Тень снизу
            var shadowHeight = Math.Min(compact ? 6.0 : 8.0, layerHeight / 3);
            var botGrad = new LinearGradientBrush();
            botGrad.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0));
            botGrad.GradientStops.Add(new GradientStop(Color.FromArgb(30, 0, 0, 0), 1.0));

            var shadowRect = new Rectangle
            {
                Width = rectWidth,
                Height = shadowHeight,
                Fill = botGrad
            };

            Canvas.SetLeft(shadowRect, margin);
            Canvas.SetTop(shadowRect, y + layerHeight - shadowHeight);
            canvas.Children.Add(shadowRect);

            // Подпись слоя
            if (canvasWidth >= MinWidthForLabels(compact))
            {
                DrawLayerLabel(canvas, canvasWidth, y, layerHeight, material, thickness, compact);
            }
        }

        private void DrawLayerLabel(Canvas canvas, double canvasWidth, double y, double layerHeight,
            Material? material, double thickness, bool compact)
        {
            var margin = CanvasMargin(compact);
            double fontSize = compact ? 8 : 9;

            var formattedText = new FormattedText(
                $"{material?.Name ?? "Не указан"}\n{thickness:F0} мм",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                fontSize,
                Brushes.White,
                GetDpiScale(canvas));

            double textWidth = formattedText.Width + (compact ? 12 : 16);
            double textHeight = formattedText.Height + (compact ? 4 : 4);

            // Suppress label if the layer is too short to host it without overlap.
            // This happens for compressed layers (MinLayerHeight) where the two-line
            // label would be taller than the layer itself.
            const double labelPadding = 2.0;
            if (layerHeight < textHeight + labelPadding)
                return;

            double textY = y + layerHeight / 2 - textHeight / 2;

            var labelBackground = new Rectangle
            {
                Width = textWidth,
                Height = textHeight,
                Fill = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                RadiusX = 3,
                RadiusY = 3
            };

            Canvas.SetLeft(labelBackground, margin + (compact ? 4 : 5));
            Canvas.SetTop(labelBackground, textY);
            canvas.Children.Add(labelBackground);

            var label = new TextBlock
            {
                Text = $"{material?.Name ?? "Не указан"}\n{thickness:F0} мм",
                FontSize = fontSize,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(label, margin + (compact ? 10 : 13));
            Canvas.SetTop(label, textY + 2);
            canvas.Children.Add(label);
        }

        private void DrawPipesAndDimension(Canvas canvas, double canvasWidth, double centerX, double baseY,
            int pipeSpacing, double scaleFactor, bool compact, bool showDimensionLine)
        {
            var pipeRadius = PipeRadius(compact);
            var margin = CanvasMargin(compact);

            // Ось труб совпадает с baseY
            var pipeCenterY = baseY;

            var pipeSpacingPixels = pipeSpacing * scaleFactor;

            // Ограничиваем расстояние между трубами шириной canvas
            var maxSpacing = (canvasWidth - 2 * margin - (compact ? 30 : 40)) / 2;
            if (pipeSpacingPixels > maxSpacing)
            {
                pipeSpacingPixels = maxSpacing;
            }

            // При маленьком окне - только одна труба по центру
            if (canvasWidth < MinWidthForTwoPipes(compact))
            {
                DrawPipe(canvas, centerX, pipeCenterY, pipeRadius, compact, Brushes.DodgerBlue);
                return;
            }

            // Две трубы
            var bluePipeX = centerX - pipeSpacingPixels / 2;
            var redPipeX = centerX + pipeSpacingPixels / 2;

            DrawPipe(canvas, bluePipeX, pipeCenterY, pipeRadius, compact, Brushes.DodgerBlue);
            DrawPipe(canvas, redPipeX, pipeCenterY, pipeRadius, compact, Brushes.OrangeRed);

            // === Размерная линия между осями труб ===
            if (showDimensionLine && pipeSpacing > 0)
            {
                var dimLineY = pipeCenterY + pipeRadius + (compact ? 14 : 20);
                var tickSize = compact ? 2 : 3;
                var fontSize = compact ? 8 : 10;

                // Выносные линии от центров труб
                AddLine(canvas, bluePipeX, pipeCenterY, bluePipeX, dimLineY, Brushes.Black, 1);
                AddLine(canvas, redPipeX, pipeCenterY, redPipeX, dimLineY, Brushes.Black, 1);

                // Размерная линия
                AddLine(canvas, bluePipeX, dimLineY, redPipeX, dimLineY, Brushes.Black, 1);

                // Засечки
                AddLine(canvas, bluePipeX - tickSize, dimLineY - tickSize, bluePipeX, dimLineY, Brushes.Black, 1);
                AddLine(canvas, bluePipeX - tickSize, dimLineY + tickSize, bluePipeX, dimLineY, Brushes.Black, 1);
                AddLine(canvas, redPipeX + tickSize, dimLineY - tickSize, redPipeX, dimLineY, Brushes.Black, 1);
                AddLine(canvas, redPipeX + tickSize, dimLineY + tickSize, redPipeX, dimLineY, Brushes.Black, 1);

                // Текст размера
                var dimText = new TextBlock
                {
                    Text = pipeSpacing.ToString(),
                    FontSize = fontSize,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                    Padding = new Thickness(2, 0, 2, 0)
                };

                Canvas.SetLeft(dimText, centerX - (compact ? 8 : 10));
                Canvas.SetTop(dimText, dimLineY - (compact ? 6 : 8));
                canvas.Children.Add(dimText);
            }
        }

        private void DrawPipe(Canvas canvas, double x, double y, double pipeRadius, bool compact, Brush color)
        {
            var wallThickness = PipeWallThickness(compact);

            // Внешний круг (стенка трубы)
            var outerCircle = new Ellipse
            {
                Width = pipeRadius * 2,
                Height = pipeRadius * 2,
                Fill = color,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 1
            };

            Canvas.SetLeft(outerCircle, x - pipeRadius);
            Canvas.SetTop(outerCircle, y - pipeRadius);
            canvas.Children.Add(outerCircle);

            // Внутренний круг (полость трубы)
            var innerRadius = Math.Max(pipeRadius - wallThickness, 1);
            var innerCircle = new Ellipse
            {
                Width = innerRadius * 2,
                Height = innerRadius * 2,
                Fill = Brushes.White,
                Stroke = null
            };

            Canvas.SetLeft(innerCircle, x - innerRadius);
            Canvas.SetTop(innerCircle, y - innerRadius);
            canvas.Children.Add(innerCircle);
        }

        private static double GetDpiScale(Visual visual)
        {
            try
            {
                var source = PresentationSource.FromVisual(visual);
                if (source?.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch
            {
                // Игнорируем ошибки DPI для визуалов вне дерева
            }

            return 1.0;
        }

        private static void AddLine(Canvas canvas, double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness
            };
            canvas.Children.Add(line);
        }
    }
}
