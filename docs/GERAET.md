# Gerät

Wird bei der Vorbereitung von Dennis ausgefüllt und in Goal 01 von Claude Code ergänzt. Felder, die leer sind, sind noch nicht bekannt. Nicht raten.

## Steckbrief

| Feld | Wert |
| --- | --- |
| Gerätename (Info-Menü) | Dennis' Fire TV |
| Softwareversion (Info-Menü) | 6.7.1.1 (N56711/5908) |
| Modellcode (`ro.product.model`) | AFTMM (`ro.product.device`: mantis) |
| Android-Version (`ro.build.version.release`) | 7.1.2 |
| SDK-Level (`ro.build.version.sdk`) | 25 |
| Hersteller (`ro.product.manufacturer`) | Amazon |
| Fire OS (`ro.build.version.name`) | Fire OS 6.7.1.1 (NS6711/5908) |
| Sicherheitspatch (`ro.build.version.security_patch`) | 2025-11-01 |
| Build-Datum (`ro.build.date`) | Sat Mar 21 00:56:27 UTC 2026 |
| IP-Adresse | 192.168.178.26 |
| ADB-Port | 5555 |
| Feste IP im Router eingerichtet | ja |
| Anzahl Pakete im Ausgangszustand | 235 (223 System, 12 Drittanbieter, 0 deaktiviert) |
| Ausgangszustand gesichert am | 30.10.2026 |

## Was bleiben muss

| Frage | Antwort |
| --- | --- |
| Alexa / Sprachsuche nutzen | wird erst später genutzt. Das Tool muss mit und ohne Alexa funktionieren. Alexa-Pakete kommen in eine eigene Gruppe in der Kategorie `optional` und werden vorerst nicht deaktiviert |
| Prime Video nutzen | ja, muss voll funktionieren, inklusive aller Abhängigkeiten (z. B. Amazons Push-Dienst). Werbung im Startbildschirm und Tracking dürfen weg |
| Vorschau im Startbildschirm | muss bleiben (z. B. Netflix-Vorschau). Werbung soll trotzdem weg: über DNS-Filter (Schritt 5) oder eigenen Code, nicht durch Abschalten von `com.amazon.sneakpeek` |
| Amazon-Apps und Funktionen | nicht genutzt: Music, Photos, Silk, IMDb, Freevee, Kids, VoiceView, Fernhilfe, Nachrichten, "Schaust du noch?". Bleiben soll nur der Spiele-Bereich (`gamehub`, `recess`, eingetragen unter `keep` in `config.json`) |
| Ziel Performance | Stick soll flüssiger laufen. Vorher und nachher messen, nicht nach Gefühl |
| Amazon-Systemupdates weiter erlauben | ja (Sicherheitsupdates). OTA-Pakete kommen auf die Never-touch-Liste. Nach Updates prüft `check`, was wieder aktiv ist |
| Apps, die auf jeden Fall laufen müssen | alle installierten Drittanbieter-Apps (`pm list packages -3`). Sie werden nie angefasst. Macht eine App Probleme, wird das vorher mit Dennis besprochen |
| Sonstiges, das nicht kaputt gehen darf | HDMI-CEC (TV an/aus und Lautstärke über die Stick-Fernbedienung) und Bluetooth (Fernbedienung des 4K-Sticks, Kopfhörer, Lautsprecher, Gamepads): alles zu Bluetooth-Stack und Fernbedienungs-Pairing ist never touch. Dazu WLAN, Tastatur, HDMI-Eingänge. Bildschirmspiegelung (Miracast) kommt in `optional` und bleibt vorerst aktiv |

## Notizen

- Drittanbieter-Apps im Ausgangszustand: Netflix, Disney+, YouTube (`com.amazon.firetv.youtube`), HBO, Crunchyroll, Twitch, ZDF, ARD, 7TV, DMAX, Toggo, `com.amazon.spiderpork`
- 30.10.2026: Dennis hat Joyn/7TV, ZDF-Mediathek und ARD-Mediathek selbst am TV deinstalliert (nicht genutzt). Jetzt 9 Drittanbieter-Apps: Netflix, Disney+, YouTube, HBO, Crunchyroll, Twitch, DMAX, Toggo, `com.amazon.spiderpork`. Alle 9 im Hintergrund gesperrt
- `com.amazon.spiderpork` ist als Drittanbieter-App installiert, laut dumbtv ein Platzhalter für Amazon Luna (Cloud-Gaming)
- Softwareversion: Im Info-Menü notiert als `N56711`, `getprop` liefert `NS6711` (`ro.build.display.id`). Vermutlich ein S, das wie eine 5 aussieht
- 30.10.2026: Preset `safe` angewendet (25 Pakete), nach Neustart alles in Ordnung, `check` ohne Befund. Einzige sichtbare Änderung: Die Vorschau im Startbildschirm, z. B. bei Netflix, fehlte. Ursache `com.amazon.sneakpeek`, es spielt Vorschau und Werbung ab. Im Katalog nach `optional`, Gruppe `preview` verschoben und wieder aktiviert. Danach geht die Vorschau bei allen Apps außer Netflix. Offen, Dennis: nicht dringend. Verdacht: Cache des Startbildschirms oder `hybridadidservice`/`privacypassservice`
- 30.10.2026: `optional` (14 Pakete, Spiele-Bereich behalten) und `background` (alle 12 Drittanbieter-Apps) angewendet. Messung jeweils 8 bis 10 min nach Neustart, Bildschirmschoner an: RAM frei 339 → 362 → 415 MB, belegt 1084 → 999 MB. Netflix im Hintergrund 111 → 11 MB. 7TV läuft trotz Sperre weiter (57 MB, Firebase-Dienst `SessionLifecycleService`, wird vom System neu gestartet). Fire OS 6 setzt `RUN_IN_BACKGROUND` also nicht bei allen Apps durch
- Werbebanner oben im Startbildschirm gehört zu `com.amazon.tv.launcher` (never touch). Wird nicht über Pakete entfernt, sondern später durch den eigenen Launcher (Fahrplan Stufe 6) ersetzt
- Settings für Autoplay, gefunden am 30.10.2026, für `privacy` (Goal 04), noch nichts geändert:
  - global `com.amazon.tv.settings.featured_item_video_enabled=1`: Video-Autoplay vorgestellter Inhalte. Auf 0 fallen vermutlich auch die gewünschten Vorschauen weg
  - global `com.amazon.tv.settings.featured_item_video_volume=1.0`: Ton beim Autoplay. 0 wäre Vorschau ohne Ton
  - secure `USAGE_METRICS_UPLOAD_ENABLED=1`, `amz_limit_ad_tracking=0`, `non_amz_limit_ad_tracking=0`: weitere Kandidaten
- `limit_ad_tracking` liegt auf diesem Gerät in der Tabelle `secure`, nicht in `global` wie in der Kandidatenliste in `CLAUDE.md`. Daneben gibt es `amz_limit_ad_tracking` und `non_amz_limit_ad_tracking`. Wichtig für Goal 04

