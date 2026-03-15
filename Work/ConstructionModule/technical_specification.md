# Техническое задание: Модуль "Конструктор конструкции" ("Пирог")

**Проект:** Калькулятор снеготаяния РЕХАУ  
**Версия ТЗ:** 1.0  
**Дата:** 2026-03-15  
**Статус:** На рассмотрении

---

## 1. Общее описание

### 1.1. Краткое описание задачи

Модуль "Конструктор конструкции" ("Пирог") предназначен для визуального проектирования слоёв конструкции системы снеготаяния. Пользователь задаёт слои материалов над трубой и под трубой, система автоматически рассчитывает термические сопротивления и передаёт данные в модуль теплового расчёта.

### 1.2. Цель разработки

Создать интерактивный модуль для:
1. Визуализации слоёв конструкции ("пирога") с динамической отрисовкой
2. Ввода и редактирования параметров слоёв материалов
3. Автоматического расчёта термических сопротивлений R1 и R2
4. Интеграции с модулем теплового расчёта через интерфейс `IConstructionData`

### 1.3. Связь с существующей системой

**Существующие компоненты:**
- `src/Models/Thermal/IConstructionData.cs` — интерфейс для передачи данных (R1Total, R2Total, LambdaE)
- `src/Models/Thermal/ConstructionData.cs` — заглушка с фиксированными значениями
- `src/Services/Thermal/ThermalCalculator.cs` — использует R1Total, R2Total, LambdaE
- `src/ViewModels/Thermal/ThermalViewModel.cs` — подписан на `ConstructionDataChanged`

**Интеграционные точки:**
- Замена заглушки `ConstructionData` на реальную реализацию
- Подписка `ThermalViewModel` на события изменения конструкции
- Передача рассчитанных R1, R2, LambdaE в `ThermalParameters`

---

## 2. Список юзер-кейсов

### UC-01: Добавление слоя материала

#### 2.1. Название
Добавление слоя материала в конструкцию

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. Приложение запущено
2. Модуль "Конструктор конструкции" открыт
3. База материалов `data/materials_db.json` загружена

#### 2.4. Основной сценарий
1. Пользователь нажимает кнопку "Добавить слой над трубой" или "Добавить слой под трубой"
2. Система добавляет новый слой с материалом по умолчанию ("Бетон плотный")
3. Система отображает новый слой в списке слоёв
4. Система обновляет визуализацию "пирога" (Canvas/ItemsControl)
5. Система пересчитывает термические сопротивления R1/R2
6. Система вызывает событие `DataChanged` для уведомления ThermalViewModel

#### 2.5. Альтернативные сценарии
- **А1: Добавление первого слоя** — если список пуст, слой создаётся с толщиной 50 мм
- **А2: Добавление слоя между существующими** — пользователь может изменить порядок слоёв

#### 2.6. Постусловия
- Новый слой добавлен в коллекцию слоёв
- Визуализация обновлена
- R1/R2 пересчитаны
- Событие `DataChanged` отправлено

#### 2.7. Критерии приёмки
- ✅ Кнопка "Добавить слой" отображается и активна
- ✅ Новый слой создаётся с материалом по умолчанию
- ✅ Визуализация обновляется в течение 100 мс
- ✅ R1/R2 пересчитываются автоматически

---

### UC-02: Выбор материала из справочника

#### 2.1. Название
Выбор материала слоя из справочника материалов

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. Слой создан (UC-01)
2. База материалов загружена

#### 2.4. Основной сценарий
1. Пользователь открывает выпадающий список материалов в строке слоя
2. Система отображает список материалов из `materials_db.json`:
   - Песок (λА=0.4, λБ=2.0)
   - Грунт (λА=0.5, λБ=1.5)
   - Бетон на каменном щебне (λА=1.5, λБ=1.5)
   - Бетон на песке (λА=0.7, λБ=0.7)
   - Бетон плотный (λА=1.5, λБ=1.5)
   - Железобетон (λА=1.7, λБ=1.7)
   - Асфальтобетон (λА=1.5, λБ=1.5)
   - Щебень/Гравий (λА=0.7, λБ=1.8)
   - Цементно-песчаная стяжка (λА=1.2, λБ=1.2)
   - Пенополистирол ЭППС (λА=0.035, λБ=0.035)
   - Асфальт (λА=0.75, λБ=0.75)
