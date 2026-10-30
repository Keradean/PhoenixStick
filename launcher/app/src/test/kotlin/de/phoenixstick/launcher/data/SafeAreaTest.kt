package de.phoenixstick.launcher.data

import org.junit.Assert.assertEquals
import org.junit.Test

class SafeAreaTest {
    // Der Stick meldet 1920 × 1080 px bei Dichte 320, also 960 × 540 dp
    @Test
    fun defaultFivePercentOnTheStick() = assertEquals(0.45f, SafeArea.factor(960f, 540f, 5), 0.0001f)

    @Test
    fun zeroPercentFillsTheScreen() = assertEquals(0.5f, SafeArea.factor(960f, 540f, 0), 0.0001f)

    @Test
    fun percentIsClampedToZeroToEight() {
        assertEquals(SafeArea.factor(960f, 540f, 8), SafeArea.factor(960f, 540f, 20), 0.0001f)
        assertEquals(SafeArea.factor(960f, 540f, 0), SafeArea.factor(960f, 540f, -3), 0.0001f)
    }

    @Test
    fun smallerSideWinsOnOtherAspectRatios() {
        // 4:3-Fläche: die Breite begrenzt
        assertEquals(800f * 0.9f / 1920f, SafeArea.factor(800f, 600f, 5), 0.0001f)
    }
}
