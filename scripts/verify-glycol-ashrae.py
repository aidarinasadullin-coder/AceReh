# -*- coding: utf-8 -*-
"""Сверка и пересборка data/glycol_data.json из ASHRAE Handbook—Fundamentals (2009), гл. 31.

Возникло из сверки 2026-09-20 (роадмап 2.2): база заявляла источник
«ASHRAE 2009, Dow Chemical Tables», но поячеечная сверка с PDF показала сдвиги
колонок в cp/λ и вязкость в чужих единицах. Инструмент — канонический
acceptance-гейт: база обязана совпадать с PDF ячейка-в-ячейку (в допуске
округления).

Режимы:
  extract  --pdf PATH [--out PATH]      извлечь таблицы 4-13 в JSON (кэш/инспекция)
  check    --pdf PATH --json PATH       сверить базу с PDF, exit 1 при расхождении
  rebuild  --pdf PATH --json PATH       пересобрать базу из PDF (мета обновляется)

Требует PyMuPDF (pip install pymupdf).
Пути по умолчанию: PDF вне репозитория (нормативная папка владельца), json — data/glycol_data.json.
"""
import argparse
import json
import os
import re
import sys

import fitz

# --- Перевод единиц (ASHRAE Tables 4-13 -> СИ) ---
LBFT3_TO_KGM3 = 16.018463        # плотность, lb/ft3 -> kg/m3
BTULBF_TO_KJKG = 4.1868          # удельная теплоёмкость, Btu/(lb*F) -> kJ/(kg*K)
LAMBDA_TO_WMK = 1.730735         # теплопроводность, Btu*ft/(h*ft2*F) -> W/(m*K)
LBFTFT_TO_PAS = 1.4881639 / 3600.0  # динамическая вязкость, lb/(ft*h) -> Pa*s

MINUS = '\u2212\u2012\u2013\u2014-'
NUM_RE = re.compile('^[' + MINUS + ']?\\d+(\\.\\d+)?$')
INT_RE = re.compile('^[' + MINUS + ']?\\d+$')
CONC_RE = re.compile('^(\\d+)%$')

GRID_TABLES = {
    'EG_density':      (778, 'Table 6'),
    'EG_specific_heat': (778, 'Table 7'),
    'EG_conductivity': (779, 'Table 8'),
    'EG_viscosity':    (779, 'Table 9'),
    'PG_density':      (780, 'Table 10'),
    'PG_specific_heat': (780, 'Table 11'),
    'PG_conductivity': (781, 'Table 12'),
    'PG_viscosity':    (781, 'Table 13'),
}
GLYCOL_TABLES = {
    'ethylene_glycol': {
        'density_kg_m3': 'EG_density',
        'specific_heat_kJ_kgK': 'EG_specific_heat',
        'thermal_conductivity_W_mK': 'EG_conductivity',
        'kinematic_viscosity_mm2_s': 'EG_viscosity',
    },
    'propylene_glycol': {
        'density_kg_m3': 'PG_density',
        'specific_heat_kJ_kgK': 'PG_specific_heat',
        'thermal_conductivity_W_mK': 'PG_conductivity',
        'kinematic_viscosity_mm2_s': 'PG_viscosity',
    },
}
# Допуски check-режима: округление базы + округление источника
TOL_ABS = {
    'density_kg_m3': 0.15,
    'specific_heat_kJ_kgK': 0.006,
    'thermal_conductivity_W_mK': 0.002,
}
TOL_REL_VISC = 0.01
TOL_FREEZING = 0.06


def norm_num(text):
    text = text.replace('\u2212', '-').replace('\u2012', '-').replace('\u2013', '-').replace('\u2014', '-')
    try:
        return float(text)
    except ValueError:
        return None


def f_to_c(temp_f, ndigits=1):
    return round((temp_f - 32.0) / 1.8, ndigits)


def lines_of(page):
    grouped = {}
    for w in page.get_text('words'):
        grouped.setdefault((w[5], w[6]), []).append(w)
    lines = []
    for key in sorted(grouped, key=lambda k: grouped[k][0][1]):
        words = sorted(grouped[key], key=lambda w: w[0])
        lines.append({
            'y': (min(w[1] for w in words) + max(w[3] for w in words)) / 2,
            'text': ' '.join(w[4] for w in words),
            'words': words,
        })
    return lines


