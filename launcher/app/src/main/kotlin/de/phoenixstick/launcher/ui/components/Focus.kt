package de.phoenixstick.launcher.ui.components

import androidx.compose.foundation.shape.GenericShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.ClipOp
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.graphics.addOutline
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipPath
import de.phoenixstick.launcher.theme.GlowSpec
import de.phoenixstick.launcher.theme.LocalDesignScale
import de.phoenixstick.launcher.theme.theme

/** Zwei schräg abgeschnittene Ecken (oben links, unten rechts), für style.buttonShape = chamfer. */
fun chamferShape(cut: Float) = GenericShape { size, _ ->
    moveTo(cut, 0f)
    lineTo(size.width, 0f)
    lineTo(size.width, size.height - cut)
    lineTo(size.width - cut, size.height)
    lineTo(0f, size.height)
    lineTo(0f, cut)
    close()
}

@Composable
fun buttonShape(): Shape {
    val t = theme()
    val scale = LocalDesignScale.current
    return if (t.style.chamferButtons) {
        val cut = with(androidx.compose.ui.platform.LocalDensity.current) { scale.dp(t.shape.buttonChamfer).toPx() }
        chamferShape(cut)
    } else {
        RoundedCornerShape(scale.dp(t.shape.buttonRadius))
    }
}

/**
 * Schein nur außerhalb der Form. Auf Android 7 gibt es keinen farbigen Schatten, deshalb
 * mehrere halbtransparente Linien nach außen. Das ist billig genug für den Stick.
 */
fun Modifier.glow(spec: GlowSpec, shape: Shape, visible: Boolean, blurPx: Float): Modifier =
    if (!visible) this else drawBehind {
        val outline = shape.createOutline(size, layoutDirection, this)
        val path = Path().apply { addOutline(outline) }
        val base = Color(spec.color)
        clipPath(path, clipOp = ClipOp.Difference) {
            val steps = 6
            for (i in steps downTo 1) {
                val width = blurPx * i / steps * 2f
                drawPath(path, base.copy(alpha = base.alpha * (1f - (i - 1f) / steps) / steps * 2f), style = Stroke(width))
            }
        }
    }

/** Vier Eck-Klammern außerhalb des Elements, für style.focusStyle = brackets. */
fun Modifier.brackets(color: Color, lengthPx: Float, widthPx: Float, offsetPx: Float, visible: Boolean): Modifier =
    if (!visible) this else drawWithContent {
        drawContent()
        val l = -offsetPx
        val t = -offsetPx
        val r = size.width + offsetPx
        val b = size.height + offsetPx
        val stroke = widthPx
        fun corner(x: Float, y: Float, dx: Float, dy: Float) {
            drawLine(color, Offset(x, y), Offset(x + dx * lengthPx, y), stroke)
            drawLine(color, Offset(x, y), Offset(x, y + dy * lengthPx), stroke)
        }
        corner(l, t, 1f, 1f)
        corner(r, t, -1f, 1f)
        corner(l, b, 1f, -1f)
        corner(r, b, -1f, -1f)
    }

/** Fokus-Rahmen für Kacheln und Buttons nach dem Theme: Schein, Rahmen, bei Sci-Fi zusätzlich Eck-Klammern. */
@Composable
fun Modifier.focusFrame(focused: Boolean, shape: Shape, button: Boolean = false): Modifier {
    val t = theme()
    val scale = LocalDesignScale.current
    val density = androidx.compose.ui.platform.LocalDensity.current
    fun px(v: Float) = with(density) { scale.dp(v).toPx() }
    val spec = if (button) t.buttonGlow else t.tileGlow
    return this
        .glow(spec, shape, focused, px(spec.blur))
        .brackets(
            color = t.color("focusBrackets"),
            lengthPx = px(t.focus.brackets.size),
            widthPx = px(t.focus.brackets.width),
            offsetPx = px(t.focus.brackets.offset),
            visible = focused && t.style.brackets,
        )
}
