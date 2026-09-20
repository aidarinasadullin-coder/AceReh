# -*- coding: utf-8 -*-
"""Снятие области экрана (для кадров с popup-меню, которые не попадают
в PrintWindow главного окна).

Запуск:  python tools/shoot-screen.py <выход.png> <left> <top> <right> <bottom>
Координаты — экранные физические пиксели.
"""
import sys
from pathlib import Path

from PIL import ImageGrab

out = Path(sys.argv[1])
box = tuple(int(v) for v in sys.argv[2:6])
img = ImageGrab.grab(bbox=box, all_screens=True)
img.save(out, "PNG")
print(f"{out} {img.size}")
