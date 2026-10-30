package de.phoenixstick.launcher.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import de.phoenixstick.launcher.data.AppInfo
import de.phoenixstick.launcher.data.Category
import de.phoenixstick.launcher.data.Profile
import de.phoenixstick.launcher.theme.px
import de.phoenixstick.launcher.theme.theme
import de.phoenixstick.launcher.ui.components.AppTile
import de.phoenixstick.launcher.ui.components.ThemedText

@Composable
fun AllAppsScreen(
    apps: List<AppInfo>,
    profile: Profile,
    filter: Category,
    onTab: (Tab) -> Unit,
    onFilter: (Category) -> Unit,
    onLaunch: (AppInfo) -> Unit,
    onMenu: (AppInfo) -> Unit,
    onProfiles: () -> Unit,
    onSettings: () -> Unit,
) {
    val t = theme()
    // Alle Apps: alphabetisch, ohne Favoriten-Vorrang
    val shown = apps.filter { filter == Category.ALL || profile.categoryOf(it.packageName) == filter }
    val first = remember { FocusRequester() }
    LaunchedEffect(profile.id) { runCatching { first.requestFocus() } }

    ThemedBackground {
        Row(Modifier.fillMaxSize()) {
            Rail(Tab.ALL_APPS, onSelect = onTab, onSettings = onSettings)
            Column(
                Modifier.weight(1f).padding(top = Layout.MAIN_TOP.px, start = Layout.MAIN_LEFT.px),
                verticalArrangement = Arrangement.spacedBy(Layout.BLOCK_GAP.px),
            ) {
                Header(Tab.ALL_APPS, profile.letter, onTab = onTab, onAvatar = onProfiles)
                Chips(filter, onFilter)
                if (shown.isEmpty()) {
                    ThemedText(t.type.hint, "Keine Apps in dieser Kategorie.", colorName = "textMuted")
                }
                LazyVerticalGrid(
                    columns = GridCells.Fixed(Layout.GRID_COLUMNS),
                    contentPadding = PaddingValues(Layout.FOCUS_ROOM.px / 2, Layout.FOCUS_ROOM.px / 2, Layout.MAIN_RIGHT.px, Layout.FOCUS_ROOM.px),
                    horizontalArrangement = Arrangement.spacedBy(Layout.TILE_GAP.px),
                    verticalArrangement = Arrangement.spacedBy(Layout.TILE_GAP.px),
                ) {
                    items(shown, key = { it.packageName }) { app ->
                        AppTile(
                            app = app,
                            widthPx = Layout.GRID_TILE_WIDTH,
                            heightPx = Layout.GRID_TILE_HEIGHT,
                            showLastTag = app.packageName == profile.lastLaunched,
                            onClick = { onLaunch(app) },
                            onMenu = { onMenu(app) },
                            onFocus = {},
                            modifier = if (app == shown.first()) Modifier.focusRequester(first) else Modifier,
                        )
                    }
                }
            }
        }
    }
}
