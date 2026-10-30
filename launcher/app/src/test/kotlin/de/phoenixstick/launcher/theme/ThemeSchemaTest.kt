package de.phoenixstick.launcher.theme

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import java.io.File

/** Prüft jede Datei in design/tokens/ gegen docs/THEME-SCHEMA.md. Läuft auf dem PC, nicht auf dem Stick. */
class ThemeSchemaTest {
    // Unit-Tests laufen im Ordner launcher/app
    private val projectRoot = File("../..").canonicalFile
    private val tokenFiles = File(projectRoot, "design/tokens").listFiles { f -> f.extension == "json" }!!.sortedBy { it.name }
    private val fontDir = File("src/main/assets/fonts")

    private fun load(file: File) = try {
        ThemeJson.parse(file.readText())
    } catch (e: Exception) {
        fail("${file.name}: ${e.message}")
        error("unreachable")
    }

    @Test
    fun bothThemesExist() {
        assertEquals(listOf("phoenix.json", "scifi.json"), tokenFiles.map { it.name })
    }

    @Test
    fun everyThemeHasAllKeysAndNoUnknownOnes() {
        // ThemeJson lehnt fehlende und unbekannte Schlüssel ab
        tokenFiles.forEach { load(it) }
    }

    @Test
    fun allColorsAreValid() {
        tokenFiles.forEach { file ->
            load(file).color.forEach { (name, value) ->
                runCatching { ColorValues.parse(value) }.onFailure { fail("${file.name}: color.$name = $value") }
            }
        }
    }

    @Test
    fun everyColorReferenceExists() {
        tokenFiles.forEach { file ->
            val t = load(file)
            val refs = buildList {
                t.type.all().forEach { (name, token) ->
                    token.color?.let { add("type.$name.color" to it) }
                    token.suffixColor?.let { add("type.$name.suffixColor" to it) }
                }
                add("focus.tileBorderColor" to t.focus.tileBorderColor)
                add("focus.buttonRing.color" to t.focus.buttonRing.color)
            }
            refs.forEach { (where, name) -> assertTrue("${file.name}: $where -> $name gibt es nicht", name in t.color) }
            GlowValues.parse(t.focus.tileGlow, t.color)
            GlowValues.parse(t.focus.buttonGlow, t.color)
        }
    }

    @Test
    fun everyFontWeightHasAFile() {
        tokenFiles.forEach { file ->
            val t = load(file)
            t.type.all().forEach { (name, token) ->
                val family = t.font.of(token.font)
                assertTrue("${file.name}: type.$name will Gewicht ${token.weight}, ${family.family} hat ${family.weights}", token.weight in family.weights)
            }
            listOf(t.font.display, t.font.body, t.font.mono).forEach { family ->
                family.weights.forEach { w ->
                    val path = File(fontDir.parentFile, family.fileFor(w))
                    assertTrue("${file.name}: Schriftdatei fehlt: ${path.name}", path.exists())
                }
            }
        }
    }

    @Test
    fun imagesExistAndStyleValuesAreKnown() {
        tokenFiles.forEach { file ->
            val t = load(file)
            listOfNotNull(t.heroImage, t.backgroundImage).forEach { ref ->
                assertTrue("${file.name}: Bild fehlt: ${ref.file}", File(projectRoot, ref.file).exists())
            }
            assertTrue(t.style.tileShape in setOf("rounded", "square"))
            assertTrue(t.style.focusStyle in setOf("glowBorder", "brackets"))
            assertTrue(t.style.buttonShape in setOf("rounded", "chamfer"))
            assertTrue(t.style.labelCase in setOf("upper", "none"))
        }
    }

    @Test
    fun exactlyOneDefaultTheme() {
        assertEquals(1, tokenFiles.count { load(it).default })
    }
}