3. Пользователь выбирает материал
4. Система автоматически подставляет значение λ в зависимости от УГВ:
   - Если УГВ < 1 м → λБ (влажные условия)
   - Если УГВ ≥ 1 м → λА (сухие условия)
5. Система пересчитывает R1/R2
6. Система обновляет визуализацию

#### 2.5. Альтернативные сценарии
- **А1: Ручное редактирование λ** — пользователь может изменить значение λ вручную
- **А2: Изменение УГВ** — при изменении УГВ все λ пересчитываются автоматически

#### 2.6. Постусловия
- Материал слоя изменён
- λ обновлена в соответствии с УГВ
- R1/R2 пересчитаны

#### 2.7. Критерии приёмки
- ✅ Выпадающий список содержит все материалы из `materials_db.json`
- ✅ Автоподстановка λ работает корректно
- ✅ При изменении УГВ λ пересчитывается для всех слоёв под трубой
- ✅ Ручное редактирование λ сохраняется

---

### UC-03: Задание толщины слоя

#### 2.1. Название
Задание толщины слоя материала

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. Слой создан (UC-01)
2. Материал выбран (UC-02)

#### 2.4. Основной сценарий
1. Пользователь вводит толщину слоя в числовом поле (мм)
2. Система валидирует ввод:
   - Минимум: 10 мм
   - Максимум: 1000 мм
3. При корректном вводе:
   - Система обновляет визуализацию (высота слоя пропорциональна толщине)
   - Система пересчитывает R = d / λ / 1000
   - Система вызывает событие `DataChanged`

#### 2.5. Альтернативные сценарии
- **А1: Некорректный ввод** — система отображает ошибку валидации
- **А2: Толщина вне диапазона** — система подсвечивает поле красным

#### 2.6. Постусловия
- Толщина слоя сохранена
- Визуализация обновлена
- R1/R2 пересчитаны

#### 2.7. Критерии приёмки
- ✅ Числовое поле принимает только цифры
- ✅ Валидация диапазона 10–1000 мм работает
- ✅ Высота слоя на визуализации пропорциональна толщине
- ✅ R пересчитывается по формуле R = d / λ / 1000

---

### UC-04: Удаление слоя

#### 2.1. Название
Удаление слоя из конструкции

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. В конструкции есть хотя бы один слой
2. Пользователь выбрал слой для удаления

#### 2.4. Основной сценарий
1. Пользователь нажимает кнопку "Удалить" в строке слоя
2. Система запрашивает подтверждение удаления
3. Пользователь подтверждает удаление
4. Система удаляет слой из коллекции
5. Система обновляет визуализацию
6. Система пересчитывает R1/R2
7. Система вызывает событие `DataChanged`

#### 2.5. Альтернативные сценарии
- **А1: Отмена удаления** — пользователь отменяет операцию
- **А2: Удаление последнего слоя** — система предупреждает о невозможности расчёта

#### 2.6. Постусловия
- Слой удалён из коллекции
- Визуализация обновлена
- R1/R2 пересчитаны

#### 2.7. Критерии приёмки
- ✅ Кнопка "Удалить" отображается для каждого слоя
- ✅ Диалог подтверждения появляется перед удалением
- ✅ После удаления визуализация корректна

---

### UC-05: Учёт уровня грунтовых вод (УГВ)

#### 2.1. Название
Автоматический учёт уровня грунтовых вод

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. Конструкция содержит слои под трубой
2. Параметр УГВ задан (по умолчанию ≥ 1 м)

#### 2.4. Основной сценарий
1. Пользователь вводит значение УГВ (м)
2. Система проверяет условие:
   - Если УГВ < 1 м → влажные условия (λБ)
   - Если УГВ ≥ 1 м → сухие условия (λА)
3. Для всех слоёв **под трубой** система обновляет λ:
   - При УГВ < 1 м: λ = λБ из `materials_db.json`
   - При УГВ ≥ 1 м: λ = λА из `materials_db.json`
4. Для слоёв **над трубой** λ не изменяется (всегда λА)
5. Система пересчитывает R2 для слоёв под трубой
6. Система вызывает событие `DataChanged`

#### 2.5. Альтернативные сценарии
- **А1: Изменение УГВ** — при изменении УГВ все λ под трубой пересчитываются
- **А2: Ручное переопределение λ** — пользователь может вручную изменить λ, но при следующем изменении УГВ она пересчитается

