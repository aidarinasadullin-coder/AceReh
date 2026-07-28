// render.mjs — Markdown to HTML + PDF pipeline via installed Chrome.
// Reads README v.2.2.md, produces README v.2.2.html (artifact) and README v.2.2.pdf.
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { marked } from "marked";
import { gfmHeadingId } from "marked-gfm-heading-id";

const here = dirname(fileURLToPath(import.meta.url));
const docsDir = resolve(here, "..");
const mdPath = join(docsDir, "README v.2.2.md");
const htmlPath = join(docsDir, "README v.2.2.html");
const pdfPath = join(docsDir, "README v.2.2.pdf");
const cssPath = join(here, "style.css");

const CHROME_CANDIDATES = [
  "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
  "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe",
];

function findChrome() {
  for (const p of CHROME_CANDIDATES) {
    if (existsSync(p)) return p;
  }
  throw new Error("Chrome not found. Install Chrome or add its path to CHROME_CANDIDATES.");
}

async function main() {
  const md = readFileSync(mdPath, "utf8");
  marked.use(gfmHeadingId());
  const body = marked.parse(md);
  const css = readFileSync(cssPath, "utf8");

  const html = `<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>README v.2.2 — Екатеринбург, система снеготаяния</title>
<style>
${css}
</style>
</head>
<body>
${body}
</body>
</html>`;

  writeFileSync(htmlPath, html, "utf8");
  console.log(`HTML written: ${htmlPath} (${html.length} bytes)`);

  const chromePath = findChrome();
  console.log(`Chrome: ${chromePath}`);

  const puppeteer = await import("puppeteer-core");
  const browser = await puppeteer.default.launch({
    executablePath: chromePath,
    headless: true,
    args: ["--no-sandbox", "--disable-dev-shm-usage", "--lang=ru"],
  });
  try {
    const page = await browser.newPage();
    await page.setContent(html, { waitUntil: "networkidle0", timeout: 60000 });
    await page.emulateMediaType("print");
    const pdf = await page.pdf({
      path: pdfPath,
      format: "A4",
      printBackground: true,
      preferCSSPageSize: true,
      margin: { top: "18mm", bottom: "20mm", left: "16mm", right: "16mm" },
      displayHeaderFooter: true,
      headerTemplate: "<span></span>",
      footerTemplate: '<div style="font-size:8pt; color:#888; width:100%; text-align:center; padding:0 16mm;">Страница <span class="pageNumber"></span> из <span class="totalPages"></span></div>',
    });
    console.log(`PDF written: ${pdfPath} (${pdf.length} bytes)`);
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error("render failed:", err.message);
  process.exit(1);
});