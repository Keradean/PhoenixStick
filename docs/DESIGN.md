# PhönixStick Launcher: Design

Verbindliche Vorgaben für den Look des Launchers. Entworfen in Claude Design, abgesegnet von Dennis.

- **Aufbau:** Bühne mit Icon-Leiste, Reitern und Kategorie-Chips (Kino-Aufbau). Gilt für alle Themes.
- **Standard-Theme:** Phönix im Kino-Look. Tiefschwarz, Kino-Rot, hohe schmale Titel.
- **Zuerst gebaut:** Theme Sci-Fi (Arbeitstitel) für Dennis' Profil. Phönix muss mit demselben Code funktionieren.
- **Später:** Mondgarten (lila Fantasy).

Themes tauschen nur Werte aus `design/tokens/<theme>.json`, nie den Aufbau.

Referenzen in `design/referenz/`: HTML-Mockups aus dem Design-Tool, kein lauffähiger Code. Sie zeigen Maße, Farben und Abstände.

| Datei | Zeigt |
| --- | --- |
| `startbildschirm-phoenix.html` | **Maßgeblicher Aufbau** und Theme Phönix |
| `phoenix-hero.svg` | Bild der Bühne für Phönix |
| `startbildschirm-scifi.html` | Look von Sci-Fi. Noch im alten Aufbau ohne Icon-Leiste und Chips: nur Farben, Schriften und Formen übernehmen |
| `scifi-hintergrund.svg` | Raster und Radar für Sci-Fi zusammen (nur Referenz) |
| `scifi-hero.svg` | Bild der Bühne für Sci-Fi, 1632 × 480 |
| `scifi-raster.svg` | Hintergrund für Sci-Fi über den ganzen Bildschirm, 1920 × 1080 |
| `profilauswahl.html` | Aufbau der Profilauswahl. Farben aus dem jeweiligen Theme |
| `alle-apps-scifi.html` | **Alle Apps** im Sci-Fi-Look, maßgeblich für diese Seite |
| `profil-bearbeiten-scifi.html` | **Profil bearbeiten** im Sci-Fi-Look |
| `variante-a-raster.html` | ersetzt durch `alle-apps-scifi.html`, nicht mehr verwenden |
| `alt/` | Verworfene Entwürfe, nicht verwenden |

Wenn dieses Dokument und eine Referenz sich widersprechen, gilt dieses Dokument.

## Grundregeln

- **10-Fuß-Oberfläche:** Bedienung nur mit der Fernbedienung (Steuerkreuz, OK, Zurück, Home, Menü).
- **Fokus ist immer sichtbar.** Genau ein Element hat den Fokus.
- **Keine Werbung, keine Autoplay-Videos, kein Tracking, keine Netzwerkzugriffe.**
- **Bildrand für Fernseher (Overscan):** Viele Fernseher schneiden am Rand etwas ab. Der ganze Entwurf (1920 × 1080) wird deshalb in einen sicheren Bereich skaliert und mittig gesetzt. Standard: 5 % Rand an allen Seiten. In den Launcher-Einstellungen einstellbar von 0 bis 8 % in 1-%-Schritten, mit Vorschau-Rahmen in den Ecken. Hintergrundfarbe und Raster laufen trotzdem bis zum echten Bildschirmrand.
- **Skalierung:** Faktor = kleinerer Wert aus (sichere Breite / 1920) und (sichere Höhe / 1080). Nie nur nach der Breite skalieren.
- Alle Maße in **px bei 1920 × 1080**. Auf dem Stick prüfen: `adb shell wm size` und `adb shell wm density`. Bei Dichte 320 gilt dp = px / 2.

## Aufbau Startbildschirm (alle Themes)

```
┌────┬──────────────────────────────────────────────────────────┐
│ 🔥 │ PHÖNIXSTICK.   START  FAVORITEN  ALLE APPS     20:15 (D) │ Kopfzeile
│    │ ┌──────────────────────────────────────────────────────┐ │
│ ⌂▌ │ │ ZULETZT GEÖFFNET                 [ Theme-Bild ]      │ │
│ ☆  │ │ NETFLIX                                              │ │ Bühne (Karte)
│ ▦  │ │ Deine zuletzt gestartete App.                        │ │
│ ▭  │ │ VOR 2 STD. · STREAMING  [Favorit]                    │ │
│ ── │ │ [▶ ÖFFNEN] (−)                              ▬ ▬ ▬ ▬  │ │
│    │ └──────────────────────────────────────────────────────┘ │
│    │ DEINE APPS                              ALLE ANZEIGEN ›   │
│    │ [NETFLIX*] [PRIME VIDEO] [YOUTUBE] [DISNEY+] [CRUNCHY…    │ App-Reihe
│ ⚙  │ (ALLE) (STREAMING) (MEDIATHEKEN) (ANIME) (KIDS) (LIVE)    │ Kategorie-Chips
└────┴──────────────────────────────────────────────────────────┘
```

