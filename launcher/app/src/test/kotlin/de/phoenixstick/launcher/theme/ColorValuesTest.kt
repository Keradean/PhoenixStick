package de.phoenixstick.launcher.theme

import org.junit.Assert.assertEquals
import org.junit.Test

class ColorValuesTest {
    @Test
    fun parsesHex() = assertEquals(0xFF4FE3FFL, ColorValues.parse("#4FE3FF"))

    @Test
    fun parsesRgba() = assertEquals(0x734FE3FFL, ColorValues.parse("rgba(79,227,255,0.45)"))

    @Test
    fun resolvesNamesFromPalette() =
        assertEquals(0xFFE5322DL, ColorValues.resolve("accent", mapOf("accent" to "#E5322D")))

    @Test(expected = IllegalArgumentException::class)
    fun rejectsUnknown() {
        ColorValues.parse("red")
    }

    @Test
    fun parsesGlowWithReferenceAndLiteral() {
        val palette = mapOf("focusGlow" to "rgba(229,50,45,0.45)")
        assertEquals(36f, GlowValues.parse("0 0 36 focusGlow", palette).blur)
        assertEquals(0x80E5322DL, GlowValues.parse("0 0 40 rgba(229,50,45,0.5)", palette).color)
    }
}
