# Task 6.5: Удаление устаревших моделей

**Этап:** 6 - Интеграция  
**Приоритет:** Средний  
**Статус:** К разработке  
**Зависимости:** Task 1.2 (HydraulicInputData)

---

## 1. Цель задачи

Удалить устаревшие модели и заменить их на новые.

---

## 2. Устаревшие модели

| Файл | Действие | Замена |
|------|----------|--------|
| `src/Models/Hydraulics/HydraulicParameters.cs` | Удалить | `HydraulicInputData.cs` |
| `src/Models/Hydraulics/HydraulicResult.cs` | Удалить | `CircuitTemperatureResult` (в CircuitRow.cs) |
| `src/Models/Hydraulics/CircuitResult.cs` | Удалить | `CircuitRow.cs` |

---

## 3. Порядок удаления

### 3.1. Проверка зависимостей

Перед удалением убедиться, что:
- [ ] Все ссылки на `HydraulicParameters` заменены на `HydraulicInputData`
- [ ] Все ссылки на `HydraulicResult` заменены на `CircuitTemperatureResult`
- [ ] Все ссылки на `CircuitResult` заменены на `CircuitRow`

### 3.2. Удаление файлов

```bash
# Удалить устаревшие файлы
rm src/Models/Hydraulics/HydraulicParameters.cs
rm src/Models/Hydraulics/HydraulicResult.cs
rm src/Models/Hydraulics/CircuitResult.cs
```

---

## 4. Критерии приёмки

- [ ] Устаревшие файлы удалены
- [ ] Новые модели используются во всём коде
- [ ] Ссылки на старые модели обновлены
- [ ] Код компилируется без ошибок
- [ ] Все тесты проходят

---

## 5. Примечания

- Удаление выполняется после миграции на новые модели
- Необходимо обновить все ссылки на старые модели
- Проверить все using-директивы

---

## 6. Связанные задачи

- Task 1.2: HydraulicInputData — новая модель для замены HydraulicParameters
- Task 3.2: CircuitsCalculator — использует новые модели

---

*Дата создания: 2026-03-17*