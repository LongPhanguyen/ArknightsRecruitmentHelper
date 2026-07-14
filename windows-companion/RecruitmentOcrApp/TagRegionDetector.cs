using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using RecruitmentCore;

namespace RecruitmentOcrApp;

// Finds the recruitment tag row within a window screenshot without needing
// to know any in-game label text: it OCRs the whole window, checks each
// recognized line against the known tag vocabulary (same matching logic as
// TagMatcher), and unions the bounding boxes of every line that matched a
// real tag name. That union (padded a bit) becomes the capture region.
//
// Known risk: if unrelated on-screen text elsewhere in the window happens to
// contain a tag name, it gets included in the union too, which could make
// the detected region too large. Only the actual recruitment tag row should
// be visible when this runs, to keep that from happening.
public static class TagRegionDetector
{
    private const int PaddingPixels = 24;

    public static Rectangle? DetectTagRegion(IReadOnlyList<OcrLineResult> lines)
    {
        var matchedRects = new List<Rectangle>();

        foreach (var line in lines)
        {
            var lineTokens = TagMatcher.Tokenize(line.Text);

            var matchesAnyTag = RecruitmentData.AllTags
                .Any(tag => TagMatcher.ContainsSequence(lineTokens, TagMatcher.Tokenize(tag.Name)));

            if (matchesAnyTag)
            {
                matchedRects.Add(line.BoundingRect);
            }
        }

        if (matchedRects.Count == 0) return null;

        var union = matchedRects.Aggregate(Rectangle.Union);
        return Rectangle.Inflate(union, PaddingPixels, PaddingPixels);
    }
}
