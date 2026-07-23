using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Шаблон слоя для ConstructionTemplate
    /// </summary>
    public class LayerTemplate
    {
        /// <summary>
        /// Идентификатор материала
        /// </summary>
        public int MaterialId { get; set; }

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// Позиция слоя
        /// </summary>
        public LayerPosition Position { get; set; }

        /// <summary>
        /// Порядковый номер
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Шаблон конструкции ("Пирог")
    /// Предустановленные типовые конструкции для быстрого выбора
    /// </summary>
    public class ConstructionTemplate
    {
        /// <summary>
        /// Идентификатор шаблона
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название шаблона
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Описание шаблона
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Слои над трубой
        /// </summary>
        public List<LayerTemplate> LayersAbovePipe { get; set; } = new List<LayerTemplate>();

        /// <summary>
        /// Слои под трубой
        /// </summary>
        public List<LayerTemplate> LayersBelowPipe { get; set; } = new List<LayerTemplate>();

        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        public bool HasLoads { get; set; }

        /// <summary>
        /// Уровень грунтовых вод по умолчанию, м
        /// </summary>
        public double DefaultGroundwaterLevel { get; set; } = 2.0;

        /// <summary>
        /// Признак встроенного (предустановленного) шаблона
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Снимки материалов, используемых в шаблоне, для переносимости между ПК
        /// </summary>
        public List<MaterialSnapshot> MaterialSnapshots { get; set; } = new List<MaterialSnapshot>();

        /// <summary>
        /// Получить предустановленные шаблоны конструкций
        /// </summary>
        /// <returns>Список шаблонов</returns>
        public static List<ConstructionTemplate> GetDefaultTemplates()
        {
            return new List<ConstructionTemplate>
            {
                // 1. Парковка / площадка — бетон
                // Труба в монолитной бетонной плите. Под плитой — бетон, бетон с арматурной сеткой,
                // утеплитель XPS, песчано-гравийная подготовка и грунт.
                new ConstructionTemplate
                {
                    Id = 1,
                    Name = "Парковка / площадка — бетон",
                    Description = "Монолитная бетонная площадка или парковка для легковых автомобилей",
                    HasLoads = true,
                    DefaultGroundwaterLevel = 2.0,
                    LayersAbovePipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 } // Бетон
                    },
                    LayersBelowPipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 5, Thickness = 10, Position = LayerPosition.BelowPipe, Order = 0 }, // Бетон (у трубы, от оси до нижней образующей)
                        new LayerTemplate { MaterialId = 6, Thickness = 10, Position = LayerPosition.BelowPipe, Order = 1 }, // Бетон с арматурной сеткой
                        new LayerTemplate { MaterialId = 10, Thickness = 80, Position = LayerPosition.BelowPipe, Order = 2 }, // ЭППС
                        new LayerTemplate { MaterialId = 13, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 3 }, // ПГС уплотнённый
                        new LayerTemplate { MaterialId = 2, Thickness = 1000, Position = LayerPosition.BelowPipe, Order = 4 }, // Грунт основания (верхняя часть)
                        new LayerTemplate { MaterialId = 2, Thickness = 570, Position = LayerPosition.BelowPipe, Order = 5 }  // Грунт основания (нижняя часть)
                    }
                },
                // 3. Пешеходная дорожка — плитка
                // Труба в бетонном слое, сверху тротуарная плитка. Под бетоном — утеплитель, песок и грунт.
                new ConstructionTemplate
                {
                    Id = 3,
                    Name = "Пешеходная дорожка — плитка",
                    Description = "Тротуарная плитка или брусчатка с трубами в бетонном слое",
                    HasLoads = false,
                    DefaultGroundwaterLevel = 2.0,
                    LayersAbovePipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 12, Thickness = 60, Position = LayerPosition.AbovePipe, Order = 0 }, // Тротуарная плитка/брусчатка
                        new LayerTemplate { MaterialId = 5, Thickness = 60, Position = LayerPosition.AbovePipe, Order = 1 }  // Бетон
                    },
                    LayersBelowPipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 5, Thickness = 10, Position = LayerPosition.BelowPipe, Order = 0 }, // Бетон (у трубы, от оси до нижней образующей)
                        new LayerTemplate { MaterialId = 6, Thickness = 10, Position = LayerPosition.BelowPipe, Order = 1 }, // Бетон с арматурной сеткой
                        new LayerTemplate { MaterialId = 10, Thickness = 50, Position = LayerPosition.BelowPipe, Order = 2 }, // ЭППС
                        new LayerTemplate { MaterialId = 1, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 3 }, // Песок уплотнённый
                        new LayerTemplate { MaterialId = 2, Thickness = 1000, Position = LayerPosition.BelowPipe, Order = 4 }, // Грунт основания (верхняя часть)
                        new LayerTemplate { MaterialId = 2, Thickness = 690, Position = LayerPosition.BelowPipe, Order = 5 }  // Грунт основания (нижняя часть)
                    }
                },
                // 4. Въезд в гараж / пандус
                // Усиленная бетонная плита с арматурной сеткой. Под плитой — утеплитель и щебёночная подготовка.
                new ConstructionTemplate
                {
                    Id = 4,
                    Name = "Въезд в гараж / пандус",
                    Description = "Армированная бетонная плита для зон с высокими автомобильными нагрузками",
                    HasLoads = true,
                    DefaultGroundwaterLevel = 2.0,
                    LayersAbovePipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 6, Thickness = 120, Position = LayerPosition.AbovePipe, Order = 0 } // Бетон с арматурной сеткой
                    },
                    LayersBelowPipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 6, Thickness = 10, Position = LayerPosition.BelowPipe, Order = 0 }, // Бетон с арматурной сеткой (у трубы, от оси до нижней образующей)
                        new LayerTemplate { MaterialId = 6, Thickness = 10, Position = LayerPosition.BelowPipe, Order = 1 }, // Бетон с арматурной сеткой
                        new LayerTemplate { MaterialId = 10, Thickness = 100, Position = LayerPosition.BelowPipe, Order = 2 }, // ЭППС
                        new LayerTemplate { MaterialId = 8, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 3 }, // Щебень/ПГС уплотнённый
                        new LayerTemplate { MaterialId = 2, Thickness = 1000, Position = LayerPosition.BelowPipe, Order = 4 }, // Грунт основания (верхняя часть)
                        new LayerTemplate { MaterialId = 2, Thickness = 538, Position = LayerPosition.BelowPipe, Order = 5 }  // Грунт основания (нижняя часть)
                    }
                }
            };
        }

        public override string ToString()
        {
            return $"{Name}: {LayersAbovePipe.Count} слоёв над трубой, {LayersBelowPipe.Count} слоёв под трубой";
        }
    }
}