const pptxgen = require("pptxgenjs");
const p = new pptxgen();
p.layout = "LAYOUT_WIDE"; // 13.33 x 7.5
p.author = "REHAU";
p.title = "Калькулятор снеготаяния — ИИ-конвейер разработки";

const W = 13.33, H = 7.5, M = 0.6;
const F = "Segoe UI", MONO = "Consolas";
const INK = "1D1D1B", MUTED = "6E6E73", RED = "E50040", RED_DARK = "B60034";
const TEAL = "4FC7B5", TEAL_DEEP = "2F776D", PALE = "F5F5F6", WHITE = "FFFFFF";
const ASSET = __dirname + "/";

const sh = () => ({ type: "outer", color: "1D1D1B", blur: 9, offset: 2, angle: 90, opacity: 0.16 });

// ── helpers ──────────────────────────────────────────────
function header(s, kicker, title, lead) {
  s.addText(kicker.toUpperCase(), { x: M, y: 0.34, w: 12.1, h: 0.3, fontFace: F, fontSize: 11, bold: true, color: RED, charSpacing: 3, margin: 0 });
  s.addText(title, { x: M, y: 0.6, w: 12.1, h: 0.66, fontFace: F, fontSize: 30, bold: true, color: INK, margin: 0 });
  if (lead) s.addText(lead, { x: M, y: 1.3, w: 11.4, h: 0.42, fontFace: F, fontSize: 13.5, color: MUTED, margin: 0 });
}
function framed(s, img, x, y, w, h, caption) {
  s.addShape(p.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius: 0.08, fill: { color: WHITE }, line: { color: "E7E7EA", width: 0.75 }, shadow: sh() });
  const pad = 0.1;
  s.addImage({ path: ASSET + img, x: x + pad, y: y + pad, w: w - 2 * pad, h: h - 2 * pad });
  if (caption) s.addText(caption, { x, y: y + h + 0.08, w, h: 0.28, fontFace: F, fontSize: 11, color: MUTED, align: "center", margin: 0 });
}
function row(s, num, head, text, x, y, w, accent) {
  s.addText(num, { x, y, w: 0.85, h: 0.75, fontFace: F, fontSize: 30, bold: true, color: accent, margin: 0 });
  s.addText(head, { x: x + 0.95, y: y + 0.02, w: w - 0.95, h: 0.32, fontFace: F, fontSize: 15.5, bold: true, color: INK, margin: 0 });
  s.addText(text, { x: x + 0.95, y: y + 0.36, w: w - 0.95, h: 0.55, fontFace: F, fontSize: 12, color: MUTED, margin: 0 });
}

// ── S1 · Титул ───────────────────────────────────────────
let s = p.addSlide();
s.background = { path: ASSET + "bg-red.png" };
s.addText("REHAU · КАЛЬКУЛЯТОР СНЕГОТАЯНИЯ", { x: M, y: 1.15, w: 6.4, h: 0.32, fontFace: F, fontSize: 12.5, bold: true, color: WHITE, charSpacing: 4, margin: 0 });
s.addText("Инженерный расчёт, сделанный конвейером «человек + ИИ»", { x: M, y: 1.62, w: 6.5, h: 2.5, fontFace: F, fontSize: 37, bold: true, color: WHITE, margin: 0, lineSpacingMultiple: 1.05 });
s.addText("Один инженер и AI-агент строят промышленное Windows-приложение: от формул и нормативки — до установщика и отчётов.", { x: M, y: 4.35, w: 5.9, h: 1.1, fontFace: F, fontSize: 15.5, color: WHITE, margin: 0, lineSpacingMultiple: 1.15 });
s.addText("Сентябрь 2026 · версия 1.2.0", { x: M, y: 6.75, w: 5, h: 0.3, fontFace: F, fontSize: 11.5, color: WHITE, margin: 0 });
framed(s, "render-results.png", 7.35, 1.5, 5.45, 3.6);
s.addText("Экран «Результаты расчёта» — реальный интерфейс приложения", { x: 7.35, y: 5.25, w: 5.45, h: 0.3, fontFace: F, fontSize: 11, color: WHITE, align: "center", margin: 0 });

