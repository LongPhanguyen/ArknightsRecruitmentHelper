package com.arknights.recruitmenthelper.data

enum class TagCategory {
    POSITION,
    CLASS,
    QUALIFICATION,
    AFFIX,
}

data class Tag(
    val id: Int,
    val name: String,
    val category: TagCategory,
)
