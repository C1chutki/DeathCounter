# Vertical Monitor Scaling — Design Spec
*Date: 2026-06-04*

## Problem

The main window is designed for landscape monitors (`MinWidth="1180"`, default `Width="1582"`).
On a rotated FHD vertical monitor (1080×1920) the window cannot be maximised — `MinWidth` exceeds
the available 1080 px. Even at 1080 px the Bosses tab toolbar overflows horizontally, and the
Dashboard wastes the extra vertical space with fixed-size content centred in a sea of black.

## Goals

- Window opens and maximises correctly on a 1080 px-wide vertical monitor.
- No horizontal overflow in any tab at window widths ≥ 900 px.
- Dashboard content fills tall windows proportionally rather than floating as a fixed island.

## Out of Scope

- Overlay window (has its own scale setting already).
- Responsive layout switching (e.g. sidebar collapsing below some width).
- Scaling/zooming the entire UI (Approach C).

---

## Section 1 — Window Constraints (`MainWindow.xaml`, lines 5–8)

| Property | Before | After |
|---|---|---|
| `Width` | 1582 | 1280 |
| `MinWidth` | 1180 | 900 |
| `Height` | 840 | 840 (unchanged) |
| `MinHeight` | 720 | 720 (unchanged) |

Rationale: 900 px allows the sidebar (78 px) plus 822 px of main content, which comfortably fits
all tab layouts. 1280 px as default startup size fits entirely within a 1080 px-wide screen when
the window is not maximised (Windows will cap it at screen width).

---

## Section 2 — Bosses Tab Toolbar Restructure (`MainWindow.xaml`)

### Current structure (single horizontal row, ~950 px minimum)
```
[CONQUERED FOES label] [Sort By label] [ComboBox 170] [Direction label] [ComboBox 170] [Search 260] [Add Record 140]
```

### New structure (two rows)

**Row 1** — title + primary action:
```
[CONQUERED FOES label (left)]                           [ADD RECORD button (right)]
```

**Row 2** — filter controls:
```
[Sort By label] [ComboBox 140] [gap] [Direction label] [ComboBox 140] [gap] [Search TextBox (*)]
```

Changes:
- `ComboBox` widths reduced from `Width="170"` → `Width="140"`.
- Search `Border+TextBox` loses its fixed `Width="260"` — becomes `*` in a `DockPanel` (fills remaining space).
- `ADD RECORD` button moved to row 1, right-aligned.
- Row 2 minimum width ≈ 550 px — fits safely within the 822 px content area at `MinWidth=900`.

---

## Section 3 — Dashboard Vertical Distribution (`MainWindow.xaml`)

### Current behaviour
Inner content grid uses fixed `Margin` values between elements and `VerticalAlignment="Center"`.
At 1920 px window height the ~460 px of content floats in the centre with ~680 px of empty space
above and below.

### New behaviour — proportional spacer rows

Replace the flat `Auto`-only row definitions inside the inner content grid with proportional
spacer rows (`Height="N*"`) interspersed between content rows. The spacers define the *relative*
gaps between elements; `MinHeight` prevents collapse at short window heights.

```
RowDefinition Height="Auto"          → "YOU HAVE DIED" label
RowDefinition Height="2*" Min=4      → top spacer
RowDefinition Height="Auto"          → death counter number (FontSize 172)
RowDefinition Height="1*" Min=2      → spacer
RowDefinition Height="Auto"          → "TIMES" label
RowDefinition Height="3*" Min=20     → larger gap before controls
RowDefinition Height="Auto"          → SET COUNTER input row
RowDefinition Height="2*" Min=12     → spacer
RowDefinition Height="Auto"          → action buttons row (−, SET, +, RESET)
```

At `MinHeight=720` (−60 header −110 encounter bar = 550 px available) all spacers collapse to their
`MinHeight`, giving the same compact look as today. At 1920 px height (1750 px available) the
spacers grow proportionally and content fills the screen.

Fixed `Margin` values on the affected elements (`"0,-12,0,28"`, `"0,0,0,28"`) are removed or set
to `0` once the spacer rows provide the gaps.

---

## Affected Files

| File | Change |
|---|---|
| `src/EldenDeathCounter/MainWindow.xaml` | All three sections above |

No C# changes required.

---

## Testing Checklist

- [ ] App opens at default size 1280×840.
- [ ] Window can be resized down to 900 px wide without horizontal overflow in any tab.
- [ ] Window maximises correctly on a simulated 1080 px-wide screen (resize to 1080 px).
- [ ] Bosses tab toolbar: both rows visible, Sort/Direction/Search in row 2, Add Record in row 1.
- [ ] Dashboard at small height (720 px): compact look, no clipping.
- [ ] Dashboard at large height (1200+ px): content spreads proportionally.
- [ ] Detection tab: no regressions.
- [ ] Settings tab: no regressions.