def find_marker(lines, prefix, after_y=0.0):
    for ln in lines:
        if ln['y'] > after_y and ln['text'].strip().startswith(prefix):
            return ln['y']
    return None


def extract_grid_table(page, title_prefix):
    lines = lines_of(page)
    start_y = find_marker(lines, title_prefix)
    end_y = find_marker(lines, 'Source:', start_y or 0.0)
    if start_y is None or end_y is None:
        raise RuntimeError(f'{title_prefix}: границы таблицы не найдены')
    region = [ln for ln in lines if start_y < ln['y'] < end_y]

    conc_words = []
    for ln in region:
        for w in ln['words']:
            m = CONC_RE.match(w[4])
            if m:
                conc_words.append((float(m.group(1)), (w[0] + w[2]) / 2))
    conc_words.sort(key=lambda t: t[1])
    anchors = []
    for conc, x in conc_words:
        if anchors and abs(anchors[-1][1] - x) < 5:
            continue
        anchors.append((conc, x))
    col_x = [a[1] for a in anchors]
    concs = [int(a[0]) for a in anchors]
    x_left = min(col_x)

    temps = []
    for ln in region:
        for w in ln['words']:
            if INT_RE.match(w[4]) and (w[0] + w[2]) / 2 < x_left - 4:
                v = int(norm_num(w[4]))
                if -40 <= v <= 300:
                    temps.append((v, ln['y']))
    temps.sort(key=lambda t: t[1])
    rows = []
    for v, y in temps:
        if rows and abs(rows[-1][1] - y) < 3:
            continue
        rows.append((v, y))

    grid = {str(t): {str(c): None for c in concs} for t, _ in rows}
    for ln in region:
        for w in ln['words']:
            if not NUM_RE.match(w[4]):
                continue
            xc = (w[0] + w[2]) / 2
            yc = (w[1] + w[3]) / 2
            if xc < x_left - 4:
                continue
            ci, best_dx = None, 1e9
            for i, ax in enumerate(col_x):
                if abs(ax - xc) < best_dx:
                    best_dx, ci = abs(ax - xc), i
            ri, best_dy = None, 1e9
            for i, (_, ty) in enumerate(rows):
                if abs(ty - yc) < best_dy:
                    best_dy, ri = abs(ty - yc), i
            if ci is None or ri is None or best_dx > 14 or best_dy > 6:
                raise RuntimeError(f'{title_prefix}: слово вне сетки: {w[4]}')
            grid[str(rows[ri][0])][str(concs[ci])] = norm_num(w[4])
    return {'concs': concs, 'temps_F': [t for t, _ in rows], 'grid': grid}


