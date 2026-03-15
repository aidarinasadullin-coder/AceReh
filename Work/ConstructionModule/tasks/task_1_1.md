# Task 1.1: Создать Material.cs

**Этап:** 1. Модели данных  
**Приоритет:** P0 (Критическая)  
**Время:** 1 час  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать модель данных `Material` для представления материала из справочника `materials_db.json`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-02 | Выбор материала из справочника | Модель материала |
| UC-07 | Проверка ограничений по материалам | MaxSupplyTemperature, MinAirTemperature |

---

## 3. Описание изменений

### 3.1. Создать файл

**Путь:** `src/Models/Construction/Material.cs`

**Код:**

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Материал слоя конструкции
    /// </summary>
    /// <remarks>
    /// Загружается из справочника materials_db.json
    /// </remarks>
    public class Material
    {
        /// <summary>
        /// Идентификатор материала
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название материала
        /// </summary>
        /// <example>Бетон плотный</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Теплопроводность в сухих условиях (УГВ >= 1м), Вт/м·К
        /// </summary>
        /// <remarks>
        /// Используется для слоёв над трубой и для слоёв под трубой при УГВ >= 1м
        /// </remarks>
        public double LambdaA { get; set; }

        /// <summary>
        /// Теплопроводность во влажных условиях (УГВ < 1м), Вт/м·К
        /// </summary>
        /// <remarks>
        /// Используется для слоёв под трубой при УГВ < 1м
        /// </remarks>
        public double LambdaB { get; set; }

        /// <summary>
        /// Категория материала
        /// </summary>
        /// <example>бетон, грунт, изоляция, покрытие, стяжка</example>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Примечания к материалу
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Максимальная температура подачи, °C
        /// </summary>
        /// <remarks>
        /// Для бетона = 50°C, null = без ограничений
        /// Используется для валидации UC-07
        /// </remarks>
        public double? MaxSupplyTemperature { get; set; }

        /// <summary>
        /// Минимальная температура наружного воздуха, °C
        /// </summary>
        /// <remarks>
        /// Для асфальта = -15°C, null = без ограничений
        /// Используется для валидации UC-07
        /// </remarks>
        public double? MinAirTemperature { get; set; }

        /// <summary>
        /// Получить λ в зависимости от условий
        /// </summary>
        /// <param name="isWetConditions">Признак влажных условий (УГВ < 1м)</param>
        /// <returns>Теплопроводность, Вт/м·К</returns>
        public double GetLambda(bool isWetConditions)
        {
            return isWetConditions ? LambdaB : LambdaA;
        }

        /// <summary>
        /// Строковое представление материала
        /// </summary>
        public override string ToString()
        {
            return $"{Name} (λА={LambdaA:F3}, λБ={LambdaB:F3})";
        }
    }
}
```

### 3.2. Создать папку

Если папка `src/Models/Construction/` не существует, создать её.

---

## 4. Тест-кейсы

### TC-1.1.1: Создание материала

```csharp
[Fact]
public void Material_Create_ShouldSetProperties()
{
    // Arrange & Act
    var material = new Material
    {
        Id = 1,
        Name = "Бетон плотный",
        LambdaA = 1.5,
        LambdaB = 1.5,
        Category = "бетон",
        MaxSupplyTemperature = 50.0
    };

    // Assert
    Assert.Equal(1, material.Id);
    Assert.Equal("Бетон плотный", material.Name);
    Assert.Equal(1.5, material.LambdaA);
    Assert.Equal(1.5, material.LambdaB);
    Assert.Equal("бетон", material.Category);
    Assert.Equal(50.0, material.MaxSupplyTemperature);
}
```

### TC-1.1.2: Получение λ для сухих условий

```csharp
[Fact]
public void Material_GetLambda_DryConditions_ShouldReturnLambdaA()
{
    // Arrange
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

    // Act
    var lambda = material.GetLambda(isWetConditions: false);

    // Assert
    Assert.Equal(0.4, lambda);
}
```

### TC-1.1.3: Получение λ для влажных условий

```csharp
[Fact]
public void Material_GetLambda_WetConditions_ShouldReturnLambdaB()
{
    // Arrange
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

    // Act
    var lambda = material.GetLambda(isWetConditions: true);

    // Assert
    Assert.Equal(2.0, lambda);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Models/Construction/Material.cs` создан
- [ ] Класс `Material` содержит все свойства из ТЗ
- [ ] Метод `GetLambda()` работает корректно
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Класс должен быть POCO (Plain Old CLR Object) без зависимостей
- Использовать `double?` для nullable свойств (MaxSupplyTemperature, MinAirTemperature)
- Категории материалов: "бетон", "грунт", "изоляция", "покрытие", "подстилающий", "стяжка"

---

**Конец документа**