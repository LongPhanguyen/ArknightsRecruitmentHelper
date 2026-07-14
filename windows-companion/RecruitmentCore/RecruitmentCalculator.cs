namespace RecruitmentCore;

public record TagCombo(
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<Operator> MatchedOperators,
    int FloorRarity,
    int CeilingRarity)
{
    public bool IsGuaranteed => FloorRarity >= 4;
}

// Direct port of the Kotlin RecruitmentCalculator: enumerate every 1-3 tag
// subset of the selection, intersect each subset's matching operators, then
// rank by guaranteed floor rarity (highest first).
public class RecruitmentCalculator
{
    private const int MaxComboSize = 3;

    private readonly Dictionary<int, HashSet<Operator>> _operatorsByTag;

    public RecruitmentCalculator(IEnumerable<Operator> operators)
    {
        _operatorsByTag = new Dictionary<int, HashSet<Operator>>();
        foreach (var op in operators)
        {
            foreach (var tagId in op.TagIds)
            {
                if (!_operatorsByTag.TryGetValue(tagId, out var set))
                {
                    set = new HashSet<Operator>();
                    _operatorsByTag[tagId] = set;
                }
                set.Add(op);
            }
        }
    }

    public List<TagCombo> Evaluate(IReadOnlyList<Tag> selectedTags)
    {
        if (selectedTags.Count > 5)
            throw new ArgumentException("Arknights recruitment allows at most 5 selected tags");

        var combos = new List<TagCombo>();
        for (var size = 1; size <= MaxComboSize; size++)
        {
            foreach (var combo in Combinations(selectedTags, size))
            {
                var evaluated = EvaluateCombo(combo);
                if (evaluated is not null) combos.Add(evaluated);
            }
        }

        combos.Sort((a, b) =>
        {
            var cmp = b.FloorRarity.CompareTo(a.FloorRarity);
            if (cmp != 0) return cmp;
            cmp = b.CeilingRarity.CompareTo(a.CeilingRarity);
            if (cmp != 0) return cmp;
            return a.Tags.Count.CompareTo(b.Tags.Count);
        });

        return combos;
    }

    private TagCombo? EvaluateCombo(IReadOnlyList<Tag> tags)
    {
        HashSet<Operator>? matched = null;
        foreach (var tag in tags)
        {
            var set = _operatorsByTag.GetValueOrDefault(tag.Id) ?? new HashSet<Operator>();
            matched = matched is null ? new HashSet<Operator>(set) : new HashSet<Operator>(matched.Intersect(set));
        }

        if (matched is null || matched.Count == 0) return null;

        return new TagCombo(
            Tags: tags.ToList(),
            MatchedOperators: matched.OrderByDescending(o => o.Rarity).ToList(),
            FloorRarity: matched.Min(o => o.Rarity),
            CeilingRarity: matched.Max(o => o.Rarity));
    }

    private static IEnumerable<List<T>> Combinations<T>(IReadOnlyList<T> items, int size)
    {
        if (size == 0)
        {
            yield return new List<T>();
            yield break;
        }
        if (size > items.Count) yield break;

        for (var i = 0; i <= items.Count - size; i++)
        {
            var rest = items.Skip(i + 1).ToList();
            foreach (var tail in Combinations(rest, size - 1))
            {
                var combo = new List<T> { items[i] };
                combo.AddRange(tail);
                yield return combo;
            }
        }
    }
}
