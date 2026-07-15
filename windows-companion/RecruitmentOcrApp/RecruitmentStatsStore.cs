using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RecruitmentCore;

namespace RecruitmentOcrApp;

public sealed class RecruitmentLogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("tagIds")]
    public List<int> TagIds { get; set; } = new();

    // 0 = no guaranteed 4-star+ combo found in this tag set; otherwise 4, 5, or 6.
    [JsonPropertyName("guaranteedRarity")]
    public int GuaranteedRarity { get; set; }
}

public sealed record RecruitmentStatsSummary(int Total, int Baseline, int FourStar, int FiveStar, int SixStar);

// Persists a running log of distinct recruitments (deduplicated by tag set)
// so the ratio of no-guarantee/4-star/5-star/6-star outcomes can be tracked
// over time, across app restarts. Stored under %APPDATA% rather than the
// debug-image temp folder, since this is data worth keeping long-term, not
// a transient debugging artifact.
public sealed class RecruitmentStatsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ArknightsRecruitmentOcr",
        "recruitment-log.json");

    private readonly List<RecruitmentLogEntry> _entries;

    public RecruitmentStatsStore()
    {
        _entries = Load();
    }

    // Records a new entry only if the tag set differs from the most
    // recently recorded one -- pressing "Select Emulator Window" or
    // "Capture Tags" again on the same still-unclaimed recruitment
    // shouldn't inflate the count. Returns true if a new entry was added.
    public bool RecordIfNew(IReadOnlyCollection<int> tagIds)
    {
        if (tagIds.Count == 0) return false;

        var tagIdSet = new HashSet<int>(tagIds);
        var last = _entries.Count > 0 ? _entries[^1] : null;
        if (last is not null && new HashSet<int>(last.TagIds).SetEquals(tagIdSet))
        {
            return false;
        }

        var qualifyingCombos = TagRarityRules.FindQualifyingCombos(tagIdSet);
        var guaranteedRarity = qualifyingCombos.Count > 0 ? qualifyingCombos[0].Rarity : 0;

        _entries.Add(new RecruitmentLogEntry
        {
            Timestamp = DateTimeOffset.Now,
            TagIds = tagIdSet.OrderBy(id => id).ToList(),
            GuaranteedRarity = guaranteedRarity,
        });

        Save();
        return true;
    }

    public RecruitmentStatsSummary GetSummary() => new(
        Total: _entries.Count,
        Baseline: _entries.Count(e => e.GuaranteedRarity == 0),
        FourStar: _entries.Count(e => e.GuaranteedRarity == 4),
        FiveStar: _entries.Count(e => e.GuaranteedRarity == 5),
        SixStar: _entries.Count(e => e.GuaranteedRarity == 6));

    private static List<RecruitmentLogEntry> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<RecruitmentLogEntry>();
            using var stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize<List<RecruitmentLogEntry>>(stream) ?? new List<RecruitmentLogEntry>();
        }
        catch
        {
            // A corrupted or manually-edited log file shouldn't crash the
            // app on startup -- start fresh rather than block the whole
            // capture flow over a stats-file problem.
            return new List<RecruitmentLogEntry>();
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        using var stream = File.Create(FilePath);
        JsonSerializer.Serialize(stream, _entries, new JsonSerializerOptions { WriteIndented = true });
    }
}
