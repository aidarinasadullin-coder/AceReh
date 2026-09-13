# -*- coding: utf-8 -*-
"""Генерация схем для слайдов — в токенах REHAU, под зону 1800x981 (ratio 1.834)."""
from PIL import Image, ImageDraw, ImageFont
import os

W, H = 1800, 981
BG = (255, 255, 255)
PALE = (245, 245, 246)
INK = (29, 29, 27)
MUTED = (110, 110, 115)
RED = (229, 0, 64)
RED_DARK = (182, 0, 52)
TEAL = (79, 199, 181)
TEAL_DEEP = (47, 119, 109)
DARK = (20, 20, 18)
LINE = (201, 201, 206)
F = "C:/Windows/Fonts/segoeui.ttf"
FB = "C:/Windows/Fonts/segoeuib.ttf"
FM = "C:/Windows/Fonts/consola.ttf"

def font(path, size):
    return ImageFont.truetype(path, size)

def rr(d, box, r, fill, outline=None, width=2):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)

def arrow(d, x, y, color=LINE):
    d.line([(x, y), (x + 46, y)], fill=color, width=4)
    d.polygon([(x + 46, y - 12), (x + 46, y + 12), (x + 62, y)], fill=color)

def canvas():
    im = Image.new("RGB", (W, H), BG)
    return im, ImageDraw.Draw(im)

def center_text(d, cx, y, text, fnt, color, line_h=None):
    lines = text.split("\n")
    lh = line_h or (lines[0] and int(fnt.size * 1.3) or 30)
    for i, ln in enumerate(lines):
        w = d.textlength(ln, font=fnt)
        d.text((cx - w / 2, y + i * lh), ln, font=fnt, fill=color)

# ── 1. Конвейер: 6 шагов ─────────────────────────────────
def conv_chain():
    im, d = canvas()
    steps = [("План", "что меняем\nи зачем"), ("Ревью плана", "второй ИИ ищет\nдефекты до кода"),
             ("Show-me", "владелец видит\nкартинку будущего"), ("Реализация", "код + 2 237\nтестов"),
             ("Ревью кода", "независимая\nпроверка диффа"), ("Приёмка", "решение\nчеловека")]
    n = len(steps); cx0, gap = 170, 265
    for i, (head, sub) in enumerate(steps):
        cx = cx0 + i * gap
        col = TEAL_DEEP if i == n - 1 else RED
        d.ellipse([cx - 58, 200, cx + 58, 316], fill=col)
        fnum = font(FB, 52)
        center_text(d, cx, 226, str(i + 1), fnum, (255, 255, 255))
        fh = font(FB, 28)
        center_text(d, cx, 350, head, fh, INK)
        fs = font(F, 22)
        for j, line in enumerate(sub.split("\n")):
            center_text(d, cx, 402 + j * 30, line, fs, MUTED)
        if i < n - 1:
            arrow(d, cx + 78, 258)
    # кейс-полоса
    rr(d, [60, 640, W - 60, 920], 28, PALE)
    fc = font(FB, 30)
    d.text((110, 672), "Кейс: «Отменить / Вернуть» — откат правок в Word-стиле", font=fc, fill=INK)
    fs2 = font(F, 24)
    d.text((110, 718), "Одна фича — полный конвейер: от плана до приёмки.", font=fs2, fill=MUTED)
    kpis = [("11", "решений до кода", RED), ("3", "дефекта нашло ревью", RED), ("+22", "новых теста", RED), ("0", "регрессий", TEAL_DEEP)]
    for i, (v, l, c) in enumerate(kpis):
        kx = 110 + i * 410
        d.text((kx, 770), v, font=font(FB, 56), fill=c)
        d.text((kx + (0 if v[0] != "+" else 8), 850), l, font=font(F, 22), fill=MUTED)
    im.save("conv-chain.png")

