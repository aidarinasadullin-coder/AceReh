using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Тип трубы РЕХАУ RAUTHERM S
    /// </summary>
    public class PipeType
    {
        /// <summary>
        /// Название трубы
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Артикул
        /// </summary>
        public string Article { get; set; } = string.Empty;

        /// <summary>
        /// Наружный диаметр, мм
        /// </summary>
        public double OuterDiameter { get; set; }

        /// <summary>
        /// Внутренний диаметр, мм
        /// </summary>
        public double InnerDiameter { get; set; }

        /// <summary>
        /// Толщина стенки, мм
        /// </summary>
        public double WallThickness { get; set; }

        /// <summary>
        /// Теплопроводность материала трубы, Вт/м·К
        /// </summary>
        public double ThermalConductivity { get; set; }

        /// <summary>
        /// Отображаемое имя
        /// </summary>
        public string DisplayName => $"{Name} (Ø{OuterDiameter}×{WallThickness})";

        /// <summary>
        /// Стандартные трубы РЕХАУ RAUTHERM S
        /// </summary>
        public static readonly IReadOnlyList<PipeType> StandardPipes = new[]
        {
            new PipeType
            {
                Name = "RAUTHERM S 17x2,0",
                Article = "12180501001",
                OuterDiameter = 17,
                InnerDiameter = 13,
                WallThickness = 2.0,
                ThermalConductivity = 0.35
            },
            new PipeType
            {
                Name = "RAUTHERM S 20x2,0",
                Article = "12180502001",
                OuterDiameter = 20,
                InnerDiameter = 16,
                WallThickness = 2.0,
                ThermalConductivity = 0.35
            },
            new PipeType
            {
                Name = "RAUTHERM S 25x2,3",
                Article = "12180503001",
                OuterDiameter = 25,
                InnerDiameter = 20.4,
                WallThickness = 2.3,
                ThermalConductivity = 0.35
            }
        };

        public override string ToString() => DisplayName;

        public override bool Equals(object? obj) => obj is PipeType other &&
            string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
            OuterDiameter == other.OuterDiameter &&
            InnerDiameter == other.InnerDiameter &&
            WallThickness == other.WallThickness;

        public override int GetHashCode() => HashCode.Combine(
            Name?.ToLowerInvariant(), OuterDiameter, InnerDiameter, WallThickness);

        public static bool operator ==(PipeType? left, PipeType? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PipeType? left, PipeType? right) => !(left == right);
    }
}