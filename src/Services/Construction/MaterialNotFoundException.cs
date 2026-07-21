using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Исключение, возникающее при загрузке конструкции, если материал по идентификатору
    /// отсутствует в справочнике. Может содержать снимок материала, который можно импортировать.
    /// </summary>
    public class MaterialNotFoundException : InvalidOperationException
    {
        /// <summary>
        /// Идентификатор отсутствующего материала
        /// </summary>
        public int MaterialId { get; }

        /// <summary>
        /// Снимок материала, если он сохранён в файле конструкции
        /// </summary>
        public MaterialSnapshot? Snapshot { get; }

        /// <summary>
        /// Создать исключение без снимка
        /// </summary>
        /// <param name="materialId">Идентификатор отсутствующего материала</param>
        public MaterialNotFoundException(int materialId)
            : base($"Материал с идентификатором {materialId} не найден")
        {
            MaterialId = materialId;
        }

        /// <summary>
        /// Создать исключение со снимком материала
        /// </summary>
        /// <param name="materialId">Идентификатор отсутствующего материала</param>
        /// <param name="snapshot">Снимок материала из файла конструкции</param>
        public MaterialNotFoundException(int materialId, MaterialSnapshot snapshot)
            : base($"Материал с идентификатором {materialId} не найден. Доступен снимок для импорта: '{snapshot.Name}'.")
        {
            MaterialId = materialId;
            Snapshot = snapshot;
        }
    }
}
