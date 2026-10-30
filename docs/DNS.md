# DNS-Filter mit AdGuard Home

Fahrplan Stufe 5. Ziel: Werbung im Startbildschirm und gesponserte Kacheln weg, Vorschau, Prime Video, Appstore, Updates und Alexa bleiben.

Vorgehen in drei Phasen:

1. AdGuard Home einrichten, nur der Stick nutzt es
2. Ein bis zwei Tage nur beobachten, nichts blockieren. `phoenixstick dns` zeigt, welche Adressen der Stick abfragt
3. Adressen einzeln sperren, nach jeder Sperre am TV testen

## 1. AdGuard Home auf Unraid

Stand 30.10.2026: AdGuard Home läuft bereits auf Unraid unter `http://192.168.178.2`, mit aktiven Sperrlisten für das Heimnetz. Der Stick nutzte es nicht direkt: `net.dns1` war die FRITZ!Box, `net.dns2` war `8.8.8.8` (von Fire OS selbst gesetzt, umgeht jeden Filter). Deshalb bekommt der Stick einen eigenen Client in AdGuard mit eigener Filtereinstellung (siehe unten), und am TV wird DNS 2 leer gelassen.

Für eine Neuinstallation:

- Unraid > **Apps** > nach **AdGuard Home** suchen > installieren (offizielles Image `adguard/adguardhome`)
- **Network Type: Custom: br0** und eine feste IP vergeben, z. B. `192.168.178.5`. Eine freie Adresse außerhalb des DHCP-Bereichs der FRITZ!Box nehmen. Mit eigener IP gibt es keinen Konflikt mit Port 80 und 53 von Unraid
- Nach dem Start im Browser `http://<AdGuard-IP>:3000` öffnen, Einrichtungsassistent:
  - Web-Oberfläche: Port 80
  - DNS-Server: Port 53
  - Benutzer und Passwort anlegen
- In der Oberfläche:
  - **Filter > DNS-Sperrlisten**: alle Listen **deaktivieren**. In Phase 2 wird nur beobachtet
  - **Einstellungen > Allgemeine Einstellungen > Abfrageprotokoll**: Aufbewahrung 7 Tage
  - **Einstellungen > Client-Einstellungen**: Client „Fire TV Stick“ mit der IP `192.168.178.26` anlegen. Haken bei „Globale Einstellungen verwenden“ entfernen und „Filterung aktivieren“ ausschalten. So wird nur beobachtet, andere Geräte bleiben gefiltert

## 2. Nur dem Stick den neuen DNS geben

Am Fernseher, nicht in der FRITZ!Box (die würde das ganze Heimnetz umstellen):

- **Einstellungen > Netzwerk** > dein WLAN auswählen > **Netzwerk vergessen**
- Neu verbinden, WLAN-Passwort eingeben, dann **Erweitert**:
  - IP-Adresse: `192.168.178.26`
  - Gateway: `192.168.178.1`
  - Netzwerkpräfixlänge: `24`
  - DNS 1: AdGuard-IP, z. B. `192.168.178.5`
  - DNS 2: leer lassen
- Danach im AdGuard **Abfrageprotokoll** prüfen: Tauchen Anfragen von `192.168.178.26` auf?

Stand 30.10.2026: Eingerichtet. `net.dns1` ist jetzt `192.168.178.2`. Fire OS trägt trotz leerem Feld wieder `8.8.8.8` als DNS 2 ein, dazu die IPv6-DNS der FRITZ!Box. In der ersten Stunde kamen trotzdem 135 Anfragen in AdGuard an. Ob der Stick beim Blocken auf `8.8.8.8` ausweicht, wird in Phase 3 geprüft. Nach dem WLAN-Wechsel war ADB „offline“, ein Neustart des Sticks hat geholfen.

Erste Erkenntnis: Die Vorschau-Trailer im Startbildschirm kommen von `*.trailer.row.aiv-cdn.net` (Prime-Video-CDN, Kategorie keep).

Achtung: Ist Unraid oder AdGuard aus, hat der Stick kein Internet. Zurück geht es, indem man das WLAN am TV vergisst und ohne „Erweitert“ neu verbindet.

Falls kaum Anfragen ankommen: Die FRITZ!Box verteilt über IPv6 eigene DNS-Server, und der Stick nutzt diese. Dann melden, wir schauen es uns an.

## 3. PhönixStick anbinden

In `config.json` ergänzen (die Datei ist gitignored, das Passwort bleibt lokal):

```json
"adGuard": {
  "url": "http://192.168.178.5",
  "user": "dein-benutzer",
  "password": "dein-passwort"
}
```

Das Passwort kann statt in der Datei auch in der Umgebungsvariablen `PHOENIXSTICK_ADGUARD_PASSWORD` stehen.

Dann:

```fish
dotnet run --project src/PhoenixStick -- dns
```

## Was nie blockiert wird

Steht in `catalog/dns.json` mit Kategorie `keep`: Startbildschirm-Inhalte, Bilder, Appstore, Prime Video, Push-Dienst, Updates, YouTube, Alexa. Anders als bei dumbtv bleiben hier die Update-Server erreichbar (Entscheidung Dennis, siehe `docs/GERAET.md`).
