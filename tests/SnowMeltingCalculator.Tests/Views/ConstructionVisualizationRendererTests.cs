// ================================================================================
// REHAU Снеготаяние - Тесты ConstructionVisualizationRenderer
// Проверка режимов рендерера схемы "пирога" конструкции:
// None (полная схема без ограничения) и FixedDepthWindow (константный бокс = окно 1 м).
// Ключевой инвариант FixedDepthWindow: высота canvas НЕ зависит от данных —
// регрессия на "пляшущую" карточку шаблона (цикл перерисовки по SizeChanged).
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
        /// Проверить, есть ли на canvas подпись линии среза.
        /// </summary>
        private static bool HasCutCaption(Canvas canvas)
        {
            return canvas.Children.OfType<TextBlock>()
                .Any(tb => !string.IsNullOrEmpty(tb.Text) && tb.Text.Contains("срез"));
        }

        /// <summary>
        /// Нижняя кромка прямоугольника (Canvas.Top + Height).
        /// </summary>
        private static double BottomOf(Rectangle rectangle)
        {
            return Canvas.GetTop(rectangle) + rectangle.Height;
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

        // === Режим None (неограниченный) ===

        /// <summary>
        /// Без MaxVisualizationHeight — неограниченное поведение.
        /// Слои рендерятся в нормальном масштабе, canvas.Height = сумма слоёв + поля + подписи.
        /// Маркер "не в масштабе" отсутствует.
        /// </summary>
        [Test]
        public void NoMaxHeight_UnboundedBehavior()
        {
            // Arrange: above = [100мм (order 1), 50мм (order 0)], below = [200мм (order 0)]
            // baseScale = NormalMaxScale = 0.5 (FixedScaleFactor не задан → maxScale)
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
        /// Пустые списки слоёв (режим None) — рендер не падает,
        /// canvas.Height = 2*margin + labelsHeight.
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

        // === Режим FixedDepthWindow (константный бокс = окно 1 м) ===

        /// <summary>
        /// Ключевой инвариант режима: canvas.Height ровно MaxVisualizationHeight
        /// для любых данных (тонкий пирог, толстый пирог, с FixedScaleFactor).
        /// Регрессия на цикл перерисовки "пляшущей" карточки шаблона.
        /// </summary>
        [Test]
        public void FixedDepth_HeightIsConstant_RegardlessOfData()
        {
            // Arrange: три принципиально разных набора данных
            var thickPie = new ConstructionVisualizationParameters
            {
                CompactMode = true,
                LayersAbovePipe = new[] { MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0) },
                LayersBelowPipe = new[]
                {
                    MakeLayer(10, 5, "Бетон", LayerPosition.BelowPipe, 0),
                    MakeLayer(80, 10, "ЭППС", LayerPosition.BelowPipe, 1),
                    MakeLayer(1000, 2, "Грунт", LayerPosition.BelowPipe, 2),
                    MakeLayer(570, 2, "Грунт", LayerPosition.BelowPipe, 3)
                },
                MaxVisualizationHeight = 180,
                OverflowMode = ScaleOverflowMode.FixedDepthWindow
            };

            var shallowPie = new ConstructionVisualizationParameters
            {
                CompactMode = true,
                LayersAbovePipe = new[] { MakeLayer(50, 5, "Бетон", LayerPosition.AbovePipe, 0) },
                LayersBelowPipe = new[] { MakeLayer(50, 1, "Песок", LayerPosition.BelowPipe, 0) },
                MaxVisualizationHeight = 180,
                OverflowMode = ScaleOverflowMode.FixedDepthWindow
            };

            var withFixedScale = new ConstructionVisualizationParameters
            {
                CompactMode = true,
                FixedScaleFactor = 0.25, // в режиме окна игнорируется
                LayersAbovePipe = new[] { MakeLayer(120, 6, "Бетон с сеткой", LayerPosition.AbovePipe, 0) },
                LayersBelowPipe = new[] { MakeLayer(2000, 2, "Грунт", LayerPosition.BelowPipe, 0) },
                MaxVisualizationHeight = 180,
                OverflowMode = ScaleOverflowMode.FixedDepthWindow
            };

            // Act
            var canvasThick = Render(thickPie);
            var canvasShallow = Render(shallowPie);
            var canvasFixedScale = Render(withFixedScale);

            // Assert: бокс всегда ровно 180 — высота не зависит от данных
            Assert.That(canvasThick.Height, Is.EqualTo(180),
                "canvas.Height должен быть ровно MaxVisualizationHeight (толстый пирог)");
            Assert.That(canvasShallow.Height, Is.EqualTo(180),
                "canvas.Height должен быть ровно MaxVisualizationHeight (тонкий пирог)");
            Assert.That(canvasFixedScale.Height, Is.EqualTo(180),
                "canvas.Height должен быть ровно MaxVisualizationHeight (FixedScaleFactor игнорируется в режиме окна)");
        }

        /// <summary>
        /// Слой, пересекающий линию среза, обрезается по нижней кромке окна;
        /// верхний слой — в истинном едином масштабе; подпись среза присутствует,
        /// маркер «не в масштабе» отсутствует (обрезка — честное окно, не сжатие).
        /// </summary>
        [Test]
        public void FixedDepth_CrossingCut_ClippedAtBottom_TrueScaleAbove()
        {
            // Arrange: above = [100мм], below = [2000мм], MaxHeight = 300 (обычный режим)
            // contentHeight = 300 - 2*20 - 40 = 220; scale = 220 / (100 + 1000) = 0.2
            // baseY = 20 + 100*0.2 = 40; cutY = 40 + 1000*0.2 = 240
            // above rect = 20 (истинный масштаб), below clipped = 240 - 40 = 200
            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[] { MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0) },
                LayersBelowPipe = new[] { MakeLayer(2000, 2, "Грунт", LayerPosition.BelowPipe, 0) },
                MaxVisualizationHeight = 300,
                OverflowMode = ScaleOverflowMode.FixedDepthWindow
            };

            // Act
            var canvas = Render(parameters);
            var rectangles = canvas.Children.OfType<Rectangle>().ToList();

            // Assert: верхний слой в истинном масштабе (100мм * 0.2 = 20px)
            Assert.That(rectangles.Any(r => Math.Abs(r.Height - 20.0) < 0.01), Is.True,
                "Слой над трубой должен быть в истинном масштабе (20px)");

            // Assert: нижний слой обрезан ровно по линии среза
            var cutY = 300 - NormalCanvasMargin - NormalLabelsHeight; // 240
            Assert.That(rectangles.Any(r => Math.Abs(BottomOf(r) - cutY) < 0.01), Is.True,
                $"Нижний слой должен быть обрезан по линии среза (низ = {cutY})");

            // Assert: подпись среза есть, маркер сжатия — нет
            Assert.That(HasCutCaption(canvas), Is.True, "Должна присутствовать подпись линии среза");
            Assert.That(HasScaleMarker(canvas), Is.False,
                "Обрезка по окну — не сжатие: маркер «не в масштабе» не нужен");
        }

        /// <summary>
        /// Пирог «не дорос» до среза — последний слой дотягивается до низа окна,
        /// на границе истинного масштаба ставится маркер «не в масштабе».
        /// </summary>
        [Test]
        public void FixedDepth_ShallowPie_LastLayerStretched_WithMarker()
        {
            // Arrange: above = [100мм], below = [200мм], compact, MaxHeight = 180
            // contentHeight = 180 - 2*10 - 30 = 130; scale = 130 / 1100 ≈ 0.11818
            // baseY ≈ 21.82; cutY = 140; ниже трубы помещается только 200*0.118 ≈ 23.6px из 118.2
            var parameters = new ConstructionVisualizationParameters
            {
                CompactMode = true,
                LayersAbovePipe = new[] { MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0) },
                LayersBelowPipe = new[] { MakeLayer(200, 1, "Песок", LayerPosition.BelowPipe, 0) },
                MaxVisualizationHeight = 180,
                OverflowMode = ScaleOverflowMode.FixedDepthWindow
            };

            // Act
            var canvas = Render(parameters);
            var rectangles = canvas.Children.OfType<Rectangle>().ToList();

            // Assert: последний слой дотянут до линии среза
            var cutY = 180 - CompactCanvasMargin - CompactLabelsHeight; // 140
            Assert.That(rectangles.Any(r => Math.Abs(BottomOf(r) - cutY) < 0.01), Is.True,
                $"Последний слой должен быть дотянут до линии среза (низ = {cutY})");

            // Assert: маркер «не в масштабе» на границе дотяжки присутствует
            Assert.That(HasScaleMarker(canvas), Is.True,
                "На границе дотяжки должен присутствовать маркер «не в масштабе»");

            // Assert: подпись среза присутствует
            Assert.That(HasCutCaption(canvas), Is.True, "Должна присутствовать подпись линии среза");
        }

        /// <summary>
        /// Слишком маленький MaxVisualizationHeight (контент не помещается) —
        /// рендер не падает, canvas.Height остаётся константным и положительным.
        /// </summary>
        [Test]
        public void FixedDepth_TooSmallBox_DoesNotCrash()
        {
            // Arrange: MaxVisualizationHeight=10 < 2*margin + labelsHeight = 80 (обычный режим)
            var parameters = new ConstructionVisualizationParameters
            {
                LayersAbovePipe = new[] { MakeLayer(100, 5, "Бетон", LayerPosition.AbovePipe, 0) },
                LayersBelowPipe = new[] { MakeLayer(200, 1, "Песок", LayerPosition.BelowPipe, 0) },
                MaxVisualizationHeight = 10,
                OverflowMode = ScaleOverflowMode.FixedDepthWindow
            };

            // Act
            Canvas canvas;
            Assert.DoesNotThrow(() => canvas = Render(parameters),
                "Рендер с слишком маленьким MaxVisualizationHeight не должен падать");
            canvas = Render(parameters);

            // Assert: инвариант константного бокса сохраняется
            Assert.That(canvas.Height, Is.EqualTo(10),
                "canvas.Height должен оставаться ровно MaxVisualizationHeight");
            Assert.That(double.IsInfinity(canvas.Height), Is.False, "canvas.Height должен быть конечным");
            Assert.That(double.IsNaN(canvas.Height), Is.False, "canvas.Height не должен быть NaN");
        }
    }
}
