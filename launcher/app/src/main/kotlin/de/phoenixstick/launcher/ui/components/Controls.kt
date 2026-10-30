package de.phoenixstick.launcher.ui.components

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.key.Key
import androidx.compose.ui.input.key.KeyEventType
import androidx.compose.ui.input.key.key
import androidx.compose.ui.input.key.onPreviewKeyEvent
import androidx.compose.ui.input.key.type
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.text.withStyle
import androidx.tv.material3.Text
import de.phoenixstick.launcher.data.AppInfo
import de.phoenixstick.launcher.theme.TextToken
import de.phoenixstick.launcher.theme.px
import de.phoenixstick.launcher.theme.style
import de.phoenixstick.launcher.theme.theme
import de.phoenixstick.launcher.ui.Layout

/** Text nach einem Eintrag aus "type", inklusive Präfix, Großschreibung und farbigem Suffix. */
@Composable
fun ThemedText(
    token: TextToken,
    text: String,
    modifier: Modifier = Modifier,
    colorName: String? = null,
    maxLines: Int = 1,
    align: TextAlign? = null,
) {
    val t = theme()
    val base = token.style().let { s -> colorName?.let { s.copy(color = t.color(it)) } ?: s }
    val content: AnnotatedString = buildAnnotatedString {
        append(t.label(token, text))
        token.suffix?.let { suffix ->
            withStyle(SpanStyle(color = token.suffixColor?.let(t::color) ?: base.color)) { append(suffix) }
        }
    }
    Text(content, modifier, style = base.merge(TextStyle(textAlign = align ?: TextAlign.Unspecified)), maxLines = maxLines, overflow = TextOverflow.Ellipsis)
}

/** Fokussierbar per Steuerkreuz, OK löst onClick aus, Menütaste onMenu. Kein Ripple, der Fokus zeigt sich über das Theme. */
@Composable
fun Modifier.dpadClickable(
    interaction: MutableInteractionSource,
    onClick: () -> Unit,
    onMenu: (() -> Unit)? = null,
    onFocus: (() -> Unit)? = null,
): Modifier = this
    .onFocusChanged { if (it.isFocused) onFocus?.invoke() }
    .onPreviewKeyEvent { event ->
        if (onMenu != null && event.type == KeyEventType.KeyUp && event.key == Key.Menu) {
            onMenu(); true
        } else {
            false
        }
    }
    .clickable(interactionSource = interaction, indication = null, onClick = onClick)

@Composable
fun AppTile(
    app: AppInfo,
    widthPx: Int,
    heightPx: Int,
    showLastTag: Boolean,
    onClick: () -> Unit,
    onMenu: () -> Unit,
    onFocus: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val scale by animateFloatAsState(
        if (focused) t.focus.tileScale else 1f,
        tween(t.motion.focusMs, easing = t.easing),
        label = "tileScale",
    )
    val shape = RoundedCornerShape(t.shape.tileRadius.px)
    Box(
        modifier
            .size(widthPx.px, heightPx.px)
            .graphicsLayer { scaleX = scale; scaleY = scale }
            .focusFrame(focused, shape)
            .clip(shape)
            .background(t.color(if (focused) "surfaceFocus" else "surface"))
            .border(
                if (focused) t.focus.tileBorderWidth.px else 1.px,
                t.color(if (focused) t.focus.tileBorderColor else "borderSubtle"),
                shape,
            )
            .dpadClickable(interaction, onClick, onMenu, onFocus),
        contentAlignment = Alignment.Center,
    ) {
        if (app.banner != null) {
            Image(app.banner, contentDescription = app.label, contentScale = ContentScale.Crop, modifier = Modifier.fillMaxSize())
        } else {
            ThemedText(
                if (focused) t.type.tileLabelFocused.copy(uppercase = t.type.tileLabel.uppercase, letterSpacingEm = t.type.tileLabel.letterSpacingEm) else t.type.tileLabel,
                t.appName(app.label),
                colorName = if (focused) "textPrimary" else "textTile",
                maxLines = 2,
                align = TextAlign.Center,
                modifier = Modifier.padding(horizontal = 16.px),
            )
        }
        if (showLastTag) {
            Box(
                Modifier
                    .align(Alignment.TopStart)
                    .offset(14.px, 14.px)
                    .clip(RoundedCornerShape(4.px))
                    .background(t.color("accent"))
                    .padding(horizontal = 10.px, vertical = 4.px),
            ) {
                ThemedText(t.type.tag, "Zuletzt", colorName = "buttonPrimaryText")
            }
        }
    }
}

@Composable
fun PrimaryButton(label: String, icon: Icon?, onClick: () -> Unit, modifier: Modifier = Modifier, onFocus: (() -> Unit)? = null) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val shape = buttonShape()
    Row(
        modifier
            .height(Layout.BUTTON_HEIGHT.px)
            .focusFrame(focused, shape, button = true)
            .border(if (focused) t.focus.buttonRing.width.px else 0.px, t.color(t.focus.buttonRing.color), shape)
            .clip(shape)
            .background(t.color("buttonPrimaryBg"))
            .dpadClickable(interaction, onClick, onFocus = onFocus)
            .padding(horizontal = Layout.BUTTON_PADDING_X.px),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(14.px),
    ) {
        icon?.let { LineIcon(it, Layout.BUTTON_ICON.px, t.color("buttonPrimaryText")) }
        ThemedText(t.type.button, label, colorName = "buttonPrimaryText")
    }
}

@Composable
fun RoundIconButton(icon: Icon, onClick: () -> Unit, modifier: Modifier = Modifier, onFocus: (() -> Unit)? = null) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val shape = RoundedCornerShape(t.shape.iconButtonRadius.px)
    Box(
        modifier
            .size(Layout.BUTTON_HEIGHT.px)
            .focusFrame(focused, shape, button = true)
            .border(
                if (focused) t.focus.buttonRing.width.px else 1.px,
                t.color(if (focused) t.focus.buttonRing.color else "borderStrong"),
                shape,
            )
            .clip(shape)
            .background(t.color(if (focused) "surfaceFocus" else "heroCard"))
            .dpadClickable(interaction, onClick, onFocus = onFocus),
        contentAlignment = Alignment.Center,
    ) {
        LineIcon(icon, 26.px, t.color("textPrimary"))
    }
}

@Composable
fun FilterChip(label: String, active: Boolean, onClick: () -> Unit, modifier: Modifier = Modifier) {
    val t = theme()
    val interaction = remember { MutableInteractionSource() }
    val focused by interaction.collectIsFocusedAsState()
    val shape = RoundedCornerShape(t.shape.chipRadius.px)
    Box(
        modifier
            .focusFrame(focused, shape, button = true)
            .clip(shape)
            .background(t.color(if (active) "accentSoft" else if (focused) "surfaceFocus" else "background"))
            .border(
                if (focused) t.focus.buttonRing.width.px else 1.px,
                t.color(if (focused) t.focus.buttonRing.color else if (active) "accent" else "borderChip"),
                shape,
            )
            .dpadClickable(interaction, onClick)
            .padding(horizontal = Layout.CHIP_PADDING_X.px, vertical = Layout.CHIP_PADDING_Y.px),
    ) {
        ThemedText(t.type.chip, label, colorName = if (active) "accent" else if (focused) "textPrimary" else "textMuted")
    }
}
