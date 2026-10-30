package de.phoenixstick.launcher.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import de.phoenixstick.launcher.data.AppInfo
import de.phoenixstick.launcher.data.Category
import de.phoenixstick.launcher.data.Profile
import de.phoenixstick.launcher.theme.px
import de.phoenixstick.launcher.theme.theme
import de.phoenixstick.launcher.ui.components.ThemedText
import de.phoenixstick.launcher.ui.components.dpadClickable
import de.phoenixstick.launcher.ui.components.focusFrame

/** Menütaste auf einer Kachel: Favorit an/aus, Kategorie wechseln. */
@Composable
fun AppMenu(app: AppInfo, profile: Profile, onToggleFavorite: () -> Unit, onCategory: (Category) -> Unit, onClose: () -> Unit) {
    val t = theme()
    val first = remember { FocusRequester() }
    LaunchedEffect(app.packageName) { runCatching { first.requestFocus() } }
    BackHandler(onBack = onClose)

    val current = profile.categoryOf(app.packageName)
    val next = Category.assignable[((current?.let { Category.assignable.indexOf(it) } ?: -1) + 1) % Category.assignable.size]
    val shape = RoundedCornerShape(t.shape.heroCardRadius.px)

    Box(Modifier.fillMaxSize().background(t.color("background").copy(alpha = 0.8f)), contentAlignment = Alignment.Center) {
        Column(
            Modifier
                .width(640.px)
                .clip(shape)
                .background(t.color("heroCard"))
                .border(1.px, t.color("borderStrong"), shape)
                .padding(48.px),
            verticalArrangement = Arrangement.spacedBy(20.px),
        ) {
            ThemedText(t.type.eyebrow, "Menü")
            ThemedText(t.type.sectionTitle, app.label, colorName = "textPrimary")
            MenuEntry(if (profile.isFavorite(app.packageName)) "Aus Favoriten entfernen" else "Zu Favoriten", Modifier.focusRequester(first)) {
                onToggleFavorite()
                onClose()
            }
            // Die eingebauten Schriften haben "›", aber keinen Pfeil "→"
            MenuEntry("Kategorie: ${current?.label ?: "keine"}  ›  ${next.label}") { onCategory(next) }
            MenuEntry("Schließen", onClick = onClose)
        }
    }
}

@Composable
private fun MenuEntry(label: String, modifier: Modifier = Modifier, onClick: () -> Unit) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val shape = RoundedCornerShape(t.shape.buttonRadius.px)
    Box(
        modifier
            .focusFrame(focused, shape, button = true)
            .clip(shape)
            .background(t.color(if (focused) "surfaceFocus" else "surface"))
            .border(1.px, t.color(if (focused) "accent" else "borderSubtle"), shape)
            .dpadClickable(interaction, onClick)
            .padding(horizontal = 28.px, vertical = 18.px),
    ) {
        ThemedText(t.type.buttonSecondary, label, colorName = if (focused) "textPrimary" else "textSecondary")
    }
}
