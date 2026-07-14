# Update: auto window detection works, next — tag rarity filtering/toggles

Confirmed the single-click window auto-detection is working. Next issue:
raw detected tags aren't that useful on their own — need rarity-aware
filtering based on Arknights recruitment mechanics.

## Background: how tag combos determine guaranteed rarity
Individual tags don't guarantee a rarity by themselves (except a few
special cases below). What matters is which *combinations* of two detected
tags appear together, based on fixed combo tables. Attaching reference
images (3 total) that show the actual combo data:

- Two images show tag-pair tables: a left-side tag (either blue "Class/
  Position/Qualification" style tags, or purple "Affix" style tags) plus a
  right-side tag it's paired with. The right-side tag's color indicates the
  guaranteed outcome when that specific pair both appear together:
  - **Yellow right-side tag** = pair guarantees a 5★ (or 6★, need to
    confirm which — see open question below) result
  - **Blue right-side tag** = pair guarantees 3★
  - Left tags shown in purple in these tables inherently guarantee 4★ on
    their own, and combining them with a right-side tag can upgrade that
    guarantee further per the color coding above
- A third reference image shows the full tag list grouped into 4
  categories: Class, Position, Qualification, Affix — plus "Robot" and
  "Elemental"/"Summon" tags highlighted separately.

Recommend implementing this as a static lookup table/dictionary (tag pair ->
guaranteed rarity outcome) rather than deriving it algorithmically, since
these are fixed game-design combos, not a computed rule. Attaching the
reference images so the exact pairs and colors can be transcribed correctly
into that lookup table — please transcribe carefully, this is the actual
gameplay-critical data.

## Feature 1: "4★ and above only" toggle (default ON)
When enabled, don't show individual/raw detected tags on their own. Instead:
- Cross-reference the set of detected tags against the combo lookup table
- Only display combos (tag pairs) that are known to guarantee 4★, 5★, or 6★
  results
- A single detected tag with no qualifying partner tag present should NOT
  be shown when this toggle is on, since it doesn't guarantee anything
  useful by itself
- When toggled OFF, fall back to current behavior (show all raw detected
  tags, no combo filtering)

## Feature 2: separate toggle/warning for 1★ and "Robot" tags
This is independent from the 4★ toggle above. When a detected tag set
includes a 1★-indicating tag or the "Robot" tag, surface a distinct warning
in the UI (e.g. "Contains Robot tag — likely low value") rather than folding
it into the rarity-combo display logic above. Should be toggle-able
(on/off) separately from Feature 1.

## Confirmed rarity rules (replaces earlier open questions)
- Yellow right-side tag in the combo tables = guaranteed 5★.
- 6★ is NOT obtainable through these tag combos at all — it only comes from
  the separate "Top Operator" tag.
- **Important — guarantees are EXACT SET matches, not "contains this pair
  anywhere among detected tags."** Example given: `Slow + DPS` = 4★
  guaranteed. `Slow + Caster` = 4★ guaranteed. `Slow + Caster + DPS`
  (all three together) = 5★ guaranteed — a DIFFERENT, higher outcome than
  either 2-tag subset alone. Any single one of `Slow`, `Caster`, or `DPS`
  alone = only 3★ (the baseline).
- This means the lookup table needs entries for exact tag SETS (both 2-tag
  and 3-tag combinations appear in the reference images), not just tag
  pairs. A set of {Slow, Caster, DPS} is a distinct entry from {Slow, DPS}
  and {Slow, Caster} — all three can coexist as separate lookup entries with
  different guaranteed outcomes.
- Filtering logic should therefore: take the full set of currently detected/
  selected tags, then check which known guaranteed sets (2-tag or 3-tag) are
  fully contained within that detected set, and surface the HIGHEST rarity
  guarantee found among all matching subsets — since a larger matching
  subset (e.g. the 3-tag set) can supersede a smaller one (e.g. a 2-tag
  subset of the same tags) with a better guarantee.
- If additional, unrelated tags are also detected alongside a qualifying
  set, the qualifying set's guarantee still holds for that subset — it's the
  matching of the specific subset that matters, not requiring the ENTIRE
  detected tag list to exactly equal the known combo (the user can still
  choose to only select the qualifying tags when actually recruiting in-game
  to lock in that guarantee; the app's job is just to surface which subsets
  are valuable).
