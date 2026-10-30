package de.phoenixstick.launcher.data

import android.content.Context
import android.content.Intent

/**
 * Einstellungen, die zum Gerät und Fernseher gehören, nicht zum Profil.
 * Bis es eine Einstellungsseite gibt, lässt sich der Rand beim Start setzen:
 * adb shell am start -n de.phoenixstick.launcher/.MainActivity --ei overscanPercent 3
 */
class DeviceSettings(context: Context) {
    private val _prefs = context.getSharedPreferences("device", Context.MODE_PRIVATE)

    var overscanPercent: Int
        get() = SafeArea.clampPercent(_prefs.getInt(KEY_OVERSCAN, SafeArea.DEFAULT_PERCENT))
        set(value) = _prefs.edit().putInt(KEY_OVERSCAN, SafeArea.clampPercent(value)).apply()

    fun applyIntent(intent: Intent?) {
        if (intent?.hasExtra(KEY_OVERSCAN) == true) overscanPercent = intent.getIntExtra(KEY_OVERSCAN, SafeArea.DEFAULT_PERCENT)
    }

    private companion object {
        const val KEY_OVERSCAN = "overscanPercent"
    }
}

/** Sicherer Bereich gegen Overscan: Manche Fernseher schneiden den Bildrand ab. */
object SafeArea {
    const val DEFAULT_PERCENT = 5
    const val MAX_PERCENT = 8
    const val DESIGN_WIDTH = 1920f
    const val DESIGN_HEIGHT = 1080f

    fun clampPercent(percent: Int) = percent.coerceIn(0, MAX_PERCENT)

    /** Maßstab vom Entwurf (1920 × 1080) auf den sicheren Bereich, der an jeder Seite [percent] Prozent frei lässt. */
    fun factor(width: Float, height: Float, percent: Int): Float {
        val inset = 1f - 2f * clampPercent(percent) / 100f
        return minOf(width * inset / DESIGN_WIDTH, height * inset / DESIGN_HEIGHT)
    }
}
