package de.phoenixstick.launcher.theme

import android.content.res.AssetManager
import androidx.compose.animation.core.CubicBezierEasing
import androidx.compose.animation.core.Easing
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.LinearEasing
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.size
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import de.phoenixstick.launcher.data.SafeArea
import de.phoenixstick.launcher.ui.components.SvgImage
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.TextUnitType
import androidx.compose.ui.unit.em

/** Ein geladenes Theme: Token-Werte plus fertige Schriften. Farben werden nur hier aus Strings erzeugt. */
class LauncherTheme(val tokens: ThemeTokens, assets: AssetManager) {
    private val families: Map<String, FontFamily> = listOf("display", "body", "mono").associateWith { role ->
        val token = tokens.font.of(role)
        FontFamily(token.weights.map { w -> Font(token.fileFor(w), assets, FontWeight(w)) })
    }

    private val colorCache = HashMap<String, Color>()

    /** Farbe nach Name aus dem Block "color" oder als direkte Angabe. */
    fun color(nameOrValue: String): Color =
        colorCache.getOrPut(nameOrValue) { Color(ColorValues.resolve(nameOrValue, tokens.color)) }

    val style get() = tokens.style
    val shape get() = tokens.shape
    val focus get() = tokens.focus
    val type get() = tokens.type
    val motion get() = tokens.motion

    val tileGlow by lazy { GlowValues.parse(focus.tileGlow, tokens.color) }
    val buttonGlow by lazy { GlowValues.parse(focus.buttonGlow, tokens.color) }

    val easing: Easing
        get() = when (motion.easing) {
            "linear" -> LinearEasing
            "ease-in-out" -> FastOutSlowInEasing
            else -> CubicBezierEasing(0f, 0f, 0.58f, 1f)
        }

    fun fontFamily(role: String): FontFamily = families.getValue(role)

    /** Textstil aus einem Eintrag in "type". Größen sind px bei 1920 × 1080. */
    fun text(token: TextToken, scale: DesignScale): TextStyle = TextStyle(
        fontFamily = fontFamily(token.font),
        fontWeight = FontWeight(token.weight),
        fontSize = scale.sp(token.size),
        letterSpacing = token.letterSpacingEm.em,
        lineHeight = token.lineHeight?.em ?: TextStyle.Default.lineHeight,
        color = color(token.color ?: "textPrimary"),
        fontFeatureSettings = if (token.tabular) "tnum" else null,
    )

    /** Text so, wie der Eintrag es vorgibt: Großschreibung und Präfix. Das Suffix zeichnet der Aufrufer, weil es eine eigene Farbe haben kann. */
    fun label(token: TextToken, text: String): String =
        (token.prefix ?: "") + if (token.uppercase) text.uppercase() else text

    /** App-Namen auf Kacheln richten sich nach style.labelCase. */
    fun appName(name: String): String = if (style.labelCase == "upper") name.uppercase() else name
}

/** Rechnet Maße aus dem Entwurf (px bei 1920 breit) in Bildschirm-dp um. */
class DesignScale(private val factor: Float, private val fontScale: Float) {
    fun dp(px: Float): Dp = (px * factor).dp
    fun dp(px: Int): Dp = dp(px.toFloat())
    fun sp(px: Float) = TextUnit(px * factor / fontScale, TextUnitType.Sp)
}

val LocalLauncherTheme = staticCompositionLocalOf<LauncherTheme> { error("Kein Theme gesetzt") }
val LocalDesignScale = staticCompositionLocalOf<DesignScale> { error("Kein Maßstab gesetzt") }

/**
 * Setzt Theme und Maßstab. Hintergrundfarbe und Hintergrundbild füllen den ganzen Bildschirm,
 * die Oberfläche selbst sitzt als 1920 × 1080 große Fläche im sicheren Bereich (gegen Overscan).
 */
@Composable
fun ProvideLauncherTheme(theme: LauncherTheme, overscanPercent: Int, content: @Composable () -> Unit) {
    val density = LocalDensity.current
    CompositionLocalProvider(LocalLauncherTheme provides theme) {
        BoxWithConstraints(Modifier.fillMaxSize().background(theme.color("background"))) {
            theme.tokens.backgroundImage?.let { SvgImage(it.file, Modifier.fillMaxSize()) }
            val factor = SafeArea.factor(maxWidth.value, maxHeight.value, overscanPercent)
            CompositionLocalProvider(LocalDesignScale provides DesignScale(factor, density.fontScale)) {
                Box(
                    Modifier.align(Alignment.Center).size((SafeArea.DESIGN_WIDTH * factor).dp, (SafeArea.DESIGN_HEIGHT * factor).dp),
                ) { content() }
            }
        }
    }
}

/** Kurzschreibweise in Composables: 128.px statt Umrechnung von Hand. */
val Int.px: Dp
    @Composable @ReadOnlyComposable get() = LocalDesignScale.current.dp(this)

val Float.px: Dp
    @Composable @ReadOnlyComposable get() = LocalDesignScale.current.dp(this)

@Composable
@ReadOnlyComposable
fun theme(): LauncherTheme = LocalLauncherTheme.current

@Composable
fun TextToken.style(): TextStyle = theme().text(this, LocalDesignScale.current)

