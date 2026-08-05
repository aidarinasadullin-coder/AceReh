import { createHash, randomUUID } from "node:crypto";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { readFile, rename, rm, stat, writeFile } from "node:fs/promises";
import {
  MODES,
  VIEWS,
  counts,
  createState,
  diff,
  query,
  setDiffPair,
  setMode,
  setViews
} from "./architecture-widget.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "..");
const templatePath = join(here, "architecture-widget.template.html");
const cssPath = join(here, "architecture-widget.css");
const modelPath = join(root, "maps", "architecture-model.json");
const schemaPath = join(root, "maps", "architecture-model.widget.schema.json");
const outputPath = join(root, "architecture-widget.html");

const ACCEPTED_MODE_ORDER = Object.freeze(["current", "target", "diff"]);
const DIFF_DIRECTIONS = Object.freeze(["added", "removed", "changed", "unchanged", "unresolved"]);

const presentationScript = String.raw`(() => {
  const app = document.getElementById("app");
  const payloadNode = document.getElementById("architecture-explorer-payload");
  const fields = {
    modelId: document.querySelector('[data-field="model-id"]'),
    contractVersion: document.querySelector('[data-field="contract-version"]'),
    snapshotSha: document.querySelector('[data-field="snapshot-sha"]'),
    sourceBasis: document.querySelector('[data-field="source-basis"]'),
    currentVisible: document.querySelector('[data-field="current-visible"]'),
    currentPopulation: document.querySelector('[data-field="current-population"]'),
    targetVisible: document.querySelector('[data-field="target-visible"]'),
    targetClassification: document.querySelector('[data-field="target-classification"]'),
    diffVisible: document.querySelector('[data-field="diff-visible"]'),
    diffInvariantCount: document.querySelector('[data-field="diff-invariant-count"]'),
    statusText: document.querySelector('[data-field="status-text"]'),
    selectionSummary: document.querySelector('[data-field="selection-summary"]'),
    stateKind: document.querySelector('[data-field="state-kind"]'),
    stateTitle: document.querySelector('[data-field="state-title"]'),
    stateMessage: document.querySelector('[data-field="state-message"]'),
    stateDetail: document.querySelector('[data-field="state-detail"]'),
    resultCount: document.querySelector('[data-field="result-count"]'),
    resultPopulation: document.querySelector('[data-field="result-population"]'),
    diffDescription: document.querySelector('[data-diff-description]')
  };
  const modeList = document.querySelector('[data-mode-list]');
  const viewList = document.querySelector('[data-view-list]');
  const legendList = document.querySelector('[data-diff-legend]');
  const resultRows = document.querySelector('[data-result-rows]');
  const stateCard = document.querySelector('[data-state-card]');
  const screenButtons = [...document.querySelectorAll('[data-screen-target]')];
  const screens = [...document.querySelectorAll('[data-screen]')];
  const projectSessionRoot = document.querySelector('[data-project-session-root]');
  const migrationRoot = document.querySelector('[data-migration-root]');

  const renderError = (message, detail) => {
    if (!(app instanceof HTMLElement)) {
      return;
    }

    const section = document.createElement("section");
    section.className = "panel startup-error";
    section.setAttribute("aria-live", "assertive");
      section.innerHTML = '<div class="panel-header"><h2>Ошибка запуска</h2><span class="status-pill">Payload недоступен</span></div>' +
      '<p>' + message + '</p>' +
      '<pre class="error-detail">' + detail + '</pre>';
    app.prepend(section);
    app.focus();
  };

  const text = payloadNode?.textContent ?? "";
  if (text.length === 0) {
     renderError("Встроенный payload модели отсутствует.", "Ожидался один JSON payload внутри #architecture-explorer-payload.");
    return;
  }

  let payload;
  try {
    payload = JSON.parse(text);
  } catch (error) {
     renderError("Встроенный payload модели содержит недопустимый JSON.", error instanceof Error ? error.message : String(error));
    return;
  }

  if (!payload || typeof payload !== "object" || typeof payload.contract_version !== "string" || !payload.metadata || typeof payload.metadata.model_id !== "string" || !payload.presentation || !payload.presentation.overview || !payload.presentation.controls) {
     renderError("Встроенный payload модели не соответствует ожидаемой структуре обзора задачи 2.", "Отсутствуют metadata, contract_version или предвычисленные проекции представления.");
    return;
  }

  const overview = payload.presentation.overview;
  const controls = payload.presentation.controls;
  const projections = payload.presentation.projections;
  const projectSession = payload.presentation.project_session;
  const migration = payload.presentation.migration;

  if (fields.modelId) fields.modelId.textContent = payload.metadata.model_id;
  if (fields.contractVersion) fields.contractVersion.textContent = payload.contract_version;
  if (fields.snapshotSha) fields.snapshotSha.textContent = payload.metadata.snapshot_sha ?? "Unavailable";
  if (fields.sourceBasis) fields.sourceBasis.textContent = payload.metadata.source_basis ?? "Unavailable";
  if (fields.currentVisible) fields.currentVisible.textContent = String(overview.current.visible);
  if (fields.currentPopulation) fields.currentPopulation.textContent = String(overview.current.population);
  if (fields.targetVisible) fields.targetVisible.textContent = String(overview.target.visible);
  if (fields.targetClassification) fields.targetClassification.textContent = overview.target.classification_label;
  if (fields.diffVisible) fields.diffVisible.textContent = String(overview.diff.visible);
  if (fields.diffInvariantCount) fields.diffInvariantCount.textContent = String(overview.diff.invariant_count);
  if (fields.statusText) fields.statusText.textContent = overview.status_text;

  const state = {
    mode: controls.default_mode,
    views: [...controls.default_views],
    legendDirection: "added"
  };

  const selectionKey = () => controls.selection_key_separator ? state.views.join(controls.selection_key_separator) : state.views.join("|");
  const readProjection = () => projections[state.mode]?.[selectionKey()] ?? projections.invalid_selection;

  const renderModes = () => {
    if (!(modeList instanceof HTMLElement)) return;
    modeList.replaceChildren();
    for (const option of controls.modes) {
      const wrapper = document.createElement("div");
      wrapper.className = state.mode === option.id ? "segmented-option is-active" : "segmented-option";
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = option.label;
      button.dataset.mode = option.id;
      button.setAttribute("aria-pressed", state.mode === option.id ? "true" : "false");
      button.addEventListener("click", () => {
        state.mode = option.id;
        render();
      });
      wrapper.append(button);
      modeList.append(wrapper);
    }
  };

  const renderViews = () => {
    if (!(viewList instanceof HTMLElement)) return;
    viewList.replaceChildren();
    for (const option of controls.views) {
      const wrapper = document.createElement("div");
      wrapper.className = state.views.includes(option.id) ? "filter-option is-active" : "filter-option";
      wrapper.dataset.view = option.id;
      const button = document.createElement("button");
      button.type = "button";
      button.setAttribute("aria-pressed", state.views.includes(option.id) ? "true" : "false");
      button.textContent = option.label;
      button.addEventListener("click", () => {
        const next = state.views.includes(option.id)
          ? state.views.filter((value) => value !== option.id)
          : [...state.views, option.id].sort((left, right) => controls.view_index[left] - controls.view_index[right]);
        state.views = next;
        render();
      });
      const count = document.createElement("span");
      count.className = "filter-option-count";
      count.textContent = option.count_label;
      wrapper.append(button, count);
      viewList.append(wrapper);
    }
  };

  const renderLegend = (legend) => {
    if (!(legendList instanceof HTMLElement)) return;
    const selected = legend.find((item) => item.id === state.legendDirection) ?? legend[0];
    state.legendDirection = selected?.id ?? "added";
    legendList.replaceChildren();
    for (const item of legend) {
      const article = document.createElement("article");
      const active = item.id === state.legendDirection;
      article.className = "legend-card legend-card-" + item.id + (item.count === 0 ? " is-zero" : "") + (active ? " is-active" : "");
      const button = document.createElement("button");
      button.type = "button";
      button.setAttribute("aria-pressed", active ? "true" : "false");
      button.setAttribute("aria-label", item.label + ": " + item.count);
      const label = document.createElement("span");
      label.className = "legend-card-label";
      label.textContent = item.label + " ";
      const key = document.createElement("span");
      key.className = "legend-card-key";
      key.textContent = "(" + item.id + ")";
      label.append(key);
      const count = document.createElement("strong");
      count.className = "legend-card-count";
      count.textContent = String(item.count);
      button.append(label, count);
      button.addEventListener("click", () => {
        state.legendDirection = item.id;
        renderLegend(legend);
      });
      article.append(button);
      legendList.append(article);
    }
    if (fields.diffDescription) fields.diffDescription.textContent = selected?.description ?? "";
  };

  const renderRows = (projection) => {
    if (!(resultRows instanceof HTMLElement)) return;
    resultRows.replaceChildren();
    if (projection.rows.length === 0) {
      const row = document.createElement("tr");
      const cell = document.createElement("td");
      cell.colSpan = 7;
      cell.className = "empty-row";
      cell.textContent = projection.state.message;
      row.append(cell);
      resultRows.append(row);
      return;
    }

    for (const item of projection.rows) {
      const row = document.createElement("tr");

      const idCell = document.createElement("td");
      idCell.dataset.label = "ID";
      idCell.innerHTML = '<span class="record-code">' + item.id + '</span>';

      const kindCell = document.createElement("td");
      kindCell.dataset.label = "Тип";
      kindCell.textContent = item.record_kind;

      const statusCell = document.createElement("td");
      statusCell.dataset.label = "Статус";
      statusCell.innerHTML = '<div class="record-summary"><span class="record-direction record-direction-' + item.direction + '">' + item.direction_label + '</span><span class="record-meta">' + item.status_line + '</span></div>';

      const viewsCell = document.createElement("td");
      viewsCell.dataset.label = "Представления";
      const viewsList = document.createElement("div");
      viewsList.className = "record-view-list";
      for (const view of item.views) {
        const chip = document.createElement("span");
        chip.className = "record-chip";
        chip.textContent = view;
        viewsList.append(chip);
      }
      viewsCell.append(viewsList);

      const summaryCell = document.createElement("td");
      summaryCell.dataset.label = "Сводка";
      summaryCell.innerHTML = '<div class="record-summary"><strong>' + item.summary_primary + '</strong><span class="record-meta">' + item.summary_secondary + '</span></div>';

      const changedCell = document.createElement("td");
      changedCell.dataset.label = "Изменения";
      if (item.changed_fields.length === 0) {
        const none = document.createElement("span");
        none.className = "record-changed-none";
        none.textContent = item.changed_fields_label;
        changedCell.append(none);
      } else {
        const changedList = document.createElement("div");
        changedList.className = "record-changed-list";
        for (const field of item.changed_fields) {
          const chip = document.createElement("span");
          chip.className = "record-changed-field";
          chip.textContent = field;
          changedList.append(chip);
        }
        changedCell.append(changedList);
      }

      const markersCell = document.createElement("td");
      markersCell.dataset.label = "Инварианты";
      const label = document.createElement("span");
      label.className = "record-marker-label";
      label.textContent = item.markers_label;
      const markerList = document.createElement("div");
      markerList.className = "record-marker-list";
      for (const marker of item.markers) {
        const chip = document.createElement("span");
        chip.className = 'record-marker ' + marker.class_name;
        chip.textContent = marker.label;
        markerList.append(chip);
      }
      markersCell.append(label, markerList);

      row.append(idCell, kindCell, statusCell, viewsCell, summaryCell, changedCell, markersCell);
      resultRows.append(row);
    }
  };

  const appendText = (parent, tag, className, value) => {
    const node = document.createElement(tag);
    if (className) node.className = className;
    node.textContent = value;
    parent.append(node);
    return node;
  };

  const renderReferences = (parent, references, evidenceMissing) => {
    const list = document.createElement("div");
    list.className = "reference-list";
    if (evidenceMissing) appendText(list, "p", "reference-empty", "Доказательства не зафиксированы");
    for (const reference of references) {
      const item = document.createElement("article");
      item.className = "reference-item";
      appendText(item, "span", "record-code", reference.id);
      appendText(item, "strong", "", reference.kind_label);
      appendText(item, "p", "record-meta", reference.summary);
      if (reference.detail) appendText(item, "p", "reference-detail", reference.detail);
      list.append(item);
    }
    parent.append(list);
  };

  const renderRecord = (record) => {
    const details = document.createElement("details");
    details.className = "ownership-record";
    const summary = document.createElement("summary");
    appendText(summary, "span", "record-code", record.id);
    appendText(summary, "strong", "", record.name);
    const badges = document.createElement("span");
    badges.className = "record-badges";
    appendText(badges, "span", "inline-badge", record.status + " / " + record.confidence);
    if (record.migration_status && record.coverage_status) appendText(badges, "span", "inline-badge", record.migration_status + " / " + record.coverage_status);
    if (typeof record.position === "number") appendText(badges, "span", "inline-badge", "Позиция " + record.position);
    summary.append(badges);
    details.append(summary);
    if (record.current_owner && record.target_owner) {
      const owners = document.createElement("dl");
      owners.className = "owner-pair";
      const current = document.createElement("div");
      appendText(current, "dt", "", "Текущий владелец");
      appendText(current, "dd", "", record.current_owner);
      const target = document.createElement("div");
      appendText(target, "dt", "", "Целевой владелец (намерение)");
      appendText(target, "dd", "", record.target_owner);
      owners.append(current, target);
      details.append(owners);
    }
    if (record.views?.length) appendText(details, "p", "record-meta", "Представления: " + record.views.join(", "));
    renderReferences(details, record.references, record.evidence_missing);
    return details;
  };

  const renderRecordGroup = (title, copy, records, className) => {
    const article = document.createElement("article");
    article.className = className;
    appendText(article, "h3", "", title);
    if (copy) appendText(article, "p", "panel-copy", copy);
    const list = document.createElement("div");
    list.className = "ownership-list";
    for (const record of records) list.append(renderRecord(record));
    article.append(list);
    return article;
  };

  const renderCollection = (parent, title, entries) => {
    const details = document.createElement("details");
    details.className = "reference-catalog";
    const summary = document.createElement("summary");
    summary.textContent = title + " (" + entries.length + ")";
    details.append(summary);
    renderReferences(details, entries, false);
    parent.append(details);
  };

  const renderMigrationRecord = (record, options = {}) => {
    const details = document.createElement("details");
    details.className = "migration-record";
    const summary = document.createElement("summary");
    appendText(summary, "span", "record-code", record.id);
    appendText(summary, "strong", "", record.name);
    const badges = document.createElement("span");
    badges.className = "record-badges";
    appendText(badges, "span", "inline-badge", record.record_kind);
    appendText(badges, "span", "inline-badge", record.direction);
    if (record.views?.length) appendText(badges, "span", "inline-badge", record.views.join(", "));
    summary.append(badges);
    details.append(summary);

    if (record.changed_fields?.length) appendText(details, "p", "record-meta", "Changed fields: " + record.changed_fields.join(", "));
    if (record.before && record.after) {
      const owners = document.createElement("dl");
      owners.className = "owner-pair migration-owner-pair";
      const before = document.createElement("div");
      appendText(before, "dt", "", "До");
      appendText(before, "dd", "", "current_owner: " + (record.before.current_owner ?? "null"));
      appendText(before, "dd", "", "target_owner: " + (record.before.target_owner ?? "null"));
      const after = document.createElement("div");
      appendText(after, "dt", "", "После");
      appendText(after, "dd", "", "current_owner: " + (record.after.current_owner ?? "null"));
      appendText(after, "dd", "", "target_owner: " + (record.after.target_owner ?? "null"));
      owners.append(before, after);
      details.append(owners);
    }
    if (options.copy) appendText(details, "p", "record-meta", options.copy);
    return details;
  };

  const renderHonestGroup = (title, copy, entries, className, emptyCopy) => {
    const article = document.createElement("article");
    article.className = "migration-group " + className;
    const header = document.createElement("div");
    header.className = "migration-group-header";
    const titleWrap = document.createElement("div");
    appendText(titleWrap, "h3", "", title);
    if (copy) appendText(titleWrap, "p", "panel-copy", copy);
    appendText(header, "span", "status-pill status-pill-muted", String(entries.length));
    header.prepend(titleWrap);
    article.append(header);
    const list = document.createElement("div");
    list.className = "migration-list";
    if (entries.length === 0) appendText(list, "p", "reference-empty", emptyCopy);
    for (const record of entries) list.append(renderMigrationRecord(record));
    article.append(list);
    return article;
  };

  const renderMigrationInvariant = (invariant) => {
    const details = document.createElement("details");
    details.className = "migration-record migration-invariant";
    const summary = document.createElement("summary");
    appendText(summary, "span", "record-code", invariant.id);
    appendText(summary, "strong", "", invariant.summary);
    const badges = document.createElement("span");
    badges.className = "record-badges";
    appendText(badges, "span", "inline-badge", invariant.status + " / target: " + invariant.target_status);
    if (invariant.views?.length) appendText(badges, "span", "inline-badge", invariant.views.join(", "));
    summary.append(badges);
    details.append(summary);
    renderReferences(details, invariant.references, invariant.references.length === 0);
    return details;
  };

  const renderMigrationDecision = (decision) => {
    const details = document.createElement("details");
    details.className = "migration-record migration-decision";
    const summary = document.createElement("summary");
    appendText(summary, "span", "record-code", decision.id);
    appendText(summary, "strong", "", decision.summary);
    const badges = document.createElement("span");
    badges.className = "record-badges";
    appendText(badges, "span", "inline-badge", decision.classification);
    appendText(badges, "span", "inline-badge", decision.status);
    summary.append(badges);
    details.append(summary);
    appendText(details, "p", "record-meta", "Owner: " + decision.owner + "; blocker: " + decision.blocker);
    if (decision.detail) appendText(details, "p", "record-meta", decision.detail);
    return details;
  };

  const renderMigrationFlows = (groups) => {
    const grid = document.createElement("div");
    grid.className = "migration-flow-grid";
    for (const group of groups) grid.append(renderRecordGroup(group.name, group.positions_label, group.records, "flow-card migration-flow-card"));
    return grid;
  };

  const renderMigration = () => {
    if (!(migrationRoot instanceof HTMLElement) || !migration) return;
    migrationRoot.replaceChildren();

    const banner = document.createElement("header");
    banner.className = "migration-banner";
    const bannerCopy = document.createElement("div");
    appendText(bannerCopy, "p", "eyebrow", "Migration boundary");
    appendText(bannerCopy, "h2", "", migration.explorer_boundary);
    appendText(bannerCopy, "p", "panel-copy", "Этот экран отображает уже принятый presentation.migration payload. Браузер не читает payload.model, не пересчитывает diff и не выводит новые семантические факты.");
    const diffPair = document.createElement("span");
    diffPair.className = "status-pill project-session-status";
    diffPair.textContent = "diff_pair: " + migration.diff_pair.left + " -> " + migration.diff_pair.right;
    banner.append(bannerCopy, diffPair);
    migrationRoot.append(banner);

    const recommendation = document.createElement("section");
    recommendation.className = "migration-section migration-recommendation";
    appendText(recommendation, "h2", "", "Рекомендация и безопасный следующий refactor");
    appendText(recommendation, "p", "panel-copy", "Основание: " + migration.recommendation_basis.limitation.id + " - " + migration.recommendation_basis.limitation.summary);
    appendText(recommendation, "p", "migration-next-step", "После отдельного Phase 1 plan/approval создать ProjectSession shell и один узкий state contract; затем выполнить только один vertical slice.");
    appendText(recommendation, "p", "record-meta", "Blocking decisions: " + (migration.recommendation_basis.blocking_decision_ids.length ? migration.recommendation_basis.blocking_decision_ids.join(", ") : "нет"));
    migrationRoot.append(recommendation);

    const diffGroups = document.createElement("section");
    diffGroups.className = "migration-section";
    appendText(diffGroups, "h2", "", "Принятые изменения Diff");
    appendText(diffGroups, "p", "panel-copy", "Группы ниже используют только accepted IDs и direction из payload.presentation.migration. Пустые группы остаются видимыми.");
    const diffGrid = document.createElement("div");
    diffGrid.className = "migration-group-grid";
    diffGrid.append(
      renderHonestGroup("Additions", "Записи, присутствующие в Target и отсутствующие в Current.", migration.additions, "migration-added", "Принятых additions нет."),
      renderHonestGroup("Removals", "Записи, присутствующие в Current и отсутствующие в Target.", migration.removals, "migration-removed", "Принятых removals нет."),
      renderHonestGroup("Ownership moves", "State records с изменением current_owner или target_owner.", migration.ownership_moves, "migration-changed", "Принятых ownership moves нет.")
    );
    diffGroups.append(diffGrid);
    migrationRoot.append(diffGroups);

    const dependencies = document.createElement("section");
    dependencies.className = "migration-section";
    appendText(dependencies, "h2", "", "Dependencies");
    appendText(dependencies, "p", "panel-copy", "Edge rows сгруппированы по accepted direction без recompute в браузере.");
    const dependencyGrid = document.createElement("div");
    dependencyGrid.className = "migration-group-grid";
    dependencyGrid.append(
      renderHonestGroup("Dependency additions", "Новые target edges.", migration.dependencies.additions, "migration-added", "Принятых dependency additions нет."),
      renderHonestGroup("Dependency removals", "Удаляемые current edges.", migration.dependencies.removals, "migration-removed", "Принятых dependency removals нет."),
      renderHonestGroup("Dependency changes", "Stable edge IDs с changed_fields.", migration.dependencies.changes, "migration-changed", "Принятых dependency changes нет.")
    );
    dependencies.append(dependencyGrid);
    migrationRoot.append(dependencies);

    const invariants = document.createElement("section");
    invariants.className = "migration-section";
    appendText(invariants, "h2", "", "Protected invariants");
    appendText(invariants, "p", "panel-copy", "Все принятые инварианты остаются видимыми как защита будущей миграции.");
    const invariantList = document.createElement("div");
    invariantList.className = "migration-list";
    if (migration.protected_invariants.length === 0) appendText(invariantList, "p", "reference-empty", "Protected invariants не зафиксированы.");
    for (const invariant of migration.protected_invariants) invariantList.append(renderMigrationInvariant(invariant));
    invariants.append(invariantList);
    migrationRoot.append(invariants);

    const decisions = document.createElement("section");
    decisions.className = "migration-section";
    appendText(decisions, "h2", "", "Deferred decisions");
    appendText(decisions, "p", "panel-copy", "Отложенные решения не скрываются и не превращаются в synthetic implementation facts.");
    const decisionList = document.createElement("div");
    decisionList.className = "migration-list";
    if (migration.deferred_decisions.length === 0) appendText(decisionList, "p", "reference-empty", "Deferred decisions не зафиксированы.");
    for (const decision of migration.deferred_decisions) decisionList.append(renderMigrationDecision(decision));
    decisions.append(decisionList);
    migrationRoot.append(decisions);

    const flows = document.createElement("section");
    flows.className = "migration-section";
    appendText(flows, "h2", "", "Protected 8 flow groups");
    appendText(flows, "p", "panel-copy", "Ровно восемь групп пользовательских потоков защищают будущую Phase 1 миграцию от потери сценариев.");
    flows.append(renderMigrationFlows(migration.protected_flows));
    migrationRoot.append(flows);
  };

  const renderProjectSession = () => {
    if (!(projectSessionRoot instanceof HTMLElement) || !projectSession) return;
    projectSessionRoot.replaceChildren();
    const banner = document.createElement("header");
    banner.className = "project-session-banner";
    const bannerCopy = document.createElement("div");
    appendText(bannerCopy, "p", "eyebrow", "Целевая архитектура");
    appendText(bannerCopy, "h2", "", "ProjectSession");
    appendText(bannerCopy, "p", "panel-copy", projectSession.status_copy);
    const status = document.createElement("span");
    status.className = "status-pill project-session-status";
    status.textContent = projectSession.status_label;
    banner.append(bannerCopy, status);
    projectSessionRoot.append(banner);

    projectSessionRoot.append(renderRecordGroup("Lifecycle", "Идентичность проекта, путь, режим отображения, dirty-state и restore guard.", projectSession.lifecycle, "project-section"));
    const slices = document.createElement("section");
    slices.className = "project-section";
    appendText(slices, "h2", "", "Срезы состояния");
    appendText(slices, "p", "panel-copy", "Четыре явных среза составного aggregate root; это целевые намерения, а не реализованный Target.");
    const sliceGrid = document.createElement("div");
    sliceGrid.className = "slice-grid";
    for (const slice of projectSession.slices) sliceGrid.append(renderRecordGroup(slice.name, "", slice.records, "slice-card"));
    slices.append(sliceGrid);
    projectSessionRoot.append(slices);

    const outside = renderRecordGroup("За пределами aggregate root", "CalculationContext seams, snapshot/file boundary и производная Results projection не превращаются в writable state ProjectSession.", projectSession.outside_root, "project-section outside-root");
    const boundaries = document.createElement("div");
    boundaries.className = "boundary-list";
    for (const invariant of projectSession.boundaries) {
      const item = document.createElement("details");
      item.className = "boundary-item";
      const summary = document.createElement("summary");
      appendText(summary, "span", "record-code", invariant.id);
      appendText(summary, "strong", "", invariant.statement);
      item.append(summary);
      appendText(item, "p", "record-meta", invariant.status + " / target: " + invariant.target_status + " / " + invariant.views.join(", "));
      renderReferences(item, invariant.references, invariant.evidence_missing);
      boundaries.append(item);
    }
    outside.append(boundaries);
    projectSessionRoot.append(outside);

    const flows = document.createElement("section");
    flows.className = "project-section";
    appendText(flows, "h2", "", "Ключевые пользовательские потоки");
    appendText(flows, "p", "panel-copy", "Ровно восемь групп над 22 принятыми flow-записями. Missing и partial остаются видимыми.");
    const flowGrid = document.createElement("div");
    flowGrid.className = "flow-grid";
    for (const group of projectSession.flow_groups) flowGrid.append(renderRecordGroup(group.name, group.positions_label, group.records, "flow-card"));
    flows.append(flowGrid);
    projectSessionRoot.append(flows);

    const catalog = document.createElement("section");
    catalog.className = "project-section reference-browser";
    appendText(catalog, "h2", "", "Доказательства и решения");
    appendText(catalog, "p", "panel-copy", "Полный browseable индекс принятых источников без добавления новых архитектурных фактов.");
    renderCollection(catalog, "Доказательства", projectSession.catalog.evidence);
    renderCollection(catalog, "Ограничения", projectSession.catalog.limitations);
    renderCollection(catalog, "Инварианты", projectSession.catalog.invariants);
    renderCollection(catalog, "Отложенные решения", projectSession.catalog.deferred_decisions);
    projectSessionRoot.append(catalog);
  };

  const activateScreen = (screenName) => {
    for (const screen of screens) screen.hidden = screen.dataset.screen !== screenName;
    for (const button of screenButtons) {
      const active = button.dataset.screenTarget === screenName;
      button.classList.toggle("is-active", active);
      if (active) button.setAttribute("aria-current", "page");
      else button.removeAttribute("aria-current");
    }
    const activeScreen = screens.find((screen) => screen.dataset.screen === screenName);
    if (activeScreen instanceof HTMLElement) activeScreen.focus({ preventScroll: true });
  };

  const render = () => {
    const projection = readProjection();
    renderModes();
    renderViews();
     renderLegend(projection.diff_legend);
    renderRows(projection);

    if (fields.selectionSummary) fields.selectionSummary.textContent = projection.selection_summary;
    if (fields.stateKind) fields.stateKind.textContent = projection.state.kind_label;
    if (fields.stateTitle) fields.stateTitle.textContent = projection.state.title;
    if (fields.stateMessage) fields.stateMessage.textContent = projection.state.message;
    if (fields.stateDetail) fields.stateDetail.textContent = projection.state.detail;
    if (fields.resultCount) fields.resultCount.textContent = String(projection.counts.visible);
    if (fields.resultPopulation) fields.resultPopulation.textContent = String(projection.counts.population);
    if (stateCard instanceof HTMLElement) stateCard.dataset.stateKind = projection.state.kind;
  };

  for (const button of screenButtons) button.addEventListener("click", () => activateScreen(button.dataset.screenTarget));
  renderProjectSession();
  renderMigration();
  render();
})();`;

