# Feature: Window-Picker Region Selection for RecruitmentOcrApp

## Problem
The current capture region is hardcoded as absolute screen pixels (X/Y/W/H
entered manually). This breaks across different emulators (BlueStacks, MuMu,
Android Studio AVD), different window sizes, and different monitor setups.
Users shouldn't have to guess pixel coordinates.

## Desired flow
1. User clicks a new **"Select Emulator Window"** button in the app.
2. The app enters a "picking" mode — cursor changes to a crosshair (or similar
   visual cue) and a small instruction label appears: "Click on your emulator
   window."
3. User clicks anywhere on their emulator window (e.g. the BlueStacks or AVD
   window showing Arknights).
4. The app identifies *which window* was clicked (not just the pixel under
   the cursor) and stores a reference to that window — window handle (HWND),
   not just a static coordinate snapshot.
5. The app records the recruitment-tag panel's position **relative to that
   window's client area**, not the screen. This can start as a second,
   simpler step: after picking the window, ask the user to drag a box over
   just the tag-text area within that window (a lightweight click-drag
   selection over a temporary screenshot overlay of that window is fine for
   v1 — doesn't need to be fully automatic tag-panel detection yet).
6. Store both: the window handle/title (for re-locating the window later)
   and the relative offset/size of the tag region within that window.
7. On future "Capture Tags" clicks, the app:
   - Re-locates the window by handle (or by title match if the handle is
     stale, e.g. app was restarted)
   - Gets that window's *current* screen position (it may have moved)
   - Computes the absolute screen region by combining the window's current
     position + the stored relative offset
   - Captures only that region and runs OCR as before

## Why this approach (context for Claude Code, not user-facing)
- Full auto-detection of "which open window is an Android emulator" is
  fragile — there's no reliable universal signal across BlueStacks, MuMu,
  Nox, and Android Studio's AVD (different process names, window titles,
  window classes).
- Manually picking the window once, then tracking it by handle, solves the
  "windows move/resize" problem without needing to solve "identify emulators
  automatically."
- This should be significantly more robust than fixed pixel coordinates
  while staying implementable without a large CV/image-matching effort.

## Windows implementation notes
- Use the Win32 API (via P/Invoke in C#/.NET) to enumerate windows and
  identify which window is under the cursor at click time:
  `WindowFromPoint()` to get the HWND under a screen point, then
  `GetWindowRect()` / `GetClientRect()` for its bounds.
- Store the window's **title text** (`GetWindowText`) as a fallback matcher
  in case the HWND becomes invalid (e.g. app restarted) — try HWND first,
  fall back to title-substring match if it fails.
- For the "drag a box to select the tag region" step, a simple approach:
  take a screenshot of just the picked window, show it in an overlay/dialog,
  let the user click-drag a rectangle over it (basic WPF canvas + mouse
  event handlers), and store the rectangle's coordinates as offsets from
  that window's top-left corner.

## Out of scope for this iteration
- Fully automatic detection of the recruitment panel via image/template
  matching (OpenCV or similar) — worth considering later, but not needed for
  this pass.
- Auto-detecting which of multiple open windows is "the emulator" without
  the user clicking it.
- Handling window minimized/hidden states gracefully — for v1, just fail
  clearly with a message like "Could not find the emulator window — please
  select it again" if the window can't be located.

## Acceptance check
- Open any emulator window, resize/move it, click "Select Emulator Window,"
  click the emulator, drag a box over where tags appear.
- Move or resize the emulator window again.
- Click "Capture Tags" — it should still find and OCR the correct region
  without the user re-selecting anything, as long as the window still
  exists.
