# Техническое задание: Исправление GlycolDataService

## 1. Описание проблемы

### 1.1. Проблема 1: Неправильный парсинг JSON

**Файл:** `src/Services/Hydraulics/GlycolDataService.cs`

JSON файл `data/glycol_data.json` использует snake_case для ключей:
- `"ethylene_glycol"` → C# свойство `EthyleneGlycol`
- `"propylene_glycol"` → C# свойство `PropyleneGlycol`
- `"density_kg_m3"` → C# свойство `Density`
- `"specific_heat_kJ_kgK"` → C# свойство `SpecificHeat`
- `"kinematic_viscosity_mm2_s"` → C# свойство `KinematicViscosity`
- `"thermal_conductivity_W_mK"` → C# свойство `ThermalConductivity`
- `"concentration_vol_pct"` → C# свойство `Concentrations`

**Причина:** `PropertyNameCaseInsensitive = true` делает имена нечувствительными к регистру, но НЕ конвертирует snake_case в PascalCase.

**Результат:** JSON не парсится корректно, `rawContainer.EthyleneGlycol` и `rawContainer.PropyleneGlycol` равны `null`, используются fallback данные.

### 1.2. Проблема 2: Идентичные fallback данные

Методы `GetDefaultEthyleneData()` (строки 512-526) и `GetDefaultPropyleneData()` (строки 528-542) возвращают ОДИНАКОВЫЕ данные, используя одни и те же методы:
- `DefaultDensityValues()`
- `DefaultSpecificHeatValues()`
- `DefaultViscosityValues()`
- `DefaultConductivityValues()`

**Результат:** При ошибке загрузки JSON оба типа гликолей возвращают одинаковые значения плотности, вязкости и теплоёмкости.

### 1.3. Текущее поведение

При смене типа гликоля (этилен ↔ пропилен) удельные потери давления не меняются, потому что:
1. JSON не загружается из-за несоответствия имён
2. Fallback данные идентичны для обоих типов

### 1.4. Ожидаемое поведение

- Этиленгликоль и пропиленгликоль должны возвращать РАЗНЫЕ значения свойств
- При 50% концентрации и 37.8°C (ближайшая точка ASHRAE к 40°C):
  - **Этиленгликоль:** плотность 1086.6 кг/м³, вязкость 1.3 мм²/с, теплоёмкость 4.05 кДж/(кг·К)
  - **Пропиленгликоль:** плотность 1037 кг/м³, вязкость 4.19 мм²/с, теплоёмкость 3.90 кДж/(кг·К)

---

## 2. Важное примечание о температурах ASHRAE

### 2.1. Температуры в JSON

JSON файл использует температуры ASHRAE, переведённые из °F в °C по формуле:
```
°C = (°F - 32) × 5/9
```

**Полный список температур в JSON:**
```
-34.4, -28.9, -23.3, -17.8, -12.2, -6.7, -1.1, 4.4, 10.0, 15.6, 21.1, 26.7, 
32.2, 37.8, 43.3, 48.9, 54.4, 60.0, 65.6, 71.1, 76.7, 82.2, 87.8, 93.3, 98.9
```

### 2.2. Требование к fallback данным

Fallback матрицы должны использовать **те же температуры**, что и JSON (или подмножество), чтобы обеспечить согласованность интерполяции при переключении между JSON и fallback.

---

## 3. Требования к исправлению

### 3.1. Юзер-кейс 1: Корректная загрузка данных из JSON

#### 3.1.1. Актёры
- Пользователь (инженер-проектировщик)
- Система (GlycolDataService)

#### 3.1.2. Предусловия
- Файл `data/glycol_data.json` существует и содержит корректные данные
- Сервис GlycolDataService инициализирован

#### 3.1.3. Основной сценарий
1. Пользователь выбирает тип гликоля (этиленгликоль или пропиленгликоль)
2. Пользователь задаёт концентрацию (например, 50%)
3. Пользователь задаёт температуру (например, 37.8°C)
4. Система вызывает `GlycolDataService.GetProperties(glycolType, concentration, temperature)`
5. Система загружает данные из JSON файла
6. Система корректно парсит JSON с snake_case ключами
7. Система возвращает РАЗНЫЕ значения для разных типов гликолей

#### 3.1.4. Альтернативные сценарии
- **A1: Файл JSON не найден** — используются fallback данные с предупреждением в лог
- **A2: Ошибка парсинга JSON** — используются fallback данные с предупреждением в лог

#### 3.1.5. Постусловия
- Данные успешно загружены из JSON
- Свойства гликолей различаются для этиленгликоля и пропиленгликоля

#### 3.1.6. Критерии приёмки
- ✅ JSON файл успешно парсится
- ✅ `rawContainer.EthyleneGlycol` не равен `null`
- ✅ `rawContainer.PropyleneGlycol` не равен `null`
- ✅ Значения свойств соответствуют данным из JSON

---

