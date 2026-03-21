# Техническое задание: Изменение интерфейса вкладки контура

**Версия:** 1.1  
**Дата:** 2026-03-20  
**Модуль:** Hydraulics  
**Статус:** На разработку

---

## 1. Общее описание

### 1.1. Цель разработки
Модификация интерфейса вкладки "Контур" (CircuitsView) для соответствия немецкому образцу расчёта (Berechnung.png). Добавление информационных блоков с входными данными, результатами расчёта и расширение таблицы контуров.

### 1.2. Связь с существующей системой
- **Входные данные:** CircuitsViewModel, HydraulicInputData, ThermalViewModel
- **Модели:** CircuitRow, CircuitTemperatureResult, CollectorSummary
- **Сервисы:** CircuitsCalculator, GlycolDataService
- **Формулы:** docs/Formulas_Snegotayanie.md (разделы 11.1-11.12)

### 1.3. Текущее состояние
- Таблица контуров содержит 15 столбцов
- Отсутствует шапка с общими входными данными
- Отсутствует блок результатов коллектора
- Число Рейнольдса (Re) уже отображается
- Коэффициент трения (λ) рассчитывается, но не отображается

---

## 2. Список юзер-кейсов

### UC-1: Просмотр входных данных гидравлики

#### 2.1. Название
Просмотр входных данных гидравлического расчёта (EINGABEN - Allgemein)

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выполнен тепловой расчёт
- Выбран тип трубы в ThermalViewModel
- Выбран тип гликоля и концентрация

#### 2.4. Основной сценарий
1. Пользователь открывает вкладку "Контур"
2. Система отображает блок "Входные данные" в левой части информационной панели
3. Блок содержит следующие поля:
   - **Температура подачи (T_VL):** значение из ThermalViewModel.SupplyTemperature
   - **Температура обратки (T_RL):** значение из ThermalViewModel.ReturnTemperature
   - **Тип трубы:** наименование из ThermalViewModel.SelectedPipe.Name (например, "RAUTHERM S 20×2.0")
   - **Наружный диаметр трубы:** D_ext × s мм (например, "20.0 × 2.0")
   - **Внутренний диаметр трубы:** d_inner мм (вычисляется: D_ext - 2×s)
   - **Шероховатость трубы:** ε = 0.007 мм (константа для PE-Xa)
   - **Тип гликоля:** Ethylene/Propylene
   - **Концентрация гликоля:** % (например, "50 %")

#### 2.5. Альтернативные сценарии
- **А1: Тепловой расчёт не выполнен** — отображаются нулевые/пустые значения
- **А2: Труба не выбрана** — отображается "Труба не выбрана"

#### 2.6. Постусловия
- Пользователь видит все входные параметры гидравлического расчёта

#### 2.7. Критерии приёмки
- ✅ Блок отображается в левой части информационной панели
- ✅ Температура подачи отображается с точностью 1 знак после запятой
- ✅ Температура обратки отображается с точностью 1 знак после запятой
- ✅ Тип трубы отображается в формате "RAUTHERM S 20×2.0"
- ✅ Диаметры отображаются в формате "20.0 × 2.0 мм"
- ✅ Шероховатость отображается как "0.007 мм"
- ✅ Тип гликоля отображается на русском языке
- ✅ Данные трубы берутся из ThermalViewModel.SelectedPipe

---

### UC-2: Просмотр данных укладки и мощности

#### 2.1. Название
Просмотр данных укладки и мощности (Verlege- und Leistungsdaten)

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выполнен тепловой расчёт
- Выбран шаг укладки трубы

#### 2.4. Основной сценарий
1. Пользователь открывает вкладку "Контур"
2. Система отображает блок "Данные укладки и мощности" в центральной части информационной панели
3. Блок содержит следующие поля:
   - **Удельная мощность вверх (q_up):** значение из ThermalViewModel.Result.PowerUp, Вт/м²
   - **Удельная мощность вниз (q_down):** значение из ThermalViewModel.Result.PowerDown, Вт/м²
   - **Шаг укладки контура (VA_HK):** значение из ThermalViewModel.PipeSpacing, см
   - **Шаг укладки подводки (VA_ZU):** значение из CircuitsViewModel.InputData.SupplySpacing_cm, см
   - **Доля потерь в подводке (%QZU):** значение из CircuitsViewModel.InputData.SupplyHeatPercent, %

#### 2.5. Альтернативные сценарии
- **А1: Тепловой расчёт не выполнен** — отображаются нулевые значения

#### 2.6. Постусловия
- Пользователь видит параметры укладки и мощности

#### 2.7. Критерии приёмки
- ✅ Блок отображается в центральной части информационной панели
- ✅ Мощность вверх отображается с точностью 1 знак после запятой
- ✅ Мощность вниз отображается с точностью 1 знак после запятой
- ✅ Шаг укладки отображается в см с точностью 0 знаков
- ✅ Доля потерь отображается в % с точностью 0 знаков

---

### UC-3: Просмотр результатов коллектора

