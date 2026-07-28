// screenshot.mjs — Manual QA: screenshot HTML for visual verification.
import { writeFileSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const docsDir = resolve(here, "..");
const htmlPath = join(docsDir, "README v.2.2.html");
const shotPath = join(here, "qa-screenshot.png");

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";

async function main() {
  const puppeteer = await import("puppeteer-core");
  const browser = await puppeteer.default.launch({
    executablePath: CHROME,
    headless: true,
    args: ["--no-sandbox", "--disable-dev-shm-usage", "--lang=ru"],
  });
  try {
    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600, deviceScaleFactor: 1 });
    await page.goto(pathToFileURL(htmlPath).href, { waitUntil: "networkidle0", timeout: 30000 });
    await page.screenshot({ path: shotPath, fullPage: true });
    console.log(`Screenshot: ${shotPath}`);
  } finally {
    await browser.close();
  }
}
main().catch((err) => { console.error(err.message); process.exit(1); });