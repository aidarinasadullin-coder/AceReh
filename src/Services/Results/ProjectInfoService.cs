namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Сервис для хранения информации о проекте
    /// </summary>
    public class ProjectInfoService : IProjectInfoService
    {
        /// <summary>
        /// Номер проекта
        /// </summary>
        public string ProjectNumber { get; set; } = string.Empty;

        /// <summary>
        /// Наименование объекта
        /// </summary>
        public string ProjectObject { get; set; } = string.Empty;
    }
}
