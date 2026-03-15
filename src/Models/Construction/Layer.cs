using System;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Слой конструкции
    /// </summary>
    public class Layer
    {
        /// <summary>
        /// Уникальный идентификатор слоя
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Материал слоя
        /// </summary>
        public Material Material { get; set; } = null!;

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        public double Thickness { get; set; } = 50.0;

        /// <summary>
        /// Теплопроводность (λ), Вт/м·К
        /// Автоматически подставляется из Material, но может быть изменена вручную
        /// </summary>
        public double CalculatedLambda { get; set; }

        /// <summary>
        /// Признак того, что λ изменена вручную
        /// </summary>
        public bool IsLambdaOverridden { get; set; } = false;

        /// <summary>
        /// Позиция слоя относительно трубы (над/под)
        /// </summary>
        public LayerPosition Position { get; set; }

        /// <summary>
        /// Порядковый номер слоя (от поверхности)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Термическое сопротивление слоя, м²·К/Вт
        /// R = d / λ / 1000
        /// </summary>
        public double CalculatedR
        {
            get
            {
                if (CalculatedLambda <= 0)
                    return 0;
                return Thickness / CalculatedLambda / 1000.0;
            }
        }

        /// <summary>
        /// Создать копию слоя
        /// </summary>
        /// <returns>Копия слоя</returns>
        public Layer Clone()
        {
            return new Layer
            {
                Id = Id,
                Material = Material,
                Thickness = Thickness,
                CalculatedLambda = CalculatedLambda,
                IsLambdaOverridden = IsLambdaOverridden,
                Position = Position,
                Order = Order
            };
        }

        /// <summary>
        /// Обновить λ в зависимости от УГВ
        /// </summary>
        /// <param name="groundwaterLevel">Уровень грунтовых вод, м</param>
        public void UpdateLambda(double groundwaterLevel)
        {
            if (IsLambdaOverridden)
                return;

            // Слои над трубой всегда используют λА
            if (Position == LayerPosition.AbovePipe)
            {
                CalculatedLambda = Material.LambdaA;
            }
            else
            {
                // Слои под трубой: λБ при УГВ < 1м, λА при УГВ >= 1м
                CalculatedLambda = groundwaterLevel < 1.0 ? Material.LambdaB : Material.LambdaA;
            }
        }

        public override string ToString()
        {
            return $"{Material?.Name ?? "Не указан"}: {Thickness} мм (R={CalculatedR:F4} м²·К/Вт)";
        }
    }
}