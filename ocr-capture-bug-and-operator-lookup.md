# Bug: inconsistent tag capture + new feature: operator lookup by tag combo

## Bug: capture sometimes misses tags, especially the middle-bottom slot
Confirmed publish/standalone exe works. Found a reliability issue during
testing:

- Recruitment normally shows 5 tag slots. Sometimes, especially right after
  switching from one recruitment to another, only 4 of the 5 tags get
  captured/detected.
- The tag that goes missing is inconsistent but seems to happen most often
  in the **middle-bottom slot** of the 5-tag layout.
- Tags that seem to have the most trouble being captured specifically:
  `DP-Recovery`, `Defender`, `Healing` — not sure yet if this is coincidence
  (these might just be the ones in that problem slot most often during
  testing) or if something about these specific tag strings is harder for
  OCR to read (e.g. hyphenated text like "DP-Recovery" possibly being split
  oddly by OCR, longer text getting clipped at region edges, etc).

### Requested investigation / fix
- Look into whether the capture region calculated after switching
  recruitment slots is slightly off (e.g. off-by-a-few-pixels vertical
  boundary that clips the middle-bottom tag), since this seems to correlate
  with switching between recruitments rather than being random on a single
  static screen.
- Consider adding a retry: if fewer than 5 tags are detected on a capture,
  automatically re-capture once or twice before showing results to the user,
  in case it's an intermittent OCR misread rather than a structural region
  problem.
- If `DP-Recovery`'s hyphen is causing tokenization issues in the text
  matching (separate from the earlier substring-matching fix), check that
  specifically — hyphenated tag names may need special handling in the
  tokenizer so they don't get split into two separate unmatched fragments.
- Add logging/diagnostic output (even just to console during testing) showing
  the raw OCR text and the calculated region bounds per capture, so future
  misses can be debugged from raw output rather than only from what tags
  end up displayed.

## New feature: operator lookup by detected tag combo
Want to start building toward showing which specific operators (4★, 5★, or
6★) can actually come from a given set of detected tags/combo — not just
"this combo guarantees 4★+" but "here are the actual operators possible at
that rarity with these tags."

This is a bigger feature, so for now: just start the groundwork rather than
a full implementation. Suggested first steps:
- Set up a data structure to hold operator data: name, rarity, and their
  associated recruitment tags (Class, Position, Qualification, Affix tags
  each operator has).
- This data will need to come from an actual up-to-date operator list — do
  not fabricate or guess operator-to-tag mappings. Flag clearly if this data
  needs to be sourced from an external database/wiki export rather than
  written manually, since Arknights has hundreds of operators and this list
  changes as new operators release.
- Once that data structure exists, a lookup function that takes a set of
  currently detected tags and returns which operators have a tag set that is
  a SUBSET of (contained within) the detected tags — those are the operators
  that could actually appear in that recruitment.
- No UI needed yet for this — just the data layer and lookup logic as a
  starting point. UI display of results can come in a follow-up once the
  underlying lookup works correctly.
