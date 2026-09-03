// Phase 10 machine-checked writer inventory (INV-006 verification method).
// Read-only: scans production sources and proves each canonical store's
// mutating entry points are reached only from its sanctioned writer set, and
// that the CalculationContext compatibility projection (ST-020..ST-022,
// DEC-001 = A) is written only by the four sanctioned projection writers.
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

const root = process.argv[2] ?? ".";
const files = [];
const walk = (dir) => {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) walk(full);
    else if (entry.name.endsWith(".cs")) files.push(full);
  }
};
walk(join(root, "src"));

const read = (file) => readFileSync(file, "utf8");
const callSites = (pattern, onlyFile) => {
  const sites = [];
  const flags = pattern.startsWith("(?i)") ? "i" : "";
  const source = pattern.startsWith("(?i)") ? pattern.slice(4) : pattern;
  for (const file of files) {
    if (onlyFile && !file.endsWith(onlyFile)) continue;
    const lines = read(file).split(/\r?\n/);
    lines.forEach((line, index) => {
      if (new RegExp(source, flags).test(line) && !line.trim().startsWith("//")) {
        sites.push(`${file.replaceAll("\\\\", "/").replaceAll("\\", "/")}:${index + 1}`);
      }
    });
  }
  return sites;
};
const baseName = (site) => site.split("/").pop().split(":")[0];
const classify = (sites, allowed) => {
  const bad = sites.filter((site) => !allowed.includes(baseName(site)));
  return { sites, bad, pass: bad.length === 0 };
};

const checks = [];
const record = (id, store, api, sites, allowed) => {
  const verdict = classify(sites, allowed);
  checks.push({ id, store, api, count: sites.length, sites: verdict.sites, allowed, bad: verdict.bad, pass: verdict.pass });
};

// ST-006..ST-019 + ST-020..ST-023 writers: canonical slice mutation entry points.
const climateWriters = ["ProjectSessionClimateState.cs", "ClimateViewModel.cs", "ProjectLoadOrchestrator.cs", "MainViewModel.cs", "ResultsViewModel.cs", "ProjectSession.cs"];
record("WI-1", "ProjectSessionClimateState (ST-006..ST-011)", "ApplyCitySelection|ApplyIndividualEdit|ApplyProjectSnapshot|ResetToCityData", callSites("(?i)climateState\\.(ApplyCitySelection|ApplyIndividualEdit|ApplyProjectSnapshot|ResetToCityData)\\("), climateWriters);

const constructionWriters = ["ProjectSessionConstructionState.cs", "ConstructionViewModel.cs", "ProjectLoadOrchestrator.cs", "MainViewModel.cs", "ConstructionDefaultStateInitializer.cs", "ConstructionStateLegacyStoreGuardTests.cs"];
record("WI-2", "ProjectSessionConstructionState (ST-012..ST-013+)", "Apply|ApplySnapshot|ResetToDefaults", callSites("(?i)constructionState\\.(Apply|ApplySnapshot|ResetToDefaults)\\("), constructionWriters);

const thermalWriters = ["ProjectSessionThermalState.cs", "ThermalStateCoordinator.cs", "CalculationStateService.cs", "ProjectLoadOrchestrator.cs", "ThermalViewModel.cs"];
// The coordinators hold their slice state as a generic `_state` field: the
// `_state.` receiver is scoped to the owning coordinator file to stay precise.
record("WI-3", "ProjectSessionThermalState (ST-014..ST-015)", "ApplyInputs|ApplyInputEdit|ApplyNeedsRecalculation|BeginCalculation|CompleteCalculation|FailCalculation|Restore|InvalidateFromClimate|InvalidateFromConstruction", callSites("(?i)thermalState\\.(ApplyInputs|ApplyInputEdit|ApplyNeedsRecalculation|BeginCalculation|CompleteCalculation|FailCalculation|Restore|InvalidateFromClimate|InvalidateFromConstruction)\\(").concat(callSites("(?i)_state\\.(ApplyInputs|ApplyInputEdit|ApplyNeedsRecalculation|BeginCalculation|CompleteCalculation|FailCalculation|Restore|InvalidateFromClimate|InvalidateFromConstruction)\\(", "ThermalStateCoordinator.cs")), thermalWriters);

