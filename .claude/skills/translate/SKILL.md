---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla RimWorld terminology. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Unique Melee Weapons. English is
the source of truth; every other language derives from it.

## Non-negotiables

- **Run the checker first and last.** `python3 Scripts/check-translations.py`
  validates key sets, placeholders, DefInjected paths, staleness, and file
  hygiene deterministically. Never hand-derive anything it reports; never
  finish with it failing.
- **Community translations are owned by their contributors.** Update
  stale/missing keys in an existing language when asked, but do not rewrite a
  contributor's phrasing wholesale without the user's explicit direction.
- **Machine-assisted output is a first pass.** PRs and commits containing
  generated translations must say so and invite native-speaker review.
- **Keep the public roster current.** CONTRIBUTING.md's localization table
  (Planned / Machine-assisted / Native, plus credit) must be updated in the
  same commit whenever a language is added or a native review lands. The
  target roster lives there — consult it before proposing new languages.

## File map and conventions

- English Keyed source: `1.6/Languages/English/Keyed/UMW_UI.xml` (settings
  window) and `UMW_Stats.xml` (info-card trait-effect lines).
- Most player-facing text lives in the defs themselves
  (`1.6/Defs/**`) — weapon/trait/hediff/thought/ability labels and
  descriptions, quest letter text, name-grammar rulesStrings — and is
  translated per language via DefInjected, not Keyed.
- Target layout: `1.6/Languages/<Language>/Keyed/*.xml` and
  `1.6/Languages/<Language>/DefInjected/<DefTypeFolder>/*.xml`
- `<DefTypeFolder>` must be the def's resolvable type name: bare for vanilla
  types (`ThingDef`, `WeaponTraitDef`, `HediffDef`, `AbilityDef`,
  `QuestScriptDef`, ...). This mod currently defines no Def subclasses of its
  own (audited 2026-07); if one is ever added, its folder must be
  **namespace-qualified** (`UniqueMeleeWeapons.<DefClass>`) — a bare custom
  name silently drops every translation in the folder.
- DefInjected keys are `DefName.field` paths (`UMW_LongSword_Unique.label`,
  `UMW_Earthshake.description`). Translate `label`, `description`, and the
  long tail of secondary fields this mod actually uses: `traitAdjectives`
  (all WeaponTraitDefs) and `namerLabels` (all weapon ThingDefs' comps),
  which feed generated unique names; hediff `labelNoun`, injury-comp labels
  (`labelTendedWell`, `permanentLabel`, `destroyedLabel`, ...) and stage
  labels; thought `stages` labels/descriptions; DamageDef `deathMessage`;
  FactionDef `pawnSingular`/`pawnsPlural`/`leaderTitle`; and quest
  `rulesStrings` grammar. The checker warns on uncovered label/description;
  the rest it validates structurally once present.
- The name-generation grammar (`RulePackDefs/`, and the `stuff_adjective`
  symbol it consumes) is translatable content, not fixed data: its
  rulesStrings carry English adjectives/nouns that each language rewrites to
  produce natural names in that language.
- **EN comment convention (required):** every translated entry carries the
  current English source directly above it:
  `<!-- EN: Reset to defaults -->` — this is how the checker detects
  staleness.
- Formatting: UTF-8 without BOM, LF endings, 2-space indent, final newline,
  root element `<LanguageData>`.
- Placeholders (`{0}`, `{1}`, named args) must match English exactly per key.
  Translator comments above placeholdered English keys explain what gets
  injected — injected values are lowercase def labels; phrase around them
  accordingly.

## Terminology grounding (do not skip)

Every game term must match the official localization, not a plausible
translation. Sources, in order:

1. Vanilla language data:
   `"$RIMWORLD_PATH"/Data/<Expansion>/Languages/<Language> (<Native>).tar`
   (read entries with `tar -xOf`). Check Core plus Odyssey (this mod's DLC),
   and Royalty (the `MayRequire`-gated ultratech traits borrow its melee
   kit).
2. This file's glossary below (lessons already learned — apply them).
3. If a term appears nowhere official, flag it in the PR for native review
   rather than inventing silently.

Terms that MUST be grounded before use: weapon trait, unique weapon, the
base melee weapon names we mirror (longsword, spear, mace, knife, and the
Royalty pair), quality tiers, material/stuff names (wood, plasteel, uranium,
jade, ...), Royalty's ultratech melee weapons for the ultratech trait
descriptions (English labels are fused lowercase words: "monosword",
"plasmasword", "zeushammer" — ground each language's forms from the Royalty
tar), damage/condition terms (EMP,
stun, burn, bleeding), and the opportunity-site quest vocabulary
(ancient mercenaries, bandit camp, item stash).

### Glossary — carried over from Unique Weapons Unbound

These rows were learned in the companion mod (UWU) from native review (RU)
and vanilla-data study (JP); they apply verbatim here. Add rows whenever a
native review lands corrections.

#### Russian (from UWU PR #6 native review)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | свойство | черта | vanilla `WeaponTraits`=Свойства; черта = pawn personality traits |
| charge (weapons) | энерг- root | заряд- | vanilla `Gun_ChargeRifle`=энерговинтовка; заряд reads as ammo |
| Cancel (button) | Отменить | Отмена | vanilla `Cancel`; buttons use infinitive verbs |
| report/inspect strings | noun phrases | finite verbs | matches inspect-pane convention |

#### Japanese (from UWU machine-assisted generation, 2026-07)

Style rules discovered from the vanilla JP data (mandatory):

- Vanilla JP uses ASCII punctuation: `,` and `.` — never `、` or `。`.
- Descriptions/tooltips: polite です/ます form ending `.`; labels/buttons no
  period.
- Quote injected def labels and cross-referenced UI labels with 「」.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 | — | vanilla `WeaponTraits`=特性 |
| unique weapon | ユニークな武器 | | vanilla `UniqueWeapon` |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| Cancel / Reset | キャンセル / リセット | | vanilla Keyed buttons |

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}») — injected labels never inflect, and quoting sidesteps case and
  agreement problems.
- When an English string is reworded, refresh the EN comments in every
  language **in the same commit** — the checker reports the mismatch as STALE
  either way, but batching avoids churn.
- Coined vanilla terms may be a portmanteau in one language and a plain word
  in another — always check, never extrapolate between languages.

## Workflows

### Initial generation (`/translate <Language>`)

1. Run the checker; confirm English itself is clean.
2. Enumerate English Keyed keys and DefInjected-translatable def fields
   (mirror the structure of an existing language if one exists).
3. Extract the vanilla tar for the target language into the scratchpad;
   build a term list for the grounded terms above.
4. Translate via subagent(s) carrying: the glossary, the vanilla term list,
   the EN-comment requirement, placeholder rules, and formatting rules.
   Chunk by file section if the key count is large.
5. Run the checker (`--strict` for new languages); fix everything.
6. Review the diff yourself before committing. Commit message and PR text
   must state machine-assisted origin and invite native review.

### Update pass (`/translate update`)

1. Run the checker; it lists missing keys and stale entries per language.
2. Translate only that delta, refreshing each entry's EN comment.
3. Leave correct existing entries untouched. Re-run the checker.

### Audit only (`/translate check`)

Run the checker and report; change nothing.

## Optional in-game verification

RimWorld Dev Mode offers "Save translation report" and "clean up translation
files" (Verse.LanguageReportGenerator / TranslationFilesCleaner). These need a
running game with the mod loaded — useful as a final QA pass, not a substitute
for the checker.
