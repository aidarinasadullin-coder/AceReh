# Task 6.1: Обновить UI

**Статус:** Ожидает выполнения  
**Приоритет:** Высокий  
**Связанные UC:** UC-4  
**Зависимости:** Task 2.1 (новые свойства), Task 5.1 (единицы Па)  

---

## 1. Цель задачи

Обновить таблицу контуров в `CircuitsView.xaml` для отображения новых колонок: `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `zu_drosseln` в Паскалях (целые числа).

---

## 2. Проблема

**Текущее поведение:**
- Таблица отображает старые колонки: `Δp контур (мбар)`, `Δp клапан (мбар)`, `Δp сумма (мбар)`
- Значения в миллибарах с десятичными дробями
- Нет отдельной колонки для `DpVerteiler`
- Нет колонки для `zu_drosseln`

**Ожидаемое поведение:**
- Таблица отображает новые колонки: `DpRohr (Па)`, `DpVerteiler (Па)`, `DpVent (Па)`, `DpGesamt (Па)`, `zu_drosseln (Па)`
- Значения в Паскалях как целые числа
- Обороты отображаются как дроби (2/4, 2 1/2)

---

## 3. Связанные юзер-кейсы

### UC-4: Отображение результатов в таблице

**Таблица содержит колонки:**
- № (номер контура)
- Длина (м)
- Подводка (м)
- Площадь (м²)
- Шаг (см)
- Мощность (Вт)
- Расход (л/ч)
- Скорость (м/с)
- Re (число Рейнольдса)
- λ (коэффициент трения)
- Режим
- Уд.потери (Па/м)
- **DpRohr (Па)** — потери в трубе
- **DpVerteiler (Па)** — потери в распределителе
- **DpVent (Па)** — потери в вентиле
- **DpGesamt (Па)** — суммарные потери
- **zu_drosseln (Па)** — дросселирование
- **Обороты** — настройка клапана (дробь)

**Критерии приёмки:**
- ✅ DpRohr отображается в Па (целые числа)
- ✅ DpVerteiler отображается в Па (целые числа)
- ✅ DpVent отображается в Па (целые числа)
- ✅ DpGesamt отображается в Па (целые числа)
- ✅ zu_drosseln отображается в Па (целые числа)
- ✅ Обороты отображаются как дроби (2/4, 2 1/2)

---

## 4. Изменения в файлах

### 4.1. Файл: `src/Views/Hydraulics/CircuitsView.xaml`

#### 4.1.1. Найти текущие колонки давления

**Текущий код (примерно строки 850-900):**

```xml
<!-- Потери в контуре (мбар) -->
<DataGridTextColumn Header="Δp контур (мбар)"
                    Binding="{Binding CurrentResult.CircuitPipeLoss_mbar, Mode=OneWay, StringFormat=F1}"
                    IsReadOnly="True" Width="90">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<!-- Потери на клапане (мбар) -->
<DataGridTextColumn Header="Δp клапан (мбар)"
                    Binding="{Binding CurrentResult.ValveLoss_mbar, Mode=OneWay, StringFormat=F1}"
                    IsReadOnly="True" Width="90">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<!-- Суммарные потери (мбар) -->
<DataGridTextColumn Header="Δp сумма (мбар)"
                    Binding="{Binding CurrentResult.TotalLoss_mbar, Mode=OneWay, StringFormat=F1}"
                    IsReadOnly="True" Width="90">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

#### 4.1.2. Заменить на новые колонки

**Новый код:**

```xml
<!-- DpRohr - потери в трубе (Па) -->
<DataGridTextColumn Header="DpRohr (Па)"
                    Binding="{Binding CurrentResult.DpRohr, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="90">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<!-- DpVerteiler - потери в распределителе (Па) -->
<DataGridTextColumn Header="DpVerteiler (Па)"
                    Binding="{Binding CurrentResult.DpVerteiler, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="100">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<!-- DpVent - потери в вентиле (Па) -->
<DataGridTextColumn Header="DpVent (Па)"
                    Binding="{Binding CurrentResult.DpVent, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="90">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<!-- DpGesamt - суммарные потери (Па) -->
<DataGridTextColumn Header="DpGesamt (Па)"
                    Binding="{Binding CurrentResult.DpGesamt, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="100">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
            <Setter Property="Foreground" Value="{Binding CurrentResult.DpGesamt, Converter={StaticResource PressureColorConverter}}"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<!-- zu_drosseln - дросселирование (Па) -->
<DataGridTextColumn Header="zu_drosseln (Па)"
                    Binding="{Binding Throttling, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="100">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

#### 4.1.3. Обновить колонку оборотов

**Текущий код (примерно):**

```xml
<!-- Обороты клапана -->
<DataGridTextColumn Header="Обороты"
                    Binding="{Binding ValveTurns, Mode=OneWay, StringFormat=F2}"
                    IsReadOnly="True" Width="80">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

**Новый код:**

