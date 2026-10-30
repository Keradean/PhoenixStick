# Aufbau einer Theme-Datei

Jede Datei in `design/tokens/` hat genau diesen Aufbau. Ein neues Theme ist nur eine neue Datei, der Launcher-Code bleibt gleich. Ein Test prüft, dass jede Theme-Datei alle Schlüssel hat.

Maße und Anordnung (Seitenleiste 128, Bühne 480 hoch, Kacheln 340 × 190 usw.) stehen in `docs/DESIGN.md` und sind für alle Themes gleich. Sie gehören nicht in die Theme-Datei.

Farben als `#RRGGBB` oder `rgba(r,g,b,a)`. Größen in px bei 1920 × 1080.

## Pflichtblöcke

| Block | Schlüssel |
| --- | --- |
| oben | `name`, `id`, `default`, `description` |
| `font` | `display`, `body`, `mono`, je mit `family`, `weights`, `license`. Braucht ein Theme keine eigene Mono-Schrift, steht dort dieselbe wie bei `body` |
| `color` | `background`, `rail`, `railBorder`, `heroCard`, `surface`, `surfaceRaised`, `surfaceFocus`, `borderSubtle`, `borderChip`, `borderStrong`, `grid`, `textPrimary`, `textSecondary`, `textTile`, `textMuted`, `accent`, `accentSoft`, `focusGlow`, `focusRingButton`, `focusBrackets`, `buttonPrimaryBg`, `buttonPrimaryText` |
| `type` | `wordmark`, `tab`, `clock`, `date`, `eyebrow`, `heroTitle`, `heroSub`, `heroMeta`, `button`, `buttonSecondary`, `sectionTitle`, `viewAll`, `tileLabel`, `tileLabelFocused`, `tag`, `chip`, `hint` |
| `shape` | `railItemRadius`, `railActiveIndicator` (`width`, `height`), `heroCardRadius`, `tileRadius`, `buttonRadius`, `buttonChamfer`, `chipRadius`, `avatarRadius`, `iconButtonRadius` |
| `focus` | `tileBorderWidth`, `tileBorderColor`, `tileGlow`, `tileScale`, `brackets` (`size`, `width`, `offset`), `buttonRing` (`width`, `color`), `buttonGlow` |
| `style` | `tileShape` (`rounded`/`square`), `focusStyle` (`glowBorder`/`brackets`), `buttonShape` (`rounded`/`chamfer`), `labelCase` (`upper`/`none`), `hasBackgroundGrid` (true/false) |
| `motion` | `focusMs`, `heroCrossfadeMs`, `maxMs`, `easing` |
| `heroImage` | `file`: SVG für die Bühne, Größe 1632 × 480 |
| `backgroundImage` | `file`: SVG über den ganzen Bildschirm 1920 × 1080, oder `null` |

Jeder Eintrag in `type` hat: `font` (`display`/`body`/`mono`), `size`, `weight`, `letterSpacingEm`, `uppercase`. Optional: `color` (Name aus `color`), `lineHeight`, `prefix`, `suffix`, `suffixColor`, `tabular`.

Werte, die ein Theme durch seinen `style` nicht nutzt (zum Beispiel `brackets` bei `glowBorder`), stehen trotzdem drin, mit sinnvollen Werten.