def extract_freezing_table_with_stars(page, title_prefix):
    """Tables 4-5 лежат на странице рядом (левая/правая панели).

    Столбцы панели: mass %, vol %, freezing F ('*' — ниже -60 F), boiling F.
    """
    lines = lines_of(page)
    mid = page.rect.width / 2.0

    def side_of(ln):
        xs = [(w[0] + w[2]) / 2 for w in ln['words']]
        return sum(xs) / len(xs)

    title_y = None
    panel_left = True
    for ln in lines:
        if title_prefix in ln['text']:
            title_y = ln['y']
            panel_left = side_of(ln) < mid
            break
    end_y = None
    for ln in lines:
        if title_y is not None and ln['y'] > title_y and 'Source:' in ln['text'] \
                and (side_of(ln) < mid) == panel_left:
            end_y = ln['y']
            break
    if title_y is None or end_y is None:
        raise RuntimeError(f'{title_prefix}: границы таблицы не найдены')

    header_ys = [ln['y'] for ln in lines
                 if title_y < ln['y'] < end_y and (side_of(ln) < mid) == panel_left
                 and ('By Volume' in ln['text'] or 'By Mass' in ln['text'] or 'psia' in ln['text'])]
    if not header_ys:
        raise RuntimeError(f'{title_prefix}: шапка таблицы не найдена')
    header_end_y = max(header_ys)

    cells = []
    for ln in lines:
        if not (header_end_y < ln['y'] < end_y):
            continue
        for w in ln['words']:
            wx = (w[0] + w[2]) / 2
            if (wx >= mid) == panel_left:
                continue
            cells.append(((w[1] + w[3]) / 2, wx, w[4]))
    cells.sort(key=lambda c: (c[0], c[1]))
    table_rows = []
    for y, x, text in cells:
        if table_rows and abs(table_rows[-1][0] - y) < 3:
            table_rows[-1][1].append(text)
        else:
            table_rows.append((y, [text]))
    rows = []
    for _, tokens in table_rows:
        nums = [norm_num(t) for t in tokens if NUM_RE.match(t)]
        has_star = any(t == '*' for t in tokens)
        if has_star:
            if len(nums) != 3:
                raise RuntimeError(f'{title_prefix}: строка со звёздочкой: {tokens}')
            mass, vol, bp = nums
            rows.append({'mass_pct': mass, 'vol_pct': vol, 'freezing_F': None,
                         'boiling_F': bp, 'freezing_below_minus60F': True})
        else:
            if len(nums) != 4:
                raise RuntimeError(f'{title_prefix}: строка без звёздочки: {tokens}')
            mass, vol, fz, bp = nums
            rows.append({'mass_pct': mass, 'vol_pct': vol, 'freezing_F': fz,
                         'boiling_F': bp, 'freezing_below_minus60F': False})
    if not rows or rows[0]['mass_pct'] != 0.0:
        raise RuntimeError(f'{title_prefix}: первая строка не mass=0')
    for a, b in zip(rows, rows[1:]):
        if b['mass_pct'] <= a['mass_pct']:
            raise RuntimeError(f'{title_prefix}: mass не возрастает: {a["mass_pct"]} -> {b["mass_pct"]}')
    return {'rows': rows}


def extract_reference(pdf_path):
    doc = fitz.open(pdf_path)
    # Контроль издания: главы 31 «Secondary Coolants (Brines)» 2009 Handbook
    page_text = doc[776].get_text()
    if 'Secondary Coolants' not in page_text:
        raise RuntimeError('стр. 776 не содержит главу «Secondary Coolants (Brines)» — '
                           'не тот PDF или другое издание; страницы таблиц захардкожены')
    ref = {'grids': {}, 'freezing': {}}
    for key, (pno, title) in GRID_TABLES.items():
        ref['grids'][key] = extract_grid_table(doc[pno], title)
    ref['freezing']['ethylene_glycol'] = extract_freezing_table_with_stars(doc[776], 'Table 4')
    ref['freezing']['propylene_glycol'] = extract_freezing_table_with_stars(doc[776], 'Table 5')
    doc.close()
    return ref


def grid_to_metric(table, property_key):
    """Таблица ASHRAE -> узлы СИ: [(temp_c, {conc: value_or_None})]."""
    out = {}
    rho = None
    for temp_f in table['temps_F']:
        temp_c = f_to_c(temp_f)
        out[temp_c] = {}
        for conc in table['concs']:
            v = table['grid'][str(temp_f)][str(conc)]
            if v is None:
                out[temp_c][conc] = None
                continue
            if property_key == 'density_kg_m3':
                out[temp_c][conc] = round(v * LBFT3_TO_KGM3, 1)
            elif property_key == 'specific_heat_kJ_kgK':
                out[temp_c][conc] = round(v * BTULBF_TO_KJKG, 3)
            elif property_key == 'thermal_conductivity_W_mK':
                out[temp_c][conc] = round(v * LAMBDA_TO_WMK, 3)
            elif property_key == 'kinematic_viscosity_mm2_s':
                raise RuntimeError('вязкость считается через плотность: используй viscosity_to_metric')
            else:
                raise RuntimeError(property_key)
    return out


