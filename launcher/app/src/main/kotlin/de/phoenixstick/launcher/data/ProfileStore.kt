package de.phoenixstick.launcher.data

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import java.io.File

@Serializable
private data class ProfileFile(val profiles: List<Profile>)

/** Profile als JSON im privaten App-Ordner. Kein Netzwerk, keine Synchronisierung. */
class ProfileStore(private val _file: File, private val _defaultThemeId: String) {
    private val _json = Json { ignoreUnknownKeys = true; prettyPrint = true }

    fun load(): List<Profile> {
        if (_file.exists()) {
            runCatching { _json.decodeFromString(ProfileFile.serializer(), _file.readText()).profiles }
                .getOrNull()
                ?.takeIf { it.isNotEmpty() }
                ?.let { return it }
        }
        // Dennis startet mit Phönix (Entscheidung vom 30.10.2026), Sci-Fi über die Menütaste in der Profilauswahl
        return listOf(Profile(id = "dennis", name = "Dennis", letter = "D", themeId = "phoenix")).also(::save)
    }

    fun save(profiles: List<Profile>) {
        // Erst in eine temporäre Datei, dann ersetzen
        val temp = File(_file.parentFile, _file.name + ".tmp")
        temp.writeText(_json.encodeToString(ProfileFile.serializer(), ProfileFile(profiles)))
        temp.renameTo(_file)
    }

    fun newProfile(existing: List<Profile>): Profile {
        val number = existing.size + 1
        val name = "Profil $number"
        return Profile(id = "profil$number-${System.currentTimeMillis()}", name = name, letter = number.toString(), themeId = _defaultThemeId)
    }
}
