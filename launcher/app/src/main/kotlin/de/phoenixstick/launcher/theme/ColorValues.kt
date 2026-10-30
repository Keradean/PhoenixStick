package de.phoenixstick.launcher.theme

/** Wertet Farbangaben aus den Theme-Dateien aus. Reines Kotlin, damit es im JVM-Test läuft. */
object ColorValues {
    private val hex = Regex("^#([0-9A-Fa-f]{6})$")
    private val rgba = Regex("""^rgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*([\d.]+)\s*\)$""")

    /** Liefert ARGB als Long, z. B. für androidx.compose.ui.graphics.Color(Long). */
    fun parse(value: String): Long {
        hex.matchEntire(value.trim())?.let { m ->
            return 0xFF000000L or m.groupValues[1].toLong(16)
        }
        rgba.matchEntire(value.trim())?.let { m ->
            val (r, g, b) = m.groupValues.drop(1).take(3).map { it.toInt().coerceIn(0, 255).toLong() }
            val a = (m.groupValues[4].toFloat().coerceIn(0f, 1f) * 255f + 0.5f).toLong()
            return (a shl 24) or (r shl 16) or (g shl 8) or b
        }
        throw IllegalArgumentException("Ungültige Farbe: $value")
    }

    /** Ein Wert ist entweder ein Name aus dem Block "color" oder eine Farbangabe. */
    fun resolve(value: String, palette: Map<String, String>): Long =
        palette[value]?.let(::parse) ?: parse(value)
}

/** Schein um ein Element, Format wie CSS box-shadow: "x y blur farbe". */
data class GlowSpec(val blur: Float, val color: Long)

object GlowValues {
    fun parse(value: String, palette: Map<String, String>): GlowSpec {
        val parts = value.trim().split(Regex("\\s+"), limit = 4)
        require(parts.size == 4) { "Ungültiger Schein: $value" }
        return GlowSpec(parts[2].toFloat(), ColorValues.resolve(parts[3], palette))
    }
}
