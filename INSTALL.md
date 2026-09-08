# Установка Калькулятора снеготаяния РЕХАУ

## Системные требования

- **ОС:** Windows 10/11 (64-бит)
- **RAM:** 4 GB минимум
- **Диск:** ~250 MB свободного места (развёрнутая поставка ~200 MB)
- **.NET устанавливать не нужно:** рантайм .NET 8 входит в состав поставки (self-contained)

## Установка (Setup.exe, рекомендуется)

1. Запустите `SnowMeltingCalculator-v1.1.2-Setup.exe` (~60 MB)
2. Следуйте инструкциям мастера установки:
   - Примите лицензионное соглашение
   - Выберите папку установки (по умолчанию: `C:\Program Files\REHAU\SnowMeltingCalculator`)
   - При желании создайте ярлык на рабочем столе
3. Готово! Ярлык появится в меню Пуск → REHAU → Калькулятор снеготаяния

Установщик также регистрирует файловую ассоциацию: проекты `.smc` открываются
приложением двойным кликом.

## Портативный запуск

Папка `publish\` после публикации самодостаточна: её можно скопировать на любую
машину с Windows x64 и запустить `SnowMeltingCalculator.exe` без установки
и без .NET.

## Сборка из исходников

### Требования

- Windows 10/11 (64-бит)
- [Git](https://git-scm.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6.3+](https://jrsoftware.org/isdl.php) — только для шага сборки установщика

### Сборка и тесты

```powershell
# 1. Клонирование репозитория
git clone <url-репозитория> ace
cd ace/

# 2. Восстановление зависимостей
dotnet restore

# 3. Сборка Release
dotnet build -c Release

# 4. Запуск тестов
dotnet test -c Release
```

### Публикация

```powershell
dotnet publish src/SnowMeltingCalculator.csproj -c Release -r win-x64 --self-contained true -o ./publish

# Запуск
./publish/SnowMeltingCalculator.exe
```

**Важно:** указывайте путь к проекту явно. Команда без пути из корня репозитория
подхватывает `SnowMeltingCalculator.sln` и публикует вместе с тестовым проектом —
в `publish\` попадает тестовый мусор (FlaUI, CodeCoverage и т.д.), который уйдёт
в инсталлятор.

Параметры `SelfContained`, `RuntimeIdentifier` и `PublishReadyToRun` уже заданы
в `src/SnowMeltingCalculator.csproj`; флаги в команде оставлены для явности.

## Сборка установщика (Inno Setup)

```powershell
# 1. Публикация приложения (см. шаг выше) — папка publish\ должна существовать

# 2. Компиляция установщика
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\SnowMeltingCalculator.iss

# 3. Результат
# output\SnowMeltingCalculator-v1.1.2-Setup.exe
```

- Установщик пакует содержимое `publish\*` (без `*.pdb`), сжатие LZMA solid,
  итоговый размер ~60 MB.
- Версия задаётся в двух местах и должна совпадать: `<Version>` в
  `src/SnowMeltingCalculator.csproj` и `#define MyAppVersion` в
  `installer/SnowMeltingCalculator.iss`.

## Решение проблем

### Приложение не запускается

Проверьте наличие файлов данных в папке `data\` рядом с exe:
- `climate_db.json`
- `glycol_data.json`
- `materials_db.json`
- `rehau_products.json`

### Файл .smc не открывается двойным кликом

Переустановите приложение — установщик регистрирует ассоциацию `.smc`.

### "dotnet не является внутренней или внешней командой"

Установите .NET 8 SDK:
```powershell
winget install Microsoft.DotNet.SDK.8
```

### ISCC не найден

Установите [Inno Setup 6](https://jrsoftware.org/isdl.php) — по умолчанию
в `C:\Program Files (x86)\Inno Setup 6\`.

## Структура установленной папки

```
C:\Program Files\REHAU\SnowMeltingCalculator\
├── SnowMeltingCalculator.exe     # запускаемый файл
├── SnowMeltingCalculator.dll     # сборка приложения
├── *.dll                         # рантайм .NET 8 (self-contained) и зависимости
├── data\                         # базы данных
│   ├── climate_db.json           # климаты, 550 городов РФ (СП 131.13330.2025)
│   ├── glycol_data.json          # свойства этилен-/пропиленгликоля
│   ├── materials_db.json         # теплопроводность материалов
│   └── rehau_products.json       # трубы RAUTHERM S, коллекторы HKV и IV
├── docs\Инструкция полная\       # полная инструкция пользователя
│   ├── README.html               # открывается из меню «Файл → Инструкция»
│   └── media\                    # скриншоты и GIF-демонстрации инструкции
├── LatoFont\                     # шрифты (лицензия OFL)
└── cs\ de\ … zh-Hant\            # локализованные ресурсы .NET-рантайма
```

Шрифты Inter, используемые интерфейсом, встроены в сборку приложения
и отдельной папкой не идут.

## Удаление

**Способ 1:** Параметры Windows → Приложения → «Калькулятор снеготаяния РЕХАУ» → Удалить

**Способ 2:** Панель управления → Программы и компоненты → Удалить

Файлы проектов `.smc`, созданные пользователем, при удалении не затрагиваются.

---

*Версия: 1.1.2 | Дата: 2026-09-07*
