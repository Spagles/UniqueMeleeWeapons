# HANDOVER — L10nProbe integration (structural closure of l10n gap detection)

Working doc for a future session. Delete when the integration lands.
Prerequisite met 2026-07-31: the probe mod at `../L10nProbe` is implemented,
verified against all four of its SPEC acceptance criteria, and this machine is
already configured (probe deployed and active last in the mod list, its
settings pre-seeded with the family's three packageIds). See "Probe
implementation facts" below for what the implementation nailed down.

## Where this comes from (2026-07-30 session)

A pre-release in-game translation report (Russian smoke-test) found 44
DefInjected entries missing per language across all 8 shipped languages.
Fixed in `abd0a9b` (this repo), ported to the siblings in UWU `48856ce` and
PWU `e48ee1e`. Root causes and the current defense are documented in
CLAUDE.md's Localization section, `.claude/skills/translate/SKILL.md`
("Some translatable fields never appear in this repo's XML"), and the header
of `Scripts/check-translations.py`.

**Residual blind spot:** the checker's `EXTERNAL_INJECTIONS` manifest is
static and its guards only recognize the four def shapes seen so far (unique
weapons by `thingSetMakerTags` tag, `abilityProps` traits, `InjuryBase`
hediffs, FactionDefs). A _new class_ of externally-sourced field — new
vanilla parent, new comp with C#-default strings, a future mod using other
APIs — is invisible until someone reruns the in-game report. The probe mod
closes this: it calls the game's own walker
(`Verse.DefInjectionUtility.ForEachPossibleDefInjection`, decompile findings
recorded in `../L10nProbe/SPEC.md`) at boot, filtered to our packageIds, and
dumps the complete expected key set + English text as JSON. Expected keys are
language-independent, so no language switching, no vanilla noise, no UI.

## Probe implementation facts (2026-07-31, verified against live runs)

Schema details the consumer must handle (authoritative: `../L10nProbe/SPEC.md` §3):

- **Every entry is a _legal_ injection point** (translationAllowed, non-generated
  def, non-empty English). The game's must-translate verdict is a per-entry
  flag, not a cut: `"required": true` == the in-game report would list it as
  missing. The completeness set to demand in all languages is the **required**
  subset; non-required entries (single-word fields without `[MustTranslate]`
  like the vanilla tend labels, `[MayTranslate]` fields, keyword lists) exist
  so translations that target them can still be staleness-checked against
  English. UMW's dump: 267 entries, 223 required; all 38 manifest keys are
  present with `required: true`.
- **Keys are `suggestedPath`** (handle form: `comps.HediffComp_GetsPermanent.
  permanentLabel`, `stages.dosed.label`). Entries carry `"normalized"` (index
  form: `comps.2.permanentLabel`) only when it differs. UMW's language folders
  currently use the index form — match legacy keys via the alias, or migrate
  the folders to handle form during integration (the game accepts both, dedups
  by normalizedPath).
- **Collections** are one entry at the collection's path carrying **all**
  elements (`"english": [...]`, `"isCollection": true`, `"fullListAllowed"`
  when `[TranslationCanChangeCount]`). Full-list `<li>` translations must
  match element count unless fullListAllowed.
- **Def types** are keyed by the DefInjected folder name the game's loader
  resolves: short names for UMW; custom-namespace def types are full names
  (UWU's `UniqueWeaponsUnbound.TraitCostRuleDef` — matches its folders).
- **meta.activeDlcs** is `ExpansionDef.defName` in database order; on this
  machine `["Core", "Royalty", "Ideology", "Biotech", "Odyssey"]` (Anomaly
  installed but inactive). `meta.gameBuild` e.g. `"1.6.4871 rev591"`.
- Output is UTF-8 (no BOM), LF, two-space indent, ordinally sorted, verified
  byte-identical across runs modulo `meta.generated`.

Operational contract for the refresh script:

- Run: `cd "$RIMWORLD_PATH" && ./RimWorldWin64.exe -l10nprobe` — blocks until
  the game exits (~1.5 min boot on this machine; the probe itself reports
  ~seconds in its timing line). **Exit code is 0 even when probing fails** (the
  game shuts down normally either way) — check the log and files, not the code.
- Success signals: `[L10nProbe] probe (-l10nprobe): 3/3 dump(s) written in
  N.Ns.` in Player.log, and one JSON per packageId in
  `$RIMWORLD_PATH/Mods/L10nProbe/Output/`. On per-mod failure the probe logs
  `[L10nProbe] FAILED probing <packageId>: ...` and guarantees **no output
  file** at that mod's path (a pre-existing one is deleted) — absence of the
  file IS the failure marker; never trust a leftover.
- UNC write-back (probe writing straight into WSL repos via a settings
  override) is implemented but untested — start by fetching from the default
  `Output/` folder; flip to overrides later if wanted (set in the probe's
  settings window).

## Integration steps (this repo first, then propagate)

1. **Sidecar**: run the probe (game launch with `-l10nprobe`), take its UMW
   output, and check it in as `Scripts/expected-injections.json` (stable
   sorted; carries game build + active DLC set in `meta`). Verify it is a
   superset of the current checker's expected set: every key in
   `EXTERNAL_INJECTIONS`, every `label`/`description` the checker derives
   from def XML, and every key currently translated in the 8 language
   folders. Investigate any discrepancy before proceeding — a _missing_
   expected key means a probe filter bug; an _extra_ key means the manifest
   era genuinely under-covered and the new keys need translating (use
   `/translate` with the grounding map in SKILL.md; glossaries + vanilla-tar
   verbatim-copy workflow are all documented there).
2. **Checker consumes the sidecar** (`Scripts/check-translations.py`):
   - `expected_injections()` reads the sidecar when present and uses it as
     the expected set (union with XML-derived label/description as a sanity
     cross-check, or replace outright — decide when implementing; CI keeps
     working because the sidecar is checked in).
   - Add the exact freshness rule that replaces the heuristic guards: **every
     defName in this repo's `Defs/` must appear in the sidecar**, else error
     "expectations stale — rerun the probe". This is what makes unknown gap
     classes impossible by structure: any new def forces a regen, and the
     regen sees everything the game sees.
   - Also fail when `meta.activeDlcs` lacks Royalty (MayRequire-gated defs
     would silently vanish from the expected set).
   - Retire `EXTERNAL_INJECTIONS` and `check_manifest_guards()` once the
     sidecar is authoritative (keep `PARITY_EXEMPT_FIELDS`; the sidecar's
     `fullListAllowed` flag can eventually subsume it).
3. **Regeneration script** `Scripts/refresh-translation-expectations.py`:
   launches the game with `-l10nprobe` (WSL → Windows exe via
   `"$RIMWORLD_PATH/RimWorldWin64.exe"`; game boot is graphical and takes
   ~1-2 min — acceptable, release flow already assumes the local client),
   waits for the output, rewrites the sidecar, prints the diff summary.
   If the probe's UNC write-back to WSL paths proved unreliable (see SPEC
   caveat), fetch from the probe's default output folder instead.
4. **Release skill**: ported from UWU/PWU and now at
   `.claude/skills/release/SKILL.md`. Rework its step 3 into: refresh
   expectations → if sidecar diff shows new keys, translate them
   (`/translate update`) → run checker `--strict` → proceed, replacing the
   in-game-report backstop bullet that step currently carries.
   Keep every step deterministic-first: the only model judgment in the loop
   is translating newly discovered strings, so the flow stays runnable by
   mid-tier agents (Opus) without Fable.
5. **Propagate** the checker changes + sidecar + refresh script + release-
   skill step to UWU and PWU (their checkers are near-identical ports —
   per-repo constants at the top of the script; keep them structurally
   identical, as the 2026-07-30 port did). Their sidecars should confirm
   their manifests were correctly empty.
6. Update docs in lockstep: CLAUDE.md Localization paragraph (manifest →
   sidecar + probe), SKILL.md externally-sourced-fields bullet (same), the
   CI step comment in `.github/workflows/release.yml` (the in-game report is
   no longer the backstop; the probe is). Mirror doc updates to the siblings
   per the cross-repo rule. Delete this file.

## Verification cheatsheet

- `python3 Scripts/check-translations.py --strict` → 0/0 before and after.
- The 2026-07-30 ground truth for cross-checking step 1 lives in `abd0a9b`
  (the manifest) — the original `TranslationReport.txt` was discarded, but
  the manifest is its verified transcription.
- A deliberate test: add a scratch `WeaponTraitDef` with `<abilityProps>`
  (or any def) without regenerating → checker must fail with the staleness
  error; regenerate → it must demand the new keys in all 8 languages.

## Follow-up

One nuance worth keeping in mind even after integration: a RimWorld update
can change vanilla-inherited values or C# defaults without any of our defNames
changing, which the freshness rule alone wouldn't notice. It's covered as long
as regeneration is part of every release run (it is, in the handover design) —
and the sidecar's meta.gameBuild gives the refresh script an explicit trigger
to compare against the installed version, which is worth wiring in when you do
the integration. Until then, the in-game report remains the documented backstop,
exactly as the CI comments and SKILL.md state.
