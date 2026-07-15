using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RecruitmentCore;

// Real operator roster: name, rarity, and full recruitment tag set (Class +
// Position/Qualification/Affix), for every operator currently recruitable on
// the Global server. Loaded from the embedded Data/operators.json resource
// at startup -- no network access needed at runtime.
//
// Source: Aceship/AN-EN-Tags (github.com/Aceship/AN-EN-Tags), specifically
// json/tl-akhr.json (operator roster, CN tag/class names, updated as
// recently as 2025-11-01 per its commit history) joined against
// json/tl-type.json (CN->EN class names) and json/tl-tags.json (CN->EN tag
// names + category). NOT the repo's older json/akhr.json/akhr2.json files --
// those haven't been touched since a 2019 file-move commit and are stale by
// several years' worth of operator releases.
//
// Filtered to operators where globalHidden is false (falling back to hidden
// if globalHidden is absent) -- a handful of operators are recruitable on CN
// (hidden=false) but not yet on Global (globalHidden=true), and this project
// is targeting the Global/EN client.
//
// This is a point-in-time snapshot, not a live feed: it will drift out of
// date as new operators release. Regenerating it means re-running the same
// fetch-join-filter process against the current tl-akhr.json/tl-type.json/
// tl-tags.json.
public static class OperatorDatabase
{
    public static readonly IReadOnlyList<Operator> AllOperators = Load();

    private static IReadOnlyList<Operator> Load()
    {
        var assembly = typeof(OperatorDatabase).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("Data.operators.json", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        var records = System.Text.Json.JsonSerializer.Deserialize<List<OperatorRecord>>(stream)
            ?? new List<OperatorRecord>();

        return records
            .Select(r => new Operator(r.Name, r.Rarity, new HashSet<int>(r.TagIds)))
            .ToList();
    }

    private sealed class OperatorRecord
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("rarity")]
        public int Rarity { get; set; }

        [JsonPropertyName("tagIds")]
        public List<int> TagIds { get; set; } = new();
    }
}
