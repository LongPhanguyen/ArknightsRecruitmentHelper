# Recruitment OCR (Windows companion)

Lets you click your emulator window once, and the app takes it from there: it
finds the recruitment tag row on its own (by OCR-matching against the known
tag vocabulary, not any hardcoded label text), and tracks that window (by
handle, falling back to title match if it restarts) so future captures find
the right region even after the window moves or resizes. OCRs the captured
region with Windows' built-in text recognition, matches the result against
the known tag list, and surfaces which detected tags form a real
rarity-guaranteeing combo per Arknights' actual recruitment mechanics.

**Verified building and running on Windows**, including publishing a
standalone .exe, and the window-picking/capture-region pipeline has been
through several rounds of real testing and fixes (see "Reliability" below).
**Not yet tested by the user specifically: the rarity-combo toggles, the
Robot warning, the "Possible Operators" list, auto-capture-after-select, the
lifetime recruitment stats log, the proportional (screenshot-height-based)
region extension, and re-detecting the region fresh on every capture.**
Treat the next build as the real first test pass for those parts
specifically.

## Projects

- `RecruitmentCore` — platform-agnostic library: `Tag`, `Operator`,
  `RecruitmentData` (placeholder fixture data, mirrors the Android project's
  data 1:1 — used only by `RecruitmentCalculator`'s tests, not real
  operators), `RecruitmentCalculator` (an operator-roster-based combo
  evaluator — not currently wired into the OCR app's UI, though it could
  now be pointed at `OperatorDatabase` instead of the fixture data as a
  future step), `TagRarityRules` (the real, static tag-combo →
  guaranteed-rarity table the app's UI actually uses today), and
  `OperatorLookup`/`OperatorDatabase` (real operator roster + "which
  operators can this tag combo produce" lookup — groundwork, not wired into
  the UI yet; see below for exactly where the data came from).
- `RecruitmentCore.Tests` — xUnit tests covering the calculator, the rarity
  rules (including the supersede/dedup logic), and the operator lookup.
- `RecruitmentOcrApp` — WPF app: window picking (`WindowPicker.cs` +
  `Win32Interop.cs`), automatic tag-region detection (`TagRegionDetector.cs`),
  screen capture (GDI `CopyFromScreen`), OCR (`Windows.Media.Ocr`), tag
  matching, rarity-combo filtering, retry logic, and the main UI (including a
  diagnostics panel).

## Repo layout

- `docs/reference/` — reference images the code/data were built from:
  `recruitment-screen.webp` (a real screenshot confirming the in-game tag
  layout), `tag-categories.webp` and `tag-combos-1.webp`/`tag-combos-2.webp`
  (the tag-combo tables `TagRarityRules` was transcribed from). Kept and
  tracked in git since this is source material the code depends on, not
  disposable debugging output.
- `docs/planning/` — the instruction/bug-report markdown files from
  development history. Kept as a record of what was asked and why, not
  meant to be authoritative documentation (that's this README).
- `local-debug/` — **gitignored.** Ad hoc screenshots dropped in for
  reviewing a specific bug during development; not reference material and
  not meant to be committed. Safe to delete anytime; nothing depends on
  its contents persisting.

## Prerequisites

- .NET 8 SDK
- Windows 10 SDK (10.0.19041.0 or later) — needed for the `Windows.Media.Ocr` /
  `Windows.Graphics.Imaging` WinRT APIs used by `RecruitmentOcrApp`.
- An OCR language pack installed: Settings > Time & Language > Language &
  region > Add a language, and make sure "Optical character recognition" is
  checked for it. Without this, `OcrEngine.TryCreateFromUserProfileLanguages()`
  returns null and the app throws on startup.

## Build & run

Open `ArknightsRecruitmentOcr.sln` in Visual Studio and run `RecruitmentOcrApp`
as the startup project, or from the command line:

```
cd windows-companion
dotnet build
dotnet test RecruitmentCore.Tests
dotnet run --project RecruitmentOcrApp
```

## Distributing a standalone .exe to friends

A publish profile (`RecruitmentOcrApp/Properties/PublishProfiles/win-x64.pubxml`)
bundles the .NET runtime into a single self-contained `.exe`, so friends don't
need to install anything to run it:

```
cd windows-companion
dotnet publish RecruitmentOcrApp -p:PublishProfile=win-x64
```

(or in Visual Studio: right-click `RecruitmentOcrApp` → Publish → pick the
`win-x64` profile). Output lands at:

```
windows-companion/RecruitmentOcrApp/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/RecruitmentOcrApp.exe
```

That's the one file to hand to a friend. A few things worth knowing:

- **File size will be large** (expect somewhere in the 150-250MB range) --
  the entire .NET + WPF runtime is bundled in, which is the tradeoff for not
  requiring an install. Not something to fix, just don't be surprised by it.
- **Trimming is deliberately left off.** WPF apps are broadly known to be
  fragile under trimming (XAML/BAML loading and data binding lean on
  reflection that static trimming analysis can strip), and this app's WinRT-
  projected OCR calls are a second, independent reason to avoid it. This
  profile doesn't enable `PublishTrimmed` at all, so it defaults to off.
- **The OCR language pack requirement doesn't go away.** Bundling the .NET
  runtime has nothing to do with it -- the actual OCR engine/language data
  lives in Windows itself as an optional OS feature. Each friend still needs
  one installed (Settings > Time & Language > Language & region > Add a
  language, with "Optical character recognition" checked), or the app throws
  on startup for them the same way it would for you without it.
- **Windows 10 version 2004 (build 19041) or later is the floor**, since
  that's the WinRT API contract version this project targets (`net8.0-
  windows10.0.19041.0`). Effectively all actively-updated Windows 10/11
  machines meet this; a long-unupdated Windows 10 install might not.
- **No separate .NET runtime or Visual C++ Redistributable install should be
  needed.** The only native calls this app makes are to `user32.dll`/
  `gdi32.dll` (always present on Windows) and OS-level WinRT/COM activation
  for OCR -- neither depends on the VC++ redistributable. I can't verify this
  by actually running it on a fresh machine, though, so treat it as a
  reasonably confident expectation rather than a guarantee.
- **Expect a Windows SmartScreen "Windows protected your PC" warning** the
  first time a friend runs it, since it's an unsigned executable from an
  unrecognized publisher. That's normal, not a sign anything's broken --
  they'll need to click "More info" -> "Run anyway." Getting rid of this
  warning requires code-signing the executable, which isn't set up here.
- Some antivirus tools occasionally flag self-extracting single-file .NET
  publishes (the "unpack embedded files to a temp folder at startup"
  behavior can look similar to how some malware unpackers behave). Not
  expected, but worth knowing about if a friend's antivirus complains.

## Selecting the emulator window

1. Make sure the recruitment tag screen is actually open and visible in the
   emulator first — detection only works if the 5 tags are on screen.
2. Click "Select Emulator Window".
3. Click anywhere on your emulator window (LDPlayer, MuMu, an Android Studio
   AVD, etc). Note: this click also reaches whatever's under the cursor
   normally — it isn't suppressed — so it may register as a click inside the
   game too. Clicking on a neutral part of the window (title bar, background)
   avoids that.
4. The app OCRs the whole window, finds every contiguous run of recognized
   words that matches a known tag name, and unions their bounding boxes
   (padded a bit) into the capture region — no manual selection needed.
5. If it can't find any recognizable tags, it says so clearly rather than
   guessing — just make sure the tag screen is visible and try again.
6. **Tags are captured automatically right after the window is selected** —
   no separate "Capture Tags" click needed for the normal flow. The button
   is still there for manually re-capturing later (e.g. after switching to
   a new recruitment) without re-selecting the window.
7. "Capture Tags" (whether automatic or manual) re-locates the window (by
   handle, or by title match if the handle goes stale, e.g. the emulator
   restarted) and **re-detects the tag region from scratch** every time,
   rather than reusing a region computed at some earlier point — so
   resizing the window between captures doesn't leave a stale,
   wrong-sized region in play.

Known remaining inconsistency: region detection occasionally still
computes the *whole window* as the capture region instead of just the tag
cluster. Root cause isn't nailed down yet — if you want to help pin it
down, the diagnostics panel's word dump and the saved debug images
(`%TEMP%\ArknightsOcrDebug\`) for a run where this happens would help, the
same way they did for the earlier bugs.

## If fewer than 5 tags are detected: check the window size first

Confirmed via a saved debug screenshot: if the emulator window is too
short to display the *entire* recruitment popup (all 5 tags down through
the Cost/Confirm buttons), the missing tags simply never get rendered in
the window at all — they're not there to capture, not a bug in the
region/OCR logic. Window-bounds logging (hwnd, window rect, client rect,
monitor bounds) confirmed the region math itself is correct in this case;
the debug screenshot showed content cutting off immediately after the
first row of tags, with no space below for the rest of the popup. **Resize
or maximize the emulator window so the whole popup is visible, then
select the window again**, before assuming it's a code problem.

## Reliability: retries and diagnostics

Three independent issues were found during testing and fixed:

- **Window size affecting which tags get picked up.** Region detection used
  to union *every* tag-name match found anywhere in the window, with no
  regard for how far apart they were -- on a bigger window, unrelated
  on-screen text elsewhere could coincidentally match a tag name and drag
  the union out into a wrong, oversized region. Two follow-on attempts at a
  fix each overcorrected in turn:
  - Pairwise proximity clustering (keep only the largest cluster) broke on
    very small windows: tiny chips with proportionally large gaps between
    grid columns could split into uneven clusters, silently discarding a
    whole side of the grid.
  - Always-on centroid-distance outlier removal (drop anything more than 3x
    the group's median spread from center) fixed that, but could still
    misfire and drop a single legitimate tag whenever the 5-tag grid's
    natural spread made one corner look statistically distant from the rest
    -- still guessing at a threshold rather than using what's actually known.
  - Then: **only run outlier removal when there are *more* matches than the
    known 5-tag cap allows.** With 5 or fewer matches, there's no evidence
    anything is spurious, so all of them are kept unconditionally --
    Arknights never shows more than 5 tags, so a set already at or under
    that cap needs no second-guessing regardless of how spread out it
    looks. Outlier removal (still centroid-based, for the same reasons as
    before) only kicks in once there's real evidence of a stray match.
  - Remaining gap this didn't cover: at smaller window sizes, OCR sometimes
    doesn't recognize a whole second row of tags as text *at all* during
    region detection (not an outlier/clustering problem -- those 2 tags
    just never became candidate matches in the first place, confirmed via
    debug screenshots showing only 3 of 5 chips in the captured pixels).
    Log evidence: region height shrank much faster than width as the
    window got smaller (roughly halved vs. barely changed), consistent
    with only one row of a 2-row grid being captured. First fix attempt:
    extend the region's bottom edge by the matched cluster's *own* height.
    This helped but still fell short at smaller sizes -- the matched
    text's bounding box is a poor proxy for how much room a second row
    actually needs, since it doesn't scale the same way the rest of the
    UI (chip size, spacing, row gaps) does as the window shrinks. Fixed by
    making the bottom extension a **fraction of the whole screenshot's
    height** (22%) instead -- since everything in the UI scales with the
    window, sizing the extension off the window's own dimensions (rather
    than the matched text's, which can be much smaller) scales correctly
    at any window size. The expanded rectangle is clamped to the
    screenshot's actual bounds afterward, since a match near the edge plus
    a generous extension can otherwise push it past the image entirely.
    None of this risks false positives: the separate tag-reading OCR pass
    that runs against the final cropped region only pulls out real tag
    names from whatever's actually present, so extra empty space below is
    harmless.
  - Also: the region is now **re-detected fresh on every single capture**
    (both the initial one right after "Select Emulator Window" and any
    later manual "Capture Tags" click) rather than computing it once and
    reusing a cached value. A region cached from a larger window is sized
    wrong once the window is resized smaller -- recomputing it from the
    window's current state every time means a resize between captures
    can't leave a stale, wrongly-sized region in play.
- **A hyphenated tag like `DP-Recovery` could get silently dropped from
  region detection.** OCR sometimes recognizes it as two separate lines
  ("DP" and "Recovery") rather than one. The detector used to check each
  line's text against the tag vocabulary *in isolation*, so neither fragment
  alone contained the full tag name, and its bounding box just wasn't
  included in the region calculation — shrinking the capture area and
  potentially clipping whichever tag sat at that edge. This is consistent
  with the reported symptom being positional (often the middle-bottom slot)
  rather than tied to one specific tag. Fixed by matching against the
  flattened, ordered *word* sequence instead of per-line, so a tag split
  across lines by OCR is still found as long as the words remain adjacent.
- **Intermittent OCR misreads.** Both "Select Emulator Window" (region
  detection) and "Capture Tags" (the actual tag read) now retry up to 3
  times if fewer than 5 tags are found, with a short delay between attempts,
  before giving up and showing whatever was found with a clear "only found
  N/5" message rather than silently under-reporting.
- **A diagnostics panel** at the bottom of the window logs the raw OCR
  output, calculated region bounds, and window/monitor info (hwnd, class
  name, title, window rect, client rect, monitor bounds) for every attempt
  (both region detection and capture), so a miss can be debugged from what
  was actually read rather than only from what tags ended up displayed.
  This uses a visible UI panel rather than `Debug.WriteLine`, since a
  published standalone .exe has no attached debugger to see that output.
- **Every captured image is also saved to disk** as a PNG, at
  `%TEMP%\ArknightsOcrDebug\` (path logged alongside each diagnostic entry).
  Text logs can tell you *that* something was missed, but not whether the
  region is genuinely too small or the captured image itself doesn't extend
  far enough — looking at the actual saved image answers that immediately.

## Lifetime recruitment stats

`RecruitmentStatsStore` keeps a running log of distinct recruitments and
their best guaranteed rarity (baseline/4★/5★/6★), persisted to
`%APPDATA%\ArknightsRecruitmentOcr\recruitment-log.json` so it survives app
restarts. The "Lifetime recruitments" line near the top of the window shows
the running ratio, e.g. `Lifetime recruitments: 48 — Baseline: 12 (25%) |
4★: 30 (62.5%) | 5★: 5 (10.4%) | 6★: 1 (2.1%)`.

**Deduplicated by tag set**, not by button click: a new entry is only
recorded when the freshly-captured tag set differs from the *most recently
recorded* one. Pressing "Select Emulator Window" or "Capture Tags" again
on a recruitment you haven't moved past yet re-detects the same 5 tags and
correctly doesn't inflate the count. Recording happens once per capture
(right after OCR, using the raw detected tags) rather than inside
`Recalculate()`, since that also runs on every checkbox toggle and would
otherwise multi-count a single recruitment. A stats-file problem (e.g.
permissions) is caught separately from the main capture flow and logged to
the diagnostics panel rather than showing as a capture failure.

Known limitation: this dedupes on exact tag-set equality, so if two
genuinely different recruitments happen to draw the identical 5 tags back
to back, the second one won't be counted. Accepted as the simplest
reasonable heuristic rather than something more elaborate (e.g. a
time-based cutoff).

## Operator lookup

`OperatorLookup.FindPossibleOperators` takes a detected tag set and returns
every operator in `OperatorDatabase.AllOperators` whose own tags are fully
contained within it — i.e. operators that could actually appear in that
recruitment. Wired into the UI as the "Possible Operators" list, sorted by
rarity, updating alongside the Results list whenever tags are toggled.

**Gated on an actual guaranteed 4★+ combo existing** (checked via
`TagRarityRules.FindQualifyingCombos`, independent of whatever the "Only
show 4★+ combos" checkbox happens to be set to) — without a guarantee, any
random 1-2★ operator could technically match the selection, which isn't
useful information. When there's nothing to show, the whole section
(header + list) collapses rather than just showing an empty list, and the
window (which sizes to its content) shrinks back down accordingly.

**`OperatorDatabase.AllOperators` is now real data** (149 operators,
currently recruitable on Global), loaded from an embedded JSON resource
(`RecruitmentCore/Data/operators.json`) baked in at build time — no network
access needed at runtime, and it survives single-file publish since embedded
resources live inside the compiled assembly.

### Where this data came from, and what had to change from the original plan

The originally-documented path (`akhr.json` at the repo root) 404s because
the repo was restructured; it moved into a `json/` folder. But investigating
further turned up more than just a path change:

- **`json/akhr.json` and `json/akhr2.json` (the two files matching the
  originally-assumed name) are both stale since a single 2019 file-move
  commit** — neither has been touched since, meaning ~6 years of missing
  operator releases. The repo itself is still actively maintained (pushed as
  recently as November 2025), just not those two specific files.
- **The actually-current file is `json/tl-akhr.json`**, with commits as
  recent as 2025-11-01 ("CN Recruiting 2025-11-01"). Correct raw URL:
  `https://raw.githubusercontent.com/Aceship/AN-EN-Tags/master/json/tl-akhr.json`.
  416 total entries (including non-recruitable/CN-only ones).
- **The schema doesn't match what was assumed.** `type` and `tags` in
  `tl-akhr.json` are in *Chinese*, not English — this is a translation-table
  repo, and the CN→EN mapping lives in two companion files:
  `json/tl-type.json` (class name CN→EN) and `json/tl-tags.json` (tag
  name CN→EN, plus a `type` field: `qualifications`/`position`/`affix`).
  All 8 classes and all 23 position/qualification/affix tags in those two
  files map cleanly onto this project's existing tag list with zero
  unmapped entries.
- **The operator's Class tag is a separate `type` field, not part of the
  `tags` array.** An operator's full tag set = `{translated type}` ∪
  `{translated tags}` — e.g. Lancet-2's `tags` array is `["Ranged",
  "Healing"]` but her full recruitment tag set also needs "Medic" added in
  from `type`.
- **Two hidden-status fields exist: `hidden` and `globalHidden`.** 4 of the
  416 operators are recruitable on CN (`hidden: false`) but not yet on
  Global (`globalHidden: true`) — e.g. Kal'tsit. Filtered on `globalHidden`
  (falling back to `hidden` when absent, since only 345/416 entries have
  `globalHidden` at all) to match this project's Global/EN client target.
- **Sanity-checked the join**: every 6★ operator has exactly the Top
  Operator tag and no others do (30/30 match), and every Senior-Operator-
  tagged operator is 5★ (51/51) — strong evidence the CN→EN translation
  join is correct rather than silently misaligned.

This is a **point-in-time snapshot** (generated 2026-07-15 from the commit
state described above), not a live feed — it'll drift as new operators
release. Regenerating means re-running the same fetch → CN/EN join → filter
process against the then-current `tl-akhr.json`/`tl-type.json`/
`tl-tags.json` and overwriting `Data/operators.json`.

## Rarity-combo toggles

The "Results" list reflects real Arknights recruitment mechanics, not a raw
tag dump:

- **"Only show 4★+ combos"** (default on): cross-references the detected tags
  against `TagRarityRules`' static combo table and shows only the tag subsets
  that guarantee 4★, 5★, or 6★ — a lone detected tag with no qualifying
  partner isn't shown, since it doesn't guarantee anything by itself. When a
  larger matching subset (e.g. a 3-tag combo) supersedes a smaller one (e.g.
  its own 2-tag subset) with a better guarantee, only the better one is
  shown. Turning this off just lists the raw detected tag names instead, with
  no filtering.
- **"Warn on Robot/1★ tag"** (default on, independent of the toggle above):
  shows a standalone warning banner if the Robot tag was detected, rather
  than folding it into the combo display.

## Known limitations / next steps

- `TagRarityRules.KnownCombos` was transcribed by visually reading a
  compressed reference image's color-coded cells (orange = 5★ pair, purple =
  4★ pair, on top of purple-anchor tags already guaranteeing 4★ alone). Two
  individual cells were initially misread and corrected after conflicting
  with confirmed examples (`Slow + Caster` and `Slow + Healing` are both 4★,
  not 5★) — the rest of the table hasn't been independently re-verified
  beyond those two corrections and the one explicit `DPS + Defense = 5★`
  confirmation. If a combo's shown rarity doesn't match what actually happens
  in-game, that transcription is the first place to check.
- Tag matching (`TagMatcher.cs`) compares whole word sequences (not raw
  substrings), so distinct tags that happen to overlap textually (e.g.
  Support/Supporter, Guard/Vanguard) no longer both match when only one is on
  screen. If OCR still misreads a tag, it just won't show up — use the
  checkboxes in the UI to add/remove tags by hand before recalculating.
- Automatic tag-region detection (`TagRegionDetector.cs`) OCRs the *whole*
  window, so if unrelated on-screen text elsewhere in the window happens to
  contain a real tag name, it gets unioned into the detected region too,
  potentially making it too large. Keeping only the recruitment tag screen
  visible (no other overlapping UI) when clicking "Select Emulator Window"
  avoids this. If the detected region still looks wrong, that's the first
  thing to check.
- The word-level matching fix, retry loops, and diagnostics panel are all
  new this round and haven't been build/run-tested. If retries don't seem to
  help with the missing-tag issue, check the diagnostics panel's raw OCR
  text per attempt first -- it'll show whether the tag was read at all
  (region/matching problem) or read but with wrong characters (OCR quality
  problem), which point to different fixes.
- `RecruitmentData.cs` is the same placeholder data as the Android app. Update
  both when the real tag list and operator data are available; consider
  a shared JSON file consumed by both apps as a later step rather than hand-
  syncing two hardcoded copies.
- If `CopyFromScreen` captures black/blank pixels for the emulator window,
  that usually means the window is being drawn through a hardware overlay the
  GDI capture path can't see — try disabling any "hardware rendering" /
  "OpenGL passthrough" style setting in the emulator, or switch its renderer
  mode, and re-test.
- Window/region pixel math relies on `ApplicationHighDpiMode=PerMonitorV2`
  (set in `RecruitmentOcrApp.csproj`) actually taking effect. If captures land
  offset from where tags actually are (especially on a display with 125%/150%
  scaling), that mismatch is the first thing to check.
- Window-picking uses a polling loop (`GetAsyncKeyState`) rather than a global
  hook, checked every ~15ms. This should feel instant in practice, but it's
  worth knowing about if a click ever seems to not register.
