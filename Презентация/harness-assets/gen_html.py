# -*- coding: utf-8 -*-
"""HTML-версия презентации: self-contained, картинки base64, токены REHAU."""
import base64, os

A = "D:/IA/ace/Презентация/harness-assets/"

def b64(name):
    with open(A + name, "rb") as f:
        return base64.b64encode(f.read()).decode()

def b64raw(name):
    with open(A + name, "rb") as f:
        return base64.b64encode(f.read()).decode()

VID = b64raw("vid/phone-clip.mp4")

IMG = {k: b64(v) for k, v in {
    "results": "render-results.png", "hydraulics": "render-hydraulics.png",
    "conv": "conv-chain.png", "term": "terminal.png", "tape": "adr-tape.png",
    "phone": "phone-composite.png", "ci": "ci-flow.png", "growth": "growth.png",
}.items()}

def row(n, head, text, color="red"):
    c = "#E50040" if color == "red" else "#2F776D"
    return f'<div class="row"><div class="rnum" style="color:{c}">{n}</div><div><div class="rhead">{head}</div><div class="rtext">{text}</div></div></div>'

def pic(key, cap):
    return f'<figure class="pic"><img src="data:image/png;base64,{IMG[key]}" alt="{cap}"><figcaption>{cap}</figcaption></figure>'

