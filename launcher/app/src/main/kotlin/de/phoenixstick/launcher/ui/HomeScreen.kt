package de.phoenixstick.launcher.ui

import androidx.compose.animation.Crossfade
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Path
import de.phoenixstick.launcher.data.AppInfo
import de.phoenixstick.launcher.data.Category
import de.phoenixstick.launcher.data.Profile
import de.phoenixstick.launcher.theme.px
import de.phoenixstick.launcher.theme.theme
import de.phoenixstick.launcher.ui.components.AppTile
import de.phoenixstick.launcher.ui.components.FilterChip
import de.phoenixstick.launcher.ui.components.Icon
import de.phoenixstick.launcher.ui.components.PrimaryButton
import de.phoenixstick.launcher.ui.components.RoundIconButton
import de.phoenixstick.launcher.ui.components.SvgImage
import de.phoenixstick.launcher.ui.components.ThemedText
import de.phoenixstick.launcher.ui.components.dpadClickable

/** Reihenfolge der App-Reihe: Favoriten zuerst, dann alle anderen alphabetisch. */
fun rowApps(apps: List<AppInfo>, profile: Profile, tab: Tab, filter: Category): List<AppInfo> =
    apps
        .filter { tab != Tab.FAVORITES || profile.isFavorite(it.packageName) }
        .filter { filter == Category.ALL || profile.categoryOf(it.packageName) == filter }
        .sortedWith(compareByDescending<AppInfo> { profile.isFavorite(it.packageName) }.thenBy { it.label.lowercase() })

@Composable
fun HomeScreen(
    apps: List<AppInfo>,
    profile: Profile,
    tab: Tab,
    filter: Category,
    onTab: (Tab) -> Unit,
    onFilter: (Category) -> Unit,
    onLaunch: (AppInfo) -> Unit,
    onToggleFavorite: (AppInfo) -> Unit,
    onMenu: (AppInfo) -> Unit,
    onProfiles: () -> Unit,
    onSettings: () -> Unit,
) {
    val row = rowApps(apps, profile, tab, filter)
    var stageApp by remember(profile.id) { mutableStateOf(row.firstOrNull { it.packageName == profile.lastLaunched } ?: row.firstOrNull()) }
    val firstTile = remember { FocusRequester() }

    LaunchedEffect(profile.id, tab) {
        // Start-Fokus: erste Kachel der App-Reihe
        runCatching { firstTile.requestFocus() }
    }

    ThemedBackground {
        Row(Modifier.fillMaxSize()) {
            Rail(tab, onSelect = onTab, onSettings = onSettings)
            Column(
                Modifier
                    .weight(1f)
                    .padding(top = Layout.MAIN_TOP.px, start = Layout.MAIN_LEFT.px, bottom = Layout.MAIN_BOTTOM.px),
                verticalArrangement = Arrangement.spacedBy(Layout.BLOCK_GAP.px),
            ) {
                Header(tab, profile.letter, onTab = onTab, onAvatar = onProfiles)
                Stage(stageApp, profile, onLaunch, onToggleFavorite)
                AppRow(
                    title = if (tab == Tab.FAVORITES) "Favoriten" else "Deine Apps",
                    apps = row,
                    profile = profile,
                    firstTile = firstTile,
                    onFocus = { stageApp = it },
                    onLaunch = onLaunch,
                    onMenu = onMenu,
                    onViewAll = { onTab(Tab.ALL_APPS) },
                )
                Spacer(Modifier.weight(1f))
                Chips(filter, onFilter)
            }
        }
    }
}

@Composable
private fun Stage(app: AppInfo?, profile: Profile, onLaunch: (AppInfo) -> Unit, onToggleFavorite: (AppInfo) -> Unit) {
    val t = theme()
    val shape = RoundedCornerShape(t.shape.heroCardRadius.px)
    Box(
        Modifier
            .padding(end = Layout.MAIN_RIGHT.px)
            .fillMaxWidth()
            .height(Layout.STAGE_HEIGHT.px)
            .clip(shape)
            .background(t.color("heroCard"))
            .border(1.px, t.color("borderSubtle"), shape),
    ) {
        SvgImage(t.tokens.heroImage.file, Modifier.fillMaxSize())
        StageCover()
        Crossfade(app, animationSpec = tween(t.motion.heroCrossfadeMs, easing = t.easing), label = "stage") { shown ->
            if (shown != null) StageContent(shown, profile, onLaunch, onToggleFavorite)
        }
        RecentDots(profile, app)
    }
}

/** Schräg abgedeckte Fläche links, damit der Text auf dem Theme-Bild lesbar bleibt. */
@Composable
private fun StageCover() {
    val color = theme().color("heroCard")
    Canvas(Modifier.width(Layout.STAGE_COVER_WIDTH.px).height(Layout.STAGE_HEIGHT.px)) {
        val path = Path().apply {
            moveTo(0f, 0f)
            lineTo(size.width, 0f)
            lineTo(size.width * Layout.STAGE_COVER_SLANT, size.height)
            lineTo(0f, size.height)
            close()
        }
        drawPath(path, color, alpha = 0.92f)
    }
}