| Bereich | Maße |
| --- | --- |
| Icon-Leiste links | 128 breit, Trennlinie rechts 1 px. Logo oben (48 px), darunter Start, Favoriten, Alle Apps, Eingänge, Trennstrich, Einstellungen ganz unten. Punkte 72 × 72. Aktiver Punkt: getönte Fläche plus Balken 4 × 44 am linken Rand |
| Hauptbereich | Innenabstand oben 44, links 64, rechts 96, unten 36. Abstand zwischen den Blöcken 32 |
| Kopfzeile | Wortmarke "PHÖNIXSTICK." links, daneben im Abstand 72 die Reiter Start, Favoriten, Alle Apps (aktiver Reiter: Akzentfarbe, Unterstrich 2 px). Rechts Uhrzeit und Avatar 64 |
| Bühne | Karte, volle Breite, 480 hoch, Rahmen 1 px. Rechts das Theme-Bild, links davon eine schräg abgedeckte Fläche für den Text. Text ab 72/72. Unten rechts Punkte für die zuletzt geöffneten Apps |
| Bühnen-Inhalt | Eyebrow, App-Name, Untertitel, Meta-Zeile (Zeit seit Start, Kategorie, Favorit-Kennzeichen), Haupt-Button "Öffnen" mit Play-Symbol, runder Zweit-Button für Favorit an/aus |
| App-Reihe | Titel "Deine Apps" plus "Alle anzeigen ›". Kacheln 340 × 190, Abstand 24, läuft rechts aus dem Bild. Die zuletzt gestartete App bekommt das Label "ZULETZT" oben links |
| Chips | Kategorien zum Filtern der App-Reihe: Alle, Streaming, Mediatheken, Anime, Kids, Live. Aktiver Chip in Akzentfarbe. Kategorien pro App im Profil einstellbar, sinnvolle Vorbelegung für die installierten Apps |

**Steuerung:**
- Start-Fokus: erste Kachel der App-Reihe.
- Hoch/Runter wechselt zwischen Chips, App-Reihe, Bühnen-Buttons und Kopfzeile. Links von der ersten Kachel geht es in die Icon-Leiste.
- Der Inhalt der Bühne folgt der Kachel mit Fokus.
- Menü auf einer Kachel: Favorit an/aus, Kategorie ändern.
- Zurück auf dem Startbildschirm öffnet die Profilauswahl.

**Kachelbild:** Das Banner der App (Android liefert es mit). Gibt es keins, den App-Namen in der Titelschrift anzeigen.

## Theme Phönix (Standard)

Werte: `design/tokens/phoenix.json`. Referenz: `startbildschirm-phoenix.html`.

| Token | Wert | Wofür |
| --- | --- | --- |
| `background` | `#0B0A0A` | Tiefschwarz |
| `heroCard` | `#120F0F` | Bühnen-Karte |
| `surface` | `#151212` | Kacheln |
| `surfaceFocus` | `#1C1615` | Kachel mit Fokus |
| `borderSubtle` | `rgba(255,255,255,0.08)` | Rahmen von Karte und Kacheln |
| `borderChip` | `rgba(255,255,255,0.14)` | Rahmen der Chips |
| `textPrimary` | `#F2EDEA` | Titel |
| `textSecondary` | `#D8CFCB` | Untertitel |
| `textMuted` | `#A39A96` | Meta, inaktive Reiter und Icons |
| `accent` | `#E5322D` | Kino-Rot: aktiver Reiter, Eyebrow, Haupt-Button, Fokus, aktiver Chip, Logo |
| `focusGlow` | `rgba(229,50,45,0.45)` | roter Schein um den Fokus |

- **Schriften:** Antonio (hohe, schmale Titel, Großbuchstaben) und Manrope (Text, Reiter, Buttons). Beide SIL Open Font License, in die App einbauen.
- **Fokus Kachel:** Rahmen 2 px `accent`, roter Schein, Skalierung 1.05.
- **Fokus Haupt-Button:** Ring 4 px `#F2EDEA` und roter Schein.
- **Bühnen-Bild:** `phoenix-hero.svg`, ein roter Mond mit Federbögen, feinem Regen und Glutfunken.
- **Logo** in der Icon-Leiste: Linien-Phönix aus der Referenz, in `accent`.

## Theme Sci-Fi (Arbeitstitel): wird zuerst gebaut

Werte: `design/tokens/scifi.json`. Referenz für den Look: `startbildschirm-scifi.html`, Bühne `scifi-hero.svg`, Hintergrund `scifi-raster.svg`.

- **Stimmung:** Holo-Oberfläche auf einem Raumschiff. Fast schwarzes Nachtblau, Cyan, feine Linien statt Flächen.
- **Hintergrund:** feines Raster (48 px) über den ganzen Bildschirm, Radar-Kreise als Bild der Bühne.
- **Schriften:** Chakra Petch für Titel, Buttons, Kacheln (Großbuchstaben), IBM Plex Mono für Uhrzeit, Eyebrow, Abschnittstitel, Chips und Hinweise. SIL Open Font License.
- **Kacheln:** Ecken 4, dünner Cyan-Rahmen, Fläche leicht getönt.
- **Fokus:** Rahmen 2 px Cyan, Cyan-Schein, Skalierung 1.06 und vier weiße Eck-Klammern außen (22 px lang, 3 px dick, 10 px Abstand).
- **Haupt-Button:** Cyan-Fläche, dunkle Schrift, zwei schräg abgeschnittene Ecken (16 px).
- **Eyebrow** beginnt mit `// `.
- Icon-Leiste, Reiter und Chips gibt es auch hier, im selben Aufbau, nur im Sci-Fi-Look (eckig, Cyan, Mono-Schrift).

