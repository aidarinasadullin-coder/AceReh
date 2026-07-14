namespace SnowMeltingCalculator.Services.Visualization
{
    /// <summary>
    /// Сервис генерации изображения схемы конструкции
    /// </summary>
    public interface IConstructionVisualizationImageService
    {
        /// <summary>
        /// Сгенерировать PNG-изображение схемы конструкции
        /// </summary>
        /// <param name="parameters">Параметры визуализации</param>
        /// <param name="width">Ширина изображения, пиксели</param>
        /// <param name="height">Высота изображения, пиксели</param>
        /// <returns>Массив байт PNG или null в случае ошибки</returns>
        byte[]? GenerateImage(ConstructionVisualizationParameters parameters, double width, double height);
    }
}
