package com.arknights.recruitmenthelper.ui

import androidx.lifecycle.ViewModel
import com.arknights.recruitmenthelper.data.RecruitmentData
import com.arknights.recruitmenthelper.data.Tag
import com.arknights.recruitmenthelper.domain.RecruitmentCalculator
import com.arknights.recruitmenthelper.domain.TagCombo
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

data class RecruitmentUiState(
    val allTags: List<Tag> = RecruitmentData.ALL_TAGS,
    val selectedTagIds: Set<Int> = emptySet(),
    val combos: List<TagCombo> = emptyList(),
)

class RecruitmentViewModel : ViewModel() {

    private val calculator = RecruitmentCalculator(RecruitmentData.ALL_OPERATORS)

    private val _uiState = MutableStateFlow(RecruitmentUiState())
    val uiState: StateFlow<RecruitmentUiState> = _uiState.asStateFlow()

    fun toggleTag(tagId: Int) {
        _uiState.update { state ->
            val newSelection = when {
                tagId in state.selectedTagIds -> state.selectedTagIds - tagId
                state.selectedTagIds.size >= MAX_SELECTED_TAGS -> return@update state
                else -> state.selectedTagIds + tagId
            }
            state.copy(selectedTagIds = newSelection, combos = evaluate(newSelection))
        }
    }

    fun clearSelection() {
        _uiState.update { it.copy(selectedTagIds = emptySet(), combos = emptyList()) }
    }

    // Entry point for wiring in detected tags (e.g. from the planned OCR pipeline)
    // without going through individual toggles.
    fun setSelection(tagIds: Set<Int>) {
        val bounded = tagIds.take(MAX_SELECTED_TAGS).toSet()
        _uiState.update { it.copy(selectedTagIds = bounded, combos = evaluate(bounded)) }
    }

    private fun evaluate(tagIds: Set<Int>): List<TagCombo> {
        if (tagIds.isEmpty()) return emptyList()
        val tags = _uiState.value.allTags.filter { it.id in tagIds }
        return calculator.evaluate(tags)
    }

    private companion object {
        const val MAX_SELECTED_TAGS = 5
    }
}
