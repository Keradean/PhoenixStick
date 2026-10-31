# PhönixStick

Kommandozeilen-Tool in C#, das einen Fire TV Stick per ADB über WLAN entrümpelt. Es schaltet Amazon-Bloat, Werbung und Telemetrie ab. Jede Änderung wird protokolliert und lässt sich rückgängig machen. Der Stick steigt sozusagen aus der Asche neu auf.

Lern- und Portfolio-Projekt. Entwickelt und getestet auf einem Fire TV Stick 4K (AFTMM) mit Fire OS 6.7.1.1 (Android 7.1.2).

## Grundsätze

- **Erst schauen, dann ändern.** Jeder Befehl macht zuerst einen Probelauf und zeigt, was er tun würde. Geändert wird nur mit `--apply`, nach einer Ja/Nein-Abfrage.
- **Alles umkehrbar.** Pakete werden nur deaktiviert (`pm disable-user --user 0`), nie deinstalliert. Kein `pm clear`, kein Root, kein Flashen. Vor jedem `--apply` entsteht ein Snapshot, jede Änderung landet im Protokoll, `restore` nimmt sie zurück.
- **Nur was im Katalog steht.** Pakete, die nicht im Katalog stehen, werden nie angefasst. Die Never-touch-Liste (Systemkern, Startbildschirm, Einstellungen, Bluetooth, WLAN, HDMI-CEC, Tastatur, OTA-Updates) gewinnt immer, egal was in einem Preset steht.
- **Kein Umweg.** Meldet das Gerät etwas Unerwartetes, etwa `Cannot disable a protected package`, überspringt das Tool das Paket und meldet es.
- **Messen statt raten.** `perf` misst RAM und CPU vorher und nachher.

Alle schreibenden adb-Befehle laufen im Code durch genau eine Methode. Ohne `--apply` wirft sie eine Exception, und eine Whitelist lässt nur die erlaubten Befehle durch. Beides ist mit Tests abgesichert.

## Voraussetzungen

- .NET SDK 10
- `adb` (Android Platform Tools)
- Fire TV Stick mit Fire OS 6, 7 oder 8. Geräte mit Vega OS (Version beginnt mit 1.) haben kein ADB
- Auf dem Stick: **Einstellungen > Mein Fire TV > Entwickleroptionen > ADB-Debugging: AN**
- Eine feste IP für den Stick im Router

Unter Arch/CachyOS:

```bash
sudo pacman -S --needed android-tools dotnet-sdk
```

## Einrichten

```bash
git clone https://github.com/<benutzer>/PhoenixStick.git
```

```bash
cd PhoenixStick
```

```bash
cp config.example.json config.json
```

In `config.json` die IP des Sticks eintragen:

```json
{
  "host": "192.168.178.**",
  "port": 5555,
  "keep": [],
  "restrictBackground": [],
  "adGuard": {
    "url": "http://192.168.178.*",
    "user": "admin",
    "password": "hier-eintragen"
  }
}
```

| Feld | Bedeutung |
| --- | --- |
| `host`, `port` | Adresse des Sticks. Mit `--host` überschreibbar |
| `keep` | Pakete, die trotz Preset bleiben sollen |
| `restrictBackground` | Drittanbieter-Apps für den Befehl `background` |
| `adGuard` | Zugang zu AdGuard Home für den Befehl `dns`. Das Passwort geht auch über die Umgebungsvariable `PHOENIXSTICK_ADGUARD_PASSWORD` |

`config.json`, `state/` und `backup/` stehen in der `.gitignore` und bleiben lokal.

Verbindung testen (beim ersten Mal am Fernseher die ADB-Abfrage bestätigen):

```bash
dotnet run --project src/PhoenixStick -- device
```

Das Tool liest `catalog/` und schreibt nach `state/` im aktuellen Ordner. Deshalb immer aus dem Hauptordner des Repos starten.

## Befehle

