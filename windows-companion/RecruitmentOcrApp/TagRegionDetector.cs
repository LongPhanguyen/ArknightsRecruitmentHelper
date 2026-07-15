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
// dragged out to include that stray match. So instead of unioning
// everything, matches whose distance from the overall centroid is a clear
// outlier get dropped before computing the region.
//
// This is deliberately centroid-based rather than strict pairwise
// nearest-neighbor clustering: on a very small window, individual tag chips
// can be tiny while the gaps between grid columns stay proportionally large
// (spacing doesn't always shrink at the same rate as the text/chips do), and
// a pairwise "are these two touching within a small margin" test can
// misfire and split the real grid into uneven pieces -- discarding a whole
// side of the grid if only the largest piece is kept. Distance-from-the-
// whole-group's-center doesn't have that failure mode: every real tag stays
// close to the *group's* center even if adjacent chips aren't close to each
// other individually.
public static class TagRegionDetector
{
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

        var inliers = RemoveDistantOutliers(matchedRects);

        // Padding scales with the matched text's own size for the same
        // scale-independence reason -- a fixed pixel value that looks right
        // on a small window would be too tight (or too loose) on a large one.
        var averageSize = inliers.Average(r => Math.Max(r.Width, r.Height));
        var padding = Math.Max(16, (int)(averageSize * 0.6));

        var union = inliers.Aggregate(Rectangle.Union);
        return new TagRegionDetectionResult(Rectangle.Inflate(union, padding, padding), inliers.Count);
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
