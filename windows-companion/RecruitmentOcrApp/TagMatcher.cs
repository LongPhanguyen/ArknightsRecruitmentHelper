using System;
using System.Collections.Generic;
using System.Linq;
using RecruitmentCore;

namespace RecruitmentOcrApp;

// Matches recognized OCR text against known tag names by whole-word
// sequence, not raw substring containment. Substring matching broke on tag
// names that are themselves substrings of other tag names once whitespace
// was stripped -- e.g. "vanguard" contains "guard", so seeing only the
// Vanguard tag on screen would also falsely match the separate Guard tag;
// same for "supporter" containing "support". Tokenizing and comparing whole
// words closes that off, and still supports multi-word tags like
// "Senior Operator" by matching them as a contiguous token sequence.
public static class TagMatcher
{
    public static IReadOnlyList<Tag> Match(string ocrText)
    {
        var textTokens = Tokenize(ocrText);

        return RecruitmentData.AllTags
            .Where(tag => ContainsSequence(textTokens, Tokenize(tag.Name)))
            .ToList();
    }

    internal static bool ContainsSequence(IReadOnlyList<string> haystack, IReadOnlyList<string> needle)
    {
        if (needle.Count == 0) return false;

        for (var i = 0; i <= haystack.Count - needle.Count; i++)
        {
            var matched = true;
            for (var j = 0; j < needle.Count; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    matched = false;
                    break;
                }
            }
            if (matched) return true;
        }

        return false;
    }

    internal static List<string> Tokenize(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(token => token.Length > 0)
            .ToList();

    private static string Normalize(string token) =>
        new string(token.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
