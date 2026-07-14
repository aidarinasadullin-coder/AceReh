using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnowMeltingCalculator.Services.Visualization
{
    /// <summary>
    /// Провайдер процедурных текстур для материалов конструкции
    /// </summary>
    public class MaterialTextureProvider
    {
        private const int TILE_SIZE = 150;
        private readonly Random _rnd = new Random(42); // Фиксированный seed для консистентности

        private readonly Dictionary<int, ImageBrush> _textureCache = new();

        /// <summary>
        /// Получить текстуру для материала по ID
        /// </summary>
        public ImageBrush GetTexture(int materialId)
        {
            if (_textureCache.TryGetValue(materialId, out var brush))
                return brush;

            brush = CreateTextureForMaterial(materialId);
            brush.Freeze();
            _textureCache[materialId] = brush;
            return brush;
        }

        private ImageBrush CreateTextureForMaterial(int materialId)
        {
            return materialId switch
            {
                1 => CreateSandTile(),           // Песок
                2 => CreateSoilTile(),           // Грунт
                3 => CreateConcreteTile(),       // Бетон на каменном щебне
                4 => CreateConcreteTile(),       // Бетон на песке
                5 => CreateConcreteTile(),       // Бетон плотный
                6 => CreateReinforcedConcreteTile(), // Железобетон
                7 => CreateAsphaltTile(),        // Асфальтобетон
                8 => CreateGravelTile(),         // Щебень/Гравий
                9 => CreateScreedTile(),         // Цементно-песчаная стяжка
                10 => CreateXPSTile(),           // Пенополистирол ЭППС
                11 => CreateAsphaltTile(),       // Асфальт
                _ => CreateConcreteTile()       // По умолчанию
            };
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        private double NextRnd(double min, double max) => min + _rnd.NextDouble() * (max - min);

        private void DrawWrapped(DrawingContext dc, Action<DrawingContext, double, double> drawAction, double x, double y, double w, double h)
        {
            if (x < w) drawAction(dc, x + TILE_SIZE, y);
            if (x + w > TILE_SIZE) drawAction(dc, x - TILE_SIZE, y);
            if (y < h) drawAction(dc, x, y + TILE_SIZE);
            if (y + h > TILE_SIZE) drawAction(dc, x, y - TILE_SIZE);
            drawAction(dc, x, y);
        }

        private ImageBrush VisualToTileBrush(DrawingVisual visual)
        {
            var renderTarget = new RenderTargetBitmap(TILE_SIZE, TILE_SIZE, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(visual);
            renderTarget.Freeze();

            var brush = new ImageBrush(renderTarget)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, TILE_SIZE, TILE_SIZE),
                ViewportUnits = BrushMappingMode.Absolute
            };
            return brush;
        }

        // === ГЕНЕРАЦИЯ ТЕКСТУР ===

        /// <summary>
        /// Песок - мелкие песчинки бежевого цвета
        /// </summary>
        private ImageBrush CreateSandTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xD4, 0xB0, 0x80)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                for (int i = 0; i < 600; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 1 + _rnd.NextDouble() * 2.5;

                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        ctx.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 150, 120, 80)), null, new Point(px + 0.5, py + 0.5), size, size);
                        byte r = (byte)(220 + _rnd.NextDouble() * 35);
                        byte g = (byte)(200 + _rnd.NextDouble() * 20);
                        byte b = (byte)(150 + _rnd.NextDouble() * 20);
                        ctx.DrawEllipse(new SolidColorBrush(Color.FromArgb(230, r, g, b)), null, new Point(px, py), size, size);
                    }, x, y, size * 2, size * 2);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// Грунт - коричневый с мелкими частицами
        /// </summary>
        private ImageBrush CreateSoilTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var grad = new LinearGradientBrush();
                grad.GradientStops.Add(new GradientStop(Color.FromRgb(0x8B, 0x73, 0x55), 0.0));
                grad.GradientStops.Add(new GradientStop(Color.FromRgb(0x6B, 0x5B, 0x45), 1.0));
                dc.DrawRectangle(grad, null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                for (int i = 0; i < 2000; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 0.5 + _rnd.NextDouble() * 1.5;
                    byte r = (byte)(100 + _rnd.NextDouble() * 50);
                    byte g = (byte)(80 + _rnd.NextDouble() * 40);
                    byte b = (byte)(50 + _rnd.NextDouble() * 30);

                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        ctx.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(128 + _rnd.NextDouble() * 127), r, g, b)), null, new Point(px, py), size, size);
                    }, x, y, size * 2, size * 2);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// Бетон - серый с трещинами
        /// </summary>
        private ImageBrush CreateConcreteTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                for (int i = 0; i < 300; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 1 + _rnd.NextDouble() * 3;

                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 60, 60, 60)), null, new Rect(px + 1, py + 1, size, size));
                        byte gray = (byte)(110 + _rnd.NextDouble() * 50);
                        ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(gray, gray, gray)), null, new Rect(px, py, size, size));
                    }, x, y, size * 2, size * 2);
                }

                var darkPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 50, 50, 50)), 1.5);
                var lightPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 180, 180, 180)), 0.5);

                for (int i = 0; i < 5; i++)
                {
                    var geo = new StreamGeometry();
                    using (var sgc = geo.Open())
                    {
                        double x = _rnd.NextDouble() * TILE_SIZE;
                        double y = _rnd.NextDouble() * TILE_SIZE;
                        sgc.BeginFigure(new Point(x, y), false, false);
                        for (int j = 0; j < 8; j++)
                        {
                            x += (_rnd.NextDouble() - 0.5) * 40;
                            y += (_rnd.NextDouble() - 0.5) * 40;
                            if (x < 0) x += TILE_SIZE; if (x > TILE_SIZE) x -= TILE_SIZE;
                            if (y < 0) y += TILE_SIZE; if (y > TILE_SIZE) y -= TILE_SIZE;
                            sgc.LineTo(new Point(x, y), true, false);
                        }
                    }
                    geo.Freeze();
                    dc.DrawGeometry(null, darkPen, geo);
                    dc.DrawGeometry(null, lightPen, geo);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// Железобетон - бетон с арматурой
        /// </summary>
        private ImageBrush CreateReinforcedConcreteTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Бетонная основа
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8A)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                // Текстура бетона
                for (int i = 0; i < 200; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 1 + _rnd.NextDouble() * 2;
                    byte gray = (byte)(100 + _rnd.NextDouble() * 40);
                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(gray, gray, gray)), null, new Rect(px, py, size, size));
                    }, x, y, size * 2, size * 2);
                }

                // Арматура - сетка
                var rebarPen = new Pen(new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)), 3);
                var rebarHighlight = new Pen(new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)), 1);

                // Горизонтальные прутья
                for (int y = 20; y < TILE_SIZE; y += 40)
                {
                    dc.DrawLine(rebarPen, new Point(0, y), new Point(TILE_SIZE, y));
                    dc.DrawLine(rebarHighlight, new Point(0, y - 1), new Point(TILE_SIZE, y - 1));
                }

                // Вертикальные прутья
                for (int x = 20; x < TILE_SIZE; x += 40)
                {
                    dc.DrawLine(rebarPen, new Point(x, 0), new Point(x, TILE_SIZE));
                    dc.DrawLine(rebarHighlight, new Point(x - 1, 0), new Point(x - 1, TILE_SIZE));
                }

                // Трещины
                var crackPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 40, 40, 40)), 1);
                for (int i = 0; i < 3; i++)
                {
                    var geo = new StreamGeometry();
                    using (var sgc = geo.Open())
                    {
                        double x = _rnd.NextDouble() * TILE_SIZE;
                        double y = _rnd.NextDouble() * TILE_SIZE;
                        sgc.BeginFigure(new Point(x, y), false, false);
                        for (int j = 0; j < 5; j++)
                        {
                            x += (_rnd.NextDouble() - 0.5) * 30;
                            y += (_rnd.NextDouble() - 0.5) * 30;
                            sgc.LineTo(new Point(x, y), true, false);
                        }
                    }
                    geo.Freeze();
                    dc.DrawGeometry(null, crackPen, geo);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// Асфальт - чёрный с мелким щебнем
        /// </summary>
        private ImageBrush CreateAsphaltTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Тёмный фон
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                // Мелкий щебень в асфальте
                for (int i = 0; i < 400; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 2 + _rnd.NextDouble() * 4;

                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        byte gray = (byte)(40 + _rnd.NextDouble() * 30);
                        ctx.DrawEllipse(new SolidColorBrush(Color.FromRgb(gray, gray, gray)), null, new Point(px, py), size, size * 0.8);
                    }, x, y, size * 2, size * 2);
                }

                // Блики
                for (int i = 0; i < 50; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), null, new Point(x, y), 1.5, 1);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// Щебень/Гравий - камни разного размера
        /// </summary>
        private ImageBrush CreateGravelTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                for (int i = 0; i < 250; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 8 + _rnd.NextDouble() * 16;

                    int vertices = 4 + _rnd.Next(0, 3);
                    var points = new Point[vertices];
                    for (int j = 0; j < vertices; j++)
                    {
                        double angle = (Math.PI * 2 / vertices) * j + (_rnd.NextDouble() - 0.5) * 1.2;
                        double r = size * (0.6 + _rnd.NextDouble() * 0.4);
                        points[j] = new Point(Math.Cos(angle) * r, Math.Sin(angle) * r);
                    }

                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        var geo = new StreamGeometry();
                        using (var sgc = geo.Open())
                        {
                            sgc.BeginFigure(new Point(px + points[0].X, py + points[0].Y), true, true);
                            for (int k = 1; k < points.Length; k++)
                                sgc.LineTo(new Point(px + points[k].X, py + points[k].Y), true, false);
                        }
                        geo.Freeze();

                        byte gray = (byte)(180 + _rnd.NextDouble() * 70);
                        byte offset = (byte)(_rnd.NextDouble() * 30);
                        var color = Color.FromRgb(gray, gray, (byte)(gray + offset > 255 ? 255 : gray + offset));

                        ctx.DrawGeometry(new SolidColorBrush(color), new Pen(new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a)), 2 + _rnd.NextDouble() * 2), geo);
                        ctx.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), null, new Point(px - size * 0.15, py - size * 0.15), size * 0.3, size * 0.2);
                    }, x, y, size * 2.5, size * 2.5);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// Цементно-песчаная стяжка - светло-серая с мелкими трещинами
        /// </summary>
        private ImageBrush CreateScreedTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                // Мелкие песчинки
                for (int i = 0; i < 400; i++)
                {
                    double x = _rnd.NextDouble() * TILE_SIZE;
                    double y = _rnd.NextDouble() * TILE_SIZE;
                    double size = 0.5 + _rnd.NextDouble() * 1.5;

                    DrawWrapped(dc, (ctx, px, py) =>
                    {
                        byte gray = (byte)(160 + _rnd.NextDouble() * 40);
                        ctx.DrawEllipse(new SolidColorBrush(Color.FromRgb(gray, gray, gray)), null, new Point(px, py), size, size);
                    }, x, y, size * 2, size * 2);
                }

                // Мелкие трещины
                var crackPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 80, 80, 80)), 0.5);
                for (int i = 0; i < 8; i++)
                {
                    var geo = new StreamGeometry();
                    using (var sgc = geo.Open())
                    {
                        double x = _rnd.NextDouble() * TILE_SIZE;
                        double y = _rnd.NextDouble() * TILE_SIZE;
                        sgc.BeginFigure(new Point(x, y), false, false);
                        for (int j = 0; j < 4; j++)
                        {
                            x += (_rnd.NextDouble() - 0.5) * 25;
                            y += (_rnd.NextDouble() - 0.5) * 25;
                            sgc.LineTo(new Point(x, y), true, false);
                        }
                    }
                    geo.Freeze();
                    dc.DrawGeometry(null, crackPen, geo);
                }
            }
            return VisualToTileBrush(visual);
        }

        /// <summary>
        /// ЭППС (Пенополистирол) - оранжевый с диагональными линиями
        /// </summary>
        private ImageBrush CreateXPSTile()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)), null, new Rect(0, 0, TILE_SIZE, TILE_SIZE));

                var pen = new Pen(Brushes.White, 1);
                int step = 30;
                for (int i = -TILE_SIZE; i <= TILE_SIZE * 2; i += step)
                {
                    dc.DrawLine(pen, new Point(i, 0), new Point(i + TILE_SIZE, TILE_SIZE));
                    dc.DrawLine(pen, new Point(i + TILE_SIZE, 0), new Point(i, TILE_SIZE));
                }
            }
            return VisualToTileBrush(visual);
        }
    }
}
