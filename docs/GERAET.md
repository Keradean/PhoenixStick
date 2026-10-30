# Gerät

Wird bei der Vorbereitung von Dennis ausgefüllt und in Goal 01 von Claude Code ergänzt. Felder, die leer sind, sind noch nicht bekannt. Nicht raten.

## Steckbrief

| Feld | Wert |
| --- | --- |
| Gerätename (Info-Menü) | |
| Softwareversion (Info-Menü) | 6.7.1.1 (N56711/5908) |
| Modellcode (`ro.product.model`) | AFTMM (`ro.product.device`: mantis) |
| Android-Version (`ro.build.version.release`) | 7.1.2 |
| SDK-Level (`ro.build.version.sdk`) | 25 |
| IP-Adresse | 192.168.178.26 |
| ADB-Port | 5555 |
| Feste IP im Router eingerichtet | ja |
| Anzahl Pakete im Ausgangszustand | 235 (223 System, 12 Drittanbieter, 0 deaktiviert) |
| Ausgangszustand gesichert am | 30.10.2026 |

## Was bleiben muss

| Frage | Antwort |
| --- | --- |
| Alexa / Sprachsuche nutzen | wird erst später genutzt. Das Tool muss mit und ohne Alexa funktionieren. Alexa-Pakete kommen in eine eigene Gruppe in der Kategorie `optional` und werden vorerst nicht deaktiviert |
| Prime Video nutzen | ja, muss voll funktionieren, inklusive aller Abhängigkeiten (z. B. Amazons Push-Dienst). Werbung im Startbildschirm, automatische Trailer und Tracking dürfen weg |
| Amazon-Systemupdates weiter erlauben | ja (Sicherheitsupdates). OTA-Pakete kommen auf die Never-touch-Liste. Nach Updates prüft `check`, was wieder aktiv ist |
| Apps, die auf jeden Fall laufen müssen | alle installierten Drittanbieter-Apps (`pm list packages -3`). Sie werden nie angefasst. Macht eine App Probleme, wird das vorher mit Dennis besprochen |
| Sonstiges, das nicht kaputt gehen darf | |

## Notizen

- Drittanbieter-Apps im Ausgangszustand: Netflix, Disney+, YouTube (`com.amazon.firetv.youtube`), HBO, Crunchyroll, Twitch, ZDF, ARD, 7TV, DMAX, Toggo, `com.amazon.spiderpork`
- `com.amazon.spiderpork` ist als Drittanbieter-App installiert, Zweck noch unklar

