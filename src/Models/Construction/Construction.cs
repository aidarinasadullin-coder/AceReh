using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Конструкция ("Пирог") системы снеготаяния
    /// Реализует интерфейс IConstructionData для интеграции с модулем теплового расчёта
    /// </summary>
    public class Construction : IConstructionData
    {
        private double _groundwaterLevel = 2.0;
        private bool _hasLoads = false;

        /// <summary>
        /// Слои над трубой (к поверхности)
        /// </summary>
        public ObservableCollection<Layer> LayersAbovePipe { get; } = new ObservableCollection<Layer>();

        /// <summary>
        /// Слои под трубой (к грунту)
        /// </summary>
        public ObservableCollection<Layer> Layers { get; } = new ObservableCollection<Layer>();

        /// <summary>
        /// Уровень грунтовых вод, м
        /// </summary>
        public double GroundwaterLevel
        {
            get => _groundwaterLevel;
            set
            {
                if (_groundwaterLevel != value)
                {
                    _groundwaterLevel = value;
                    UpdateLambdaForGroundwater();
                }
            }
        }

        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        public bool HasLoads
        {
            get => _hasLoads;
            set
            {
                if (_hasLoads != value)
                {
                    _hasLoads = value;
                    OnDataChanged();
                }
            }
        }

        /// <summary>
        /// Материал вокруг трубы (для LambdaE)
        /// Определяется автоматически как первый слой над трубой
        /// </summary>
        public Material? MaterialAroundPipe => LayersAbovePipe.FirstOrDefault()?.Material;

        // === IConstructionData Implementation ===

        /// <summary>
        /// Суммарное термическое сопротивление слоёв над трубой, м²·К/Вт
        /// R1Total = Σ(R_i) для всех слоёв над трубой
        /// </summary>
        public double R1Total => LayersAbovePipe.Sum(l => l.CalculatedR);

        /// <summary>
        /// Суммарное термическое сопротивление слоёв под трубой, м²·К/Вт
        /// R2Total = Σ(R_i) для всех слоёв под трубой
        /// </summary>
        public double R2Total => Layers.Where(l => l.Position == LayerPosition.BelowPipe).Sum(l => l.CalculatedR);

        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// LambdaE = λ материала вокруг трубы (первый слой над трубой)
        /// </summary>
        public double LambdaE => MaterialAroundPipe?.LambdaA ?? 1.6;

        /// <summary>
        /// Признак валидности данных конструкции
        /// </summary>
        public bool IsValid => ValidateConstruction().IsValid;

        /// <summary>
        /// Событие изменения данных
        /// </summary>
        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Создать новую конструкцию
        /// </summary>
        public Construction()
        {
            // Подписываемся на изменения коллекций
            LayersAbovePipe.CollectionChanged += (s, e) => OnDataChanged();
            Layers.CollectionChanged += (s, e) => OnDataChanged();
        }

        // === Методы управления слоями ===

        /// <summary>
        /// Добавить слой над трубой
        /// </summary>
        /// <param name="material">Материал слоя</param>
        /// <param name="thickness">Толщина слоя, мм</param>
        /// <returns>Созданный слой</returns>
        public Layer AddLayerAbovePipe(Material material, double thickness)
        {
            ArgumentNullException.ThrowIfNull(material, nameof(material));

            if (thickness > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(thickness),
                    "Толщина слоя не может превышать 1000 мм");
            }

            var layer = new Layer
            {
                Material = material,
                Thickness = thickness,
                CalculatedLambda = GetLambdaForLayer(material, LayerPosition.AbovePipe),
                Position = LayerPosition.AbovePipe,
                Order = LayersAbovePipe.Count
            };

            LayersAbovePipe.Add(layer);
            OnDataChanged();
            return layer;
        }

        /// <summary>
        /// Добавить слой под трубой
        /// </summary>
        /// <param name="material">Материал слоя</param>
        /// <param name="thickness">Толщина слоя, мм</param>
        /// <returns>Созданный слой</returns>
        public Layer AddLayerBelowPipe(Material material, double thickness)
        {
            ArgumentNullException.ThrowIfNull(material, nameof(material));

            if (thickness > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(thickness),
                    "Толщина слоя не может превышать 1000 мм");
            }

            var layer = new Layer
            {
                Material = material,
                Thickness = thickness,
                CalculatedLambda = GetLambdaForLayer(material, LayerPosition.BelowPipe),
                Position = LayerPosition.BelowPipe,
                Order = Layers.Count(l => l.Position == LayerPosition.BelowPipe)
            };

            Layers.Add(layer);
            OnDataChanged();
            return layer;
        }

        /// <summary>
        /// Удалить слой
        /// </summary>
        /// <param name="layer">Слой для удаления</param>
        public void RemoveLayer(Layer layer)
        {
            ArgumentNullException.ThrowIfNull(layer, nameof(layer));

            if (layer.Position == LayerPosition.AbovePipe)
            {
                LayersAbovePipe.Remove(layer);
                // Пересчитываем порядок слоёв
                for (int i = 0; i < LayersAbovePipe.Count; i++)
                {
                    LayersAbovePipe[i].Order = i;
                }
            }
            else
            {
                Layers.Remove(layer);
                // Пересчитываем порядок слоёв
                int order = 0;
                foreach (var l in Layers.Where(l => l.Position == LayerPosition.BelowPipe))
                {
                    l.Order = order++;
                }
            }

            OnDataChanged();
        }

        /// <summary>
        /// Очистить все слои
        /// </summary>
        public void ClearLayers()
        {
            LayersAbovePipe.Clear();
            Layers.Clear();
            OnDataChanged();
        }

        // === Методы расчёта ===

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление над трубой (R1)
        /// </summary>
        /// <returns>R1, м²·К/Вт</returns>
        public double CalculateR1()
        {
            return LayersAbovePipe.Sum(l => l.CalculatedR);
        }

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление под трубой (R2)
        /// </summary>
        /// <returns>R2, м²·К/Вт</returns>
        public double CalculateR2()
        {
            return Layers.Where(l => l.Position == LayerPosition.BelowPipe).Sum(l => l.CalculatedR);
        }

        /// <summary>
        /// Получить теплопроводность материала вокруг трубы (LambdaE)
        /// </summary>
        /// <returns>LambdaE, Вт/м·К</returns>
        public double GetLambdaE()
        {
            return MaterialAroundPipe?.LambdaA ?? 1.6;
        }

        /// <summary>
        /// Обновить λ для всех слоёв под трубой при изменении УГВ
        /// </summary>
        public void UpdateLambdaForGroundwater()
        {
            foreach (var layer in Layers.Where(l => l.Position == LayerPosition.BelowPipe))
            {
                layer.UpdateLambda(GroundwaterLevel);
            }
            OnDataChanged();
        }

        /// <summary>
        /// Получить λ для слоя в зависимости от УГВ
        /// </summary>
        /// <param name="material">Материал слоя</param>
        /// <param name="position">Позиция слоя</param>
        /// <returns>λ, Вт/м·К</returns>
        private double GetLambdaForLayer(Material material, LayerPosition position)
        {
            if (position == LayerPosition.AbovePipe)
            {
                // Слои над трубой всегда используют λА
                return material.LambdaA;
            }
            else
            {
                // Слои под трубой: λБ при УГВ < 1м, λА при УГВ >= 1м
                return GroundwaterLevel < 1.0 ? material.LambdaB : material.LambdaA;
            }
        }

        // === Валидация ===

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        /// <returns>Результат валидации</returns>
        public ValidationResult ValidateConstruction()
        {
            var result = ValidationResult.Success();

            // Проверка наличия слоёв
            if (LayersAbovePipe.Count == 0 && Layers.Count == 0)
            {
                result.AddError("Конструкция должна содержать хотя бы один слой");
                return result;
            }

            // Проверка минимальной стяжки над трубой
            var minThickness = HasLoads ? 50.0 : 40.0;
            var totalAbove = LayersAbovePipe.Sum(l => l.Thickness);
            if (LayersAbovePipe.Count > 0 && totalAbove < minThickness)
            {
                result.AddError($"Минимальная толщина слоёв над трубой: {minThickness} мм (текущая: {totalAbove} мм)");
            }

            // Проверка толщины слоёв
            foreach (var layer in LayersAbovePipe.Concat(Layers))
            {
                if (layer.Thickness > 1000)
                {
                    result.AddError($"Толщина слоя '{layer.Material?.Name ?? "Не указан"}' не может превышать 1000 мм (текущая: {layer.Thickness} мм)");
                }
            }

            // Проверка УГВ
            if (GroundwaterLevel < 0 || GroundwaterLevel > 10)
            {
                result.AddError("Уровень грунтовых вод должен быть от 0 до 10 м");
            }

            // Проверка материалов
            foreach (var layer in LayersAbovePipe)
            {
                if (layer.Material?.MaxSupplyTemp.HasValue == true)
                {
                    // Предупреждение о максимальной температуре подачи
                    result.AddWarning($"Материал '{layer.Material.Name}': максимальная температура подачи {layer.Material.MaxSupplyTemp}°C");
                }

                if (layer.Material?.MinOutdoorTemp.HasValue == true)
                {
                    // Предупреждение о минимальной температуре воздуха
                    result.AddWarning($"Материал '{layer.Material.Name}': не применять при температуре <= {layer.Material.MinOutdoorTemp}°C");
                }
            }

            return result;
        }

        // === События ===

        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
        /// <param name="propertyName">Имя изменённого свойства</param>
        /// <param name="oldValue">Старое значение</param>
        /// <param name="newValue">Новое значение</param>
        /// <param name="isValid">Признак валидности</param>
        public void RaiseDataChanged(string propertyName, object? oldValue, object? newValue, bool isValid = true)
        {
            DataChanged?.Invoke(this, new ConstructionDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = isValid
            });
        }

        /// <summary>
        /// Внутренний метод вызова события изменения данных
        /// </summary>
        private void OnDataChanged()
        {
            RaiseDataChanged("Construction", null, null, IsValid);
        }

        // === Утилиты ===

        /// <summary>
        /// Получить все слои (над и под трубой)
        /// </summary>
        /// <returns>Все слои конструкции</returns>
        public IEnumerable<Layer> GetAllLayers()
        {
            return LayersAbovePipe.Concat(Layers);
        }

        /// <summary>
        /// Получить общую толщину слоёв над трубой
        /// </summary>
        /// <returns>Толщина, мм</returns>
        public double GetTotalThicknessAbovePipe()
        {
            return LayersAbovePipe.Sum(l => l.Thickness);
        }

        /// <summary>
        /// Получить общую толщину слоёв под трубой
        /// </summary>
        /// <returns>Толщина, мм</returns>
        public double GetTotalThicknessBelowPipe()
        {
            return Layers.Where(l => l.Position == LayerPosition.BelowPipe).Sum(l => l.Thickness);
        }

        public override string ToString()
        {
            return $"Конструкция: {LayersAbovePipe.Count} слоёв над трубой (R1={R1Total:F4}), " +
                   $"{Layers.Count(l => l.Position == LayerPosition.BelowPipe)} слоёв под трубой (R2={R2Total:F4}), " +
                   $"λE={LambdaE:F2}";
        }
    }
}