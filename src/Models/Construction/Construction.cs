using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        /// <summary>
        /// Слои над трубой в физическом порядке сверху-вниз: индекс 0 = поверхность, последний элемент = ближайший к трубе (стяжка/бетон вокруг трубы).
        /// </summary>
        public ObservableCollection<Layer> LayersAbovePipe { get; } = new ObservableCollection<Layer>();

        /// <summary>
        /// Все слои конструкции. Слои под трубой хранятся в физическом порядке сверху-вниз: индекс 0 = ближайший к трубе, последний элемент = грунт.
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
        /// Материал слоя, ближайшего к трубе (последний слой над трубой = стяжка/бетон вокруг трубы).
        /// Используется для расчёта LambdaE.
        /// </summary>
        public Material? MaterialAroundPipe => LayersAbovePipe.LastOrDefault()?.Material;

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
        /// LambdaE = λ материала вокруг трубы (последний слой над трубой = слой, ближайший к трубе)
        /// </summary>
        public double LambdaE => MaterialAroundPipe?.LambdaA ?? 1.6;

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
            }
            else
            {
                Layers.Remove(layer);
            }

            ReindexLayers();
            OnDataChanged();
        }

        /// <summary>
        /// Единый источник истины для порядка слоёв: переустанавливает <see cref="Layer.Order"/>
        /// равным индексу элемента в коллекции. Для LayersAbovePipe 0 = поверхность,
        /// последний = у трубы; для LayersBelowPipe 0 = ближайший к трубе, последний = грунт.
        /// Вызывайте после любой мутации коллекций слоёв.
        /// </summary>
        public void ReindexLayers()
        {
            for (int i = 0; i < LayersAbovePipe.Count; i++)
            {
                LayersAbovePipe[i].Order = i;
            }

            var below = Layers.Where(l => l.Position == LayerPosition.BelowPipe).ToList();
            for (int i = 0; i < below.Count; i++)
            {
                below[i].Order = i;
            }
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
            RaiseDataChanged("Construction", null, null);
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