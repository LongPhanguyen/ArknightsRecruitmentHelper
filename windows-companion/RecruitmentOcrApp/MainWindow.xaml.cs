using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RecruitmentCore;

namespace RecruitmentOcrApp;

public partial class MainWindow : Window
{
    // Arknights recruitment always shows exactly 5 tag slots.
    private const int ExpectedTagCount = 5;
    private const int MaxAttempts = 3;
    private const int RetryDelayMs = 300;
    private const int MaxDiagnosticEntries = 30;

    private readonly OcrService _ocrService = new();
    private readonly Dictionary<int, CheckBox> _tagCheckboxes = new();
    private readonly Queue<string> _diagnosticEntries = new();
    private readonly RecruitmentStatsStore _statsStore = new();

    private IntPtr _pickedWindowHandle = IntPtr.Zero;
    private string _pickedWindowTitle = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        // Set after InitializeComponent (not via XAML IsChecked="True") so the
        // Checked event's Recalculate() call only fires once every control it
        // touches is guaranteed to already be connected.
        FourStarFilterCheckBox.IsChecked = true;
        RobotWarningCheckBox.IsChecked = true;

        UpdateStatsDisplay();
    }

    private async void OnSelectWindowClicked(object sender, RoutedEventArgs e)
    {
        SelectWindowButton.IsEnabled = false;
        WindowStatusText.Text = "Click on your emulator window...";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var picked = await WindowPicker.WaitForClickAsync(cts.Token);

            WindowStatusText.Text = $"Window picked: \"{picked.Title}\". Detecting tag region...";
            AppendDiagnostic($"[Select] picked window: {DescribeWindow(picked.Handle)}");

            // Only used here to validate the window has tags on it at all
            // (and to give early feedback) -- CaptureTagsAsync re-detects
            // fresh from the window's CURRENT size rather than trusting
            // this result, since the window can be resized in between.
            var detectionResult = await DetectRegionAsync(picked.Handle, "select");

            if (detectionResult.Region is null)
            {
                WindowStatusText.Text =
                    "Could not automatically find the tag area -- no recognizable tags were read from " +
                    "this window. Make sure the recruitment tag screen is open and visible, then try again.";
                return;
            }

            _pickedWindowHandle = picked.Handle;
            _pickedWindowTitle = picked.Title;
            CaptureButton.IsEnabled = true;

            WindowStatusText.Text = detectionResult.MatchedTagCount >= ExpectedTagCount
                ? $"Window selected: \"{picked.Title}\". Tag region auto-detected. Capturing tags..."
                : $"Window selected: \"{picked.Title}\", but only found {detectionResult.MatchedTagCount}/{ExpectedTagCount} " +
                  "tags after retrying. Most likely cause: the emulator window is too small/short to show the " +
                  "whole recruitment popup -- try resizing or maximizing it so all 5 tags AND the Cost/Confirm " +
                  "buttons beneath them are visible, then select the window again. Capturing with what was found...";

            // Auto-capture right away so selecting the window is the only
            // manual step -- no separate "Capture Tags" click needed.
            await CaptureTagsAsync();
        }
        catch (OperationCanceledException)
        {
            WindowStatusText.Text = "Timed out waiting for a click. Try again.";
        }
        catch (Exception ex)
        {
            WindowStatusText.Text = $"Window selection failed: {ex.Message}";
        }
        finally
        {
            SelectWindowButton.IsEnabled = true;
        }
    }

    private async void OnCaptureClicked(object sender, RoutedEventArgs e) => await CaptureTagsAsync();

    private async Task CaptureTagsAsync()
    {
        CaptureButton.IsEnabled = false;
        StatusText.Text = "Capturing...";
        try
        {
            var hwnd = WindowPicker.Resolve(_pickedWindowHandle, _pickedWindowTitle);
            if (hwnd == IntPtr.Zero)
            {
                StatusText.Text = "Could not find the emulator window -- please select it again.";
                return;
            }
            _pickedWindowHandle = hwnd;
            AppendDiagnostic($"[Capture] resolved window: {DescribeWindow(hwnd)}");

            // Re-detect the region fresh every time rather than reusing a
            // region computed at Select time -- the window can be resized
            // in between, and a cached region sized for a different window
            // size is exactly what caused tags to go missing at smaller
            // sizes.
            var detectionResult = await DetectRegionAsync(hwnd, "capture-detect");
            if (detectionResult.Region is not { } relativeRegion)
            {
                StatusText.Text = "Could not find the tag area on this capture -- try again, or re-select the window.";
                return;
            }

            var clientRect = Win32Interop.GetClientScreenRect(hwnd);
            var region = new Rectangle(
                clientRect.Left + relativeRegion.X,
                clientRect.Top + relativeRegion.Y,
                relativeRegion.Width,
                relativeRegion.Height);

            IReadOnlyList<Tag> detected = Array.Empty<Tag>();
            var text = string.Empty;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                using var bitmap = ScreenCapture.CaptureRegion(region);
                var savedPath = SaveDebugImage(bitmap, $"capture-attempt{attempt}");
                text = await _ocrService.RecognizeAsync(bitmap);
                detected = TagMatcher.Match(text);

                AppendDiagnostic(
                    $"[Capture attempt {attempt}/{MaxAttempts}] region={FormatRegion(region)}, " +
                    $"matched {detected.Count}/{ExpectedTagCount} tags, saved={savedPath}\ntext: \"{text}\"");

                if (detected.Count >= ExpectedTagCount) break;
                if (attempt < MaxAttempts) await Task.Delay(RetryDelayMs);
            }

            // Recorded here (once per capture, using the raw OCR result) rather
            // than inside Recalculate() -- that runs on every checkbox toggle
            // too, which would multi-count a single recruitment. Isolated in
            // its own try/catch so a stats-file problem can't masquerade as a
            // capture failure in StatusText.
            try
            {
                var recorded = _statsStore.RecordIfNew(detected.Select(t => t.Id).ToList());
                if (recorded) AppendDiagnostic($"[Stats] recorded new recruitment: {string.Join(",", detected.Select(t => t.Name))}");
                UpdateStatsDisplay();
            }
            catch (Exception statsEx)
            {
                AppendDiagnostic($"[Stats] failed to record: {statsEx.Message}");
            }

            PopulateDetectedTags(detected);
            Recalculate();
            StatusText.Text = detected.Count == 0
                ? $"No known tags recognized. Raw OCR text: \"{text}\""
                : detected.Count < ExpectedTagCount
                    ? $"Detected {detected.Count}/{ExpectedTagCount} tag(s) after retrying. If the emulator window is " +
                      "too small/short to show all 5 tags on screen, resizing it and re-selecting the window is the fix."
                    : $"Detected {detected.Count} tag(s).";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Capture failed: {ex.Message}";
        }
        finally
        {
            CaptureButton.IsEnabled = true;
        }
    }

    // Shared by both "Select Emulator Window" and every "Capture Tags" run:
    // captures the window's current whole client area, OCRs it, and finds
    // the tag region relative to that capture's own top-left. Called fresh
    // each time (not cached) so a window resized between selecting it and
    // any later capture always gets a region sized for its CURRENT state.
    private async Task<TagRegionDetectionResult> DetectRegionAsync(IntPtr hwnd, string logLabel)
    {
        var clientRect = ToRectangle(Win32Interop.GetClientScreenRect(hwnd));

        // The loop always runs at least once (MaxAttempts >= 1), so this
        // starts with a definite "nothing found yet" result rather than
        // null, keeping the caller's post-loop check unambiguous.
        var detectionResult = new TagRegionDetectionResult(null, 0);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var attemptBitmap = ScreenCapture.CaptureRegion(clientRect);
            var savedPath = SaveDebugImage(attemptBitmap, $"{logLabel}-attempt{attempt}");
            var words = await _ocrService.RecognizeWordsAsync(attemptBitmap);
            detectionResult = TagRegionDetector.DetectTagRegion(words, attemptBitmap.Size);

            AppendDiagnostic(
                $"[{logLabel} attempt {attempt}/{MaxAttempts}] matched {detectionResult.MatchedTagCount}/{ExpectedTagCount} tags, " +
                $"windowSize={clientRect.Width}x{clientRect.Height}, region={FormatRegion(detectionResult.Region)}, saved={savedPath}\n" +
                $"words: {string.Join(" | ", words.Select(w => w.Text))}");

            if (detectionResult.MatchedTagCount >= ExpectedTagCount) break;
            if (attempt < MaxAttempts) await Task.Delay(RetryDelayMs);
        }

        return detectionResult;
    }

    private static Rectangle ToRectangle(Win32Interop.RECT rect) =>
        new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

    private static string FormatRegion(Rectangle? region) =>
        region is { } r ? $"({r.X},{r.Y},{r.Width}x{r.Height})" : "(none)";

    private static string FormatRect(Win32Interop.RECT r) =>
        $"({r.Left},{r.Top},{r.Right - r.Left}x{r.Bottom - r.Top})";

    private static readonly string DebugImageDirectory = Path.Combine(Path.GetTempPath(), "ArknightsOcrDebug");

    // Saves exactly what got fed to OCR so it can be looked at directly --
    // text logs alone can't distinguish "the region is too small" from "the
    // captured image itself doesn't extend far enough," but a screenshot
    // answers that immediately.
    private static string SaveDebugImage(Bitmap bitmap, string label)
    {
        Directory.CreateDirectory(DebugImageDirectory);
        var fileName = $"{label}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png";
        var path = Path.Combine(DebugImageDirectory, fileName);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    // Everything needed to sanity-check the region math: is this literally
    // the same OS window both times (hwnd), what are its real screen bounds,
    // and which monitor is it on -- lets a positional mismatch between two
    // captures be diagnosed as "window moved" vs. "wrong window tracked" vs.
    // "multi-monitor/DPI coordinate mismatch".
    private static string DescribeWindow(IntPtr hwnd)
    {
        var windowRect = Win32Interop.GetWindowScreenRect(hwnd);
        var clientRect = Win32Interop.GetClientScreenRect(hwnd);
        var monitorBounds = Win32Interop.GetMonitorBounds(hwnd);
        var className = Win32Interop.GetClassNameOf(hwnd);
        var title = Win32Interop.GetWindowTitle(hwnd);

        return $"hwnd=0x{hwnd.ToInt64():X}, class=\"{className}\", title=\"{title}\", " +
               $"windowRect={FormatRect(windowRect)}, clientRect(screen)={FormatRect(clientRect)}, " +
               $"monitor={(monitorBounds is { } m ? FormatRect(m) : "unknown")}";
    }

    private void AppendDiagnostic(string message)
    {
        _diagnosticEntries.Enqueue($"{DateTime.Now:HH:mm:ss} {message}");
        while (_diagnosticEntries.Count > MaxDiagnosticEntries)
        {
            _diagnosticEntries.Dequeue();
        }

        DiagnosticsText.Text = string.Join("\n---\n", _diagnosticEntries);
        DiagnosticsText.ScrollToEnd();
    }

    private void UpdateStatsDisplay()
    {
        var summary = _statsStore.GetSummary();
        if (summary.Total == 0)
        {
            StatsText.Text = "Lifetime recruitments: none recorded yet.";
            return;
        }

        string Pct(int count) => $"{count} ({100.0 * count / summary.Total:0.#}%)";
        StatsText.Text = $"Lifetime recruitments: {summary.Total} — Baseline: {Pct(summary.Baseline)} | " +
                          $"4★: {Pct(summary.FourStar)} | 5★: {Pct(summary.FiveStar)} | 6★: {Pct(summary.SixStar)}";
    }

    private void OnRecalculateClicked(object sender, RoutedEventArgs e) => Recalculate();

    private void PopulateDetectedTags(IReadOnlyList<Tag> detected)
    {
        DetectedTagsPanel.Children.Clear();
        _tagCheckboxes.Clear();

        foreach (var tag in detected)
        {
            var checkbox = new CheckBox { Content = tag.Name, IsChecked = true, Tag = tag.Id };
            _tagCheckboxes[tag.Id] = checkbox;
            DetectedTagsPanel.Children.Add(checkbox);
        }
    }

    private void Recalculate()
    {
        var selectedIds = _tagCheckboxes.Values
            .Where(cb => cb.IsChecked == true)
            .Select(cb => (int)cb.Tag)
            .ToHashSet();

        UpdateRobotWarning(selectedIds);

        var qualifyingCombos = TagRarityRules.FindQualifyingCombos(selectedIds);

        ResultsList.ItemsSource = FourStarFilterCheckBox.IsChecked == true
            ? qualifyingCombos.Select(FormatComboEntry).ToList()
            : RecruitmentData.AllTags.Where(t => selectedIds.Contains(t.Id)).Select(t => t.Name).ToList();

        // Only worth showing when the selection actually guarantees a
        // 4-star+ outcome -- without that, any random 1-2 star operator
        // could technically match, which isn't useful/actionable
        // information. Collapsing the whole section (not just clearing the
        // list) when empty lets the window shrink back down instead of
        // sitting on unused space.
        var possibleOperators = qualifyingCombos.Count > 0
            ? OperatorLookup.FindPossibleOperators(OperatorDatabase.AllOperators, selectedIds)
                .OrderByDescending(o => o.Rarity)
                .ThenBy(o => o.Name)
                .Select(o => $"{o.Name} ({o.Rarity}★)")
                .ToList()
            : new List<string>();

        PossibleOperatorsList.ItemsSource = possibleOperators;
        PossibleOperatorsSection.Visibility = possibleOperators.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRobotWarning(IReadOnlyCollection<int> selectedIds)
    {
        var shouldWarn = RobotWarningCheckBox.IsChecked == true && TagRarityRules.HasRobotTag(selectedIds);
        RobotWarningText.Visibility = shouldWarn ? Visibility.Visible : Visibility.Collapsed;
        RobotWarningText.Text = shouldWarn ? "⚠ Contains Robot tag — likely low value." : string.Empty;
    }

    private static string FormatComboEntry(ComboRarityEntry entry)
    {
        var names = string.Join(" + ", entry.TagIds.Select(id => RecruitmentData.AllTags.First(t => t.Id == id).Name));
        return $"{names}: Guaranteed {entry.Rarity}★";
    }
}
