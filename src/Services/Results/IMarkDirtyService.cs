namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Минимальный интерфейс для пометки проекта как изменённого
    /// </summary>
    public interface IMarkDirtyService
    {
        /// <summary>
        /// Пометить проект как содержащий несохранённые изменения
        /// </summary>
        void MarkDirty();
    }
}
