# Task 1.3: Создать ValidationResult.cs

**Этап:** 1. Модели данных  
**Приоритет:** P1 (Высокая)  
**Время:** 0.5 часа  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать модель данных `ValidationResult` для хранения результатов валидации конструкции.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-06 | Валидация минимальной стяжки | IsValid, Errors |
| UC-07 | Проверка ограничений по материалам | Errors |

---

## 3. Описание изменений

### 3.1. Создать файл ValidationResult.cs

**Путь:** `src/Models/Construction/ValidationResult.cs`

**Код:**

```csharp
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Результат валидации конструкции
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Признак валидности
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Список ошибок валидации
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Создать успешный результат валидации
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            };
        }

        /// <summary>
        /// Создать результат с ошибками
        /// </summary>
        /// <param name="errors">Список ошибок</param>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        /// <param name="error">Текст ошибки</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Объединить результаты валидации
        /// </summary>
        /// <param name="other">Другой результат</param>
        public void Merge(ValidationResult other)
        {
            if (!other.IsValid)
            {
                Errors.AddRange(other.Errors);
                IsValid = false;
            }
        }

        /// <summary>
        /// Получить строковое представление ошибок
        /// </summary>
        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }

        /// <summary>
        /// Строковое представление результата
        /// </summary>
        public override string ToString()
        {
            return IsValid 
                ? "Валидация пройдена" 
                : $"Ошибки: {GetErrorMessage()}";
        }
    }
}
```

---

## 4. Тест-кейсы

### TC-1.3.1: Создание успешного результата

```csharp
[Fact]
public void ValidationResult_Success_ShouldBeValid()
{
    // Act
    var result = ValidationResult.Success();

    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
}
```

### TC-1.3.2: Создание результата с ошибками

```csharp
[Fact]
public void ValidationResult_Failure_ShouldBeInvalid()
{
    // Act
    var result = ValidationResult.Failure("Ошибка 1", "Ошибка 2");

    // Assert
    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Contains("Ошибка 1", result.Errors);
    Assert.Contains("Ошибка 2", result.Errors);
}
```

### TC-1.3.3: Добавление ошибки

```csharp
[Fact]
public void ValidationResult_AddError_ShouldInvalidate()
{
    // Arrange
    var result = ValidationResult.Success();

    // Act
    result.AddError("Новая ошибка");

    // Assert
    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Contains("Новая ошибка", result.Errors);
}
```

### TC-1.3.4: Объединение результатов

```csharp
[Fact]
public void ValidationResult_Merge_ShouldCombineErrors()
{
    // Arrange
    var result1 = ValidationResult.Failure("Ошибка 1");
    var result2 = ValidationResult.Failure("Ошибка 2");

    // Act
    result1.Merge(result2);

    // Assert
    Assert.False(result1.IsValid);
    Assert.Equal(2, result1.Errors.Count);
}
```

### TC-1.3.5: Объединение с успешным результатом

```csharp
[Fact]
public void ValidationResult_Merge_WithSuccess_ShouldNotChange()
{
    // Arrange
    var result1 = ValidationResult.Success();
    var result2 = ValidationResult.Success();

    // Act
    result1.Merge(result2);

    // Assert
    Assert.True(result1.IsValid);
    Assert.Empty(result1.Errors);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Models/Construction/ValidationResult.cs` создан
- [ ] Класс `ValidationResult` содержит IsValid и Errors
- [ ] Статические методы `Success()` и `Failure()` работают
- [ ] Методы `AddError()` и `Merge()` работают
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Класс должен быть простым POCO без зависимостей
- Использовать `List<string>` для хранения ошибок
- Статические фабричные методы упрощают создание результатов

---

**Конец документа**