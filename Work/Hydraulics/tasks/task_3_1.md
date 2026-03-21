# Задача 3.1: Удалить CircuitViewModel.cs

## 1. Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_3_1 |
| **Приоритет** | P3 (Рекомендуется) |
| **Этап** | 3 |
| **Зависимости** | Нет |
| **Юзер-кейсы** | Нет (техническая задача) |
| **Оценка времени** | 0.1 часа |

---

## 2. Цель задачи

Удалить неиспользуемый файл `CircuitViewModel.cs`, который дублирует функциональность `CircuitRow`.

---

## 3. Описание проблемы

### 3.1. Корневая причина
**Файл**: `src/ViewModels/Hydraulics/CircuitViewModel.cs`

Класс `CircuitViewModel` не используется в приложении. Он дублирует функциональность `CircuitRow` из `src/Models/Hydraulics/CircuitRow.cs`.

### 3.2. Сравнение классов

#### CircuitViewModel (не используется)
```csharp
public partial class CircuitViewModel : ObservableObject
{
    [ObservableProperty]
    private int _circuitNumber;  // <-- ПРАВИЛЬНО! Уведомляет UI
    
    [ObservableProperty]
    private string _pipeType = string.Empty;
    
    // ... другие свойства
}
```

#### CircuitRow (используется)
```csharp
public partial class CircuitRow : ObservableObject
{
    public int CircuitNumber { get; set; }  // <-- БЕЗ уведомления (исправляется в Task 1.1)
    
    [ObservableProperty]
    private double _circuitLength;
    
    // ... другие свойства
}
```

### 3.3. Почему CircuitViewModel не используется
- `CircuitsViewModel` использует `ObservableCollection<CircuitRow>` (строка 25)
- `CircuitViewModel` нигде не создаётся и не используется
- Это "мёртвый код"

---

## 4. Изменения

### 4.1. Удалить файл

**Файл**: `src/ViewModels/Hydraulics/CircuitViewModel.cs`

**Действие**: Удалить файл полностью.

### 4.2. Проверить отсутствие ссылок

**Проверить**:
1. Поиск `CircuitViewModel` в проекте
2. Убедиться, что класс не используется в XAML
3. Убедиться, что класс не используется в других ViewModel

**Команда поиска**:
```bash
grep -r "CircuitViewModel" src/
```

**Ожидаемый результат**: Нет ссылок на `CircuitViewModel`.

---

## 5. Тест-кейсы

### TC-3.1.1: Компиляция после удаления
**Предусловия**:
- Файл `CircuitViewModel.cs` удалён

**Шаги**:
1. Выполнить сборку проекта

**Ожидаемый результат**:
- Проект компилируется без ошибок
- Нет предупреждений о missing types

### TC-3.1.2: Функциональность не нарушена
**Предусловия**:
- Проект скомпилирован

**Шаги**:
1. Запустить приложение
2. Открыть экран "Гидравлический расчёт"
3. Добавить контур
4. Удалить контур

**Ожидаемый результат**:
- Функциональность работает корректно
- Нет ошибок времени выполнения

---

## 6. Критерии приёмки

- [ ] Файл `CircuitViewModel.cs` удалён
- [ ] Проект компилируется без ошибок
- [ ] Нет ссылок на `CircuitViewModel` в проекте
- [ ] Функциональность не нарушена

---

## 7. Примечания

### 7.1. Решение пользователя
**Удалить CircuitViewModel.cs** — класс не используется в приложении, является дублированием функциональности `CircuitRow`.

### 7.2. Почему CircuitRow лучше
- `CircuitRow` используется в приложении
- `CircuitRow` содержит все необходимые свойства для расчётов
- После исправления `CircuitNumber` (Task 1.1) `CircuitRow` будет полностью функционален

---

## 8. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification.md`, раздел 4.4
- **Файл**: `src/ViewModels/Hydraulics/CircuitViewModel.cs`
- **Альтернатива**: `src/Models/Hydraulics/CircuitRow.cs`