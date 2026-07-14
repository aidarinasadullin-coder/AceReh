using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnowMeltingCalculator.Services.Visualization
{
    /// <summary>
    /// Реализация сервиса генерации изображения схемы конструкции
    /// </summary>
    public class ConstructionVisualizationImageService : IConstructionVisualizationImageService
    {
        private readonly ConstructionVisualizationRenderer _renderer = new();

        /// <summary>
        /// Сгенерировать PNG-изображение схемы конструкции
        /// </summary>
        public byte[]? GenerateImage(ConstructionVisualizationParameters parameters, double width, double height)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive", nameof(width));

            try
            {
                // Создаём Canvas с фиксированными размерами
                var canvas = new Canvas
                {
                    Width = width,
                    Height = height,
                    Background = Brushes.White
                };

                // Фиксируем размеры и layout
                canvas.Measure(new Size(width, height));
                canvas.Arrange(new Rect(0, 0, width, height));

                // Рендерим схему
                _renderer.Render(canvas, parameters, width);

                // Принудительно обновляем layout после рендера
                canvas.UpdateLayout();

                // Рендерим Canvas в bitmap
                var renderTarget = new RenderTargetBitmap(
                    (int)width,
                    (int)height,
                    96,
                    96,
                    PixelFormats.Pbgra32);

                renderTarget.Render(canvas);
                renderTarget.Freeze();

                // Кодируем в PNG
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка генерации изображения конструкции: {ex.Message}");
                return null;
            }
        }
    }
}