### 3.2. Юзер-кейс 2: Различие свойств гликолей

#### 3.2.1. Актёры
- Пользователь (инженер-проектировщик)
- Система (GlycolDataService)

#### 3.2.2. Предусловия
- GlycolDataService работает корректно
- Данные загружены из JSON или fallback

#### 3.2.3. Основной сценарий
1. Пользователь выбирает этиленгликоль 50% при 37.8°C
2. Система возвращает плотность 1086.6 кг/м³, вязкость 1.3 мм²/с
3. Пользователь меняет тип на пропиленгликоль 50% при 37.8°C
4. Система возвращает плотность 1037 кг/м³, вязкость 4.19 мм²/с
5. Значения РАЗЛИЧАЮТСЯ

#### 3.2.4. Критерии приёмки
- ✅ Плотность этиленгликоля отличается от плотности пропиленгликоля
- ✅ Вязкость этиленгликоля отличается от вязкости пропиленгликоля
- ✅ Теплоёмкость этиленгликоля отличается от теплоёмкости пропиленгликоля
- ✅ Разница значений соответствует данным ASHRAE

---

### 3.3. Юзер-кейс 3: Fallback данные при ошибке загрузки

#### 3.3.1. Актёры
- Система (GlycolDataService)

#### 3.3.2. Предусловия
- Файл JSON отсутствует или повреждён

#### 3.3.3. Основной сценарий
1. Система пытается загрузить JSON
2. Система обнаруживает ошибку (файл не найден / ошибка парсинга)
3. Система логирует предупреждение
4. Система использует fallback данные
5. Fallback данные для этиленгликоля ОТЛИЧАЮТСЯ от пропиленгликоля

#### 3.3.4. Критерии приёмки
- ✅ Fallback данные для этиленгликоля отличаются от пропиленгликоля
- ✅ Плотность этиленгликоля (fallback) ~1080-1095 кг/м³ при 50% и 37.8°C
- ✅ Плотность пропиленгликоля (fallback) ~1030-1045 кг/м³ при 50% и 37.8°C
- ✅ Вязкости различаются минимум на 200%

- ✅ Fallback использует те же температуры ASHRAE, что и JSON

---

## 4. Конкретные изменения в коде

### 4.1. Добавить атрибуты [JsonPropertyName] к классам

#### 4.1.1. Класс GlycolRawContainer (строки 625-629)

**Было:**
```csharp
internal class GlycolRawContainer
{
    public GlycolTypeRawData? EthyleneGlycol { get; set; }
    public GlycolTypeRawData? PropyleneGlycol { get; set; }
}
```

**Стало:**
```csharp
using System.Text.Json.Serialization;

internal class GlycolRawContainer
{
    [JsonPropertyName("ethylene_glycol")]
    public GlycolTypeRawData? EthyleneGlycol { get; set; }
    
    [JsonPropertyName("propylene_glycol")]
    public GlycolTypeRawData? PropyleneGlycol { get; set; }
}
```

#### 4.1.2. Класс GlycolTypeRawData (строки 634-641)

**Было:**
```csharp
internal class GlycolTypeRawData
{
    public double[]? Concentrations { get; set; }
    public PropertyData? Density { get; set; }
    public PropertyData? SpecificHeat { get; set; }
    public PropertyData? KinematicViscosity { get; set; }
    public PropertyData? ThermalConductivity { get; set; }
}
```

**Стало:**
```csharp
internal class GlycolTypeRawData
{
    [JsonPropertyName("concentration_vol_pct")]
    public double[]? Concentrations { get; set; }
    
    [JsonPropertyName("density_kg_m3")]
    public PropertyData? Density { get; set; }
    
    [JsonPropertyName("specific_heat_kJ_kgK")]
    public PropertyData? SpecificHeat { get; set; }
    
    [JsonPropertyName("kinematic_viscosity_mm2_s")]
    public PropertyData? KinematicViscosity { get; set; }
    
    [JsonPropertyName("thermal_conductivity_W_mK")]
    public PropertyData? ThermalConductivity { get; set; }
}
```

#### 4.1.3. Класс PropertyData (строки 646-649)

**Было:**
```csharp
internal class PropertyData
{
    public List<TemperatureDataRow>? Data { get; set; }
}
```

**Стало:**
```csharp
internal class PropertyData
{
    [JsonPropertyName("data")]
    public List<TemperatureDataRow>? Data { get; set; }
}
```

#### 4.1.4. Класс TemperatureDataRow (строки 654-658)

**Было:**
```csharp
internal class TemperatureDataRow
{
    public double? TempC { get; set; }
    public double?[]? Values { get; set; }
}
```

**Стало:**
```csharp
internal class TemperatureDataRow
{
    [JsonPropertyName("temp_c")]
    public double? TempC { get; set; }
    
    [JsonPropertyName("values")]
    public double?[]? Values { get; set; }
}
```