// ── S2 · Что делает приложение ───────────────────────────
s = p.addSlide();
header(s, "Продукт", "Что делает приложение", "Полный цикл проектирования систем снеготаяния — от климата города до пояснительной записки.");
row(s, "01", "Расчёт снеготаяния", "Тепловая нагрузка и антиобледенение по климатической зоне города", M, 2.0, 5.6, RED);
row(s, "02", "Гидравлика коллекторов", "Диаметры, скорости, потери давления и балансировка контуров", M, 3.15, 5.6, RED);
row(s, "03", "Подбор оборудования REHAU", "По каталогу производителя, с проверкой ограничений монтажа", M, 4.3, 5.6, RED);
row(s, "04", "Пояснительная записка в PDF", "Полный расчёт с формулами и обоснованием — одним кликом", M, 5.45, 5.6, RED);
framed(s, "render-hydraulics.png", 6.85, 1.95, 5.9, 3.86, "Гидравлический расчёт: контуры, коллекторы, дросселирование");

// ── S3 · Технологии внутри ───────────────────────────────
s = p.addSlide();
header(s, "Стек", "Технологии внутри", "Промышленный стек уровня крупного вендора — и ИИ-инструментарий разработки.");
const tech = [
  ["8", RED, "C# · .NET 8", "Язык и рантайм Microsoft — индустриальный стандарт корпоративных приложений"],
  ["W", TEAL_DEEP, "WPF", "Настольный UI-фреймворк Windows, на нём работает сама Visual Studio"],
  ["N", RED, "NUnit", "2 237 автотестов: каждый расчёт и правило проверяются кодом"],
  ["PDF", TEAL_DEEP, "PDFsharp · MigraDoc", "Генерация отчётов, свободная лицензия MIT — без лицензионных рисков"],
  ["CI", RED, "GitHub · Actions", "Код в облаке, каждая сборка автоматически проверяется тестами"],
  ["AI", TEAL_DEEP, "ZCode · GLM", "AI-агент разработки: план, код, тесты, ревью — под контролем инженера"],
];
tech.forEach((t, i) => {
  const cx = M + (i % 2) * 6.25, cy = 2.0 + Math.floor(i / 2) * 1.72;
  s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: cx, y: cy, w: 5.95, h: 1.5, rectRadius: 0.09, fill: { color: PALE } });
  s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: cx + 0.25, y: cy + 0.37, w: 0.76, h: 0.76, rectRadius: 0.12, fill: { color: t[1] } });
  s.addText(t[0], { x: cx + 0.25, y: cy + 0.37, w: 0.76, h: 0.76, fontFace: F, fontSize: t[0].length > 2 ? 14 : 20, bold: true, color: WHITE, align: "center", valign: "middle", margin: 0 });
  s.addText(t[2], { x: cx + 1.25, y: cy + 0.24, w: 4.55, h: 0.36, fontFace: F, fontSize: 16, bold: true, color: INK, margin: 0 });
  s.addText(t[3], { x: cx + 1.25, y: cy + 0.62, w: 4.55, h: 0.75, fontFace: F, fontSize: 11.5, color: MUTED, margin: 0 });
});

