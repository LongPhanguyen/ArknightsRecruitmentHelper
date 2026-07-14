# Recruitment OCR (Windows companion)

Lets you click your emulator window once, drag-select where the recruitment
tag row is, and have the app track that window (by handle, falling back to
title match if it restarts) so future captures find the right region even
after the window moves or resizes. OCRs the captured region with Windows'
built-in text recognition, matches the result against the known tag list, and
runs the same recruitment-combo ranking logic as the Android app.

**Verified building and running on Windows as of the previous commit** (fixed
missing `using` directives after a real build). **The window-picker flow below
is new and has not been build/run-tested yet** — the OCR/capture/UI plumbing
underneath it was already verified working, but the new Win32 interop
(`Win32Interop.cs`, `WindowPicker.cs`, `BitmapInterop.cs`) and drag-select
dialog (`TagRegionPickerWindow`) are untested. Treat the next build as the
real first test pass for this part specifically.

## Projects

- `RecruitmentCore` — platform-agnostic library: `Tag`, `Operator`,
  `RecruitmentData` (placeholder data, mirrors the Android project's data 1:1),
  and `RecruitmentCalculator` (the enumerate/score/rank algorithm).
- `RecruitmentCore.Tests` — xUnit test reproducing the same 5-star-combo
  scenario as the Android test.
- `RecruitmentOcrApp` — WPF app: window picking (`WindowPicker.cs` +
  `Win32Interop.cs`), tag-region drag-select (`TagRegionPickerWindow`), screen
  capture (GDI `CopyFromScreen`), OCR (`Windows.Media.Ocr`), tag matching, and
  the main UI.

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

## Selecting the emulator window and tag region

1. Click "Select Emulator Window".
2. Click anywhere on your emulator window (LDPlayer, MuMu, an Android Studio
   AVD, etc). Note: this click also reaches whatever's under the cursor
   normally — it isn't suppressed — so it may register as a click inside the
   game too. Clicking on a neutral part of the window (title bar, background)
   avoids that.
3. A preview of that window pops up — drag a box over the tag row, then click
   Confirm.
4. From then on, "Capture Tags" re-locates the window (by handle, or by title
   match if the handle goes stale, e.g. the emulator restarted) and captures
   the stored region relative to its *current* position, so moving/resizing
   the window doesn't break capture.

If the window can't be found, the status text says so and you just need to
click "Select Emulator Window" again.

## Known limitations / next steps

- Tag matching (`TagMatcher.cs`) is a naive substring match after stripping
  spaces/punctuation. If OCR misreads a tag, it just won't show up — use the
  checkboxes in the UI to add/remove tags by hand before recalculating.
- `RecruitmentData.cs` is the same placeholder data as the Android app. Update
  both when the real tag list and operator data are available; consider
  a shared JSON file consumed by both apps as a later step rather than hand-
  syncing two hardcoded copies.
- If `CopyFromScreen` captures black/blank pixels for the emulator window,
  that usually means the window is being drawn through a hardware overlay the
  GDI capture path can't see — try disabling any "hardware rendering" /
  "OpenGL passthrough" style setting in the emulator, or switch its renderer
  mode, and re-test.
- The drag-select dialog (`TagRegionPickerWindow`) assumes 1 WPF unit == 1
  screen pixel, which relies on `ApplicationHighDpiMode=PerMonitorV2` (set in
  `RecruitmentOcrApp.csproj`) actually taking effect. If the selected region
  ends up off from where you dragged (especially on a display with
  125%/150% scaling), that mismatch is the first thing to check.
- Window-picking uses a polling loop (`GetAsyncKeyState`) rather than a global
  hook, checked every ~15ms. This should feel instant in practice, but it's
  worth knowing about if a click ever seems to not register.
