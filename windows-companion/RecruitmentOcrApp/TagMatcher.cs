using System.Collections.Generic;
using System.Linq;
using RecruitmentCore;

namespace RecruitmentOcrApp;

// Naive substring match: strip everything but letters/digits from both the OCR
// text and each known tag name, then check containment. Good enough as a first
// pass; if OCR misreads a tag often, the MainWindow checkboxes let you fix it
// by hand before recalculating.
public static class TagMatcher
{
    public static IReadOnlyList<Tag> Match(string ocrText)
    {
        var normalizedText = Normalize(ocrText);

        return RecruitmentData.AllTags
            .Where(tag => normalizedText.Contains(Normalize(tag.Name)))
            .ToList();
    }

    private static string Normalize(string text) =>
        new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