#### 2.1. Название
Просмотр результатов расчёта коллектора (ERGEBNISSE - Verteiler)

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выполнен гидравлический расчёт
- Есть хотя бы один активный контур

#### 2.4. Основной сценарий
1. Пользователь открывает вкладку "Контур"
2. Система отображает блок "Результаты коллектора" в правой части информационной панели
3. Блок содержит следующие поля:
   - **Количество контуров:** CollectorSummary.CircuitCount, шт
   - **Общая длина контуров:** CollectorSummary.TotalPipeLength, м
   - **Общая мощность:** CollectorSummary.TotalPower, Вт
   - **Общий расход:** CollectorSummary.TotalFlowRate, л/ч
   - **Потери давления (рабочая T):** CollectorSummary.PressureLoss_Operating_mbar, мбар
   - **Потери давления (расчётная T):** CollectorSummary.PressureLoss_Cold_mbar, мбар
   - **Тип коллектора:** CollectorData.CollectorType (например, "HKV-D")

#### 2.5. Альтернативные сценарии
- **А1: Нет активных контуров** — отображаются нулевые значения
- **А2: Превышение давления (>320 мбар)** — выделение красным цветом

#### 2.6. Постусловия
- Пользователь видит итоговые результаты расчёта коллектора

#### 2.7. Критерии приёмки
- ✅ Блок отображается в правой части информационной панели
- ✅ Количество контуров отображается целым числом
- ✅ Общая длина отображается с точностью 1 знак после запятой
- ✅ Общая мощность отображается с точностью 1 знак после запятой
- ✅ Общий расход отображается с точностью 1 знак после запятой
- ✅ Потери давления отображаются с точностью 1 знак после запятой
- ✅ При превышении 320 мбар — красное выделение

---

### UC-4: Переключение режима расчёта

#### 2.1. Название
Переключение между режимами расчёта (Рабочая/Расчётная температура)

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выполнен гидравлический расчёт
- Есть хотя бы один активный контур

#### 2.4. Основной сценарий
1. Пользователь нажимает кнопку "Режим расчёта"
2. Система переключает режим между "Рабочая температура" и "Расчётная температура"
3. Система обновляет отображаемые результаты:
   - В блоке "Результаты коллектора" — потери давления
   - В таблице контуров — Re, λ, скорость, уд.потери, Δp контур, Δp клапан
4. Результаты отображаются для активного режима

#### 2.5. Альтернативные сценарии
- **А1: Расчёт не выполнен** — кнопка неактивна

#### 2.6. Постусловия
- Пользователь видит результаты для выбранного режима

#### 2.7. Критерии приёмки
- ✅ Кнопка "Режим расчёта" отображается в интерфейсе
- ✅ При нажатии — переключение между режимами
- ✅ Результаты обновляются мгновенно (< 100 мс)
- ✅ Активный режим подсвечивается

---

### UC-5: Просмотр расширенной таблицы контуров

#### 2.1. Название
Просмотр таблицы контуров с коэффициентом трения λ

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выполнен гидравлический расчёт
- Есть хотя бы один контур

#### 2.4. Основной сценарий
1. Пользователь открывает вкладку "Контур"
2. Система отображает таблицу контуров
3. Таблица содержит столбцы (в порядке слева направо):
   - **№** — номер контура (только чтение)
   - **Длина (м)** — длина греющего контура (редактируется)
   - **Подводка (м)** — длина подводки (редактируется)
   - **Площадь (м²)** — площадь контура (редактируется)
   - **Шаг (см)** — шаг укладки (редактируется)
   - **Мощность (Вт)** — мощность контура (только чтение)
   - **Расход (л/ч)** — расход теплоносителя (только чтение)
   - **Скорость (м/с)** — скорость потока (только чтение)
   - **Re** — число Рейнольдса (только чтение)
   - **λ** — коэффициент трения (только чтение) — **НОВЫЙ СТОЛБЕЦ**
   - **Режим** — режим течения (только чтение)
   - **Уд.потери (Па/м)** — удельные потери (только чтение)
   - **Δp контур (мбар)** — потери в трубе контура (только чтение)
   - **Δp клапан (мбар)** — потери в вентиле (только чтение)
   - **Обороты** — обороты балансировочного клапана (только чтение)

#### 2.5. Альтернативные сценарии
- **А1: Пустой контур (длина = 0)** — отображается "—" (прочерк) во всех вычисляемых полях
- **А2: Расчёт не выполнен** — отображаются прочерки

#### 2.6. Постусловия
- Пользователь видит все параметры контура включая λ

#### 2.7. Критерии приёмки
- ✅ Столбец λ отображается после Re
- ✅ Значение λ отображается с точностью 4 знака после запятой (например, "0.0423")
- ✅ Значение берётся из CircuitTemperatureResult.FrictionFactor
- ✅ Значение обновляется при переключении режима (рабочая/расчётная температура)
- ✅ Фон столбца — серый (только чтение)
- ✅ Пустые контуры отображаются с "—" во всех вычисляемых полях

---

## 3. Макет интерфейса

