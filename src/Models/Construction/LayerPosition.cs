namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Позиция слоя относительно трубы
    /// </summary>
    public enum LayerPosition
    {
        /// <summary>
        /// Над трубой (к поверхности)
        /// </summary>
        AbovePipe = 0,

        /// <summary>
        /// Под трубой (к грунту)
        /// </summary>
        BelowPipe = 1
    }
}