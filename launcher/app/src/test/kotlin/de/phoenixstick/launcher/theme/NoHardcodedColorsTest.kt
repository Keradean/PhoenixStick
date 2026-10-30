package de.phoenixstick.launcher.theme

import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/** Farben kommen nur aus design/tokens/. Dieser Test findet fest eingebaute Farben im App-Code. */
class NoHardcodedColorsTest {
    private val sources = File("src/main/kotlin").walkTopDown().filter { it.extension == "kt" }.toList()

    private val forbidden = listOf(
        Regex("""Color\(\s*0x"""),
        Regex("""Color\(\s*red\s*="""),
        Regex("""Color\(\s*\d"""),
        Regex("""Color\.(Black|White|Red|Green|Blue|Gray|Grey|DarkGray|LightGray|Cyan|Magenta|Yellow|Transparent)\b"""),
        Regex("""["']#[0-9A-Fa-f]{3,8}["']"""),
        Regex("""rgba?\(\s*\d"""),
        Regex("""android\.graphics\.Color\."""),
    )

    @Test
    fun sourcesExist() = assertTrue(sources.size > 5)

    @Test
    fun noColorLiteralsInCode() {
        val hits = sources.flatMap { file ->
            file.readLines().mapIndexedNotNull { i, line ->
                val code = line.substringBefore("//")
                if (forbidden.any { it.containsMatchIn(code) }) "${file.name}:${i + 1}: ${line.trim()}" else null
            }
        }
        assertTrue("Fest eingebaute Farben:\n" + hits.joinToString("\n"), hits.isEmpty())
    }
}
