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
- **The type folder is load-bearing, not organizational** (decompile-verified,
  `Verse.LoadedLanguage`): RimWorld enumerates only the top-level directories
  under `DefInjected/` and resolves each directory *name* to the def type its
  files target. An `.xml` placed directly in `DefInjected/` is never loaded,
  and the checker likewise iterates only directories — a misplaced file fails
  silently on both sides, so never flatten the tree. *Inside* a type folder
  everything is free: file names are arbitrary and files are found recursively,
  so one bundled file per type vs one-def-per-file is pure preference — this
  repo bundles per type, since reviewers work in whole-language passes and
  entries are found by their defName-prefixed keys, not by file. (The loader
  even tolerates a pluralized folder name by retrying with the last character
  stripped — `ThingDefs` → `ThingDef` — but the checker does not; use exact
  type names.)
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

### Glossary — shared across the mod family

The RU and JP rows were learned in the companion mod (UWU) from native review
(RU) and vanilla-data study (JP); the Simplified Chinese section was learned
in this repo's 2026-07 generation. Lessons propagate across all three repos
(here, ../UniqueWeaponsUnbound, ../PersonaWeaponsUnbound): when a row is added
or corrected in one skill, mirror it into the siblings, adjusting
domain-specific rows. Add rows whenever a native review lands corrections.

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

#### Simplified Chinese (from this repo's machine-assisted generation, 2026-07)

RimWorld's language folder is `ChineseSimplified` (tar: `ChineseSimplified
(简体中文).tar`) — the mod's folder must match it exactly, whatever the
public roster calls the language.

Style rules discovered from the vanilla zh data (mandatory):

- Full-width punctuation in prose (，。、；：（）……); descriptions end with 。;
  labels and buttons carry no trailing period. Placeholders, digits and units
  stay ASCII. Vanilla labels use full-width parens: 锻造台（燃料）.
- Quote cited names in prose with full-width curly quotes — vanilla writes
  任务“{0}”. Terse stat templates take no quotes ({0}伤害).
- `traitAdjectives` are bare attributive words with no trailing 的: the zh
  Odyssey namer composes both [weapon_adjective]的[weapon_noun] and
  [weapon_adjective][weapon_type], so each must read both ways. Avoid weak
  single characters (快 → 迅疾).
- Name grammar: no spaces around [symbols]; zh links with 的 and 之 and drops
  English "The" ("The X of Y" → Y之X). Material names compose directly:
  [stuff_adjective][weapon_noun] → 钢铁长剑, [stuff_adjective]之[badass_noun]
  → 翡翠之獠牙.
- Battle-log grammar: zh [skillAdv] entries end in 地, so an optional
  [skillAdvMaybe] slots cleanly before the verb; [RECIPIENT_possessive] is
  idiomatically dropped (vanilla zh does the same).
- Vanilla zh files can contain untranslated English values (Odyssey's
  ancient-mercenaries name symbols) — vanilla incompleteness is not style
  guidance. Some vanilla zh files carry a BOM; ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器特性) | — | Odyssey `WeaponTraits` / `StatsReport_WeaponTraits` |
| unique weapon | 特化武器 | 独特武器 | Odyssey `UniqueWeapon` |
| ultratech (attributive) | 极致科技 | 超科技 | `TechLevel_Ultra`=极致时代; `BodyPartsUltra`=极致科技 |
| monosword / plasmasword / zeushammer | 单分子剑 / 等离子剑 / 宙斯锤 | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | 长剑 / 长矛 / 钉头锤 / 匕首 / 短剑 / 战斧 / 战锤 | | Core/Odyssey/Royalty labels |
| plasteel | 玻璃钢 | 塑钢 | Core `Plasteel` — counterintuitive, always check |
| wood (material adjective) | 木 | | `WoodLog.stuffProps.stuffAdjective` |
| wielder (stat context) / bearer (flavour prose) | 使用者 / 持有者 | | Royalty `SpeedBoost`, Odyssey `EMPPulser` descs |
| stun / EMP | 击晕 / 电磁脉冲 (prose may keep "EMP") | | Core damage defs; zeushammer desc uses EMP冲击 |
| mechanoid | 机械族 | 机械体 | Core |
| item stash / bandit camp / ancient mercenaries | 物品藏匿点 / 匪徒营地 / 古代雇佣兵 | | Core sites, Odyssey quest |
| ancient (sealed) crate | 密封储物箱 | | Odyssey `AncientSealedCrate` |
| tribesfolk / tribal chief | 部众 / 酋长 | | Core `TribeRough` |
| quality tiers | 极差/较差/一般/良好/极佳/大师级/传奇级 | | Core `QualityCategory_*` |

Mod-decided terms pending native review (from the 2026-07 commit): 格挡
(parry, register-matched to `TextMote_Dodge` 闪避), 战团 (warband), 战帮
(war party), 剑格 / 十字护手 (quillons / crossguard), 撼地 (earthshake),
鼓舞呐喊 (rallying cry), 士气大振 (rallied), 传世 (storied), 打桩头
(piledriver), 阿片 (opiated), 珐琅 (enameled), 无回弹 (dead-blow).

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”) — injected labels never inflect, and quoting
  sidesteps case and agreement problems.
- When an English string is reworded, refresh the EN comments in every
  language **in the same commit** — the checker reports the mismatch as STALE
  either way, but batching avoids churn.
- Coined vanilla terms may be a portmanteau in one language and a plain word
  in another — always check, never extrapolate between languages.
- Mod-coined terms recur in def labels AND in Keyed settings prose that
  restates them. When generation is chunked across files or subagents,
  reconcile those terms across the whole language before committing (the
  zh-Hans run needed an alignment pass for earthshake / rallying cry /
  rallied / storied).

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
