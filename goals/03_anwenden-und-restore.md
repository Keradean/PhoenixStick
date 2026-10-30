# Goal 03: Anwenden, Protokoll, Restore

## Ziel

`debloat` kann wirklich deaktivieren, jede Änderung ist protokolliert und lässt sich mit `restore` zurücknehmen. Nach einem Firmware-Update zeigt `check`, was wieder an ist.

## Voraussetzungen

- Goal 02 ist fertig und der Katalog ist von mir abgesegnet

## Aufgaben

1. `debloat ... --apply`
   - Probelauf anzeigen wie bisher
   - automatisch einen Snapshot speichern
   - Ja/Nein-Abfrage mit Anzahl der Pakete. Bei `risky` eine zweite Abfrage
   - Paket für Paket `pm disable-user --user 0 <paket>` über die schreibende Methode aus Goal 01
   - nach jedem Paket prüfen, ob es wirklich deaktiviert ist
   - `Cannot disable a protected package`: überspringen, protokollieren, weitermachen
   - jeder andere unerwartete Fehler: sofort stoppen
2. Änderungsprotokoll `state/changes.json`
   - pro Eintrag: Zeitpunkt, Paket, Aktion, Status vorher, Ergebnis, Meldung vom Gerät
   - wird nach jedem Paket geschrieben, nicht erst am Ende. So geht nach einem Absturz nichts verloren
3. Befehl `restore`
   - ohne Option: alles aus dem Protokoll, mit `--package <name>`: nur dieses Paket
   - Probelauf als Standard, `--apply` zum Ausführen
   - `pm enable --user 0 <paket>`, danach prüfen und protokollieren
4. Befehl `check`
   - vergleicht Protokoll und aktuellen Zustand
   - zeigt, was wieder aktiv ist (z. B. nach einem Update) und schlägt `debloat` erneut vor
5. Zusammenfassung am Ende jedes Laufs: deaktiviert, übersprungen, geschützt, Fehler

## Erster echter Test am Gerät

In dieser Reihenfolge, mit Pause nach jedem Schritt:

1. Ich suche ein einzelnes harmloses Paket aus dem `safe`-Katalog aus
2. Nur dieses eine Paket deaktivieren (`--package`)
3. Ich prüfe am Fernseher: Startbildschirm, Einstellungen, Fernbedienung, eine Streaming-App
4. `restore --package ... --apply` und nochmal prüfen
5. Erst wenn das klappt: `debloat --preset safe --apply`
6. Stick neu starten, nochmal alles prüfen, dann `check`

## Tests

- Ohne `--apply` wird die schreibende Methode nie aufgerufen
- Protokoll wird nach jedem Paket geschrieben
- Bei einem unerwarteten Fehler stoppt der Lauf und die bisherigen Einträge sind im Protokoll
- `restore` nimmt nur Pakete, die das Tool selbst deaktiviert hat, nie Pakete, die schon vorher aus waren
- `check` erkennt ein Paket, das wieder aktiv ist

## Fertig, wenn

- [ ] Alle Tests laufen ohne Fehler
- [ ] Der Test mit einem einzelnen Paket hat geklappt, inklusive `restore`
- [ ] `safe` ist angewendet, der Stick läuft nach einem Neustart normal
- [ ] Commit-Nachricht vorgeschlagen

## Nicht in diesem Goal

Telemetrie-Settings (`privacy`), DNS-Filter, Launcher.