// ── S4 · Конвейер одной фичи ─────────────────────────────
s = p.addSlide();
header(s, "Как это делается", "Конвейер одной фичи", "Ни одно серьёзное изменение не проходит через одного ИИ — каждый шаг имеет свой гейт.");
const steps = [
  ["План", "что именно и зачем меняем — письменно, с решениями"],
  ["Ревью плана", "второй независимый ИИ ищет дефекты до кода"],
  ["Show-me", "владелец видит картинку будущего и утверждает"],
  ["Реализация", "агент пишет код и гоняет 2 237 тестов"],
  ["Ревью кода", "независимая проверка готового диффа"],
  ["Приёмка", "решение принимает человек, не машина"],
];
steps.forEach((st, i) => {
  const cx = M + i * 2.06;
  s.addShape(p.shapes.OVAL, { x: cx + 0.55, y: 2.0, w: 0.66, h: 0.66, fill: { color: i === 5 ? TEAL_DEEP : RED } });
  s.addText(String(i + 1), { x: cx + 0.55, y: 2.0, w: 0.66, h: 0.66, fontFace: F, fontSize: 20, bold: true, color: WHITE, align: "center", valign: "middle", margin: 0 });
  if (i < 5) s.addText("→", { x: cx + 1.42, y: 2.05, w: 0.6, h: 0.6, fontFace: F, fontSize: 20, color: "C9C9CE", align: "center", valign: "middle", margin: 0 });
  s.addText(st[0], { x: cx - 0.12, y: 2.82, w: 2.0, h: 0.3, fontFace: F, fontSize: 13.5, bold: true, color: INK, align: "center", margin: 0 });
  s.addText(st[1], { x: cx - 0.12, y: 3.14, w: 2.0, h: 0.95, fontFace: F, fontSize: 10.5, color: MUTED, align: "center", margin: 0 });
});
s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: M, y: 4.55, w: 12.13, h: 2.2, rectRadius: 0.1, fill: { color: PALE } });
s.addText([
  { text: "Кейс: «Отменить / Вернуть» — откат любых правок в Word-стиле", options: { fontSize: 15.5, bold: true, color: INK, breakLine: true } },
  { text: "Одна фича — полный конвейер: от плана до приёмки за 2 дня.", options: { fontSize: 12, color: MUTED } },
], { x: M + 0.35, y: 4.78, w: 11.4, h: 0.85, fontFace: F, margin: 0 });
[["11", "решений зафиксировано до кода"], ["3", "критических дефекта нашло ревью — до реализации"], ["+22", "новых автотеста"], ["0", "регрессий в 2 237 тестах"]].forEach((k, i) => {
  const kx = M + 0.35 + i * 2.95;
  s.addText(k[0], { x: kx, y: 5.6, w: 2.7, h: 0.62, fontFace: F, fontSize: 34, bold: true, color: i === 3 ? TEAL_DEEP : RED, margin: 0 });
  s.addText(k[1], { x: kx, y: 6.22, w: 2.7, h: 0.5, fontFace: F, fontSize: 10.5, color: MUTED, margin: 0 });
});

// ── S5 · Правила для ИИ ──────────────────────────────────
s = p.addSlide();
header(s, "Харнесс", "Правила для ИИ — письменно и машинно", "ИИ работает по уставу проекта; устав обеспечен не памятью, а кодом.");
row(s, "01", "AGENTS.md — устав в репозитории", "Правила архитектуры и процесса: ИИ читает их в каждой сессии", M, 2.1, 5.9, RED);
row(s, "02", "ADR — журнал решений", "14 записей: почему система устроена так, а не иначе — с причинами", M, 3.3, 5.9, RED);
row(s, "03", "Машинные проверки правил", "Инварианты превращены в тесты: нарушение = падение сборки", M, 4.5, 5.9, RED);
s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: 7.0, y: 1.95, w: 5.75, h: 4.15, rectRadius: 0.1, fill: { color: "141412" }, shadow: sh() });
s.addShape(p.shapes.OVAL, { x: 7.3, y: 2.2, w: 0.12, h: 0.12, fill: { color: "FF5F57" } });
s.addShape(p.shapes.OVAL, { x: 7.5, y: 2.2, w: 0.12, h: 0.12, fill: { color: "FEBC2E" } });
s.addShape(p.shapes.OVAL, { x: 7.7, y: 2.2, w: 0.12, h: 0.12, fill: { color: "28C840" } });
s.addText([
  { text: "$ dotnet test --filter Architecture", options: { color: "9FE8DB", breakLine: true } },
  { text: " ", options: { breakLine: true, fontSize: 6 } },
  { text: "R2_ClimateState_MutatedOnlyBySanctionedWriters", options: { color: "FFFFFF", breakLine: true } },
  { text: "  Failed: несанкционированный писатель", options: { color: "FF6B6B", breakLine: true } },
  { text: "  в канонический слайс ClimateState", options: { color: "FF6B6B", breakLine: true } },
  { text: " ", options: { breakLine: true, fontSize: 6 } },
  { text: "1 failed / 2 236 passed", options: { color: "FF6B6B", bold: true } },
], { x: 7.3, y: 2.5, w: 5.15, h: 3.3, fontFace: MONO, fontSize: 12.5, margin: 0, lineSpacingMultiple: 1.25 });
s.addText("Попытка нарушить устройство системы падает тестом — автоматически, до попадания в продукт", { x: 7.0, y: 6.25, w: 5.75, h: 0.55, fontFace: F, fontSize: 11.5, color: MUTED, margin: 0 });

