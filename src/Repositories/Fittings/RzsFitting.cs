// ================================================================================
// REHAU Снеготаяние - Модель резьбозажимного соединения (РЗС)
// ================================================================================
//
// Назначение: позиция каталога фитингов из data/rehau_products.json (секция
// «fittings»). РЗС подбирается по типоразмеру трубы контура для спецификации
// закупки: на каждом подключении контура к коллектору (подача и обратка) —
// 2 шт на контур (данные владельца 2026-09-13).
//
// ================================================================================

namespace SnowMeltingCalculator.Models.Fittings
{
    /// <summary>
    /// Резьбозажимное соединение REHAU (евроконус) для трубы заданного типоразмера.
    /// </summary>
    public class RzsFitting
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        /// <summary>Артикул; null/пусто — в рознице не подтверждён (РЗС 25×2,3).</summary>
        public string? ArticleNumber { get; set; }

        /// <summary>Наружный диаметр трубы, мм.</summary>
        public double PipeOuterDiameterMm { get; set; }

        /// <summary>Толщина стенки трубы, мм.</summary>
        public double PipeWallThicknessMm { get; set; }

        public string? Notes { get; set; }
    }
}