def viscosity_to_metric(vtab, rhotab):
    """Кинематическая вязкость: nu = mu/rho. mu - lb/(ft*h), rho - lb/ft3 той же ячейки."""
    out = {}
    for temp_f in vtab['temps_F']:
        temp_c = f_to_c(temp_f)
        out[temp_c] = {}
        for conc in vtab['concs']:
            mu = vtab['grid'][str(temp_f)][str(conc)]
            rho_lb = rhotab['grid'][str(temp_f)][str(conc)]
            if mu is None or rho_lb is None:
                out[temp_c][conc] = None
                continue
            nu = (mu * LBFTFT_TO_PAS) / (rho_lb * LBFT3_TO_KGM3) * 1.0e6
            out[temp_c][conc] = round(nu, 2)
    return out


def rebuild_json(pdf_path, json_path, out_path):
    ref = extract_reference(pdf_path)
    db = json.load(open(json_path, encoding='utf-8'))

    metric = {}
    for glycol, props in GLYCOL_TABLES.items():
        metric[glycol] = {}
        for prop, ref_key in props.items():
            if prop == 'kinematic_viscosity_mm2_s':
                metric[glycol][prop] = viscosity_to_metric(ref['grids'][ref_key],
                                                           ref['grids'][GLYCOL_TABLES[glycol]['density_kg_m3']])
            else:
                metric[glycol][prop] = grid_to_metric(ref['grids'][ref_key], prop)

    for glycol, props in GLYCOL_TABLES.items():
        temps = sorted(metric[glycol]['density_kg_m3'].keys())
        concs = ref['grids'][props['density_kg_m3']]['concs']
        for prop in props:
            rows = []
            for temp_c in temps:
                rows.append({'temp_c': temp_c,
                             'values': [metric[glycol][prop][temp_c][c] for c in concs]})
            db[glycol][prop]['concentration_vol_pct'] = concs
            db[glycol][prop]['data'] = rows

    for glycol in ('ethylene_glycol', 'propylene_glycol'):
        fb = ref['freezing'][glycol]['rows']
        data = []
        for r in fb:
            if r['freezing_below_minus60F']:
                continue  # точки ниже -60 F: ASHRAE помечает '*', база их не хранит
            data.append({
                'mass_pct': r['mass_pct'],
                'vol_pct': r['vol_pct'],
                'freezing_C': f_to_c(r['freezing_F']),
                'boiling_C': f_to_c(r['boiling_F']),
            })
        db[glycol]['freezing_boiling_points']['data'] = data

    meta = db.get('meta', {})
    if meta.get('version') != '3.0':
        meta['version'] = '3.0'
        meta['date'] = '2026-09-20'
    notes = [n for n in meta.get('notes', []) if 'Rebuilt' not in n]
    notes.append('Rebuilt 2026-09-20 cell-by-cell from AshraeFundamentals.pdf '
                 '(Tables 4-13) by scripts/verify-glycol-ashrae.py: fixes column '
                 'shifts in specific heat/thermal conductivity, raw lb/(ft*h) '
                 'stored as kinematic viscosity, and corrupted cold-row densities '
                 'found by the ASHRAE verification (roadmap 2.2).')
    meta['notes'] = notes
    conv = meta.get('conversions', {})
    # v2-рудимент, утверждавший «вязкость уже в cSt» (та самая единичная
    # схема, которая оказалась багом), заменяется фактической формулой.
    conv.pop('viscosity', None)
    conv['kinematic_viscosity'] = 'mm2/s = (lb/(ft*h) x 0.000413379 Pa s) / (lb/ft3 x 16.018463 kg/m3) x 1e6'
    meta['conversions'] = conv
    db['meta'] = meta

    with open(out_path, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(db, f, ensure_ascii=False, indent=2)
        f.write('\n')


def check_json(pdf_path, json_path):
    ref = extract_reference(pdf_path)
    db = json.load(open(json_path, encoding='utf-8'))
    problems = []
    matched = 0
    for glycol, props in GLYCOL_TABLES.items():
        for prop, ref_key in props.items():
            vtab = ref['grids'][ref_key]
            if prop == 'kinematic_viscosity_mm2_s':
                conv = viscosity_to_metric(vtab, ref['grids'][GLYCOL_TABLES[glycol]['density_kg_m3']])
            else:
                conv = grid_to_metric(vtab, prop)
            rows = {r['temp_c']: r['values'] for r in db[glycol][prop]['data']}
            concs = db[glycol][prop]['concentration_vol_pct']
            # Обратное покрытие: в базе не должно быть узлов вне сетки PDF
            ref_temps = {f_to_c(t) for t in vtab['temps_F']}
            if concs != vtab['concs']:
                problems.append(f'{glycol}/{prop}: сетка концентраций {concs} != ASHRAE {vtab["concs"]}')
            for temp_c in rows:
                if temp_c not in ref_temps:
                    problems.append(f'{glycol}/{prop}: в базе температура {temp_c}, отсутствующая в ASHRAE')
            for temp_c, cells in conv.items():
                if temp_c not in rows:
                    problems.append(f'{glycol}/{prop}: нет строки t={temp_c} (есть в ASHRAE)')
                    continue
                for i, conc in enumerate(vtab['concs']):
                    if conc not in concs:
                        problems.append(f'{glycol}/{prop}: нет концентрации {conc}')
                        continue
                    expected = cells[conc]
                    actual = rows[temp_c][concs.index(conc)]
                    if expected is None and actual is None:
                        continue
                    if expected is None and actual is not None:
                        problems.append(f'{glycol}/{prop} t={temp_c} c={conc}: в базе {actual}, в ASHRAE пусто')
                        continue
                    if expected is not None and actual is None:
                        problems.append(f'{glycol}/{prop} t={temp_c} c={conc}: в базе None, в ASHRAE {expected}')
                        continue
                    if prop == 'kinematic_viscosity_mm2_s':
                        rel = abs(actual - expected) / max(abs(expected), 1e-9)
                        if rel > TOL_REL_VISC:
                            problems.append(f'{glycol}/{prop} t={temp_c} c={conc}: база {actual}, ASHRAE {expected} ({rel:.1%})')
                        else:
                            matched += 1
                    else:
                        if abs(actual - expected) > TOL_ABS[prop]:
                            problems.append(f'{glycol}/{prop} t={temp_c} c={conc}: база {actual}, ASHRAE {expected}')
                        else:
                            matched += 1
    for glycol in ('ethylene_glycol', 'propylene_glycol'):
        fb = {round(r['mass_pct'], 1): r for r in ref['freezing'][glycol]['rows']
              if not r['freezing_below_minus60F']}
        for r in db[glycol]['freezing_boiling_points']['data']:
            mass = round(r['mass_pct'], 1)
            src = fb.get(mass)
            if src is None:
                problems.append(f'{glycol}/freezing: строка mass={mass} отсутствует в ASHRAE')
                continue
            for key, src_key in (('freezing_C', 'freezing_F'), ('boiling_C', 'boiling_F')):
                expected = f_to_c(src[src_key])
                if abs(r[key] - expected) > TOL_FREEZING:
                    problems.append(f'{glycol}/freezing mass={mass}: {key}={r[key]}, ASHRAE {expected}')

    print(f'Ячеек сошлось: {matched}; расхождений: {len(problems)}')
    for p in problems[:60]:
        print(' -', p)
    if len(problems) > 60:
        print(f' ... и ещё {len(problems) - 60}')
    return 0 if not problems else 1


def main():
    ap = argparse.ArgumentParser(description='ASHRAE <-> glycol_data.json gate')
    ap.add_argument('mode', choices=['extract', 'check', 'rebuild'])
    ap.add_argument('--pdf', required=True, help='путь к AshraeFundamentals.pdf')
    ap.add_argument('--json', default='data/glycol_data.json')
    ap.add_argument('--out', help='куда записать результат (extract/rebuild)')
    args = ap.parse_args()

    if args.mode == 'extract':
        ref = extract_reference(args.pdf)
        out = args.out or os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ashrae_ref.json')
        with open(out, 'w', encoding='utf-8') as f:
            json.dump(ref, f, ensure_ascii=False, indent=1)
        print('извлечено ->', out)
    elif args.mode == 'check':
        sys.exit(check_json(args.pdf, args.json))
    elif args.mode == 'rebuild':
        rebuild_json(args.pdf, args.json, args.out or args.json)
        print('база пересобрана ->', args.out or args.json)
        print('прогоните: python scripts/verify-glycol-ashrae.py check --pdf ... для acceptance-гейта')


if __name__ == '__main__':
    main()
