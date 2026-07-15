using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using RecruitmentCore;

namespace RecruitmentOcrApp;

public sealed record TagRegionDetectionResult(Rectangle? Region, int MatchedTagCount);

// Finds the recruitment tag row within a window screenshot without needing
// to know any in-game label text: OCRs the whole window, checks every
// contiguous run of recognized words against the known tag vocabulary (same
// matching logic as TagMatcher), and unions the matched words' bounding
// boxes into the capture region.
//
// This works on the flattened WORD sequence rather than per-line -- a
// hyphenated tag like "DP-Recovery" can get OCR'd as two separate lines
// ("DP", "Recovery"), and matching within a single line at a time would
// silently drop it since neither line alone contains the full name.
//
// Known risk: if unrelated on-screen text elsewhere in the window happens to
// contain a tag name, it gets included in the union too, which could make
// the detected region too large. Only the actual recruitment tag row should
// be visible when this runs, to keep that from happening.
public static class TagRegionDetector
{
    private const int PaddingPixels = 32;

    public static TagRegionDetectionResult DetectTagRegion(IReadOnlyList<OcrWordResult> words)
    {
        var wordTokens = words
            .Select(w => TagMatcher.Tokenize(w.Text).FirstOrDefault() ?? string.Empty)
            .ToList();

        var matchedRects = new List<Rectangle>();

        foreach (var tag in RecruitmentData.AllTags)
        {
            var tagTokens = TagMatcher.Tokenize(tag.Name);
            if (tagTokens.Count == 0) continue;

            for (var i = 0; i <= wordTokens.Count - tagTokens.Count; i++)
            {
                if (!wordTokens.Skip(i).Take(tagTokens.Count).SequenceEqual(tagTokens))
                {
                    continue;
                }

                var matchedWordRects = words.Skip(i).Take(tagTokens.Count).Select(w => w.BoundingRect);
                matchedRects.Add(matchedWordRects.Aggregate(Rectangle.Union));
            }
        }

        if (matchedRects.Count == 0) return new TagRegionDetectionResult(null, 0);

        var union = matchedRects.Aggregate(Rectangle.Union);
        return new TagRegionDetectionResult(Rectangle.Inflate(union, PaddingPixels, PaddingPixels), matchedRects.Count);
    }
}
