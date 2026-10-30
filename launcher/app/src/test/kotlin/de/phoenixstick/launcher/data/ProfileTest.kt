package de.phoenixstick.launcher.data

import de.phoenixstick.launcher.ui.Tab
import de.phoenixstick.launcher.ui.rowApps
import de.phoenixstick.launcher.ui.sinceText
import org.junit.Assert.assertEquals
import org.junit.Test

class ProfileTest {
    private val dennis = Profile("dennis", "Dennis", "D", "scifi")
    private fun app(pkg: String, label: String) = AppInfo(pkg, label, null)
    private val apps = listOf(app("com.netflix.ninja", "Netflix"), app("de.toggo", "Toggo"), app("com.disney.disneyplus", "Disney+"))

    @Test
    fun historyKeepsNewestFirstAndEachAppOnce() {
        val p = dennis.launched("a", 1).launched("b", 2).launched("a", 3)
        assertEquals(listOf("a", "b"), p.history.map { it.packageName })
        assertEquals("a", p.lastLaunched)
    }

    @Test
    fun favoritesComeFirstInRow() {
        val p = dennis.toggleFavorite("de.toggo")
        assertEquals(listOf("Toggo", "Disney+", "Netflix"), rowApps(apps, p, Tab.START, Category.ALL).map { it.label })
    }

    @Test
    fun favoritesTabShowsOnlyFavorites() {
        val p = dennis.toggleFavorite("com.netflix.ninja")
        assertEquals(listOf("Netflix"), rowApps(apps, p, Tab.FAVORITES, Category.ALL).map { it.label })
    }

    @Test
    fun categoryFilterUsesDefaultsAndOverrides() {
        assertEquals(listOf("Toggo"), rowApps(apps, dennis, Tab.START, Category.KIDS).map { it.label })
        val p = dennis.withCategory("de.toggo", Category.STREAMING)
        assertEquals(emptyList<String>(), rowApps(apps, p, Tab.START, Category.KIDS).map { it.label })
    }

    @Test
    fun sinceTextIsHonest() {
        assertEquals("Noch nicht geöffnet", sinceText(null))
        assertEquals("Vor 2 Std.", sinceText(0, now = 2 * 60 * 60_000L + 5))
    }
}
