# phase-2-climate-state - Task 1 Baseline Report

## Scope

This report replaces the invalid placeholder baseline report. It is derived only from the four already saved raw Git snapshots in this directory. No new repository snapshot was taken for this report.

Input files:
- `baseline-git-status.bin`
- `baseline-git-diff-name-only.bin`
- `baseline-git-cached-diff-name-only.bin`
- `post-git-status.bin`

## Checksums and record counts

| File | Bytes | NUL bytes | Non-empty records | SHA-256 |
|---|---:|---:|---:|---|
| `baseline-git-status.bin` | 13028 | 226 | 225 status records + 1 branch header | `42BD4EE368BAAE77D37E2B306780945F436A2D2FD5FCBD09202EC3E3113597C3` |
| `baseline-git-diff-name-only.bin` | 899 | 13 | 13 paths | `574252FE538E15F64722BAC6909294675F0001809133FE3E1C956B9B94475FD0` |
| `baseline-git-cached-diff-name-only.bin` | 0 | 0 | 0 paths | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `post-git-status.bin` | 13028 | 226 | 225 status records + 1 branch header | `42BD4EE368BAAE77D37E2B306780945F436A2D2FD5FCBD09202EC3E3113597C3` |

## Branch header

- Baseline: `## master...origin/master [ahead 20]`
- Post: `## master...origin/master [ahead 20]`

## Baseline status summary

- Total status records: 225
- Modified: 215
- Deleted: 0
- Untracked/new: 10
- Other status records: 0
- Staged changes: 0

## Staged changes

- none

## Modified files

