using System.Collections.Generic;
using System.Linq;

namespace RecruitmentCore;

public sealed record ComboRarityEntry(IReadOnlyList<int> TagIds, int Rarity);

// Static combo-rarity table transcribed from the recruitment tag-combo
// reference images, not derived from operator data -- these are fixed
// game-design combos, not something a roster intersection can compute.
// Best-effort read of a compressed reference image: one entry (Slow+Caster)
// was already corrected after conflicting with a confirmed example; a
// handful of other individually-colored cells in the same table haven't
// been independently re-verified. Flag anything that looks wrong once you
// see real results against these.
public static class TagRarityRules
{
    public const int TopOperatorTagId = 13;
    public const int RobotTagId = 28;

    // Tags whose mere presence -- no partner needed -- already guarantees 4-star.
    public static readonly IReadOnlyList<int> FourStarAloneTagIds = new[]
    {
        14, // Crowd-Control
        15, // Nuker
        17, // Support
        21, // Debuff
        22, // Fast-Redeploy
        26, // Shift
        27, // Summon
        10, // Specialist
    };

    public static readonly IReadOnlyList<ComboRarityEntry> KnownCombos = new List<ComboRarityEntry>
    {
        // Slow (25)
        new(new[] { 25, 9, 19 }, 5), // Slow + Caster + DPS
        new(new[] { 25, 23 }, 4),    // Slow + AoE
        new(new[] { 25, 5 }, 4),     // Slow + Sniper
        new(new[] { 25, 19 }, 4),    // Slow + DPS
        new(new[] { 25, 1 }, 4),     // Slow + Melee
        new(new[] { 25, 4 }, 4),     // Slow + Guard
        new(new[] { 25, 9 }, 4),     // Slow + Caster (corrected per confirmed example)
        new(new[] { 25, 16 }, 4),    // Slow + Healing (corrected per confirmed example)

        // DPS (19)
        new(new[] { 19, 24 }, 5),      // DPS + Defense
        new(new[] { 19, 6 }, 5),       // DPS + Defender
        new(new[] { 19, 8 }, 5),       // DPS + Supporter
        new(new[] { 19, 16 }, 5),      // DPS + Healing
        new(new[] { 19, 23, 1 }, 5),   // DPS + AoE + Melee
        new(new[] { 19, 23, 4 }, 5),   // DPS + AoE + Guard
        new(new[] { 19, 23 }, 4),      // DPS + AoE

        // Defense (24)
        new(new[] { 24, 20 }, 5), // Defense + Survival
        new(new[] { 24, 4 }, 5),  // Defense + Guard
        new(new[] { 24, 23 }, 5), // Defense + AoE
        new(new[] { 24, 2 }, 5),  // Defense + Ranged
        new(new[] { 24, 9 }, 5),  // Defense + Caster

        // Survival (20)
        new(new[] { 20, 6 }, 5), // Survival + Defender
        new(new[] { 20, 8 }, 5), // Survival + Supporter
        new(new[] { 20, 2 }, 4), // Survival + Ranged
        new(new[] { 20, 5 }, 4), // Survival + Sniper

        // Healing (16)
        new(new[] { 16, 9 }, 5),  // Healing + Caster
        new(new[] { 16, 18 }, 4), // Healing + DP-Recovery
        new(new[] { 16, 3 }, 4),  // Healing + Vanguard
        new(new[] { 16, 8 }, 4),  // Healing + Supporter

        // Ranged (2)
        new(new[] { 2, 18 }, 4), // Ranged + DP-Recovery
        new(new[] { 2, 3 }, 4),  // Ranged + Vanguard

        // Crowd-Control (14) -- already 4-star alone; these pairs upgrade to 5
        new(new[] { 14, 18 }, 5), // + DP-Recovery
        new(new[] { 14, 1 }, 5),  // + Melee
        new(new[] { 14, 3 }, 5),  // + Vanguard
        new(new[] { 14, 27 }, 5), // + Summon
        new(new[] { 14, 8 }, 5),  // + Supporter
        new(new[] { 14, 22 }, 5), // + Fast-Redeploy
        new(new[] { 14, 10 }, 5), // + Specialist
        new(new[] { 14, 25 }, 5), // + Slow

        // Debuff (21) -- already 4-star alone
        new(new[] { 21, 23 }, 5), // + AoE
        new(new[] { 21, 8 }, 5),  // + Supporter
        new(new[] { 21, 22 }, 5), // + Fast-Redeploy
        new(new[] { 21, 1 }, 5),  // + Melee
        new(new[] { 21, 10 }, 5), // + Specialist

        // Support (17) -- already 4-star alone
        new(new[] { 17, 18 }, 5), // + DP-Recovery
        new(new[] { 17, 3 }, 5),  // + Vanguard
        new(new[] { 17, 20 }, 5), // + Survival
        new(new[] { 17, 8 }, 5),  // + Supporter

        // Shift (26) -- already 4-star alone
        new(new[] { 26, 24 }, 5), // + Defense
        new(new[] { 26, 6 }, 5),  // + Defender
        new(new[] { 26, 19 }, 5), // + DPS
        new(new[] { 26, 25 }, 5), // + Slow

        // Nuker (15) -- already 4-star alone
        new(new[] { 15, 2 }, 5),  // + Ranged
        new(new[] { 15, 5 }, 5),  // + Sniper
        new(new[] { 15, 23 }, 5), // + AoE
        new(new[] { 15, 9 }, 5),  // + Caster

        // Specialist (10) -- already 4-star alone
        new(new[] { 10, 20 }, 5), // + Survival
        new(new[] { 10, 25 }, 5), // + Slow

        // Summon (27) -- already 4-star alone
        new(new[] { 27, 8 }, 5), // + Supporter
    };

    // Finds every known combo fully contained in the detected tag set (plus
    // the always-4-star-alone tags and the Top Operator 6-star rule), then
    // drops any match that's a strict subset of another match -- a bigger
    // matching set's guarantee is at least as good, so the smaller one is
    // redundant to show. Returned in descending rarity order.
    public static IReadOnlyList<ComboRarityEntry> FindQualifyingCombos(IReadOnlyCollection<int> detectedTagIds)
    {
        var detected = new HashSet<int>(detectedTagIds);
        var candidates = new List<ComboRarityEntry>();

        if (detected.Contains(TopOperatorTagId))
        {
            candidates.Add(new ComboRarityEntry(new[] { TopOperatorTagId }, 6));
        }

        foreach (var tagId in FourStarAloneTagIds)
        {
            if (detected.Contains(tagId))
            {
                candidates.Add(new ComboRarityEntry(new[] { tagId }, 4));
            }
        }

        foreach (var combo in KnownCombos)
        {
            if (combo.TagIds.All(detected.Contains))
            {
                candidates.Add(combo);
            }
        }

        return candidates
            .Where(candidate => !candidates.Any(other =>
                !ReferenceEquals(other, candidate) &&
                other.TagIds.Count > candidate.TagIds.Count &&
                candidate.TagIds.All(id => other.TagIds.Contains(id))))
            .OrderByDescending(m => m.Rarity)
            .ToList();
    }

    public static bool HasRobotTag(IReadOnlyCollection<int> detectedTagIds) => detectedTagIds.Contains(RobotTagId);
}
