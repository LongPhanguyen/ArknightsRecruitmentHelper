# Bug found: capture region breaks when emulator window is smaller — root cause identified from logs

Got much more detailed logs this time (window bounds + monitor info now
included). This pinned down the actual root cause — sharing full evidence
below.

## Pattern observed
- **Large window** (`windowRect=(2559,-402,1658x972)`, i.e. roughly
  1658x972): capture region `858x276` → **matched 5/5 tags**. OCR text:
  "Sniper Starter Medic DPS Ranged" — correct, all 5 tags read.
- **Smaller window** (`windowRect=(2559,-402,791x484)`, then later
  `(2559,-402,909x551)`): capture region shrinks proportionally to `409x72`
  and `471x83` → **only matched 3/5 tags** both times. OCR text: "Sniper
  Medic Ranged" — 2 tags (Starter, DPS) are missing.
- Attached the actual captured screenshot from one of the 3/5 failures
  (`capture-attempt3-20260720-021043-838.png`) — it clearly shows only 3 tag
  chips rendered in the captured image: Sniper, Medic, Ranged. The other 2
  tags aren't just misread by OCR — they are not present in the captured
  pixels at all.

## Root cause (my read — please confirm/investigate further)
This is not a monitor/DPI/coordinate offset bug — window position and
monitor info are consistent and correct across all these logs
(`monitor=(2560,-405,2560x1440)` throughout). The actual issue: **the
in-game recruitment tag UI reflows depending on window size** — at smaller
window sizes, the game likely wraps tags onto a second row, or shrinks/
repositions them, rather than keeping the exact same relative layout at
every scale. Since the capture region is calculated once (as a fixed
proportion of the window, presumably based on where the "Job Tags" label
was found at selection time) and then reused, it only correctly captures
all 5 tags when the window is close to the size it was at during selection.
Shrinking the window afterward causes tags to move outside the previously
calculated region bounds.

Full log evidence attached below and as a separate text file for reference.

## Ask
- Confirm this diagnosis by comparing the actual pixel layout of the tag
  chips at both window sizes (large vs small) — does the recruitment panel
  visibly reflow/wrap at smaller sizes, or move to a different relative
  position within the window?
- If confirmed: rather than caching a single fixed region size once at
  selection time, consider one of:
  1. **Re-locating the "Job Tags" label fresh on every single capture**
     (not just at selection time) and computing the region relative to the
     window's CURRENT size each time, instead of reusing a cached region
     from selection time. This adds a bit of per-capture overhead but
     should be robust to window resizing between captures.
  2. **Capturing a generously oversized region** (the full lower/right
     portion of the window below the label, rather than a tightly-fit box)
     so that even if tags wrap to a second row, they're still within
     bounds — then let OCR find whatever tag text exists within that larger
     region rather than needing the region to be pixel-perfect.
  3. If the game UI has a minimum/typical window size where the layout is
     stable (and reflow only happens below some threshold), it might be
     simplest to just document/enforce a recommended minimum emulator
     window size and detect+warn if the window is smaller than that,
     rather than trying to handle arbitrary reflow programmatically.
- My preference would be option 2 (oversized capture region) or option 1
  (re-detect the label every capture) since those don't require the user to
  maintain a specific window size — but happy to hear which is more
  practical given the existing code structure.

## Additional context
Window is roughly 1/6 of my 1440p monitor when at the size that fails
(909x551 out of a 2560x1440 monitor, per the monitor bounds in the logs) —
so this isn't an unreasonably tiny window, it's a fairly normal size someone
might resize their emulator to.
