using System.Collections.Generic;

namespace RecruitmentCore;

// Intentionally EMPTY. Populating this needs a real, up-to-date operator
// roster (name + rarity + recruitment tags) for several hundred operators,
// which changes as new operators release -- that data has to come from an
// external source (e.g. a wiki data export or an existing community
// database/API), not be hand-written or guessed here. Fabricating
// operator-to-tag mappings would make OperatorLookup's results wrong in a
// way that's hard to notice later.
//
// To populate: import a real data export into AllOperators (e.g. parse a
// bundled JSON file), keeping the Tag/Operator shapes this project already
// uses. This is distinct from RecruitmentData.AllOperators, which is
// placeholder fixture data for RecruitmentCalculator's tests, not meant to
// represent real operators.
public static class OperatorDatabase
{
    public static readonly IReadOnlyList<Operator> AllOperators = new List<Operator>();
}
