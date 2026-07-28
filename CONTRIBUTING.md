# Contributing

Thanks for your interest in improving Unique Melee Weapons! Bug reports,
suggestions and pull requests are welcome.

## Localization

The mod targets the languages below, chosen by RimWorld's per-language
audience size. Contributions for any other language RimWorld supports are
welcome too.

| Language             | Status           | Credit  |
| -------------------- | ---------------- | ------- |
| English              | Source           | —       |
| Simplified Chinese   | Machine-assisted | Fable 5 |
| Russian              | Machine-assisted | Fable 5 |
| Korean               | Machine-assisted | Opus 5  |
| German               | Machine-assisted | Opus 5  |
| Spanish              | Machine-assisted | Opus 5  |
| French               | Planned          |         |
| Brazilian Portuguese | Planned          |         |
| Japanese             | Machine-assisted | Opus 5  |

Spanish here means Castilian (RimWorld's `Spanish` language folder). RimWorld also
ships a separate Latin American Spanish (`SpanishLatin`); a translation for it is
welcome as its own folder rather than as edits to this one.

Statuses: **Source** (the authoritative English strings), **Machine-assisted**
(generated with terminology grounded against the official RimWorld
localization; awaiting native review), **Native** (written or reviewed by a
native speaker), **Planned** (not started — contributions welcome).

### Contributing a translation

- Files live under `1.6/Languages/<Language>/` (`Keyed/` and `DefInjected/`),
  mirroring the structure of `1.6/Languages/English/`.
- Every translated entry carries the current English source in a comment
  directly above it, e.g. `<!-- EN: Reset to defaults -->` — this is how stale
  translations are detected when the English changes.
- Placeholders (`{0}`, `{1}`, ...) must match the English exactly.
- Vanilla def types use bare DefInjected folder names (`ThingDef`,
  `AbilityDef`); any of this mod's own def classes would use
  namespace-qualified names (`UniqueMeleeWeapons.<DefClass>`).
- Formatting: UTF-8 without BOM, LF line endings, 2-space indent.
- Validate before opening a PR:

  ```bash
  python3 Scripts/check-translations.py --strict
  ```

  It checks key coverage, placeholders, DefInjected paths, staleness, and
  file hygiene.

- Improving a machine-assisted language? Corrections from native speakers
  are gladly merged, no matter how small.
