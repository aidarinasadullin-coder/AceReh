# Task 1 — Protected Baseline: Execution-Time Repository, Tools and Dirty Boundary

Plan SHA-256: `B81E82DEFC2DC2D2108F9240BDED6575FD1244DFCBC164AB2602829249CC5FB2`

> This receipt captures the execution-time repository, toolchain and dirty
> boundary BEFORE any Phase 3 construction-state edit. Every later Phase 3 task
> compares its pre-task status against this baseline to prove it touched only
> its own allow-list. Recovery for this receipt is delete/recreate via patch;
> never Git rollback on the shared dirty tree.

## 1. Identity

| Field | Value |
| --- | --- |
| Git root | `D:/IA/ace v.2` |
| HEAD | `e655735dfa66c00cf9c53be93d511eda8989e8bf` |
| Branch | `master` |
| Symbolic ref | `refs/heads/master` |
| Upstream | `origin/master` |
| Ahead/behind (porcelain branch line) | `ahead 33` |
| Remote | `origin https://github.com/aidarinasadullin-coder/AceReh.git` |
| Capture UTC | `2026-08-13T12:47:31Z` |

## 2. Toolchain (`dotnet --info` summary)

| Field | Value |
| --- | --- |
| SDK | `8.0.418` (commit `5854a779c1`) |
| Workload version | `8.0.400-manifests.e5a1450a` |
| MSBuild | `17.11.48+02bf66295` |
| OS | Windows `10.0.19045`, `win-x64` |
| Host | `.NET 8.0.24` x64 (commit `b3b35ce80e`) |
| Base path | `C:\Program Files\dotnet\sdk\8.0.418\` |
| Installed workloads | none |
| `global.json` | not found |

Raw command: `dotnet --info` (full output verified at capture time; exit `0`).

## 3. Dirty Boundary — NUL-safe capture

Command used (byte-preserving, no text-pipeline decoding):

```powershell
cmd /c "git status --porcelain=v1 -z --branch > %TEMP%\opencode\baseline-status.bin 2>&1"
```

Raw capture byte length: **12543** bytes. Parser logic (NUL-split, not
newline-split):

```powershell
$bytes = [System.IO.File]::ReadAllBytes("$env:TEMP\opencode\baseline-status.bin")
# split $bytes on 0x00; decode each chunk as UTF-8
# chunk[0]  = branch line
# chunks 1.. = porcelain v1 entries
```

### 3.1 Branch line

```text
## master...origin/master [ahead 33]
```

### 3.2 Porcelain entry summary

| Set | Count |
| --- | --- |
| Branch line entries | 1 |
| Staged entries (`^[MADRC]`) | **0** |
| Worktree-modified entries (` M`) | 209 |
| Untracked entries (`??`) | 9 (2 collapsed dirs + 7 files) |
| **Total NUL chunks** | 219 |

Staged set is empty: verified both by the porcelain prefix parse and by
`git diff --cached --name-only` (empty output, exit `0`).

### 3.3 Untracked directory expansion

Porcelain `-z` collapses `.opencode/` and `.playwright-mcp/` to directory
entries; git itself expands them under `--exclude-standard`. Non-ignored
untracked **files**:

```powershell
cmd /c "git ls-files --others --exclude-standard -z > %TEMP%\opencode\baseline-others.bin 2>&1"
```

Count: **23 files**. (Ignore provenance: `.gitignore` re-includes
`.opencode/commands/architecture-*.md`; `.playwright-mcp/` files are not
excluded.)

### 3.4 Dirty file inventory (232 = 209 modified + 23 untracked)

Status prefix: `M` = worktree-modified, `U` = untracked (non-ignored). Hash =
`git hash-object <path>` (working-tree blob).

```text
    M	.gitignore	eeda4862b13021a5358c9c78c4af59f17a4808c8
    M	.omo/evidence/fix-design-temperature-source/f1-plan-compliance-corrected.txt	5fadb6df538be1d734536149331ad52e88b725ec
    M	.omo/evidence/fix-design-temperature-source/f1-plan-compliance.txt	e781e26721ce8f8ff7bec6e730e6a0ee4bff7eb0
    M	.omo/evidence/fix-design-temperature-source/f2-code-quality.txt	09fd6e9fae55493e27e89605b0fc767a0c3a1223
    M	.omo/evidence/fix-design-temperature-source/f3-manual-qa.txt	5e0816287d738664fd88b0a95c4e41429e572fd3
    M	.omo/evidence/fix-design-temperature-source/f4-scope-fidelity.txt	2b61b2eb7658d01ce3cb620d43449f512293f1d9
    M	.omo/evidence/fix-design-temperature-source/ui-tree-discovery.txt	f81e452245d0609cd3a37bd8190b6d0e555d70a2
    M	.omo/evidence/fix-thermal-to-hydraulics-sync/construction-dump.txt	e4267a8d3cb2e184453d35a992f398fa9bd34e90
    M	.omo/evidence/fix-thermal-to-hydraulics-sync/f2-code-quality.txt	c2c80631c12a7509d189c4cfa6e8ffee88fb5cca
    M	.omo/evidence/fix-thermal-to-hydraulics-sync/f4-scope-fidelity.txt	081b171dd7e244b1357c6e68e24264c2d7c8289b
    M	.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-manual-qa.txt	248416b82012b27c0811458a493021184fb2e1c2
    M	.omo/evidence/fix-thermal-to-hydraulics-sync/thermal-dump.txt	71e8d99523f0d889cc68c8786b0e54457b3dce32
    M	.omo/evidence/refactor-dedupe-params/f2/_build_src.txt	42c70e717a62cba925bbb43d43186d05d57208ad
    M	.omo/evidence/refactor-dedupe-params/f2/_build_tests.txt	5fdf4695bf34b1e7a5fff59ca4a29d3da1c829ef
    M	.omo/evidence/refactor-dedupe-params/f2/_format_raw.txt	0f7f26e36033e1cab4821f3e1cd11c76eb2f069c
    M	.omo/evidence/refactor-dedupe-params/f2/_src_files.txt	8ccf562bcfbbbec408cf1dac84716ac7cccf1546
    M	.omo/evidence/refactor-dedupe-params/f2/_test_files.txt	39147beb787d294045e17a84fb7e9bf9ce7e690f
    M	.omo/evidence/refactor-dedupe-params/f4/f4-scope-fidelity.txt	cd86b6147011ea9ce27ec32c740a1cb2d9e77b68
    M	.omo/evidence/refactor-dedupe-params/task-1a/baseline_refactor_dedupe.json	50b64a460d38ef1e347376a1fe6f8b1a07c0a763
    M	.omo/evidence/refactor-dedupe-params/task-1a/git-diff-stat.txt	0444af0966baa7c358a48de4151e5e76018c43ff
    M	.omo/evidence/refactor-dedupe-params/task-3/task-3-refactor-dedupe-params.txt	e14ad02f01ba5c011ff3cc16c4fa22ed246b82dd
    M	.omo/notepads/fix-delta-t-input/learnings.md	9c4d27217d57720f15bbf54c2ff350790191527a
    M	.omo/notepads/fix-glycol-concentration-constants/learnings.md	2d61daf772c1df84826d4064efe9299b6f62456d
    M	.omo/notepads/refactor-dedupe-params/learnings.md	f530749b0349cbf591993e8e35188eb1b1f6048c
    M	.omo/notepads/refactor-hydraulics-tests/learnings.md	5bfa5b5e459b69c99eabdc99ddeefa5db4906fb1
    M	.omo/notepads/unify-validation/learnings.md	0636ea7b18e2b564f1178180571e47f423395161
    M	.omo/run-continuation/ses_0a358c786ffeXzxbyBqAXxW7oK.json	06e2af9e530bc566d37e117514721f67f7eca35e
    M	.omo/start-work/ledger.jsonl	b9e57048e4ad285deec7adeaac1dbebe6c3dbd06
    M	PROJECT_STATUS.md	13aff6accc24db94602c86470fa594d1add092de
    M	README.md	658327a587783ef299ffd857a9b6e7d2d750f9eb
    M	build_temp/SnowMeltingCalculator.deps.json	a99d35761ac47f1b624b6b34b75810d4eb34758b
    M	build_temp/SnowMeltingCalculator.runtimeconfig.json	c7a41177c9eb348d3b4338c2f114524f47c4f778
    M	docs/architecture-migration/TASK_CONTEXT.md	1a490652947b5218349e54a82ea0e3fcae82367f
    M	docs/formulas/traceability-matrix.md	e5da5aa2b47df7b4c44bc64dd3b5e3740b540f7d
    M	docs/workspace/rehau_assets/brand.css	c8a10de231dee4b9524f5609a7a27a530351d608
    M	docs/workspace/rehau_assets/element_squares_large.svg	13a7ef3b801f80cb3aa1ab4e672a2a423ce766b7
    M	docs/workspace/rehau_assets/element_squares_small.svg	035025b7c6615e97aee4039cc99a77d78ce135ea
    M	docs/workspace/rehau_assets/logo_black.svg	03c7654ccffa61ce1ed5e02b263c350013c3f429
    M	docs/workspace/rehau_assets/logo_icon_color.svg	059bd6b165d618a5f4334f60eac0ecbd996d29f6
    M	docs/workspace/rehau_assets/logo_main_color.svg	94596c0f7ed56e0150ec71013bfe612544854bdb
    M	docs/workspace/rehau_assets/logo_white.svg	380052a16b719a1898de7c616d0652868c87e704
    M	docs/workspace/rehau_assets/logo_wordmark_black.svg	1623649bd96ccbcd2ec029bbedd790886d678541
    M	docs/workspace/rehau_assets/palette.json	6626f88a52481818ccf8f5633a2c54dc91960006
    M	docs/workspace/rehau_assets/pattern_black_bg.svg	e8c2537c80d11ad9db057f865321c8f5dc8cbab4
    M	docs/workspace/rehau_assets/pattern_green_bg.svg	1f1985672c05081f29449004ebaa120ee00422d1
    M	docs/workspace/rehau_assets/pattern_white_bg.svg	72a17c634d734c3cec5d4b920d6c347f0defcb87
    M	docs/workspace/rehau_assets/slogan_icon_black.svg	df266516b9cf2acaf6cf16f7bd5f2083a5468747
    M	docs/workspace/rehau_assets/slogan_text_black.svg	b977a7154d6c09741b46c9b4e8166c3a4451ca79
    M	docs/workspace/rehau_assets/slogan_with_icon_black.svg	65e52f143d28e8388823965d755393503e39ad3e
    M	docs/Планируемые_изменения.md	8276443222ce3a4a69a1958df8cf9f5eb06ace90
    M	docs/Руководство_пользователя.md	62b2acf3182d7a920ac8641708ba15e6ffca5e23
    M	docs/инструкция/README v.2.2 kimi.html	5f765227b0d691be2a3d0f69f673dfb18558a864
    M	docs/инструкция/README v.2.3 kimi.html	f78dbe995645270092b35054e4a25df3672eebaf
    M	docs/инструкция/README.md	9e2b8672d9e6a508e32eaa60029a204cd8cf1dc5
    M	docs/инструкция/images/brand/element_squares_large.svg	13a7ef3b801f80cb3aa1ab4e672a2a423ce766b7
    M	docs/инструкция/images/brand/logo_black.svg	03c7654ccffa61ce1ed5e02b263c350013c3f429
    M	docs/инструкция/images/brand/logo_main_color.svg	94596c0f7ed56e0150ec71013bfe612544854bdb
    M	docs/инструкция/images/brand/logo_white.svg	380052a16b719a1898de7c616d0652868c87e704
    M	docs/инструкция/images/brand/slogan_text_black.svg	b977a7154d6c09741b46c9b4e8166c3a4451ca79
    M	installer/SnowMeltingCalculator.iss	c75b7bd1e408dc88de8186539e4f688b6be3eaad
    M	publish/LatoFont/OFL.txt	98383e3d835a5c5aea988a74b569193a30dcbcec
    M	publish/SnowMeltingCalculator.deps.json	da58d65f5824558310f82c3bc5996bb3251e66e8
    M	publish/SnowMeltingCalculator.pdb	b28234b0694c978c9ec0ad235c39fd964e815c92
    M	publish/SnowMeltingCalculator.runtimeconfig.json	e342774c895a7427c57b624a9ec65e0594c7fe99
    M	src/App.xaml.cs	60c531534b110220d831cbbaa0d27eb467b46c0a
    M	src/Assets/Brand/logo_icon_color.svg	059bd6b165d618a5f4334f60eac0ecbd996d29f6
    M	src/Behaviors/DataGridBehavior.cs	4ffb3323b883c5acfe2d529868a09a2e2748a693
    M	src/Behaviors/TextBoxBehavior.cs	473d14ade2326b43097c751408c90d06e4c48bbf
    M	src/Controls/Climate/CityAutoCompleteBox.xaml.cs	244a1ad067dfb088ea154d3bdda8eed6a1e42e42
    M	src/Controls/RecalcIndicator.xaml.cs	17df806d0eb584f641b1cfaf4110f6f79a0eb4d3
    M	src/Converters/CityMatchToHighlightConverter.cs	7b61c9860e57d35b11369805ebed090d463824e4
    M	src/Converters/Converters.cs	28ac29851435e7730ab6ed063d415b41c936dc4b
    M	src/Models/Construction/Construction.cs	857e72392653e8ca50cb30b39e789a107086fefc
    M	src/Models/Construction/ConstructionTemplate.cs	5cf3f3c41e50c25275ef8b0c77c37607ec47f91c
    M	src/Models/Construction/Layer.cs	a3a62299ec0b26c2865b4a3274c4d5cda9dabe89
    M	src/Models/Construction/Material.cs	3bfdce672ea94f117a5091471ab31b6d4e4ccebd
    M	src/Models/Construction/MaterialSnapshot.cs	a0dee4ae8760bb8b66a67a82e85104d8a5620092
    M	src/Models/Enums/RecalcState.cs	e1d8c05869df44825cbd060cace499ca4298df82
    M	src/Models/Hydraulics/CircuitRow.cs	fa0bc27fd21f6598ed7ffe39f6c87f643da3f235
    M	src/Models/Hydraulics/Collector.cs	38b7159488a153a0cc002b3bca0664eabe62609c
    M	src/Models/Hydraulics/CollectorSummary.cs	bae97f9dca1f7c74f6ab11aa5be9a2897b749129
    M	src/Models/Hydraulics/CollectorType.cs	c76da77dc1942570720ccb3ae62e134739157e76
    M	src/Models/Hydraulics/FlowRegime.cs	2cea95825c0c7fdc70459e16ef6f22fb526ed5bb
    M	src/Models/Hydraulics/GlycolDataModels.cs	3bcddcf13598ec70ed94ce956a435edc7bde1a26
    M	src/Models/Hydraulics/GlycolProperties.cs	06085cbee3d684aff78cb361185346a671e7cd2a
    M	src/Models/Hydraulics/GlycolType.cs	edc51f171590babdf33426b1457e2f393a69635c
    M	src/Models/Hydraulics/HydraulicMode.cs	459fa8c6a18ea12b22a0c7517352618a54b96f7b
    M	src/Models/Hydraulics/ValveType.cs	23e629eddd1dce387973ac475cac372cf5964dff
    M	src/Models/Navigation/ModuleStateChangedEventArgs.cs	44587f7c01ac0cb9f3aaf6745cdda66810088867
    M	src/Models/Project/ProjectData.cs	d70ac761ebdc60e7879b40954a6af069d3db4ced
    M	src/Models/Thermal/IConstructionData.cs	5107768e2b96760cba427e69cfa92721657111f0
    M	src/Models/Thermal/OperatingMode.cs	1214470f4ef49e860fb059da680845cb02b51207
    M	src/Models/Thermal/PipeType.cs	0edfcb13c05f622eaee2a00a7633f84e3fb4433e
    M	src/Models/Thermal/ThermalCalculationResult.cs	5a53da9f18a82bdc60c7a132de06d9034dc21480
    M	src/Repositories/ClimateDataRepository.cs	c9f75ff5ad31bc13e928bffe35a74988b9b2fbf6
    M	src/Repositories/Construction/ConstructionRepository.cs	4512bdbad5f33ed82c0f9c88c0f5fefd6d8740cc
    M	src/Repositories/Construction/ConstructionTemplateRepository.cs	f174537f35515a0a46574eeb5042015f2dde9f3e
    M	src/Repositories/Construction/IConstructionTemplateRepository.cs	4a7879db88658658faf5f0d5f08b57b2e05cf796
    M	src/Repositories/Construction/IMaterialRepository.cs	0d58d24fbcf9bd3e301891f1f6fc5e6aaefcd9d1
    M	src/Repositories/Construction/MaterialRepository.cs	66f75719725bb10b40426cc3c7b0abee282b7ce3
    M	src/Repositories/Hydraulics/CollectorRepository.cs	3b2056140467a8e6eb4e9d57ac750cfbd89a01eb
    M	src/Repositories/Hydraulics/ICollectorRepository.cs	4066cd642853078eb09b1ab446e2bf6d1964587a
    M	src/Resources/Dictionary.xaml	f0e667c430d720c94b14a1c08c7fc7b7dd956a7c
    M	src/Services/Climate/ClimateDataService.cs	01cad0db5974a08cc695f2b9c060366a550cc0b1
    M	src/Services/Climate/ISearchHistoryService.cs	e32804f0d3e11701bc4a7f7734e0da3aa78ec69f
    M	src/Services/Climate/SearchHistoryService.cs	c602b822f9f9764a5aa0381be5b8fe4c75ac96bd
    M	src/Services/Construction/ConstructionService.cs	13a95ff5d72617c044dbc323d683e211069151ee
    M	src/Services/Construction/ConstructionTemplateValidator.cs	f07f9e8050d80ee8ef1434a2aa433987c6c18c06
    M	src/Services/Construction/ConstructionValidator.cs	e13d8c60534c198dae4090883aac1489522153e5
    M	src/Services/Construction/IConstructionService.cs	7a8a19adb28d48761a364579b5ab384afe66edac
    M	src/Services/Construction/MaterialCrudValidator.cs	09493925134cb0def4d795cb098fdc0a62cd80ee
    M	src/Services/Construction/MaterialNotFoundException.cs	848cf54f103d2fbde6d182533f2c80813e4e0ec2
    M	src/Services/Hydraulics/CircuitsCalculator.cs	6c09bf4574176991a480bd9def79d5470c2bfcd9
    M	src/Services/Hydraulics/FlowRegimeCalculator.cs	03fc527f2bb1d7f711decfed2f01a41ea32cd76e
    M	src/Services/Hydraulics/GlycolDataService.cs	8d552eac1cf8290467faa2bd39a31e8ab1114409
    M	src/Services/Hydraulics/ICircuitsCalculator.cs	cb2dd192be87d41729477ca38da721e86b1609fb
    M	src/Services/Hydraulics/IGlycolDataService.cs	6ad9f28578abf41761c38e3a02a41305ad4bfc79
    M	src/Services/Navigation/EditorDialogService.cs	a94317e4ea6f39f1cfd4a27220a349045cd694e3
    M	src/Services/Navigation/IEditorDialogService.cs	3bbdbc8cd6e049d69ce4eb46e2698178fae12f6e
    M	src/Services/Project/ProjectFileService.cs	27c06574e4782cd5bd5c947c0d21a419f621b3c5
    M	src/Services/Results/PdfExportService.cs	baa055ad07a90e00a8ab581f855b8ac5b419044e
    M	src/Services/Thermal/ThermalCalculator.cs	bfec410b2e55a14109bb6aab26d15b1658f1a150
    M	src/Services/Visualization/ConstructionVisualizationRenderer.cs	60c7cbc4316651c1d3d0dedd2e241b890014ac9f
    M	src/SnowMeltingCalculator.csproj	e2f21dc7d0ad5ead112c12db5e7e311120d4ef51
    M	src/ViewModels/Construction/ConstructionViewModel.cs	e8f7f88d46888984770fc19598dc0d45b9c175de
    M	src/ViewModels/Construction/MaterialEditorViewModel.cs	7df4bb8e0ae72e6cb682610e3da0bebdf4572b48
    M	src/ViewModels/Construction/TemplateEditorViewModel.cs	aea3dd2238bb40f57f26d24518027b249f488679
    M	src/ViewModels/Hydraulics/CollectorViewModel.cs	c3271f53144a1ca6b3feb024a94a938107b535d8
    M	src/ViewModels/Shared/RecalcIndicatorViewModel.cs	ae781dc35b7bdbe0b1251f789f5592a1e5c40701
    M	src/ViewModels/Thermal/ThermalViewModel.cs	e262a2edebf4f5787efc74c7c20ced2cd2d63e7e
    M	src/Views/Construction/ConstructionView.xaml	d8ba2eac3bafd2eed61d366ad7656fe63a64d33b
    M	src/Views/Construction/MaterialEditorView.xaml	049ef2dc91f8ddb8aba205c57acff08fa8c8317f
    M	src/Views/Construction/MaterialEditorView.xaml.cs	57c65271cea6ea3d0a9c578211b18a45462775d4
    M	src/Views/Construction/TemplateEditorView.xaml	825a34700808ee27da959743a7a9c9e423ea425b
    M	src/Views/Construction/TemplateEditorView.xaml.cs	a703598877f87a42284a57e83044de07c9f074f3
    M	src/Views/Results/ResultsView.xaml	fea752ae21a9d2f1b9f376ff08abf379f82ee53e
    M	src/Views/Shared/ConstructionVisualizationView.xaml.cs	3c573ecaccfd66c5809a01a7d11405663bff36e2
    M	tests/SnowMeltingCalculator.Tests/AttachedProperties/InlinesPropertyTests.cs	2620d93b5ca7c2534ca87fd8d903db0077b3ad27
    M	tests/SnowMeltingCalculator.Tests/Climate/ClimateDataServiceTests.cs	35ba49ecbc9ac9dd55101747ace60891a567bd01
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionRepositoryTests.cs	1a2d8c5a646f0e414136a771aec5aaf717b98e07
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTemplateImportTests.cs	b066d70eedce53806dfc53c70c7234abd00a56e7
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs	ef961f86f85cf921a4a94352a40c44ce39981ebc
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateRepositoryTests.cs	929b145db25a29102fd7d9c54c71e1bd8136f22c
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateValidatorTests.cs	c4147e558370800977bba5c53d67b8100c202331
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionValidatorTests.cs	cedc78c715ee30edc8770e05ef9281a0d520c889
    M	tests/SnowMeltingCalculator.Tests/Construction/ConstructionViewModelTests.cs	06d6647bd156a843fdea6d458a5ffcc054d52a77
    M	tests/SnowMeltingCalculator.Tests/Construction/MaterialCrudValidatorTests.cs	a601a954fc714ee892309569d88615b503849d08
    M	tests/SnowMeltingCalculator.Tests/Construction/MaterialEditorViewModelTests.cs	284df57c5a273281e964f3bdf86a2ec442573301
    M	tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryCrudTests.cs	0e708e7a6f0850b96eee04f229bfd918f2849e2b
    M	tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryMigrationVerification.cs	9141fe3823241e603f16f6af115fb845634c9dc4
    M	tests/SnowMeltingCalculator.Tests/Construction/MaterialSnapshotTests.cs	b4e7b8fa59f6da809e9d05424db632b740a547c7
    M	tests/SnowMeltingCalculator.Tests/Construction/TemplateEditorViewModelTests.cs	c19af3c845b7b9bbf7fbef08d027cc735de05532
    M	tests/SnowMeltingCalculator.Tests/Converters/CityMatchToHighlightConverterTests.cs	eb2824a23a5594673cf386a34fecd9b7147fd362
    M	tests/SnowMeltingCalculator.Tests/Converters/PressureColorConverterTests.cs	20b2407a8fcabba26f5fe5e7f4fa6a1d786255e9
    M	tests/SnowMeltingCalculator.Tests/Converters/SidebarTooltipConverterTests.cs	7990c13d1221efc35c2871988ad14e51781019a2
    M	tests/SnowMeltingCalculator.Tests/Core/ValidationExtensionsTests.cs	36714453baa34c7b7112e8197651012b984709c7
    M	tests/SnowMeltingCalculator.Tests/Integration/HydraulicsIntegrationTests.cs	1578f7ad39a0bd7ae99588b541044a0ca4654377
    M	tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs	e1b18e76a28ec60071ebe17ecf3a1a35725ebec2
    M	tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/GlycolAutoRecalculationTests.cs	b2b1024298d6b2e0d2cf17f461d5bd15746c0e02
    M	tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs	13fb06b0a8f7a2babbde4ff0474cc7d082e716a6
    M	tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs	0f79f495496b12719a651eda5be5d28496117121
    M	tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitRowTests.cs	d52e276d339b778c86315ae1864b49b4705d57ab
    M	tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CollectorSummaryTests.cs	778b19bec0f66c8ddd3ff295cef3fdd730ca75fc
    M	tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CollectorTests.cs	0917da5d5d8b27a995706ee7054dc665f4215d37
    M	tests/SnowMeltingCalculator.Tests/Models/Hydraulics/EnumsTests.cs	02d1739bc94ab692d4bee221e26eda8658f908a2
    M	tests/SnowMeltingCalculator.Tests/Models/Hydraulics/GlycolPropertiesTests.cs	84f16ec24172776e4a49afc29561f5cb47080556
    M	tests/SnowMeltingCalculator.Tests/Models/Hydraulics/ValveTypeTests.cs	a0c5bead41476e40a56f49ec099c1981b5c6da8a
    M	tests/SnowMeltingCalculator.Tests/Repositories/Climate/SearchHistoryRepositoryTests.cs	d69a21226b91cf9fd0dc16ea2a855dc408aa521c
    M	tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/CollectorRepositoryJsonLoadingTests.cs	80a4b87455b5cfdbc41bece14eb332f6d84750b9
    M	tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/CollectorRepositoryTests.cs	7da7f5773bf58b4ee1a5cc7f172eee5c7c0f9b79
    M	tests/SnowMeltingCalculator.Tests/Repositories/Hydraulics/ICollectorRepositoryTests.cs	4f7e68851547e8bcfe31d86ee3b4a0253ec79edc
    M	tests/SnowMeltingCalculator.Tests/Services/AppSettingsTests.cs	51e1bd5763988512b25b5f580cbd6ac7ad2b9ff9
    M	tests/SnowMeltingCalculator.Tests/Services/Climate/SearchHistoryServiceTests.cs	ddfdc44dd25e9abfb96dfa6ab45d9f474388b962
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs	1a54c1c721504c0af52a69a70dd955c9ec24471d
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/FlowRegimeCalculatorTests.cs	00cfd682cc879a8febec3008e3af27997e9d92d8
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceJsonLoadingTests.cs	2782d7c9aad0faf5478f5aa85ebe42a0f56f52a5
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolDataServiceTests.cs	98d3ad2934ea7e991d3b343fc960c760bb6a26a3
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolInterpolationTests.cs	06502c38355bd1debc5fd9568a349eef9e4a7722
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/IGlycolDataServiceTests.cs	86b70db6b33a2abf03b526a3ad90167d7dc7c53c
    M	tests/SnowMeltingCalculator.Tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs	907750005e3b45c4a0534f899fc20cfce33456a9
    M	tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs	ad6d6342826330524454823b5e10aae4f03371ab
    M	tests/SnowMeltingCalculator.Tests/Services/Navigation/EditorDialogServiceTests.cs	bcf26d28fa33e0ed890aa12a03866996c0969d92
    M	tests/SnowMeltingCalculator.Tests/Thermal/ThermalCalculatorTests.cs	7d7ed10384c97775e451fc685712bbacaedafb49
    M	tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs	7a3c7244bbf6230c8b3768d65d9795d2fb32adad
    M	tests/SnowMeltingCalculator.Tests/Views/ConstructionVisualizationRendererTests.cs	3b7c07aaf4209591d0361e815cedf85a24eb2bc5
    M	tests/SnowMeltingCalculator.Tests/baseline_refactor_dedupe.json	ee58a720c72a4c34ed11d6ef7c0e8e677ad96c40
    M	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx	ef1cff6c45ea1d99d1703d75ded38494ba0b35bb
    M	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx	244246a23a874b670852d730e8c7026c508a5a3e
    M	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx	6a83f2acd83937090543c7001d98dcd5f025c5d2
    M	Тест/1.smc	cd8d1a720be732d7942a8c89b23994a16007c462
    M	Тест/_20260724.smc	d38c772cb04ce74014290e6e4986ac797871842b
    M	Тест/Детальный_отчёт_рабочий_9-100000_20260727.md	7065dc1e7e54592fcf3808b2a0d49af47de346b7
    M	Тест/Екат 1.smc	d14a2f8c1f1b10a30c3de45ae887438c691dd850
    M	Тест/Екат 1.smc.bak	e196be1cc99eada507e53c514fadd474d5e83c4b
    M	Тест/Екат для версии 1.1.smc	00adef84f13f56fe0184c5d25ed7a3321917516a
    M	Тест/Екат.smc	713ca004e31702d96c79fd6a1fa9b52f08083000
    M	Тест/Екат.smc.bak	418e2e56c9686104f67fd59ae704c2f6e83ba7b6
    M	Тест/Пермь площадка.smc	095860c6815b3d906b060b4e39952b827e2e8cad
    M	Тест/Пермь площадка.smc.bak	757ea2e0b471a98b73103a0dc65b5c9c7ee99541
    M	Тест/перм.smc	17785728089fce0cb87d5ba88f0fa558fd2d535f
    M	Тест/тест 1.smc	85dd11042fc7d8f2be792a4065d7aa132c82c555
    M	Тест/тест 1.smc.bak	3e2f1b7c6004e0e97d2b8841879d66b8cae1f148
    M	Тест/тест 10.smc	73eee3429908df9f2ae9434f04d2bacb32b537b4
    M	Тест/тест 10.smc.bak	69ab32d0805269f810951d6c931f49080659e38e
    M	Тест/тест 2.smc	a5a28da629f31a98b6640c126bed799397f2933d
    M	Тест/тест 3.smc	461af961191945ebbe4c16a0b54b5508a5925d76
    M	Тест/тест 4.smc	8fc8fbf111d1350ac64e8587ca4a5cea215c7108
    M	Тест/ушалы 2.smc	4c8fd5d9eb6a01da0597a339b2a428e17bf4ef06
    M	Тест/ушалы.smc	65cf89503de1aa5e930671b4a6d322be3c199e71
    U	.opencode/commands/architecture-approve.md	264b6c9ecd36927ca3a4903e5789ffdd9abe08b5
    U	.opencode/commands/architecture-draft.md	5af52a821d82d565b30a114e8cf26f85b478f4f8
    U	.opencode/commands/architecture-plan.md	b096c967db395c8cdf4874af8dd5143d1068c715
    U	.opencode/commands/architecture-resume.md	c8992989e5f722fa159a35e394ca4fdefc0d2df4
    U	.opencode/commands/architecture-start.md	c7215caa428932369dab2941c2197e941c263aa1
    U	.playwright-mcp/page-2026-08-03T07-36-00-520Z.yml	7641d000116327adafbe43476f01bae104f1da1d
    U	.playwright-mcp/page-2026-08-03T08-32-40-903Z.yml	7641d000116327adafbe43476f01bae104f1da1d
    U	.playwright-mcp/page-2026-08-03T09-25-17-911Z.yml	7641d000116327adafbe43476f01bae104f1da1d
    U	.playwright-mcp/page-2026-08-03T09-40-15-607Z.yml	7641d000116327adafbe43476f01bae104f1da1d
    U	.playwright-mcp/page-2026-08-03T10-00-14-912Z.yml	7641d000116327adafbe43476f01bae104f1da1d
    U	.playwright-mcp/task2-legend-1280.png	01dbbb332ca7263dfc083c02e0e968551aabdb0f
    U	.playwright-mcp/task2-legend-375.png	0256dad37ba8bed7beb4795b979501bfcdcf9573
    U	.playwright-mcp/task2-legend-768.png	08c67df36777c3edf4f65f32556266b875926e9f
    U	.playwright-mcp/task2-legend-final-1280.png	bc9f1fc1073cfbb6e70c9e8a3386fe746f130841
    U	.playwright-mcp/task2-legend-final-375.png	0256dad37ba8bed7beb4795b979501bfcdcf9573
    U	.playwright-mcp/task2-legend-final-768.png	08c67df36777c3edf4f65f32556266b875926e9f
    U	AGENTS.md	33a96ed8e44c02c1cabc91ede1ebc80a3b2b9a52
    U	Target	b27c1a3ff401b866c1b50b4f065167d19a7799e8
    U	console.log(item))	e69de29bb2d1d6434b8b29ae775ad8c2e48c5391
    U	docs/architecture-migration/plans/phase-3-construction-state.md	96d2176893a470c3933de7094b39eb2a01a4d73a
    U	docs/architecture-migration/правка архитектуры.jpg	fd1ecda6101d5ffa2e91e8a2aef455ca58d1519d
    U	docs/architecture-migration/правка архитектуры.txt	02cee8b433015dc1ec53e2434e5fb67746a3002f
    U	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx	70b5f44bfbf14c05521f12c32b410c6e28cd32ef