### 3.1. Структура вкладки "Контур"

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Таблица контуров                              [+ Добавить коллектор]      │
│                                                [- Удалить коллектор]       │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Коллектор №1] [Коллектор №2] [Коллектор №3] [Коллектор №4]                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐│
│  │ ВХОДНЫЕ ДАННЫЕ       │ │ ДАННЫЕ УКЛАДКИ       │ │ РЕЗУЛЬТАТЫ           ││
│  │ (EINGABEN)           │ │ И МОЩНОСТИ           │ │ КОЛЛЕКТОРА           ││
│  │                      │ │ (Verlege- und        │ │ (ERGEBNISSE)         ││
│  │ T_подачи: 47.0 °C    │ │ Leistungsdaten)      │ │                      ││
│  │ T_обратки: 35.0 °C   │ │                      │ │ Контуров: 4 шт       ││
│  │ Труба: RAUTHERM S    │ │ q_вверх: 256.6 Вт/м² │ │ Длина: 297.0 м       ││
│  │ D_нар: 20.0×2.0 мм   │ │ q_вниз: 4.6 Вт/м²    │ │ Мощность: 11184.6 Вт ││
│  │ D_вн: 16.0 мм        │ │ Шаг: 20.0 см         │ │ Расход: 992.7 л/ч    ││
│  │ ε: 0.007 мм          │ │ Подводка: 5.0 см     │ │ Δp (раб): 233.6 мбар ││
│  │ Гликоль: Этилен      │ │ Потери: 10.0 %       │ │ Δp (расч): 308.2 мбар││
│  │ Конц.: 50 %          │ │                      │ │ Тип: HKV-D           ││
│  └──────────────────────┘ └──────────────────────┘ └──────────────────────┘│
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Параметры теплоносителя                                            │   │
│  │ [Тип гликоли ▼] [Концентрация: 50%] [Режим: Рабочая температура]   │   │
│  │ [Шаг подводки: 5 см] [Полезное тепло: 10%] [Тип коллектора ▼]      │   │
│  │ [Кнопка: Режим расчёта ▼]                                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Свойства теплоносителя                                             │   │
│  │ Рабочая температура: 41.0°C    │ Расчётная температура: -15.0°C    │   │
│  │ Плотность: 1053.0 кг/м³       │ Плотность: 1085.0 кг/м³           │   │
│  │ Вязкость: 2.16 мм²/с          │ Вязкость: 18.17 мм²/с            │   │
│  │ Теплоёмкость: 3.21 кДж/(кг·К) │ Теплоёмкость: 3.05 кДж/(кг·К)    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ № │ Длина │ Подводка │ Площадь │ Шаг │ Мощность │ Расход │ Скорость│   │
│  │   │  (м)  │   (м)    │  (м²)   │(см) │  (Вт)   │ (л/ч)  │  (м/с)  │   │
│  ├────┼───────┼──────────┼─────────┼─────┼──────────┼────────┼─────────┤   │
│  │ 1  │ 100.0 │   10.0   │  20.0   │ 20  │ 5246.0  │ 560.2  │  0.59   │   │
│  │ 2  │  80.0 │   12.0   │  16.0   │ 20  │ 4196.8  │ 448.1  │  0.47   │   │
│  │ 3  │  60.0 │    8.0   │  12.0   │ 20  │ 3147.6  │ 336.1  │  0.35   │   │
│  │ 4  │  57.0 │   10.0   │  11.4   │ 20  │ 2990.2  │ 319.3  │  0.34   │   │
│  │ 5  │   —   │    —     │    —    │  —  │    —    │   —    │   —     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Re    │   λ    │ Режим      │ Уд.потери │ Δp контур │ Δp клапан │ Об. │   │
│  ├───────┼────────┼────────────┼───────────┼───────────┼───────────┼─────┤   │
│  │ 3551  │ 0.0423 │ Турбулент. │  592.0    │   233.6   │   57.3    │ 3.2 │   │
│  │ 2841  │ 0.0438 │ Переходный │  421.5    │   187.2   │   36.6    │ 2.8 │   │
│  │ 2131  │ 0.0452 │ Переходный │  298.3    │   140.6   │   20.6    │ 2.1 │   │
│  │ 2039  │ 0.0458 │ Переходный │  275.1    │   133.2   │   18.5    │ 1.9 │   │
│  │   —   │   —    │     —      │    —      │     —     │    —      │  —  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [+ Добавить контур] [Рассчитать]                                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2. Расположение блоков

**Принятое решение:** Вариант A — В один ряд (слева направо)

Блоки располагаются горизонтально в следующем порядке:

```
┌──────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐
│   ВХОДНЫЕ ДАННЫЕ     │ │   ДАННЫЕ УКЛАДКИ      │ │   РЕЗУЛЬТАТЫ         │
│   (EINGABEN)         │ │   И МОЩНОСТИ          │ │   КОЛЛЕКТОРА         │
│                      │ │   (Verlege- und       │ │   (ERGEBNISSE)       │
│   Левый блок         │ │   Leistungsdaten)     │ │                      │
│                      │ │   Центральный блок    │ │   Правый блок        │
└──────────────────────┘ └──────────────────────┘ └──────────────────────┘
```

