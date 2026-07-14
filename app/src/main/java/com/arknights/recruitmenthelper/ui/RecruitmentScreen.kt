package com.arknights.recruitmenthelper.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.arknights.recruitmenthelper.data.Tag
import com.arknights.recruitmenthelper.data.TagCategory
import com.arknights.recruitmenthelper.domain.TagCombo

@Composable
fun RecruitmentScreen(
    modifier: Modifier = Modifier,
    viewModel: RecruitmentViewModel = viewModel(),
) {
    val uiState by viewModel.uiState.collectAsState()

    LazyColumn(
        modifier = modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            Text(
                "Select up to 5 tags (${uiState.selectedTagIds.size}/5)",
                style = MaterialTheme.typography.titleMedium,
            )
        }

        TagCategory.entries.forEach { category ->
            item {
                TagCategorySection(
                    category = category,
                    tags = uiState.allTags.filter { it.category == category },
                    selectedTagIds = uiState.selectedTagIds,
                    onTagClick = viewModel::toggleTag,
                )
            }
        }

        item { HorizontalDivider() }

        item { Text("Results", style = MaterialTheme.typography.titleMedium) }

        if (uiState.combos.isEmpty()) {
            item { Text("Pick some tags to see recruitment combinations.") }
        } else {
            items(uiState.combos) { combo -> TagComboRow(combo) }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun TagCategorySection(
    category: TagCategory,
    tags: List<Tag>,
    selectedTagIds: Set<Int>,
    onTagClick: (Int) -> Unit,
) {
    Column {
        Text(category.name, style = MaterialTheme.typography.labelLarge)
        FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            tags.forEach { tag ->
                FilterChip(
                    selected = tag.id in selectedTagIds,
                    onClick = { onTagClick(tag.id) },
                    label = { Text(tag.name) },
                )
            }
        }
    }
}

@Composable
private fun TagComboRow(combo: TagCombo) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(12.dp)) {
            Text(
                text = combo.tags.joinToString(" + ") { it.name },
                style = MaterialTheme.typography.bodyLarge,
            )
            val rarityLabel = if (combo.floorRarity == combo.ceilingRarity) {
                "${combo.floorRarity}★"
            } else {
                "${combo.floorRarity}★-${combo.ceilingRarity}★"
            }
            Text(
                text = if (combo.isGuaranteed) "Guaranteed $rarityLabel" else "Possible $rarityLabel",
                style = MaterialTheme.typography.bodyMedium,
            )
        }
    }
}
