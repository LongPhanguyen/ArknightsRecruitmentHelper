# Recruitment OCR (Windows companion)

Lets you click your emulator window once, and the app takes it from there: it
finds the recruitment tag row on its own (by OCR-matching against the known
tag vocabulary, not any hardcoded label text), and tracks that window (by
handle, falling back to title match if it restarts) so future captures find
the right region even after the window moves or resizes. OCRs the captured
region with Windows' built-in text recognition, matches the result against
the known tag list, and runs the same recruitment-combo ranking logic as the
Android app.

**Verified building and running on Windows**, including the one-click window
picker (fixed a couple of real build errors along the way — see git history).
**Automatic tag-region detection (`TagRegionDetector.cs`) is new and has not
been build/run-tested yet** — everything else in this list has been. Treat
the next build as the real first test pass for this part specifically.

## Projects

- `RecruitmentCore` — platform-agnostic library: `Tag`, `Operator`,
  `RecruitmentData` (placeholder data, mirrors the Android project's data 1:1),
  and `RecruitmentCalculator` (the enumerate/score/rank algorithm).
- `RecruitmentCore.Tests` — xUnit test reproducing the same 5-star-combo
  scenario as the Android test.
- `RecruitmentOcrApp` — WPF app: window picking (`WindowPicker.cs` +
  `Win32Interop.cs`), automatic tag-region detection (`TagRegionDetector.cs`),
  screen capture (GDI `CopyFromScreen`), OCR (`Windows.Media.Ocr`), tag
  matching, and the main UI.

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

## Selecting the emulator window

1. Make sure the recruitment tag screen is actually open and visible in the
   emulator first — detection only works if the 5 tags are on screen.
2. Click "Select Emulator Window".
3. Click anywhere on your emulator window (LDPlayer, MuMu, an Android Studio
   AVD, etc). Note: this click also reaches whatever's under the cursor
   normally — it isn't suppressed — so it may register as a click inside the
   game too. Clicking on a neutral part of the window (title bar, background)
   avoids that.
4. The app OCRs the whole window, finds every recognized line that matches a
   known tag name, and unions their bounding boxes (padded a bit) into the
   capture region — no manual selection needed.
5. If it can't find any recognizable tags, it says so clearly rather than
   guessing — just make sure the tag screen is visible and try again.
6. From then on, "Capture Tags" re-locates the window (by handle, or by title
   match if the handle goes stale, e.g. the emulator restarted) and captures
   the stored region relative to its *current* position, so moving/resizing
   the window doesn't break capture.

## Known limitations / next steps

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
