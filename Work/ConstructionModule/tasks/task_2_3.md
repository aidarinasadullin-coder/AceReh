# Task 2.3: Создать data/materials_db.json

**Этап:** 2. Репозитории  
**Приоритет:** P1 (Высокая)  
**Время:** 1 час  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать JSON-файл `data/materials_db.json` с базой материалов для расчёта систем снеготаяния.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-02 | Выбор материала из справочника | База материалов |

---

## 3. Описание изменений

### 3.1. Создать папку data

Если папка `data/` не существует, создать её.

### 3.2. Создать файл materials_db.json

**Путь:** `data/materials_db.json`

**Код:**

```json
{
  "meta": {
    "source": "Расчет1этап.xlsx, вкладка Materials",
    "version": "1.0",
    "date": "2026-03-15",
    "description": "База материалов для расчёта систем снеготаяния РЕХАУ"
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
    {
      "id": 2,
      "name": "Грунт",
      "lambda_A": 0.5,
      "lambda_B": 1.5,
      "category": "грунт",
      "notes": "Средние значения для различных типов грунтов"
    },
    {
      "id": 3,
      "name": "Бетон на каменном щебне",
      "lambda_A": 1.5,
      "lambda_B": 1.5,
      "category": "бетон",
      "notes": "Не зависит от влажности",
      "maxSupplyTemperature": 50.0
    },
    {
      "id": 4,
      "name": "Бетон на песке",
      "lambda_A": 0.7,
      "lambda_B": 0.7,
      "category": "бетон",
      "notes": "Не зависит от влажности",
      "maxSupplyTemperature": 50.0
    },
    {
      "id": 5,
      "name": "Бетон плотный",
      "lambda_A": 1.5,
      "lambda_B": 1.5,
      "category": "бетон",
      "notes": "Материал по умолчанию. Не зависит от влажности.",
      "maxSupplyTemperature": 50.0
    },
    {
      "id": 6,
      "name": "Железобетон",
      "lambda_A": 1.7,
      "lambda_B": 1.7,
      "category": "бетон",
      "notes": "Не зависит от влажности",
      "maxSupplyTemperature": 50.0
    },
    {
      "id": 7,
      "name": "Асфальтобетон",
      "lambda_A": 1.5,
      "lambda_B": 1.5,
      "category": "покрытие",
      "notes": "Не зависит от влажности"
    },
    {
      "id": 8,
      "name": "Щебень/Гравий",
      "lambda_A": 0.7,
      "lambda_B": 1.8,
      "category": "подстилающий",
      "notes": "При высоком УГВ теплопроводность возрастает"
    },
    {
      "id": 9,
      "name": "Цементно-песчаная стяжка",
      "lambda_A": 1.2,
      "lambda_B": 1.2,
      "category": "стяжка",
      "notes": "Не зависит от влажности"
    },
    {
      "id": 10,
      "name": "Пенополистирол ЭППС",
      "lambda_A": 0.035,
      "lambda_B": 0.035,
      "category": "изоляция",
      "notes": "Экструдированный пенополистирол. Не зависит от влажности."
    },
    {
      "id": 11,
      "name": "Асфальт",
      "lambda_A": 0.75,
      "lambda_B": 0.75,
      "category": "покрытие",
      "notes": "Не применять при температуре наружного воздуха ≤ -15°C",
      "minAirTemperature": -15.0
    }
  ],
  "usage_rules": {
    "ugw_condition": "Уровень грунтовых вод (УГВ)",
    "lambda_A": "Используется при УГВ >= 1м (сухие условия)",
    "lambda_B": "Используется при УГВ < 1м (влажные условия)",
    "categories": {
      "бетон": "Бетон на каменном щебне, Бетон на песке, Бетон плотный, Железобетон",
      "грунт": "Песок, Грунт",
      "изоляция": "Пенополистирол ЭППС",
      "покрытие": "Асфальтобетон, Асфальт",
      "подстилающий": "Щебень/Гравий",
      "стяжка": "Цементно-песчаная стяжка"
    },
    "validation_rules": {
      "бетон": "Максимальная температура подачи 50°C",
      "асфальт": "Не применять при температуре наружного воздуха ≤ -15°C"
    }
  }
}
```

---

## 4. Тест-кейсы

### TC-2.3.1: Валидация JSON-файла

```csharp
[Fact]
public void MaterialsDb_ShouldBeValidJson()
{
    // Arrange
    var filePath = "data/materials_db.json";

    // Act
    var jsonContent = File.ReadAllText(filePath);
    var jsonModel = JsonSerializer.Deserialize<MaterialsJsonModel>(jsonContent, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    // Assert
    Assert.NotNull(jsonModel);
    Assert.NotNull(jsonModel.Materials);
    Assert.True(jsonModel.Materials.Count > 0);
}
```

### TC-2.3.2: Проверка обязательных полей

```csharp
[Fact]
public void MaterialsDb_ShouldHaveRequiredFields()
{
    // Arrange
    var filePath = "data/materials_db.json";
    var jsonContent = File.ReadAllText(filePath);
    var jsonModel = JsonSerializer.Deserialize<MaterialsJsonModel>(jsonContent, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    // Assert
    foreach (var material in jsonModel!.Materials!)
    {
        Assert.True(material.Id > 0, $"Material {material.Name} should have valid Id");
        Assert.False(string.IsNullOrEmpty(material.Name), $"Material should have Name");
        Assert.True(material.LambdaA > 0, $"Material {material.Name} should have valid LambdaA");
        Assert.True(material.LambdaB > 0, $"Material {material.Name} should have valid LambdaB");
        Assert.False(string.IsNullOrEmpty(material.Category), $"Material {material.Name} should have Category");
    }
}
```

### TC-2.3.3: Проверка категорий

```csharp
[Fact]
public void MaterialsDb_ShouldHaveValidCategories()
{
    // Arrange
    var validCategories = new[] { "бетон", "грунт", "изоляция", "покрытие", "подстилающий", "стяжка" };
    var filePath = "data/materials_db.json";
    var jsonContent = File.ReadAllText(filePath);
    var jsonModel = JsonSerializer.Deserialize<MaterialsJsonModel>(jsonContent, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    // Assert
    foreach (var material in jsonModel!.Materials!)
    {
        Assert.Contains(material.Category.ToLower(), validCategories);
    }
}
```

---

## 5. Критерии приёмки

- [ ] Папка `data/` создана
- [ ] Файл `data/materials_db.json` создан
- [ ] JSON валиден
- [ ] Все материалы имеют обязательные поля (id, name, lambda_A, lambda_B, category)
- [ ] Категории соответствуют допустимым значениям
- [ ] Материал по умолчанию (ID=5, "Бетон плотный") присутствует

---

## 6. Примечания

- Источник данных: `Расчет1этап.xlsx`, вкладка Materials
- `lambda_A` — теплопроводность в сухих условиях (УГВ >= 1м)
- `lambda_B` — теплопроводность во влажных условиях (УГВ < 1м)
- `maxSupplyTemperature` — ограничение для бетона (50°C)
- `minAirTemperature` — ограничение для асфальта (-15°C)

---

**Конец документа**