| Befehl | Was er macht | Ändert das Gerät |
| --- | --- | --- |
| `device` | Verbindung prüfen, Modell, Fire OS, Android-Version | nein |
| `inventory` | alle Pakete mit Status, Abgleich mit dem Katalog, Unbekannte markiert | nein |
| `snapshot` | Pakete und Settings nach `state/snapshots/<zeitstempel>/` | nein |
| `debloat --preset <preset>` | Pakete aus dem Katalog deaktivieren | nur mit `--apply` |
| `restore` | Änderungen aus dem Protokoll zurücknehmen | nur mit `--apply` |
| `check` | nach einem Update prüfen, ob die Änderungen noch wirken | nein |
| `perf` | RAM und CPU messen und mit der letzten Messung vergleichen | nein |
| `background` | Drittanbieter-Apps Autostart und Hintergrundlauf verbieten | nur mit `--apply` |
| `privacy` | Telemetrie- und Werbe-Settings setzen | nur mit `--apply` |
| `dns` | DNS-Anfragen des Sticks aus AdGuard Home auswerten | nein |

Alle Befehle verstehen `--host <ip[:port]>`. Hilfe zu jedem Befehl gibt es mit `--help`.

### Typischer Ablauf

Probelauf:

```bash
dotnet run --project src/PhoenixStick -- debloat --preset safe
```

Wirklich anwenden:

```bash
dotnet run --project src/PhoenixStick -- debloat --preset safe --apply
```

Danach den Stick neu starten, alles am Fernseher ausprobieren und prüfen:

```bash
dotnet run --project src/PhoenixStick -- check
```

Alles zurücknehmen:

```bash
dotnet run --project src/PhoenixStick -- restore --apply
```

Nur ein einzelnes Paket zurücknehmen:

```bash
dotnet run --project src/PhoenixStick -- restore --package com.amazon.beispiel --apply
```

### Presets und Gruppen

| Preset | Inhalt |
| --- | --- |
| `safe` | Werbung, Telemetrie und Bloat ohne bekannte Nebenwirkung |
| `optional` | `safe` plus Funktionen, die man nutzen könnte (Music, Photos, Silk, Kids usw.) |
| `risky` | alles, auch Pakete mit Abhängigkeiten oder unklarer Wirkung. Verlangt eine zweite Bestätigung |

Einige Pakete aus `optional` gehören zu Gruppen und bleiben, bis man sie ausdrücklich mitnimmt:

| Gruppe | Inhalt |
| --- | --- |
| `alexa` | Sprachsteuerung und Alexa-Dienste |
| `mirroring` | Bildschirmspiegelung (Miracast) |
| `preview` | Vorschau-Videos im Startbildschirm (`com.amazon.sneakpeek`) |

```bash
dotnet run --project src/PhoenixStick -- debloat --preset optional --include mirroring
```

## Katalog

Die Paketlisten liegen als JSON in `catalog/`:

| Datei | Inhalt |
| --- | --- |
| `never-touch.json` | Pakete, die nie angefasst werden |
| `fireos.json` | Pakete mit Kategorie, Begründung, bekannten Nebenwirkungen und Quelle |
| `privacy.json` | Telemetrie- und Werbe-Settings mit Zielwert |
| `dns.json` | bekannte Werbe- und Tracking-Adressen sowie Adressen, die nie gesperrt werden dürfen |

Ein Eintrag in `fireos.json`:

```json
{
  "package": "com.amazon.beispiel",
  "label": "Kurzer Name",
  "category": "safe",
  "reason": "Wofür das Paket da ist und warum es weg kann",
  "breaks": "Was danach nicht mehr geht, leer wenn nichts bekannt",
  "source": "Woher die Info stammt"
}
```

Der Katalog ist auf Fire OS 6.7 abgestimmt. Auf anderen Versionen erst `inventory` laufen lassen und die Unbekannten prüfen.

## Was bewusst bleibt

