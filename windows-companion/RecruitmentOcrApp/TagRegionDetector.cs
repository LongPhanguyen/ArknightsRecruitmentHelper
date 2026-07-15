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
// A real recruitment tag grid is compact -- all 5 tags sit close together.
// If unrelated on-screen text elsewhere in the window happens to match a tag
// name too (more likely the bigger/more content-filled the window is), a
// blind union across every match would get dragged out to include that
// stray match, producing a wrong, oversized region. So instead of unioning
// everything, matches are grouped by spatial proximity and only the
// largest tight cluster is used -- a stray far-away match just ends up
// alone in its own (discarded) cluster.
public static class TagRegionDetector
{
    // How close two matches need to be (relative to their own size) to
    // count as "the same cluster". Scales with the matches' size rather
    // than a fixed pixel distance, so it behaves the same whether the
    // emulator window (and therefore its text/chips) is small or large.
    private const double ClusterMarginMultiplier = 2.5;

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

        var largestCluster = FindLargestCluster(matchedRects);

        // Padding scales with the matched text's own size for the same
        // reason clustering does -- a fixed pixel value that looks right on
        // a small window would be too tight (or too loose) on a large one.
        var averageHeight = largestCluster.Average(r => r.Height);
        var padding = Math.Max(16, (int)(averageHeight * 0.6));

        var union = largestCluster.Aggregate(Rectangle.Union);
        return new TagRegionDetectionResult(Rectangle.Inflate(union, padding, padding), largestCluster.Count);
    }

    private static List<Rectangle> FindLargestCluster(List<Rectangle> rects)
    {
        var parent = Enumerable.Range(0, rects.Count).ToArray();

        int Find(int i) => parent[i] == i ? i : (parent[i] = Find(parent[i]));
        void Union(int a, int b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA != rootB) parent[rootA] = rootB;
        }

        for (var i = 0; i < rects.Count; i++)
        {
            for (var j = i + 1; j < rects.Count; j++)
            {
                if (AreNearby(rects[i], rects[j]))
                {
                    Union(i, j);
                }
            }
        }

        return Enumerable.Range(0, rects.Count)
            .GroupBy(Find)
            .Select(group => group.Select(i => rects[i]).ToList())
            .OrderByDescending(cluster => cluster.Count)
            .First();
    }

    private static bool AreNearby(Rectangle a, Rectangle b)
    {
        var margin = (int)(Math.Max(Math.Max(a.Width, a.Height), Math.Max(b.Width, b.Height)) * ClusterMarginMultiplier);
        var expandedA = Rectangle.Inflate(a, margin, margin);
        return expandedA.IntersectsWith(b);
    }
}
