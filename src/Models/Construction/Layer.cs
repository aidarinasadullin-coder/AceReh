using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Слой конструкции
    /// </summary>
    public class Layer : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private Material _material = null!;
        private double _thickness = 50.0;
        private double _calculatedLambda;
        private bool _isLambdaOverridden;
        private LayerPosition _position;
        private int _order;

        /// <summary>
        /// Уникальный идентификатор слоя
        /// </summary>
        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Материал слоя
        /// </summary>
        public Material Material
        {
            get => _material;
            set
            {
                if (_material != value)
                {
                    _material = value;
                    OnPropertyChanged();
                    // При изменении материала устанавливаем LambdaA по умолчанию
                    // и сбрасываем ручное переопределение, чтобы λ соответствовала новому материалу.
                    // Для слоёв под трубой UpdateLambda() вызывается отдельно с учётом УГВ.
                    if (_material != null)
                    {
                        IsLambdaOverridden = false;
                        CalculatedLambda = _material.LambdaA;
                    }
                }
            }
        }

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        public double Thickness
        {
            get => _thickness;
            set
            {
                if (_thickness != value)
                {
                    _thickness = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CalculatedR));
                }
            }
        }

        /// <summary>
        /// Теплопроводность (λ), Вт/м·К
        /// Автоматически подставляется из Material, но может быть изменена вручную
        /// </summary>
        public double CalculatedLambda
        {
            get => _calculatedLambda;
            set
            {
                if (_calculatedLambda != value)
                {
                    _calculatedLambda = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CalculatedR));
                }
            }
        }

        /// <summary>
        /// Признак того, что λ изменена вручную
        /// </summary>
        public bool IsLambdaOverridden
        {
            get => _isLambdaOverridden;
            set { _isLambdaOverridden = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Позиция слоя относительно трубы (над/под)
        /// </summary>
        public LayerPosition Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Индекс слоя в коллекции (автоматически переиндексируется). Для LayersAbovePipe 0 = поверхность, растёт к трубе; для LayersBelowPipe 0 = ближайший к трубе, растёт к грунту.
        /// </summary>
        public int Order
        {
            get => _order;
            set { _order = value; OnPropertyChanged(); }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}