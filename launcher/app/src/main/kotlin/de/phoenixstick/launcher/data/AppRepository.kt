package de.phoenixstick.launcher.data

import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.content.pm.ResolveInfo
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.drawable.BitmapDrawable
import android.graphics.drawable.Drawable
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap

data class AppInfo(
    val packageName: String,
    val label: String,
    val banner: ImageBitmap?,
)

/** Liest installierte Apps mit Startseite aus. Nur lesend, ohne Netzwerk. */
class AppRepository(private val _context: Context) {
    private val _pm: PackageManager = _context.packageManager

    fun load(): List<AppInfo> {
        val tv = query(Intent.CATEGORY_LEANBACK_LAUNCHER)
        val phone = query(Intent.CATEGORY_LAUNCHER)
        // TV-Einträge haben Vorrang, sie bringen das Banner mit
        return (tv + phone)
            .distinctBy { it.activityInfo.packageName }
            .filter { it.activityInfo.packageName != _context.packageName }
            .map { info ->
                AppInfo(
                    packageName = info.activityInfo.packageName,
                    label = info.loadLabel(_pm).toString(),
                    banner = banner(info),
                )
            }
            .sortedBy { it.label.lowercase() }
    }

    fun launchIntent(packageName: String): Intent? =
        (_pm.getLeanbackLaunchIntentForPackage(packageName) ?: _pm.getLaunchIntentForPackage(packageName))
            ?.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)

    private fun query(category: String): List<ResolveInfo> =
        _pm.queryIntentActivities(Intent(Intent.ACTION_MAIN).addCategory(category), 0)

    private fun banner(info: ResolveInfo): ImageBitmap? {
        val drawable = info.activityInfo.loadBanner(_pm) ?: info.activityInfo.applicationInfo.loadBanner(_pm) ?: return null
        return drawable.toBitmap().asImageBitmap()
    }

    private fun Drawable.toBitmap(): Bitmap {
        if (this is BitmapDrawable && bitmap != null) return bitmap
        val w = intrinsicWidth.coerceAtLeast(1)
        val h = intrinsicHeight.coerceAtLeast(1)
        return Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888).also { bmp ->
            setBounds(0, 0, w, h)
            draw(Canvas(bmp))
        }
    }
}
