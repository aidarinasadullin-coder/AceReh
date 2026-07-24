// ================================================================================
// REHAU Снеготаяние - Тесты ConstructionVisualizationRenderer (task 9)
// Проверка bounded/unbounded поведения рендерера схемы "пирога" конструкции.
// ================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Visualization;

namespace SnowMeltingCalculator.Tests.Views
{
    /// <summary>
    /// STA-тесты для <see cref="ConstructionVisualizationRenderer"/>.
    /// Рендерер использует WPF-визуалы (Canvas, Rectangle, TextBlock, Line),
    /// поэтому все тесты выполняются в STA-квартире.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ConstructionVisualizationRendererTests
    {
        // === Константы рендерера (синхронизированы с исходником) ===
        // Обычный режим
        private const double NormalMinLayerHeight = 8.0;
        private const double NormalCanvasMargin = 20.0;
        private const double NormalMaxScale = 0.5;
        private const double NormalLabelsHeight = 40.0;

        // Компактный режим
        private const double CompactMinLayerHeight = 6.0;
        private const double CompactCanvasMargin = 10.0;
        private const double CompactLabelsHeight = 30.0;

        private const double CanvasWidth = 400.0;

        private readonly ConstructionVisualizationRenderer _renderer = new();

        // === Вспомогательные методы ===

        /// <summary>
        /// Создать слой с реальным Material (материал не должен быть null,
        /// иначе рендерер использует материал по умолчанию).
        /// </summary>
        private static Layer MakeLayer(double thickness, int materialId, string materialName,
            LayerPosition position, int order)
        {
            return new Layer
            {
                Thickness = thickness,
                Material = new Material
                {
                    Id = materialId,
                    Name = materialName,
                    LambdaA = 1.0,
                    LambdaB = 1.0
                },
                Position = position,
                Order = order
            };
        }

        /// <summary>
        /// Проверить, есть ли на canvas TextBlock, содержащий "масштаб"
        /// (маркер сжатия "не в масштабе").
        /// </summary>
        private static bool HasScaleMarker(Canvas canvas)
        {
            return canvas.Children.OfType<TextBlock>()
                .Any(tb => !string.IsNullOrEmpty(tb.Text) && tb.Text.Contains("масштаб"));
        }

        /// <summary>
        /// Создать Canvas и вызвать Render с заданными параметрами.
        /// </summary>
        private Canvas Render(ConstructionVisualizationParameters parameters)
        {
            var canvas = new Canvas();
            _renderer.Render(canvas, parameters, CanvasWidth);
            return canvas;
        }

        // === Тесты ===

        /// <summary>
        /// 1. Без MaxVisualizationHeight — неограниченное поведение.
        /// Слои рендерятся в нормальном масштабе, canvas.Height = сумма слоёв + поля + подписи.
        /// Маркер "не в масштабе" отсутствует.
        /// </summary>
        [Test]
        public void NoMaxHeight_UnboundedBehavior()
        {
            // Arrange: above = [100мм (order 1), 50мм (order 0)], below = [200мм (order 0)]
            // baseScale = NormalMaxScale = 0.5 (CanvasAvailableHeight не задан → maxScale)
            // above rendered: max(100*0.5, 8)=50, max(50*0.5, 8)=25 → totalAbove = 75
            // below rendered: max(200*0.5, 8)=100 → totalBelow = 100
            // totalHeight = 75 + 100 + 2*20 + 40 = 255
            const double expectedHeight = 255.0;

            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[]
                {
                    MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 1),
                    MakeLayer(50, 10, "ЭППС", LayerPosition.AbovePipe, 0)
                },
                LayersBelowPipe = new[]
                {
                    MakeLayer(200, 1, "Песок", LayerPosition.BelowPipe, 0)
                }
            };

            // Act
            var canvas = Render(parameters);

