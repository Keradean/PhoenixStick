# Goal 06: Launcher im Sci-Fi-Design (erster Schritt)

## Ziel

Eine eigene Launcher-App für den Stick, die den Startbildschirm "Bühne", die Profilauswahl und die Alle-Apps-Seite im Theme **Sci-Fi** zeigt und installierte Apps startet. In diesem Schritt läuft sie als **normale App**. Amazons Startbildschirm bleibt unangetastet, die Home-Taste führt weiter dorthin.

## Lies zuerst

- `docs/DESIGN.md`: alle Vorgaben zum Look. Das ist verbindlich
- `design/tokens/scifi.json`: Farben, Schriften, Maße und Stil des Themes, das zuerst gebaut wird. Abschnitt "Theme Sci-Fi" in `DESIGN.md`
- `design/tokens/phoenix.json`: zweites Theme. Muss mit demselben Code funktionieren, wird in diesem Goal aber nur zum Testen einmal umgeschaltet
- `design/referenz/`: die Entwürfe als HTML-Mockups zum Nachschauen, dazu `scifi-hintergrund.svg` und `phoenix-hero.svg`
- `docs/GERAET.md`: Fire OS 6.7.1.1, Android 7.1.2, SDK 25, Fire TV Stick 4K (AFTMM)

## Schritt 1: Technik festlegen (Stopp)

Bevor du Code schreibst, zeig mir kurz die beiden Möglichkeiten mit Vor- und Nachteilen für genau dieses Gerät, und warte auf meine Entscheidung:

- **Kotlin mit Jetpack Compose:** der übliche Weg für Android-TV-Apps, kleine App, läuft flüssig auf altem Gerät. Für mich eine neue Sprache
- **.NET für Android mit C#:** gleiche Sprache wie das CLI-Tool, aber größere App und weniger Beispiele für Fernseh-Oberflächen

Sag mir außerdem genau, was ich auf CachyOS installieren muss (JDK, Android SDK, Build-Tools) und mit welchen Befehlen. `sudo` darfst du nicht, das mache ich selbst.

## Schritt 2: Grundgerüst

1. Neuer Ordner `launcher/` neben `src/`. Das CLI-Tool bleibt, wie es ist
2. `minSdk` 25. Die App erscheint auf dem Fire TV in der App-Liste (Leanback-Launcher-Eintrag mit Banner)
3. Die Schriften (Chakra Petch, IBM Plex Mono, Antonio, Manrope) werden in die App eingebaut, nichts wird zur Laufzeit aus dem Netz geladen
4. Theme-Werte kommen aus den Dateien in `design/tokens/` (beim Build in die App kopiert). Im Code steht keine einzige fest eingebaute Farbe. Die Komponenten zeichnen sich nach dem Block `style` (Kachelform, Fokus-Stil, Button-Form, Großschreibung, Raster)
5. Kein Tracking, keine Analytics, keine Netzwerkzugriffe außer was zum Starten von Apps nötig ist (also keine)

## Schritt 3: Bildschirme

1. **Profilauswahl** wie in `DESIGN.md`. Profile lokal speichern (Name, Buchstabe, Theme, Favoriten, Verlauf). Zum Start ein Profil "Dennis" mit Theme `scifi`, weitere per "Profil hinzufügen"
2. **Startbildschirm** im Aufbau aus `DESIGN.md` (maßgeblich ist `startbildschirm-phoenix.html`):
   - Icon-Leiste links, Kopfzeile mit Wortmarke, Reitern, Uhrzeit und Avatar
   - Bühne als Karte mit Theme-Bild und dem Inhalt der Kachel, die gerade den Fokus hat
   - App-Reihe mit Favoriten zuerst, Label "ZULETZT" an der zuletzt gestarteten App
   - Kategorie-Chips zum Filtern
3. **Alle Apps**: Raster mit 6 Spalten, alphabetisch, Chips zum Filtern
4. Apps auflisten: alles, was einen Leanback-Launcher- oder Launcher-Eintrag hat. Die eigene App und Amazon-Systemkram ohne Startseite ausblenden
5. Kachelbild: das Banner der App, sonst der App-Name auf `surface`
6. "Zuletzt geöffnet" kommt aus dem eigenen Startverlauf. Kein "Weiterschauen", siehe `DESIGN.md`
7. Steuerung komplett mit dem Steuerkreuz. Fokus immer sichtbar, wie in `DESIGN.md` beschrieben

## Schritt 4: Auf dem Stick testen

- Installieren mit `adb install`. Dafür fragst du mich vorher (Regel in `.claude/settings.json`)
- Ich teste am Fernseher: App aus der App-Liste starten, Profil wählen, durch die Reihe gehen, Netflix starten, zurück, "Zuletzt geöffnet" prüfen
- Achte auf Flüssigkeit: Der Stick hat wenig Speicher. Wenn etwas ruckelt, lieber Effekte weglassen

## Nicht in diesem Goal

- Die Home-Taste übernehmen. Auf Fire OS 6.7 geht das nur über einen Umweg (siehe Abschnitt "Dieses Gerät: Fire OS 6" in `CLAUDE.md`), das kommt als eigenes Goal
- Theme Mondgarten
- Eine Oberfläche zum Wechseln des Themes (zum Testen reicht ein fester Wert im Profil)
- HDMI-Eingänge
- Irgendetwas an `com.amazon.tv.launcher` ändern. Der bleibt never touch

## Fertig, wenn

- [ ] Ich habe die Technik entschieden
- [ ] Die App läuft auf dem Stick und sieht aus wie die Entwürfe
- [ ] Profilauswahl, Bühne und Alle Apps funktionieren mit der Fernbedienung
- [ ] Apps lassen sich starten, Favoriten und Verlauf bleiben pro Profil gespeichert
- [ ] Keine fest eingebauten Farben im Code, alles aus den Theme-Dateien
- [ ] Einmal testweise auf `phoenix` umgestellt: der Launcher sieht aus wie der Phönix-Entwurf, ohne Codeänderung
- [ ] Commit-Nachricht vorgeschlagen
