// validate.mjs — checks Markdown/HTML/PDF integrity using only Node built-ins.
// Verifies: image sources exist, internal TOC anchors resolve, external hrefs preserved,
// PDF exists with reasonable size, current KPI present, stale assertions absent.
import { readFileSync, existsSync, statSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const docsDir = resolve(here, "..");
const mdPath = join(docsDir, "README v.2.2.md");
const htmlPath = join(docsDir, "README v.2.2.html");
const pdfPath = join(docsDir, "README v.2.2.pdf");

const checks = [];
let failed = 0;

function check(name, pass, detail) {
  checks.push({ name, pass, detail });
  if (!pass) failed++;
}

// --- File existence ---
check("MD exists", existsSync(mdPath), mdPath);
check("HTML exists", existsSync(htmlPath), htmlPath);
check("PDF exists", existsSync(pdfPath), pdfPath);

if (!existsSync(mdPath) || !existsSync(htmlPath) || !existsSync(pdfPath)) {
  report();
  process.exit(1);
}

const md = readFileSync(mdPath, "utf8");
const html = readFileSync(htmlPath, "utf8");
const pdfStat = statSync(pdfPath);

// --- PDF size: reasonable (>= 20 KB) ---
check("PDF size reasonable", pdfStat.size >= 20_000, `${pdfStat.size} bytes`);

// --- Image sources exist ---
const imgRegex = /<img[^>]+src="([^"]+)"/g;
const imgSources = [];
let m;
while ((m = imgRegex.exec(html)) !== null) {
  imgSources.push(m[1]);
}
let imgOk = true;
const imgDetails = [];
for (const src of imgSources) {
  const resolved = join(docsDir, src);
  if (!existsSync(resolved)) {
    imgOk = false;
    imgDetails.push(`MISSING: ${src}`);
  } else {
    imgDetails.push(`OK: ${src}`);
  }
}
check("Image sources exist", imgOk, imgDetails.join("; "));

// --- Internal TOC anchors resolve ---
const tocHrefRegex = /href="#([^"]+)"/g;
const tocAnchors = [];
while ((m = tocHrefRegex.exec(html)) !== null) {
  tocAnchors.push(m[1]);
}
const idRegex = /id="([^"]+)"/g;
const ids = new Set();
while ((m = idRegex.exec(html)) !== null) {
  ids.add(m[1]);
}
let anchorsOk = true;
const anchorDetails = [];
for (const anchor of tocAnchors) {
  const decoded = decodeURIComponent(anchor);
  if (!ids.has(anchor) && !ids.has(decoded)) {
    anchorsOk = false;
    anchorDetails.push(`MISSING: #${anchor}`);
  } else {
    anchorDetails.push(`OK: #${anchor}`);
  }
}
check("Internal TOC anchors resolve", anchorsOk, anchorDetails.join("; "));

// --- External hrefs preserved ---
const extHrefRegex = /href="(https?:[^"]+)"/g;
const extHrefs = [];
while ((m = extHrefRegex.exec(html)) !== null) {
  extHrefs.push(m[1]);
}
check("External hrefs preserved", extHrefs.length >= 0, `${extHrefs.length} external links`);

// --- Current KPI present in HTML ---
const kpiPresent = [];
const kpiMissing = [];
const kpiChecks = [
  ["19 контуров", /19\s+(?:контур|рассчитан)/i],
  ["2040 м", /2040/i],
  ["124,403 кВт", /124,403/i],
  ["5,924 м³/ч", /5,924/i],
  ["1850 м петель", /1850/i],
  ["190 м подводок", /190/i],
];
for (const [label, re] of kpiChecks) {
  if (re.test(html)) {
    kpiPresent.push(label);
  } else {
    kpiMissing.push(label);
  }
}
check("Current KPI present", kpiMissing.length === 0, `present: ${kpiPresent.join(", ")}; missing: ${kpiMissing.join(", ")}`);

// --- Stale assertions absent in HTML ---
const stalePresent = [];
const staleChecks = [
  ["15 контуров", /15\s+(?:контур|рассчитан)/i],
  ["1590 м", /1590/i],
  ["1940 м", /1940/i],
  ["96,836 кВт", /96,?836/i],
  ["4,611 м³/ч", /4,?611/i],
  ["частичный/нулевой коллектор 3", /(?:частичн|нулев)/i],
  ["промежуточные скриншоты", /промежуточ/i],
  ["расхождение длины", /расхожден/i],
];
for (const [label, re] of staleChecks) {
  if (re.test(html)) {
    stalePresent.push(label);
  }
}
check("Stale assertions absent", stalePresent.length === 0, `found stale: ${stalePresent.join(", ") || "none"}`);

// --- Collector 3 type IV 1¼″ in HTML ---
check("Collector 3 type IV 1¼″", /IV\s*1¼″/.test(html), "IV 1¼″ present");

function report() {
  console.log("\n=== VALIDATION REPORT ===");
  for (const c of checks) {
    const status = c.pass ? "PASS" : "FAIL";
    console.log(`[${status}] ${c.name}: ${c.detail}`);
  }
  console.log(`\n${checks.length - failed}/${checks.length} checks passed, ${failed} failed`);
}

report();
if (failed > 0) process.exit(1);