#### 2.6. Постусловия
- λ для слоёв под трубой соответствует УГВ
- R2 пересчитан
- Визуализация обновлена

#### 2.7. Критерии приёмки
- ✅ Поле УГВ принимает значения от 0 до 10 м
- ✅ При УГВ < 1 м для слоёв под трубой используется λБ
- ✅ При УГВ ≥ 1 м для слоёв под трубой используется λА
- ✅ Слои над трубой всегда используют λА

---

### UC-06: Валидация минимальной стяжки

#### 2.1. Название
Проверка минимальной толщины стяжки над трубой

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. Конструкция содержит слои над трубой
2. Труба задана (наружный диаметр известен)

#### 2.4. Основной сценарий
1. Пользователь задаёт слои над трубой
2. Система автоматически проверяет:
   - Суммарная толщина слоёв над трубой ≥ 40 мм (без нагрузок)
   - Суммарная толщина слоёв над трубой ≥ 50 мм (при нагрузках)
3. Если условие не выполнено:
   - Система отображает предупреждение
   - Система подсвечивает поле "Толщина стяжки"
   - Флаг `IsValid = false`

#### 2.5. Альтернативные сценарии
- **А1: Включён флаг нагрузок** — минимальная толщина 50 мм
- **А2: Валидация пройдена** — предупреждение скрыто

#### 2.6. Постусловия
- Валидация выполнена
- Предупреждение отображено (если есть)

#### 2.7. Критерии приёмки
- ✅ Минимальная толщина стяжки проверяется автоматически
- ✅ При нагрузках минимальная толщина = 50 мм
- ✅ Без нагрузок минимальная толщина = 40 мм
- ✅ Предупреждение отображается при нарушении

---

### UC-07: Проверка ограничений по материалам

#### 2.1. Название
Проверка ограничений по материалам

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)
- Модуль климатических данных (IClimateData)

#### 2.3. Предусловия
1. Конструкция содержит слои
2. Климатические данные загружены (температура наружного воздуха)

#### 2.4. Основной сценарий
1. Пользователь выбирает материал "Бетон"
2. Система проверяет:
   - Если температура подачи > 50°C → предупреждение "Бетон: макс. температура подачи 50°C"
3. Пользователь выбирает материал "Асфальт"
4. Система проверяет:
   - Если температура наружного воздуха ≤ -15°C → предупреждение "Асфальт не применяется при t ≤ -15°C"
5. Предупреждения отображаются в панели валидации

#### 2.5. Альтернативные сценарии
- **А1: Климатические данные не загружены** — проверка пропускается
- **А2: Материал без ограничений** — предупреждение не отображается

#### 2.6. Постусловия
- Ограничения проверены
- Предупреждения отображены (если есть)

#### 2.7. Критерии приёмки
- ✅ Бетон: предупреждение при температуре подачи > 50°C
- ✅ Асфальт: предупреждение при температуре наружного воздуха ≤ -15°C
- ✅ Предупреждения отображаются в реальном времени

---

### UC-08: Визуализация конструкции ("Пирог")

#### 2.1. Название
Интерактивная визуализация слоёв конструкции

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
1. Модуль открыт
2. Конструкция содержит слои

#### 2.4. Основной сценарий
1. Система отображает Canvas/ItemsControl с визуализацией "пирога"
2. Визуализация содержит:
   - Слои над трубой (сверху вниз)
   - Труба (фиксированная позиция)
   - Слои под трубой (снизу вверх)
3. Высота каждого слоя пропорциональна толщине
4. Цвет слоя соответствует категории материала:
   - Бетон — серый
   - Песок/Грунт — коричневый
   - Изоляция — жёлтый
   - Покрытие (асфальт) — чёрный
5. При изменении слоёв визуализация перерисовывается

#### 2.5. Альтернативные сценарии
- **А1: Пустая конструкция** — отображается только труба
- **А2: Очень толстый слой** — масштабирование с прокруткой

#### 2.6. Постусловия
- Визуализация актуальна

#### 2.7. Критерии приёмки
- ✅ Canvas/ItemsControl отображает слои корректно
- ✅ Высота слоя пропорциональна толщине
- ✅ Цвет соответствует категории материала
- ✅ Труба отображается фиксированно
- ✅ При изменении слоёв визуализация обновляется < 100 мс

---

### UC-09: Интеграция с ThermalViewModel

