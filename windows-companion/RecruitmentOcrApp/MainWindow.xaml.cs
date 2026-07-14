using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using RecruitmentCore;

namespace RecruitmentOcrApp;

public partial class MainWindow : Window
{
    private readonly OcrService _ocrService = new();
    private readonly Dictionary<int, CheckBox> _tagCheckboxes = new();

    private IntPtr _pickedWindowHandle = IntPtr.Zero;
    private string _pickedWindowTitle = string.Empty;
    private Rectangle _tagRegionOffset; // relative to the picked window's client top-left

    public MainWindow()
    {
        InitializeComponent();

        // Set after InitializeComponent (not via XAML IsChecked="True") so the
        // Checked event's Recalculate() call only fires once every control it
        // touches is guaranteed to already be connected.
        FourStarFilterCheckBox.IsChecked = true;
        RobotWarningCheckBox.IsChecked = true;
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

            var clientRect = ToRectangle(Win32Interop.GetClientScreenRect(picked.Handle));
            using var windowBitmap = ScreenCapture.CaptureRegion(clientRect);

            var lines = await _ocrService.RecognizeLinesAsync(windowBitmap);
            var detectedRegion = TagRegionDetector.DetectTagRegion(lines);

            if (detectedRegion is null)
            {
                WindowStatusText.Text =
                    "Could not automatically find the tag area -- no recognizable tags were read from " +
                    "this window. Make sure the recruitment tag screen is open and visible, then try again.";
                return;
            }

            _pickedWindowHandle = picked.Handle;
            _pickedWindowTitle = picked.Title;
            _tagRegionOffset = detectedRegion.Value;

            WindowStatusText.Text =
                $"Window selected: \"{picked.Title}\". Tag region auto-detected ({_tagRegionOffset.Width}x{_tagRegionOffset.Height}).";
            CaptureButton.IsEnabled = true;
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

    private async void OnCaptureClicked(object sender, RoutedEventArgs e)
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

            var clientRect = Win32Interop.GetClientScreenRect(hwnd);
            var region = new Rectangle(
                clientRect.Left + _tagRegionOffset.X,
                clientRect.Top + _tagRegionOffset.Y,
                _tagRegionOffset.Width,
                _tagRegionOffset.Height);

            using var bitmap = ScreenCapture.CaptureRegion(region);
            var text = await _ocrService.RecognizeAsync(bitmap);
            var detected = TagMatcher.Match(text);

            PopulateDetectedTags(detected);
            Recalculate();
            StatusText.Text = detected.Count == 0
                ? $"No known tags recognized. Raw OCR text: \"{text}\""
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

    private static Rectangle ToRectangle(Win32Interop.RECT rect) =>
        new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

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

        ResultsList.ItemsSource = FourStarFilterCheckBox.IsChecked == true
            ? TagRarityRules.FindQualifyingCombos(selectedIds).Select(FormatComboEntry).ToList()
            : RecruitmentData.AllTags.Where(t => selectedIds.Contains(t.Id)).Select(t => t.Name).ToList();
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
