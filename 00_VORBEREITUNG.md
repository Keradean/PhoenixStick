# PhönixStick: Vorbereitung

Das hier machst du einmal selbst, bevor Claude Code loslegt. Danach ist der Stick per ADB im WLAN erreichbar, dein Rechner hat alle Werkzeuge und der Ausgangszustand ist gesichert. Dauer: etwa 20 bis 30 Minuten.

Alles, was du unterwegs herausfindest, trägst du in `docs/GERAET.md` ein. Claude Code liest die Datei bei jedem Start mit.

## 1. Prüfen, ob der Stick überhaupt geht

- [ ] Am Fernseher: **Einstellungen > Mein Fire TV > Info**
- [ ] Gerätename und Softwareversion notieren
- [ ] Steht bei der Version **Fire OS 6, 7 oder 8**: passt alles
- [ ] Fängt die Version mit **1.** an: das ist Vega OS. Da gibt es kein ADB, mit diesem Gerät ist das Projekt erledigt

## 2. ADB auf dem Stick einschalten

- [ ] **Einstellungen > Mein Fire TV > Info**, dann 7x OK auf den obersten Eintrag (Gerätename) drücken. Bei manchen Versionen ist es der Eintrag **Build**
- [ ] Es kommt eine Meldung, dass die Entwickleroptionen jetzt da sind
- [ ] Zurück zu **Mein Fire TV > Entwickleroptionen**
- [ ] **ADB-Debugging: AN**
- [ ] Falls es einen Punkt **Netzwerk-Debugging** gibt: auch AN
- [ ] Apps unbekannter Herkunft brauchst du jetzt noch nicht, das wird erst beim eigenen Launcher interessant

## 3. Feste IP für den Stick

- [ ] IP ablesen: **Einstellungen > Mein Fire TV > Info > Netzwerk**
- [ ] Im Router für den Stick eine feste IP vergeben (DHCP-Reservierung). Sonst bekommt er irgendwann eine neue Adresse und das Tool findet ihn nicht mehr
- [ ] Die IP in `docs/GERAET.md` eintragen

## 4. Werkzeuge auf CachyOS

```fish
sudo pacman -S --needed android-tools dotnet-sdk
adb version
dotnet --list-sdks
```

- [ ] `adb version` zeigt eine Version
- [ ] `dotnet --list-sdks` zeigt mindestens ein SDK ab Version 8. Du hast schon einen `~/.dotnet`-Ordner, vielleicht ist das SDK also schon da. Dann reicht `android-tools`
- [ ] Claude Code läuft (`claude --version`)
- [ ] Passt zum Thema: .NET sendet standardmäßig Nutzungsdaten an Microsoft. Abschalten mit `set -Ux DOTNET_CLI_TELEMETRY_OPTOUT 1`. In Claude-Code-Sessions in diesem Projekt ist das über `.claude/settings.json` schon gesetzt

## 5. Erste Verbindung

`DEINE_IP` durch die IP aus Schritt 3 ersetzen:

```fish
adb connect DEINE_IP:5555
adb devices
```

- [ ] Auf dem Fernseher kommt eine Abfrage, ob dieser Rechner debuggen darf. Haken bei "immer von diesem Computer zulassen" setzen, dann OK
- [ ] `adb devices` zeigt den Stick mit Status `device`

Wenn es hakt:

| Was du siehst | Woran es meistens liegt |
| --- | --- |
| `unauthorized` | Abfrage am TV übersehen oder abgelehnt. `adb disconnect`, nochmal verbinden, am TV bestätigen |
| `Connection refused` | ADB-Debugging ist aus oder die IP stimmt nicht |
| Timeout | Stick und Rechner hängen in verschiedenen Netzen (Gast-WLAN, VLAN) |
| `offline` | Stick einmal neu starten, dann neu verbinden |

Zum Schluss ein Lesetest:

```fish
adb shell getprop ro.product.model
adb shell getprop ro.build.version.release
```

Die beiden Werte (Modellcode und Android-Version) auch in `docs/GERAET.md` eintragen.

## 6. Ausgangszustand sichern

Bevor irgendwas geändert wird, sicherst du den Werkszustand. Das sind nur Lesebefehle, am Stick ändert sich nichts.

```fish
cd /mnt/storage/Claude/PhoenixStick
mkdir -p backup/ausgangszustand
cd backup/ausgangszustand
adb shell getprop > getprop.txt
adb shell pm list packages -f > pakete_alle.txt
adb shell pm list packages -s > pakete_system.txt
adb shell pm list packages -3 > pakete_drittanbieter.txt
adb shell pm list packages -d > pakete_deaktiviert.txt
adb shell settings list global > settings_global.txt
adb shell settings list secure > settings_secure.txt
adb shell settings list system > settings_system.txt
wc -l pakete_alle.txt
```

- [ ] Alle Dateien sind da und nicht leer (`pakete_deaktiviert.txt` darf leer sein)
- [ ] Anzahl Pakete aus `wc -l` notieren

Der Ordner `backup/` steht in der `.gitignore`, weil da Seriennummer und Gerätedaten drin sind.

## 7. Entscheidungen treffen

In `docs/GERAET.md` gibt es einen Abschnitt **Was bleiben muss**. Den füllst du aus, bevor Goal 02 startet. Wichtig sind vor allem:

- Nutzt ihr Alexa oder die Sprachsuche auf der Fernbedienung?
- Nutzt ihr Prime Video?
- Welche Apps müssen auf jeden Fall laufen (z. B. Netflix, YouTube, Jellyfin, Plex)?
- Sollen Systemupdates von Amazon weiter kommen?

Davon hängt ab, welche Amazon-Pakete überhaupt in Frage kommen.

## 8. Claude Code starten

Die Claude-Code-Einstellungen liegen noch als `claude-settings.json` im Projektordner, weil ich in `.claude/` nicht direkt schreiben darf. Einmal an den richtigen Platz schieben:

```fish
cd /mnt/storage/Claude/PhoenixStick
mkdir -p .claude
mv claude-settings.json .claude/settings.json
git init
claude
```

- [ ] In Claude Code `/permissions` aufrufen und prüfen, dass die Regeln aus `.claude/settings.json` geladen sind
- [ ] Goal-Datei `goals/01_grundgeruest-und-geraet.md` per `/goal` übergeben

## Wenn ihr fertig seid

- [ ] ADB-Debugging wieder ausschalten. Während der Entwicklung muss es an bleiben, danach nicht. Ohne Bestätigung am TV kommt zwar niemand rein, aber jeder im WLAN kann die Abfrage auslösen, und die ist schnell mal aus Versehen weggeklickt