# ── 2. Терминал с падающим тестом ────────────────────────
def terminal():
    im, d = canvas()
    rr(d, [40, 40, W - 40, H - 40], 30, DARK)
    for i, c in enumerate([(255, 95, 87), (254, 188, 46), (40, 200, 64)]):
        d.ellipse([92 + i * 44, 84, 116 + i * 44, 108], fill=c)
    fm = font(FM, 30); fmb = font(FM + "b" if os.path.exists(FM + "b") else FM, 30)
    y = 160
    d.text((92, y), "$ dotnet test --filter Architecture", font=font("C:/Windows/Fonts/consolai.ttf", 30), fill=(159, 232, 219)); y += 66
    d.text((92, y), "R2_ClimateState_MutatedOnlyBy", font=font(FM, 30), fill=(255, 255, 255)); y += 44
    d.text((92, y), "SanctionedWriters", font=font(FM, 30), fill=(255, 255, 255)); y += 66
    d.text((92, y), "  Failed: несанкционированный писатель", font=font(FM, 30), fill=(255, 107, 107)); y += 46
    d.text((92, y), "  в канонический слайс ClimateState", font=font(FM, 30), fill=(255, 107, 107)); y += 90
    rr(d, [92, y, 620, y + 64], 12, (60, 16, 24))
    d.text((116, y + 12), "1 failed / 2 236 passed", font=font(FM, 30), fill=(255, 107, 107)); y += 120
    d.text((92, y), "Правило R2: у каждого значения —", font=font(F, 26), fill=(180, 180, 186)); y += 38
    d.text((92, y), "ровно один санкционированный писатель", font=font(F, 26), fill=(180, 180, 186))
    im.save("terminal.png")

# ── 3. Лента ADR ─────────────────────────────────────────
def adr_tape():
    im, d = canvas()
    items = [("ADR-009 · 2026-09", "PDF-движок переведён на свободную библиотеку (MIT) — снят лицензионный риск"),
             ("ADR-012 · 2026-09", "Индикация «рассчитано» стала честной: галочка гаснет при правке данных"),
             ("ADR-014 · 2026-09", "«Отменить / Вернуть» через журнал снимков — как в Word, с точкой чистоты")]
    y = 50
    for tag, text in items:
        rr(d, [60, y, W - 60, y + 240], 26, PALE)
        d.text((110, y + 34), tag, font=font(FB, 30), fill=RED)
        # перенос текста на 2 строки
        fbody = font(F, 28)
        words, lines, cur = text.split(" "), [], ""
        for w_ in words:
            t = (cur + " " + w_).strip()
            if d.textlength(t, font=fbody) > 1450:
                lines.append(cur); cur = w_
            else:
                cur = t
        lines.append(cur)
        for j, ln in enumerate(lines[:2]):
            d.text((110, y + 92 + j * 42), ln, font=fbody, fill=INK)
        y += 264
    d.text((60, y + 20), "…и ещё 11 записей — от пост-миграционного контура до честного степпера", font=font(F, 24), fill=MUTED)
    im.save("adr-tape.png")

# ── 4. Телефон-композиция ────────────────────────────────
def phone_composite():
    im, d = canvas()
    ph = Image.open("phone-zcode.jpg").convert("RGB")
    ph_h = 880; ph_w = round(ph.width * ph_h / ph.height)
    ph = ph.resize((ph_w, ph_h))
    px, py = 120, 50
    rr(d, [px - 18, py - 18, px + ph_w + 18, py + ph_h + 18], 40, (29, 29, 27))
    im.paste(ph, (px, py))
    cards = [("Онлайн", "сессия агента на ПК — видна с телефона в реальном времени", TEAL_DEEP),
             ("Планы и show-me", "визуализация фазы открывается на экране телефона", RED),
             ("Аппрув сообщением", "приёмка фазы — из командировки, из машины, из дома", RED)]
    y = 120
    for head, text, c in cards:
        rr(d, [820, y, 1730, y + 220], 26, PALE)
        d.ellipse([860, y + 48, 890, y + 78], fill=c)
        d.text((920, y + 38), head, font=font(FB, 30), fill=INK)
        fbody = font(F, 24)
        words, lines, cur = text.split(" "), [], ""
        for w_ in words:
            t = (cur + " " + w_).strip()
            if d.textlength(t, font=fbody) > 700:
                lines.append(cur); cur = w_
            else:
                cur = t
        lines.append(cur)
        for j, ln in enumerate(lines[:2]):
            d.text((920, y + 92 + j * 34), ln, font=fbody, fill=MUTED)
        y += 268
    im.save("phone-composite.png")