## Themes im Code

Aufbau einer Theme-Datei: `docs/THEME-SCHEMA.md`. Jede Theme-Datei hat einen Block `style`. Die Komponenten lesen ihn und zeichnen sich danach:

| Schlüssel | Phönix | Sci-Fi |
| --- | --- | --- |
| `tileShape` | `rounded` | `square` |
| `focusStyle` | `glowBorder` | `brackets` |
| `buttonShape` | `rounded` | `chamfer` |
| `labelCase` | `upper` | `upper` |
| `hasBackgroundGrid` | `false` | `true` |

- Im Code gibt es keine fest eingebauten Farben, Schriften oder Ecken.
- Das Theme hängt am Profil. Neue Profile bekommen `phoenix`, Dennis bekommt `scifi`.
- Ein neues Theme (Mondgarten) ist dann nur eine neue Datei.

## Bewegung

- Fokuswechsel: 150 ms, ease-out. Bühne blendet beim Fokuswechsel in 200 ms über.
- Keine Animation länger als 300 ms. Auf dem alten Stick zählt Flüssigkeit mehr als Effekte: lieber weglassen als ruckeln.

## Profilauswahl

Referenz: `profilauswahl.html` (Farben aus dem Theme).

- Erscheint beim Einschalten und über den Avatar.
- "Wer schaut?", darunter bis zu 4 Profile: Kreis 220 px, Name, darunter klein das Theme.
- Letzter Eintrag: "Profil hinzufügen".
- Pro Profil gespeichert: Name, Buchstabe, Theme, Favoriten, Kategorien der Apps, Verlauf "zuletzt geöffnet".

## Alle Apps

Referenz: `alle-apps-scifi.html`.

- Gleicher Rahmen wie der Startbildschirm: Icon-Leiste (Punkt "Alle Apps" aktiv), Kopfzeile mit Reiter "Alle Apps" aktiv, Chips unten.
- Oben die **Favoriten**: 5 Kacheln nebeneinander (Höhe 172, Abstand 24), rechts Zähler "05 / 12". Die zuletzt gestartete App trägt das Label "ZULETZT".
- Darunter durch eine feine Linie getrennt **Alle Apps A bis Z**: Raster mit 6 Spalten, Kacheln 140 hoch, Abstand 24, rechts "12 INSTALLIERT".
- Chips filtern beide Bereiche. Rechts unten die Hinweise "OK starten", "Menü Favorit".
- Zurück führt zum Startbildschirm.

## Profil bearbeiten

Referenz: `profil-bearbeiten-scifi.html`. Erreichbar über Menü-Taste auf einem Profil in der Profilauswahl und über den Avatar.

- Eigener Vollbild-Bildschirm ohne Icon-Leiste. Oben Eyebrow "PROFIL BEARBEITEN" und der Profilname groß.
- Links der Avatar (280 × 280) und die Auswahl des Buchstabens (vorgeschlagen aus dem Namen, "…" für jeden anderen).
- Rechts:
  - **Name** als Eingabefeld. OK öffnet die Bildschirmtastatur von Android.
  - **Theme** als drei Karten mit kleiner Vorschau. Das aktive Theme trägt "AKTIV". Themes, die es noch nicht gibt (Mondgarten), sind gestrichelt, mit "BALD" markiert und nicht wählbar.
  - **Favoriten und Kategorien** öffnet eine Liste aller Apps, in der man pro App Favorit an/aus und die Kategorie setzt.
  - **Verlauf leeren** mit Anzahl der Einträge.
  - **Profil löschen** in Warnfarbe, nur nach einer Bestätigungsabfrage. Das letzte Profil kann nicht gelöscht werden.
- Unten **Speichern** (Haupt-Button) und **Abbrechen**. Zurück verwirft die Änderungen.
- Ein Theme-Wechsel wird nach dem Speichern sofort sichtbar, ohne Neustart der App.
- Warnfarbe Sci-Fi: `#FF5F7E`. Gehört als `danger` in beide Theme-Dateien (Phönix: `#FF6A5F`), siehe `docs/THEME-SCHEMA.md`.

## Was der Launcher kann und was nicht

- **Kann:** installierte Apps anzeigen und starten, Favoriten und Kategorien pro Profil, "zuletzt geöffnet" aus dem eigenen Startverlauf.
- **Kann nicht:** Filmbilder, Fortschrittsbalken, Watchlist oder "Weiterschauen". Was in einer App läuft, verraten die Apps auf Fire OS 6 nicht. Nicht nachbauen und nicht vortäuschen.