// ── S6 · ИИ не забывает ──────────────────────────────────
s = p.addSlide();
header(s, "Харнесс", "ИИ не забывает", "Каждая новая сессия агента стартует не с нуля — а с накопленным опытом проекта.");
s.addText("14", { x: M, y: 2.05, w: 2.9, h: 1.3, fontFace: F, fontSize: 72, bold: true, color: RED, margin: 0 });
s.addText("решений в журнале ADR — каждое с причинами и последствиями", { x: M, y: 3.35, w: 2.7, h: 0.85, fontFace: F, fontSize: 12, color: INK, margin: 0 });
s.addText("21", { x: M, y: 4.45, w: 2.9, h: 1.3, fontFace: F, fontSize: 72, bold: true, color: TEAL_DEEP, margin: 0 });
s.addText("урок в журнале находок — ошибку нельзя повторить «по незнанию»", { x: M, y: 5.75, w: 2.7, h: 0.85, fontFace: F, fontSize: 12, color: INK, margin: 0 });
const adrs = [
  ["ADR-009", "PDF-движок переведён на свободную библиотеку (MIT) — снят лицензионный риск для бизнеса"],
  ["ADR-012", "Индикация «рассчитано» стала честной: галочка гаснет при правке данных"],
  ["ADR-014", "«Отменить / Вернуть» через журнал снимков — как в Word, с точкой чистоты"],
];
adrs.forEach((a, i) => {
  const ay = 2.05 + i * 1.62;
  s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: 3.9, y: ay, w: 8.8, h: 1.4, rectRadius: 0.09, fill: { color: PALE } });
  s.addText([
    { text: a[0] + "  ·  2026-09", options: { fontSize: 13, bold: true, color: RED, breakLine: true } },
    { text: a[1], options: { fontSize: 12.5, color: INK } },
  ], { x: 4.25, y: ay + 0.18, w: 8.2, h: 1.05, fontFace: F, margin: 0, lineSpacingMultiple: 1.12 });
});
s.addText("Записи примеров — реальные решения проекта", { x: 3.9, y: 6.95, w: 8.8, h: 0.3, fontFace: F, fontSize: 10.5, color: MUTED, margin: 0 });

// ── S7 · Пульт в кармане ─────────────────────────────────
s = p.addSlide();
header(s, "Мобильность", "Пульт — в кармане", "Конвейер не требует сидеть за компьютером: агент работает на ПК, управление — с телефона.");
s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: 0.85, y: 1.75, w: 2.85, h: 5.5, rectRadius: 0.18, fill: { color: INK }, shadow: sh() });
s.addImage({ path: ASSET + "phone-zcode.jpg", x: 1.02, y: 1.92, w: 2.51, h: 5.16 });
row(s, "01", "Сессия — в реальном времени", "ZCode Remote Control: ход работы агента на ПК виден с телефона", 4.35, 2.0, 8.3, RED);
row(s, "02", "Планы и визуализации — на экране телефона", "Show-me открывается в браузере: утверждай, глядя на картинку", 4.35, 3.2, 8.3, RED);
row(s, "03", "Приёмка — коротким сообщением", "Аппрув фазы из командировки, из машины, из дома", 4.35, 4.4, 8.3, RED);
row(s, "04", "Живые задачи настоящего проекта", "На скрине — задачи этого приложения: расчёты, редизайны, дедлоки", 4.35, 5.6, 8.3, TEAL_DEEP);

