package de.phoenixstick.launcher.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.focusable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import de.phoenixstick.launcher.theme.px
import de.phoenixstick.launcher.theme.theme
import de.phoenixstick.launcher.ui.components.Icon
import de.phoenixstick.launcher.ui.components.LineIcon
import de.phoenixstick.launcher.ui.components.ThemedText
import de.phoenixstick.launcher.ui.components.dpadClickable
import de.phoenixstick.launcher.ui.components.focusFrame
import kotlinx.coroutines.delay
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

enum class Tab(val label: String) { START("Start"), FAVORITES("Favoriten"), ALL_APPS("Alle Apps") }

/** Fläche eines Bildschirms. Hintergrund und Hintergrundbild zeichnet ProvideLauncherTheme über den ganzen Bildschirm. */
@Composable
fun ThemedBackground(content: @Composable () -> Unit) {
    Box(Modifier.fillMaxSize()) { content() }
}

@Composable
fun Rail(active: Tab, onSelect: (Tab) -> Unit, onSettings: () -> Unit) {
    val t = theme()
    Column(
        Modifier
            .width(Layout.RAIL_WIDTH.px)
            .fillMaxHeight()
            .background(t.color("rail"))
            .drawBehind {
                drawLine(t.color("railBorder"), Offset(size.width, 0f), Offset(size.width, size.height), 1f)
            }
            .padding(top = Layout.RAIL_PADDING_TOP.px, bottom = Layout.RAIL_PADDING_BOTTOM.px),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(Layout.RAIL_GAP.px),
    ) {
        Box(Modifier.size(64.px).padding(bottom = 0.px), contentAlignment = Alignment.Center) {
            LineIcon(Icon.Logo, Layout.RAIL_LOGO.px, t.color("accent"))
        }
        Spacer(Modifier.height(36.px))
        RailItem(Icon.Home, active == Tab.START) { onSelect(Tab.START) }
        RailItem(Icon.Favorites, active == Tab.FAVORITES) { onSelect(Tab.FAVORITES) }
        RailItem(Icon.AllApps, active == Tab.ALL_APPS) { onSelect(Tab.ALL_APPS) }
        // Eingänge kommen in einem späteren Goal, bis dahin nur angedeutet
        RailItem(Icon.Inputs, active = false, enabled = false) {}
        Box(Modifier.padding(vertical = 8.px).width(40.px).height(1.px).background(t.color("borderSubtle")))
        Spacer(Modifier.weight(1f))
        RailItem(Icon.Settings, active = false, onClick = onSettings)
    }
}

@Composable
private fun RailItem(icon: Icon, active: Boolean, enabled: Boolean = true, onClick: () -> Unit) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val shape = RoundedCornerShape(t.shape.railItemRadius.px)
    Box(Modifier.size(Layout.RAIL_ITEM.px), contentAlignment = Alignment.Center) {
        if (active) {
            Box(
                Modifier
                    .align(Alignment.CenterStart)
                    .offset(x = Layout.RAIL_INDICATOR_X.px)
                    .size(t.shape.railActiveIndicator.width.px, t.shape.railActiveIndicator.height.px)
                    .clip(RoundedCornerShape(2.px))
                    .background(t.color("accent")),
            )
        }
        val base = Modifier
            .size(Layout.RAIL_ITEM.px)
            .focusFrame(focused, shape, button = true)
            .clip(shape)
            .background(t.color(if (focused) "surfaceFocus" else if (active) "accentSoft" else "rail"))
            .border(if (focused) t.focus.buttonRing.width.px else 0.px, t.color(t.focus.buttonRing.color), shape)
        Box(
            if (enabled) base.dpadClickable(interaction, onClick) else base.alpha(0.4f),
            contentAlignment = Alignment.Center,
        ) {
            LineIcon(icon, Layout.RAIL_ICON.px, t.color(if (active || focused) "textPrimary" else "textMuted"))
        }
    }
}

@Composable
fun Header(active: Tab, avatarLetter: String, onTab: (Tab) -> Unit, onAvatar: () -> Unit) {
    val t = theme()
    Row(
        Modifier.fillMaxWidth().padding(end = Layout.MAIN_RIGHT.px),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        ThemedText(t.type.wordmark, "PhönixStick")
        Spacer(Modifier.width(Layout.WORDMARK_TAB_GAP.px))
        Row(horizontalArrangement = Arrangement.spacedBy(Layout.TAB_GAP.px)) {
            Tab.entries.forEach { tab -> HeaderTab(tab, tab == active) { onTab(tab) } }
        }
        Spacer(Modifier.weight(1f))
        Clock()
        Spacer(Modifier.width(28.px))
        Avatar(avatarLetter, onAvatar)
    }
}

@Composable
private fun HeaderTab(tab: Tab, active: Boolean, onClick: () -> Unit) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val underline = t.color("accent")
    val underlinePx = Layout.TAB_UNDERLINE.px
    Box(
        Modifier
            .dpadClickable(interaction, onClick)
            .drawBehind {
                if (active || focused) {
                    val y = size.height - underlinePx.toPx() / 2
                    drawLine(underline, Offset(0f, y), Offset(size.width, y), underlinePx.toPx())
                }
            }
            .padding(bottom = 10.px),
    ) {
        ThemedText(t.type.tab, tab.label, colorName = if (focused) "textPrimary" else if (active) "accent" else "textMuted")
    }
}

@Composable
fun Clock() {
    val t = theme()
    var now by remember { mutableStateOf(Date()) }
    LaunchedEffect(Unit) {
        while (true) {
            now = Date()
            delay(15_000)
        }
    }
    ThemedText(t.type.clock, SimpleDateFormat("HH:mm", Locale.GERMANY).format(now))
}

@Composable
private fun Avatar(letter: String, onClick: () -> Unit) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val shape = RoundedCornerShape(t.shape.avatarRadius.px)
    Box(
        Modifier
            .size(Layout.AVATAR.px)
            .focusFrame(focused, shape, button = true)
            .clip(shape)
            .background(t.color("surfaceRaised"))
            .border(2.px, t.color(if (focused) "accent" else "borderStrong"), shape)
            .dpadClickable(interaction, onClick),
        contentAlignment = Alignment.Center,
    ) {
        ThemedText(t.type.wordmark.copy(suffix = null, size = 30f), letter)
    }
}
