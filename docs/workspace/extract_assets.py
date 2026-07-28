# -*- coding: utf-8 -*-
"""Извлечение векторных элементов бренда REHAU из гайдлайна PDF в SVG."""
import os
import pymupdf

doc = pymupdf.open("rehau_guideline.pdf")
OUT = "rehau_assets"
os.makedirs(OUT, exist_ok=True)

def hexc(c):
    if c is None:
        return None
    # нормализация «почти чёрного/белого» из-за погрешностей конвертации
    vals = [0 if v < 0.02 else 1 if v > 0.98 else v for v in c]
    return "#%02X%02X%02X" % tuple(round(v * 255) for v in vals)

def path_d(items, close_path=False):
    parts = []
    cur = None  # текущая точка пера

    def moveto(p):
        nonlocal cur
        if cur is None or abs(p.x - cur.x) > 0.01 or abs(p.y - cur.y) > 0.01:
            parts.append(f"M{p.x:.2f},{p.y:.2f}")
        cur = p

    for it in items:
        op = it[0]
        if op == "l":
            _, p1, p2 = it
            moveto(p1)
            parts.append(f"L{p2.x:.2f},{p2.y:.2f}")
            cur = p2
        elif op == "c":
            _, p1, p2, p3, p4 = it
            moveto(p1)
            parts.append(f"C{p2.x:.2f},{p2.y:.2f} {p3.x:.2f},{p3.y:.2f} {p4.x:.2f},{p4.y:.2f}")
            cur = p4
        elif op == "re":
            r = it[1]
            parts.append(
                f"M{r.x0:.2f},{r.y0:.2f} L{r.x1:.2f},{r.y0:.2f} L{r.x1:.2f},{r.y1:.2f} L{r.x0:.2f},{r.y1:.2f} Z"
            )
            cur = None
        elif op == "qu":
            q = it[1]
            parts.append(
                f"M{q.ul.x:.2f},{q.ul.y:.2f} L{q.ur.x:.2f},{q.ur.y:.2f} L{q.lr.x:.2f},{q.lr.y:.2f} L{q.ll.x:.2f},{q.ll.y:.2f} Z"
            )
            cur = None
    if close_path:
        parts.append("Z")
    return " ".join(parts)

def collect(page_no, region, fill_filter=None, min_size=0.0, max_size=None):
    """Собрать рисунки страницы внутри region. fill_filter: 'white', 'black', None."""
    pg = doc[page_no - 1]
    R = pymupdf.Rect(*region)
    out = []
    for d in pg.get_drawings():
        r = d["rect"]
        if r.is_empty or r.is_infinite:
            continue
        if not r.intersects(R):
            continue
        if r.width < min_size and r.height < min_size:
            continue
        if max_size and (r.width > max_size or r.height > max_size):
            continue
        fill = d.get("fill")
        if fill_filter == "white" and not (fill and all(v > 0.95 for v in fill)):
            continue
        if fill_filter == "black" and not (fill and all(v < 0.05 for v in fill)):
            continue
        out.append(d)
    return out

def save_svg(name, drawings, pad=2.0, comment=""):
    if not drawings:
        print(f"!! {name}: пусто"); return
    bbox = None
    for d in drawings:
        bbox = pymupdf.Rect(d["rect"]) if bbox is None else bbox | d["rect"]
    bbox = pymupdf.Rect(bbox.x0 - pad, bbox.y0 - pad, bbox.x1 + pad, bbox.y1 + pad)
    body = []
    for d in drawings:
        dd = path_d(d["items"], close_path=d.get("closePath", False))
        if not dd:
            continue
        t = d["type"]
        attrs = []
        if "f" in t and d.get("fill") is not None:
            attrs.append(f'fill="{hexc(d["fill"])}"')
            if d.get("even_odd"):
                attrs.append('fill-rule="evenodd"')
            if d.get("fill_opacity", 1) < 1:
                attrs.append(f'fill-opacity="{d["fill_opacity"]:.3f}"')
        else:
            attrs.append('fill="none"')
        if "s" in t and d.get("color") is not None:
            attrs.append(f'stroke="{hexc(d["color"])}" stroke-width="{d.get("width",1):.2f}"')
        body.append(f"  <path {' '.join(attrs)} d=\"{dd}\"/>")
    w, h = bbox.width, bbox.height
    svg = (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{w:.1f}" height="{h:.1f}" '
        f'viewBox="{bbox.x0:.2f} {bbox.y0:.2f} {w:.2f} {h:.2f}">\n'
        + (f"  <!-- {comment} -->\n" if comment else "")
        + "\n".join(body)
        + "\n</svg>\n"
    )
    with open(os.path.join(OUT, name), "w", encoding="utf-8") as f:
        f.write(svg)
    print(f"ok {name}: {len(drawings)} фигур, {w:.0f}x{h:.0f}")

# ---- 1. Основной логотип (стр. 5): знак + чёрное шрифтовое начертание ----
logo = collect(5, (290, 250, 915, 480))
save_svg("logo_main_color.svg", logo, comment="Основной логотип REHAU, цветной")

# ---- 2. Знак отдельно ----
icon = collect(5, (295, 260, 425, 395))
save_svg("logo_icon_color.svg", icon, comment="Графический знак REHAU (пиксельное сердце)")

# ---- 3. Шрифтовое начертание отдельно ----
word = collect(5, (440, 300, 915, 480), fill_filter="black")
save_svg("logo_wordmark_black.svg", word, comment="Шрифтовое начертание РЕХАУ")

# ---- 4. Белый монохром (стр. 6, чёрный бокс) ----
white = collect(6, (240, 295, 600, 485), fill_filter="white")
save_svg("logo_white.svg", white, comment="Логотип REHAU, белый монохром")

# ---- 5. Чёрный монохром (стр. 6, белый бокс) ----
black = collect(6, (645, 295, 1005, 485), fill_filter="black")
save_svg("logo_black.svg", black, comment="Логотип REHAU, чёрный монохром")

# ---- 6. Дополнительный графический элемент (стр. 36, две композиции слева) ----
elem_big = collect(36, (60, 265, 320, 560))
save_svg("element_squares_large.svg", elem_big, comment="Фирменный доп. элемент, большая композиция")
elem_small = collect(36, (375, 305, 510, 520))
save_svg("element_squares_small.svg", elem_small, comment="Фирменный доп. элемент, малая композиция")

# ---- 7. Паттерны (стр. 36) ----
pat_white = collect(36, (665, 268, 1242, 634))
save_svg("pattern_white_bg.svg", pat_white, pad=0, comment="Фирменный паттерн на белом фоне")
pat_green = collect(36, (64, 660, 642, 1026))
save_svg("pattern_green_bg.svg", pat_green, pad=0, comment="Фирменный паттерн на зелёном фоне")
pat_black = collect(36, (664, 660, 1242, 1034))
save_svg("pattern_black_bg.svg", pat_black, pad=0, comment="Фирменный паттерн на чёрном фоне")

# ---- 8. Слоган (стр. 16): знак-сетка и локапы ----
slogan_icon = collect(16, (70, 270, 200, 410), fill_filter="black")
save_svg("slogan_icon_black.svg", slogan_icon, comment="Знак слогана (сетка 3x3)")

print("\n-- clusters page 16 (для локапов слогана) --")
pg = doc[16 - 1]
for d in pg.get_drawings():
    r = d["rect"]
    if r.width > 15 and r.height > 15:
        print("  ", tuple(round(v, 1) for v in r), hexc(d.get("fill")))
