# Task 4.2: Адаптировать CollectorViewModel.cs

**Этап:** 4 - ViewModels  
**Приоритет:** Средний  
**Статус:** К разработке  
**Зависимости:** Task 1.1 (ValveType), Task 3.2 (CircuitsCalculator)

---

## 1. Цель задачи

Адаптировать существующую `CollectorViewModel` для работы с CircuitRow.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-06 | Подбор коллектора | Свойство ValveType, Summary |

---

## 3. Изменяемые файлы

### 3.1. CollectorViewModel.cs

**Путь:** `src/ViewModels/Hydraulics/CollectorViewModel.cs`

**Изменения:**

Добавить свойства:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// ViewModel для коллектора
    /// </summary>
    public partial class CollectorViewModel : ObservableObject
    {
        #region Observable Properties

        /// <summary>
        /// Номер коллектора
        /// </summary>
        [ObservableProperty]
        private int _collectorNumber;

        /// <summary>
        /// Список контуров
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CircuitRow> _circuits = new();

        /// <summary>
        /// Итоги расчёта коллектора
        /// </summary>
        [ObservableProperty]
        private CollectorSummary _summary;

        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;

        /// <summary>
        /// Можно добавить контур
        /// </summary>
        public bool CanAddCircuit => Circuits.Count < 12;

        /// <summary>
        /// Количество контуров
        /// </summary>
        public int CircuitCount => Circuits.Count;

        #endregion

        #region Constructor

        public CollectorViewModel()
        {
            // Инициализация с 4 контурами по умолчанию
            for (int i = 0; i < 4; i++)
            {
                Circuits.Add(new CircuitRow
                {
                    CircuitNumber = i + 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    PipeSpacing_cm = 20,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                });
            }
        }

        #endregion
    }
}
```

---

## 4. Критерии приёмки

- [ ] Свойство `Circuits` добавлено (ObservableCollection<CircuitRow>)
- [ ] Свойство `Summary` добавлено (CollectorSummary)
- [ ] Свойство `ValveType` добавлено
- [ ] Свойство `CanAddCircuit` работает (максимум 12 контуров)
- [ ] INotifyPropertyChanged реализовано
- [ ] XML-документация добавлена

---

## 5. Примечания

- CollectorViewModel используется в CircuitsViewModel
- Каждый коллектор может содержать до 12 контуров

---

## 6. Связанные задачи

- Task 1.1: ValveType — используется в CollectorViewModel
- Task 4.1: CircuitsViewModel — использует CollectorViewModel

---

*Дата создания: 2026-03-17*