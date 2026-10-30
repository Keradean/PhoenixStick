# Theme- und Profil-Klassen werden per kotlinx.serialization gelesen
-keepattributes *Annotation*, InnerClasses
-keepclassmembers class de.phoenixstick.launcher.** {
    *** Companion;
}
-keepclasseswithmembers class de.phoenixstick.launcher.** {
    kotlinx.serialization.KSerializer serializer(...);
}
-dontwarn com.caverock.androidsvg.**
