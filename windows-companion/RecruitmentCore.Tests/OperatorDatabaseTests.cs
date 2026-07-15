using System.Linq;
using RecruitmentCore;
using Xunit;

namespace RecruitmentCore.Tests;

// Regression checks for the embedded operator data snapshot (see
// OperatorDatabase.cs for provenance). These pin against the exact
// generated snapshot, so they'll need updating whenever the data is
// regenerated from a newer source pull.
public class OperatorDatabaseTests
{
    [Fact]
    public void LoadsTheEmbeddedSnapshot()
    {
        Assert.Equal(149, OperatorDatabase.AllOperators.Count);
    }

    [Fact]
    public void KnownOperatorHasExpectedTags()
    {
        var lancet2 = OperatorDatabase.AllOperators.Single(o => o.Name == "Lancet-2");

        Assert.Equal(1, lancet2.Rarity);
        // Ranged (2), Medic (7), Healing (16), Robot (28)
        Assert.Equal(new HashSet<int> { 2, 7, 16, 28 }, lancet2.TagIds);
    }

    [Fact]
    public void TopOperatorTagExactlyMatchesSixStarRarity()
    {
        var sixStar = OperatorDatabase.AllOperators.Where(o => o.Rarity == 6).Select(o => o.Name).ToHashSet();
        var topOperatorTagged = OperatorDatabase.AllOperators
            .Where(o => o.TagIds.Contains(TagRarityRules.TopOperatorTagId))
            .Select(o => o.Name)
            .ToHashSet();

        Assert.Equal(sixStar, topOperatorTagged);
    }

    [Fact]
    public void SeniorOperatorTaggedOperatorsAreAllFiveStar()
    {
        const int seniorOperatorTagId = 12;

        var seniorTagged = OperatorDatabase.AllOperators.Where(o => o.TagIds.Contains(seniorOperatorTagId));

        Assert.All(seniorTagged, o => Assert.Equal(5, o.Rarity));
    }
}