### 4.2. Создать отдельные fallback данные для пропиленгликоля

#### 4.2.1. Обновить температуры в fallback

Fallback матрицы должны использовать температуры ASHRAE (подмножество из JSON):

```csharp
// Температуры ASHRAE (подмножество для fallback)
// Выбрана каждая 3-я точка для покрытия всего диапазона
private static readonly double[] FallbackTemperatures = new double[]
{
    -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
};
```

#### 4.2.2. Заменить метод GetDefaultEthyleneData() (строки 512-526)

**Было:**
```csharp
private static GlycolTypeData GetDefaultEthyleneData()
{
    var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
    var temperatures = new[] { -20.0, -10.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0 };

    return new GlycolTypeData
    {
        Concentrations = concentrations,
        Temperatures = temperatures,
        Density = CreateDefaultTable(concentrations, temperatures, DefaultDensityValues()),
        SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultSpecificHeatValues()),
        KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultViscosityValues()),
        ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultConductivityValues())
    };
}
```

**Стало:**
```csharp
private static GlycolTypeData GetDefaultEthyleneData()
{
    var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
    // Температуры ASHRAE (подмножество из JSON)
    var temperatures = new[] { -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };

    return new GlycolTypeData
    {
        Concentrations = concentrations,
        Temperatures = temperatures,
        Density = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneDensityValues()),
        SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneSpecificHeatValues()),
        KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneViscosityValues()),
        ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneConductivityValues())
    };
}
```

#### 4.2.3. Заменить метод GetDefaultPropyleneData() (строки 528-542)

**Было:**
```csharp
private static GlycolTypeData GetDefaultPropyleneData()
{
    var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
    var temperatures = new[] { -20.0, -10.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0 };

    return new GlycolTypeData
    {
        Concentrations = concentrations,
        Temperatures = temperatures,
        Density = CreateDefaultTable(concentrations, temperatures, DefaultDensityValues()),
        SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultSpecificHeatValues()),
        KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultViscosityValues()),
        ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultConductivityValues())
    };
}
```

**Стало:**
```csharp
private static GlycolTypeData GetDefaultPropyleneData()
{
    var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
    // Температуры ASHRAE (подмножество из JSON)
    var temperatures = new[] { -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };

    return new GlycolTypeData
    {
        Concentrations = concentrations,
        Temperatures = temperatures,
        Density = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneDensityValues()),
        SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneSpecificHeatValues()),
        KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneViscosityValues()),
        ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneConductivityValues())
    };
}
```

#### 4.2.4. Добавить fallback данные для этиленгликоля

Заменить существующие методы `DefaultDensityValues()`, `DefaultSpecificHeatValues()`, `DefaultViscosityValues()`, `DefaultConductivityValues()` на новые с реальными данными ASHRAE:

