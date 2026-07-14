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
    private readonly RecruitmentCalculator _calculator = new(RecruitmentData.AllOperators);
    private readonly Dictionary<int, CheckBox> _tagCheckboxes = new();

    private IntPtr _pickedWindowHandle = IntPtr.Zero;
    private string _pickedWindowTitle = string.Empty;
    private Rectangle _tagRegionOffset; // relative to the picked window's client top-left

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnSelectWindowClicked(object sender, RoutedEventArgs e)
    {
        SelectWindowButton.IsEnabled = false;
        WindowStatusText.Text = "Click on your emulator window...";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var picked = await WindowPicker.WaitForClickAsync(cts.Token);

            var clientRect = ToRectangle(Win32Interop.GetClientScreenRect(picked.Handle));

            using var windowBitmap = ScreenCapture.CaptureRegion(clientRect);
            var pickerWindow = new TagRegionPickerWindow(windowBitmap) { Owner = this };
            var confirmed = pickerWindow.ShowDialog();

            if (confirmed != true)
            {
                WindowStatusText.Text = "Window selection cancelled.";
                return;
            }

            _pickedWindowHandle = picked.Handle;
            _pickedWindowTitle = picked.Title;
            _tagRegionOffset = pickerWindow.SelectedRegion;

            WindowStatusText.Text =
                $"Window selected: \"{picked.Title}\". Tag region set ({_tagRegionOffset.Width}x{_tagRegionOffset.Height}).";
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

        var selectedTags = RecruitmentData.AllTags.Where(t => selectedIds.Contains(t.Id)).ToList();

        var combos = selectedTags.Count == 0
            ? new List<TagCombo>()
            : _calculator.Evaluate(selectedTags);

        ResultsList.ItemsSource = combos.Select(FormatCombo).ToList();
    }

    private static string FormatCombo(TagCombo combo)
    {
        var names = string.Join(" + ", combo.Tags.Select(t => t.Name));
        var rarity = combo.FloorRarity == combo.CeilingRarity
            ? $"{combo.FloorRarity}★"
            : $"{combo.FloorRarity}★-{combo.CeilingRarity}★";
        var label = combo.IsGuaranteed ? "Guaranteed" : "Possible";
        return $"{names}: {label} {rarity}";
    }
}
