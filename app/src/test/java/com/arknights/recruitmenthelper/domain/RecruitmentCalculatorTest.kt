package com.arknights.recruitmenthelper.domain

import com.arknights.recruitmenthelper.data.RecruitmentData
import org.junit.Assert.assertEquals
import org.junit.Test

class RecruitmentCalculatorTest {

    private val calculator = RecruitmentCalculator(RecruitmentData.ALL_OPERATORS)

    private fun tag(id: Int) = RecruitmentData.ALL_TAGS.first { it.id == id }

    @Test
    fun `a 5-star combo outranks its individually-4-star tags`() {
        // melee, ranged, vanguard, fast-redeploy, debuff
        val selected = listOf(tag(1), tag(2), tag(3), tag(22), tag(21))

        val results = calculator.evaluate(selected)

        val top = results.first()
        assertEquals(setOf(21, 22), top.tags.map { it.id }.toSet())
        assertEquals(5, top.floorRarity)

        val rangedVanguard = results.first { it.tags.map { t -> t.id }.toSet() == setOf(2, 3) }
        assertEquals(4, rangedVanguard.floorRarity)
    }
}