# ── 5. CI-флоу ───────────────────────────────────────────
def ci_flow():
    im, d = canvas()
    flow = [("Изменение", "агент закончил фазу", PALE, INK), ("GitHub", "код уходит в облако", PALE, INK),
            ("CI-проверка", "сборка + 2 237 тестов", PALE, INK), ("Приёмка", "только зелёное", (228, 246, 242), TEAL_DEEP)]
    cw, ch, y = 360, 300, 180
    for i, (head, sub, fill, hc) in enumerate(flow):
        x = 70 + i * 440
        rr(d, [x, y, x + cw, y + ch], 28, fill)
        fh = font(FB, 32)
        center_text(d, x + cw / 2, y + 90, head, fh, hc)
        fs = font(F, 24)
        for j, ln in enumerate(sub.split("\n")):
            center_text(d, x + cw / 2, y + 155 + j * 34, ln, fs, MUTED)
        if i < 3:
            arrow(d, x + cw + 14, y + ch / 2)
    rr(d, [70, 600, W - 70, 920], 28, PALE)
    d.text((120, 640), "Реестр «правило → проверка»", font=font(FB, 30), fill=INK)
    d.text((120, 690), "у каждого правила проекта — адресная проверка: тест, скрипт или шаг ревью", font=font(F, 24), fill=MUTED)
    d.text((120, 770), "Ревью-чеки", font=font(FB, 30), fill=INK)
    d.text((120, 820), "у каждого независимого ревью — перечитываемый вердикт-файл с находками", font=font(F, 24), fill=MUTED)
    im.save("ci-flow.png")

# ── 6. Куда расту ────────────────────────────────────────
def growth():
    im, d = canvas()
    items = [("T", "Telegram-приёмка", "аппрувы фаз прямо в мессенджере", TEAL_DEEP),
             ("R", "База знаний (RAG)", "агент сам находит первоисточник в брендбуке и нормативке", RED),
             ("M", "Интеграции (MCP)", "GitHub, сборки, задачи — как инструменты агента", RED),
             ("A", "Автономные сценарии", "сборка релиза и разбор сбоев; метрики качества агента", TEAL_DEEP)]
    cw, ch = 830, 400
    for i, (letter, head, text, c) in enumerate(items):
        x = 60 + (i % 2) * 850
        y = 70 + (i // 2) * 450
        rr(d, [x, y, x + cw, y + ch], 30, PALE)
        rr(d, [x + 40, y + 60, x + 140, y + 160], 24, c)
        fnum = font(FB, 56)
        d.text((x + 90 - d.textlength(letter, font=fnum) / 2, y + 78), letter, font=fnum, fill=(255, 255, 255))
        d.text((x + 180, y + 66), head, font=font(FB, 32), fill=INK)
        fbody = font(F, 25)
        words, lines, cur = text.split(" "), [], ""
        for w_ in words:
            t = (cur + " " + w_).strip()
            if d.textlength(t, font=fbody) > 590:
                lines.append(cur); cur = w_
            else:
                cur = t
        lines.append(cur)
        for j, ln in enumerate(lines[:3]):
            d.text((x + 180, y + 120 + j * 38), ln, font=fbody, fill=MUTED)
    im.save("growth.png")

conv_chain(); terminal(); adr_tape(); phone_composite(); ci_flow(); growth()
print("schemes done:", [f for f in os.listdir(".") if f.endswith(".png") and f[0].islower()])
