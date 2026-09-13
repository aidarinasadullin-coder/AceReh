using SnowMeltingCalculator.Models.Navigation;

namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Совет теплового расчёта: рекомендация «что делать» при выходе за лимит.
    /// Derived-значение — строится <c>IThermalAdviceService</c> из результата
    /// расчёта, канонического состояния не создаёт и ничего не гейтит
    /// (ошибки валидации остаются гейтом в shell).
    /// </summary>
    public sealed record ThermalAdvice
    {
        /// <summary>Стабильный идентификатор совета из каталога (напр. RETURN_NEGATIVE)</summary>
        public required string Id { get; init; }

        /// <summary>Серьёзность совета</summary>
        public required ThermalAdviceSeverity Severity { get; init; }

        /// <summary>Готовый к показу текст совета (форматирование по AppCulture)</summary>
        public required string Message { get; init; }

        /// <summary>Целевой шаг навигации (куда идти лечить)</summary>
        public required NavigationTarget TargetStep { get; init; }

        /// <summary>Заголовок целевого шага для кнопки перехода (напр. «Тепловой расчёт»)</summary>
        public required string TargetTitle { get; init; }
    }
}
