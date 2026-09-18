# -*- coding: utf-8 -*-
"""Сборка инструкции пользователя (docs/manual/README.html) из одного источника.

Источник: docs/manual/src/template.html + docs/manual/src/media/*.png.
Плейсхолдеры: {{IMG:имя.png}} -> base64 data URI; {{VERSION}} -> версия из csproj.
README.html самодостаточен (base64, внешних зависимостей нет) — деплоится
рядом с exe через src/SnowMeltingCalculator.csproj. README вручную не править:
правим template.html и пересобираем.

Запуск:  python docs/manual/build_manual.py
Опции:   --version 1.7.0   подставить версию вручную (по умолчанию — из csproj)
"""
import argparse
import base64
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent
TEMPLATE = ROOT / "docs" / "manual" / "src" / "template.html"
MEDIA = ROOT / "docs" / "manual" / "src" / "media"
CSproj = ROOT / "src" / "SnowMeltingCalculator.csproj"
OUTPUT = ROOT / "docs" / "manual" / "README.html"


def version_from_csproj() -> str:
    text = CSproj.read_text(encoding="utf-8")
    m = re.search(r"<Version>([^<]+)</Version>", text)
    if not m:
        sys.exit(f"не найден <Version> в {CSproj}")
    return m.group(1).strip()


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--version", default=None, help="версия вручную (иначе из csproj)")
    args = ap.parse_args()

    version = args.version or version_from_csproj()
    html = TEMPLATE.read_bytes().decode("utf-8")

    def inline(m: re.Match) -> str:
        name = m.group(1)
        path = MEDIA / name
        if not path.exists():
            sys.exit(f"нет картинки для плейсхолдера: {{IMG:{name}}} ({path})")
        b64 = base64.b64encode(path.read_bytes()).decode("ascii")
        return f'src="data:image/png;base64,{b64}"'

    html = re.sub(r'src="\{\{IMG:([^}]+)\}\}"', inline, html)
    html = html.replace("{{VERSION}}", version)

    leftover = re.findall(r"\{\{[A-Z]+:[^}]*\}\}|\{\{VERSION\}\}", html)
    if leftover:
        sys.exit(f"незаполненные плейсхолдеры: {leftover}")

    OUTPUT.write_bytes(html.encode("utf-8"))
    print(f"собрано {OUTPUT} (версия {version}, {len(html) / 1048576:.2f} MB)")


if __name__ == "__main__":
    main()
