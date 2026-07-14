namespace RecruitmentCore;

// A plain class (not a record) so equality is reference-based -- the same
// Operator instance flows through every tag-indexed set in RecruitmentCalculator,
// so identity comparison is exactly what we want.
public sealed class Operator
{
    public string Name { get; }
    public int Rarity { get; }
    public IReadOnlySet<int> TagIds { get; }

    public Operator(string name, int rarity, IReadOnlySet<int> tagIds)
    {
        Name = name;
        Rarity = rarity;
        TagIds = tagIds;
    }
}