// ── S8 · Надёжность в цифрах ─────────────────────────────
s = p.addSlide();
header(s, "Качество", "Надёжность — в цифрах", "Ни одна цифра не «для красоты»: за каждой стоит тест или журнал репозитория.");
[["2 237", "автотестов — каждый расчёт, правило и файл проекта", RED],
 ["11", "фаз перестройки архитектуры — без потери данных пользователей", TEAL_DEEP],
 ["14", "архитектурных решений задокументировано с причинами", RED],
 ["10", "шагов отмены любых правок — как в Word", TEAL_DEEP]].forEach((k, i) => {
  const kx = M + (i % 2) * 6.25, ky = 2.05 + Math.floor(i / 2) * 2.25;
  s.addText(k[0], { x: kx, y: ky, w: 5.9, h: 1.15, fontFace: F, fontSize: 64, bold: true, color: k[2], margin: 0 });
  s.addText(k[1], { x: kx + 0.05, y: ky + 1.2, w: 5.6, h: 0.6, fontFace: F, fontSize: 13, color: INK, margin: 0 });
});
s.addText("Файлы старых версий открываются в новых: совместимость формата закреплена контрактными тестами. Источник: репозиторий проекта, сентябрь 2026.", { x: M, y: 6.7, w: 12.1, h: 0.5, fontFace: F, fontSize: 11.5, color: MUTED, margin: 0 });

// ── S9 · Инфраструктура качества ─────────────────────────
s = p.addSlide();
header(s, "Автоматизация", "Инфраструктура качества — уже работает", "GitHub проверяет каждую сборку в облаке: зелёная галочка — к приёмке, красный крест — обратно агенту.");
const flow = [["Изменение", "агент закончил фазу"], ["GitHub", "код уходит в облако"], ["CI-проверка", "сборка + 2 237 тестов"], ["Приёмка", "владелец смотрит только зелёное"]];
flow.forEach((f2, i) => {
  const fx = M + i * 3.18;
  const good = i === 3;
  s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: fx, y: 2.1, w: 2.75, h: 1.5, rectRadius: 0.1, fill: { color: good ? "E4F6F2" : PALE } });
  s.addText(f2[0], { x: fx + 0.2, y: 2.32, w: 2.35, h: 0.35, fontFace: F, fontSize: 15.5, bold: true, color: good ? TEAL_DEEP : INK, align: "center", margin: 0 });
  s.addText(f2[1], { x: fx + 0.2, y: 2.72, w: 2.35, h: 0.7, fontFace: F, fontSize: 11, color: MUTED, align: "center", margin: 0 });
  if (i < 3) s.addText("→", { x: fx + 2.76, y: 2.55, w: 0.45, h: 0.6, fontFace: F, fontSize: 22, color: "C9C9CE", align: "center", valign: "middle", margin: 0 });
});
s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: M, y: 4.15, w: 5.95, h: 2.3, rectRadius: 0.1, fill: { color: PALE } });
s.addText([
  { text: "Реестр «правило → проверка»", options: { fontSize: 15.5, bold: true, color: INK, breakLine: true } },
  { text: "У каждого правила проекта — адресная проверка: тест, скрипт или шаг ревью. Правило не может протухнуть: верификатор падает, если запись отстала от кода.", options: { fontSize: 12, color: MUTED } },
], { x: M + 0.3, y: 4.4, w: 5.35, h: 1.85, fontFace: F, margin: 0, lineSpacingMultiple: 1.12 });
s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: 6.8, y: 4.15, w: 5.95, h: 2.3, rectRadius: 0.1, fill: { color: PALE } });
s.addText([
  { text: "Ревью-чеки", options: { fontSize: 15.5, bold: true, color: INK, breakLine: true } },
  { text: "У каждого независимого ревью — перечитываемый файл-вердикт: находки по уровню критичности и их судьба. Ревью оставляет след, а не растворяется в переписке.", options: { fontSize: 12, color: MUTED } },
], { x: 7.1, y: 4.4, w: 5.35, h: 1.85, fontFace: F, margin: 0, lineSpacingMultiple: 1.12 });