**Порядок блоков (слева направо):**
1. **ВХОДНЫЕ ДАННЫЕ** (левый блок) — параметры трубы, температуры, гликоль
2. **ДАННЫЕ УКЛАДКИ И МОЩНОСТИ** (центральный блок) — шаг укладки, мощности
3. **РЕЗУЛЬТАТЫ КОЛЛЕКТОРА** (правый блок) — итоговые результаты

**Под блоками:**
4. **Параметры теплоносителя** (существующий блок — оставить без изменений)
5. **Свойства теплоносителя** (существующий блок — оставить без изменений)
6. **Таблица контуров** (существующая таблица + новый столбец λ)

---

## 4. Детализация изменений

### 4.1. Новый блок "ВХОДНЫЕ ДАННЫЕ"

#### 4.1.1. Источники данных

| Поле | Источник | Формат |
|------|----------|--------|
| Температура подачи | ThermalViewModel.Result.SupplyTemperature | F1 °C |
| Температура обратки | ThermalViewModel.Result.ReturnTemperature | F1 °C |
| Тип трубы | ThermalViewModel.SelectedPipe.Name | Строка |
| Наружный диаметр | ThermalViewModel.SelectedPipe.OuterDiameter | F1 × F1 мм |
| Толщина стенки | ThermalViewModel.SelectedPipe.WallThickness | F1 мм |
| Внутренний диаметр | HydraulicInputData.InnerDiameter | F1 мм |
| Шероховатость | Константа 0.007 мм | F3 мм |
| Тип гликоля | CircuitsViewModel.GlycolType | Строка |
| Концентрация | CircuitsViewModel.GlycolConcentration | F0 % |

#### 4.1.2. XAML-структура

```xml
<!-- Блок ВХОДНЫЕ ДАННЫЕ (левый блок) -->
<Border Grid.Column="0"
        Background="#FFF3E0"
        BorderBrush="#FF9800"
        BorderThickness="1"
        Padding="10"
        Margin="0,0,5,10"
        CornerRadius="5">
    <StackPanel>
        <TextBlock Text="ВХОДНЫЕ ДАННЫЕ"
                   FontWeight="Bold"
                   FontSize="12"
                   Foreground="#E65100"
                   Margin="0,0,0,8"/>
        <TextBlock Text="EINGABEN - Allgemein"
                   FontSize="10"
                   Foreground="#757575"
                   Margin="0,0,0,8"/>
        
        <!-- Температура подачи -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="T_подачи:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding SupplyTemperature, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" °C" FontSize="11"/>
        </StackPanel>
        
        <!-- Температура обратки -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="T_обратки:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding ReturnTemperature, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" °C" FontSize="11"/>
        </StackPanel>
        
        <!-- Тип трубы -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Труба:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PipeType}" FontSize="11"/>
        </StackPanel>
        
        <!-- Наружный диаметр -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="D_нар:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding OuterDiameter, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text="×" Margin="2,0,2,0" FontSize="11"/>
            <TextBlock Text="{Binding WallThickness, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" мм" FontSize="11"/>
        </StackPanel>
        
        <!-- Внутренний диаметр -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="D_вн:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding InnerDiameter, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" мм" FontSize="11"/>
        </StackPanel>
        
        <!-- Шероховатость -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="ε:" Width="100" FontSize="11"/>
            <TextBlock Text="0.007" FontSize="11"/>
            <TextBlock Text=" мм" FontSize="11"/>
        </StackPanel>
        
        <!-- Тип гликоля -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Гликоль:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding GlycolTypeName}" FontSize="11"/>
        </StackPanel>
        
        <!-- Концентрация -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Конц.:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding GlycolConcentration, StringFormat=F0}" FontSize="11"/>
            <TextBlock Text=" %" FontSize="11"/>
        </StackPanel>
    </StackPanel>
</Border>
```

### 4.2. Новый блок "ДАННЫЕ УКЛАДКИ И МОЩНОСТИ"

#### 4.2.1. Источники данных

| Поле | Источник | Формат |
|------|----------|--------|
| Уд. мощность вверх | ThermalViewModel.Result.PowerUp | F1 Вт/м² |
| Уд. мощность вниз | ThermalViewModel.Result.PowerDown | F1 Вт/м² |
| Шаг укладки | ThermalViewModel.PipeSpacing / 10 | F1 см |
| Шаг подводки | CircuitsViewModel.InputData.SupplySpacing_cm | F1 см |
| Доля потерь | CircuitsViewModel.InputData.SupplyHeatPercent | F0 % |

#### 4.2.2. XAML-структура

