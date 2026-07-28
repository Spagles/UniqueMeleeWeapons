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
(RU) and vanilla-data study (JP); the Simplified Chinese and Korean sections
were learned in this repo's 2026-07 generations. Lessons propagate across all three repos
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

#### Japanese (from UWU machine-assisted generation, 2026-07, extended by this
repo's melee/quest pass, 2026-07)

RimWorld's language folder is `Japanese` (tar: `Japanese (日本語).tar`).

Style rules discovered from the vanilla JP data (mandatory):

- Vanilla JP uses ASCII punctuation: `,` and `.` — never `、` or `。`.
- Descriptions/tooltips: polite です/ます form ending `.`; labels/buttons no
  period. Thought (`ThoughtDef` stage) descriptions are the exception — plain
  first-person form, no です/ます.
- Quote injected def labels and cross-referenced UI labels with 「」. Suffixes
  and parentheticals take no leading space and use ASCII parens.
- `traitAdjectives` are **attributive** forms ending in の / な / い / a verb
  (Odyssey ships 探知の, 正確な, 灼熱の). The JP namer concatenates with no
  space, so a bare noun reads broken.
- Name grammar: no spaces around [symbols]; "The X of Y" → `[Y]の[X]`; vanilla
  keeps `[RECIPIENT_possessive]` (unlike zh, which drops it).
- `stuffProps.stuffAdjective` is `〜製` (鉄製, プラスチール製, 木製, ヒスイ製),
  so `[stuff_adjective]の[noun]` composes cleanly — supply the の in our rules,
  matching vanilla's の-terminated trait adjectives.
- Battle-log entries end in plain past tense (よけた, 受け流した) and JP
  `[skillAdv]` values are adverbials (巧みに, ゆっくりと), so `[skillAdvMaybe]`
  slots directly before the verb.
