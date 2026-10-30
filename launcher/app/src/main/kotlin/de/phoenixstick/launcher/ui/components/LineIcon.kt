package de.phoenixstick.launcher.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Fill
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.scale
import androidx.compose.ui.graphics.vector.PathParser
import androidx.compose.ui.unit.Dp
import androidx.compose.foundation.layout.size

/** Linien-Icons aus dem Entwurf startbildschirm-phoenix.html, als SVG-Pfade. */
enum class Icon(val viewport: Float, val strokeWidth: Float, val path: String, val filled: Boolean = false) {
    Logo(48f, 2.4f, "M24 42C24 30 18 22 8 18C16 18 22 22 24 28C26 20 32 12 42 8C34 16 30 24 30 32M24 42C24 36 27 32 32 30" + circle(35f, 20f, 1.6f)),
    Home(24f, 1.8f, "M3 11l9-7 9 7v9a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z"),
    Favorites(24f, 1.8f, "M6 3h12v18l-6-4-6 4z"),
    AllApps(24f, 1.8f, rect(3f, 3f, 7f, 7f, 1.5f) + rect(14f, 3f, 7f, 7f, 1.5f) + rect(3f, 14f, 7f, 7f, 1.5f) + rect(14f, 14f, 7f, 7f, 1.5f)),
    Inputs(24f, 1.8f, rect(3f, 5f, 18f, 12f, 2f) + "M8 21h8M12 17v4"),
    Settings(24f, 1.8f, circle(12f, 12f, 3f) + "M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"),
    Play(24f, 0f, "M7 4v16l13-8z", filled = true),
    Minus(24f, 2f, "M5 12h14"),
    Plus(24f, 2f, "M12 5v14M5 12h14"),
    Person(24f, 1.6f, circle(12f, 8f, 4f) + "M4 21a8 8 0 0 1 16 0"),
}

private fun rect(x: Float, y: Float, w: Float, h: Float, r: Float) =
    "M${x + r} ${y}H${x + w - r}A$r $r 0 0 1 ${x + w} ${y + r}V${y + h - r}A$r $r 0 0 1 ${x + w - r} ${y + h}" +
        "H${x + r}A$r $r 0 0 1 $x ${y + h - r}V${y + r}A$r $r 0 0 1 ${x + r} ${y}Z"

private fun circle(cx: Float, cy: Float, r: Float) =
    "M${cx - r} ${cy}A$r $r 0 1 0 ${cx + r} ${cy}A$r $r 0 1 0 ${cx - r} ${cy}Z"

@Composable
fun LineIcon(icon: Icon, size: Dp, color: Color, modifier: Modifier = Modifier) {
    val path = remember(icon) { PathParser().parsePathString(icon.path).toPath() }
    Canvas(modifier.size(size)) {
        val factor = this.size.minDimension / icon.viewport
        scale(factor, pivot = androidx.compose.ui.geometry.Offset.Zero) {
            if (icon.filled) {
                drawPath(path, color, style = Fill)
            } else {
                drawPath(path, color, style = Stroke(icon.strokeWidth, cap = StrokeCap.Round, join = StrokeJoin.Round))
            }
        }
    }
}
