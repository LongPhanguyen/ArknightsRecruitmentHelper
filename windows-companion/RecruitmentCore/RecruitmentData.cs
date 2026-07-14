namespace RecruitmentCore;

// Placeholder dataset -- mirrors the Android project's RecruitmentData.kt 1:1
// (same tag IDs/names, same fake operators) so both apps agree until the real
// tag list and operator data are available. Swap this out once they are.
public static class RecruitmentData
{
    public static readonly IReadOnlyList<Tag> AllTags = new List<Tag>
    {
        new(1, "Melee", TagCategory.Position),
        new(2, "Ranged", TagCategory.Position),
        new(3, "Vanguard", TagCategory.Class),
        new(4, "Guard", TagCategory.Class),
        new(5, "Sniper", TagCategory.Class),
        new(6, "Defender", TagCategory.Class),
        new(7, "Medic", TagCategory.Class),
        new(8, "Supporter", TagCategory.Class),
        new(9, "Caster", TagCategory.Class),
        new(10, "Specialist", TagCategory.Class),
        new(11, "Starter", TagCategory.Qualification),
        new(12, "Senior Operator", TagCategory.Qualification),
        new(13, "Top Operator", TagCategory.Qualification),
        new(14, "Crowd-Control", TagCategory.Affix),
        new(15, "Nuker", TagCategory.Affix),
        new(16, "Healing", TagCategory.Affix),
        new(17, "Support", TagCategory.Affix),
        new(18, "DP-Recovery", TagCategory.Affix),
        new(19, "DPS", TagCategory.Affix),
        new(20, "Survival", TagCategory.Affix),
        new(21, "Debuff", TagCategory.Affix),
        new(22, "Fast-Redeploy", TagCategory.Affix),
        new(23, "AoE", TagCategory.Affix),
        new(24, "Defense", TagCategory.Affix),
        new(25, "Slow", TagCategory.Affix),
        new(26, "Shift", TagCategory.Affix),
        new(27, "Summon", TagCategory.Affix),
        new(28, "Robot", TagCategory.Affix),
        new(29, "Elemental", TagCategory.Affix),
    };

    public static readonly IReadOnlyList<Operator> AllOperators = new List<Operator>
    {
        new("Sample Op A", 1, new HashSet<int> { 1, 3 }),
        new("Sample Op B", 2, new HashSet<int> { 1 }),
        new("Sample Op C", 3, new HashSet<int> { 2 }),
        new("Sample Op D", 4, new HashSet<int> { 2, 3 }),
        new("Sample Op E", 4, new HashSet<int> { 21 }),
        new("Sample Op F", 5, new HashSet<int> { 21 }),
        new("Sample Op G", 4, new HashSet<int> { 22 }),
        new("Sample Op H", 5, new HashSet<int> { 22 }),
        new("Sample Op I", 5, new HashSet<int> { 21, 22 }),
    };
}
