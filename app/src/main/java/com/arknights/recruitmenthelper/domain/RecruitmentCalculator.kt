package com.arknights.recruitmenthelper.domain

import com.arknights.recruitmenthelper.data.Operator
import com.arknights.recruitmenthelper.data.Tag

data class TagCombo(
    val tags: List<Tag>,
    val matchedOperators: List<Operator>,
    val floorRarity: Int,
    val ceilingRarity: Int,
) {
    val isGuaranteed: Boolean get() = floorRarity >= 4
}

class RecruitmentCalculator(operators: List<Operator>) {

    private val operatorsByTag: Map<Int, Set<Operator>> =
        operators
            .flatMap { operator -> operator.tagIds.map { tagId -> tagId to operator } }
            .groupBy({ it.first }, { it.second })
            .mapValues { it.value.toSet() }

    // Arknights recruitment only honors combinations of 1-3 selected tags.
    fun evaluate(selectedTags: List<Tag>): List<TagCombo> {
        require(selectedTags.size <= 5) { "Arknights recruitment allows at most 5 selected tags" }

        return (1..MAX_COMBO_SIZE)
            .flatMap { size -> selectedTags.combinations(size) }
            .mapNotNull(::evaluateCombo)
            .sortedWith(
                compareByDescending<TagCombo> { it.floorRarity }
                    .thenByDescending { it.ceilingRarity }
                    .thenBy { it.tags.size }
            )
    }

    private fun evaluateCombo(tags: List<Tag>): TagCombo? {
        val matched = tags
            .map { operatorsByTag[it.id].orEmpty() }
            .reduce { acc, set -> acc.intersect(set) }

        if (matched.isEmpty()) return null

        return TagCombo(
            tags = tags,
            matchedOperators = matched.sortedByDescending { it.rarity },
            floorRarity = matched.minOf { it.rarity },
            ceilingRarity = matched.maxOf { it.rarity },
        )
    }

    private companion object {
        const val MAX_COMBO_SIZE = 3
    }
}

private fun <T> List<T>.combinations(size: Int): List<List<T>> {
    if (size == 0) return listOf(emptyList())
    if (size > this.size) return emptyList()
    return indices.flatMap { i ->
        drop(i + 1).combinations(size - 1).map { rest -> listOf(this[i]) + rest }
    }
}
