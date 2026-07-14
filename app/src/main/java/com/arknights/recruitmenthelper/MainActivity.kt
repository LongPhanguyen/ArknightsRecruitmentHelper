package com.arknights.recruitmenthelper

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.ui.Modifier
import com.arknights.recruitmenthelper.ui.RecruitmentScreen
import com.arknights.recruitmenthelper.ui.theme.ArknightsRecruitmentHelperTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            ArknightsRecruitmentHelperTheme {
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    RecruitmentScreen(modifier = Modifier.padding(innerPadding))
                }
            }
        }
    }
}