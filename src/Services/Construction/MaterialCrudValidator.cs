using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Валидатор материала для CRUD-операций
    /// </summary>
    public class MaterialCrudValidator : IValidator<Material>
    {
        /// <summary>
        /// Минимальная длина названия материала
        /// </summary>
        private const int MinNameLength = 1;

        /// <summary>
        /// Максимальная длина названия материала
        /// </summary>
        private const int MaxNameLength = 100;

        /// <summary>
        /// Минимальная допустимая максимальная температура подачи, °C
        /// </summary>
        private const double MinMaxSupplyTemp = -50.0;

        /// <summary>
        /// Максимальная допустимая максимальная температура подачи, °C
        /// </summary>
        private const double MaxMaxSupplyTemp = 200.0;

        /// <summary>
        /// Минимальная допустимая минимальная температура наружного воздуха, °C
        /// </summary>
        private const double MinMinOutdoorTemp = -60.0;

        /// <summary>
        /// Максимальная допустимая минимальная температура наружного воздуха, °C
        /// </summary>
        private const double MaxMinOutdoorTemp = 50.0;

        private readonly IMaterialRepository _materialRepository;

        /// <summary>
        /// Создать валидатор материала
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов для проверки уникальности имени</param>
        public MaterialCrudValidator(IMaterialRepository materialRepository)
        {
            ArgumentNullException.ThrowIfNull(materialRepository, nameof(materialRepository));
            _materialRepository = materialRepository;
        }

        /// <summary>
        /// Валидировать материал
        /// </summary>
        /// <param name="material">Материал для валидации</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(Material material)
        {
            ArgumentNullException.ThrowIfNull(material, nameof(material));

            var result = new ValidationResult();

            ValidateName(material, result);
            ValidateCategory(material, result);
            ValidateLambdaA(material, result);
            ValidateLambdaB(material, result);
            ValidateMaxSupplyTemp(material, result);
            ValidateMinOutdoorTemp(material, result);
            ValidateNameUniqueness(material, result);

            return result;
        }

        private static void ValidateName(Material material, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(material.Name))
            {
                result.AddError("Название материала не может быть пустым");
                return;
            }

            if (material.Name.Length < MinNameLength || material.Name.Length > MaxNameLength)
            {
                result.AddError(
                    $"Название материала должно быть от {MinNameLength} до {MaxNameLength} символов " +
                    $"(текущая длина: {material.Name.Length})");
            }
        }

        private static void ValidateCategory(Material material, ValidationResult result)
        {
            if (!Enum.IsDefined(typeof(MaterialCategory), material.Category))
            {
                result.AddError($"Категория материала '{material.Category}' не является допустимой");
            }
        }

        private static void ValidateLambdaA(Material material, ValidationResult result)
        {
            if (material.LambdaA <= 0)
            {
                result.AddError("Теплопроводность в сухих условиях (λА) должна быть больше 0");
            }
        }

        private static void ValidateLambdaB(Material material, ValidationResult result)
        {
            if (material.LambdaB <= 0)
            {
                result.AddError("Теплопроводность во влажных условиях (λБ) должна быть больше 0");
            }
        }

        private static void ValidateMaxSupplyTemp(Material material, ValidationResult result)
        {
            if (material.MaxSupplyTemp.HasValue &&
                (material.MaxSupplyTemp.Value < MinMaxSupplyTemp || material.MaxSupplyTemp.Value > MaxMaxSupplyTemp))
            {
                result.AddError(
                    $"Максимальная температура подачи должна быть от {MinMaxSupplyTemp} до {MaxMaxSupplyTemp} °C " +
                    $"(текущая: {material.MaxSupplyTemp.Value}°C)");
            }
        }

        private static void ValidateMinOutdoorTemp(Material material, ValidationResult result)
        {
            if (material.MinOutdoorTemp.HasValue &&
                (material.MinOutdoorTemp.Value < MinMinOutdoorTemp || material.MinOutdoorTemp.Value > MaxMinOutdoorTemp))
            {
                result.AddError(
                    $"Минимальная температура наружного воздуха должна быть от {MinMinOutdoorTemp} до {MaxMinOutdoorTemp} °C " +
                    $"(текущая: {material.MinOutdoorTemp.Value}°C)");
            }
        }

        private void ValidateNameUniqueness(Material material, ValidationResult result)
        {
            var duplicate = _materialRepository
                .GetAllMaterials()
                .FirstOrDefault(m =>
                    m.Id != material.Id &&
                    string.Equals(m.Name, material.Name, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
            {
                result.AddError($"Материал с названием '{material.Name}' уже существует");
            }
        }
    }
}