```xml
<!-- Блок ДАННЫЕ УКЛАДКИ И МОЩНОСТИ (центральный блок) -->
<Border Grid.Column="1"
        Background="#E8F5E9"
        BorderBrush="#4CAF50"
        BorderThickness="1"
        Padding="10"
        Margin="5,0,5,10"
        CornerRadius="5">
    <StackPanel>
        <TextBlock Text="ДАННЫЕ УКЛАДКИ"
                   FontWeight="Bold"
                   FontSize="12"
                   Foreground="#2E7D32"
                   Margin="0,0,0,8"/>
        <TextBlock Text="Verlege- und Leistungsdaten"
                   FontSize="10"
                   Foreground="#757575"
                   Margin="0,0,0,8"/>
        
        <!-- Уд. мощность вверх -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="q_вверх:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PowerUp, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" Вт/м²" FontSize="11"/>
        </StackPanel>
        
        <!-- Уд. мощность вниз -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="q_вниз:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PowerDown, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" Вт/м²" FontSize="11"/>
        </StackPanel>
        
        <!-- Шаг укладки -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Шаг:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PipeSpacing_cm, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" см" FontSize="11"/>
        </StackPanel>
        
        <!-- Шаг подводки -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Подводка:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding SupplySpacing_cm, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" см" FontSize="11"/>
        </StackPanel>
        
        <!-- Доля потерь -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Потери:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding SupplyHeatPercent, StringFormat=F0}" FontSize="11"/>
            <TextBlock Text=" %" FontSize="11"/>
        </StackPanel>
    </StackPanel>
</Border>
```

### 4.3. Новый блок "РЕЗУЛЬТАТЫ КОЛЛЕКТОРА"

#### 4.3.1. Источники данных

| Поле | Источник | Формат |
|------|----------|--------|
| Количество контуров | CollectorSummary.CircuitCount | F0 шт |
| Общая длина | CollectorSummary.TotalPipeLength | F1 м |
| Общая мощность | CollectorSummary.TotalPower | F1 Вт |
| Общий расход | CollectorSummary.TotalFlowRate | F1 л/ч |
| Потери (рабочая T) | CollectorSummary.PressureLoss_Operating_mbar | F1 мбар |
| Потери (расчётная T) | CollectorSummary.PressureLoss_Cold_mbar | F1 мбар |
| Тип коллектора | CollectorData.CollectorType | Строка |
| Kv клапана | CollectorSummary.Kv | F2 м³/ч |

#### 4.3.2. XAML-структура

```xml
<!-- Блок РЕЗУЛЬТАТЫ КОЛЛЕКТОРА (правый блок) -->
<Border Grid.Column="2"
        Background="#E3F2FD"
        BorderBrush="#2196F3"
        BorderThickness="1"
        Padding="10"
        Margin="5,0,0,10"
        CornerRadius="5">
    <StackPanel>
        <TextBlock Text="РЕЗУЛЬТАТЫ"
                   FontWeight="Bold"
                   FontSize="12"
                   Foreground="#1565C0"
                   Margin="0,0,0,8"/>
        <TextBlock Text="ERGEBNISSE - Verteiler"
                   FontSize="10"
                   Foreground="#757575"
                   Margin="0,0,0,8"/>
        
        <!-- Количество контуров -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Контуров:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding Summary.CircuitCount}" FontSize="11"/>
            <TextBlock Text=" шт" FontSize="11"/>
        </StackPanel>
        
        <!-- Общая длина -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Длина:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding Summary.TotalPipeLength, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" м" FontSize="11"/>
        </StackPanel>
        
        <!-- Общая мощность -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Мощность:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding Summary.TotalPower, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" Вт" FontSize="11"/>
        </StackPanel>
        
        <!-- Общий расход -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Расход:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding Summary.TotalFlowRate, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" л/ч" FontSize="11"/>
        </StackPanel>
        
        <!-- Потери (рабочая T) -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Δp (раб):" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding Summary.PressureLoss_Operating_mbar, StringFormat=F1}"
                       FontSize="11"
                       Foreground="{Binding Summary.PressureLoss_Operating_mbar, Converter={StaticResource PressureColorConverter}}"/>
            <TextBlock Text=" мбар" FontSize="11"/>
        </StackPanel>
        
        <!-- Потери (расчётная T) -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Δp (расч):" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding Summary.PressureLoss_Cold_mbar, StringFormat=F1}"
                       FontSize="11"
                       Foreground="{Binding Summary.PressureLoss_Cold_mbar, Converter={StaticResource PressureColorConverter}}"/>
            <TextBlock Text=" мбар" FontSize="11"/>
        </StackPanel>
        
        <!-- Тип коллектора -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Тип:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding CollectorType}" FontSize="11"/>
        </StackPanel>
    </StackPanel>
</Border>
```

### 4.4. Цветовая индикация превышения давления

#### 4.4.1. Требования к цветовой индикации

**Принятое решение:** Вариант A — Цветовое выделение при превышении давления (>320 мбар)

