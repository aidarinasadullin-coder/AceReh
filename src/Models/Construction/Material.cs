using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Категория материала
    /// </summary>
    public enum MaterialCategory
    {
        /// <summary>
        /// Бетон (Бетон на каменном щебне, Бетон на песке, Бетон плотный, Железобетон)
        /// </summary>
        Concrete = 0,

        /// <summary>
        /// Грунт (Песок, Грунт)
        /// </summary>
        Soil = 1,

        /// <summary>
        /// Изоляция (Пенополистирол ЭППС)
        /// </summary>
        Insulation = 2,

        /// <summary>
        /// Покрытие (Асфальтобетон, Асфальт)
        /// </summary>
        Coating = 3,

        /// <summary>
        /// Подстилающий слой (Щебень/Гравий)
        /// </summary>
        Subbase = 4,

        /// <summary>
        /// Стяжка (Цементно-песчаная стяжка)
        /// </summary>
        Screed = 5
    }

    /// <summary>
    /// Материал слоя конструкции
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Идентификатор материала
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название материала
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Категория материала
        /// </summary>
        public MaterialCategory Category { get; set; }

        /// <summary>
        /// Теплопроводность в сухих условиях (УГВ >= 1м), Вт/м·К
        /// </summary>
        public double LambdaA { get; set; }

        /// <summary>
        /// Теплопроводность во влажных условиях (УГВ < 1м), Вт/м·К
        /// </summary>
        public double LambdaB { get; set; }

        /// <summary>
        /// Максимальная температура подачи, °C (null = без ограничений)
        /// Для бетона = 50°C
        /// </summary>
        public double? MaxSupplyTemp { get; set; }

        /// <summary>
        /// Минимальная температура наружного воздуха, °C (null = без ограничений)
        /// Для асфальта = -15°C
        /// </summary>
        public double? MinOutdoorTemp { get; set; }

        /// <summary>
        /// Примечания к материалу
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Получить предустановленные материалы
        /// </summary>
        /// <returns>Список предустановленных материалов</returns>
        public static List<Material> GetDefaultMaterials()
        {
            return new List<Material>
            {
                new Material
                {
                    Id = 1,
                    Name = "Песок",
                    Category = MaterialCategory.Soil,
                    LambdaA = 0.4,
                    LambdaB = 2.0,
                    Notes = "При высоком УГВ теплопроводность резко возрастает"
                },
                new Material
                {
                    Id = 2,
                    Name = "Грунт",
                    Category = MaterialCategory.Soil,
                    LambdaA = 0.5,
                    LambdaB = 1.5,
                    Notes = "Естественный грунт"
                },
                new Material
                {
                    Id = 3,
                    Name = "Бетон на каменном щебне",
                    Category = MaterialCategory.Concrete,
                    LambdaA = 1.5,
                    LambdaB = 1.5,
                    MaxSupplyTemp = 50,
                    Notes = "Не зависит от влажности"
                },
                new Material
                {
                    Id = 4,
                    Name = "Бетон на песке",
                    Category = MaterialCategory.Concrete,
                    LambdaA = 0.7,
                    LambdaB = 0.7,
                    MaxSupplyTemp = 50,
                    Notes = "Не зависит от влажности"
                },
                new Material
                {
                    Id = 5,
                    Name = "Бетон плотный",
                    Category = MaterialCategory.Concrete,
                    LambdaA = 1.5,
                    LambdaB = 1.5,
                    MaxSupplyTemp = 50,
                    Notes = "Не зависит от влажности"
                },
                new Material
                {
                    Id = 6,
                    Name = "Железобетон",
                    Category = MaterialCategory.Concrete,
                    LambdaA = 1.7,
                    LambdaB = 1.7,
                    MaxSupplyTemp = 50,
                    Notes = "Не зависит от влажности"
                },
                new Material
                {
                    Id = 7,
                    Name = "Асфальтобетон",
                    Category = MaterialCategory.Coating,
                    LambdaA = 1.5,
                    LambdaB = 1.5,
                    MinOutdoorTemp = -15,
                    Notes = "Не применять при температуре наружного воздуха <= -15°C"
                },
                new Material
                {
                    Id = 8,
                    Name = "Щебень/Гравий",
                    Category = MaterialCategory.Subbase,
                    LambdaA = 0.7,
                    LambdaB = 1.8,
                    Notes = "Подстилающий слой"
                },
                new Material
                {
                    Id = 9,
                    Name = "Цементно-песчаная стяжка",
                    Category = MaterialCategory.Screed,
                    LambdaA = 1.2,
                    LambdaB = 1.2,
                    MaxSupplyTemp = 50,
                    Notes = "Не зависит от влажности"
                },
                new Material
                {
                    Id = 10,
                    Name = "Пенополистирол ЭППС",
                    Category = MaterialCategory.Insulation,
                    LambdaA = 0.035,
                    LambdaB = 0.035,
                    Notes = "Теплоизоляция"
                },
                new Material
                {
                    Id = 11,
                    Name = "Асфальт",
                    Category = MaterialCategory.Coating,
                    LambdaA = 0.75,
                    LambdaB = 0.75,
                    MinOutdoorTemp = -15,
                    Notes = "Не применять при температуре наружного воздуха <= -15°C"
                }
            };
        }

        /// <summary>
        /// Получить материал по умолчанию (Бетон плотный)
        /// </summary>
        /// <returns>Материал по умолчанию</returns>
        public static Material GetDefaultMaterial()
        {
            return new Material
            {
                Id = 5,
                Name = "Бетон плотный",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.5,
                LambdaB = 1.5,
                MaxSupplyTemp = 50,
                Notes = "Не зависит от влажности"
            };
        }

        /// <summary>
        /// Получить цвет материала для визуализации
        /// </summary>
        /// <returns>Цвет в формате HEX</returns>
        public string GetColor()
        {
            return Category switch
            {
                MaterialCategory.Concrete => "#808080",    // Серый
                MaterialCategory.Soil => "#8B4513",         // Коричневый
                MaterialCategory.Insulation => "#FFD700",   // Жёлтый
                MaterialCategory.Coating => "#000000",      // Чёрный
                MaterialCategory.Subbase => "#A0A0A0",      // Светло-серый
                MaterialCategory.Screed => "#C0C0C0",       // Светло-серый
                _ => "#CCCCCC"
            };
        }

        public override string ToString()
        {
            return $"{Name} (λА={LambdaA:F3}, λБ={LambdaB:F3})";
        }
    }
}