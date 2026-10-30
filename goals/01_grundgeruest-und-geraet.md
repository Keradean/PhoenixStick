# Goal 01: Grundgerüst und Gerät auslesen

## Ziel

Die Solution steht, das Tool kann sich mit dem Stick verbinden und alles auslesen. In diesem Goal wird am Gerät nichts geändert.

## Voraussetzungen

- `00_VORBEREITUNG.md` ist bis Schritt 6 erledigt
- In `docs/GERAET.md` steht die IP
- `adb devices` zeigt den Stick als `device`

## Aufgaben

1. `dotnet --list-sdks` prüfen und das neueste LTS-SDK als Target nehmen
2. Solution `PhoenixStick.sln` anlegen mit
   - `src/PhoenixStick` (Konsolenanwendung, Spectre.Console und Spectre.Console.Cli)
   - `tests/PhoenixStick.Tests` (xUnit)
3. `config.example.json` mit `host` und `port` anlegen. Das Tool liest `config.json`, `--host` überschreibt
4. `IAdbClient` und `AdbClient`
   - ruft `adb -s <host>:<port> ...` als Prozess auf
   - liefert Exit-Code, stdout und stderr als `record AdbResult` zurück
   - Timeout pro Befehl, Fehlermeldung auf Deutsch, wenn adb nicht installiert ist
   - Lesende und schreibende Befehle sind getrennte Methoden. Die schreibende Methode wirft ohne `--apply` eine Exception (siehe Sicherheitsregel 2 in `CLAUDE.md`). Sie wird in diesem Goal noch nirgends benutzt
5. `FakeAdbClient` für Tests, der vorbereitete Ausgaben zurückgibt
6. Befehl `device`
   - verbindet und prüft den Status (`device`, `unauthorized`, `offline`, nicht erreichbar)
   - bei `unauthorized` klarer Hinweis, dass am Fernseher bestätigt werden muss
   - zeigt Modellcode, Android-Version, SDK-Level und alles zu Fire OS, was in `getprop` steht
7. Befehl `inventory`
   - alle Pakete mit: Name, aktiviert/deaktiviert, System/Drittanbieter
   - Tabelle in der Konsole, gruppiert nach Präfix (`com.amazon.*`, `com.android.*`, Rest)
   - Export nach `state/inventory/<zeitstempel>.json`
8. Befehl `snapshot`
   - Pakete mit Status und `settings list global/secure/system` nach `state/snapshots/<zeitstempel>/`
9. Tests für das Parsen der Ausgaben von `pm list packages` (mit `-d`, `-s`, `-3`) und `getprop`, mit echten Beispielzeilen aus `backup/ausgangszustand/`
10. `docs/GERAET.md` mit den echten Werten aus `device` ergänzen. Nur Felder füllen, die das Gerät wirklich liefert

## Fertig, wenn

- [ ] `dotnet build` und `dotnet test` laufen ohne Fehler
- [ ] `dotnet run --project src/PhoenixStick -- device` zeigt die echten Gerätedaten
- [ ] `inventory` findet genauso viele Pakete, wie in `backup/ausgangszustand/pakete_alle.txt` stehen
- [ ] Ein Test beweist, dass die schreibende Methode ohne `--apply` wirft
- [ ] Kein Befehl hat am Gerät etwas geändert
- [ ] Du hast mir kurz erklärt, was ein `record` ist und warum `IAdbClient` ein Interface ist
- [ ] Commit-Nachricht vorgeschlagen

## Nicht in diesem Goal

Katalog, Debloat, Settings ändern.
