# PhönixStick

Ein eigenes Kommandozeilen-Tool in C#, das einen Amazon Fire TV Stick per ADB über WLAN entrümpelt: Amazon-Bloat und Telemetrie abschalten, alles umkehrbar und nachvollziehbar. Der Stick steigt sozusagen aus der Asche neu auf. Lern- und Portfolio-Projekt von Dennis.

Schreibweise:

- Im Text, in der README und in Ausgaben des Tools: **PhönixStick**
- Im Code, bei Namespaces, Solution, Projekten und Repo: **PhoenixStick** (keine Umlaute)
- Name des Befehls in der Konsole: `phoenixstick`
- In Beschreibungen heißt es "für Fire TV", der Markenname Fire TV kommt nicht in den Projektnamen

Gerätedaten und Entscheidungen, was bleiben muss:

@docs/GERAET.md

## Über mich

- Ich heiße Dennis. C#/Unity-Entwickler, außerhalb von Unity eher Einsteiger.
- Wenn du ein Konzept zum ersten Mal einsetzt (z. B. async/await, record, Interfaces für Tests, Dependency Injection), erklär es in zwei, drei Sätzen. Mit enums habe ich noch nicht gearbeitet.
- System: CachyOS, Shell ist fish. Befehle, die ich selbst ausführen soll, müssen in fish funktionieren.
- Git mache ich selbst. Du committest, pushst, stasht und resettest nie. Am Ende eines Arbeitsschritts schreibst du mir eine Commit-Nachricht (Conventional Commits, Englisch).

## Sicherheitsregeln

Diese Regeln gehen vor allem anderen. Der Stick wird im Alltag genutzt und muss danach ganz normal weiterlaufen.

1. Änderungen am Gerät laufen nur über unser Tool, nie über direkte adb-Befehle von dir. Direkt darfst du nur lesen (`getprop`, `pm list`, `settings get/list`, `dumpsys`).
2. Das Tool macht immer erst einen Probelauf und zeigt, was es tun würde. Geändert wird nur mit `--apply`. Schreibende adb-Befehle laufen im Code über genau eine Methode, die ohne `--apply` eine Exception wirft. Dafür gibt es einen Test.
3. Pakete werden ausschließlich mit `pm disable-user --user 0 <paket>` deaktiviert und mit `pm enable --user 0 <paket>` wieder aktiviert. Kein `pm uninstall`, kein `pm clear`, kein Root, nichts mit Recovery oder Flashen.
   Ausnahme (freigegeben am 30.10.2026): Drittanbieter-Apps dürfen über den Befehl `background` mit `appops set <app> RUN_IN_BACKGROUND ignore` am Autostart und Hintergrundlauf gehindert werden, zurück mit `allow`. Deaktiviert werden sie weiterhin nie.
4. Vor jedem `--apply` speichert das Tool einen Snapshot (Paketliste und Settings). Jede einzelne Änderung kommt ins Änderungsprotokoll, damit `restore` sie rückgängig machen kann.
5. Die Never-touch-Liste wird beim Laden aus allen Presets entfernt, egal was im Katalog steht. Dafür gibt es einen Test.
6. Pakete, die nicht im Katalog stehen, werden nie angefasst.
7. Settings: erst mit `settings get` den alten Wert lesen und sichern, dann setzen. Schlüssel, die es auf dem Gerät nicht gibt, werden nicht angelegt.
8. Wenn das Gerät etwas Unerwartetes meldet (z. B. `Cannot disable a protected package`), suchst du keinen anderen Weg drumherum. Überspringen, protokollieren, mir Bescheid geben.
9. Wenn ich am Fernseher etwas tun muss (Abfrage bestätigen, Menü prüfen, testen ob eine App noch startet), sag es klar und warte auf meine Antwort.

## Never touch (Mindestliste)

Wird in Goal 02 mit dem echten Inventar erweitert.

- `android`
- `com.android.systemui`
- `com.android.providers.settings`
- `com.android.providers.downloads`
- `com.amazon.tv.launcher` (Amazon-Startbildschirm)
- `com.amazon.firehomestarter`
- `com.amazon.tv.settings.v2` (Einstellungen)
- alles für Tastatur, Fernbedienungs-Pairing, Bluetooth, WLAN und HDMI-Eingänge. Die genauen Paketnamen erst aus dem Inventar ermitteln und mir zur Bestätigung zeigen

