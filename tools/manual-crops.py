# -*- coding: utf-8 -*-
"""Детерминированные кропы для иллюстраций инструкции.

Источник — эталонные полные скрины окна приложения (docs/manual/src/shots/
state-*.png, снимались tools/shoot.py при 1440x900, масштаб 100%). Скрипт
вырезает кадры по координатной таблице и кладёт их в docs/manual/src/media/
под именами, которые использует docs/manual/src/template.html.

Правило кадра (план 2026-09-19-manual-screenshots-plan.md §1):
  - резать по границам блоков UI, никогда посередине текста/поля/строки;
  - степпер — все 5 шагов целиком;
  - строка состояния — только сама полоса, без панели выше, хвост <= 16px;
  - внутри семейства одинаковая высота.

Запуск:  python tools/manual-crops.py
"""
import shutil
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SHOTS = ROOT / "docs" / "manual" / "src" / "shots"
MEDIA = ROOT / "docs" / "manual" / "src" / "media"

# (источник, приёмник, box или None для полной копии)
CROPS = [
    # Полнокадровые иллюстрации шагов
    ("state-climate.png", "00-trim.png", None),
    ("state-construction.png", "01-construction.png", None),
    ("state-thermal.png", "thermal-modes.png", None),
    ("state-thermal-manual.png", "thermal-manual-surface.png", None),
    ("state-thermal-validation.png", "thermal-surface-warning.png", None),
    ("state-thermal-advice.png", "thermal-advice.png", None),
    ("state-hydraulics.png", "03-hydraulics.png", None),
    ("state-results.png", "04-results.png", None),
    # Шапка: кнопки управления (сброс, отмена, файл, рассчитать, отчёт PDF)
    ("state-climate.png", "c-toolbar.png", (880, 4, 1292, 52)),
    # Степпер: все 5 шагов целиком
    ("state-recalc.png", "c-stepper.png", (0, 60, 228, 318)),
    # Ошибка ввода: степпер с ⚠ + форма с полем вне диапазона
    ("state-error.png", "c-error-stepper.png", (0, 60, 840, 622)),
    # Строки состояния: только полоса, без панели выше
    ("state-gw-info.png", "sb-info2.png", (0, 866, 700, 900)),
    ("state-thermal-lay.png", "sb-recalc.png", (0, 866, 460, 900)),
    ("state-results.png", "sb-ready.png", (0, 866, 272, 900)),
    ("state-recalc.png", "sb-recalc-scenario.png", (1080, 866, 1440, 900)),
    ("state-error.png", "sb-error-range.png", (0, 866, 376, 900)),
    ("state-error.png", "sb-error.png", (0, 866, 376, 900)),
    # Таблица контуров со статусами (вкл. предупреждения контура 2)
    ("state-hydraulics.png", "c-circuit-status.png", (250, 515, 1420, 645)),
    # Развёрнутые дополнительные параметры (полоса под сводкой мощности)
    ("state-thermal-extra.png", "c-thermal-extra.png", (216, 735, 1425, 856)),
    # Панель «Сводка проекта» (снималась при окне 1900x900)
    ("state-summary.png", "c-summary.png", (1666, 62, 1898, 790)),
    # Меню «Отчёт PDF ▾» (снималось tools/shoot-screen.py, уже готово)
    ("state-menu.png", "report-menu.png", None),
    # Страницы PDF-отчётов (150 dpi)
    ("state-pg1.png", "pg1.png", None),
    ("state-pg2.png", "pg2.png", None),
    ("state-pz1.png", "pz1.png", None),
    ("state-pz2.png", "pz2.png", None),
    ("state-pz3.png", "pz3.png", None),
]


def main() -> None:
    MEDIA.mkdir(parents=True, exist_ok=True)
    for src, dst, box in CROPS:
        src_path = SHOTS / src
        dst_path = MEDIA / dst
        if not src_path.exists():
            raise SystemExit(f"нет исходника: {src_path}")
        if box is None:
            shutil.copyfile(src_path, dst_path)
            print(f"{dst} <- {src} (копия)")
        else:
            img = Image.open(src_path)
            crop = img.crop(box)
            crop.save(dst_path, "PNG")
            print(f"{dst} <- {src} {box} -> {crop.size[0]}x{crop.size[1]}")


if __name__ == "__main__":
    main()
