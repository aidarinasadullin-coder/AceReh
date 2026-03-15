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
        /// Получить предустановленные шаблоны конструкций
        /// </summary>
        /// <returns>Список шаблонов</returns>
        public static List<ConstructionTemplate> GetDefaultTemplates()
        {
            return new List<ConstructionTemplate>
            {
                new ConstructionTemplate
                {
                    Id = 1,
                    Name = "Типовая парковка",
                    Description = "Стандартная конструкция для парковок с асфальтобетонным покрытием",
                    HasLoads = true,
                    DefaultGroundwaterLevel = 2.0,
                    LayersAbovePipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 7, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 }, // Асфальтобетон
                        new LayerTemplate { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 1 } // Бетон плотный
                    },
                    LayersBelowPipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 1, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 0 }, // Песок
                        new LayerTemplate { MaterialId = 2, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 1 } // Грунт
                    }
                },
                new ConstructionTemplate
                {
                    Id = 2,
                    Name = "Пешеходная дорожка",
                    Description = "Облегчённая конструкция для пешеходных дорожек",
                    HasLoads = false,
                    DefaultGroundwaterLevel = 2.0,
                    LayersAbovePipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 7, Thickness = 40, Position = LayerPosition.AbovePipe, Order = 0 }, // Асфальтобетон
                        new LayerTemplate { MaterialId = 9, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 1 } // Цементно-песчаная стяжка
                    },
                    LayersBelowPipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 1, Thickness = 100, Position = LayerPosition.BelowPipe, Order = 0 }, // Песок
                        new LayerTemplate { MaterialId = 2, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 1 } // Грунт
                    }
                },
                new ConstructionTemplate
                {
                    Id = 3,
                    Name = "Въезд в гараж",
                    Description = "Усиленная конструкция для въездов в гараж с железобетонным покрытием",
                    HasLoads = true,
                    DefaultGroundwaterLevel = 1.5,
                    LayersAbovePipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 7, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 }, // Асфальтобетон
                        new LayerTemplate { MaterialId = 6, Thickness = 150, Position = LayerPosition.AbovePipe, Order = 1 } // Железобетон
                    },
                    LayersBelowPipe = new List<LayerTemplate>
                    {
                        new LayerTemplate { MaterialId = 1, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 0 }, // Песок
                        new LayerTemplate { MaterialId = 2, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 1 } // Грунт
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