#### 2.1. Название
Передача данных конструкции в модуль теплового расчёта

#### 2.2. Актёры
- ConstructionViewModel (отправитель)
- ThermalViewModel (получатель)
- IConstructionData (интерфейс)

#### 2.3. Предусловия
1. Конструкция валидна
2. ThermalViewModel подписан на `ConstructionDataChanged`

#### 2.4. Основной сценарий
1. Пользователь изменяет конструкцию (добавляет/удаляет/редактирует слой)
2. ConstructionViewModel пересчитывает:
   - R1Total = Σ(R_i для слоёв над трубой)
   - R2Total = Σ(R_i для слоёв под трубой)
   - LambdaE = λ материала вокруг трубы (стяжка/бетон)
3. ConstructionViewModel вызывает `RaiseDataChanged`
4. ThermalViewModel получает событие `ConstructionDataChanged`
5. ThermalViewModel обновляет `ThermalParameters`:
   - `parameters.R1Total = _constructionData.R1Total`
   - `parameters.R2Total = _constructionData.R2Total`
   - `parameters.LambdaE = _constructionData.LambdaE`
6. ThermalViewModel сбрасывает результат расчёта

#### 2.5. Альтернативные сценарии
- **А1: Конструкция невалидна** — `IsValid = false`, ThermalViewModel отображает ошибку
- **А2: Автоматический пересчёт** — если включён флаг автопересчёта

#### 2.6. Постусловия
- ThermalParameters обновлены
- ThermalViewModel уведомлён

#### 2.7. Критерии приёмки
- ✅ R1Total рассчитывается как сумма R слоёв над трубой
- ✅ R2Total рассчитывается как сумма R слоёв под трубой
- ✅ LambdaE = λ материала вокруг трубы
- ✅ Событие `DataChanged` вызывается при любом изменении
- ✅ ThermalViewModel корректно обрабатывает событие

---

## 3. Модель данных

