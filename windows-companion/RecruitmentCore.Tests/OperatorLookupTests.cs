using System.Collections.Generic;
using RecruitmentCore;
using Xunit;

namespace RecruitmentCore.Tests;

public class OperatorLookupTests
{
    [Fact]
    public void ReturnsOnlyOperatorsWhoseTagsAreFullySubsetOfDetected()
    {
        // Fixture-only fake operators, not real game data -- just exercising
        // the subset-matching logic.
        var roster = new List<Operator>
        {
            new("Fixture A", 4, new HashSet<int> { 1, 3 }),
            new("Fixture B", 5, new HashSet<int> { 1, 3, 21 }),
            new("Fixture C", 4, new HashSet<int> { 2 }),
        };

        var detected = new HashSet<int> { 1, 3 };

        var results = OperatorLookup.FindPossibleOperators(roster, detected);

        Assert.Contains(results, o => o.Name == "Fixture A");
        Assert.DoesNotContain(results, o => o.Name == "Fixture B"); // needs tag 21 too, not detected
        Assert.DoesNotContain(results, o => o.Name == "Fixture C"); // needs tag 2, not detected
    }

    [Fact]
    public void EmptyDatabaseReturnsNoResults()
    {
        var results = OperatorLookup.FindPossibleOperators(OperatorDatabase.AllOperators, new HashSet<int> { 1, 2, 3 });

        Assert.Empty(results);
    }
}
