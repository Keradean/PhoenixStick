package de.phoenixstick.launcher.ui

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.style.TextAlign
import de.phoenixstick.launcher.data.Profile
import de.phoenixstick.launcher.theme.px
import de.phoenixstick.launcher.theme.theme
import de.phoenixstick.launcher.ui.components.Icon
import de.phoenixstick.launcher.ui.components.LineIcon
import de.phoenixstick.launcher.ui.components.ThemedText
import de.phoenixstick.launcher.ui.components.dpadClickable
import de.phoenixstick.launcher.ui.components.focusFrame

/** "Wer schaut?" Das Theme der Seite folgt dem Profil, das gerade den Fokus hat. */
@Composable
fun ProfileScreen(
    profiles: List<Profile>,
    themeNames: Map<String, String>,
    initialId: String?,
    onFocusProfile: (Profile) -> Unit,
    onSelect: (Profile) -> Unit,
    onAdd: () -> Unit,
    onCycleTheme: (Profile) -> Unit,
) {
    val t = theme()
    val first = remember { FocusRequester() }
    LaunchedEffect(Unit) { runCatching { first.requestFocus() } }

    ThemedBackground {
        Column(
            Modifier.fillMaxSize().padding(horizontal = Layout.MAIN_RIGHT.px, vertical = 64.px),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.CenterEnd) { Clock() }
            Spacer(Modifier.height(150.px))
            ThemedText(t.type.heroTitle.copy(size = 72f), "Wer schaut?")
            Spacer(Modifier.height(96.px))
            Row(horizontalArrangement = Arrangement.spacedBy(Layout.PROFILE_GAP.px), verticalAlignment = Alignment.Top) {
                profiles.forEach { profile ->
                    ProfileCircle(
                        label = profile.name,
                        sub = "Theme: ${themeNames[profile.themeId] ?: profile.themeId}",
                        letter = profile.letter,
                        modifier = if (profile.id == (initialId ?: profiles.first().id)) Modifier.focusRequester(first) else Modifier,
                        onFocus = { onFocusProfile(profile) },
                        onClick = { onSelect(profile) },
                        onMenu = { onCycleTheme(profile) },
                    )
                }
                if (profiles.size < Layout.MAX_PROFILES) {
                    ProfileCircle(label = "Profil hinzufügen", sub = null, letter = null, onFocus = {}, onClick = onAdd)
                }
            }
            Spacer(Modifier.weight(1f))
            Row(horizontalArrangement = Arrangement.spacedBy(40.px)) {
                ThemedText(t.type.hint, "OK  Profil wählen", colorName = "textMuted")
                ThemedText(t.type.hint, "Menü  Theme wechseln", colorName = "textMuted")
                ThemedText(t.type.hint, "Zurück  zum Amazon-Startbildschirm", colorName = "textMuted")
            }
        }
    }
}

@Composable
private fun ProfileCircle(
    label: String,
    sub: String?,
    letter: String?,
    onFocus: () -> Unit,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    onMenu: (() -> Unit)? = null,
) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val dashColor = t.color("borderStrong")
    Column(
        modifier.width((Layout.PROFILE_CIRCLE + 40).px).dpadClickable(interaction, onClick, onMenu = onMenu, onFocus = onFocus),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(22.px),
    ) {
        Box(
            Modifier
                .size(Layout.PROFILE_CIRCLE.px)
                .focusFrame(focused, CircleShape)
                .border(if (focused) 6.px else 0.px, t.color("accent"), CircleShape)
                .clip(CircleShape)
                .background(t.color(if (letter != null) "surfaceRaised" else "background")),
            contentAlignment = Alignment.Center,
        ) {
            if (letter != null) {
                ThemedText(t.type.heroTitle.copy(size = 96f, lineHeight = null), letter)
            } else {
                Canvas(Modifier.fillMaxSize()) {
                    drawCircle(dashColor, style = Stroke(3.dp2px(this), pathEffect = PathEffect.dashPathEffect(floatArrayOf(12f, 10f))))
                }
                LineIcon(Icon.Plus, 72.px, t.color("textMuted"))
            }
        }
        ThemedText(t.type.tileLabel, label, colorName = if (focused) "textPrimary" else "textTile", align = TextAlign.Center, maxLines = 2)
        sub?.let { ThemedText(t.type.hint, it, colorName = "textMuted") }
    }
}

private fun Int.dp2px(scope: androidx.compose.ui.graphics.drawscope.DrawScope) = this * scope.density