const safeJson = (value) => JSON.stringify(value, null, 2)
  .replace(/</g, "\\u003C")
  .replace(/>/g, "\\u003E")
  .replace(/&/g, "\\u0026")
  .replace(/\u2028/g, "\\u2028")
  .replace(/\u2029/g, "\\u2029")
  .replace(/<\//g, "\\u003C/");

const escapeHtml = (value) => String(value)
  .replace(/&/g, "&amp;")
  .replace(/</g, "&lt;")
  .replace(/>/g, "&gt;")
  .replace(/\"/g, "&quot;")
  .replace(/'/g, "&#39;");

const deterministicTempPath = (destination) => join(dirname(destination), `${randomUUID()}.tmp`);
const selectionKey = (views) => views.join("|");
const sortedViews = (views) => [...views].sort((left, right) => VIEWS.indexOf(left) - VIEWS.indexOf(right));

const combinations = () => {
  const result = [];
  for (let mask = 1; mask < (1 << VIEWS.length); mask += 1) {
    const selected = VIEWS.filter((_, index) => (mask & (1 << index)) !== 0);
    result.push(sortedViews(selected));
  }
  return result;
};

const formatClassification = (value) => {
  switch (value) {
    case null:
      return "Строки доступны";
    case "valid-empty-target":
      return "Корректная пустая цель";
    case "no-match":
      return "Нет строк для этой выборки";
    case "empty-snapshot":
      return "Снимок пуст";
    case "empty-diff":
      return "Diff пуст";
    default:
      return value;
  }
};

const diffDirectionLabel = (direction) => {
  switch (direction) {
    case "added":
      return "Добавлено";
    case "removed":
      return "Удалено";
    case "changed":
      return "Изменено";
    case "unchanged":
      return "Без изменений";
    case "unresolved":
      return "Не определено";
    default:
      return direction;
  }
};

const diffDescription = (direction) => {
  switch (direction) {
    case "added":
      return "Запись присутствует только в целевом снимке Target.";
    case "removed":
      return "Запись со стабильным ID присутствует только в текущем снимке Current.";
    case "changed":
      return "Стабильный ID есть в обоих снимках, но runtime обнаружил изменения канонических полей.";
    case "unchanged":
      return "Стабильный ID есть в обоих снимках без изменений канонических полей.";
    case "unresolved":
      return "Runtime не смог однозначно определить каноническую эквивалентность записи.";
    default:
      return direction;
  }
};

const viewLabel = (view) => {
  switch (view) {
    case "compile-time":
      return "Компиляция";
    case "di-runtime":
      return "DI/runtime";
    case "state-ownership":
      return "Владение состоянием";
    case "reactive":
      return "Реактивность";
    case "persistence":
      return "Сохранение";
    case "user-flow":
      return "Пользовательский поток";
    default:
      return view;
  }
};

const modeLabel = (mode) => {
  switch (mode) {
    case "current":
      return "Текущее";
    case "target":
      return "Цель";
    case "diff":
      return "Текущее -> Цель";
    default:
      return mode;
  }
};

const stateKindLabel = (kind) => {
  switch (kind) {
    case "ready":
      return "Строки доступны";
    case "valid-empty":
      return "Корректная пустая цель";
    case "no-match":
      return "Нет совпадающих строк";
    case "invalid":
      return "Недопустимая выборка";
    default:
      return kind;
  }
};

const invariantMarker = (flag) => flag
  ? Object.freeze([{ label: "Метка инварианта", class_name: "record-marker-flag" }])
  : Object.freeze([{ label: "Нет метки инварианта", class_name: "record-marker-ok" }]);

const rowViews = (row) => row.before?.canonical.views ?? row.after?.canonical.views ?? row.canonical.views;
const rowStatus = (row) => row.before?.status ?? row.after?.status ?? row.status;
const rowConfidence = (row) => row.before?.confidence ?? row.after?.confidence ?? row.confidence;
const canonicalName = (canonical) => canonical?.name ?? canonical?.source ?? canonical?.target ?? canonical?.consumer ?? canonical?.producer ?? canonical?.owner ?? canonical?.kind ?? null;
const rowName = (row) => canonicalName(row.before?.canonical) ?? canonicalName(row.after?.canonical) ?? canonicalName(row.canonical) ?? row.id;

const summarizeRow = (row) => {
  const viewText = rowViews(row).join(", ");
  const primary = rowName(row);
  const secondary = `Представления: ${viewText}`;
  return Object.freeze({ primary, secondary });
};

const statusLine = (row) => {
  const status = rowStatus(row) ?? "unknown";
  const confidence = rowConfidence(row) ?? "unknown";
  return `${status} / ${confidence}`;
};

const projectionState = (mode, selection, countInfo) => {
  if (selection.length === 0) {
    return Object.freeze({
      kind: "invalid",
      kind_label: stateKindLabel("invalid"),
      title: "Недопустимая выборка представлений",
      message: "Выберите хотя бы одно из шести общих представлений модели.",
      detail: `Для режима «${modeLabel(mode)}» нужна непустая выборка представлений.`
    });
  }
  if (countInfo.classification === "valid-empty-target") {
    return Object.freeze({
      kind: "valid-empty",
      kind_label: stateKindLabel("valid-empty"),
      title: "Корректная пустая цель",
      message: "Цель намеренно не реализована в принятом runtime и поэтому отображается как управляемое пустое состояние.",
      detail: `Выбранные представления: ${selection.map(viewLabel).join(", ")}.`
    });
  }
  if (countInfo.classification === "no-match") {
    return Object.freeze({
      kind: "no-match",
      kind_label: stateKindLabel("no-match"),
      title: "Нет совпадающих строк",
      message: "Выбранные фильтры корректны, но текущая проекция runtime без поиска не содержит подходящих строк.",
      detail: `Выбранные представления: ${selection.map(viewLabel).join(", ")}.`
    });
  }
  return Object.freeze({
    kind: "ready",
    kind_label: stateKindLabel("ready"),
    title: `Проекция «${modeLabel(mode)}» готова`,
    message: `Видимо строк: ${countInfo.visible} из общего количества ${countInfo.population}.`,
    detail: `Выбранные представления: ${selection.map(viewLabel).join(", ")}.`
  });
};

const makeProjection = (baseState, mode, selectedViews) => {
  let modeState = setMode(baseState, mode);
  if (mode === "diff") {
    modeState = setDiffPair(modeState, { left: "current", right: "target" });
  }
  const filteredState = setViews(modeState, selectedViews);
  const countInfo = counts(filteredState);
  const legendDiffState = setViews(
    setMode(setDiffPair(baseState, { left: "current", right: "target" }), "diff"),
    selectedViews
  );
  const filteredDiffRows = diff(legendDiffState)
    .filter((row) => rowViews(row).some((view) => selectedViews.includes(view)));
  const rows = mode === "diff"
    ? filteredDiffRows.map((row) => {
      const summary = summarizeRow(row);
      return Object.freeze({
        id: row.id,
        record_kind: row.record_kind,
        direction: row.direction,
        direction_label: diffDirectionLabel(row.direction),
        status_line: statusLine(row),
        views: Object.freeze([...rowViews(row)]),
        summary_primary: summary.primary,
        summary_secondary: summary.secondary,
        changed_fields: Object.freeze([...row.changed_fields]),
        changed_fields_label: row.changed_fields.length === 0 ? "Нет изменённых полей" : `Изменённых полей: ${row.changed_fields.length}`,
        markers_label: row.invariant_violation ? "Метка инварианта" : "Статус инварианта",
        markers: invariantMarker(row.invariant_violation)
      });
    })
    : query(filteredState).map((row) => {
      const summary = summarizeRow(row);
      return Object.freeze({
        id: row.id,
        record_kind: row.record_kind,
        direction: mode === "target" ? "unresolved" : "unchanged",
        direction_label: mode === "target" ? "цель" : "текущее",
        status_line: `${row.status} / ${row.confidence}`,
        views: Object.freeze([...row.canonical.views]),
        summary_primary: summary.primary,
        summary_secondary: summary.secondary,
        changed_fields: Object.freeze([]),
        changed_fields_label: mode === "target" ? "Цель не реализована" : "Diff не активен",
        markers_label: row.invariant_refs.length === 0 ? "Статус инварианта" : "Ссылки на инварианты",
        markers: row.invariant_refs.length === 0 ? invariantMarker(false) : Object.freeze([{ label: `Ссылок на инварианты: ${row.invariant_refs.length}`, class_name: "record-marker-ok" }])
      });
    });

  return Object.freeze({
    mode,
    mode_label: modeLabel(mode),
    selection_key: selectionKey(selectedViews),
    selection_summary: `${modeLabel(mode)} - ${selectedViews.map(viewLabel).join(" + ")}`,
    counts: Object.freeze({
      visible: countInfo.visible,
      population: countInfo.population,
      classification: countInfo.classification,
      classification_label: formatClassification(countInfo.classification)
    }),
    state: projectionState(mode, selectedViews, countInfo),
    diff_legend: Object.freeze(DIFF_DIRECTIONS.map((direction) => Object.freeze({
      id: direction,
      label: diffDirectionLabel(direction),
      count: filteredDiffRows.filter((row) => row.direction === direction).length,
      description: diffDescription(direction)
    }))),
    rows: Object.freeze(rows)
  });
};

const invalidProjection = Object.freeze({
  mode: "invalid",
  mode_label: "Недопустимый",
  selection_key: "",
  selection_summary: "Не выбрана допустимая проекция режима и представлений",
  counts: Object.freeze({ visible: 0, population: 0, classification: "invalid", classification_label: "Недопустимая выборка" }),
  state: Object.freeze({
    kind: "invalid",
    kind_label: stateKindLabel("invalid"),
    title: "Недопустимая выборка",
    message: "Браузер запросил комбинацию режима и представлений, которая не была предвычислена генератором.",
    detail: "Вернитесь к одному из принятых режимов хотя бы с одним из шести принятых представлений."
  }),
  diff_legend: Object.freeze(DIFF_DIRECTIONS.map((direction) => Object.freeze({ id: direction, label: diffDirectionLabel(direction), count: 0, description: diffDescription(direction) }))),
  rows: Object.freeze([])
});

const controlledStateHooks = Object.freeze({
  valid_empty: Object.freeze({
    kind: "valid-empty",
    kind_label: stateKindLabel("valid-empty"),
    title: "Корректная пустая цель",
    message: "Цель намеренно не реализована в принятом runtime и остаётся видимо пустой, не означая ошибку запуска.",
    detail: "Этот hook существует, хотя браузер не пересчитывает семантику архитектуры."
  }),
  no_match: Object.freeze({
    kind: "no-match",
    kind_label: stateKindLabel("no-match"),
    title: "Нет совпадающих строк",
    message: "Управляемое состояние отсутствия совпадений доступно для будущих суженных выборок без пересчёта семантики в браузере.",
    detail: "Задача 2 не добавляет поиск, поэтому это объявленный hook, а не режим, вызываемый пользователем."
  }),
  invalid: invalidProjection.state
});

const PROJECT_SESSION_IDS = Object.freeze({
  lifecycle: Object.freeze(["ST-001", "ST-002", "ST-003", "ST-004", "ST-005"]),
  slices: Object.freeze([
    Object.freeze({ name: "ClimateState", ids: Object.freeze(["ST-006", "ST-007"]) }),
    Object.freeze({ name: "ConstructionState", ids: Object.freeze(["ST-008", "ST-009", "ST-010", "ST-011"]) }),
    Object.freeze({ name: "ThermalState", ids: Object.freeze(["ST-012", "ST-013", "ST-014", "ST-015"]) }),
    Object.freeze({ name: "HydraulicsState", ids: Object.freeze(["ST-016", "ST-017", "ST-018", "ST-019"]) })
  ]),
  outside_root: Object.freeze(["ST-020", "ST-021", "ST-022", "ST-023", "ST-024", "ST-025", "ST-026", "ST-027"]),
  boundaries: Object.freeze(["INV-001", "INV-006", "INV-007", "INV-008", "INV-009", "INV-013", "INV-014", "INV-015"]),
  flows: Object.freeze([
    Object.freeze({ name: "Новый проект", ids: Object.freeze(["CF-001"]) }),
    Object.freeze({ name: "Загрузка проекта", ids: Object.freeze(["CF-002", "CF-003"]) }),
    Object.freeze({ name: "Повторная загрузка", ids: Object.freeze(["CF-004"]) }),
    Object.freeze({ name: "Изменение входных данных", ids: Object.freeze(["CF-005", "CF-006", "CF-007", "CF-008", "CF-009"]) }),
    Object.freeze({ name: "Расчёт", ids: Object.freeze(["CF-010"]) }),
    Object.freeze({ name: "Сброс", ids: Object.freeze(["CF-011", "CF-012"]) }),
    Object.freeze({ name: "Сохранение и повторное открытие", ids: Object.freeze(["CF-013", "CF-014", "CF-020", "CF-021", "CF-022"]) }),
    Object.freeze({ name: "Экспорт", ids: Object.freeze(["CF-015", "CF-016", "CF-017", "CF-018", "CF-019"]) })
  ])
});

const collectionEntry = (kind, item) => {
  if (kind === "evidence") {
    return Object.freeze({ kind, kind_label: "Доказательство", id: item.id, summary: `${item.path} — ${item.locator}`, detail: `${item.confidence} / ${item.freshness}` });
  }
  if (kind === "limitations") {
    return Object.freeze({ kind, kind_label: "Ограничение", id: item.id, summary: item.statement, detail: item.status });
  }
  if (kind === "invariants") {
    return Object.freeze({ kind, kind_label: "Инвариант", id: item.id, summary: item.statement, detail: `${item.status} / target: ${item.target_status}` });
  }
  return Object.freeze({ kind, kind_label: "Отложенное решение", id: item.id, summary: item.decision, detail: `${item.classification} / ${item.status}; ${item.blocker}` });
};

const buildProjectSession = (baseState) => {
  const model = baseState.model;
  const collections = Object.freeze({
    evidence: Object.freeze(model.evidence.map((item) => collectionEntry("evidence", item))),
    limitations: Object.freeze(model.limitations.map((item) => collectionEntry("limitations", item))),
    invariants: Object.freeze(model.invariants.map((item) => collectionEntry("invariants", item))),
    deferred_decisions: Object.freeze(model.deferred_decisions.map((item) => collectionEntry("deferred_decisions", item)))
  });
  const referenceIndexes = Object.freeze(Object.fromEntries(Object.entries(collections).map(([kind, entries]) => [kind, new Map(entries.map((entry) => [entry.id, entry]))])));
  const resolveReferences = (state, ownerId) => {
    const references = [];
    const groups = [
      ["evidence", state.evidence_refs ?? []],
      ["limitations", state.limitation_refs ?? []],
      ["invariants", state.invariant_refs ?? []],
      ["deferred_decisions", state.decision_refs ?? []]
    ];
    for (const [kind, ids] of groups) {
      for (const id of ids) {
        const entry = referenceIndexes[kind].get(id);
        if (!entry) throw new Error(`project-session unresolved reference ${ownerId}:${id}`);
        references.push(entry);
      }
    }
    return Object.freeze(references);
  };
  const stateRecord = (id) => {
    const record = baseState.indexes.by_id[id];
    const state = record?.snapshot_states.current;
    if (!record || record.record_kind !== "state_record" || !state) throw new Error(`project-session missing current state record ${id}`);
    return Object.freeze({
      id,
      name: state.canonical.name,
      views: Object.freeze([...state.canonical.views]),
      current_owner: state.canonical.current_owner,
      target_owner: state.canonical.target_owner,
      migration_status: state.canonical.migration_status,
      coverage_status: state.canonical.coverage_status,
      status: state.status,
      confidence: state.confidence,
      evidence_refs: Object.freeze([...state.evidence_refs]),
      limitation_refs: Object.freeze([...state.limitation_refs]),
      invariant_refs: Object.freeze([...state.invariant_refs]),
      decision_refs: Object.freeze([...state.decision_refs]),
      evidence_missing: state.evidence_refs.length === 0,
      references: resolveReferences(state, id)
    });
  };
  const flowRecord = (id) => {
    const record = baseState.indexes.by_id[id];
    const state = record?.snapshot_states.current;
    if (!record || record.record_kind !== "flow" || !state) throw new Error(`project-session missing current flow ${id}`);
    return Object.freeze({
      id,
      name: state.canonical.name,
      position: state.canonical.position,
      views: Object.freeze([...state.canonical.views]),
      status: state.status,
      confidence: state.confidence,
      evidence_refs: Object.freeze([...state.evidence_refs]),
      limitation_refs: Object.freeze([...state.limitation_refs]),
      invariant_refs: Object.freeze([...state.invariant_refs]),
      decision_refs: Object.freeze([...state.decision_refs]),
      evidence_missing: state.evidence_refs.length === 0,
      references: resolveReferences(state, id)
    });
  };
  const expectedFlowIds = PROJECT_SESSION_IDS.flows.flatMap((group) => group.ids);
  const actualFlows = model.records
    .filter((record) => record.record_kind === "flow" && record.snapshot_states.current)
    .sort((left, right) => left.snapshot_states.current.canonical.position - right.snapshot_states.current.canonical.position);
  const actualFlowIds = actualFlows.map((record) => record.id);
  const uniqueExpected = new Set(expectedFlowIds);
  if (PROJECT_SESSION_IDS.flows.length !== 8 || expectedFlowIds.length !== 22 || uniqueExpected.size !== 22) throw new Error("project-session invalid flow partition");
  if (actualFlowIds.length !== 22 || actualFlowIds.some((id, index) => id !== `CF-${String(index + 1).padStart(3, "0")}`)) throw new Error("project-session unexpected current flow set or position order");
  if (actualFlowIds.some((id) => !uniqueExpected.has(id))) throw new Error("project-session flow partition is incomplete");
  const flowGroups = PROJECT_SESSION_IDS.flows.map((group) => {
    const records = group.ids.map(flowRecord).sort((left, right) => left.position - right.position);
    return Object.freeze({ name: group.name, positions_label: `Позиции: ${records.map((record) => record.position).join(", ")}`, records: Object.freeze(records) });
  });
  const flattenedGroups = flowGroups.flatMap((group) => group.records.map((record) => record.id));
  if (flattenedGroups.length !== 22 || new Set(flattenedGroups).size !== 22 || flattenedGroups.some((id) => !actualFlowIds.includes(id))) throw new Error("project-session flow assignment mismatch");

  const invariantById = new Map(model.invariants.map((item) => [item.id, item]));
  const boundaries = PROJECT_SESSION_IDS.boundaries.map((id) => {
    const invariant = invariantById.get(id);
    if (!invariant) throw new Error(`project-session missing boundary invariant ${id}`);
    const evidenceState = { evidence_refs: invariant.evidence, limitation_refs: [], invariant_refs: [], decision_refs: [] };
    return Object.freeze({ ...invariant, evidence_missing: invariant.evidence.length === 0, references: resolveReferences(evidenceState, id) });
  });
  const targetLimitation = model.limitations.find((item) => item.id === "LIM-003");
  if (!targetLimitation) throw new Error("project-session missing LIM-003 target limitation");

  return Object.freeze({
    status: "implemented",
    status_label: "Реализовано: lifecycle shell",
    status_copy: `${targetLimitation.statement} Реализованы только lifecycle values ProjectSession; целевые module slices ниже остаются принятыми намерениями, а не наблюдаемой реализацией.`,
    lifecycle: Object.freeze(PROJECT_SESSION_IDS.lifecycle.map(stateRecord)),
    slices: Object.freeze(PROJECT_SESSION_IDS.slices.map((slice) => Object.freeze({ name: slice.name, records: Object.freeze(slice.ids.map(stateRecord)) }))),
    outside_root: Object.freeze(PROJECT_SESSION_IDS.outside_root.map(stateRecord)),
    boundaries: Object.freeze(boundaries),
    flow_groups: Object.freeze(flowGroups),
    catalog: collections
  });
};

const migrationRecord = (row) => {
  const canonical = row.after?.canonical ?? row.before?.canonical;
  return Object.freeze({
    id: row.id,
    record_kind: row.record_kind,
    direction: row.direction,
    name: canonicalName(canonical) ?? row.id,
    views: Object.freeze([...(canonical?.views ?? [])]),
    changed_fields: Object.freeze([...row.changed_fields])
  });
};

const buildMigration = (baseState, diffRows, projectSession) => {
  const evidenceById = new Map(baseState.model.evidence.map((item) => [item.id, collectionEntry("evidence", item)]));
  const invariantEntry = (item) => Object.freeze({
    ...collectionEntry("invariants", item),
    views: Object.freeze([...item.views]),
    status: item.status,
    target_status: item.target_status,
    references: Object.freeze(item.evidence.map((id) => {
      const evidence = evidenceById.get(id);
      if (!evidence) throw new Error(`migration unresolved invariant evidence ${item.id}:${id}`);
      return evidence;
    }))
  });
  const decisionEntry = (item) => Object.freeze({
    ...collectionEntry("deferred_decisions", item),
    classification: item.classification,
    owner: item.owner,
    blocker: item.blocker,
    status: item.status
  });
  const ownershipMoves = diffRows.filter((row) => row.record_kind === "state_record" && row.direction === "changed" && row.changed_fields.some((field) => field === "current_owner" || field === "target_owner"));
  const dependencyRows = diffRows.filter((row) => row.record_kind === "edge");
  const limitation = baseState.model.limitations.find((item) => item.id === "LIM-003");
  if (!limitation) throw new Error("migration missing LIM-003 recommendation basis");

  return Object.freeze({
    diff_pair: Object.freeze({ left: "current", right: "target" }),
    additions: Object.freeze(diffRows.filter((row) => row.direction === "added").map(migrationRecord)),
    removals: Object.freeze(diffRows.filter((row) => row.direction === "removed").map(migrationRecord)),
    ownership_moves: Object.freeze(ownershipMoves.map((row) => Object.freeze({
      ...migrationRecord(row),
      before: Object.freeze({
        current_owner: row.before?.canonical.current_owner ?? null,
        target_owner: row.before?.canonical.target_owner ?? null
      }),
      after: Object.freeze({
        current_owner: row.after?.canonical.current_owner ?? null,
        target_owner: row.after?.canonical.target_owner ?? null
      })
    }))),
    dependencies: Object.freeze({
      additions: Object.freeze(dependencyRows.filter((row) => row.direction === "added").map(migrationRecord)),
      removals: Object.freeze(dependencyRows.filter((row) => row.direction === "removed").map(migrationRecord)),
      changes: Object.freeze(dependencyRows.filter((row) => row.direction === "changed").map(migrationRecord))
    }),
    protected_invariants: Object.freeze(baseState.model.invariants.map(invariantEntry)),
    deferred_decisions: Object.freeze(baseState.model.deferred_decisions.map(decisionEntry)),
    protected_flows: projectSession.flow_groups,
    recommendation_basis: Object.freeze({
      limitation: collectionEntry("limitations", limitation),
      blocking_decision_ids: Object.freeze(baseState.model.deferred_decisions.filter((item) => item.classification === "blocking-for-target").map((item) => item.id))
    }),
    explorer_boundary: "Explorer ничего не мигрирует"
  });
};

const buildPresentation = (baseState) => {
  const defaultViews = Object.freeze([...VIEWS]);
  const combos = combinations();
  const projectionsByMode = Object.fromEntries(ACCEPTED_MODE_ORDER.map((mode) => [mode, Object.create(null)]));
  for (const mode of ACCEPTED_MODE_ORDER) {
    for (const selectedViews of combos) {
      const projection = makeProjection(baseState, mode, selectedViews);
      projectionsByMode[mode][selectionKey(selectedViews)] = projection;
    }
  }

  const currentProjection = projectionsByMode.current[selectionKey(defaultViews)];
  const targetProjection = projectionsByMode.target[selectionKey(defaultViews)];
  const diffProjection = projectionsByMode.diff[selectionKey(defaultViews)];
  const baseDiffState = setMode(setDiffPair(baseState, { left: "current", right: "target" }), "diff");
  const diffRows = diff(baseDiffState);

  const viewCounts = Object.fromEntries(VIEWS.map((view) => {
    const projection = projectionsByMode.current[selectionKey([view])];
    return [view, projection.counts.visible];
  }));

  const projectSession = buildProjectSession(baseState);
  return Object.freeze({
    overview: Object.freeze({
      status_text: `Метаданные: ${baseState.model.metadata.status}; целевой снимок остаётся не реализован`,
      current: Object.freeze({
        visible: currentProjection.counts.visible,
        population: currentProjection.counts.population
      }),
      target: Object.freeze({
        visible: targetProjection.counts.visible,
        classification: targetProjection.counts.classification,
        classification_label: targetProjection.counts.classification_label
      }),
      diff: Object.freeze({
        visible: diffProjection.counts.visible,
        population: diffProjection.counts.population,
        invariant_count: diffRows.filter((row) => row.invariant_violation).length
      })
    }),
    controls: Object.freeze({
      default_mode: "current",
      default_views: defaultViews,
      selection_key_separator: "|",
      modes: Object.freeze(ACCEPTED_MODE_ORDER.map((mode) => Object.freeze({ id: mode, label: modeLabel(mode) }))),
      views: Object.freeze(VIEWS.map((view) => Object.freeze({
        id: view,
        label: viewLabel(view),
        count_label: `${viewCounts[view]} строк`
      }))),
      view_index: Object.freeze(Object.fromEntries(VIEWS.map((view, index) => [view, index])))
    }),
    project_session: projectSession,
    migration: buildMigration(baseState, diffRows, projectSession),
    projections: Object.freeze({
      current: Object.freeze(projectionsByMode.current),
      target: Object.freeze(projectionsByMode.target),
      diff: Object.freeze(projectionsByMode.diff),
      invalid_selection: invalidProjection,
      state_hooks: controlledStateHooks
    })
  });
};

const renderModeButtons = (controls, activeMode) => controls.modes
  .map((option) => `<div class="segmented-option${activeMode === option.id ? " is-active" : ""}"><button type="button" data-mode="${escapeHtml(option.id)}" aria-pressed="${activeMode === option.id ? "true" : "false"}">${escapeHtml(option.label)}</button></div>`)
  .join("");

const renderViewButtons = (controls, activeViews) => controls.views
  .map((option) => `<div class="filter-option${activeViews.includes(option.id) ? " is-active" : ""}" data-view="${escapeHtml(option.id)}"><button type="button" aria-pressed="${activeViews.includes(option.id) ? "true" : "false"}">${escapeHtml(option.label)}</button><span class="filter-option-count">${escapeHtml(option.count_label)}</span></div>`)
  .join("");

const renderLegend = (legend) => legend
  .map((item, index) => `<article class="legend-card legend-card-${escapeHtml(item.id)}${item.count === 0 ? " is-zero" : ""}${index === 0 ? " is-active" : ""}"><button type="button" aria-pressed="${index === 0 ? "true" : "false"}" aria-label="${escapeHtml(item.label)}: ${escapeHtml(item.count)}"><span class="legend-card-label">${escapeHtml(item.label)} <span class="legend-card-key">(${escapeHtml(item.id)})</span></span><strong class="legend-card-count">${escapeHtml(item.count)}</strong></button></article>`)
  .join("");

const renderRows = (projection) => {
  if (projection.rows.length === 0) {
    return `<tr><td colspan="7" class="empty-row">${escapeHtml(projection.state.message)}</td></tr>`;
  }
  return projection.rows.map((item) => {
    const viewsMarkup = item.views.map((view) => `<span class="record-chip">${escapeHtml(view)}</span>`).join("");
    const changedMarkup = item.changed_fields.length === 0
      ? `<span class="record-changed-none">${escapeHtml(item.changed_fields_label)}</span>`
      : `<div class="record-changed-list">${item.changed_fields.map((field) => `<span class="record-changed-field">${escapeHtml(field)}</span>`).join("")}</div>`;
    const markerMarkup = item.markers.map((marker) => `<span class="record-marker ${escapeHtml(marker.class_name)}">${escapeHtml(marker.label)}</span>`).join("");
    return `<tr><td data-label="ID"><span class="record-code">${escapeHtml(item.id)}</span></td><td data-label="Тип">${escapeHtml(item.record_kind)}</td><td data-label="Статус"><div class="record-summary"><span class="record-direction record-direction-${escapeHtml(item.direction)}">${escapeHtml(item.direction_label)}</span><span class="record-meta">${escapeHtml(item.status_line)}</span></div></td><td data-label="Представления"><div class="record-view-list">${viewsMarkup}</div></td><td data-label="Сводка"><div class="record-summary"><strong>${escapeHtml(item.summary_primary)}</strong><span class="record-meta">${escapeHtml(item.summary_secondary)}</span></div></td><td data-label="Изменения">${changedMarkup}</td><td data-label="Инварианты"><span class="record-marker-label">${escapeHtml(item.markers_label)}</span><div class="record-marker-list">${markerMarkup}</div></td></tr>`;
  }).join("");
};

export const buildHtml = async () => {
  const [template, css, modelRaw, schemaRaw] = await Promise.all([
    readFile(templatePath, "utf8"),
    readFile(cssPath, "utf8"),
    readFile(modelPath, "utf8"),
    readFile(schemaPath, "utf8")
  ]);
  const schema = JSON.parse(schemaRaw);
  const state = createState(modelRaw, schema);
  const payload = Object.freeze({
    metadata: Object.freeze({
      model_id: state.model.metadata.model_id,
      snapshot_sha: state.provenance.snapshot_sha,
      source_basis: state.provenance.source_basis,
      accepted_views: Object.freeze([...VIEWS]),
      accepted_modes: Object.freeze([...ACCEPTED_MODE_ORDER])
    }),
    contract_version: state.provenance.contract_version,
    model: state.model,
    presentation: buildPresentation(state)
  });
  const initialProjection = payload.presentation.projections.current[selectionKey(VIEWS)];
  const escapedPayload = safeJson(payload);
  return template
    .replace("__INLINE_CSS__", css.trimEnd())
    .replace("__EMBEDDED_PAYLOAD__", escapedPayload)
    .replace("__INITIAL_MODEL_ID__", escapeHtml(payload.metadata.model_id))
    .replace("__INITIAL_CONTRACT_VERSION__", escapeHtml(payload.contract_version))
    .replace("__INITIAL_SNAPSHOT_SHA__", escapeHtml(payload.metadata.snapshot_sha ?? "Unavailable"))
    .replace("__INITIAL_SOURCE_BASIS__", escapeHtml(payload.metadata.source_basis ?? "Unavailable"))
    .replace("__INITIAL_STATUS_TEXT__", escapeHtml(payload.presentation.overview.status_text))
    .replace("__INITIAL_CURRENT_VISIBLE__", escapeHtml(payload.presentation.overview.current.visible))
    .replace("__INITIAL_CURRENT_POPULATION__", escapeHtml(payload.presentation.overview.current.population))
    .replace("__INITIAL_TARGET_VISIBLE__", escapeHtml(payload.presentation.overview.target.visible))
    .replace("__INITIAL_TARGET_CLASSIFICATION__", escapeHtml(payload.presentation.overview.target.classification_label))
    .replace("__INITIAL_DIFF_VISIBLE__", escapeHtml(payload.presentation.overview.diff.visible))
    .replace("__INITIAL_DIFF_INVARIANT_COUNT__", escapeHtml(payload.presentation.overview.diff.invariant_count))
    .replace("__INITIAL_SELECTION_SUMMARY__", escapeHtml(initialProjection.selection_summary))
    .replace("__INITIAL_MODE_BUTTONS__", renderModeButtons(payload.presentation.controls, "current"))
    .replace("__INITIAL_VIEW_BUTTONS__", renderViewButtons(payload.presentation.controls, [...VIEWS]))
    .replace("__INITIAL_STATE_KIND__", escapeHtml(initialProjection.state.kind_label))
    .replace("__INITIAL_STATE_KIND_ATTR__", escapeHtml(initialProjection.state.kind))
    .replace("__INITIAL_STATE_TITLE__", escapeHtml(initialProjection.state.title))
    .replace("__INITIAL_STATE_MESSAGE__", escapeHtml(initialProjection.state.message))
    .replace("__INITIAL_STATE_DETAIL__", escapeHtml(initialProjection.state.detail))
    .replace("__INITIAL_DIFF_LEGEND__", renderLegend(initialProjection.diff_legend))
    .replace("__INITIAL_DIFF_DESCRIPTION__", escapeHtml(initialProjection.diff_legend[0].description))
    .replace("__INITIAL_RESULT_COUNT__", escapeHtml(initialProjection.counts.visible))
    .replace("__INITIAL_RESULT_POPULATION__", escapeHtml(initialProjection.counts.population))
    .replace("__INITIAL_RESULT_ROWS__", renderRows(initialProjection))
    .replace("__PRESENTATION_SCRIPT__", presentationScript);
};

const sha256 = (value) => createHash("sha256").update(value).digest("hex");
const payloadFromHtml = (html) => {
  const matches = [...html.matchAll(/<script id="architecture-explorer-payload" type="application\/json">([\s\S]*?)<\/script>/g)];
  if (matches.length !== 1) throw new Error(`embedded payload count must be 1, received ${matches.length}`);
  return JSON.parse(matches[0][1]);
};
const requireCheck = (name, condition) => {
  if (!condition) throw new Error(`check ${name} failed`);
  return name;
};
const allReferencesResolve = (payload) => {
  const catalog = payload.presentation.project_session.catalog;
  const known = new Set(Object.values(catalog).flat().map((entry) => entry.id));
  const recordReferences = [
    ...payload.presentation.project_session.lifecycle,
    ...payload.presentation.project_session.slices.flatMap((slice) => slice.records),
    ...payload.presentation.project_session.outside_root,
    ...payload.presentation.project_session.boundaries.flatMap((item) => [{ references: item.references }]),
    ...payload.presentation.project_session.flow_groups.flatMap((group) => group.records),
    ...payload.presentation.migration.protected_invariants
  ];
  return recordReferences.every((record) => (record.references ?? []).every((reference) => known.has(reference.id)));
};

const check = async () => {
  const canonicalBefore = await readFile(outputPath, "utf8");
  const canonicalHashBefore = sha256(canonicalBefore);
  const [firstBuild, secondBuild] = await Promise.all([buildHtml(), buildHtml()]);
  const payload = payloadFromHtml(firstBuild);
  const migration = payload.presentation?.migration;
  const directions = new Set(DIFF_DIRECTIONS);
  const diffRows = Object.values(payload.presentation.projections.diff).flatMap((projection) => projection.rows);
  const checks = Object.freeze([
    requireCheck("html-non-empty-utf8", canonicalBefore.length > 0 && !canonicalBefore.includes("\u0000")),
    requireCheck("one-embedded-payload", [...canonicalBefore.matchAll(/<script id="architecture-explorer-payload" type="application\/json">[\s\S]*?<\/script>/g)].length === 1),
    requireCheck("accepted-identity", payload.metadata?.model_id === payload.model?.metadata?.model_id && payload.contract_version === payload.model?.contract_version && payload.metadata?.source_basis === payload.model?.metadata?.source_basis),
    requireCheck("overview-projectsession-migration", canonicalBefore.includes("Обзор") && canonicalBefore.includes("ProjectSession") && canonicalBefore.includes("Миграция") && migration),
    requireCheck("current-target-diff", payload.metadata.accepted_modes.join("|") === ACCEPTED_MODE_ORDER.join("|")),
    requireCheck("six-accepted-views", payload.metadata.accepted_views.length === 6 && payload.metadata.accepted_views.join("|") === VIEWS.join("|")),
    requireCheck("diff-stable-id-and-direction", diffRows.every((row) => typeof row.id === "string" && row.id.length > 0 && directions.has(row.direction))),
    requireCheck("projectsession-lifecycle-implemented", payload.presentation.project_session.status === "implemented"),
    requireCheck("lifecycle-and-four-slices", payload.presentation.project_session.lifecycle.length > 0 && payload.presentation.project_session.slices.length === 4),
    requireCheck("eight-core-flow-groups", payload.presentation.project_session.flow_groups.length === 8),
    requireCheck("displayed-references-resolve", allReferencesResolve(payload)),
    requireCheck("no-external-network-runtime-dependencies", !/<script[^>]+\bsrc=|\b(?:fetch|XMLHttpRequest|WebSocket)\s*\(|\bhttps?:\/\//i.test(canonicalBefore)),
    requireCheck("two-in-memory-builds-byte-identical", firstBuild === secondBuild),
    requireCheck("check-does-not-change-canonical-html", canonicalHashBefore === sha256(await readFile(outputPath, "utf8")))
  ]);
  for (const name of checks) console.log(`PASS ${name}`);
  console.log(`PASS count ${checks.length}`);
  console.log(`canonical sha256 before ${canonicalHashBefore}`);
  console.log(`canonical sha256 after ${sha256(await readFile(outputPath, "utf8"))}`);
  console.log(`generated sha256 ${sha256(firstBuild)}`);
};

const generate = async () => {
  const tempPath = deterministicTempPath(outputPath);
  let wroteTemp = false;
  try {
    const html = await buildHtml();
    await writeFile(tempPath, html, "utf8");
    wroteTemp = true;
    await rename(tempPath, outputPath);
    const outputStat = await stat(outputPath);
    console.log(`generated ${outputPath}`);
    console.log(`bytes ${outputStat.size}`);
  } catch (error) {
    await rm(tempPath, { force: true });
    throw error;
  }
};

const arguments_ = process.argv.slice(2);
if (arguments_.length === 0) {
  await generate();
} else if (arguments_.length === 1 && arguments_[0] === "--check") {
  await check();
} else {
  throw new Error("usage: node generate-widget.mjs [--check]");
}