```

Count check: 209 `M` rows + 23 `U` rows = **232**.

## 4. Construction-relevant dirty paths

Explicit prefix matching only (no guessing):

```text
src/Models/Construction/*
src/ViewModels/Construction/*
src/Services/Project/*
tests/**/Construction/*
tests/**/Project*          (none dirty)
tests/**/lifecycle filters: Integration/HydraulicsIntegration*, IntegrationTests/Hydraulics/* (recalculation / synchronization / integration)
```

28 Construction-relevant dirty paths, all worktree-modified:

| Path | Hash |
| --- | --- |
| `src/Models/Construction/Construction.cs` | `857e72392653e8ca50cb30b39e789a107086fefc` |
| `src/Models/Construction/ConstructionTemplate.cs` | `5cf3f3c41e50c25275ef8b0c77c37607ec47f91c` |
| `src/Models/Construction/Layer.cs` | `a3a62299ec0b26c2865b4a3274c4d5cda9dabe89` |
| `src/Models/Construction/Material.cs` | `3bfdce672ea94f117a5091471ab31b6d4e4ccebd` |
| `src/Models/Construction/MaterialSnapshot.cs` | `a0dee4ae8760bb8b66a67a82e85104d8a5620092` |
| `src/Services/Project/ProjectFileService.cs` | `27c06574e4782cd5bd5c947c0d21a419f621b3c5` |
| `src/ViewModels/Construction/ConstructionViewModel.cs` | `e8f7f88d46888984770fc19598dc0d45b9c175de` |
| `src/ViewModels/Construction/MaterialEditorViewModel.cs` | `7df4bb8e0ae72e6cb682610e3da0bebdf4572b48` |
| `src/ViewModels/Construction/TemplateEditorViewModel.cs` | `aea3dd2238bb40f57f26d24518027b249f488679` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionRepositoryTests.cs` | `1a2d8c5a646f0e414136a771aec5aaf717b98e07` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTemplateImportTests.cs` | `b066d70eedce53806dfc53c70c7234abd00a56e7` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs` | `ef961f86f85cf921a4a94352a40c44ce39981ebc` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateRepositoryTests.cs` | `929b145db25a29102fd7d9c54c71e1bd8136f22c` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionTemplateValidatorTests.cs` | `c4147e558370800977bba5c53d67b8100c202331` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionValidatorTests.cs` | `cedc78c715ee30edc8770e05ef9281a0d520c889` |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionViewModelTests.cs` | `06d6647bd156a843fdea6d458a5ffcc054d52a77` |
| `tests/SnowMeltingCalculator.Tests/Construction/MaterialCrudValidatorTests.cs` | `a601a954fc714ee892309569d88615b503849d08` |
| `tests/SnowMeltingCalculator.Tests/Construction/MaterialEditorViewModelTests.cs` | `284df57c5a273281e964f3bdf86a2ec442573301` |
| `tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryCrudTests.cs` | `0e708e7a6f0850b96eee04f229bfd918f2849e2b` |
| `tests/SnowMeltingCalculator.Tests/Construction/MaterialRepositoryMigrationVerification.cs` | `9141fe3823241e603f16f6af115fb845634c9dc4` |
| `tests/SnowMeltingCalculator.Tests/Construction/MaterialSnapshotTests.cs` | `b4e7b8fa59f6da809e9d05424db632b740a547c7` |
| `tests/SnowMeltingCalculator.Tests/Construction/TemplateEditorViewModelTests.cs` | `c19af3c845b7b9bbf7fbef08d027cc735de05532` |
| `tests/SnowMeltingCalculator.Tests/Integration/HydraulicsIntegrationTests.cs` | `1578f7ad39a0bd7ae99588b541044a0ca4654377` |
| `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs` | `e1b18e76a28ec60071ebe17ecf3a1a35725ebec2` |
| `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/GlycolAutoRecalculationTests.cs` | `b2b1024298d6b2e0d2cf17f461d5bd15746c0e02` |
| `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs` | `13fb06b0a8f7a2babbde4ff0474cc7d082e716a6` |
| `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs` | `0f79f495496b12719a651eda5be5d28496117121` |
| `tests/SnowMeltingCalculator.Tests/Views/ConstructionVisualizationRendererTests.cs` | `3b7c07aaf4209591d0361e815cedf85a24eb2bc5` |

## 5. Protected set

Protected = all existing repository files EXCEPT paths this receipt is allowed
to create, i.e. everything under
`docs/architecture-migration/evidence/phase-3-construction-state/`. That is the
only write allow-list for Task 1. The full protected inventory is the 232 dirty
files above plus all clean tracked files; the dirty boundary that Task 2+ must
preserve byte-for-byte is section 3.4.

## 6. Post-capture symmetry check (immediate)

Re-run of the identical NUL-safe status capture immediately after this
receipt's directory was created, compared against the pre-capture set:

| Metric | Pre | Post | Delta |
| --- | --- | --- | --- |
| Raw capture bytes | 12543 | 12543 + this receipt's entry | +1 file (this receipt) |
| Staged entries | 0 | 0 | none |
| Worktree-modified entries | 209 | 209 | none |
| Untracked `??` entries | 9 | 10 (`docs/architecture-migration/evidence/phase-3-construction-state/` added) | +1 Task 1 evidence |

Symmetric after excluding only Task 1 evidence. No staged change, no HEAD
move, no protected-path drift.

## 7. Reproducibility

All raw receipts are reproducible:

1. `git rev-parse --show-toplevel` → `D:/IA/ace v.2`
2. `git rev-parse HEAD` → `e655735dfa66c00cf9c53be93d511eda8989e8bf`
3. `git symbolic-ref HEAD` → `refs/heads/master`
4. `git rev-parse --abbrev-ref "@{u}"` → `origin/master`
5. `dotnet --info` (exit 0)
6. `cmd /c "git status --porcelain=v1 -z --branch > <file>.bin"` then NUL-split
   the bytes and decode each chunk as UTF-8
7. `cmd /c "git ls-files --others --exclude-standard -z > <file>.bin"` for the
   untracked expansion
8. `git --literal-pathspecs hash-object -- "<path>"` per dirty file

Parser constraint honored throughout: the status stream was captured to a
binary file and split on `0x00` byte boundaries; it was never piped through a
PowerShell text pipeline (which would corrupt the NUL-delimited stream).

## 8. Guardrails

- No `git add`, `commit`, `reset`, `clean`, `checkout`, `push` or `stash` was
  run.
- No file outside the Task 1 allow-list was created or modified.
- Recovery for this receipt: delete/recreate via patch only; never Git
  rollback on the shared dirty tree.
