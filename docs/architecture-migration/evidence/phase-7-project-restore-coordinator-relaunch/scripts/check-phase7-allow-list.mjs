import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import path from 'node:path';

const args = process.argv.slice(2);
const value = (name) => {
  const index = args.indexOf(name);
  if (index < 0 || !args[index + 1]) throw new Error(`Missing ${name}`);
  return args[index + 1];
};
if (args.length !== 6 || !args.includes('--baseline') || !args.includes('--allow-list') || !args.includes('--pre-existing')) {
  throw new Error('Expected exactly --baseline <path> --allow-list <path> --pre-existing <path>');
}

const root = execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
const normalize = (p) => p.replaceAll('\\', '/').replace(/^\.\//, '');
const relative = (p) => normalize(path.relative(root, p));
const baselinePath = value('--baseline');
const allowPath = value('--allow-list');
const preExistingPath = value('--pre-existing');
const allowBytes = readFileSync(allowPath);
const allow = JSON.parse(allowBytes);
const preExisting = JSON.parse(readFileSync(preExistingPath, 'utf8'));
const recordedAllowHash = readFileSync(path.join(root, 'docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/baseline/allow-list-sha256.txt'), 'utf8').trim().toUpperCase();
const actualAllowHash = createHash('sha256').update(allowBytes).digest('hex').toUpperCase();

const status = execFileSync('git', ['status', '--porcelain=v1', '--untracked-files=all'], { encoding: 'utf8' });
const ignored = execFileSync('git', ['status', '--ignored', '--porcelain=v1'], { encoding: 'utf8' });
const parse = (text) => text.split(/\r?\n/).filter(Boolean).map((line) => ({ code: line.slice(0, 2), path: normalize(line.slice(3)) }));
const current = [...parse(status), ...parse(ignored)];
const currentByPath = new Map(current.map((entry) => [entry.path, entry]));
const baselineEntries = Array.isArray(preExisting.paths) ? preExisting.paths : [];
const protectedSet = new Set((allow.protectedPaths ?? []).map(normalize));
const allowed = (allow.allowedChangedPaths ?? []).map(normalize);
const ignoredAllowed = (allow.allowedIgnoredPaths ?? []).map((p) => normalize(p).replace(/\/$/, ''));
const under = (p, prefix) => p === prefix || p.startsWith(`${prefix}/`);
const isIgnoredAllowed = (p) => ignoredAllowed.some((prefix) => under(p, prefix));
const isAllowed = (p) => allowed.some((prefix) => under(p, prefix));
const baselineSet = new Set(baselineEntries.map((entry) => normalize(entry.path)));
const forbidden = current.filter(({ path: p }) => !isAllowed(p) && !isIgnoredAllowed(p) && !baselineSet.has(p));
const unexpectedDeletions = current.filter(({ code, path: p }) => code.includes('D') && !baselineSet.has(p) && !isAllowed(p));
const untrackedForbidden = current.filter(({ code, path: p }) => code.includes('?') && !isAllowed(p) && !isIgnoredAllowed(p) && !baselineSet.has(p));
const ignoredForbidden = parse(ignored).filter(({ code, path: p }) => code === '!!' && !isIgnoredAllowed(p) && !isAllowed(p) && !baselineSet.has(p));
const baselineChanged = baselineEntries.filter(({ path: p, status: expected }) => {
  const actual = currentByPath.get(normalize(p));
  if (!actual) return true;
  return actual.code !== expected;
});
const hashViolations = baselineEntries.filter(({ path: p, sha256 }) => {
  const absolute = path.join(root, normalize(p));
  if (!sha256 || !existsSync(absolute)) return Boolean(sha256);
  const actual = createHash('sha256').update(readFileSync(absolute)).digest('hex').toUpperCase();
  return actual !== String(sha256).toUpperCase();
});
const protectedViolations = [...protectedSet].filter((p) => {
  const baseline = baselineEntries.find((entry) => normalize(entry.path) === p);
  if (!baseline) return true;
  const actual = currentByPath.get(p);
  if (!actual || actual.code !== baseline.status) return true;
  if (!baseline.sha256) return false;
  const absolute = path.join(root, p);
  if (!existsSync(absolute)) return true;
  const actualHash = createHash('sha256').update(readFileSync(absolute)).digest('hex').toUpperCase();
  return actualHash !== String(baseline.sha256).toUpperCase();
});
const overlap = [...new Set((allow.protectedPaths ?? []).map(normalize))].filter((p) => isAllowed(p));
const counters = {
  FORBIDDEN_PATHS: forbidden.length,
  UNEXPECTED_DELETIONS: unexpectedDeletions.length,
  ALLOW_LIST_SHA_MISMATCH: actualAllowHash === recordedAllowHash && overlap.length === 0 ? 0 : 1,
  UNTRACKED_FORBIDDEN_PATHS: untrackedForbidden.length,
  IGNORED_FORBIDDEN_PATHS: ignoredForbidden.length,
  BASELINE_PATH_CHANGED: baselineChanged.length + hashViolations.length,
  PROTECTED_PATH_VIOLATIONS: protectedViolations.length,
};
for (const [name, count] of Object.entries(counters)) console.log(`${name}=${count}`);
console.log(`BASELINE=${relative(path.resolve(baselinePath))}`);
console.log(`ALLOW_LIST=${relative(path.resolve(allowPath))}`);
console.log(`PRE_EXISTING=${relative(path.resolve(preExistingPath))}`);
console.log(`ALLOW_LIST_SHA256=${actualAllowHash}`);
console.log(`PROTECTED_OVERLAP=${overlap.join(',')}`);
if (Object.values(counters).some((count) => count !== 0)) process.exitCode = 1;
