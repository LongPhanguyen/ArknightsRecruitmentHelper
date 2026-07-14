using System.Collections.Generic;
using System.Linq;
using RecruitmentCore;
using Xunit;

namespace RecruitmentCore.Tests;

public class TagRarityRulesTests
{
    [Fact]
    public void ThreeTagComboSupersedesItsTwoTagSubsets()
    {
        // Slow (25), Caster (9), DPS (19): the 3-tag set is 5-star and should
        // suppress its own weaker 2-tag subsets (Slow+Caster, Slow+DPS -- both
        // individually 4-star) from also being reported.
        var detected = new HashSet<int> { 25, 9, 19 };

        var results = TagRarityRules.FindQualifyingCombos(detected);

        var top = results.First();
        Assert.Equal(new HashSet<int> { 25, 9, 19 }, top.TagIds.ToHashSet());
        Assert.Equal(5, top.Rarity);
        Assert.DoesNotContain(results, r => r.TagIds.ToHashSet().SetEquals(new HashSet<int> { 25, 9 }));
        Assert.DoesNotContain(results, r => r.TagIds.ToHashSet().SetEquals(new HashSet<int> { 25, 19 }));
    }

    [Fact]
    public void UnrelatedQualifyingPairIsNotSuppressedByAnotherMatch()
    {
        // Debuff (21) + Fast-Redeploy (22) is its own separate 5-star match,
        // unrelated to whatever else got detected -- it shouldn't disappear
        // just because something else in the set also qualifies.
        var detected = new HashSet<int> { 21, 22, 25, 4 }; // + Slow, Guard (Slow+Guard = 4-star)

        var results = TagRarityRules.FindQualifyingCombos(detected);

        Assert.Contains(results, r => r.TagIds.ToHashSet().SetEquals(new HashSet<int> { 21, 22 }) && r.Rarity == 5);
        Assert.Contains(results, r => r.TagIds.ToHashSet().SetEquals(new HashSet<int> { 25, 4 }) && r.Rarity == 4);
    }

    [Fact]
    public void FourStarAloneTagIsSuppressedWhenABetterComboContainsIt()
    {
        // Debuff (21) alone is 4-star, but Debuff + AoE (23) is 5-star --
        // the weaker "alone" entry shouldn't also show up redundantly.
        var detected = new HashSet<int> { 21, 23 };

        var results = TagRarityRules.FindQualifyingCombos(detected);

        Assert.Single(results);
        Assert.Equal(5, results[0].Rarity);
    }

    [Fact]
    public void TopOperatorAloneIsSixStar()
    {
        var results = TagRarityRules.FindQualifyingCombos(new HashSet<int> { TagRarityRules.TopOperatorTagId });

        Assert.Single(results);
        Assert.Equal(6, results[0].Rarity);
    }

    [Fact]
    public void RobotTagIsDetectedIndependently()
    {
        Assert.True(TagRarityRules.HasRobotTag(new HashSet<int> { TagRarityRules.RobotTagId, 1 }));
        Assert.False(TagRarityRules.HasRobotTag(new HashSet<int> { 1, 2 }));
    }
}