- `.gitignore`
- `.omo/evidence/fix-design-temperature-source/f1-plan-compliance-corrected.txt`
- `.omo/evidence/fix-design-temperature-source/f1-plan-compliance.txt`
- `.omo/evidence/fix-design-temperature-source/f2-code-quality.txt`
- `.omo/evidence/fix-design-temperature-source/f3-manual-qa.txt`
- `.omo/evidence/fix-design-temperature-source/f4-scope-fidelity.txt`
- `.omo/evidence/fix-design-temperature-source/ui-tree-discovery.txt`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/construction-dump.txt`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/f2-code-quality.txt`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/f4-scope-fidelity.txt`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-manual-qa.txt`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/thermal-dump.txt`
- `.omo/evidence/refactor-dedupe-params/f2/_build_src.txt`
- `.omo/evidence/refactor-dedupe-params/f2/_build_tests.txt`
- `.omo/evidence/refactor-dedupe-params/f2/_format_raw.txt`
- `.omo/evidence/refactor-dedupe-params/f2/_src_files.txt`
- `.omo/evidence/refactor-dedupe-params/f2/_test_files.txt`
- `.omo/evidence/refactor-dedupe-params/f4/f4-scope-fidelity.txt`
- `.omo/evidence/refactor-dedupe-params/task-1a/baseline_refactor_dedupe.json`
- `.omo/evidence/refactor-dedupe-params/task-1a/git-diff-stat.txt`
- `.omo/evidence/refactor-dedupe-params/task-3/task-3-refactor-dedupe-params.txt`
- `.omo/notepads/fix-delta-t-input/learnings.md`
- `.omo/notepads/fix-glycol-concentration-constants/learnings.md`
- `.omo/notepads/refactor-dedupe-params/learnings.md`
- `.omo/notepads/refactor-hydraulics-tests/learnings.md`
- `.omo/notepads/unify-validation/learnings.md`
- `.omo/run-continuation/ses_0a358c786ffeXzxbyBqAXxW7oK.json`
- `.omo/start-work/ledger.jsonl`
- `PROJECT_STATUS.md`
- `README.md`
- `build_temp/SnowMeltingCalculator.deps.json`
- `build_temp/SnowMeltingCalculator.runtimeconfig.json`
- `docs/architecture-migration/TASK_CONTEXT.md`
- `docs/architecture-migration/maps/target-invariants.md`
- `docs/formulas/traceability-matrix.md`
- `docs/workspace/rehau_assets/brand.css`
- `docs/workspace/rehau_assets/element_squares_large.svg`
- `docs/workspace/rehau_assets/element_squares_small.svg`
- `docs/workspace/rehau_assets/logo_black.svg`
- `docs/workspace/rehau_assets/logo_icon_color.svg`
- `docs/workspace/rehau_assets/logo_main_color.svg`
- `docs/workspace/rehau_assets/logo_white.svg`
- `docs/workspace/rehau_assets/logo_wordmark_black.svg`
- `docs/workspace/rehau_assets/palette.json`
- `docs/workspace/rehau_assets/pattern_black_bg.svg`
- `docs/workspace/rehau_assets/pattern_green_bg.svg`
- `docs/workspace/rehau_assets/pattern_white_bg.svg`
- `docs/workspace/rehau_assets/slogan_icon_black.svg`
- `docs/workspace/rehau_assets/slogan_text_black.svg`
- `docs/workspace/rehau_assets/slogan_with_icon_black.svg`
- `docs/Планируемые_изменения.md`
- `docs/Руководство_пользователя.md`
- `docs/инструкция/README v.2.2 kimi.html`
- `docs/инструкция/README v.2.3 kimi.html`
- `docs/инструкция/README.md`
- `docs/инструкция/images/brand/element_squares_large.svg`
- `docs/инструкция/images/brand/logo_black.svg`
- `docs/инструкция/images/brand/logo_main_color.svg`
- `docs/инструкция/images/brand/logo_white.svg`
- `docs/инструкция/images/brand/slogan_text_black.svg`
- `installer/SnowMeltingCalculator.iss`
- `publish/LatoFont/OFL.txt`
- `publish/SnowMeltingCalculator.deps.json`
- `publish/SnowMeltingCalculator.pdb`
- `publish/SnowMeltingCalculator.runtimeconfig.json`
- `src/App.xaml.cs`
- `src/Assets/Brand/logo_icon_color.svg`
- `src/Behaviors/DataGridBehavior.cs`
- `src/Behaviors/TextBoxBehavior.cs`
- `src/Controls/Climate/CityAutoCompleteBox.xaml.cs`
- `src/Controls/RecalcIndicator.xaml.cs`
- `src/Converters/CityMatchToHighlightConverter.cs`
- `src/Converters/Converters.cs`
- `src/Models/Construction/Construction.cs`
- `src/Models/Construction/ConstructionTemplate.cs`
- `src/Models/Construction/Layer.cs`
- `src/Models/Construction/Material.cs`
- `src/Models/Construction/MaterialSnapshot.cs`
- `src/Models/Enums/RecalcState.cs`
- `src/Models/Hydraulics/CircuitRow.cs`
- `src/Models/Hydraulics/Collector.cs`
- `src/Models/Hydraulics/CollectorSummary.cs`
- `src/Models/Hydraulics/CollectorType.cs`
- `src/Models/Hydraulics/FlowRegime.cs`
- `src/Models/Hydraulics/GlycolDataModels.cs`
- `src/Models/Hydraulics/GlycolProperties.cs`
- `src/Models/Hydraulics/GlycolType.cs`
- `src/Models/Hydraulics/HydraulicMode.cs`
- `src/Models/Hydraulics/ValveType.cs`
- `src/Models/Navigation/ModuleStateChangedEventArgs.cs`
- `src/Models/Project/ProjectData.cs`
- `src/Models/Thermal/IConstructionData.cs`
- `src/Models/Thermal/OperatingMode.cs`
- `src/Models/Thermal/PipeType.cs`
- `src/Models/Thermal/ThermalCalculationResult.cs`
- `src/Repositories/ClimateDataRepository.cs`
- `src/Repositories/Construction/ConstructionRepository.cs`
- `src/Repositories/Construction/ConstructionTemplateRepository.cs`
- `src/Repositories/Construction/IConstructionTemplateRepository.cs`
- `src/Repositories/Construction/IMaterialRepository.cs`
- `src/Repositories/Construction/MaterialRepository.cs`
- `src/Repositories/Hydraulics/CollectorRepository.cs`
- `src/Repositories/Hydraulics/ICollectorRepository.cs`
- `src/Resources/Dictionary.xaml`
- `src/Services/Climate/ClimateDataService.cs`
- `src/Services/Climate/ISearchHistoryService.cs`
- `src/Services/Climate/SearchHistoryService.cs`
- `src/Services/Construction/ConstructionService.cs`
- `src/Services/Construction/ConstructionTemplateValidator.cs`
- `src/Services/Construction/ConstructionValidator.cs`
- `src/Services/Construction/IConstructionService.cs`
- `src/Services/Construction/MaterialCrudValidator.cs`
- `src/Services/Construction/MaterialNotFoundException.cs`
- `src/Services/Hydraulics/CircuitsCalculator.cs`
- `src/Services/Hydraulics/FlowRegimeCalculator.cs`
- `src/Services/Hydraulics/GlycolDataService.cs`
- `src/Services/Hydraulics/ICircuitsCalculator.cs`
- `src/Services/Hydraulics/IGlycolDataService.cs`
- `src/Services/Navigation/EditorDialogService.cs`
- `src/Services/Navigation/IEditorDialogService.cs`
- `src/Services/Project/ProjectFileService.cs`
- `src/Services/Results/PdfExportService.cs`
- `src/Services/Thermal/ThermalCalculator.cs`
- `src/Services/Visualization/ConstructionVisualizationRenderer.cs`
- `src/SnowMeltingCalculator.csproj`
- `src/ViewModels/Climate/ClimateViewModel.cs`
- `src/ViewModels/Construction/ConstructionViewModel.cs`
- `src/ViewModels/Construction/MaterialEditorViewModel.cs`
- `src/ViewModels/Construction/TemplateEditorViewModel.cs`
- `src/ViewModels/Hydraulics/CollectorViewModel.cs`
- `src/ViewModels/Shared/RecalcIndicatorViewModel.cs`
- `src/ViewModels/Thermal/ThermalViewModel.cs`
- `src/Views/Construction/ConstructionView.xaml`
- `src/Views/Construction/MaterialEditorView.xaml`
- `src/Views/Construction/MaterialEditorView.xaml.cs`
- `src/Views/Construction/TemplateEditorView.xaml`
- `src/Views/Construction/TemplateEditorView.xaml.cs`
- `src/Views/Results/ResultsView.xaml`
- `src/Views/Shared/ConstructionVisualizationView.xaml.cs`
- `tests/SnowMeltingCalculator.Tests/AttachedProperties/InlinesPropertyTests.cs`
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateDataServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionRepositoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTemplateImportTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateRepositoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateValidatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionValidatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/MaterialCrudValidatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/MaterialEditorViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryCrudTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryMigrationVerification.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/MaterialSnapshotTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/TemplateEditorViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/Converters/CityMatchToHighlightConverterTests.cs`
- `tests/SnowMeltingCalculator.Tests/Converters/PressureColorConverterTests.cs`
- `tests/SnowMeltingCalculator.Tests/Converters/SidebarTooltipConverterTests.cs`
- `tests/SnowMeltingCalculator.Tests/Core/ValidationExtensionsTests.cs`
- `tests/SnowMeltingCalculator.Tests/Integration/HydraulicsIntegrationTests.cs`
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs`
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/GlycolAutoRecalculationTests.cs`
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs`
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs`
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitRowTests.cs`
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CollectorSummaryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CollectorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/EnumsTests.cs`
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/GlycolPropertiesTests.cs`
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/ValveTypeTests.cs`
- `tests/SnowMeltingCalculator.Tests/Repositories/Climate/SearchHistoryRepositoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/CollectorRepositoryJsonLoadingTests.cs`
- `tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/CollectorRepositoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/ICollectorRepositoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/AppSettingsTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Climate/SearchHistoryServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/FlowRegimeCalculatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceJsonLoadingTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolInterpolationTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/IGlycolDataServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Navigation/EditorDialogServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Thermal/ThermalCalculatorTests.cs`
- `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`
- `tests/SnowMeltingCalculator.Tests/Views/ConstructionVisualizationRendererTests.cs`
- `tests/SnowMeltingCalculator.Tests/baseline_refactor_dedupe.json`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx`
- `Тест/1.smc`
- `Тест/_20260724.smc`
- `Тест/Детальный_отчёт_рабочий_9-100000_20260727.md`
- `Тест/Екат 1.smc`
- `Тест/Екат 1.smc.bak`
- `Тест/Екат для версии 1.1.smc`
- `Тест/Екат.smc`
- `Тест/Екат.smc.bak`
- `Тест/Пермь площадка.smc`
- `Тест/Пермь площадка.smc.bak`
- `Тест/перм.smc`
- `Тест/тест 1.smc`
- `Тест/тест 1.smc.bak`
- `Тест/тест 10.smc`
- `Тест/тест 10.smc.bak`
- `Тест/тест 2.smc`
- `Тест/тест 3.smc`
- `Тест/тест 4.smc`
- `Тест/ушалы 2.smc`
- `Тест/ушалы.smc`

## Deleted files

- none

## New / untracked files

- `.opencode/`
- `.playwright-mcp/`
- `AGENTS.md`
- `Target`
- `console.log(item))`
- `docs/architecture-migration/evidence/phase-2-climate-state/`
- `docs/architecture-migration/plans/phase-2-climate-state.md`
- `docs/architecture-migration/правка архитектуры.jpg`
- `docs/architecture-migration/правка архитектуры.txt`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx`

