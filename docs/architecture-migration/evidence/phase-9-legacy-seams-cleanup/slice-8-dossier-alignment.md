# Slice 8 — Dossier alignment (Phase 9)

Класс: architecture artifacts. Дата: 2026-09-03.

## Изменения досье

1. **`maps/architecture-model.json`** (hash ниже):
   - `INV-008`: status `unverified` → **`verified`**, `target_status` →
     `implemented`, evidence `EV-P9-SLICE-5`, `EV-P9-SLICE-6` (+`EV-CT`, `EV-DR`).
   - `ST-026`/`ST-027`: `current`-состояния → `covered` (Results-owned строки
     из канонических снапшотов; builder на снапшотах; selection Results-owned),
     evidence `EV-P9-SLICE-3`, `EV-P9-SLICE-4`.
   - Добавлены записи evidence `EV-P9-SLICE-2..7` (paths → slice receipts).
   - `INV-016` не флипается глобально: закрыт только Results-clause
     (mutation-boundary части остаются открытыми) — честная заметка в
     `target-invariants.md`.
2. **Карты — dated overlay "Phase 9 Legacy-Seams-Cleanup Overlay"**:
   `state-ownership.md`, `state-inventory.md`, `target-invariants.md`,
   `compile-time.md`, `di-runtime.md`, `user-flow.md`,
   `characterization-tests.md`. `reactive.md` не менялся: `INV-010` в Phase 9
   не трогался.
3. **`architecture-widget.html`** — перегенерирован детерминированно из
   обновлённой модели (см. generation-hash-receipt).
4. **`widget/verify-widget.mjs` — НЕ изменялся** (owner gate): exemplar
   `INV-008` в синтетических runtime-сценариях (lines 33-34) теперь ссылается
   на verified-инвариант. Обе suite PASS и так (refs задаются синтетически),
   но по прецеденту Phase 7.5 exemplar должен указывать на реально открытый
   инвариант (`INV-010`). Re-point записан как **PENDING owner authorization**
   (OWNER_DECISION_REQUIRED по плану slice 8).
5. **`TASK_CONTEXT.md`** — датированная запись о выполнении Phase 9 (после
   этого receipt).

## Верификация

- `verify-widget.mjs --suite model-v2` — **PASS** (33 assertions / 21 mutations).
- `verify-widget.mjs --suite runtime-v2` — **PASS** (47 assertions / 20 mutations).
- `generate-widget.mjs --check` — **PASS 14/14**, два build'а байт-идентичны.
- `git diff --check` — чисто.
- Content-review: каждое утверждение overlay'ев ссылается на slice receipts
  1-7; claims: INV-008 verified; ST-026/027 covered; INV-016 Results-clause
  closed (не глобально); INV-006/007 — прогресс без глобального закрытия;
  INV-010 — не закрыт; Markdown/export — вне объёма.

## Hashes (после slice 8)

- model: `fddf315226eb07da7a980ffdc2823e33e06746f583ad88223b8d4400c5529c34`
- widget: `c2a74404e1ba35a03f6c7fe91fe23098d657ea5add1b891c51e441b05eb4fd97`
- verifier: `c9ea25d6b2c7190f1b067033c38a3aa36e05610c72c0279ec6ea9de771d6d6c6` (без изменений)

## Статус

SLICE 8: PASS (verifier exemplar re-point — PENDING owner authorization,
блокирующим не является; обе suite проходят).
