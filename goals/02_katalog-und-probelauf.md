# Goal 02: Katalog aufbauen und Probelauf

## Ziel

Für jedes Amazon-Paket auf diesem Stick wissen wir, was es macht und ob es weg kann. `debloat` zeigt als Probelauf, was passieren würde. Am Gerät wird weiterhin nichts geändert.

## Voraussetzungen

- Goal 01 ist fertig
- Der Abschnitt **Was bleiben muss** in `docs/GERAET.md` ist ausgefüllt. Wenn nicht: stoppen und mich fragen

## Aufgaben

1. Aktuelles Inventar holen und alle `com.amazon.*`-Pakete sowie auffällige Fremdpakete auflisten
2. Pro Paket recherchieren, wofür es da ist (Referenz-Repos aus `CLAUDE.md`, Paketname, `dumpsys package`). Wenn du dir unsicher bist: `risky`, nie `safe`
3. `catalog/never-touch.json` anlegen: Mindestliste aus `CLAUDE.md` plus die Pakete für Tastatur, Fernbedienung, Bluetooth, WLAN und HDMI-Eingänge, die du im Inventar findest
4. `catalog/fireos.json` im Format aus `CLAUDE.md` anlegen. Entscheidungen aus `docs/GERAET.md` berücksichtigen: was ich nutze, kommt nicht in `safe`
5. **Stopp:** Zeig mir den Katalog als Tabelle (Paket, Kategorie, Grund, was kaputt gehen kann). Ich gehe ihn durch und sage, was ich ändern will. Erst danach weiter
6. Kategorie als `enum` im Code. Erklär mir kurz, wie enums in C# funktionieren und wie sie aus JSON gelesen werden
7. `CatalogLoader`: lädt beide Dateien, entfernt Never-touch-Pakete aus allen Kategorien, warnt bei doppelten Einträgen
8. Befehl `debloat --preset safe|optional|risky` (ohne `--apply`)
   - nimmt nur Pakete, die auf dem Gerät existieren und gerade aktiviert sind
   - Tabelle: Paket, Kategorie, Grund, was kaputt gehen kann, exakter adb-Befehl
   - darunter: was übersprungen wird und warum (never touch, nicht im Katalog, schon deaktiviert)
9. `inventory` zeigt jetzt zusätzlich die Kategorie oder "unbekannt"

## Tests

- Never-touch gewinnt immer, auch wenn ein Paket im Katalog als `safe` steht
- Pakete, die nicht im Katalog stehen, tauchen nie im Probelauf auf
- Presets enthalten die richtigen Kategorien
- Bereits deaktivierte Pakete werden übersprungen
- Kaputtes oder leeres JSON gibt eine verständliche Fehlermeldung

## Fertig, wenn

- [ ] Ich habe den Katalog abgesegnet
- [ ] `dotnet test` läuft ohne Fehler
- [ ] `debloat --preset safe` zeigt eine nachvollziehbare Liste
- [ ] Am Gerät hat sich nichts geändert (`pm list packages -d` wie vorher)
- [ ] Commit-Nachricht vorgeschlagen

## Nicht in diesem Goal

`--apply`, `restore`, Settings.
