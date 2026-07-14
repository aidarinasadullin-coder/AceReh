# Draft — refactor-deduplicate-params

## Routing
- **intent: UNCLEAR** — бриф открытый («думаю есть дубли», «навести порядок», «посмотреть архитектуру»), исход не артикулирован.
- **review_required: true** (UNCLEAR + Architecture → авто dual high-accuracy review после approval, нет Trivial-уровня).
- **Classify: Architecture** (5+ модулей, долгосрочное влияние: Climate/Thermal/Construction/Hydraulics/Results + контрактный слой + константы).

## Подход (adopted best-practice, behavior-preserving, NOT rewrite)
Последовательный инкрементный рефакторинг в стиле Fowler: каждое изменение мелкое, поведение сохранено, тесты зелёные на каждом шаге. Единая точка истины на параметр; плоские snapshot-DTO и прямые ViewModel→ViewModel ссылки мигрируют на существующий контрактный слой (`IClimateData`, `IConstructionData`, `ICalculationStateService`, `CalculationContext`).

## Собранные факты (evidence)
- `ThermalParameters` (src/Models/Thermal/ThermalParameters.cs) повторно объявляет `AirTemperature`/`WindSpeed`/`SnowfallIntensity` (из ClimateParameters/IClimateData) и `R1Total`/`R2Total` (из IConstructionData). Calculator получает клон, а не контракты.
- `ThermalCalculationResult` эхо-возвращает входы: `SupplyTemperature`, `ReturnTemperature`, `DeltaT`, `R1Total`, `R2Total`, `Pipe`, `PipeSpacing` — не реальные «выходы».
- `HydraulicInputData` дублирует `PowerUp`/`PowerDown`/`SupplyTemperature`/`ReturnTemperature`/`InnerDiameter` (из ThermalCalculationResult) и `ColdFiveDayTemperature` (из ClimateData), синхронизируется вручную из VM (drift risk).
- `PipeSpacing` (мм) — 3 конкурирующих источника: `ThermalViewModel.PipeSpacing` (пишет в `ICalculationStateService.SetPipeSpacing`), `ConstructionViewModel.PipeSpacing` (читает из `ICalculationStateService.PipeSpacingChanged` И выставляет через `OnPipeSpacingChanged`), и `ResultsViewModel` читает ИЗ ОБОИХ (`_constructionViewModel.PipeSpacing` line 662, `_thermalViewModel.PipeSpacing` lines 1046/1732). `CircuitsViewModel.PipeSpacing_cm` = `/10.0` от ThermalVM; `CircuitRow.PipeSpacing_cm`; `CircuitData.PipeSpacingCm` (serialization). 6+ мест, 2 единицы (мм/см).
- Константа `MinPipeSpacing`/`MaxPipeSpacing` объявлена ДВАЖДЫ: `ValidationConstants` (lines 128/133) И `ThermalConstants` (lines 189/194). `ValidationExtensions.ValidatePipeSpacing` ссылается на `ValidationConstants`; `ThermalCalculator.Validate` — на `ThermalConstants`.
- Корневой пустой каталог `D:\IA\ace\ViewModels\Hydraulics\` (0 entries) — мёртвый, дублирует `src/ViewModels/`.
- `CalculationContext` (src/Core/CalculationContext.cs) — UЖЕ правильная централизованная агрегация (делегирующие свойства к Climate/Construction/ThermalResult), но ViewModels его НЕ используют как канал связи: `CircuitsViewModel` инжектит `ThermalViewModel` и `ClimateViewModel` напрямую Параллельная неформальная шина = «split-brain».

## Adopted-defaults ledger (reversible internal — не owner-decision ветки)
| # | Дефолт | Рационал | Reversible? |
|---|--------|----------|-------------|
| D1 | Scope = behavior-preserving consolidation, НЕ rewrite и НЕ параллельная новая архитектура | Fowler; минимальный риск для готового v1.0 продукта | Да |
| D2 | Единая точка истины на параметр; остальные становятся delegating props либо удаляются | устраняет drift | Да |
| D3 | Забой файлов / шага — мм как канон (как в ThermalConstants за вычетом дубля) | совпадает с физикой проекта | Да |
| D4 | PipeSpacing канонический.owner = `ThermalViewModel`; `ConstructionViewModel`/`ResultsViewModel` — наблюдатели через `ICalculationStateService.PipeSpacingChanged` (уже есть); убрать конкурирующие write-пути | подтверждается структурой (Set в ThermalVM уже есть) | Да |
| D5 | Константы: оставить `ValidationConstants`; удалить дубль из `ThermalConstants` | одно назначение | Да |
| D6 | `ThermalParameters` → immutable record `ThermalInputs` (только thermal-поля: Mode, SupplyTemperature, DeltaT, GroundTemperature, Pipe, PipeSpacing, LambdaE); климат/конструкция передаются калькулятору как `IClimateData`/`IConstructionData` контракты, а НЕ копируются | удаляет cross-domain дублирование | Да |
| D7 | `ThermalCalculationResult`: убрать эхо-входы (`Pipe`, `PipeSpacing`, `R1Total`, `R2Total`, `DeltaT`, `SupplyTemperature`/`ReturnTemperature` остаются КАК вычисленные выходы только если они реально вычисляются; в `ToString`/PDF — читать из источника) | уточнить из Testового следа | Да |
| D8 | `HydraulicInputData`: оставить как user-input DTO (гликоль, supply spacing, supply heat %, valve type); убрать копии thermal/climate полей, калькулятор читает из контрактов | единая модель | Да |
| D9 | Удалить мёртвый корневой `D:\IA\ace\ViewModels\Hydraulics\` | мусор | Да |
| D10 | TDD: characterization-тесты на текущие потоки данных ДО рефакторинга + каждый шаг зелёный; build + `dotnet test` — ворота | охрана поведения | Да |

## Owner-decision (поверхностный fork — не блокирует, но громко в brief)
- **Ambition scope fork**: full architectural consolidation contract-layer (D1-D10) vs минимальный cosmetic dedupe-токен (только D5/D9 + убрать мёртвые поля). Дефолт D1-D10 (behaviour-preserving consolidation). Если пользователь хотел меньше — veto в TL;DR.

## Gate
- **status: awaiting-approval**
- **pending action**: run scaffold → append todos → Metis → dual high-accuracy review → present
- **SLUG**: refactor-deduplicate-params