```xml
<!-- Обороты клапана (дробь) -->
<DataGridTextColumn Header="Обороты"
                    Binding="{Binding ValveTurns, Mode=OneWay, Converter={StaticResource ValveTurnsToFractionConverter}}"
                    IsReadOnly="True" Width="80">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="Padding" Value="4,0"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

**Примечание:** Конвертер `ValveTurnsToFractionConverter` уже существует и должен корректно отображать обороты как дроби (2/4, 2 1/2).

---

## 5. Структура таблицы

### 5.1. Порядок колонок

| № | Колонка | Binding | Формат | Ширина |
|---|---------|---------|--------|--------|
| 1 | № | CircuitNumber | F0 | 50 |
| 2 | Длина (м) | CircuitLength | F1 | 80 |
| 3 | Подводка (м) | SupplyLength | F1 | 80 |
| 4 | Площадь (м²) | CircuitArea | F1 | 80 |
| 5 | Шаг (см) | PipeSpacing_cm | F0 | 60 |
| 6 | Мощность (Вт) | Power | F0 | 100 |
| 7 | Расход (л/ч) | FlowRate | F1 | 80 |
| 8 | Скорость (м/с) | Velocity | F3 | 100 |
| 9 | Re | CurrentResult.ReynoldsNumber | F0 | 80 |
| 10 | λ | CurrentResult.FrictionFactor | F4 | 60 |
| 11 | Режим | CurrentResult.FlowRegime | - | 80 |
| 12 | Уд.потери (Па/м) | CurrentResult.PressureLossPerMeter | F0 | 100 |
| 13 | **DpRohr (Па)** | CurrentResult.DpRohr | F0 | 90 |
| 14 | **DpVerteiler (Па)** | CurrentResult.DpVerteiler | F0 | 100 |
| 15 | **DpVent (Па)** | CurrentResult.DpVent | F0 | 90 |
| 16 | **DpGesamt (Па)** | CurrentResult.DpGesamt | F0 | 100 |
| 17 | **zu_drosseln (Па)** | Throttling | F0 | 100 |
| 18 | Обороты | ValveTurns | Дробь | 80 |

### 5.2. Формат отображения

- **Целые числа:** `StringFormat=F0` (730, 1798, 32000)
- **Десятичные дроби:** `StringFormat=F1` (7.3, 17.98)
- **Дроби:** `ValveTurnsToFractionConverter` (2/4, 2 1/2)

---

## 6. Тест-кейсы

### 6.1. Тесты для UI

**Примечание:** UI-тесты выполняются вручную или через UI-автоматизацию.

**Чек-лист для ручного тестирования:**

1. [ ] Открыть вкладку "Гидравлика"
2. [ ] Добавить несколько контуров
3. [ ] Выполнить расчёт
4. [ ] Проверить колонки:
   - [ ] DpRohr отображается в Па (целые числа)
   - [ ] DpVerteiler отображается в Па (целые числа)
   - [ ] DpVent отображается в Па (целые числа)
   - [ ] DpGesamt отображается в Па (целые числа)
   - [ ] zu_drosseln отображается в Па (целые числа)
   - [ ] Обороты отображаются как дроби
5. [ ] Проверить цветовую индикацию для DpGesamt
6. [ ] Проверить переключение режима (рабочая/расчётная температура)

---

## 7. Критерии приёмки

### 7.1. Функциональные

- [ ] Колонка `DpRohr (Па)` отображает целые числа
- [ ] Колонка `DpVerteiler (Па)` отображает целые числа
- [ ] Колонка `DpVent (Па)` отображает целые числа
- [ ] Колонка `DpGesamt (Па)` отображает целые числа
- [ ] Колонка `zu_drosseln (Па)` отображает целые числа
- [ ] Колонка `Обороты` отображает дроби (2/4, 2 1/2)
- [ ] Старые колонки `Δp контур (мбар)`, `Δp клапан (мбар)`, `Δp сумма (мбар)` удалены

### 7.2. Нефункциональные

- [ ] UI соответствует дизайну приложения
- [ ] Цветовая индикация работает корректно
- [ ] Переключение режима (рабочая/расчётная температура) работает

---

## 8. Порядок выполнения

1. **Найти текущие колонки** давления в `CircuitsView.xaml`
2. **Заменить** на новые колонки с привязкой к новым свойствам
3. **Обновить** формат отображения (`StringFormat=F0`)
4. **Проверить** конвертер `ValveTurnsToFractionConverter`
5. **Запустить приложение** и проверить отображение
6. **Выполнить ручное тестирование** по чек-листу

---

## 9. Примечания

### 9.1. Почему целые числа?

В Excel значения давления отображаются как целые числа (730, 1798, 32000). Это упрощает чтение и сравнение результатов.

### 9.2. Почему дроби для оборотов?

Обороты балансировочного клапана настраиваются с шагом 0.25 оборота. Отображение в виде дробей (2/4, 2 1/2) более наглядно для инженеров.

### 9.3. Связь с другими задачами

Эта задача **зависит от**:
- **Task 2.1 (Модель):** Нужно, чтобы свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt` существовали
- **Task 5.1 (Единицы):** Нужно, чтобы значения были в Паскалях

---

*Задача создана: 2026-03-22*