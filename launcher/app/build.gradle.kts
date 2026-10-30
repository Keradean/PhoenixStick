import org.gradle.api.file.FileSystemOperations
import javax.inject.Inject

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.plugin.compose")
    id("org.jetbrains.kotlin.plugin.serialization")
}

android {
    namespace = "de.phoenixstick.launcher"
    compileSdk = 37

    defaultConfig {
        applicationId = "de.phoenixstick.launcher"
        minSdk = 25
        targetSdk = 36
        versionCode = 1
        versionName = "0.1"
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
            // Zum Testen auf dem eigenen Stick reicht der Debug-Schlüssel
            signingConfig = signingConfigs.getByName("debug")
        }
    }

    buildFeatures {
        compose = true
    }
}

// Theme-Dateien und Bilder kommen beim Bauen direkt aus design/, damit es nur eine Quelle gibt
abstract class CopyDesignAssets : DefaultTask() {
    @get:InputDirectory
    abstract val designDir: DirectoryProperty

    @get:OutputDirectory
    abstract val outputDir: DirectoryProperty

    @get:Inject
    abstract val fs: FileSystemOperations

    @TaskAction
    fun copy() {
        fs.sync {
            from(designDir) {
                include("tokens/*.json", "referenz/*.svg")
            }
            into(outputDir.dir("design"))
        }
    }
}

val copyDesignAssets = tasks.register<CopyDesignAssets>("copyDesignAssets") {
    designDir.set(rootProject.layout.projectDirectory.dir("../design"))
}

androidComponents {
    onVariants { variant ->
        variant.sources.assets?.addGeneratedSourceDirectory(copyDesignAssets, CopyDesignAssets::outputDir)
    }
}

dependencies {
    implementation(platform("androidx.compose:compose-bom:2026.08.00"))
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.foundation:foundation")
    implementation("androidx.tv:tv-material:1.1.0")
    implementation("androidx.activity:activity-compose:1.12.4")
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.11.0")
    implementation("com.caverock:androidsvg-aar:1.4")

    testImplementation("junit:junit:4.13.2")
}
