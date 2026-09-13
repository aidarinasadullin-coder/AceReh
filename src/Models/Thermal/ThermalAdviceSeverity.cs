namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Серьёзность совета теплового расчёта
    /// </summary>
    public enum ThermalAdviceSeverity
    {
        /// <summary>Предупреждение: расчёт работает, но параметры на границе/вне рекомендации</summary>
        Warning,

        /// <summary>Ошибка: расчёт заведомо неработоспособен (совпадает с ошибкой валидатора)</summary>
        Error
    }
}
