# phase-8-results-derived-projection — Amendment 1: ColdPeriodDays canonicalization (owner decision B)

Статус: поправка к frozen-плану `phase-8-results-derived-projection.md`
(SHA-256 `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67`).
Создана по явному owner-решению от 2026-09-03 (выбор «B: Канонизировать в
slice» на `OWNER_DECISION_REQUIRED` из slice 2). Канонический план не
редактируется; поправка сужает/расширяет write-set slice 3 и подлежит одному
terminal review (ZCode environment-adaptive rules, combined review).

## Что изменилось в write-set slice 3

Добавляются (расширение production/test write-set):

1. `src/Services/Project/ClimateStateSnapshot.cs` — additive: 12-й позиционный
   параметр `int Period0Days = 0` (дефолт сохраняет компиляцию существующих
   сайтов конструирования: `ProjectPersistenceMapperTests.cs:143`,
   `ProjectSaveServiceTests.cs:50`, `ProjectSnapshotFactoryTests.cs:17`).
   In-memory only: `ClimateProjectData`/wire `.smc` не изменяются.
2. `src/Services/Project/ProjectSessionClimateState.cs` — поле `_period0Days`;
   извлечение `city.Period_0_Days` в точках, где состояние уже получает
   `CityInfo`: `ApplyCitySelection` (`city?.Period_0_Days ?? 0`),
   `ApplyProjectSnapshot` (`city?.Period_0_Days ?? 0`),
   `ResetToCityData` (`city.Period_0_Days`, city гарантированно non-null),
   `ResetToDefaults` (0); `Snapshot` включает поле. `ApplyIndividualEdit`
   город не меняет — поле сохраняется.
3. Климатические тесты (`ClimateStateTests`, `ProjectSessionTests`) —
   characterization нового поля + регрессия существующих мутаций.

НЕ меняются (must-NOT-have сохранён): `ProjectLoadOrchestrator` — city уже
резолвится по имени на restore-пути (`ProjectLoadOrchestrator.cs:147-149`,
`FindCityByName` → `ApplyProjectSnapshot(data.ClimateData, city, Load)`),
поэтому restore-путь получает каноническое значение без правок оркестратора.
Писатели `CalculationContext` не добавляются. Persistence-мапперы не трогаются.

## Re-source ColdPeriodDays в Results (slice 3)

```csharp
ColdPeriodDays = string.IsNullOrEmpty(snapshot.SelectedCity)
    ? 150
    : snapshot.Period0Days;
```

Таблица побайтовой эквивалентности с текущим поведением
(`_climateViewModel.SelectedCity?.Period_0_Days ?? 150`):

| Сценарий | Текущее поведение | Каноническое поведение | Эквивалент |
|---|---|---|---|
| Нет города (пустое имя) | `SelectedCity == null` → 150 | пустое имя → 150 | да |
| Город выбран из каталога | `catalogCity.Period_0_Days` | `ApplyCitySelection(city)` → `city.Period_0_Days` | да |
| Restore, город найден | VM re-resolve `GetCityByName` → каталог | оркестратор передаёт `FindCityByName(...)` → то же значение | да |
| Restore/выбор, город не найден | fabricated `CityInfo{Name,Region}` → `Period_0_Days = 0` | `city == null` → 0 | да |

## Design note: Layers (LoadConstructionData) — без расширения снапшота

`ResultsPdfDataBuilder.cs:97-108` — единственный потребитель `Results.Layers`
(XAML-биндингов нет, проверено). Читает `Material?.Name`, `Thickness`,
`CalculatedLambda`, `CalculatedR`, `Position`:

- `CalculatedR` — вычисляемое свойство `Layer` (`Layer.cs:119-128`):
  `Thickness / CalculatedLambda / 1000`, `0` при λ≤0 — формула идентична
  канонической (`ConstructionStateProjection.cs:9-11`), реконструкция
  воспроизводит её автоматически; в снапшот R добавлять не нужно.
- `Material` резолвится из `IMaterialRepository.GetMaterialById(MaterialId)`
  (тот же экземпляр, что держит адаптер — материалы синхронизируются по id с
  сохранением ссылок); при промахе — стаб `new Material { Id, Name =
  MaterialName ?? "Не указан" }` (точная эквивалентность PDF-вывода во всех
  случаях, включая вырожденный).
- Реконструкция — в порядке присваиваний `Layer.Clone()`
  (`Layer.cs:133-147`): Id → Material (сеттер сбрасывает λ на `LambdaA`) →
  Thickness → CalculatedLambda (снапшотное значение побеждает) →
  IsLambdaOverridden → Position → Order.

## Terminal review (combined, ZCode mode)

REVIEW_ID: TERMINAL-AMENDMENT-P8-ZCODE-1
SUBJECT: `docs/architecture-migration/plans/phase-8-results-derived-projection.amendment-1-coldperioddays-canonicalization.md`
RECEIPT: this file (inline)
VERDICT: APPROVE
REASON: Поправка реализует явное owner-решение B; каждое проектное допущение
проверено по живому коду (resolve города оркестратором уже существует;
формула CalculatedR идентична канонической; порядок реконструкции Layer
воспроизводит Clone(); все сайты конструирования снапшота переживут additive
параметр; XAML-потребителей у Results.Layers нет). Must-NOT-have не нарушены:
оркестратор, писатели CalculationContext, wire-формат и persistence-мапперы
не затрагиваются. Эквивалентность четырёх сценариев ColdPeriodDays доказуема
тестами; план проверки — расширенный фильтр slice 3 (стабилизация +
open-project + ClimateStateTests + ProjectSessionTests).
