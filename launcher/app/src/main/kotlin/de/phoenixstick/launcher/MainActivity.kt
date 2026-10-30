package de.phoenixstick.launcher

import android.content.Intent
import android.os.Bundle
import android.provider.Settings
import androidx.activity.ComponentActivity
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Box
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import de.phoenixstick.launcher.data.AppInfo
import de.phoenixstick.launcher.data.AppRepository
import de.phoenixstick.launcher.data.Category
import de.phoenixstick.launcher.data.DeviceSettings
import de.phoenixstick.launcher.data.Profile
import de.phoenixstick.launcher.data.ProfileStore
import de.phoenixstick.launcher.theme.ProvideLauncherTheme
import de.phoenixstick.launcher.theme.ThemeRepository
import de.phoenixstick.launcher.ui.AllAppsScreen
import de.phoenixstick.launcher.ui.AppMenu
import de.phoenixstick.launcher.ui.HomeScreen
import de.phoenixstick.launcher.ui.ProfileScreen
import de.phoenixstick.launcher.ui.Tab
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File

class MainActivity : ComponentActivity() {
    private lateinit var _themes: ThemeRepository
    private lateinit var _apps: AppRepository
    private lateinit var _store: ProfileStore
    private lateinit var _device: DeviceSettings

    // Wird bei jeder Rückkehr hochgezählt, damit neu installierte Apps erscheinen
    private var _resumeCount by mutableIntStateOf(0)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        hideSystemBars()
        _device = DeviceSettings(this).also { it.applyIntent(intent) }
        _themes = ThemeRepository(assets)
        _apps = AppRepository(this)
        _store = ProfileStore(File(filesDir, "profiles.json"), _themes.defaultId)

        setContent {
            var profiles by remember { mutableStateOf(_store.load()) }
            var activeId by remember { mutableStateOf<String?>(null) }
            var focusedProfileId by remember { mutableStateOf(profiles.first().id) }
            var apps by remember { mutableStateOf<List<AppInfo>>(emptyList()) }
            var tab by remember { mutableStateOf(Tab.START) }
            var filter by remember { mutableStateOf(Category.ALL) }
            var menuApp by remember { mutableStateOf<AppInfo?>(null) }

            LaunchedEffect(_resumeCount) { apps = withContext(Dispatchers.IO) { _apps.load() } }

            fun update(profile: Profile) {
                profiles = profiles.map { if (it.id == profile.id) profile else it }
                _store.save(profiles)
            }

            val active = profiles.firstOrNull { it.id == activeId }

            if (active == null) {
                val focused = profiles.firstOrNull { it.id == focusedProfileId } ?: profiles.first()
                ProvideLauncherTheme(_themes.get(focused.themeId), _device.overscanPercent) {
                    ProfileScreen(
                        profiles = profiles,
                        themeNames = _themes.ids.associateWith { _themes.get(it).tokens.name },
                        initialId = focusedProfileId,
                        onFocusProfile = { focusedProfileId = it.id },
                        onSelect = { activeId = it.id; tab = Tab.START; filter = Category.ALL },
                        onAdd = {
                            val created = _store.newProfile(profiles)
                            profiles = profiles + created
                            _store.save(profiles)
                            focusedProfileId = created.id
                        },
                        onCycleTheme = { profile ->
                            val ids = _themes.ids
                            update(profile.copy(themeId = ids[(ids.indexOf(profile.themeId) + 1) % ids.size]))
                        },
                    )
                }
                return@setContent
            }

            fun launch(app: AppInfo) {
                update(active.launched(app.packageName, System.currentTimeMillis()))
                _apps.launchIntent(app.packageName)?.let(::startActivity)
            }

            ProvideLauncherTheme(_themes.get(active.themeId), _device.overscanPercent) {
                Box {
                    when (tab) {
                        Tab.START, Tab.FAVORITES -> {
                            // Zurück auf dem Startbildschirm öffnet die Profilauswahl
                            BackHandler(enabled = menuApp == null) { activeId = null; focusedProfileId = active.id }
                            HomeScreen(
                                apps = apps,
                                profile = active,
                                tab = tab,
                                filter = filter,
                                onTab = { tab = it },
                                onFilter = { filter = it },
                                onLaunch = ::launch,
                                onToggleFavorite = { update(active.toggleFavorite(it.packageName)) },
                                onMenu = { menuApp = it },
                                onProfiles = { activeId = null; focusedProfileId = active.id },
                                onSettings = ::openSettings,
                            )
                        }
                        Tab.ALL_APPS -> {
                            BackHandler(enabled = menuApp == null) { tab = Tab.START }
                            AllAppsScreen(
                                apps = apps,
                                profile = active,
                                filter = filter,
                                onTab = { tab = it },
                                onFilter = { filter = it },
                                onLaunch = ::launch,
                                onMenu = { menuApp = it },
                                onProfiles = { activeId = null; focusedProfileId = active.id },
                                onSettings = ::openSettings,
                            )
                        }
                    }
                    menuApp?.let { app ->
                        AppMenu(
                            app = app,
                            profile = active,
                            onToggleFavorite = { update(active.toggleFavorite(app.packageName)) },
                            onCategory = { update(active.withCategory(app.packageName, it)) },
                            onClose = { menuApp = null },
                        )
                    }
                }
            }
        }
    }

    override fun onResume() {
        super.onResume()
        hideSystemBars()
        _resumeCount++
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        _device.applyIntent(intent)
        recreate()
    }

    /** Status- und Navigationsleiste ausblenden, die Oberfläche nutzt den ganzen Bildschirm. */
    private fun hideSystemBars() {
        WindowCompat.setDecorFitsSystemWindows(window, false)
        WindowCompat.getInsetsController(window, window.decorView).apply {
            hide(WindowInsetsCompat.Type.systemBars())
            systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        }
    }

    private fun openSettings() {
        runCatching { startActivity(Intent(Settings.ACTION_SETTINGS).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)) }
    }
}