```csharp
/// <summary>
/// Fallback значения плотности для этиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// Температуры ASHRAE: -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 °C
/// Концентрации: 10, 20, 30, 40, 50, 60, 70, 80, 90 vol%
/// </summary>
private static double[,] DefaultEthyleneDensityValues()
{
    // Данные из JSON: density_kg_m3, строки 29-53
    // Строки соответствуют температурам, столбцы - концентрациям
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null, 1090.7, 1105.3, 1119.1, 1132.5 },
        // temp: -17.8°C
        {  null,   null, 1072.2, 1087.2, 1101.5, 1115.1, 1128.4, 1141.3, 1153.8 },
        // temp: -1.1°C
        { 1019.2, 1053.2, 1068.6, 1083.4, 1097.3, 1110.6, 1123.5, 1136.1, 1148.1 },
        // temp: 15.6°C
        { 1015.7, 1049.5, 1064.8, 1079.2, 1092.8, 1105.6, 1118.2, 1130.4, 1141.8 },
        // temp: 32.2°C
        { 1012.1, 1045.7, 1060.9, 1074.9, 1088.2, 1100.3, 1112.6, 1124.3, 1135.0 },
        // temp: 48.9°C
        { 1008.3, 1041.9, 1056.9, 1070.5, 1083.4, 1094.7, 1106.7, 1117.9, 1127.7 },
        // temp: 65.6°C
        { 1004.5, 1038.0, 1052.9, 1066.0, 1078.5, 1088.9, 1100.6, 1111.2, 1120.1 },
        // temp: 82.2°C
        { 1000.6, 1034.1, 1048.9, 1061.3, 1073.4, 1082.9, 1094.3, 1104.2, 1112.2 },
        // temp: 98.9°C
        {  996.7, 1030.2, 1044.8, 1056.6, 1068.2, 1076.7, 1087.9, 1097.0, 1104.1 }
    };
}

/// <summary>
/// Fallback значения удельной теплоёмкости для этиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// </summary>
private static double[,] DefaultEthyleneSpecificHeatValues()
{
    // Данные из JSON: specific_heat_kJ_kgK, строки 60-84
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null,   3.07,   2.85,   2.62,   2.37 },
        // temp: -17.8°C
        {  null,   null,   3.35,   3.13,   2.92,   2.70,   2.47,   2.23,   2.03 },
        // temp: -1.1°C
        {  3.78,   3.14,   2.92,   2.70,   2.47,   2.22,   2.01,   1.82,   1.62 },
        // temp: 15.6°C
        {  4.36,   3.77,   3.59,   3.40,   3.20,   3.00,   2.78,   2.54,   2.33 },
        // temp: 32.2°C
        {  5.00,   4.39,   4.20,   4.03,   3.84,   3.63,   3.41,   3.17,   2.94 },
        // temp: 48.9°C
        {  5.65,   5.01,   4.83,   4.65,   4.47,   4.26,   4.04,   3.80,   3.57 },
        // temp: 65.6°C
        {  6.29,   5.63,   5.46,   5.28,   5.10,   4.89,   4.67,   4.43,   4.20 },
        // temp: 82.2°C
        {  6.93,   6.25,   6.09,   5.91,   5.73,   5.52,   5.30,   5.06,   4.83 },
        // temp: 98.9°C
        {  7.58,   6.87,   6.72,   6.54,   6.36,   6.15,   5.93,   5.69,   5.46 }
    };
}

/// <summary>
/// Fallback значения кинематической вязкости для этиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// </summary>
private static double[,] DefaultEthyleneViscosityValues()
{
    // Данные из JSON: kinematic_viscosity_mm2_s, строки 123-147
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null,   58.4,   81.2,  115.0,  163.5 },
        // temp: -17.8°C
        {  null,   null,   12.9,   17.8,   27.2,   40.8,   57.5,   79.4,   93.3 },
        // temp: -1.1°C
        {   2.6,    3.7,    5.5,    7.9,   11.4,   17.4,   23.2,   31.6,   38.9 },
        // temp: 15.6°C
        {   1.0,    1.4,    2.0,    2.7,    3.8,    5.3,    7.3,   10.2,   13.7 },
        // temp: 32.2°C
        {   0.5,    0.7,    0.8,    1.1,    1.6,    2.1,    2.8,    3.7,    4.8 },
        // temp: 48.9°C
        {   0.3,    0.4,    0.5,    0.6,    0.8,    1.1,    1.4,    1.9,    2.4 },
        // temp: 65.6°C
        {   0.2,    0.2,    0.3,    0.4,    0.5,    0.6,    0.7,    0.9,    1.2 },
        // temp: 82.2°C
        {   0.1,    0.1,    0.2,    0.2,    0.3,    0.3,    0.4,    0.5,    0.5 },
        // temp: 98.9°C
        {   0.1,    0.1,    0.1,    0.1,    0.2,    0.2,    0.3,    0.3,    0.4 }
    };
}

/// <summary>
/// Fallback значения теплопроводности для этиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// </summary>
private static double[,] DefaultEthyleneConductivityValues()
{
    // Данные из JSON: thermal_conductivity_W_mK, строки 91-115
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null,  0.324,  0.300,  0.279,  0.261 },
        // temp: -17.8°C
        {  null,   null,  0.369,  0.338,  0.313,  0.291,  0.271,  0.255,  0.244 },
        // temp: -1.1°C
        {  0.462,  0.337,  0.311,  0.287,  0.268,  0.252,  0.239,  0.227,  0.225 },
        // temp: 15.6°C
        {  0.602,  0.456,  0.416,  0.382,  0.355,  0.327,  0.300,  0.275,  0.262 },
        // temp: 32.2°C
        {  0.744,  0.579,  0.527,  0.481,  0.445,  0.412,  0.377,  0.341,  0.313 },
        // temp: 48.9°C
        {  0.885,  0.702,  0.638,  0.580,  0.535,  0.493,  0.451,  0.402,  0.363 },
        // temp: 65.6°C
        {  1.027,  0.825,  0.749,  0.679,  0.625,  0.574,  0.523,  0.462,  0.412 },
        // temp: 82.2°C
        {  1.168,  0.948,  0.860,  0.778,  0.715,  0.655,  0.595,  0.522,  0.460 },
        // temp: 98.9°C
        {  1.310,  1.071,  0.971,  0.877,  0.805,  0.736,  0.667,  0.582,  0.508 }
    };
}
```

#### 4.2.5. Добавить fallback данные для пропиленгликоля

Добавить после методов этиленгликоля:

```csharp
/// <summary>
/// Fallback значения плотности для пропиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// Температуры ASHRAE: -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 °C
/// Концентрации: 10, 20, 30, 40, 50, 60, 70, 80, 90 vol%
/// </summary>
private static double[,] DefaultPropyleneDensityValues()
{
    // Данные из JSON: density_kg_m3, строки 184-212
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null, 1074.0, 1082.2, 1095.3, 1094.8 },
        // temp: -17.8°C
        {  null,   null,   null, 1073.6, 1083.2, 1081.6,   null,   null,   null },
        // temp: -1.1°C
        {  null,   null, 1036.0, 1047.0, 1054.0, 1062.0, 1066.0, 1069.0, 1069.0 },
        // temp: 15.6°C
        {  null, 1020.0, 1031.0, 1040.0, 1048.0, 1055.0, 1058.0, 1058.0, 1055.0 },
        // temp: 32.2°C
        {  null, 1014.0, 1025.0, 1033.0, 1040.0, 1046.0, 1047.0, 1044.0, 1039.0 },
        // temp: 48.9°C
        {  null, 1007.0, 1019.0, 1026.0, 1032.0, 1037.0, 1036.0, 1031.0, 1024.0 },
        // temp: 65.6°C
        {  null,  999.0, 1012.0, 1019.0, 1024.0, 1028.0, 1025.0, 1018.0, 1009.0 },
        // temp: 82.2°C
        {  null,  990.0, 1004.0, 1010.0, 1015.0, 1018.0, 1014.0, 1005.0,  994.0 },
        // temp: 98.9°C
        {  null,  981.0,  995.0, 1001.0, 1006.0, 1007.0, 1002.0,  991.0,  979.0 }
    };
}

/// <summary>
/// Fallback значения удельной теплоёмкости для пропиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// </summary>
private static double[,] DefaultPropyleneSpecificHeatValues()
{
    // Данные из JSON: specific_heat_kJ_kgK, строки 219-247
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null,   3.10,   2.85,   2.58,   2.27 },
        // temp: -17.8°C
        {  null,   null,   null,   3.58,   3.39,   3.17,   2.93,   2.67,   2.37 },
        // temp: -1.1°C
        {  null,   null,   4.05,   3.93,   3.76,   3.58,   3.38,   3.14,   2.87 },
        // temp: 15.6°C
        {  null,   null,   4.08,   3.97,   3.83,   3.68,   3.52,   3.33,   3.13 },
        // temp: 32.2°C
        {  null,   null,   4.10,   4.00,   3.89,   3.75,   3.61,   3.44,   3.28 },
        // temp: 48.9°C
        {  null,   null,   4.13,   4.04,   3.94,   3.82,   3.69,   3.54,   3.40 },
        // temp: 65.6°C
        {  null,   null,   4.15,   4.08,   3.99,   3.89,   3.78,   3.66,   3.53 },
        // temp: 82.2°C
        {  null,   null,   4.18,   4.12,   4.05,   3.96,   3.87,   3.77,   3.66 },
        // temp: 98.9°C
        {  null,   null,   4.20,   4.15,   4.09,   4.02,   3.94,   3.86,   3.77 }
    };
}

/// <summary>
/// Fallback значения кинематической вязкости для пропиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// </summary>
private static double[,] DefaultPropyleneViscosityValues()
{
    // Данные из JSON: kinematic_viscosity_mm2_s, строки 289-317
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null, 1203.67, 2092.20, 3299.03, 8600.39 },
        // temp: -17.8°C
        {  null,   null,   null,   98.99,  149.55,  277.95,  429.94,  735.26, 1350.63 },
        // temp: -1.1°C
        {  null,   null,    6.77,   10.23,   18.05,   31.74,   47.22,   81.47,  119.31 },
        // temp: 15.6°C
        {  null,   null,    3.87,    5.61,    8.76,   13.45,   19.57,   30.46,   43.20 },
        // temp: 32.2°C
        {  null,   null,    2.54,    3.46,    4.93,    6.97,    9.82,   13.96,   19.35 },
        // temp: 48.9°C
        {  null,   null,    1.81,    2.35,    3.14,    4.19,    5.71,    7.52,   10.23 },
        // temp: 65.6°C
        {  null,   null,    1.38,    1.72,    2.20,    2.81,    3.70,    4.62,    6.14 },
        // temp: 82.2°C
        {  null,   null,    1.06,    1.31,    1.64,    2.06,    2.59,    3.12,    4.09 },
        // temp: 98.9°C
        {  null,   null,    0.87,    1.04,    1.31,    1.60,    1.96,    2.27,    2.93 }
    };
}

/// <summary>
/// Fallback значения теплопроводности для пропиленгликоля
/// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
/// </summary>
private static double[,] DefaultPropyleneConductivityValues()
{
    // Данные из JSON: thermal_conductivity_W_mK, строки 254-282
    return new double[,]
    {
        // temp: -34.4°C
        {  null,   null,   null,   null,   null,  0.270,  0.242,  0.220,  0.203 },
        // temp: -17.8°C
        {  null,   null,   null,  0.348,  0.313,  0.280,  0.251,  0.227,  0.206 },
        // temp: -1.1°C
        {  null,   null,  0.455,  0.408,  0.365,  0.325,  0.293,  0.261,  0.234 },
        // temp: 15.6°C
        {  null,   null,  0.533,  0.477,  0.427,  0.385,  0.343,  0.300,  0.265 },
        // temp: 32.2°C
        {  null,   null,  0.556,  0.497,  0.444,  0.395,  0.350,  0.307,  0.270 },
        // temp: 48.9°C
        {  null,   null,  0.574,  0.512,  0.456,  0.407,  0.361,  0.315,  0.275 },
        // temp: 65.6°C
        {  null,   null,  0.585,  0.522,  0.466,  0.414,  0.367,  0.320,  0.279 },
        // temp: 82.2°C
        {  null,   null,  0.601,  0.535,  0.474,  0.419,  0.371,  0.323,  0.280 },
        // temp: 98.9°C
        {  null,   null,  0.604,  0.537,  0.476,  0.420,  0.371,  0.323,  0.280 }
    };
}
```