| Условие | Цвет текста | Цвет фона |
|---------|-------------|-----------|
| Давление ≤ 320 мбар | Зелёный (#2E7D32) | Прозрачный |
| Давление > 320 мбар | Красный (#D32F2F) | Прозрачный |

#### 4.4.2. Конвертер для цветовой индикации

```csharp
/// <summary>
/// Конвертер для определения цвета текста в зависимости от давления
/// </summary>
public class PressureColorConverter : IValueConverter
{
    private const double PressureLimit = 320.0; // мбар
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pressure)
        {
            return pressure > PressureLimit 
                ? new SolidColorBrush(Color.FromRgb(211, 47, 47))  // Красный
                : new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Зелёный
        }
        return new SolidColorBrush(Colors.Black);
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

#### 4.4.3. Регистрация конвертера в App.xaml

```xml
<Application.Resources>
    <local:PressureColorConverter x:Key="PressureColorConverter"/>
</Application.Resources>
```

#### 4.4.4. Применение в XAML

```xml
<!-- Потери давления с цветовой индикацией -->
<TextBlock Text="{Binding Summary.PressureLoss_Cold_mbar, StringFormat=F1}"
           FontSize="11"
           Foreground="{Binding Summary.PressureLoss_Cold_mbar, 
                        Converter={StaticResource PressureColorConverter}}"/>
```

### 4.5. Отображение пустых контуров

#### 4.5.1. Требования к отображению

**Принятое решение:** Вариант A — Отображать строки с "—"

| Поле | Значение для пустого контура |
|------|------------------------------|
| Длина (м) | 0 или редактируемое |
| Подводка (м) | 0 или редактируемое |
| Площадь (м²) | 0 или редактируемое |
| Шаг (см) | 0 или редактируемое |
| Мощность (Вт) | "—" |
| Расход (л/ч) | "—" |
| Скорость (м/с) | "—" |
| Re | "—" |
| λ | "—" |
| Режим | "—" |
| Уд.потери (Па/м) | "—" |
| Δp контур (мбар) | "—" |
| Δp клапан (мбар) | "—" |
| Обороты | "—" |

#### 4.5.2. Конвертер для отображения прочерка

```csharp
/// <summary>
/// Конвертер для отображения "—" вместо null или 0 для вычисляемых полей
/// </summary>
public class EmptyValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "—";
            
        if (value is double d && d == 0)
            return "—";
            
        return value;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### 4.6. Переключение режима расчёта

#### 4.6.1. Требования к переключению

**Принятое решение:** Показывается активный режим. При переключении кнопкой "Режим расчета" (Расчётная/Рабочая температура) — меняются результаты.

#### 4.6.2. Реализация переключателя

```xml
<!-- Кнопка переключения режима -->
<ComboBox SelectedIndex="{Binding CalculationMode, Converter={StaticResource EnumToIndexConverter}}"
          Width="200"
          Margin="5,0">
    <ComboBoxItem Content="Рабочая температура"/>
    <ComboBoxItem Content="Расчётная температура"/>
</ComboBox>
```

#### 4.6.3. Свойство в ViewModel

```csharp
/// <summary>
/// Режим расчёта (рабочая/расчётная температура)
/// </summary>
public CalculationMode CalculationMode
{
    get => _calculationMode;
    set
    {
        if (_calculationMode != value)
        {
            _calculationMode = value;
            OnPropertyChanged();
            UpdateResultsForCurrentMode();
        }
    }
}

/// <summary>
/// Обновить результаты для текущего режима
/// </summary>
private void UpdateResultsForCurrentMode()
{
    foreach (var circuit in Circuits)
    {
        circuit.CurrentResult = CalculationMode == CalculationMode.Operating
            ? circuit.OperatingResult
            : circuit.ColdResult;
    }
    
    // Обновить итоговые потери давления
    OnPropertyChanged(nameof(TotalPressureLoss));
}
```

### 4.7. Изменение таблицы контуров

#### 4.7.1. Добавление столбца λ

Новый столбец добавляется после столбца Re:

```xml
<!-- Коэффициент трения λ (только чтение) -->
<DataGridTextColumn Header="λ"
                    Binding="{Binding CurrentResult.FrictionFactor, Mode=OneWay, StringFormat=F4, TargetNullValue='—'}"
                    IsReadOnly="True"
                    Width="60"
                    ElementStyle="{StaticResource ReadOnlyCellStyle}">
    <DataGridTextColumn.Header>
        <TextBlock Text="λ" ToolTip="Коэффициент гидравлического трения"/>
    </DataGridTextColumn.Header>
</DataGridTextColumn>
```

#### 4.7.2. Обновлённый порядок столбцов

| № | Столбец | Binding | Формат | Только чтение |
|---|---------|---------|--------|---------------|
| 1 | № | CircuitNumber | F0 | Да |
| 2 | Длина (м) | CircuitLength | F1 | Нет |
| 3 | Подводка (м) | SupplyLength | F1 | Нет |
| 4 | Площадь (м²) | CircuitArea | F1 | Нет |
| 5 | Шаг (см) | PipeSpacing_cm | F0 | Нет |
| 6 | Мощность (Вт) | Power | F0 | Да |
| 7 | Расход (л/ч) | FlowRate | F1 | Да |
| 8 | Скорость (м/с) | Velocity | F3 | Да |
| 9 | Re | CurrentResult.ReynoldsNumber | F0 | Да |
| 10 | **λ** | **CurrentResult.FrictionFactor** | **F4** | **Да** |
| 11 | Режим | FlowRegimeDescription | - | Да |
| 12 | Уд.потери (Па/м) | CurrentResult.PressureLossPerMeter | F1 | Да |
| 13 | Δp контур (мбар) | CurrentResult.CircuitPipeLoss | F1 | Да |
| 14 | Δp клапан (мбар) | CurrentResult.ValveLoss | F1 | Да |
| 15 | Обороты | ValveTurns | F1 | Да |

---

## 5. Изменения в ViewModel

### 5.1. Новые свойства в CircuitsViewModel

```csharp
// === Входные данные для отображения ===

/// <summary>
/// Температура подачи, °C
/// Берётся из ThermalViewModel.Result.SupplyTemperature
/// </summary>
public double SupplyTemperature => _thermalViewModel.Result?.SupplyTemperature ?? 0;

/// <summary>
/// Температура обратки, °C
/// Берётся из ThermalViewModel.Result.ReturnTemperature
/// </summary>
public double ReturnTemperature => _thermalViewModel.Result?.ReturnTemperature ?? 0;

/// <summary>
/// Тип трубы (наименование)
/// Берётся из ThermalViewModel.SelectedPipe.Name
/// </summary>
public string PipeType => _thermalViewModel.SelectedPipe?.Name ?? "Труба не выбрана";

/// <summary>
/// Наружный диаметр трубы, мм
/// Берётся из ThermalViewModel.SelectedPipe.OuterDiameter
/// </summary>
public double OuterDiameter => _thermalViewModel.SelectedPipe?.OuterDiameter ?? 0;

/// <summary>
/// Толщина стенки трубы, мм
/// Берётся из ThermalViewModel.SelectedPipe.WallThickness
/// </summary>
public double WallThickness => _thermalViewModel.SelectedPipe?.WallThickness ?? 0;

/// <summary>
/// Внутренний диаметр трубы, мм
/// Вычисляется: D_ext - 2 × s
/// </summary>
public double InnerDiameter => InputData.InnerDiameter;

/// <summary>
/// Шероховатость трубы, мм (константа для PE-Xa)
/// </summary>
public double PipeRoughness => 0.007;

/// <summary>
/// Тип гликоля (на русском)
/// </summary>
public string GlycolTypeName => GlycolType switch
{
    GlycolType.Ethylene => "Этиленгликоль",
    GlycolType.Propylene => "Пропиленгликоль",
    _ => "Не указан"
};

/// <summary>
/// Удельная мощность вверх, Вт/м²
/// Берётся из ThermalViewModel.Result.PowerUp
/// </summary>
public double PowerUp => _thermalViewModel.Result?.PowerUp ?? 0;

/// <summary>
/// Удельная мощность вниз, Вт/м²
/// Берётся из ThermalViewModel.Result.PowerDown
/// </summary>
public double PowerDown => _thermalViewModel.Result?.PowerDown ?? 0;

/// <summary>
/// Шаг укладки, см
/// Берётся из ThermalViewModel.PipeSpacing
/// </summary>
public double PipeSpacing_cm => _thermalViewModel.PipeSpacing / 10.0;
```

### 5.2. Обновление при изменении ThermalViewModel

Добавить уведомления об изменении свойств в методе `OnThermalViewModelPropertyChanged`:

```csharp
private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(ThermalViewModel.Result))
    {
        UpdateFromThermalModule();
        
        // Уведомить об изменении свойств для отображения
        OnPropertyChanged(nameof(SupplyTemperature));
        OnPropertyChanged(nameof(ReturnTemperature));
        OnPropertyChanged(nameof(PowerUp));
        OnPropertyChanged(nameof(PowerDown));
        OnPropertyChanged(nameof(PipeType));
        OnPropertyChanged(nameof(OuterDiameter));
        OnPropertyChanged(nameof(WallThickness));
        OnPropertyChanged(nameof(InnerDiameter));
    }
    else if (e.PropertyName == nameof(ThermalViewModel.PipeSpacing))
    {
        UpdatePipeSpacingInCircuits();
        OnPropertyChanged(nameof(PipeSpacing_cm));
    }
    else if (e.PropertyName == nameof(ThermalViewModel.SelectedPipe))
    {
        // Обновить внутренний диаметр при смене трубы
        UpdateInnerDiameter();
        OnPropertyChanged(nameof(PipeType));
        OnPropertyChanged(nameof(OuterDiameter));
        OnPropertyChanged(nameof(WallThickness));
        OnPropertyChanged(nameof(InnerDiameter));
    }
}
```

### 5.3. Переключение режима расчёта

```csharp
/// <summary>
/// Режим расчёта (рабочая/расчётная температура)
/// </summary>
public CalculationMode CalculationMode
{
    get => _calculationMode;
    set
    {
        if (_calculationMode != value)
        {
            _calculationMode = value;
            OnPropertyChanged();
            UpdateResultsForCurrentMode();
        }
    }
}

/// <summary>
/// Обновить результаты для текущего режима
/// </summary>
private void UpdateResultsForCurrentMode()
{
    foreach (var circuit in Circuits)
    {
        circuit.CurrentResult = CalculationMode == CalculationMode.Operating
            ? circuit.OperatingResult
            : circuit.ColdResult;
    }
    
    // Обновить итоговые потери давления
    OnPropertyChanged(nameof(TotalPressureLoss));
}
```

---

## 6. Нефункциональные требования

### 6.1. Производительность
- Отображение блоков должно происходить мгновенно (< 100 мс)
- Обновление таблицы при расчёте — < 500 мс для 12 контуров
- Переключение режима расчёта — < 100 мс

### 6.2. Локализация
- Все тексты на русском языке
- Дублирование заголовков на немецком (как в образце)
- Формат чисел: десятичный разделитель — запятая

### 6.3. Доступность
- Поддержка масштабирования интерфейса (DPI)
- Поддержка высокой контрастности
- Tooltips для всех столбцов таблицы

### 6.4. Цветовая индикация
- Зелёный цвет (#2E7D32) для нормального давления (≤ 320 мбар)
- Красный цвет (#D32F2F) для превышения давления (> 320 мбар)
- Прочерк ("—") для пустых/нулевых вычисляемых значений

---

## 7. Ограничения и допущения

### 7.1. Технические ограничения
- WPF .NET 8
- MVVM-паттерн
- Существующая структура CircuitsViewModel

### 7.2. Бизнес-ограничения
- Соответствие немецкому образцу Berechnung.png
- Формулы из docs/Formulas_Snegotayanie.md

### 7.3. Допущения
- Шероховатость трубы — константа 0.007 мм для PE-Xa
- Тип трубы берётся из ThermalViewModel.SelectedPipe
- Данные теплового расчёта валидны
- Пустые контуры отображаются с "—" в вычисляемых полях

---

## 8. Критерии приёмки

### 8.1. Функциональные критерии
- ✅ Блок "ВХОДНЫЕ ДАННЫЕ" отображается в левой части панели
- ✅ Блок "ДАННЫЕ УКЛАДКИ И МОЩНОСТИ" отображается в центральной части панели
- ✅ Блок "РЕЗУЛЬТАТЫ КОЛЛЕКТОРА" отображается в правой части панели
- ✅ Блоки расположены в один ряд (слева направо)
- ✅ Столбец λ добавлен в таблицу контуров
- ✅ Значение λ отображается с точностью 4 знака
- ✅ Значение λ обновляется при переключении режима
- ✅ При превышении давления > 320 мбар — красный цвет текста
- ✅ При нормальном давлении ≤ 320 мбар — зелёный цвет текста
- ✅ Пустые контуры отображаются с "—" в вычисляемых полях
- ✅ Кнопка "Режим расчёта" переключает между рабочей и расчётной температурой
- ✅ Данные трубы берутся из ThermalViewModel.SelectedPipe

### 8.2. Нефункциональные критерии
- ✅ Время отображения блоков < 100 мс
- ✅ Время обновления таблицы < 500 мс
- ✅ Время переключения режима < 100 мс
- ✅ Все тексты на русском языке
- ✅ Tooltips для всех столбцов

### 8.3. Критерии качества кода
- ✅ MVVM-паттерн соблюдён
- ✅ Нет дублирования кода
- ✅ Свойства уведомляют об изменениях
- ✅ Unit-тесты для новых свойств ViewModel
- ✅ Конвертеры для цветовой индикации

---

## 9. План реализации

### 9.1. Этап 1: ViewModel (1 день)
- Добавить свойства для отображения входных данных
- Добавить свойства для отображения результатов коллектора
- Добавить свойство CalculationMode для переключения режима
- Добавить уведомления об изменениях
- Добавить конвертеры для цветовой индикации

### 9.2. Этап 2: XAML (1 день)
- Изменить макет на горизонтальное расположение блоков
- Добавить блок "ВХОДНЫЕ ДАННЫЕ" (левый)
- Добавить блок "ДАННЫЕ УКЛАДКИ И МОЩНОСТИ" (центральный)
- Добавить блок "РЕЗУЛЬТАТЫ КОЛЛЕКТОРА" (правый)
- Добавить столбец λ в таблицу
- Добавить цветовую индикацию для давления
- Добавить отображение "—" для пустых контуров

### 9.3. Этап 3: Тестирование (0.5 дня)
- Unit-тесты для новых свойств
- UI-тесты для отображения блоков
- Тесты цветовой индикации
- Тесты переключения режима
- Проверка соответствия образцу

### 9.4. Этап 4: Документация (0.5 дня)
- Обновление user guide
- Скриншоты интерфейса

---

**Итого:** 3 дня

---

## 10. Ссылки

- **Образец:** Work/Berechnung.png
- **Формулы:** docs/Formulas_Snegotayanie.md (разделы 11.1-11.12)
- **Текущий код:** src/Views/Hydraulics/CircuitsView.xaml
- **ViewModel:** src/ViewModels/Hydraulics/CircuitsViewModel.cs
- **Модель:** src/Models/Hydraulics/CircuitRow.cs