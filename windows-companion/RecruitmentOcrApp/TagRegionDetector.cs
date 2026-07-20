using System;
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
// A real recruitment tag grid is compact -- all 5 tags sit close together
// relative to their own centroid. If unrelated on-screen text elsewhere in
// the window happens to match a tag name too (more likely the bigger/more
// content-filled the window is), a blind union across every match would get
// dragged out to include that stray match. So outlier removal only runs
// when there are MORE matches than the known 5-tag cap allows -- that's the
// actual signal something is spurious. With 5 or fewer matches, all of them
// are kept unconditionally: Arknights never shows more than 5 tags, so a
// set already at or under that cap needs no second-guessing, however spread
// out it looks. (An earlier version always ran outlier removal, which
// backfired on small windows: individually tiny chips with proportionally
// large gaps between grid columns could make a perfectly real tag look like
// a statistical outlier relative to the rest.)
//
// When outlier removal does run, it's centroid-based rather than strict
// pairwise nearest-neighbor clustering: distance-from-the-whole-group's-
// center doesn't depend on any one pair being close, so it isn't thrown off
// by uneven spacing the way a pairwise "are these two touching" test is.
public static class TagRegionDetector
{
    // Arknights recruitment never shows more than 5 tag slots. Outlier
    // removal should only kick in when there's actual evidence of a stray
    // match -- i.e. more matches than could possibly all be real -- rather
    // than always second-guessing the spread of a set that's already at or
    // under the known cap.
    private const int MaxRealTags = 5;

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

        var inliers = matchedRects.Count > MaxRealTags
            ? RemoveDistantOutliers(matchedRects)
            : matchedRects;

        var union = inliers.Aggregate(Rectangle.Union);

        // Padding scales with the matched text's own size for the same
        // scale-independence reason -- a fixed pixel value that looks right
        // on a small window would be too tight (or too loose) on a large one.
        var averageSize = inliers.Average(r => Math.Max(r.Width, r.Height));
        var sidePadding = Math.Max(16, (int)(averageSize * 0.6));

        // The bottom edge gets extra room deliberately: the recruitment tag
        // grid can have a second row that this OCR pass didn't recognize as
        // text at all (observed at smaller window sizes), and that row
        // always sits just below whatever WAS detected. Extending downward
        // by the matched cluster's own height gives a missed second row
        // somewhere to be captured. This doesn't risk false positives --
        // the separate tag-reading OCR pass that runs against the final
        // cropped region only pulls out real tag names from whatever text
        // is actually present, so extra empty/unrelated space below is
        // harmless, not misleading.
        var bottomPadding = sidePadding + union.Height;

        var expanded = new Rectangle(
            union.X - sidePadding,
            union.Y - sidePadding,
            union.Width + sidePadding * 2,
            union.Height + sidePadding + bottomPadding);

        return new TagRegionDetectionResult(expanded, inliers.Count);
    }

    private static List<Rectangle> RemoveDistantOutliers(List<Rectangle> rects)
    {
        var centers = rects.Select(r => (X: r.X + r.Width / 2.0, Y: r.Y + r.Height / 2.0)).ToList();
        var centroidX = centers.Average(c => c.X);
        var centroidY = centers.Average(c => c.Y);

        var distances = centers
            .Select(c => Math.Sqrt(Math.Pow(c.X - centroidX, 2) + Math.Pow(c.Y - centroidY, 2)))
            .ToList();

        // Using the group's own median spread as the reference (rather than
        // a fixed pixel distance) is what makes this scale-independent: a
        // tight, tiny grid and a large, spread-out one both judge "is this
        // one far from the rest" relative to their own typical spread. The
        // small absolute floor just guards against a degenerate near-zero
        // threshold when everything happens to be nearly on top of itself.
        var medianDistance = distances.OrderBy(d => d).ElementAt(distances.Count / 2);
        var threshold = Math.Max(medianDistance * 3, 50);

        return rects.Where((_, i) => distances[i] <= threshold).ToList();
    }
}