### 4.3. Добавить логирование при использовании fallback данных

#### 4.3.1. Добавить поле для логирования

В начало класса добавить:
```csharp
private static readonly ILogger? _logger;
```

#### 4.3.2. Модифицировать метод LoadData()

В блоке `catch` и при отсутствии файла добавить логирование:
```csharp
catch (Exception ex)
{
    // Логировать предупреждение
    System.Diagnostics.Debug.WriteLine($"[GlycolDataService] Ошибка загрузки JSON: {ex.Message}. Используются fallback данные.");
    
    // При ошибке парсинга используем встроенные данные
    _cachedJsonData = GetDefaultData();
}
```

---

## 5. Тесты для верификации

### 5.1. Файл тестов

Создать файл: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceTests.cs`

### 5.2. Тест 1: Проверка загрузки JSON

```csharp
[Test]
public void LoadData_JsonFileExists_ParsesCorrectly()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    
    // Act
    var ethyleneProps = service.GetProperties(GlycolType.Ethylene, 50, 37.8);
    var propyleneProps = service.GetProperties(GlycolType.Propylene, 50, 37.8);
    
    // Assert
    Assert.That(ethyleneProps.Density, Is.GreaterThan(0), "Этиленгликоль: плотность должна быть > 0");
    Assert.That(propyleneProps.Density, Is.GreaterThan(0), "Пропиленгликоль: плотность должна быть > 0");
}
```

### 5.3. Тест 2: Различие плотности гликолей

```csharp
[Test]
public void GetProperties_DifferentGlycolTypes_ReturnDifferentDensity()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    double concentration = 50;
    double temperature = 37.8; // Температура ASHRAE
    
    // Act
    var ethylene = service.GetProperties(GlycolType.Ethylene, concentration, temperature);
    var propylene = service.GetProperties(GlycolType.Propylene, concentration, temperature);
    
    // Assert
    // Этиленгликоль при 50% и 37.8°C: 1086.6 кг/м³ (из JSON)
    // Пропиленгликоль при 50% и 37.8°C: 1037 кг/м³ (из JSON)
    Assert.That(ethylene.Density, Is.GreaterThan(1080).And.LessThan(1095), 
        $"Этиленгликоль: плотность должна быть 1080-1095 кг/м³, получено {ethylene.Density}");
    Assert.That(propylene.Density, Is.GreaterThan(1030).And.LessThan(1045), 
        $"Пропиленгликоль: плотность должна быть 1030-1045 кг/м³, получено {propylene.Density}");
    Assert.That(Math.Abs(ethylene.Density - propylene.Density), Is.GreaterThan(40), 
        "Плотности должны различаться минимум на 40 кг/м³");
}
```

### 5.4. Тест 3: Различие вязкости гликолей

```csharp
[Test]
public void GetProperties_DifferentGlycolTypes_ReturnDifferentViscosity()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    double concentration = 50;
    double temperature = 37.8; // Температура ASHRAE
    
    // Act
    var ethylene = service.GetProperties(GlycolType.Ethylene, concentration, temperature);
    var propylene = service.GetProperties(GlycolType.Propylene, concentration, temperature);
    
    // Assert
    // Этиленгликоль при 50% и 37.8°C: 1.3 мм²/с (из JSON)
    // Пропиленгликоль при 50% и 37.8°C: 4.19 мм²/с (из JSON)
    Assert.That(propylene.KinematicViscosity, Is.GreaterThan(ethylene.KinematicViscosity), 
        "Пропиленгликоль должен иметь более высокую вязкость");
    // Разница должна быть значительной (минимум 200%)
    double ratio = propylene.KinematicViscosity / ethylene.KinematicViscosity;
    Assert.That(ratio, Is.GreaterThan(2.0), 
        $"Отношение вязкостей должно быть > 2.0, получено {ratio:F2}");
}
```

### 5.5. Тест 4: Различие теплоёмкости гликолей

```csharp
[Test]
public void GetProperties_DifferentGlycolTypes_ReturnDifferentSpecificHeat()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    double concentration = 50;
    double temperature = 37.8; // Температура ASHRAE
    
    // Act
    var ethylene = service.GetProperties(GlycolType.Ethylene, concentration, temperature);
    var propylene = service.GetProperties(GlycolType.Propylene, concentration, temperature);
    
    // Assert
    // Этиленгликоль при 50% и 37.8°C: 4.05 кДж/(кг·К) (из JSON)
    // Пропиленгликоль при 50% и 37.8°C: 3.90 кДж/(кг·К) (из JSON)
    Assert.That(ethylene.SpecificHeat, Is.GreaterThan(3.9).And.LessThan(4.2), 
        $"Этиленгликоль: теплоёмкость должна быть 3.9-4.2 кДж/(кг·К), получено {ethylene.SpecificHeat}");
    Assert.That(propylene.SpecificHeat, Is.GreaterThan(3.8).And.LessThan(4.0), 
        $"Пропиленгликоль: теплоёмкость должна быть 3.8-4.0 кДж/(кг·К), получено {propylene.SpecificHeat}");
}
```

### 5.6. Тест 5: Fallback данные различаются

```csharp
[Test]
public void GetDefaultData_DifferentGlycolTypes_ReturnDifferentFallbackValues()
{
    // Arrange
    var service = new GlycolDataService("non_existent_file.json"); // Файл не существует
    
    // Act
    var ethylene = service.GetProperties(GlycolType.Ethylene, 50, 37.8);
    var propylene = service.GetProperties(GlycolType.Propylene, 50, 37.8);
    
    // Assert - даже fallback данные должны различаться
    Assert.That(ethylene.Density, Is.Not.EqualTo(propylene.Density), 
        "Fallback плотность должна различаться для разных гликолей");
    Assert.That(ethylene.KinematicViscosity, Is.Not.EqualTo(propylene.KinematicViscosity), 
        "Fallback вязкость должна различаться для разных гликолей");
}
```

### 5.7. Тест 6: Проверка конкретных значений из ASHRAE

```csharp
[Test]
public void GetProperties_EthyleneGlycol_50Percent_37_8C_MatchesASHRAE()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    
    // Act
    var props = service.GetProperties(GlycolType.Ethylene, 50, 37.8);
    
    // Assert - проверка по данным ASHRAE из JSON
    // При 50% концентрации и 37.8°C:
    // Плотность: 1086.6 кг/м³ (строка 42, индекс 4 в values)
    // Вязкость: 1.3 мм²/с (строка 136, индекс 4 в values)
    // Теплоёмкость: 4.05 кДж/(кг·К) (строка 73, индекс 4 в values)
    // Теплопроводность: 0.475 Вт/(м·К) (строка 104, индекс 4 в values)
    Assert.That(props.Density, Is.GreaterThan(1080).And.LessThan(1095), 
        $"Плотность этиленгликоля 50% при 37.8°C должна быть ~1086.6 кг/м³, получено {props.Density}");
    Assert.That(props.KinematicViscosity, Is.GreaterThan(1.0).And.LessThan(1.5), 
        $"Вязкость этиленгликоля 50% при 37.8°C должна быть ~1.3 мм²/с, получено {props.KinematicViscosity}");
    Assert.That(props.SpecificHeat, Is.GreaterThan(3.9).And.LessThan(4.2), 
        $"Теплоёмкость этиленгликоля 50% при 37.8°C должна быть ~4.05 кДж/(кг·К), получено {props.SpecificHeat}");
}
```

### 5.8. Тест 7: Проверка пропиленгликоля из ASHRAE

```csharp
[Test]
public void GetProperties_PropyleneGlycol_50Percent_37_8C_MatchesASHRAE()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    
    // Act
    var props = service.GetProperties(GlycolType.Propylene, 50, 37.8);
    
    // Assert - проверка по данным ASHRAE из JSON
    // При 50% концентрации и 37.8°C:
    // Плотность: 1037 кг/м³ (строка 197, индекс 4 в values)
    // Вязкость: 4.19 мм²/с (строка 302, индекс 4 в values)
    // Теплоёмкость: 3.90 кДж/(кг·К) (строка 232, индекс 4 в values)
    // Теплопроводность: 0.444 Вт/(м·К) (строка 267, индекс 4 в values)
    Assert.That(props.Density, Is.GreaterThan(1030).And.LessThan(1045), 
        $"Плотность пропиленгликоля 50% при 37.8°C должна быть ~1037 кг/м³, получено {props.Density}");
    Assert.That(props.KinematicViscosity, Is.GreaterThan(3.5).And.LessThan(5.0), 
        $"Вязкость пропиленгликоля 50% при 37.8°C должна быть ~4.19 мм²/с, получено {props.KinematicViscosity}");
    Assert.That(props.SpecificHeat, Is.GreaterThan(3.8).And.LessThan(4.0), 
        $"Теплоёмкость пропиленгликоля 50% при 37.8°C должна быть ~3.90 кДж/(кг·К), получено {props.SpecificHeat}");
}
```

### 5.9. Тест 8: Интерполяция работает корректно

```csharp
[Test]
public void GetProperties_Interpolation_WorksCorrectly()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    
    // Act - температура между точками данных (37.8°C и 43.3°C)
    var props35 = service.GetProperties(GlycolType.Ethylene, 50, 35.0);
    var props40 = service.GetProperties(GlycolType.Ethylene, 50, 40.0);
    var props45 = service.GetProperties(GlycolType.Ethylene, 50, 45.0);
    
    // Assert - интерполированное значение должно быть между соседними
    Assert.That(props40.Density, Is.GreaterThan(props45.Density), 
        "Плотность должна уменьшаться с ростом температуры");
    Assert.That(props40.Density, Is.LessThan(props35.Density), 
        "Плотность должна уменьшаться с ростом температуры");
}
```

---

## 6. Критерии приёмки

### 6.1. Функциональные требования

| № | Критерий | Ожидаемый результат |
|---|----------|---------------------|
| 1 | JSON парсится корректно | `rawContainer.EthyleneGlycol != null`, `rawContainer.PropyleneGlycol != null` |
| 2 | Плотность этиленгликоля при 50%, 37.8°C | 1080-1095 кг/м³ (точное значение из JSON: 1086.6) |
| 3 | Плотность пропиленгликоля при 50%, 37.8°C | 1030-1045 кг/м³ (точное значение из JSON: 1037) |
| 4 | Разница плотностей | Минимум 40 кг/м³ |
| 5 | Вязкость пропиленгликоля > вязкости этиленгликоля | Отношение > 2.0 |
| 6 | Fallback данные различаются | Плотности и вязкости различаются |
| 7 | Интерполяция работает | Значения между точками данных корректны |
| 8 | Fallback использует температуры ASHRAE | Те же температуры, что и JSON |

### 6.2. Нефункциональные требования

| № | Критерий | Ожидаемый результат |
|---|----------|---------------------|
| 1 | Производительность | Загрузка JSON < 100 мс |
| 2 | Кэширование | Повторные вызовы используют кэш |
| 3 | Логирование | Предупреждение при использовании fallback |
| 4 | Обработка ошибок | Graceful degradation при отсутствии файла |

### 6.3. Тестовое покрытие

- Минимум 8 unit-тестов
- Покрытие кода > 80%
- Все тесты проходят успешно

---

## 7. Ограничения и допущения

### 7.1. Технические ограничения
- .NET 8, C# 12
- System.Text.Json для парсинга
- Файл JSON должен быть в кодировке UTF-8

### 7.2. Бизнес-ограничения
- Диапазон концентраций: 10-90%
- Диапазон температур: -34.4°C до 121.1°C (температуры ASHRAE)
- Данные из ASHRAE Handbook

### 7.3. Допущения
- Fallback данные являются точными значениями из ASHRAE (подмножество JSON)
- Интерполяция линейная между точками данных
- Данные JSON считаются корректными

---

## 8. Открытые вопросы

1. **Нужно ли добавить логирование через ILogger?** — Сейчас используется `System.Diagnostics.Debug.WriteLine`. Рекомендуется добавить полноценное логирование.

2. **Нужно ли добавить валидацию JSON?** — Проверка структуры данных при загрузке.

3. **Нужно ли добавить кэширование с инвалидацией?** — При изменении файла JSON кэш должен обновляться.

---

## 9. Справочный вывод характеристик

### 9.1. Требование к UI

Добавить в интерфейс отображение характеристик выбранного гликоля:
- Тип гликоля (этиленгликоль / пропиленгликоль)
- Концентрация (%)
- Температура (°C)
- Плотность (кг/м³)
- Кинематическая вязкость (мм²/с)
- Удельная теплоёмкость (кДж/(кг·К))
- Теплопроводность (Вт/(м·К))
- Число Прандтля (безразмерное)

### 9.2. Пример вывода (точные значения из JSON)

```
Этиленгликоль 50% при 37.8°C:
  Плотность: 1086.6 кг/м³
  Вязкость: 1.3 мм²/с
  Теплоёмкость: 4.05 кДж/(кг·К)
  Теплопроводность: 0.475 Вт/(м·К)

Пропиленгликоль 50% при 37.8°C:
  Плотность: 1037 кг/м³
  Вязкость: 4.19 мм²/с
  Теплоёмкость: 3.90 кДж/(кг·К)
  Теплопроводность: 0.444 Вт/(м·К)
```

---

## 10. Файлы для изменения

| Файл | Изменения |
|------|-----------|
| `src/Services/Hydraulics/GlycolDataService.cs` | Добавить `[JsonPropertyName]`, создать отдельные fallback методы с данными ASHRAE |
| `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceTests.cs` | Создать файл с тестами |

---

## 11. Приоритет

**Высокий** — Блокирует корректную работу гидравлического расчёта.