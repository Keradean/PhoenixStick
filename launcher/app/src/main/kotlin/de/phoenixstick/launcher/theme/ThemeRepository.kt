package de.phoenixstick.launcher.theme

import android.content.res.AssetManager

/** Lädt Themes aus assets/design/tokens/, die beim Bauen aus design/tokens/ kopiert werden. */
class ThemeRepository(private val _assets: AssetManager) {
    private val _cache = HashMap<String, LauncherTheme>()

    val ids: List<String> by lazy {
        (_assets.list(TOKENS_DIR) ?: emptyArray()).filter { it.endsWith(".json") }.map { it.removeSuffix(".json") }.sorted()
    }

    val defaultId: String by lazy { ids.firstOrNull { tokens(it).default } ?: ids.first() }

    fun get(id: String): LauncherTheme =
        _cache.getOrPut(id) { LauncherTheme(tokens(if (id in ids) id else defaultId), _assets) }

    private fun tokens(id: String): ThemeTokens =
        _assets.open("$TOKENS_DIR/$id.json").bufferedReader().use { ThemeJson.parse(it.readText()) }

    companion object {
        const val TOKENS_DIR = "design/tokens"
    }
}
