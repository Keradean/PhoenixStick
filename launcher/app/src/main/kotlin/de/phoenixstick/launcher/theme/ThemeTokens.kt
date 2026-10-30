package de.phoenixstick.launcher.theme

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json

/** Abbild einer Theme-Datei aus design/tokens/, Aufbau siehe docs/THEME-SCHEMA.md. */
@Serializable
data class ThemeTokens(
    val name: String,
    val id: String,
    val default: Boolean,
    val description: String,
    val font: FontRoles,
    val color: Map<String, String>,
    val type: TypeTokens,
    val shape: ShapeTokens,
    val focus: FocusTokens,
    val style: StyleTokens,
    val motion: MotionTokens,
    val heroImage: ImageRef,
    val backgroundImage: ImageRef?,
)

@Serializable
data class FontRoles(val display: FontFamilyToken, val body: FontFamilyToken, val mono: FontFamilyToken) {
    fun of(role: String): FontFamilyToken = when (role) {
        "display" -> display
        "body" -> body
        "mono" -> mono
        else -> throw IllegalArgumentException("Unbekannte Schriftrolle: $role")
    }
}

@Serializable
data class FontFamilyToken(val family: String, val weights: List<Int>, val license: String) {
    /** Dateiname in assets/fonts/, z. B. "Chakra Petch" 700 -> "chakra_petch_700.ttf". */
    fun fileFor(weight: Int): String = "fonts/${family.lowercase().replace(' ', '_')}_$weight.ttf"
}

@Serializable
data class TextToken(
    val font: String,
    val size: Float,
    val weight: Int,
    val letterSpacingEm: Float = 0f,
    val uppercase: Boolean = false,
    val color: String? = null,
    val lineHeight: Float? = null,
    val prefix: String? = null,
    val suffix: String? = null,
    val suffixColor: String? = null,
    val tabular: Boolean = false,
)

@Serializable
data class TypeTokens(
    val wordmark: TextToken,
    val tab: TextToken,
    val clock: TextToken,
    val date: TextToken,
    val eyebrow: TextToken,
    val heroTitle: TextToken,
    val heroSub: TextToken,
    val heroMeta: TextToken,
    val button: TextToken,
    val buttonSecondary: TextToken,
    val sectionTitle: TextToken,
    val viewAll: TextToken,
    val tileLabel: TextToken,
    val tileLabelFocused: TextToken,
    val tag: TextToken,
    val chip: TextToken,
    val hint: TextToken,
) {
    fun all(): Map<String, TextToken> = mapOf(
        "wordmark" to wordmark, "tab" to tab, "clock" to clock, "date" to date, "eyebrow" to eyebrow,
        "heroTitle" to heroTitle, "heroSub" to heroSub, "heroMeta" to heroMeta, "button" to button,
        "buttonSecondary" to buttonSecondary, "sectionTitle" to sectionTitle, "viewAll" to viewAll,
        "tileLabel" to tileLabel, "tileLabelFocused" to tileLabelFocused, "tag" to tag, "chip" to chip, "hint" to hint,
    )
}

@Serializable
data class Indicator(val width: Float, val height: Float)

@Serializable
data class ShapeTokens(
    val railItemRadius: Float,
    val railActiveIndicator: Indicator,
    val heroCardRadius: Float,
    val tileRadius: Float,
    val buttonRadius: Float,
    val buttonChamfer: Float,
    val chipRadius: Float,
    val avatarRadius: Float,
    val iconButtonRadius: Float,
)

@Serializable
data class Brackets(val size: Float, val width: Float, val offset: Float)

@Serializable
data class ButtonRing(val width: Float, val color: String)

@Serializable
data class FocusTokens(
    val tileBorderWidth: Float,
    val tileBorderColor: String,
    val tileGlow: String,
    val tileScale: Float,
    val brackets: Brackets,
    val buttonRing: ButtonRing,
    val buttonGlow: String,
)

@Serializable
data class StyleTokens(
    val tileShape: String,
    val focusStyle: String,
    val buttonShape: String,
    val labelCase: String,
    val hasBackgroundGrid: Boolean,
) {
    val squareTiles get() = tileShape == "square"
    val brackets get() = focusStyle == "brackets"
    val chamferButtons get() = buttonShape == "chamfer"
}

@Serializable
data class MotionTokens(val focusMs: Int, val heroCrossfadeMs: Int, val maxMs: Int, val easing: String)

@Serializable
data class ImageRef(val file: String)

object ThemeJson {
    // Unbekannte Schlüssel sind ein Fehler: so fällt auf, wenn eine Theme-Datei vom Schema abweicht
    private val json = Json { ignoreUnknownKeys = false }

    fun parse(text: String): ThemeTokens = json.decodeFromString(ThemeTokens.serializer(), text)
}
