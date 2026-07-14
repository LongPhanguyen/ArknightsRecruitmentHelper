package com.arknights.recruitmenthelper.data

// Placeholder dataset. Swap ALL_TAGS/ALL_OPERATORS for the real tag list and a
// bundled operator list (e.g. loaded from an assets/operators.json) once available —
// RecruitmentCalculator only depends on the Tag/Operator shapes, not on how they're sourced.
object RecruitmentData {

    val ALL_TAGS = listOf(
        Tag(1, "Melee", TagCategory.POSITION),
        Tag(2, "Ranged", TagCategory.POSITION),
        Tag(3, "Vanguard", TagCategory.CLASS),
        Tag(4, "Guard", TagCategory.CLASS),
        Tag(5, "Sniper", TagCategory.CLASS),
        Tag(6, "Defender", TagCategory.CLASS),
        Tag(7, "Medic", TagCategory.CLASS),
        Tag(8, "Supporter", TagCategory.CLASS),
        Tag(9, "Caster", TagCategory.CLASS),
        Tag(10, "Specialist", TagCategory.CLASS),
        Tag(11, "Starter", TagCategory.QUALIFICATION),
        Tag(12, "Senior Operator", TagCategory.QUALIFICATION),
        Tag(13, "Top Operator", TagCategory.QUALIFICATION),
        Tag(14, "Crowd-Control", TagCategory.AFFIX),
        Tag(15, "Nuker", TagCategory.AFFIX),
        Tag(16, "Healing", TagCategory.AFFIX),
        Tag(17, "Support", TagCategory.AFFIX),
        Tag(18, "DP-Recovery", TagCategory.AFFIX),
        Tag(19, "DPS", TagCategory.AFFIX),
        Tag(20, "Survival", TagCategory.AFFIX),
        Tag(21, "Debuff", TagCategory.AFFIX),
        Tag(22, "Fast-Redeploy", TagCategory.AFFIX),
        Tag(23, "AoE", TagCategory.AFFIX),
        Tag(24, "Defense", TagCategory.AFFIX),
        Tag(25, "Slow", TagCategory.AFFIX),
        Tag(26, "Shift", TagCategory.AFFIX),
        Tag(27, "Summon", TagCategory.AFFIX),
        Tag(28, "Robot", TagCategory.AFFIX),
        Tag(29, "Elemental", TagCategory.AFFIX),
    )

    // Just enough fake operators to exercise every branch of the calculator:
    // - Melee/Vanguard alone pull in a 1-star op, so neither is "guaranteed".
    // - Debuff alone and Fast-Redeploy alone are each guaranteed 4-star.
    // - Ranged+Vanguard only intersects on one 4-star op -> guaranteed 4-star combo.
    // - Debuff+Fast-Redeploy only intersects on one 5-star op -> guaranteed 5-star combo,
    //   which should outrank all of the above when evaluated together.
    val ALL_OPERATORS = listOf(
        Operator("Sample Op A", rarity = 1, tagIds = setOf(1, 3)),
        Operator("Sample Op B", rarity = 2, tagIds = setOf(1)),
        Operator("Sample Op C", rarity = 3, tagIds = setOf(2)),
        Operator("Sample Op D", rarity = 4, tagIds = setOf(2, 3)),
        Operator("Sample Op E", rarity = 4, tagIds = setOf(21)),
        Operator("Sample Op F", rarity = 5, tagIds = setOf(21)),
        Operator("Sample Op G", rarity = 4, tagIds = setOf(22)),
        Operator("Sample Op H", rarity = 5, tagIds = setOf(22)),
        Operator("Sample Op I", rarity = 5, tagIds = setOf(21, 22)),
    )
}