### 3.1. Класс Material (Материал)

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
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
        /// Теплопроводность в сухих условиях (УГВ >= 1м), Вт/м·К
        /// </summary>
        public double LambdaA { get; set; }
        
        /// <summary>
        /// Теплопроводность во влажных условиях (УГВ < 1м), Вт/м·К
        /// </summary>
        public double LambdaB { get; set; }
        
        /// <summary>
        /// Категория материала (бетон, груннт, изоляция, покрытие)
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Примечания
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// Максимальная температура подачи (для бетона = 50°C, null = без ограничений)
        /// </summary>
        public double? MaxSupplyTemperature { get; set; }
        
        /// <summary>
        /// Минимальная температура наружного воздуха (для асфальта = -15°C, null = без ограничений)
        /// </summary>
        public double? MinAirTemperature { get; set; }
    }
}
```

### 3.2. Класс Layer (Слой)

```csharp
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
        public double Lambda { get; set; }
        
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
        public double ThermalResistance => Thickness / Lambda / 1000.0;
    }
    
    /// <summary>
    /// Позиция слоя относительно трубы
    /// </summary>
    public enum LayerPosition
    {
        /// <summary>
        /// Над трубой (к поверхности)
        /// </summary>
        AbovePipe,
        
        /// <summary>
        /// Под трубой (к грунту)
        /// </summary>
        BelowPipe
    }
}
```

### 3.3. Класс Construction (Конструкция)

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Конструкция ("Пирог") системы снеготаяния
    /// </summary>
    public class Construction : IConstructionData
    {
        /// <summary>
        /// Слои над трубой (к поверхности)
        /// </summary>
        public ObservableCollection<Layer> LayersAbovePipe { get; } = new();
        
        /// <summary>
        /// Слои под трубой (к грунту)
        /// </summary>
        public ObservableCollection<Layer> LayersBelowPipe { get; } = new();
        
        /// <summary>
        /// Уровень грунтовых вод, м
        /// </summary>
        public double GroundwaterLevel { get; set; } = 2.0;
        
        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        public bool HasLoads { get; set; } = false;
        
        /// <summary>
        /// Материал вокруг трубы (для LambdaE)
        /// </summary>
        public Material? MaterialAroundPipe { get; set; }
        
        // === IConstructionData ===
        
        /// <summary>
        /// Суммарное термическое сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        public double R1Total => LayersAbovePipe.Sum(l => l.ThermalResistance);
        
        /// <summary>
        /// Суммарное термическое сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        public double R2Total => LayersBelowPipe.Sum(l => l.ThermalResistance);
        
        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// </summary>
        public double LambdaE => MaterialAroundPipe?.LambdaA ?? 1.6;
        
        /// <summary>
        /// Признак валидности данных конструкции
        /// </summary>
        public bool IsValid => ValidateConstruction();
        
        /// <summary>
        /// Событие изменения данных
        /// </summary>
        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;
        
        // === Методы ===
        
        /// <summary>
        /// Добавить слой над трубой
        /// </summary>
        public void AddLayerAbovePipe(Material material, double thickness)
        {
            var layer = new Layer
            {
                Material = material,
                Thickness = thickness,
                Lambda = GetLambdaForLayer(material, LayerPosition.AbovePipe),
                Position = LayerPosition.AbovePipe,
                Order = LayersAbovePipe.Count
            };
            LayersAbovePipe.Add(layer);
            OnDataChanged();
        }
        
        /// <summary>
        /// Добавить слой под трубой
        /// </summary>
        public void AddLayerBelowPipe(Material material, double thickness)
        {
            var layer = new Layer
            {
                Material = material,
                Thickness = thickness,
                Lambda = GetLambdaForLayer(material, LayerPosition.BelowPipe),
                Position = LayerPosition.BelowPipe,
                Order = LayersBelowPipe.Count
            };
            LayersBelowPipe.Add(layer);
            OnDataChanged();
        }
        
        /// <summary>
        /// Удалить слой
        /// </summary>
        public void RemoveLayer(Layer layer)
        {
            if (layer.Position == LayerPosition.AbovePipe)
                LayersAbovePipe.Remove(layer);
            else
                LayersBelowPipe.Remove(layer);
            
            OnDataChanged();
        }
        
        /// <summary>
        /// Обновить λ для всех слоёв под трубой при изменении УГВ
        /// </summary>
        public void UpdateLambdaForGroundwater()
        {
            foreach (var layer in LayersBelowPipe)
            {
                if (!layer.IsLambdaOverridden)
                {
                    layer.Lambda = GetLambdaForLayer(layer.Material, LayerPosition.BelowPipe);
                }
            }
            OnDataChanged();
        }
        
        /// <summary>
        /// Получить λ для слоя в зависимости от УГВ
        /// </summary>
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
        
        /// <summary>
        /// Валидация конструкции
        /// </summary>
        private bool ValidateConstruction()
        {
            // Проверка минимальной стяжки над трубой
            var minThickness = HasLoads ? 50.0 : 40.0;
            var totalAbove = LayersAbovePipe.Sum(l => l.Thickness);
            if (totalAbove < minThickness)
                return false;
            
            // Проверка наличия слоёв
            if (LayersAbovePipe.Count == 0 && LayersBelowPipe.Count == 0)
                return false;
            
            // Проверка толщины слоёв
            foreach (var layer in LayersAbovePipe.Concat(LayersBelowPipe))
            {
                if (layer.Thickness < 10 || layer.Thickness > 1000)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
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
        
        private void OnDataChanged()
        {
            RaiseDataChanged("Construction", null, null, IsValid);
        }
    }
}
```

---

## 4. Интерфейс (View и ViewModel)

### 4.1. ConstructionViewModel

```csharp
namespace SnowMeltingCalculator.ViewModels.Construction
{
    public partial class ConstructionViewModel : ObservableObject
    {
        private readonly IMaterialService _materialService;
        private readonly Construction _construction;
        private readonly IClimateData _climateData;
        
        // === Observable Properties ===
        
        [ObservableProperty]
        private ObservableCollection<Layer> _layersAbovePipe = new();
        
        [ObservableProperty]
        private ObservableCollection<Layer> _layersBelowPipe = new();
        
        [ObservableProperty]
        private ObservableCollection<Material> _availableMaterials = new();
        
        [ObservableProperty]
        private double _groundwaterLevel = 2.0;
        
        [ObservableProperty]
        private bool _hasLoads = false;
        
        [ObservableProperty]
        private string _validationMessage = string.Empty;
        
        [ObservableProperty]
        private bool _isValid = true;
        
        // === Computed Properties ===
        
        public double R1Total => _construction.R1Total;
        public double R2Total => _construction.R2Total;
        public double LambdaE => _construction.LambdaE;
        
        // === Commands ===
        
        [RelayCommand]
        private void AddLayerAbovePipe() { ... }
        
        [RelayCommand]
        private void AddLayerBelowPipe() { ... }
        
        [RelayCommand]
        private void RemoveLayer(Layer layer) { ... }
        
        [RelayCommand]
        private void UpdateGroundwaterLevel() { ... }
        
        // === Methods ===
        
        partial void OnGroundwaterLevelChanged(double value)
        {
            _construction.GroundwaterLevel = value;
            _construction.UpdateLambdaForGroundwater();
            Validate();
        }
        
        private void Validate() { ... }
    }
}
```

