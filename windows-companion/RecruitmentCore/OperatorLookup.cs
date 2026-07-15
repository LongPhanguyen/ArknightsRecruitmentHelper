using System.Collections.Generic;
using System.Linq;

namespace RecruitmentCore;

public static class OperatorLookup
{
    // Returns every operator whose full tag set is contained within the
    // detected tags -- i.e. operators that could actually appear in a
    // recruitment showing exactly these tags.
    public static IReadOnlyList<Operator> FindPossibleOperators(
        IEnumerable<Operator> operators, IReadOnlyCollection<int> detectedTagIds)
    {
        var detected = new HashSet<int>(detectedTagIds);
        return operators
            .Where(op => op.TagIds.All(detected.Contains))
            .ToList();
    }
}