- `deathMessage` keeps vanilla's space after the pawn token: `{0}は 斬られて…`.
- DLC names stay in Latin script (Odyssey, Royalty), as does MOD.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器の特性) | 特性・特徴 | `WeaponTraits` / `StatsReport_WeaponTraits` / Odyssey `Stat_ThingUniqueWeaponTrait_Label`; 特性・特徴 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`) and belongs to PWU's domain, not ours |
| unique weapon | ユニークな武器 | | vanilla `UniqueWeapon`, Odyssey `*_Unique` labels |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| Cancel / Reset / Reset to defaults | キャンセル / リセット / デフォルトに戻す | | vanilla Keyed buttons |
| monosword / plasmasword / zeushammer | モノソード / プラズマソード / ゼウスハンマー | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | ロングソード / スピア / メイス / ナイフ / グラディウス / 戦斧 / ウォーハンマー | | Core/Odyssey/Royalty labels (mostly katakana, not 長剣/槍) |
| plasteel / jade / wood (stuff adjectives) | プラスチール製 / ヒスイ製 / 木製 | 塑鋼, 翡翠 | Core `stuffProps.stuffAdjective` |
| mechanite / mechanoid | メカナイト / メカノイド | | Royalty, Odyssey descs |
| wielder / bearer | 使用者 / 持ち主 | | Odyssey `EMPPulser` desc |
| stun / EMP / stagger | スタン / EMP / よろめき | | `StunnedByEMP`, `StaggerDurationFactor` |
| armor penetration / bleed rate / move speed | アーマー貫通力 / 出血量 / 移動速度 | | Core Keyed + StatDefs |
| cut / stab (DamageDef) | 斬る / 刺す | 切創, 刺し傷 (those are the *hediff* labels) | Core DamageDefs vs HediffDefs differ |
| bandaged / sutured / set / cut off / cut out | 包帯 / 縫合 / セット / 切り落とされた / 切り取られた | | Core `Cut`/`Stab` injury comps |
| toxic buildup | 毒物が蓄積 | | Core `ToxicBuildup` |
| item stash / bandit camp / ancient mercenaries / sealed crate | 埋蔵品 / 盗賊の野営地 / 古代の傭兵 / 密封されたクレート | | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement / tribesfolk / chief | 放棄された集落 / 蛮族 / 族長 | | Core `AbandonedSettlement`, `TribeRough` |
| humanlike / ability / quest / cooldown / cells | 人型 / 能力 / クエスト / クールダウン / セル | | Core Keyed |
| quality tiers | 壊れかけ/低品質/標準品/良品/秀品/名品/幻の一品 | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | 貿易商は高値で/低い価格でこれを買い取ります. | | Odyssey `GoldInlay`/`Ugly` descs — reuse verbatim |

The six Odyssey trait ports (`Lightweight`, `Cumbersome`, `Ornamental`,
`Ugly`, `GoldInlay`, `JadeInlay`) have official JP labels, adjectives and — for
four of them — descriptions that our English matches word for word; copy them
rather than retranslating.

Mod-decided terms pending native review (from the 2026-07 commit): 受け流し
(parry, register-matched to `TextMote_Dodge` 回避), 戦士団 (warband, parallel
to vanilla 傭兵団), 襲撃団 (war party), 頭目 (warlord), 鍔 / クロスガード
(quillons / crossguard), 地響き (earthshake), 鼓舞の叫び (rallying cry),
士気高揚 (rallied), 由緒ある (storied), 杭打ちヘッド (piledriver), アヘン塗布
(opiated), 琺瑯 (enameled), 無反発 (dead-blow, from the real tool term
無反発ハンマー), 稜付き (flanged), 鋲打ち (studded), 徹甲スパイク (armor
spike), 先重心 (head-weighted), 素早い (quickdraw — vanilla's 早撃ちの is
ranged-specific and wrong on melee).

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

#### Korean (from this repo's machine-assisted generation, 2026-07)

RimWorld's language folder is `Korean` (tar: `Korean (한국어).tar`). Decompile-
verified why the paren-stripped name works: `LoadedLanguage` derives
`legacyFolderName` by cutting at `(`, and mod language dirs match on *either*
`folderName` or `legacyFolderName` — the same mechanism behind `Japanese`.

**Josa (particle) markers are the one hard mechanical rule Korean adds, and
nothing else in this skill has an equivalent.** Korean particles are
allomorphic: the correct form depends on whether the previous syllable ends in
a consonant, which is unknowable when the preceding text is an injected value.
`Verse.LanguageWorker_Korean.ReplaceJosa` (decompile-verified) resolves exactly
eight tokens, and no others:

```
(이)가   (와)과   (을)를   (은)는   (아)야   (이)어   (으)로   (이)
```

- Every particle following `{0}`, `[symbol]` or `[TOKEN_x]` MUST use a marker.
  `{0}(을)를 생성` is correct; `{0}를 생성` breaks on consonant-final labels.
- Never hand-roll `{0}을(를)` — the worker does not recognize it.
- The one safe exception, which vanilla ko itself uses: a symbol that always
  resolves the same way, e.g. `[refugee_pronoun]는` (Korean pronouns are always
  vowel-final). Def labels, pawn names and material words are never safe.
- A lint for this lives outside the repo checker (which is language-agnostic);
  it was calibrated to zero false positives against Odyssey's WeaponTraitDefs
  and Core's DamageDefs.

Other style rules discovered from the vanilla ko data (mandatory):

- ASCII punctuation (`.` `,`), never `。`. Descriptions/tooltips take polite
  formal `-습니다.`/`-입니다.`; labels, buttons and stat fragments take no
  trailing period.
- `ThoughtDef` stage descriptions are the exception: casual first-person
  (`-어`, `-지`, `-군`, `-거야`), e.g. vanilla `이제 거의 깼어.`
- Battle-log rulesStrings end in the nominalized `-함.`/`-임.` form, not polite
  form (`Combat_Dodge`: `… [implement](을)를 [skillAdvMaybe] 피함.`).
- Korean **uses spaces**, unlike JP/zh: the ko namer composes
  `[weapon_adjective] [weapon_noun]` with a space, so `traitAdjectives` may be
  attributive verb forms (`가벼운`, `저주받은`) *or* bare noun modifiers
  (`황금`, `신속`, `특제`). Genitive epithets carry their own `의` (`죽음의`).
- Korean drops English "The" in name grammar and links with `의`
  (`[badass_concept]의 [weapon_type]`). Material composes bare:
  `[stuff_adjective] [weapon_noun]` → 강철 장검.
- Vanilla ko **drops `[RECIPIENT_possessive]`** in the combat packs — 12
  textual occurrences, all in EN comments, zero in Korean values. Korean omits
  possessive pronouns, so follow suit rather than rendering 그의.
- Units attach with no space: `{0}시간`, `{0}일`, `{0}칸`. Some vanilla ko
  files carry a BOM; ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 특성 (stats-entry title 무기 특성) | 개성 | Odyssey `WeaponTraits` / `Stat_ThingUniqueWeaponTrait_Label`; 개성 is Royalty's *persona* word (`Stat_Thing_PersonaWeaponTrait_Label`), PWU's domain |
| unique weapon | 고유 무기 | | Odyssey `UniqueWeapon` |
| **unique \<weapon\>** (label) | **특제 \<weapon\>** | | Odyssey's ranged uniques: 특제 장궁, 특제 돌격소총 |
| longsword / spear / mace / knife / gladius / axe / warhammer | 장검 / 창 / 철퇴 / 단검 / 검 / 도끼 / 전투망치 | | Core/Odyssey/Royalty labels |
| monosword / plasmasword / zeushammer | 단분자검 / 플라즈마검 / 제우스망치 | | Royalty labels |
| **mechanite(s)** | **기계입자** | 나노머신 | Core, 36/36 (근섬유질 기계입자); 나노머신 renders English *nanomachines* — a different word. Easy trap: they look interchangeable and are not |
| mechanoid | 메카노이드 | | Core |
| ultratech | 미래 (`TechLevel_Ultra`); 최첨단 attributively in prose | | monosword desc 최첨단 금속 검입니다 |
| plasteel / jade / wood / steel | 플라스틸 / 비취옥 (Odyssey inlay uses 옥) / 나무 · 목재 / 강철 | | Core labels + `stuffAdjective` |
| cut / stab (DamageDef) | 잘림 / 찔림 | 베임 (that is the *hediff* label) | Core DamageDefs vs HediffDefs differ |
| toxic \<damage\> label | `찔림 (독성)` shape | | Core `ScratchToxic`=찢김 (독성), `ToxicBite`=물림 (독성) |
| bandaged / sutured / set | 붕대 감음 / 봉합됨 / 접합됨 | | Core Cut/Stab injury comps |
| cut off / cut out | 끊어짐 / 잘림 | | Core `injuryProps` |
| toxic buildup / anesthetic | 중독 / 마취 | | Core |
| woozy / sedated | 혼미함 / 안정됨 | | Core `Anesthetic` stages; `-됨` is the hediff-stage family |
| point (tool) / edge (tool) | 칼끝 / 칼날 | 첨단 for "point" | Core tool labels; 첨단 reads "cutting-edge" (첨단 기술) in modern ko |
| armor penetration / move speed / stagger multiplier / bleeding | 방어 관통력 (melee: 근접 방어 관통력) / 이동속도 / 비틀거림 배수 / 출혈 | | Core StatDefs |
| Dodge (TextMote) | 회피 | | Core `TextMote_Dodge` |
| radius / cells / cooldown / ability / quest | 범위 / 칸 / 대기시간 / 능력 / 임무 | | Core Keyed |
| Cancel / Reset / Reset all | 취소 / 초기화 / 모두 초기화 | | Core Keyed |
| quality tiers | 끔찍/빈약/평범/상급/완벽/걸작/전설적 | | Core `QualityCategory_*` |
| item stash / bandit camp / ancient mercenaries / sealed crate | 귀중품 은닉처 / 도적 캠프 / 고대 용병들 / 밀봉된 상자 | | Core sites, Odyssey quest + `AncientSealedCrate` |
| tribesman / tribespeople / chief / fierce | 부족민 / 부족민들 / 족장 / 호전적인 | | Core `TribeRough` |
| wielder / bearer | 사용자 / 주인 | | Odyssey `EMPPulser`, Royalty descs |
| Traders will pay more/less for it. | 상인들이 더 높은 값을 쳐줍니다. / 상인들은 더 적은 돈을 쳐줍니다. | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim |

**Cross-checked against PWU's own ko pass (landed the same day, independently
grounded).** It reached the same josa conclusion, and adds two findings worth
reusing: `LanguageWorker_Korean.FindLastChar` skips a preceding `'`/`"`, so
quoting an injected label and *then* attaching a josa resolves correctly; and
`AlphabetEndPattern` contains no digits, so a josa directly after a number
always picks the no-batchim form and is wrong for 1/3/6/7/8/0 — phrase around
it. Two rows genuinely diverge, and neither repo should silently "fix" the other:

- **PWU's `mechanite` → 나노머신 row is wrong; use 기계입자.** PWU grounded on
  Royalty/Biotech only and concluded ko has no term for it. Core in fact has one
  in 7 files (`Hediffs_Local_Infections`, `Luciferium`, `Items_Exotic`, ...):
  all 36 English "mechanite" occurrences render 기계입자. 나노머신 is Core/Biotech's
  word for English *nanomachines*. Correct this when mirroring.
- **armor penetration: 방어 관통력 here, 관통력 in PWU — both correct.** Core
  Keyed `ArmorPenetration` is 관통력, but the StatDef this mod's info-card lines
  sit under is `MeleeWeapon_AverageArmorPenetration` = 근접 방어 관통력. Match
  whichever anchor the surrounding screen shows.

The six Odyssey trait ports have official ko labels/adjectives, and descriptions
that match our English verbatim for four of them (장식용, 난잡한 외형, 금 상감,
옥 상감); `Lightweight` 경량 and `Cumbersome` 불편 differ only in aim-vs-swing,
so adapt that clause alone. Note Odyssey's `Ugly` adjective *indices* differ
from ours: re-map by meaning (crude=조잡한, ugly=난잡한, monstrous=끔찍한).

Mod-decided terms pending native review (from the 2026-07 commit): 받아넘김
(parry, register-matched to `TextMote_Dodge` 회피), 전사단 (warband, parallel to
vanilla 용병단), 습격단 (war party), 두목 (warlord, distinct from Pirate 대장),
날받이 / 십자 가드 (quillons / crossguard), 지진 강타 (earthshake), 결집의 외침
(rallying cry), 결집됨 (rallied), 유서 있는 (storied), 항타기 (piledriver),
무반동 (dead-blow), 아편 도포 (opiated), 독 도포 (envenomed), 법랑 (enameled),
날개 돌기 (flanged), 징 박음 (studded), 관통 스파이크 (armor spike), 선단 편중
(head-weighted), 균형추 (counterweighted), 종 주조 (bell-cast), 바늘 끝 (needle
point), 미늘 (barbed, keeping 갈고리 for its "hooked" adjective), 탄화
(carbonized), 혈흔 (blood-stained), 톱니 (serrated), 면도날 (razored), 단분자 /
플라즈마 코어 / 제우스 헤드 (the ultratech trio), 진정제 축적 (sedative
buildup), 투여됨 (dosed), 찢긴 (ragged), 명장이 벼린 (master-forged), 도살도
(cleaver), 쇠메 (maul), 쇠뭉치 (mace head), 혈홍색 / 탄흑색 (colours, patterned
on Odyssey's 염홍색 / 전청색).

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”) — injected labels never inflect, and quoting
  sidesteps case and agreement problems. **Korean is the exception**: it solves
  the same problem mechanically with josa markers, so inject bare and mark the
  particle instead of quoting.
- **Check for a `LanguageWorker_<Language>` before generating.** It post-
  processes every string, so it can impose authoring requirements no amount of
  reading the vanilla data will reveal as *mandatory* — Korean's josa markers
  are invisible until you find `ReplaceJosa`. Decompile it:
  `ilspycmd "$RIMWORLD_PATH/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t
  "Verse.LanguageWorker_<Language>"`. Languages with heavy inflection (Russian,
  Polish, Turkish, Czech) are the ones to check first.
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
