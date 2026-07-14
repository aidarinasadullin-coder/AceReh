# Установка Калькулятора снеготаяния РЕХАУ

## Системные требования

- **ОС:** Windows 10/11 (64-bit)
- **RAM:** 4 GB минимум
- **Диск:** 150 MB свободного места

## Способ 1: MSI-установщик (рекомендуется)

1. Скачайте `SnowMeltingCalculator-v1.0.exe` (Bootstrapper)
2. Запустите файл
3. Следуйте инструкциям мастера установки:
   - Примите лицензионное соглашение
   - Выберите папку установки (по умолчанию: `C:\Program Files\REHAU\SnowMeltingCalculator`)
   - Дождитесь завершения установки .NET 8 Runtime (при необходимости)
4. Готово! Ярлык появится в меню Пуск → REHAU → Калькулятор снеготаяния

**Примечание:** Если .NET 8 Desktop Runtime уже установлен, установка займёт 1-2 минуты. Если нет — потребуется скачивание (~50 MB) и установка .NET 8.

## Способ 2: Портативная версия

1. Скачайте `SnowMeltingCalculator-v1.0-Portable.zip`
2. Распакуйте архив в любую папку
3. Запустите `SnowMeltingCalculator.exe`

**Требования:** Установленный .NET 8 Desktop Runtime

## Сборка из исходников

### Требования

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WiX Toolset v4](https://wixtoolset.org/) (для сборки MSI)

### Шаги сборки

```powershell
# 1. Клонирование репозитория
cd ace/

# 2. Восстановление зависимостей
dotnet restore

# 3. Сборка Release
dotnet build -c Release

# 4. Запуск тестов
dotnet test -c Release --no-build

# 5. Публикация (Self-contained)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

# 6. Запуск
./publish/SnowMeltingCalculator.exe
```

### Сборка MSI-установщика

```powershell
# 1. Публикация приложения
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

# 2. Сборка MSI
cd installer/
dotnet build -c Release

# 3. Результат
# ./installer/bin/Release/SnowMeltingCalculator.msi
# ./installer/bin/Release/SnowMeltingCalculator.exe (Bootstrapper с .NET)
```

## Решение проблем

### "Не найден .NET SDK"

Установите .NET 8 SDK:
```powershell
winget install Microsoft.DotNet.SDK.8
```

### "Ошибка при запуске приложения"

Проверьте наличие файлов данных в папке `data/`:
- `climate_db.json`
- `glycol_data.json`
- `materials_db.json`
- `rehau_products.json`

### "MSI не собирается"

Убедитесь, что установлен WiX Toolset v4:
```powershell
dotnet tool install --global wix
wix --version
```

## Структура установки

```
C:\Program Files\REHAU\SnowMeltingCalculator\
├── SnowMeltingCalculator.exe    # Запускаемый файл
├── SnowMeltingCalculator.dll    # Основная сборка
├── *.dll                        # Зависимости
├── data\                        # Базы данных
│   ├── climate_db.json
│   ├── glycol_data.json
│   ├── materials_db.json
│   └── rehau_products.json
└── Assets\Fonts\                # Шрифты
    └── Inter-*.ttf
```

## Удаление

**Способ 1:** Панель управления → Программы → Калькулятор снеготаяния РЕХАУ → Удалить

**Способ 2:** Запустить MSI снова и выбрать "Удалить"

---

*Версия: 1.0.0 | Дата: 2026-04-06*