html = f"""<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Калькулятор снеготаяния — ИИ-конвейер разработки</title>
<style>
  :root{{--red:#E50040;--red-dark:#B60034;--teal:#4FC7B5;--teal-deep:#2F776D;--ink:#000000;
    --text:#4E4E4E;--bg:#F6F6F7;--card:#FFFFFF;--line:#E4E4E4;}}
  *{{box-sizing:border-box;margin:0;padding:0}}
  body{{font-family:Inter,"Segoe UI",system-ui,-apple-system,sans-serif;background:var(--bg);color:var(--text);line-height:1.55}}
  .wrap{{max-width:1080px;margin:0 auto;padding:0 20px 60px}}

  header{{background:linear-gradient(180deg,#E50040 0%,#000000 100%);color:#fff;padding:46px 20px 42px;margin-bottom:34px}}
  .kicker{{font-size:12px;letter-spacing:.2em;opacity:.9;margin-bottom:12px}}
  h1{{font-size:30px;max-width:820px;line-height:1.22}}
  .sub{{margin-top:14px;font-size:15.5px;opacity:.94;max-width:760px}}
  .badges{{margin-top:18px;display:flex;gap:10px;flex-wrap:wrap}}
  .badge{{background:rgba(255,255,255,.14);border:1px solid rgba(255,255,255,.35);border-radius:999px;padding:4px 14px;font-size:12.5px}}

  h2{{font-size:22px;color:var(--ink);margin:0 0 4px}}
  .tag{{font-size:11px;letter-spacing:.14em;font-weight:700;color:#B60034;background:#FCC2B8;
    display:inline-block;padding:3px 14px 3px 10px;margin-bottom:10px;
    clip-path:polygon(0 0,100% 0,calc(100% - 12px) 100%,0 100%)}}
  .lead{{font-size:14.5px;color:var(--text);margin-bottom:20px;max-width:860px}}
  section{{background:var(--card);border:1px solid var(--line);border-radius:16px;padding:26px 26px 22px;margin-bottom:18px}}
  .cols{{display:grid;grid-template-columns:1fr 1.25fr;gap:22px;align-items:start}}
  .cols.eq{{grid-template-columns:1fr 1fr}}
  .cols.rev{{grid-template-columns:1.25fr 1fr}}

  .row{{display:flex;gap:14px;margin-bottom:16px}}
  .rnum{{font-size:26px;font-weight:700;min-width:44px;line-height:1.1}}
  .rhead{{font-size:15px;font-weight:700;color:var(--ink)}}
  .rtext{{font-size:12.5px;margin-top:2px}}

  .pic{{margin:0}}
  .pic img{{width:100%;border:1px solid var(--line);border-radius:10px;display:block}}
  .pic figcaption{{font-size:11px;color:#818181;text-align:center;margin-top:6px}}
  .phonevid{{display:flex;justify-content:center;background:var(--bg);border-radius:10px;padding:18px 0}}
  .phonevid video{{height:540px;border-radius:26px;border:10px solid #000;display:block}}

  .grid2{{display:grid;grid-template-columns:1fr 1fr;gap:12px}}
  .tcard{{background:var(--bg);border-radius:12px;padding:14px 16px;display:flex;gap:14px;align-items:flex-start}}
  .tchip{{min-width:52px;height:52px;border-radius:12px;color:#fff;font-weight:700;display:flex;align-items:center;justify-content:center;font-size:15px}}
  .tname{{font-size:14.5px;font-weight:700;color:var(--ink)}}
  .tdesc{{font-size:12px;margin-top:2px}}

  .kpis{{display:grid;grid-template-columns:1fr 1fr;gap:14px}}
  .kpi .v{{font-size:52px;font-weight:700;line-height:1.1}}
  .kpi .l{{font-size:12.5px;color:var(--ink);margin-top:4px;max-width:340px}}
  .scard{{background:var(--bg);border-radius:12px;padding:14px 16px}}
  .scard .sv{{font-size:17px;font-weight:700;color:var(--ink)}}
  .scard .sl{{font-size:12px;margin-top:2px}}
  .note{{background:var(--bg);border-radius:12px;padding:14px 18px;font-size:12.5px;margin-top:16px}}
  .note b{{color:var(--ink)}}

  footer{{margin-top:26px;border-top:1px solid var(--line);padding-top:16px;font-size:12px;color:#8A8A90}}
  @media (max-width:800px){{.cols,.cols.eq,.cols.rev{{grid-template-columns:1fr}}.grid2{{grid-template-columns:1fr}}}}
</style>
</head>
<body>

<header>
  <div class="kicker">РЕХАУ · КАЛЬКУЛЯТОР СНЕГОТАЯНИЯ</div>
  <h1>Инженерный расчёт, сделанный конвейером «человек&nbsp;+&nbsp;ИИ»</h1>
  <div class="sub">Один инженер и AI-агент строят промышленное Windows-приложение: от формул и нормативки — до установщика, отчётов и облака проверок.</div>
  <div class="badges"><div class="badge">v1.2.0 · сентябрь 2026</div><div class="badge">2 237 автотестов</div><div class="badge">HTML-версия для телефона</div></div>
</header>

<div class="wrap">

<section>
  <div class="tag">ПРОДУКТ</div>
  <h2>Что делает приложение</h2>
  <div class="lead">Полный цикл проектирования систем снеготаяния — от климата города до пояснительной записки.</div>
  <div class="cols">
    <div>
      {row("01", "Расчёт снеготаяния", "Тепловая нагрузка и антиобледенение по климатической зоне города")}
      {row("02", "Гидравлика коллекторов", "Диаметры, скорости, потери давления и балансировка контуров")}
      {row("03", "Подбор оборудования РЕХАУ", "По каталогу производителя, с проверкой ограничений монтажа")}
      {row("04", "Пояснительная записка в PDF", "Полный расчёт с формулами и обоснованием — одним кликом")}
    </div>
    {pic("hydraulics", "Гидравлический расчёт: контуры, коллекторы, дросселирование")}
  </div>
</section>

<section>
  <div class="tag">СТЕК</div>
  <h2>Технологии внутри</h2>
  <div class="lead">Промышленный стек уровня крупного вендора — и ИИ-инструментарий разработки.</div>
  <div class="grid2">
    <div class="tcard"><div class="tchip" style="background:#E50040">8</div><div><div class="tname">C# · .NET 8</div><div class="tdesc">Язык и рантайм Microsoft — индустриальный стандарт</div></div></div>
    <div class="tcard"><div class="tchip" style="background:#2F776D">W</div><div><div class="tname">WPF</div><div class="tdesc">Настольный UI-фреймворк Windows, на нём Visual Studio</div></div></div>
    <div class="tcard"><div class="tchip" style="background:#E50040">N</div><div><div class="tname">NUnit</div><div class="tdesc">2 237 автотестов: каждый расчёт проверяется кодом</div></div></div>
    <div class="tcard"><div class="tchip" style="background:#2F776D">PDF</div><div><div class="tname">PDFsharp · MigraDoc</div><div class="tdesc">Генерация отчётов, свободная лицензия MIT</div></div></div>
    <div class="tcard"><div class="tchip" style="background:#E50040">CI</div><div><div class="tname">GitHub · Actions</div><div class="tdesc">Код в облаке, каждая сборка проверяется автоматически</div></div></div>
    <div class="tcard"><div class="tchip" style="background:#2F776D">AI</div><div><div class="tname">ZCode · GLM</div><div class="tdesc">AI-агент разработки — под контролем инженера</div></div></div>
  </div>
</section>

<section>
  <div class="tag">КОНВЕЙЕР</div>
  <h2>Конвейер одной фичи</h2>
  <div class="lead">Ни одно серьёзное изменение не проходит через одного ИИ — каждый шаг имеет свой гейт.</div>
  <div class="cols">
    <div>
      {row("01", "План и ревью", "Второй независимый ИИ ищет дефекты плана до написания кода")}
      {row("02", "Show-me", "Владелец утверждает картинку будущего, а не описание словами")}
      {row("03", "Реализация с тестами", "2 237 проверок на каждом шаге")}
      {row("04", "Независимое ревью диффа", "Затем приёмка — решает человек", "teal")}
    </div>
    {pic("conv", "Кейс «Отменить/Вернуть»: 11 решений до кода · 3 дефекта нашло ревью · +22 теста · 0 регрессий")}
  </div>
</section>

<section>
  <div class="tag">ХАРНЕСС</div>
  <h2>Правила для ИИ — письменно и машинно</h2>
  <div class="lead">ИИ работает по уставу проекта; устав обеспечен не памятью, а кодом.</div>
  <div class="cols">
    <div>
      {row("01", "AGENTS.md — устав в репозитории", "Правила архитектуры и процесса: ИИ читает их в каждой сессии")}
      {row("02", "ADR — журнал решений", "14 записей: почему система устроена так, а не иначе")}
      {row("03", "Машинные проверки правил", "Инварианты превращены в тесты: нарушение = падение сборки", "teal")}
    </div>
    {pic("term", "Нарушение правила падает тестом — автоматически")}
  </div>
</section>

<section>
  <div class="tag">ХАРНЕСС</div>
  <h2>ИИ не начинает с нуля</h2>
  <div class="lead">Каждая новая сессия агента стартует с накопленным опытом проекта.</div>
  <div class="cols">
    <div>
      {row("01", "Журнал решений", "14 ADR — почему система устроена так, с причинами и последствиями")}
      {row("02", "Журнал уроков", "21 запись — ошибку нельзя повторить «по незнанию»")}
      {row("03", "Коммиты-досье", "История каждой фичи читается как отчёт")}
      {row("04", "Продолжение, а не повтор", "Новая сессия ИИ читает историю и продолжает работу", "teal")}
    </div>
    {pic("tape", "Реальные записи: ADR-009, ADR-012, ADR-014")}
  </div>
</section>

<section>
  <div class="tag">МОБИЛЬНОСТЬ</div>
  <h2>Пульт — в кармане</h2>
  <div class="lead">Конвейер не требует сидеть за компьютером: агент работает на ПК, управление — с телефона. Живая запись: список задач проекта и чат фичи, где агент коммитит и пушит, пока владелец набирает следующую инструкцию.</div>
  <div class="phonevid"><video src="data:video/mp4;base64,{VID}" autoplay loop muted playsinline></video></div>
  <div style="font-size:11px;color:#818181;text-align:center;margin-top:6px">Живая запись: ZCode Remote Control — задачи проекта (Running/Done) и чат фичи с Changes&nbsp;+1334&nbsp;−2501</div>
</section>

<section>
  <div class="tag">КАЧЕСТВО</div>
  <h2>Надёжность — в цифрах</h2>
  <div class="lead">Ни одна цифра не «для красоты»: за каждой стоит тест или журнал репозитория.</div>
  <div class="kpis">
    <div class="kpi"><div class="v" style="color:#E50040">2 237</div><div class="l">автотестов: код, расчёты, правила</div></div>
    <div class="kpi"><div class="v" style="color:#2F776D">11</div><div class="l">фаз перестройки архитектуры — без потери данных пользователей</div></div>
    <div class="kpi"><div class="v" style="color:#E50040">10</div><div class="l">шагов отмены любых правок — как в Word</div></div>
    <div class="kpi"><div class="v" style="color:#2F776D">0</div><div class="l">регрессий в кейсе «Отменить / Вернуть»</div></div>
  </div>
  <div class="note"><b>Совместимость:</b> файлы старых версий открываются в новых — контракт формата закреплён тестами. Источник цифр — репозиторий проекта, сентябрь 2026.</div>
</section>

<section>
  <div class="tag">АВТОМАТИЗАЦИЯ</div>
  <h2>Инфраструктура качества — уже работает</h2>
  <div class="lead">GitHub проверяет каждую сборку в облаке: зелёная галочка — к приёмке, красный крест — обратно агенту.</div>
  {pic("ci", "Контур: изменение → GitHub → CI → приёмка владельцем")}
</section>

<section>
  <div class="tag">РАЗВИТИЕ</div>
  <h2>Куда это растёт</h2>
  <div class="lead">Следующие шаги того же конвейера — каждая технология уже стандарт индустрии.</div>
  {pic("growth", "Telegram-приёмка · база знаний (RAG) · интеграции (MCP) · автономные сценарии")}
</section>

<section>
  <div class="tag">МАСШТАБИРОВАНИЕ</div>
  <h2>Как это переносится на команду</h2>
  <div class="lead">Тот же контур работает у одного инженера и у отдела — меняется только число людей на последнем шаге.</div>
  <div class="kpis">
    <div class="scard"><div class="sv" style="color:#E50040">01 · Правила</div><div class="sl">письменно, в репозитории — а не «в головах»</div></div>
    <div class="scard"><div class="sv" style="color:#E50040">02 · Проверки</div><div class="sl">машинные: тесты вместо «поверь на слово»</div></div>
    <div class="scard"><div class="sv" style="color:#E50040">03 · Сборки</div><div class="sl">в облаке: каждая проверена автоматически</div></div>
    <div class="scard"><div class="sv" style="color:#2F776D">04 · Ревью</div><div class="sl">ИИ проверяет ИИ — решает человек</div></div>
  </div>
  <div class="note"><b>Четыре шага</b> внедряются по очереди; первый — обычный текстовый файл, окупается в первую же неделю.</div>
</section>

<section style="background:linear-gradient(180deg,#E50040 0%,#000000 100%);border:0;color:#fff">
  <div class="tag" style="color:#fff">ГОТОВ К ПИЛОТУ</div>
  <h2 style="color:#fff">Проверим полезность на реальных задачах проектировщиков</h2>
  <div class="cols eq" style="margin-top:18px">
    <div><b>Скорость</b><br>полный расчёт и записка — за минуты, не за дни</div>
    <div><b>Корректность</b><br>2 237 тестов и машинные проверки правил</div>
  </div>
  <div style="margin-top:14px;font-size:12px;opacity:.9">РЕХАУ · Калькулятор снеготаяния · v1.2.0 · сентябрь 2026</div>
</section>

<footer>
  Собрано на базе корпоративного шаблона РЕХАУ · скрины и схемы — реальные артефакты проекта · сентябрь 2026
</footer>

</div>
</body>
</html>"""

out = A + "harness-deck.html"
with open(out, "w", encoding="utf-8") as f:
    f.write(html)
print("HTML written:", out, round(os.path.getsize(out) / 1024), "KB")