## Bekannte Stolperfallen

Aus anderen Projekten zum gleichen Thema:

- Amazon schützt manche Pakete, dann kommt `Cannot disable a protected package`. Welche das sind, hängt vom Build ab.
- Firmware-Updates können deaktivierte Pakete wieder einschalten. Fire OS schreibt manche Settings beim Booten neu. Das Tool muss deshalb beliebig oft laufen dürfen, ohne Schaden anzurichten, und braucht einen `check`-Befehl.
- Fire OS 8: Animationsfaktor unter 1 legt im Amazon-Menü Einstellungen und Eingänge lahm. Animationswerte nie unter 1 setzen.
- Prime Video startet nicht ohne Amazons Push-Dienst. Alexa hängt an mehreren Paketen. Was davon bleiben muss, steht in `docs/GERAET.md`.
- OTA-Update-Pakete abschalten heißt: keine Sicherheitsupdates mehr. Die gehören immer in die Kategorie `risky`.
- `cmd package set-home-activity` meldet auf Fire OS `Success`, ändert aber nichts. Erst wichtig, wenn wir später einen eigenen Launcher bauen.

## Dieses Gerät: Fire OS 6

Der Stick läuft mit Fire OS 6.7, das basiert auf Android 7.1. Das heißt:

- Paketlisten aus anderen Projekten gelten oft nur für Fire OS 7 oder 8. dumbtv hat eigene Listen für Fire OS 6 unter `data/fireos6/`, die sind die erste Referenz.
- `appops`-Regeln wie `RUN_ANY_IN_BACKGROUND` gibt es erst ab Android 9, hier also nicht. `RUN_IN_BACKGROUND` gibt es auf diesem Gerät aber (laut `dumpsys appops`).
- Der Hinweis zum Animationsfaktor betrifft Fire OS 8. Unter 1 wird trotzdem nichts gesetzt.
- Für einen eigenen Launcher später: Auf 6.7 lässt Amazon den Weg über einen Bedienungshilfe-Dienst (Accessibility Service) nicht zu. dumbtv nutzt dort einen Vordergrund-Dienst.

## Tech-Stack

- C# Konsolenanwendung. Target-Framework ist das neueste installierte LTS-SDK (`dotnet --list-sdks`).
- adb wird als externer Prozess aufgerufen (`System.Diagnostics.Process`), gekapselt hinter `IAdbClient`. Keine ADB-Bibliothek: so bleibt jeder Befehl sichtbar und kann im Probelauf einfach ausgegeben werden.
- Spectre.Console und Spectre.Console.Cli für Befehle, Tabellen, Farben und Ja/Nein-Abfragen.
- Katalog und Protokolle als JSON mit `System.Text.Json`.
- Tests mit xUnit. In Tests ersetzt ein `FakeAdbClient` das echte Gerät. Tests laufen nie gegen den echten Stick.
- Verbindungsdaten in `config.json` (gitignored), Vorlage `config.example.json`. Mit `--host` überschreibbar.

## Projektstruktur (Ziel)

```
PhoenixStick/
├── CLAUDE.md
├── 00_VORBEREITUNG.md
├── .claude/settings.json
├── config.example.json
├── docs/GERAET.md
├── goals/                  Arbeitsaufträge, eins nach dem anderen
├── catalog/
│   ├── never-touch.json
│   └── fireos.json         Pakete mit Kategorie und Begründung
├── src/PhoenixStick/
├── tests/PhoenixStick.Tests/
├── backup/                 gitignored, Ausgangszustand von Hand gesichert
└── state/                  gitignored, Snapshots und Änderungsprotokoll
```

## Geplante Befehle

| Befehl | Was er macht | Ändert das Gerät |
| --- | --- | --- |
| `device` | Verbindung prüfen, Modell, Fire OS, Android-Version | nein |
| `inventory` | alle Pakete mit Status, Abgleich mit Katalog, Unbekannte markieren | nein |
| `snapshot` | Pakete und Settings nach `state/snapshots/<zeitstempel>/` | nein |
| `debloat --preset safe` | zeigt, was deaktiviert würde und warum | nein |
| `debloat --preset safe --apply` | deaktiviert und protokolliert | ja |
| `restore` | macht Änderungen aus dem Protokoll rückgängig | nur mit `--apply` |
| `check` | nach einem Update: was ist wieder an | nein |
| `privacy` | Telemetrie-Settings lesen und setzen | nur mit `--apply` |