## Other status records

- none

## Baseline diff --name-only records

- `.gitignore`
- `docs/architecture-migration/TASK_CONTEXT.md`
- `docs/architecture-migration/maps/target-invariants.md`
- `installer/SnowMeltingCalculator.iss`
- `publish/SnowMeltingCalculator.deps.json`
- `publish/SnowMeltingCalculator.pdb`
- `src/SnowMeltingCalculator.csproj`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx`
- `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx`

## Baseline vs post status comparison

Comparison excludes only `docs/architecture-migration/evidence/phase-2-climate-state/`, because that directory is the Task 1 evidence directory.

- Removed records: 0
- Added records: 0
- Status-changed records: 0

### Removed

- none

### Added

- none

### Changed

- none

## Full baseline status table

| Status | Path |
|---|---|
| ` M` | `.gitignore` |
| ` M` | `.omo/evidence/fix-design-temperature-source/f1-plan-compliance-corrected.txt` |
| ` M` | `.omo/evidence/fix-design-temperature-source/f1-plan-compliance.txt` |
| ` M` | `.omo/evidence/fix-design-temperature-source/f2-code-quality.txt` |
| ` M` | `.omo/evidence/fix-design-temperature-source/f3-manual-qa.txt` |
| ` M` | `.omo/evidence/fix-design-temperature-source/f4-scope-fidelity.txt` |
| ` M` | `.omo/evidence/fix-design-temperature-source/ui-tree-discovery.txt` |
| ` M` | `.omo/evidence/fix-thermal-to-hydraulics-sync/construction-dump.txt` |
| ` M` | `.omo/evidence/fix-thermal-to-hydraulics-sync/f2-code-quality.txt` |
| ` M` | `.omo/evidence/fix-thermal-to-hydraulics-sync/f4-scope-fidelity.txt` |
| ` M` | `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-manual-qa.txt` |
| ` M` | `.omo/evidence/fix-thermal-to-hydraulics-sync/thermal-dump.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/f2/_build_src.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/f2/_build_tests.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/f2/_format_raw.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/f2/_src_files.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/f2/_test_files.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/f4/f4-scope-fidelity.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/task-1a/baseline_refactor_dedupe.json` |
| ` M` | `.omo/evidence/refactor-dedupe-params/task-1a/git-diff-stat.txt` |
| ` M` | `.omo/evidence/refactor-dedupe-params/task-3/task-3-refactor-dedupe-params.txt` |
| ` M` | `.omo/notepads/fix-delta-t-input/learnings.md` |
| ` M` | `.omo/notepads/fix-glycol-concentration-constants/learnings.md` |
| ` M` | `.omo/notepads/refactor-dedupe-params/learnings.md` |
| ` M` | `.omo/notepads/refactor-hydraulics-tests/learnings.md` |
| ` M` | `.omo/notepads/unify-validation/learnings.md` |
| ` M` | `.omo/run-continuation/ses_0a358c786ffeXzxbyBqAXxW7oK.json` |
| ` M` | `.omo/start-work/ledger.jsonl` |
| `??` | `.opencode/` |
| `??` | `.playwright-mcp/` |
| `??` | `AGENTS.md` |
| ` M` | `PROJECT_STATUS.md` |
| ` M` | `README.md` |
| `??` | `Target` |
| ` M` | `build_temp/SnowMeltingCalculator.deps.json` |
| ` M` | `build_temp/SnowMeltingCalculator.runtimeconfig.json` |
| `??` | `console.log(item))` |
| ` M` | `docs/architecture-migration/TASK_CONTEXT.md` |
| `??` | `docs/architecture-migration/evidence/phase-2-climate-state/` |
| ` M` | `docs/architecture-migration/maps/target-invariants.md` |
| `??` | `docs/architecture-migration/plans/phase-2-climate-state.md` |
| `??` | `docs/architecture-migration/правка архитектуры.jpg` |
| `??` | `docs/architecture-migration/правка архитектуры.txt` |
| ` M` | `docs/formulas/traceability-matrix.md` |
| ` M` | `docs/workspace/rehau_assets/brand.css` |
| ` M` | `docs/workspace/rehau_assets/element_squares_large.svg` |
| ` M` | `docs/workspace/rehau_assets/element_squares_small.svg` |
| ` M` | `docs/workspace/rehau_assets/logo_black.svg` |
| ` M` | `docs/workspace/rehau_assets/logo_icon_color.svg` |
| ` M` | `docs/workspace/rehau_assets/logo_main_color.svg` |
| ` M` | `docs/workspace/rehau_assets/logo_white.svg` |
| ` M` | `docs/workspace/rehau_assets/logo_wordmark_black.svg` |
| ` M` | `docs/workspace/rehau_assets/palette.json` |
| ` M` | `docs/workspace/rehau_assets/pattern_black_bg.svg` |
| ` M` | `docs/workspace/rehau_assets/pattern_green_bg.svg` |
| ` M` | `docs/workspace/rehau_assets/pattern_white_bg.svg` |
| ` M` | `docs/workspace/rehau_assets/slogan_icon_black.svg` |
| ` M` | `docs/workspace/rehau_assets/slogan_text_black.svg` |
| ` M` | `docs/workspace/rehau_assets/slogan_with_icon_black.svg` |
| ` M` | `docs/Планируемые_изменения.md` |
| ` M` | `docs/Руководство_пользователя.md` |
| ` M` | `docs/инструкция/README v.2.2 kimi.html` |
| ` M` | `docs/инструкция/README v.2.3 kimi.html` |
| ` M` | `docs/инструкция/README.md` |
| ` M` | `docs/инструкция/images/brand/element_squares_large.svg` |
| ` M` | `docs/инструкция/images/brand/logo_black.svg` |
| ` M` | `docs/инструкция/images/brand/logo_main_color.svg` |
| ` M` | `docs/инструкция/images/brand/logo_white.svg` |
| ` M` | `docs/инструкция/images/brand/slogan_text_black.svg` |
| ` M` | `installer/SnowMeltingCalculator.iss` |
| ` M` | `publish/LatoFont/OFL.txt` |
| ` M` | `publish/SnowMeltingCalculator.deps.json` |
| ` M` | `publish/SnowMeltingCalculator.pdb` |
| ` M` | `publish/SnowMeltingCalculator.runtimeconfig.json` |
| ` M` | `src/App.xaml.cs` |
| ` M` | `src/Assets/Brand/logo_icon_color.svg` |
| ` M` | `src/Behaviors/DataGridBehavior.cs` |
| ` M` | `src/Behaviors/TextBoxBehavior.cs` |
| ` M` | `src/Controls/Climate/CityAutoCompleteBox.xaml.cs` |
| ` M` | `src/Controls/RecalcIndicator.xaml.cs` |
| ` M` | `src/Converters/CityMatchToHighlightConverter.cs` |
| ` M` | `src/Converters/Converters.cs` |
| ` M` | `src/Models/Construction/Construction.cs` |
| ` M` | `src/Models/Construction/ConstructionTemplate.cs` |
| ` M` | `src/Models/Construction/Layer.cs` |
| ` M` | `src/Models/Construction/Material.cs` |
| ` M` | `src/Models/Construction/MaterialSnapshot.cs` |
| ` M` | `src/Models/Enums/RecalcState.cs` |
| ` M` | `src/Models/Hydraulics/CircuitRow.cs` |
| ` M` | `src/Models/Hydraulics/Collector.cs` |
| ` M` | `src/Models/Hydraulics/CollectorSummary.cs` |
| ` M` | `src/Models/Hydraulics/CollectorType.cs` |
| ` M` | `src/Models/Hydraulics/FlowRegime.cs` |
| ` M` | `src/Models/Hydraulics/GlycolDataModels.cs` |
| ` M` | `src/Models/Hydraulics/GlycolProperties.cs` |
| ` M` | `src/Models/Hydraulics/GlycolType.cs` |
| ` M` | `src/Models/Hydraulics/HydraulicMode.cs` |
| ` M` | `src/Models/Hydraulics/ValveType.cs` |
| ` M` | `src/Models/Navigation/ModuleStateChangedEventArgs.cs` |
| ` M` | `src/Models/Project/ProjectData.cs` |
| ` M` | `src/Models/Thermal/IConstructionData.cs` |
| ` M` | `src/Models/Thermal/OperatingMode.cs` |
| ` M` | `src/Models/Thermal/PipeType.cs` |
| ` M` | `src/Models/Thermal/ThermalCalculationResult.cs` |
| ` M` | `src/Repositories/ClimateDataRepository.cs` |
| ` M` | `src/Repositories/Construction/ConstructionRepository.cs` |
| ` M` | `src/Repositories/Construction/ConstructionTemplateRepository.cs` |
| ` M` | `src/Repositories/Construction/IConstructionTemplateRepository.cs` |
| ` M` | `src/Repositories/Construction/IMaterialRepository.cs` |
| ` M` | `src/Repositories/Construction/MaterialRepository.cs` |
| ` M` | `src/Repositories/Hydraulics/CollectorRepository.cs` |
| ` M` | `src/Repositories/Hydraulics/ICollectorRepository.cs` |
| ` M` | `src/Resources/Dictionary.xaml` |
| ` M` | `src/Services/Climate/ClimateDataService.cs` |
| ` M` | `src/Services/Climate/ISearchHistoryService.cs` |
| ` M` | `src/Services/Climate/SearchHistoryService.cs` |
| ` M` | `src/Services/Construction/ConstructionService.cs` |
| ` M` | `src/Services/Construction/ConstructionTemplateValidator.cs` |
| ` M` | `src/Services/Construction/ConstructionValidator.cs` |
| ` M` | `src/Services/Construction/IConstructionService.cs` |
| ` M` | `src/Services/Construction/MaterialCrudValidator.cs` |
| ` M` | `src/Services/Construction/MaterialNotFoundException.cs` |
| ` M` | `src/Services/Hydraulics/CircuitsCalculator.cs` |
| ` M` | `src/Services/Hydraulics/FlowRegimeCalculator.cs` |
| ` M` | `src/Services/Hydraulics/GlycolDataService.cs` |
| ` M` | `src/Services/Hydraulics/ICircuitsCalculator.cs` |
| ` M` | `src/Services/Hydraulics/IGlycolDataService.cs` |
| ` M` | `src/Services/Navigation/EditorDialogService.cs` |
| ` M` | `src/Services/Navigation/IEditorDialogService.cs` |
| ` M` | `src/Services/Project/ProjectFileService.cs` |
| ` M` | `src/Services/Results/PdfExportService.cs` |
| ` M` | `src/Services/Thermal/ThermalCalculator.cs` |
| ` M` | `src/Services/Visualization/ConstructionVisualizationRenderer.cs` |
| ` M` | `src/SnowMeltingCalculator.csproj` |
| ` M` | `src/ViewModels/Climate/ClimateViewModel.cs` |
| ` M` | `src/ViewModels/Construction/ConstructionViewModel.cs` |
| ` M` | `src/ViewModels/Construction/MaterialEditorViewModel.cs` |
| ` M` | `src/ViewModels/Construction/TemplateEditorViewModel.cs` |
| ` M` | `src/ViewModels/Hydraulics/CollectorViewModel.cs` |
| ` M` | `src/ViewModels/Shared/RecalcIndicatorViewModel.cs` |
| ` M` | `src/ViewModels/Thermal/ThermalViewModel.cs` |
| ` M` | `src/Views/Construction/ConstructionView.xaml` |
| ` M` | `src/Views/Construction/MaterialEditorView.xaml` |
| ` M` | `src/Views/Construction/MaterialEditorView.xaml.cs` |
| ` M` | `src/Views/Construction/TemplateEditorView.xaml` |
| ` M` | `src/Views/Construction/TemplateEditorView.xaml.cs` |
| ` M` | `src/Views/Results/ResultsView.xaml` |
| ` M` | `src/Views/Shared/ConstructionVisualizationView.xaml.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/AttachedProperties/InlinesPropertyTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Climate/ClimateDataServiceTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionRepositoryTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTemplateImportTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateRepositoryTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateValidatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionValidatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/ConstructionViewModelTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/MaterialCrudValidatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/MaterialEditorViewModelTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryCrudTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryMigrationVerification.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/MaterialSnapshotTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Construction/TemplateEditorViewModelTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Converters/CityMatchToHighlightConverterTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Converters/PressureColorConverterTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Converters/SidebarTooltipConverterTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Core/ValidationExtensionsTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Integration/HydraulicsIntegrationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/GlycolAutoRecalculationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitRowTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CollectorSummaryTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CollectorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/EnumsTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/GlycolPropertiesTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/ValveTypeTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Repositories/Climate/SearchHistoryRepositoryTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/CollectorRepositoryJsonLoadingTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/CollectorRepositoryTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/ICollectorRepositoryTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/AppSettingsTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Climate/SearchHistoryServiceTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/FlowRegimeCalculatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceJsonLoadingTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolInterpolationTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/IGlycolDataServiceTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Services/Navigation/EditorDialogServiceTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Thermal/ThermalCalculatorTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/Views/ConstructionVisualizationRendererTests.cs` |
| ` M` | `tests/SnowMeltingCalculator.Tests/baseline_refactor_dedupe.json` |
| ` M` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx` |
| `??` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx` |
| ` M` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx` |
| ` M` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx` |
| ` M` | `Тест/1.smc` |
| ` M` | `Тест/_20260724.smc` |
| ` M` | `Тест/Детальный_отчёт_рабочий_9-100000_20260727.md` |
| ` M` | `Тест/Екат 1.smc` |
| ` M` | `Тест/Екат 1.smc.bak` |
| ` M` | `Тест/Екат для версии 1.1.smc` |
| ` M` | `Тест/Екат.smc` |
| ` M` | `Тест/Екат.smc.bak` |
| ` M` | `Тест/Пермь площадка.smc` |
| ` M` | `Тест/Пермь площадка.smc.bak` |
| ` M` | `Тест/перм.smc` |
| ` M` | `Тест/тест 1.smc` |
| ` M` | `Тест/тест 1.smc.bak` |
| ` M` | `Тест/тест 10.smc` |
| ` M` | `Тест/тест 10.smc.bak` |
| ` M` | `Тест/тест 2.smc` |
| ` M` | `Тест/тест 3.smc` |
| ` M` | `Тест/тест 4.smc` |
| ` M` | `Тест/ушалы 2.smc` |
| ` M` | `Тест/ушалы.smc` |