- **OTA-Updates.** Ohne sie gibt es keine Sicherheitsupdates mehr. Nach einem Update zeigt `check`, was Fire OS wieder eingeschaltet hat. Das Tool darf beliebig oft laufen.
- **Prime Video** samt Amazons Push-Dienst, ohne den es nicht startet.
- **Amazon-Startbildschirm.** Das Werbebanner gehört zum Launcher selbst und lässt sich nicht über Pakete entfernen. Dafür gibt es den DNS-Filter.
- **Drittanbieter-Apps** werden nie deaktiviert. `background` sperrt höchstens ihren Hintergrundlauf (`appops set <app> RUN_IN_BACKGROUND ignore`, zurück mit `allow`). Nicht jede App hält sich daran.

## DNS-Filter

Werbung im Startbildschirm lässt sich am besten über DNS sperren, zum Beispiel mit AdGuard Home. Das Vorgehen: ein, zwei Tage nur beobachten, mit `dns` auswerten, dann Adressen einzeln sperren und nach jeder Sperre am Fernseher testen. Zu breite Sperrlisten legen Prime Video, Appstore und Updates lahm. Die ganze Anleitung steht in [docs/DNS.md](docs/DNS.md).

## Ergebnisse auf dem Fire TV Stick 4K

Gemessen jeweils 8 bis 10 Minuten nach einem Neustart:

| Stand | RAM frei |
| --- | --- |
| Ausgangszustand | 339 MB |
| nach `safe` und `optional` | 362 MB |
| nach `background` | 415 MB |

Netflix belegte im Hintergrund danach 11 statt 111 MB. Weitere Notizen zum Gerät stehen in [docs/GERAET.md](docs/GERAET.md).

## Entwicklung

```bash
dotnet build
```

```bash
dotnet test
```

Die Tests laufen nie gegen ein echtes Gerät: Ein `FakeAdbClient` ersetzt den Stick.

```
PhoenixStick/
├── catalog/                Paketlisten und Settings als JSON
├── docs/                   Gerätenotizen, DNS-Anleitung
├── src/PhoenixStick/
│   ├── Adb/                adb als Prozess, Whitelist der erlaubten Befehle
│   ├── Catalog/            Katalog laden, Presets, Never-touch-Filter
│   ├── Changes/            Änderungsprotokoll, Abgleich mit dem Gerät
│   ├── Commands/           ein Befehl pro Klasse
│   ├── Debloat/            Planung und Paketbefehle
│   ├── Dns/                AdGuard-Home-Abfrageprotokoll auswerten
│   ├── Parsing/            Ausgaben von pm, getprop, settings, dumpsys
│   ├── Perf/               RAM- und CPU-Messung
│   └── Privacy/            Settings lesen und setzen
└── tests/PhoenixStick.Tests/
```

Technik: .NET 10, [Spectre.Console](https://spectreconsole.net/) für die Ausgabe und die Befehle, `System.Text.Json`, xUnit. adb wird bewusst als externer Prozess aufgerufen statt über eine Bibliothek, damit jeder Befehl sichtbar bleibt.

## Quellen

Paketnamen und Erfahrungen stammen unter anderem aus diesen Projekten, die Quelle steht beim jeweiligen Katalogeintrag. Code wurde nicht übernommen.

- [humblemuzzu/dumbtv](https://github.com/humblemuzzu/dumbtv)
- [WB2024/firestrip](https://github.com/WB2024/firestrip)
- [Sarg338/Homestead](https://github.com/Sarg338/Homestead)
- [man15h/tellyscope](https://github.com/man15h/tellyscope)
- [Ansub/tv-detox](https://github.com/Ansub/tv-detox)

## Hinweis

Benutzung auf eigene Gefahr. Das Tool deaktiviert nur Pakete und setzt Settings, beides lässt sich mit `restore` zurücknehmen. Firmware-Updates von Amazon können trotzdem jederzeit etwas ändern. Fire TV ist eine Marke von Amazon. Dieses Projekt steht in keiner Verbindung zu Amazon.
