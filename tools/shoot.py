# -*- coding: utf-8 -*-
"""Снятие окна приложения «Калькулятор снеготаяния REHAU» в PNG.

Скриншоты инструкции снимаются этой утилитой (единый масштаб, без ручных
вырезок): PrintWindow клиентской области окна -> PNG 1440x900.

Запуск:  python tools/shoot.py <имя_кадра> [<имя_кадра2> ...]
Кадры:   docs/manual/src/shots/<имя>.png
Опции:   --title <подстрока>  искать окно по подстроке заголовка (по умолчанию
                              «Калькулятор снеготаяния») — для диалогов
         --out <dir>          выходная папка (по умолчанию docs/manual/src/shots)
"""
import argparse
import ctypes
import ctypes.wintypes as wt
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
u = ctypes.windll.user32
gdi = ctypes.windll.gdi32
u.SetProcessDPIAware()

PW_CLIENTONLY = 1
PW_RENDERFULLCONTENT = 2


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [
        ("biSize", ctypes.c_uint32), ("biWidth", ctypes.c_int32),
        ("biHeight", ctypes.c_int32), ("biPlanes", ctypes.c_uint16),
        ("biBitCount", ctypes.c_uint16), ("biCompression", ctypes.c_uint32),
        ("biSizeImage", ctypes.c_uint32), ("biXPelsPerMeter", ctypes.c_int32),
        ("biYPelsPerMeter", ctypes.c_int32), ("biClrUsed", ctypes.c_uint32),
        ("biClrImportant", ctypes.c_uint32),
    ]


def find_hwnd(substr: str):
    found = []

    @ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
    def cb(h, _l):
        b = ctypes.create_unicode_buffer(512)
        u.GetWindowTextW(h, b, 512)
        if substr in b.value and u.IsWindowVisible(h):
            found.append((h, b.value))
        return True

    u.EnumWindows(cb, 0)
    if not found:
        raise SystemExit(f"окно с заголовком ~{substr!r} не найдено")
    return found[0]


def shoot(hwnd: int, out: Path) -> None:
    rc = wt.RECT()
    u.GetClientRect(hwnd, ctypes.byref(rc))
    w, h = rc.right, rc.bottom
    hdc = u.GetWindowDC(hwnd)
    mem = gdi.CreateCompatibleDC(hdc)
    bmp = gdi.CreateCompatibleBitmap(hdc, w, h)
    gdi.SelectObject(mem, bmp)
    ok = u.PrintWindow(hwnd, mem, PW_CLIENTONLY | PW_RENDERFULLCONTENT)
    if not ok:
        raise SystemExit("PrintWindow отказал")

    bmi = BITMAPINFOHEADER()
    bmi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bmi.biWidth = w
    bmi.biHeight = -h
    bmi.biPlanes = 1
    bmi.biBitCount = 32
    bmi.biCompression = 0
    buf = ctypes.create_string_buffer(w * h * 4)
    gdi.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bmi), 0)
    img = Image.frombuffer("RGBA", (w, h), buf.raw, "raw", "BGRA", 0, 1)
    img.convert("RGB").save(out, "PNG")
    gdi.DeleteObject(bmp)
    gdi.DeleteDC(mem)
    u.ReleaseDC(hwnd, hdc)
    print(f"{out} ({w}x{h})")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("names", nargs="+", help="имена кадров (без .png)")
    ap.add_argument("--title", default="Калькулятор снеготаяния")
    ap.add_argument("--out", default=str(ROOT / "docs" / "manual" / "src" / "shots"))
    args = ap.parse_args()

    hwnd, title = find_hwnd(args.title)
    outdir = Path(args.out)
    outdir.mkdir(parents=True, exist_ok=True)
    print(f"окно: {title!r}")
    for name in args.names:
        shoot(hwnd, outdir / f"{name}.png")


if __name__ == "__main__":
    main()
