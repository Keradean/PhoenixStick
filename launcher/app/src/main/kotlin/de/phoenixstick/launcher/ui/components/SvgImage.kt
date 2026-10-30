package de.phoenixstick.launcher.ui.components

import android.graphics.Bitmap
import android.graphics.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.produceState
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import com.caverock.androidsvg.SVG
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/**
 * Zeichnet ein SVG aus den Assets (Pfad wie in der Theme-Datei, z. B. "design/referenz/scifi-hero.svg").
 * Es wird einmal in passender Größe in ein Bild gerendert, das schont den schwachen Stick.
 */
@Composable
fun SvgImage(assetPath: String, modifier: Modifier = Modifier) {
    BoxWithConstraints(modifier) {
        val density = LocalDensity.current
        val width = with(density) { maxWidth.roundToPx() }
        val height = with(density) { maxHeight.roundToPx() }
        val context = LocalContext.current
        val bitmap by produceState<ImageBitmap?>(null, assetPath, width, height) {
            value = withContext(Dispatchers.Default) { render(context.assets, assetPath, width, height) }
        }
        bitmap?.let { Image(it, contentDescription = null, contentScale = ContentScale.FillBounds, modifier = Modifier.fillMaxSize()) }
    }
}

private val _cache = HashMap<String, ImageBitmap>()

private fun render(assets: android.content.res.AssetManager, path: String, width: Int, height: Int): ImageBitmap? {
    if (width <= 0 || height <= 0) return null
    val key = "$path@${width}x$height"
    synchronized(_cache) { _cache[key]?.let { return it } }
    val svg = SVG.getFromAsset(assets, path)
    svg.setDocumentWidth(width.toFloat())
    svg.setDocumentHeight(height.toFloat())
    val bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)
    svg.renderToCanvas(Canvas(bitmap))
    val image = bitmap.asImageBitmap()
    synchronized(_cache) { _cache[key] = image }
    return image
}
