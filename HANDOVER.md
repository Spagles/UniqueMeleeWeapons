# HANDOVER — L10nProbe integration (structural closure of l10n gap detection)

Working doc for a future session. Delete when the integration lands.
Prerequisite: the probe mod at `../L10nProbe` is implemented per its `SPEC.md`
(being built in its own session first).

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
hediffs, FactionDefs). A *new class* of externally-sourced field — new
vanilla parent, new comp with C#-default strings, a future mod using other
APIs — is invisible until someone reruns the in-game report. The probe mod
closes this: it calls the game's own walker
(`Verse.DefInjectionUtility.ForEachPossibleDefInjection`, decompile findings
recorded in `../L10nProbe/SPEC.md`) at boot, filtered to our packageIds, and
dumps the complete expected key set + English text as JSON. Expected keys are
language-independent, so no language switching, no vanilla noise, no UI.

## Integration steps (this repo first, then propagate)

1. **Sidecar**: run the probe (game launch with `-l10nprobe`), take its UMW
   output, and check it in as `Scripts/expected-injections.json` (stable
   sorted; carries game build + active DLC set in `meta`). Verify it is a
   superset of the current checker's expected set: every key in
   `EXTERNAL_INJECTIONS`, every `label`/`description` the checker derives
   from def XML, and every key currently translated in the 8 language
   folders. Investigate any discrepancy before proceeding — a *missing*
   expected key means a probe filter bug; an *extra* key means the manifest
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
4. **Release skill**: this repo has none — port it from
   `../UniqueWeaponsUnbound/.claude/skills/release/` /
   `../PersonaWeaponsUnbound/.claude/skills/release/` (tracked in TODOs.md),
   then add the step: refresh expectations → if sidecar diff shows new keys,
   translate them (`/translate update`) → run checker `--strict` → proceed.
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
