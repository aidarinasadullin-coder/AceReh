# Task 6.3: Загрузка данных из JSON

**Этап:** 6 - Integration  
**Приоритет:** Средний  
**Статус:** Не начато  
**Зависимости:** Task 3.3, Task 3.5

---

## 1. Цель задачи

Обеспечить загрузку данных о гликолях и коллекторах из JSON-файлов.

---

## 2. Создаваемые/изменяемые файлы

### 6.1. data/glycol_data.json

**Путь:** `data/glycol_data.json`

**Структура:**
```json
{
  "ethylene_glycol": {
    "10": { "temperatures": [...], "density": [...], "viscosity": [...], "specific_heat": [...] },
    "20": { ... },
    ...
  },
  "propylene_glycol": { ... }
}
```

### 6.2. data/rehau_products.json

**Путь:** `data/rehau_products.json`

**Добавить секцию коллекторов:**
```json
{
  "collectors": [
    { "id": "HKV-D-2", "type": "HKV", "circuits": 2, "kv": 1.2, "max_flow_rate": 1.5, "max_pressure": 320 },
    ...
  ]
}
```

---

## 3. Критерии приёмки

- [ ] JSON-файлы созданы/обновлены
- [ ] Данные загружаются корректно
- [ ] Интерполяция работает