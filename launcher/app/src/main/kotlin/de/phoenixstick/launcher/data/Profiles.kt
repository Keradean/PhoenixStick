package de.phoenixstick.launcher.data

import kotlinx.serialization.Serializable

/** Kategorien für die Chips. ALL ist nur ein Filter und wird keiner App zugewiesen. */
enum class Category(val label: String) {
    ALL("Alle"),
    STREAMING("Streaming"),
    MEDIATHEKEN("Mediatheken"),
    ANIME("Anime"),
    KIDS("Kids"),
    LIVE("Live");

    companion object {
        val assignable = entries.filter { it != ALL }

        /** Vorbelegung für die Apps auf Dennis' Stick, änderbar über die Menütaste. */
        private val defaults = mapOf(
            "com.netflix.ninja" to STREAMING,
            "com.amazon.firebat" to STREAMING,
            "com.disney.disneyplus" to STREAMING,
            "com.hbo.hbonow" to STREAMING,
            "com.amazon.firetv.youtube" to STREAMING,
            "com.crunchyroll.crunchyroid" to ANIME,
            "de.toggo" to KIDS,
            "mekmedia.dmax.firetv" to MEDIATHEKEN,
            "com.zdf.android.mediathek" to MEDIATHEKEN,
            "de.swr.ard.avp.mobile.android.amazon" to MEDIATHEKEN,
            "tv.twitch.android.viewer" to LIVE,
        )

        fun defaultFor(packageName: String): Category? = defaults[packageName]
    }
}

@Serializable
data class LaunchEntry(val packageName: String, val time: Long)

@Serializable
data class Profile(
    val id: String,
    val name: String,
    val letter: String,
    val themeId: String,
    val favorites: List<String> = emptyList(),
    val categories: Map<String, Category> = emptyMap(),
    val history: List<LaunchEntry> = emptyList(),
) {
    fun isFavorite(packageName: String) = packageName in favorites

    fun categoryOf(packageName: String): Category? = categories[packageName] ?: Category.defaultFor(packageName)

    fun lastLaunch(packageName: String): Long? = history.firstOrNull { it.packageName == packageName }?.time

    val lastLaunched: String? get() = history.firstOrNull()?.packageName

    fun toggleFavorite(packageName: String) =
        copy(favorites = if (isFavorite(packageName)) favorites - packageName else favorites + packageName)

    fun withCategory(packageName: String, category: Category) = copy(categories = categories + (packageName to category))

    /** Neuester Start zuerst, jede App nur einmal. */
    fun launched(packageName: String, time: Long) =
        copy(history = (listOf(LaunchEntry(packageName, time)) + history.filter { it.packageName != packageName }).take(HISTORY_SIZE))

    companion object {
        const val HISTORY_SIZE = 20
    }
}