### 4.2. ConstructionView (WPF UserControl)

```xml
<UserControl x:Class="SnowMeltingCalculator.Views.Construction.ConstructionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="1*" />
        </Grid.ColumnDefinitions>
        
        <!-- Левая панель: Визуализация "Пирога" -->
        <Border Grid.Column="0" Background="White" BorderBrush="Gray" BorderThickness="1">
            <Canvas x:Name="ConstructionCanvas" />
        </Border>
        
        <!-- Правая панель: Ввод данных -->
        <StackPanel Grid.Column="1" Margin="10">
            
            <!-- Параметры УГВ -->
            <TextBlock Text="Уровень грунтовых вод (м):" />
            <TextBox Text="{Binding GroundwaterLevel, UpdateSourceTrigger=PropertyChanged}" />
            
            <!-- Флаг нагрузок -->
            <CheckBox Content="Наличие нагрузок на покрытие" 
                      IsChecked="{Binding HasLoads}" />
            
            <!-- Слои над трубой -->
            <TextBlock Text="Слои над трубой:" FontWeight="Bold" Margin="0,10,0,5" />
            <Button Content="Добавить слой" Command="{Binding AddLayerAbovePipeCommand}" />
            <DataGrid ItemsSource="{Binding LayersAbovePipe}" AutoGenerateColumns="False">
                <DataGrid.Columns>
                    <DataGridComboBoxColumn Header="Материал" 
                                           ItemsSource="{Binding AvailableMaterials}"
                                           DisplayMemberPath="Name"
                                           SelectedValueBinding="{Binding Material}" />
                    <DataGridTextColumn Header="Толщина (мм)" 
                                       Binding="{Binding Thickness, UpdateSourceTrigger=PropertyChanged}" />
                    <DataGridTextColumn Header="λ (Вт/м·К)" 
                                       Binding="{Binding Lambda, UpdateSourceTrigger=PropertyChanged}" />
                    <DataGridTemplateColumn Header="Действия">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="Удалить" 
                                        Command="{Binding DataContext.RemoveLayerCommand, 
                                                 RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}" />
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
            
            <!-- Слои под трубой -->
            <TextBlock Text="Слои под трубой:" FontWeight="Bold" Margin="0,10,0,5" />
            <Button Content="Добавить слой" Command="{Binding AddLayerBelowPipeCommand}" />
            <DataGrid ItemsSource="{Binding LayersBelowPipe}" AutoGenerateColumns="False">
                <!-- Аналогично -->
            </DataGrid>
            
            <!-- Результаты -->
            <TextBlock Text="Результаты:" FontWeight="Bold" Margin="0,10,0,5" />
            <TextBlock Text="{Binding R1Total, StringFormat='R1 = {0:F4} м²·К/Вт'}" />
            <TextBlock Text="{Binding R2Total, StringFormat='R2 = {0:F4} м²·К/Вт'}" />
            <TextBlock Text="{Binding LambdaE, StringFormat='λE = {0:F2} Вт/м·К'}" />
            
            <!-- Валидация -->
            <TextBlock Text="{Binding ValidationMessage}" Foreground="Red" />
            
        </StackPanel>
    </Grid>
    
</UserControl>
```

---

## 5. Валидация

### 5.1. Правила валидации

| Параметр | Правило | Сообщение об ошибке |
|----------|---------|---------------------|
| Толщина слоя | 10 ≤ d ≤ 1000 мм | "Толщина слоя должна быть от 10 до 1000 мм" |
| Суммарная толщина над трубой (без нагрузок) | ≥ 40 мм | "Минимальная стяжка над трубой: 40 мм" |
| Суммарная толщина над трубой (с нагрузками) | ≥ 50 мм | "Минимальная стяжка над трубой при нагрузках: 50 мм" |
| УГВ | 0 ≤ УГВ ≤ 10 м | "Уровень грунтовых вод должен быть от 0 до 10 м" |
| Бетон + температура подачи | T_подачи ≤ 50°C | "Бетон: максимальная температура подачи 50°C" |
| Асфальт + температура воздуха | T_воздуха > -15°C | "Асфальт не применяется при температуре ≤ -15°C" |