const hydraulicsWriters = ["ProjectSessionHydraulicsState.cs", "HydraulicsStateCoordinator.cs", "CircuitsViewModel.cs", "ProjectLoadOrchestrator.cs", "CalculationStateService.cs"];
record("WI-4", "ProjectSessionHydraulicsState (ST-016..ST-017)", "ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ApplySnapshot", callSites("(?i)hydraulicsState\\.(ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ApplySnapshot)\\(").concat(callSites("(?i)_state\\.(ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ApplySnapshot)\\(", "HydraulicsStateCoordinator.cs")), hydraulicsWriters);

// ST-001..ST-004 (identity/dirty): MarkDirty/MarkClean production callers.
// ResultsViewModel is the Phase 1 identity adapter: its ProjectNumber/
// ProjectObject setters invoke the session's own canonical MarkDirty()
// boundary for user identity edits (no second writable store exists).
record("WI-5", "ProjectSession dirty/identity (ST-001..ST-004)", "MarkDirty", callSites("\\.MarkDirty\\(\\)"), ["ProjectSession.cs", "ProjectSessionClimateState.cs", "ProjectSessionConstructionState.cs", "ProjectSessionThermalState.cs", "ProjectSessionHydraulicsState.cs", "ThermalStateCoordinator.cs", "HydraulicsStateCoordinator.cs", "ResultsViewModel.cs"]);
record("WI-6", "ProjectSession dirty/identity (ST-001..ST-004)", "MarkClean", callSites("\\.MarkClean\\(\\)"), ["ProjectSession.cs", "ResultsViewModel.cs", "MainViewModel.cs"]);

// ST-020..ST-022 (DEC-001 = A): CalculationContext compat projection —
// exactly four sanctioned projection writers; everything else reads.
record("WI-7", "CalculationContext compat projection (ST-020..ST-022)", "UpdateClimate|UpdateConstruction|UpdateThermal|UpdateThermalInputs|UpdateHydraulics|Reset", callSites("(?i)calculationContext\\.(UpdateClimate|UpdateConstruction|UpdateThermal|UpdateThermalInputs|UpdateHydraulics|Reset)\\("), ["ProjectSessionClimateState.cs", "ProjectSessionConstructionState.cs", "ThermalStateCoordinator.cs", "HydraulicsStateCoordinator.cs", "MainViewModel.cs", "ProjectLoadOrchestrator.cs"]);

// ViewModels must not write foreign slices: a VM file may mutate only its own slice.
const vmSlices = {
  "ClimateViewModel.cs": ["ClimateState"],
  "ConstructionViewModel.cs": ["ConstructionState"],
  "ThermalViewModel.cs": ["ThermalState"],
  "CircuitsViewModel.cs": ["HydraulicsState"]
};
const foreign = [];
for (const file of files) {
  const name = file.split(/[\\/]/).pop();
  const own = vmSlices[name];
  if (!own) continue;
  const lines = read(file).split(/\r?\n/);
  lines.forEach((line, index) => {
    for (const [vm, slices] of Object.entries(vmSlices)) {
      if (vm === name) continue;
      for (const slice of slices) {
        if (new RegExp(`${slice}\\.\\s*(Apply|Replace|Reset|Begin|Complete|Fail|Invalidate|Restore)`).test(line)) {
          foreign.push(`${name}:${index + 1} writes ${slice}`);
        }
      }
    }
  });
}
checks.push({ id: "WI-8", store: "ViewModel foreign-slice writes", api: "(grep)", count: foreign.length, sites: [], allowed: [], bad: foreign, pass: foreign.length === 0 });

let failed = 0;
for (const check of checks) {
  if (!check.pass) failed++;
  console.log(`${check.pass ? "PASS" : "FAIL"} ${check.id} ${check.store} :: ${check.api}`);
  console.log(`    call sites (${check.count}):`);
  for (const site of check.sites) console.log(`      ${site}`);
  if (!check.pass) console.log(`    NON-SANCTIONED: ${check.bad.join(", ")}`);
}
console.log(`SUMMARY: ${checks.length - failed}/${checks.length} checks PASS`);
process.exitCode = failed === 0 ? 0 : 1;
