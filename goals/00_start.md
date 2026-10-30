# Goal 00: Übergabe und Start

Übergabe aus dem Chat mit Claude. Lies zuerst `CLAUDE.md` und `docs/GERAET.md`.

## Stand (30.10.2026)

- Projekt PhönixStick liegt in `/mnt/storage/Claude/PhoenixStick`, Git-Repo in GitKraken angelegt, erster Commit ist drin
- Fire TV Stick mit Fire OS 6.7.1.1 (Android 7.1), IP 192.168.178.26, feste IP in der FRITZ!Box eingerichtet
- ADB-Debugging ist an, der PC ist am TV freigegeben, `adb devices` zeigt `192.168.178.26:5555 device`
- Auf dem PC installiert: `android-tools` (adb), .NET SDK 10.0.113
- `.claude/settings.json` mit den Berechtigungsregeln ist aktiv
- Schritte 1 bis 4 aus `00_VORBEREITUNG.md` sind erledigt

## Deine Aufgaben jetzt

1. Prüf mit `adb devices`, ob der Stick noch verbunden ist. Wenn nicht: `adb connect 192.168.178.26:5555`, und sag mir, falls ich am TV etwas bestätigen muss
2. Mach Schritt 5 (Lesetest) und Schritt 6 (Ausgangszustand sichern nach `backup/ausgangszustand/`) aus `00_VORBEREITUNG.md`. Nur Lesebefehle, am Gerät wird nichts geändert
3. Trag in `docs/GERAET.md` ein, was du sicher aus dem Gerät lesen kannst: Modellcode, Android-Version, SDK-Level, Anzahl Pakete, Datum der Sicherung. Den Gerätenamen aus dem Info-Menü frag mich, nicht raten
4. Zeig mir eine kurze Übersicht: wie viele Pakete insgesamt, wie viele `com.amazon.*`, wie viele Drittanbieter-Apps, schon deaktivierte Pakete
5. Frag mich danach die Punkte aus dem Abschnitt **Was bleiben muss** in `docs/GERAET.md` ab (Alexa, Prime Video, Updates, Apps) und trag meine Antworten ein
6. Stopp. Goal 01 übergebe ich dir danach selbst

## Fertig, wenn

- [ ] `backup/ausgangszustand/` enthält alle Dateien aus Schritt 6
- [ ] `docs/GERAET.md` ist so weit ausgefüllt, wie es sicher geht
- [ ] Am Gerät wurde nichts geändert
- [ ] Commit-Nachricht vorgeschlagen (nur `docs/GERAET.md` und `goals/00_start.md`, `backup/` ist gitignored)
