using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using RecruitmentCore;

namespace RecruitmentOcrApp;

public partial class MainWindow : Window
{
    private readonly OcrService _ocrService = new();
    private readonly RecruitmentCalculator _calculator = new(RecruitmentData.AllOperators);
    private readonly Dictionary<int, CheckBox> _tagCheckboxes = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnCaptureClicked(object sender, RoutedEventArgs e)
    {
        var region = new Rectangle(
            int.Parse(RegionX.Text),
            int.Parse(RegionY.Text),
            int.Parse(RegionWidth.Text),
            int.Parse(RegionHeight.Text));

        CaptureButton.IsEnabled = false;
        StatusText.Text = "Capturing...";
        try
        {
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
