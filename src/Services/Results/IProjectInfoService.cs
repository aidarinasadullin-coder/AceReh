namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Интерфейс сервиса для хранения информации о проекте
    /// </summary>
    public interface IProjectInfoService
    {
        /// <summary>
        /// Номер проекта
        /// </summary>
        string ProjectNumber { get; set; }

        /// <summary>
        /// Наименование объекта
        /// </summary>
        string ProjectObject { get; set; }
    }
}