### 5.2. Класс валидации

```csharp
namespace SnowMeltingCalculator.Services.Construction
{
    public class ConstructionValidator
    {
        private readonly IClimateData _climateData;
        
        public ValidationResult Validate(Construction construction, double supplyTemperature)
        {
            var errors = new List<string>();
            
            // Проверка минимальной стяжки
            var minThickness = construction.HasLoads ? 50.0 : 40.0;
            var totalAbove = construction.LayersAbovePipe.Sum(l => l.Thickness);
            if (totalAbove < minThickness)
            {
                errors.Add($"Минимальная стяжка над трубой: {minThickness} мм (текущая: {totalAbove} мм)");
            }
            
            // Проверка толщины слоёв
            foreach (var layer in construction.LayersAbovePipe.Concat(construction.LayersBelowPipe))
            {
                if (layer.Thickness < 10 || layer.Thickness > 1000)
                {
                    errors.Add($"Толщина слоя '{layer.Material.Name}' должна быть от 10 до 1000 мм");
                }
            }
            
            // Проверка материалов
            foreach (var layer in construction.LayersAbovePipe)
            {
                // Бетон: макс. температура подачи 50°C
                if (layer.Material.Category == "бетон" && supplyTemperature > 50)
                {
                    errors.Add($"Бетон: максимальная температура подачи 50°C (текущая: {supplyTemperature}°C)");
                }
                
                // Асфальт: не применять при t ≤ -15°C
                if (layer.Material.Name.Contains("Асфальт") && _climateData.AirTemperature <= -15)
                {
                    errors.Add($"Асфальт не применяется при температуре наружного воздуха ≤ -15°C");
                }
            }
            
            // Проверка УГВ
            if (construction.GroundwaterLevel < 0 || construction.GroundwaterLevel > 10)
            {
                errors.Add("Уровень грунтовых вод должен быть от 0 до 10 м");
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }
}
```

---

## 6. Нефункциональные требования

### 6.1. Производительность
- Время отклика при добавлении/удалении слоя: < 100 мс
- Время пересчёта R1/R2: < 50 мс
- Время отрисовки визуализации: < 100 мс
- Максимальное количество слоёв: 20 (над трубой) + 20 (под трубой)

### 6.2. Надёжность
- Автосохранение конструкции в SQLite при каждом изменении
- Восстановление последней конструкции при запуске
- Валидация всех входных данных

### 6.3. Безопасность
- Данные хранятся локально (SQLite)
- Нет передачи данных в сеть

### 6.4. Локализация
- Интерфейс: RU (основной), EN, DE
- Формат чисел: локаль пользователя
- Единицы измерения: мм, Вт/м·К, м²·К/Вт

---

## 7. Ограничения и допущения

### 7.1. Технические ограничения
- Платформа: Windows 10+
- Фреймворк: .NET 8, WPF
- Архитектура: MVVM (CommunityToolkit.Mvvm)
- DI: Microsoft.Extensions.DependencyInjection

### 7.2. Бизнес-ограничения
- Соответствие СП 131.13330.2025 (климатология)
- Соответствие EN 1264-2 (тепловой расчёт)
- Материалы только из `materials_db.json`

### 7.3. Допущения
- Труба всегда находится в одном слое (стяжка/бетон)
- Слои однородны по толщине
- Теплопроводность постоянна в диапазоне температур
- УГВ не изменяется в течение расчёта

---

## 8. Открытые вопросы

### 8.1. Требующие уточнения у пользователя

1. **Материал вокруг трубы (LambdaE)**
   - Вопрос: Как определить материал вокруг трубы?
   - Варианты:
     - a) Всегда первый слой над трубой
     - b) Отдельный выбор пользователя
     - c) Автоматически по категории "бетон/стяжка"
   - **Рекомендация**: Вариант (c) — автоматически определять по категории материала первого слоя над трубой.

2. **Порядок слоёв**
   - Вопрос: Нужна ли возможность перетаскивания слоёв (drag-and-drop)?
   - **Рекомендация**: Да, реализовать drag-and-drop для изменения порядка слоёв.