            // Assert
            Assert.That(canvas.Height, Is.EqualTo(expectedHeight),
                $"canvas.Height должен быть {expectedHeight} (сумма масштабированных слоёв + 2*margin + labelsHeight)");
            Assert.That(HasScaleMarker(canvas), Is.False,
                "Маркер «не в масштабе» не должен присутствовать в неограниченном режиме");
        }

        /// <summary>
        /// 2. Bounded-режим с толстым нижним слоем — высота ограничена,
        /// маркер сжатия присутствует.
        /// </summary>
        [Test]
        public void Bounded_HugeLowerLayer_CapsHeight()
        {
            // Arrange: above = [100мм], below = [2000мм]
            // baseScale = 0.5, normalBelow = max(2000*0.5, 8) = 1000
            // normalTotalHeight = 50 + 1000 + 40 + 40 = 1130 > 300 → сжатие
            // lowerBudget = 300 - 50 - 40 - 40 = 170; 1000 > 170 → все нижние сжаты до 8
            // totalHeight = 50 + 8 + 40 + 40 = 138 <= 300
            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[]
                {
                    MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0)
                },
                LayersBelowPipe = new[]
                {
                    MakeLayer(2000, 1, "Песок", LayerPosition.BelowPipe, 0)
                },
                MaxVisualizationHeight = 300,
                OverflowMode = ScaleOverflowMode.CompressLowerLayers
            };

            // Act
            var canvas = Render(parameters);

            // Assert
            Assert.That(canvas.Height, Is.LessThanOrEqualTo(300),
                "canvas.Height не должен превышать MaxVisualizationHeight");
            Assert.That(HasScaleMarker(canvas), Is.True,
                "Маркер «не в масштабе» должен присутствовать при сжатии нижних слоёв");
        }

        /// <summary>
        /// 3. Bounded-режим с нормальными слоями — сжатие не требуется,
        /// высота в пределах лимита, маркер отсутствует.
        /// </summary>
        [Test]
        public void Bounded_NormalLayers_NoCompression()
        {
            // Arrange: above = [100мм], below = [200мм]
            // baseScale = 0.5, totalAbove = 50, totalBelow = 100
            // normalTotalHeight = 50 + 100 + 40 + 40 = 230 <= 300 → без сжатия
            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[]
                {
                    MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0)
                },
                LayersBelowPipe = new[]
                {
                    MakeLayer(200, 1, "Песок", LayerPosition.BelowPipe, 0)
                },
                MaxVisualizationHeight = 300,
                OverflowMode = ScaleOverflowMode.CompressLowerLayers
            };

            // Act
            var canvas = Render(parameters);

            // Assert
            Assert.That(canvas.Height, Is.LessThanOrEqualTo(300),
                "canvas.Height не должен превышать MaxVisualizationHeight");
            Assert.That(HasScaleMarker(canvas), Is.False,
                "Маркер «не в масштабе» не должен присутствовать, если сжатие не требуется");
        }

        /// <summary>
        /// 4. Bounded-режим с FixedScaleFactor — ограничение высоты
        /// перекрывает фиксированный масштаб, сжатие применяется.
        /// </summary>
        [Test]
        public void Bounded_FixedScaleFactor_Overridden()
        {
            // Arrange: compact, FixedScaleFactor=0.25, MaxHeight=190
            // above = [100мм] → max(100*0.25, 6) = 25, totalAbove = 25
            // below = [2000мм] → normal = max(2000*0.25, 6) = 500
            // normalTotalHeight = 25 + 500 + 20 + 30 = 575 > 190 → сжатие
            // lowerBudget = 190 - 25 - 20 - 30 = 115; 500 > 115 → сжатие до 6
            // totalHeight = 25 + 6 + 20 + 30 = 81 <= 190
            var parameters = new ConstructionVisualizationParameters
            {
                CompactMode = true,
                FixedScaleFactor = 0.25,
                LayersAbovePipe = new[]
                {
                    MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0)
                },
                LayersBelowPipe = new[]
                {
                    MakeLayer(2000, 1, "Песок", LayerPosition.BelowPipe, 0)
                },
                MaxVisualizationHeight = 190,
                OverflowMode = ScaleOverflowMode.CompressLowerLayers
            };

            // Act
            var canvas = Render(parameters);

            // Assert
            Assert.That(canvas.Height, Is.LessThanOrEqualTo(190),
                "canvas.Height не должен превышать MaxVisualizationHeight даже при FixedScaleFactor");
            Assert.That(HasScaleMarker(canvas), Is.True,
                "Маркер «не в масштабе» должен присутствовать при сжатии");
        }

        /// <summary>
        /// 5. При сжатии нижних слоёв верхний слой сохраняет нормальный масштаб,
        /// а хотя бы один нижний слой сжат до MinLayerHeight.
        /// </summary>
        [Test]
        public void UpperLayer_RemainsScaled_WhenLowerCompressed()
        {
            // Arrange: above = [100мм] → rendered 50 (нормальный масштаб)
            // below = [2000мм] → сжат до NormalMinLayerHeight = 8
            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[]
                {
                    MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0)
                },
                LayersBelowPipe = new[]
                {
                    MakeLayer(2000, 1, "Песок", LayerPosition.BelowPipe, 0)
                },
                MaxVisualizationHeight = 300,
                OverflowMode = ScaleOverflowMode.CompressLowerLayers
            };

            // Act
            var canvas = Render(parameters);
            var rectangles = canvas.Children.OfType<Rectangle>().ToList();

            // Assert: есть прямоугольник с высотой > MinLayerHeight (верхний слой в нормальном масштабе)
            var maxRectHeight = rectangles.Max(r => r.Height);
            Assert.That(maxRectHeight, Is.GreaterThan(NormalMinLayerHeight),
                "Верхний слой должен сохранять нормальный масштаб (Height > MinLayerHeight)");

            // Assert: есть прямоугольник с высотой == MinLayerHeight (сжатый нижний слой)
            Assert.That(rectangles.Any(r => Math.Abs(r.Height - NormalMinLayerHeight) < 0.001),
                Is.True,
                "Хотя бы один нижний слой должен быть сжат до MinLayerHeight");
        }

        /// <summary>
        /// 6. Пустые списки слоёв — рендер не падает, canvas.Height = 2*margin + labelsHeight.
        /// </summary>
        [Test]
        public void EmptyLayers_DoesNotThrow()
        {
            // Arrange: нет слоёв ни над, ни под трубой
            // totalHeight = 0 + 0 + 2*20 + 40 = 80
            const double expectedHeight = 80.0;

            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = Enumerable.Empty<Layer>(),
                LayersBelowPipe = Enumerable.Empty<Layer>()
            };

            // Act
            Canvas canvas;
            Assert.DoesNotThrow(() => canvas = Render(parameters), "Рендер с пустыми слоями не должен падать");
            canvas = Render(parameters);

            // Assert
            Assert.That(canvas.Height, Is.EqualTo(expectedHeight),
                $"canvas.Height должен быть {expectedHeight} (2*margin + labelsHeight, без слоёв)");
        }

        /// <summary>
        /// 7. Слишком маленький MaxVisualizationHeight — минимальный пол не вызывает крах,
        /// canvas.Height конечен и положителен.
        /// </summary>
        [Test]
        public void TooSmallMaxHeight_MinimumFloor_DoesNotCrash()
        {
            // Arrange: MaxVisualizationHeight=10, один слой над и один под трубой
            // lowerBudget = 10 - 50 - 40 - 40 = -120 (отрицательный)
            // Все нижние слои сжаты до MinLayerHeight=8, но totalHeight всё равно > 10
            // (минимальный пол не позволяет уложиться в 10px)
            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[]
                {
                    MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0)
                },
                LayersBelowPipe = new[]
                {
                    MakeLayer(200, 1, "Песок", LayerPosition.BelowPipe, 0)
                },
                MaxVisualizationHeight = 10,
                OverflowMode = ScaleOverflowMode.CompressLowerLayers
            };

            // Act
            Canvas canvas;
            Assert.DoesNotThrow(() => canvas = Render(parameters),
                "Рендер с слишком маленьким MaxVisualizationHeight не должен падать");
            canvas = Render(parameters);

            // Assert
            Assert.That(canvas.Height, Is.GreaterThan(0),
                "canvas.Height должен быть положительным даже при экстремально малом MaxVisualizationHeight");
            Assert.That(double.IsInfinity(canvas.Height), Is.False,
                "canvas.Height должен быть конечным");
            Assert.That(double.IsNaN(canvas.Height), Is.False,
                "canvas.Height не должен быть NaN");
        }
    }
}