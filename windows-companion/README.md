# Recruitment OCR (Windows companion)

Captures a fixed screen region (where you position LDPlayer/MuMu's recruitment
tag row), OCRs it with Windows' built-in text recognition, matches the result
against the known tag list, and runs the same recruitment-combo ranking logic
as the Android app.

**This was written on a Mac with no Windows/.NET SDK available, so none of it
has been compiled or run.** Treat the first build on your Windows machine as
the real first test pass — expect to fix a build error or two.

## Projects

- `RecruitmentCore` — platform-agnostic library: `Tag`, `Operator`,
  `RecruitmentData` (placeholder data, mirrors the Android project's data 1:1),
  and `RecruitmentCalculator` (the enumerate/score/rank algorithm).
- `RecruitmentCore.Tests` — xUnit test reproducing the same 5-star-combo
  scenario as the Android test.
- `RecruitmentOcrApp` — WPF app: screen capture (GDI `CopyFromScreen`), OCR
  (`Windows.Media.Ocr`), tag matching, and the UI.

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

## Setting the capture region

The app captures a fixed rectangle in screen pixels (not window-relative), so
it only works if the emulator window stays in the same position/size. To find
the right X/Y/W/H:

1. Position the emulator window where you'll always keep it.
2. Open the recruitment tag screen in-game.
3. Use any pixel-coordinate tool (e.g. the cursor position readout in Paint,
   or PowerToys Mouse Utilities) to read the top-left corner and size of the
   tag row.
4. Enter those four numbers into the app and hit "Capture Tags".

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
