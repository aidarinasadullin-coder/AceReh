// ================================================================================
// REHAU Снеготаяние - Интерфейс репозитория фитингов
// ================================================================================

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Fittings;

namespace SnowMeltingCalculator.Repositories.Fittings
{
    /// <summary>
    /// Репозиторий резьбозажимных соединений (секция «fittings»
    /// data/rehau_products.json).
    /// </summary>
    public interface IFittingsRepository
    {
        /// <summary>Получить все РЗС каталога.</summary>
        Task<IReadOnlyList<RzsFitting>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