3. **Сохранение/загрузка конструкции**
   - Вопрос: Нужна ли возможность сохранять/загружать конструкции из файла?
   - **Рекомендация**: Да, реализовать экспорт/импорт в JSON.

4. **Предустановленные шаблоны**
   - Вопрос: Нужны ли предустановленные шаблоны конструкций?
   - Примеры: "Типовая парковка", "Пешеходная дорожка", "Въезд в гараж"
   - **Рекомендация**: Да, добавить 3-5 типовых шаблонов.

5. **Визуализация трубы**
   - Вопрос: Нужна ли возможность выбора типа трубы в модуле конструкции?
   - **Рекомендация**: Нет, тип трубы выбирается в ThermalViewModel. В модуле конструкции труба отображается схематично.

---

## 9. Приложение: Структура базы материалов

### 9.1. Файл `data/materials_db.json`

```json
{
  "meta": {
    "source": "Расчет1этап.xlsx, вкладка Materials",
    "version": "1.0",
    "date": "2026-01-21",
    "description": "База материалов для расчёта систем снеготаяния"
  },
  "materials": [
    {
      "id": 1,
      "name": "Песок",
      "lambda_A": 0.4,
      "lambda_B": 2.0,
      "category": "грунт",
      "notes": "При высоком УГВ теплопроводность резко возрастает"
    },
    // ... остальные материалы
  ],
  "usage_rules": {
    "ugw_condition": "Уровень грунтовых вод (УГВ)",
    "lambda_A": "Используется при УГВ >= 1м (сухие условия)",
    "lambda_B": "Используется при УГВ < 1м (влажные условия)"
  }
}
```

### 9.2. Категории материалов

| Категория | Материалы | Цвет на визуализации |
|-----------|-----------|----------------------|
| бетон | Бетон на каменном щебне, Бетон на песце, Бетон плотный, Железобетон | Серый |
| груннт | Песок, Грунт | Коричневый |
| изоляция | Пенополистирол ЭППС | Жёлтый |
| покрытие | Асфальтобетон, Асфальт | Чёрный |
| подстилающий | Щебень/Гравий | Серый |
| стяжка | Цементно-песчаная стяжка | Светло-серый |

---

## 10. Приложение: Формулы расчёта

### 10.1. Термическое сопротивление слоя

```
R = d / λ / 1000    [м²·К/Вт]

где:
- d — толщина слоя, мм
- λ — теплопроводность материала, Вт/м·К
```

### 10.2. Суммарное сопротивление над трубой

```
R1Total = Σ(R_i) для всех слоёв над трубой
```

### 10.3. Суммарное сопротивление под трубой

```
R2Total = Σ(R_i) для всех слоёв под трубой
```

### 10.4. Выбор λ в зависимости от УГВ

```
λ = {
    λА, если УГВ >= 1 м (сухие условия)
    λБ, если УГВ < 1 м (влажные условия)
}

Примечание: Только для слоёв ПОД трубой. Слои НАД трубой всегда используют λА.
```

---

## 11. Решения по открытым вопросам

### 11.1. Материал вокруг трубы (LambdaE)
**Решение:** Первый слой над трубой (вариант a)

LambdaE определяется автоматически как теплопроводность первого слоя над трубой. Если слой над трубой отсутствует, используется значение по умолчанию 1.6 Вт/м·К (бетон).

### 11.2. Перетаскивание слоёв (drag-and-drop)
**Решение:** Нет

В первой версии порядок слоёв определяется только их позицией в списке. Пользователь может удалить и добавить слой заново.

### 11.3. Сохранение/загрузка конструкций
**Решение:** Да

Реализовать:
- Экспорт конструкции в JSON-файл
- Импорт конструкции из JSON-файла
- Хранение в SQLite (проекты)

### 11.4. Предустановленные шаблоны
**Решение:** Да

Предустановленные шаблоны конструкций:
- **Типовая парковка** — стандартная конструкция для парковок
- **Пешеходная дорожка** — облегчённая конструкция
- **Въезд в гараж** — усиленная конструкция с арматурой

### 11.5. Выбор типа трубы
**Решение:** Нет

Труба уже выбрана в модуле теплового расчёта (ThermalViewModel). Конструктор конструкции получает тип трубы из ThermalParameters и отображает её схематично.

---

**Конец документа**