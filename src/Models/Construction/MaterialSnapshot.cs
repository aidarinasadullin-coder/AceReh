using System.Text.Json.Serialization;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Полный снимок материала для сериализации в составе конструкции или шаблона.
    /// Позволяет переносить проекты между ПК, даже если целевой справочник материалов
    /// не содержит исходный материал.
    /// </summary>
    public class MaterialSnapshot
    {
        /// <summary>
        /// Идентификатор материала
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Название материала
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Категория материала
        /// </summary>
        [JsonPropertyName("category")]
        public MaterialCategory Category { get; set; }

        /// <summary>
        /// Теплопроводность в сухих условиях, Вт/м·К
        /// </summary>
        [JsonPropertyName("lambda_a")]
        public double LambdaA { get; set; }

        /// <summary>
        /// Теплопроводность во влажных условиях, Вт/м·К
        /// </summary>
        [JsonPropertyName("lambda_b")]
        public double LambdaB { get; set; }

        /// <summary>
        /// Максимальная температура подачи, °C
        /// </summary>
        [JsonPropertyName("max_supply_temp")]
        public double? MaxSupplyTemp { get; set; }

        /// <summary>
        /// Минимальная температура наружного воздуха, °C
        /// </summary>
        [JsonPropertyName("min_outdoor_temp")]
        public double? MinOutdoorTemp { get; set; }

        /// <summary>
        /// Примечания к материалу
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Признак встроенного (предустановленного) материала
        /// </summary>
        [JsonPropertyName("is_built_in")]
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Создать снимок на основе материала
        /// </summary>
        /// <param name="material">Исходный материал</param>
        /// <returns>Снимок материала</returns>
        public static MaterialSnapshot FromMaterial(Material material)
        {
            ArgumentNullException.ThrowIfNull(material, nameof(material));

            return new MaterialSnapshot
            {
                Id = material.Id,
                Name = material.Name,
                Category = material.Category,
                LambdaA = material.LambdaA,
                LambdaB = material.LambdaB,
                MaxSupplyTemp = material.MaxSupplyTemp,
                MinOutdoorTemp = material.MinOutdoorTemp,
                Notes = material.Notes,
                IsBuiltIn = material.IsBuiltIn
            };
        }
    }
}