// ── S10 · Куда расту ─────────────────────────────────────
s = p.addSlide();
header(s, "Развитие", "Куда это растёт", "Следующие шаги того же конвейера — каждая технология уже стандарт индустрии.");
[["01", "Telegram-приёмка", "уведомления и аппрувы фаз прямо в мессенджере — пульт становится ещё ближе"],
 ["02", "База знаний с поиском (RAG)", "брендбук, нормативка, формулы: агент сам находит первоисточник и цитирует его — вместо того чтобы полагаться на память"],
 ["03", "Интеграции (MCP)", "«USB-стандарт» для ИИ: GitHub, сборки, задачи-трекеры подключаются как инструменты агента"],
 ["04", "Автономные сценарии", "сборка релиза и первичный разбор сбоев агент делает сам; метрики качества его работы считаются по ревью-чекам"]].forEach((r2, i) => {
  row(s, r2[0], r2[1], r2[2], M, 2.05 + i * 1.22, 12.0, i % 2 ? TEAL_DEEP : RED);
});

// ── S11 · Как переносится на команду ─────────────────────
s = p.addSlide();
header(s, "Масштабирование", "Как это переносится на команду", "Тот же контур работает у одного инженера и у отдела — меняется только число людей на последнем шаге.");
const tr = [["Правила", "письменно, в репозитории — а не «в головах»"], ["Проверки", "машинные: тесты вместо «поверь на слово»"], ["Сборки", "в облаке: каждая проверена автоматически"], ["Ревью", "ИИ проверяет ИИ — решает человек"]];
tr.forEach((t2, i) => {
  const tx = M + i * 3.18;
  s.addShape(p.shapes.ROUNDED_RECTANGLE, { x: tx, y: 2.2, w: 2.75, h: 2.5, rectRadius: 0.1, fill: { color: PALE } });
  s.addText(String(i + 1), { x: tx + 0.25, y: 2.45, w: 1.0, h: 0.7, fontFace: F, fontSize: 34, bold: true, color: i === 3 ? TEAL_DEEP : RED, margin: 0 });
  s.addText(t2[0], { x: tx + 0.25, y: 3.2, w: 2.25, h: 0.35, fontFace: F, fontSize: 16, bold: true, color: INK, margin: 0 });
  s.addText(t2[1], { x: tx + 0.25, y: 3.58, w: 2.25, h: 0.95, fontFace: F, fontSize: 11.5, color: MUTED, margin: 0 });
  if (i < 3) s.addText("→", { x: tx + 2.76, y: 3.15, w: 0.45, h: 0.6, fontFace: F, fontSize: 22, color: "C9C9CE", align: "center", valign: "middle", margin: 0 });
});
s.addText("Четыре шага внедряются по очереди; первый — обычный текстовый файл, окупается в первую же неделю.", { x: M, y: 5.3, w: 12.1, h: 0.5, fontFace: F, fontSize: 13, color: MUTED, margin: 0 });

// ── S12 · Финал ──────────────────────────────────────────
s = p.addSlide();
s.background = { path: ASSET + "bg-red.png" };
s.addText("ГОТОВ К ПИЛОТУ", { x: M, y: 2.0, w: 12.1, h: 0.35, fontFace: F, fontSize: 13, bold: true, color: WHITE, charSpacing: 5, margin: 0 });
s.addText("Проверим полезность на реальных задачах проектировщиков", { x: M, y: 2.45, w: 11.6, h: 1.9, fontFace: F, fontSize: 36, bold: true, color: WHITE, margin: 0, lineSpacingMultiple: 1.05 });
[["Скорость", "полный расчёт и записка — за минуты, не за дни"],
 ["Корректность", "2 237 тестов и машинные проверки правил"],
 ["Записка", "PDF для экспертизы — одним кликом"]].forEach((t2, i) => {
  const tx = M + i * 4.1;
  s.addText(t2[0], { x: tx, y: 4.7, w: 3.7, h: 0.4, fontFace: F, fontSize: 17, bold: true, color: WHITE, margin: 0 });
  s.addText(t2[1], { x: tx, y: 5.12, w: 3.7, h: 0.7, fontFace: F, fontSize: 12.5, color: WHITE, margin: 0 });
});
s.addText("REHAU · Калькулятор снеготаяния · v1.2.0 · сентябрь 2026", { x: M, y: 6.75, w: 12, h: 0.3, fontFace: F, fontSize: 11.5, color: WHITE, margin: 0 });

p.writeFile({ fileName: ASSET + "harness-deck.pptx" }).then(() => console.log("PPTX done"));
