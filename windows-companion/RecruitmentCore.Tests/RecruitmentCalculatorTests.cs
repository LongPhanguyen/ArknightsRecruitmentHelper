using System.Collections.Generic;
using System.Linq;
using RecruitmentCore;
using Xunit;

namespace RecruitmentCore.Tests;

public class RecruitmentCalculatorTests
{
    private readonly RecruitmentCalculator _calculator = new(RecruitmentData.AllOperators);

    private static Tag Tag(int id) => RecruitmentData.AllTags.First(t => t.Id == id);

    [Fact]
    public void FiveStarComboOutranksIndividuallyFourStarTags()
    {
        // melee, ranged, vanguard, fast-redeploy, debuff
        var selected = new List<Tag> { Tag(1), Tag(2), Tag(3), Tag(22), Tag(21) };

        var results = _calculator.Evaluate(selected);

        var top = results.First();
        Assert.Equal(new HashSet<int> { 21, 22 }, top.Tags.Select(t => t.Id).ToHashSet());
        Assert.Equal(5, top.FloorRarity);

        var rangedVanguard = results.First(c =>
            c.Tags.Select(t => t.Id).ToHashSet().SetEquals(new HashSet<int> { 2, 3 }));
        Assert.Equal(4, rangedVanguard.FloorRarity);
    }
}
