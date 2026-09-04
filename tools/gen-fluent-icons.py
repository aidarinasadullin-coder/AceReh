#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Генерация Themes/Icons.Fluent.xaml из @fluentui/svg-icons (Фаза 2 редизайна).

Иконки в репозиторий попадают ТОЛЬКО генерацией (план Ф2.3): этот скрипт
читает tools/icons.txt (Display Name -> ключ XAML), скачивает SVG 24px
(вариант regular) из первоисточника microsoft/fluentui-system-icons
(лицензия MIT, подтверждена по LICENSE репозитория и npm metadata),
извлекает геометрию единственного <path> и пишет Themes/Icons.Fluent.xaml.

Использование:
    python tools/gen-fluent-icons.py            # кэш используется, докачка недостающих
    python tools/gen-fluent-icons.py --refresh  # принудительная перекачка

Выход детерминирован: порядок ключей = порядок строк icons.txt; содержимое
кэша на результат не влияет. Сгенерированный файл коммитится; править его
вручную запрещено.
"""

from __future__ import annotations

import argparse
import re
import sys
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
ICONS_TXT = REPO_ROOT / "tools" / "icons.txt"
CACHE_DIR = REPO_ROOT / "tools" / ".icon-cache"
OUTPUT = REPO_ROOT / "src" / "Themes" / "Icons.Fluent.xaml"

RAW_URL = (
    "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/"
    "assets/{display}/SVG/ic_fluent_{slug}_24_regular.svg"
)
LICENSE_URL = "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/LICENSE"
LICENSE_NOTE = (
    "Источник: microsoft/fluentui-system-icons (@fluentui/svg-icons), "
    "лицензия MIT (Copyright (c) 2020 Microsoft Corporation). "
    "Проверено по LICENSE репозитория и npm metadata."
)

SVG_PATH_RE = re.compile(r"<path\b[^>]*>", re.S)
D_ATTR_RE = re.compile(r'\bd\s*=\s*"([^"]+)"')
FILL_RULE_RE = re.compile(r'fill-rule\s*=\s*"(\w+)"')
TAG_RE = re.compile(r"<\s*([a-zA-Z]+)")


def slugify(display: str) -> str:
    """'Chevron Down' -> 'chevron_down' (слаг имен файлов @fluentui/svg-icons)."""
    return display.strip().lower().replace(" ", "_")


def load_manifest() -> list[tuple[str, str]]:
    """Строки icons.txt: 'Display Name<TAB>Ключ'. Комментарии (#) и пустые — пропустить."""
    entries: list[tuple[str, str]] = []
    seen_keys: set[str] = set()
    for line_no, raw in enumerate(ICONS_TXT.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split("\t")
        if len(parts) != 2 or not parts[0] or not parts[1]:
            fail(f"{ICONS_TXT.name}:{line_no}: ожидается 'Display Name<TAB>Ключ', получено: {raw!r}")
        display, key = parts[0].strip(), parts[1].strip()
        if not re.fullmatch(r"Icon\.[A-Za-z0-9]+", key):
            fail(f"{ICONS_TXT.name}:{line_no}: ключ '{key}' не соответствует шаблону 'Icon.Имя'")
        if key in seen_keys:
            fail(f"{ICONS_TXT.name}:{line_no}: дублирующийся ключ '{key}'")
        seen_keys.add(key)
        entries.append((display, key))
    if not entries:
        fail("icons.txt пуст")
    return entries


def fail(message: str) -> None:
    print(f"ОШИБКА: {message}", file=sys.stderr)
    sys.exit(1)


def fetch_svg(display: str, refresh: bool) -> str:
    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    slug = slugify(display)
    cache_file = CACHE_DIR / f"ic_fluent_{slug}_24_regular.svg"
    if cache_file.exists() and not refresh:
        return cache_file.read_text(encoding="utf-8")

    url = RAW_URL.format(display=display.replace(" ", "%20"), slug=slug)
    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            body = response.read().decode("utf-8")
    except Exception as error:  # noqa: BLE001 — единая точка фейла с понятным сообщением
        fail(f"не удалось скачать '{display}': {url}\n  {error}")

    if not body.lstrip().startswith("<"):
        fail(f"'{display}': ответ не является SVG ({url})")
    cache_file.write_text(body, encoding="utf-8")
    return body


def extract_geometry(svg: str, display: str) -> tuple[str, str]:
    """(mini-language строка геометрии WPF, fill-rule) из SVG.

    Требования ревью Ф2: fail-fast на составные иконки (несколько path или
    не-path фигуры); fill-rule='nonzero' кодируется префиксом 'F1'
    (EvenOdd — дефолт WPF и без префикса).
    """
    tags = {name.lower() for name in TAG_RE.findall(svg)}
    unexpected = tags - {"svg", "path", "title"}
    if unexpected:
        fail(f"'{display}': составная иконка, элементы {sorted(unexpected)} — требуется ручное решение")

    paths = SVG_PATH_RE.findall(svg)
    if len(paths) != 1:
        fail(f"'{display}': ожидается ровно один <path>, найдено {len(paths)}")

    path = paths[0]
    d_match = D_ATTR_RE.search(path)
    if not d_match:
        fail(f"'{display}': у <path> нет атрибута d")

    d = d_match.group(1).strip()
    fill_rule = "evenodd"
    rule_match = FILL_RULE_RE.search(path)
    if rule_match:
        fill_rule = rule_match.group(1).lower()
    if fill_rule not in ("evenodd", "nonzero"):
        fail(f"'{display}': неизвестный fill-rule '{fill_rule}'")

    prefix = "F1" if fill_rule == "nonzero" else "F0"
    return f"{prefix}{d}", fill_rule


def render(entries: list[tuple[str, str]], geometries: list[tuple[str, str, str]]) -> str:
    lines = [
        '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
        '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
        "",
        "<!--",
        "    !!! СГЕНЕРИРОВАНО — НЕ ПРАВИТЬ ВРУЧНУЮ !!!",
        "    Регенерация: python tools/gen-fluent-icons.py",
        f"    Манифест: tools/icons.txt ({len(entries)} иконок).",
        f"    {LICENSE_NOTE}",
        "    Геометрии 24px regular; размер и кисть задаёт потребитель:",
        '    <Path Data="{StaticResource Icon.Имя}" Width="16" Height="16" Stretch="Uniform"/>',
        "",
        "    Формат строки манифеста: 'Display Name<TAB>Icon.Ключ'.",
        "    Карта соответствия текущих inline Path -> Fluent: docs/design/icons.md.",
        "-->",
        "",
    ]

    for (display, key), (geometry, _, _) in zip(entries, geometries):
        lines.append(f"    <!-- {display} -->")
        lines.append(f'    <Geometry x:Key="{key}">{geometry}</Geometry>')
        lines.append("")

    lines.append("</ResourceDictionary>")
    lines.append("")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Генерация Icons.Fluent.xaml из @fluentui/svg-icons")
    parser.add_argument("--refresh", action="store_true", help="принудительно перекачать SVG из сети, игнорируя кэш")
    args = parser.parse_args()

    entries = load_manifest()

    geometries: list[tuple[str, str, str]] = []
    for display, key in entries:
        svg = fetch_svg(display, args.refresh)
        geometry, fill_rule = extract_geometry(svg, display)
        geometries.append((geometry, key, fill_rule))
        print(f"  ok  {key:<22} <- {display} ({fill_rule})")

    output = render(entries, geometries)
    OUTPUT.write_text(output, encoding="utf-8", newline="\n")
    print(f"\nЗаписан {OUTPUT.relative_to(REPO_ROOT)}: {len(entries)} геометрий.")


if __name__ == "__main__":
    main()
