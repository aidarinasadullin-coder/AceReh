using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Каталог советов теплового расчёта: рекомендации «что делать» при
    /// нарушении лимитов. Чистая функция от результата расчёта; без состояния.
    /// </summary>
    public interface IThermalAdviceService
    {
        /// <summary>
        /// Построить список советов для результата расчёта. Нарушений нет —
        /// пустой список (карточка в UI не рендерится). Советы v1 читают
        /// только результат; входы в них не входят (план §3, правка при
        /// реализации 2026-09-13).
        /// </summary>
        IReadOnlyList<ThermalAdvice> Build(ThermalCalculationResult result);
    }
}