@Composable
private fun StageContent(app: AppInfo, profile: Profile, onLaunch: (AppInfo) -> Unit, onToggleFavorite: (AppInfo) -> Unit) {
    val t = theme()
    val isLast = app.packageName == profile.lastLaunched
    val favorite = profile.isFavorite(app.packageName)
    val category = profile.categoryOf(app.packageName)
    Column(
        Modifier
            .padding(start = Layout.STAGE_TEXT_INSET.px, top = Layout.STAGE_TEXT_INSET.px)
            .width(Layout.STAGE_TEXT_WIDTH.px),
        verticalArrangement = Arrangement.spacedBy(16.px),
    ) {
        ThemedText(t.type.eyebrow, if (isLast) "Zuletzt geöffnet" else category?.label ?: "App")
        ThemedText(t.type.heroTitle, app.label)
        ThemedText(
            t.type.heroSub,
            when {
                isLast -> "Deine zuletzt gestartete App."
                favorite -> "Einer deiner Favoriten."
                else -> "Installiert auf diesem Gerät."
            },
        )
        Row(horizontalArrangement = Arrangement.spacedBy(18.px), verticalAlignment = Alignment.CenterVertically) {
            ThemedText(t.type.heroMeta, sinceText(profile.lastLaunch(app.packageName)))
            category?.let {
                ThemedText(t.type.heroMeta, "·")
                ThemedText(t.type.heroMeta, it.label)
            }
            if (favorite) {
                val tagShape = RoundedCornerShape(6.px)
                Box(Modifier.border(1.px, t.color("borderStrong"), tagShape).padding(horizontal = 12.px, vertical = 4.px)) {
                    ThemedText(t.type.heroMeta, "Favorit")
                }
            }
        }
        Row(
            Modifier.padding(top = 18.px),
            horizontalArrangement = Arrangement.spacedBy(20.px),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            PrimaryButton("Öffnen", Icon.Play, onClick = { onLaunch(app) })
            RoundIconButton(if (favorite) Icon.Minus else Icon.Plus, onClick = { onToggleFavorite(app) })
        }
    }
}

/** Punkte unten rechts für die zuletzt geöffneten Apps, die gezeigte App in Akzentfarbe. */
@Composable
private fun RecentDots(profile: Profile, shown: AppInfo?) {
    val t = theme()
    val recent = profile.history.take(4)
    if (recent.isEmpty()) return
    Box(Modifier.fillMaxSize().padding(end = 48.px, bottom = 36.px), contentAlignment = Alignment.BottomEnd) {
        Row(horizontalArrangement = Arrangement.spacedBy(10.px)) {
            recent.forEach { entry ->
                Box(
                    Modifier
                        .size(Layout.STAGE_DOT_WIDTH.px, Layout.STAGE_DOT_HEIGHT.px)
                        .clip(RoundedCornerShape(2.px))
                        .background(t.color(if (entry.packageName == shown?.packageName) "accent" else "borderStrong")),
                )
            }
        }
    }
}

@Composable
private fun AppRow(
    title: String,
    apps: List<AppInfo>,
    profile: Profile,
    firstTile: FocusRequester,
    onFocus: (AppInfo) -> Unit,
    onLaunch: (AppInfo) -> Unit,
    onMenu: (AppInfo) -> Unit,
    onViewAll: () -> Unit,
) {
    val t = theme()
    Column(verticalArrangement = Arrangement.spacedBy(Layout.SECTION_GAP.px)) {
        Row(
            Modifier.fillMaxWidth().padding(end = Layout.MAIN_RIGHT.px),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            ThemedText(t.type.sectionTitle, title)
            Spacer(Modifier.weight(1f))
            ViewAll(onViewAll)
        }
        if (apps.isEmpty()) {
            ThemedText(t.type.hint, if (title == "Favoriten") "Noch keine Favoriten. Menütaste auf einer Kachel: Favorit an." else "Keine Apps in dieser Kategorie.", colorName = "textMuted")
            return@Column
        }
        // Zusätzlicher Rand oben und unten, sonst schneidet die Reihe Schein und Eck-Klammern ab
        LazyRow(
            Modifier.offset(y = (-Layout.FOCUS_ROOM).px).height((Layout.TILE_HEIGHT + 2 * Layout.FOCUS_ROOM).px),
            contentPadding = PaddingValues(vertical = Layout.FOCUS_ROOM.px, horizontal = Layout.FOCUS_ROOM.px / 2),
            horizontalArrangement = Arrangement.spacedBy(Layout.TILE_GAP.px),
        ) {
            items(apps, key = { it.packageName }) { app ->
                AppTile(
                    app = app,
                    widthPx = Layout.TILE_WIDTH,
                    heightPx = Layout.TILE_HEIGHT,
                    showLastTag = app.packageName == profile.lastLaunched,
                    onClick = { onLaunch(app) },
                    onMenu = { onMenu(app) },
                    onFocus = { onFocus(app) },
                    modifier = if (app == apps.first()) Modifier.focusRequester(firstTile) else Modifier,
                )
            }
        }
    }
}

@Composable
private fun ViewAll(onClick: () -> Unit) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    Box(Modifier.dpadClickable(interaction, onClick)) {
        ThemedText(t.type.viewAll.copy(suffix = " ›", suffixColor = "accent"), "Alle anzeigen", colorName = if (focused) "textPrimary" else null)
    }
}

@Composable
fun Chips(active: Category, onSelect: (Category) -> Unit) {
    Row(horizontalArrangement = Arrangement.spacedBy(Layout.CHIP_GAP.px)) {
        Category.entries.forEach { category -> FilterChip(category.label, category == active, onClick = { onSelect(category) }) }
    }
}

/** "vor 2 Std." aus dem eigenen Startverlauf, nichts erfunden. */
fun sinceText(time: Long?, now: Long = System.currentTimeMillis()): String {
    if (time == null) return "Noch nicht geöffnet"
    val minutes = (now - time) / 60_000
    return when {
        minutes < 1 -> "Gerade eben"
        minutes < 60 -> "Vor $minutes Min."
        minutes < 60 * 24 -> "Vor ${minutes / 60} Std."
        else -> "Vor ${minutes / (60 * 24)} Tg."
    }
}