Ablauf bei `--apply`:

```
Katalog laden -> Never-touch rausfiltern -> Preset wählen
-> Probelauf anzeigen -> Snapshot speichern -> Ja/Nein von mir
-> Paket für Paket deaktivieren -> Ergebnis prüfen -> Protokoll schreiben
-> beim ersten unerwarteten Fehler stoppen
```

## Katalog-Format

```json
{
  "package": "com.amazon.beispiel",
  "label": "Kurzer Name",
  "category": "safe",
  "reason": "Wofür das Paket da ist und warum es weg kann",
  "breaks": "Was danach nicht mehr geht, leer wenn nichts bekannt",
  "source": "Woher die Info stammt (Repo, eigener Test am Gerät)"
}
```

Kategorien:

- `safe`: Werbung, Telemetrie, Bloat ohne bekannte Nebenwirkung
- `optional`: Funktionen, die man nutzen könnte (Alexa, Prime Video, Fotos, Spiele)
- `risky`: Abhängigkeiten zu anderen Paketen, Updates, alles mit unklarer Wirkung

Presets: `safe` = nur safe, `optional` = safe + optional, `risky` = alles. Für `risky` zusätzlich eine zweite Bestätigung.

## Telemetrie-Settings (Kandidaten)

Stammen aus anderen Projekten. Erst mit `settings get` prüfen, ob es den Schlüssel auf diesem Gerät gibt.

| Tabelle | Schlüssel | Zielwert |
| --- | --- | --- |
| global | `amazon:device_metrics_opt_in` | 0 |
| global | `limit_ad_tracking` | 1 |
| global | `amazon:interest_based_ads` | 0 |
| global | `amazon:data_monitoring_consent` | 0 |
| global | `amazon:acr_enabled` | 0 |
| global | `amazon:sidewalk_enabled` | 0 |
| secure | `usage_stats` | 0 |

## Referenzen

Nur lesen, um Paketnamen und Erfahrungen nachzuschlagen. Keinen Code übernehmen. Wenn eine Info von dort in den Katalog wandert, kommt das Repo ins Feld `source`.

- https://github.com/humblemuzzu/dumbtv (Fire OS 6 bis 8, Paketlisten, Stolperfallen)
- https://github.com/WB2024/firestrip (MIT, Presets, Telemetrie-Settings)
- https://github.com/Sarg338/Homestead (Launcher, Hinweise zu Fire OS 8)
- https://github.com/man15h/tellyscope (GPL-3.0, Debloat per Handy)
- https://github.com/Ansub/tv-detox (Android TV, Prompt für Claude Code)

## Wie du mit mir arbeitest

- Ich gebe dir Goal-Dateien aus `goals/`, immer eine nach der anderen. Erst den Plan in drei bis sechs Punkten zeigen, dann umsetzen.
- Bezeichner auf Englisch. Kommentare und Ausgaben des Tools auf Deutsch.
- Private Felder mit Unterstrich: `_adb`, `_catalog`.
- Kommentare wie ein normaler Entwickler: kurz, sachlich, nur wo der Code es nicht selbst sagt. Keine Gedankenstriche als Stilmittel, keine Floskeln. Gilt auch für README und Ausgaben.
- Lieber kleine Klassen mit einer Aufgabe als eine große `Program.cs`.
- Am Ende jedes Goals: was fertig ist, was ich am Fernseher testen soll, Commit-Nachricht.

## Fahrplan

1. Grundgerüst, `device`, `inventory`, `snapshot` (nur lesen)
2. Katalog aufbauen, Probelauf für `debloat`
3. `--apply`, Änderungsprotokoll, `restore`, `check`
4. `privacy` für Telemetrie-Settings
5. DNS-Filter für Amazon-Tracking auf dem Homeserver (AdGuard Home oder Pi-hole im Docker auf Unraid). Vorsicht: zu aggressive Listen legen Prime Video, Appstore und Updates lahm
6. Optional: eigener Launcher als Android-App. Ersetzt endgültig das Werbebanner oben im Amazon-Startbildschirm, das zu `com.amazon.tv.launcher` gehört und deshalb nicht per Paket entfernt wird. Design-Vorgaben: `docs/DESIGN.md` und `design/tokens/`, erster Schritt (Theme Sci-Fi) in `goals/06_launcher-erster-schritt.md`
