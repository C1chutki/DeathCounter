# Prompt: skalowanie całego overlay + opacity tła

**Goal:** Dodaj do overlay dwa ustawienia modyfikowalne w locie z menu: (1) skalowanie CAŁEGO okna overlay (nie tylko rozmiaru tekstu "Śmierci"), (2) opacity tła overlay. Oba mają działać natychmiast po zmianie w Quick Settings i być zapisywane w `AppSettings`.

**Context:** This is a .NET 9 WPF app (`net9.0-windows10.0.19041.0`, win-x64). Follow AGENTS.md. Do not scan the whole repository. Start with the files listed below. If more files are needed, list them and explain why before reading many files.

Stan obecny, który trzeba poprawić:
- `AppSettings.OverlayFontScale` (double, domyślnie 1.0, zakres 0.6–1.6) jest jedynym istniejącym ustawieniem rozmiaru.
- `OverlayWindow.ApplyFontScale(double)` skaluje rozmiary RĘCZNIE, element po elemencie (`CounterTextBlock.FontSize`, `BossTextBlock.FontSize/LineHeight/MaxWidth`, `BossDeathTextBlock`, `TimerTextBlock`, nagłówki, `OverlayChrome.MinWidth`, `OverlayChrome.Padding`). To podejście jest kruche i nie skaluje pozostałej geometrii (marginesy `StackPanel`/`Grid`, `DividerBorder`, ramki, `TimerChrome`, kropka detekcji), więc okno nie skaluje się proporcjonalnie jako całość.
- Tło rysuje `OverlayChrome` (`Border`), kolor z motywu przez `ApplyTheme` → `BuildOverlayGradient(theme.OverlayBackground)`. Okno (`Window`) ma `Background="Transparent"`, `AllowsTransparency="True"`, `SizeToContent="WidthAndHeight"`.

Start files:
* `src/EldenDeathCounter/OverlayWindow.xaml`
* `src/EldenDeathCounter/OverlayWindow.xaml.cs`
* `src/EldenDeathCounter.Core/Configuration/AppSettings.cs`
* `src/EldenDeathCounter/ViewModels/MainWindowViewModel.cs`
* `src/EldenDeathCounter/MainWindow.xaml` (sekcja "⚙ Quick Settings", ~linie 791–800, gdzie jest `TextBox Text="{Binding OverlayFontScaleInput}"`)
* `tests/EldenDeathCounter.Tests/Core/AppSettingsTests.cs`
* `tests/EldenDeathCounter.Tests/Core/SettingsMenuCoverageTests.cs`

## Do:

### 1. Skalowanie całego okna (zamiast per-element)
* Zamień ręczne skalowanie czcionek na pojedynczy `ScaleTransform` przypięty jako `LayoutTransform` na root borderze `OverlayChrome` w `OverlayWindow.xaml`. Dzięki `SizeToContent="WidthAndHeight"` całe okno (tekst, odstępy, ramki, divider, timer) przeskaluje się proporcjonalnie.
* Zdecyduj o nazewnictwie ustawienia i UDOKUMENTUJ wybór:
  - **Preferowane:** zachowaj nazwę pola `OverlayFontScale` w `AppSettings` (żeby nie zepsuć istniejących zapisanych plików ustawień JSON) i nadaj mu nowe znaczenie „skala całego overlay”. ALBO
  - Dodaj nowe pole `OverlayScale` i zmigruj `OverlayFontScale` → nowe pole przy wczytaniu (zachowując kompatybilność wsteczną).
* Uprość `ApplyFontScale` (ewentualnie zmień nazwę na `ApplyScale`) tak, by ustawiał tylko `ScaleX`/`ScaleY` transformacji. Zachowaj clamp do zakresu 0.6–1.6 (stałe `OverlayFontScaleMin`/`OverlayFontScaleMax` w `MainWindowViewModel`) i obsługę wartości ≤ 0 → 1.0.
* Zachowaj wywołanie skalowania w konstruktorze `OverlayWindow(AppSettings)` oraz ścieżkę live-update z VM.

### 2. Opacity tła overlay
* Dodaj `AppSettings.OverlayBackgroundOpacity` (double, domyślnie odpowiadające obecnemu wyglądowi — patrz uwaga niżej, zakres 0.0–1.0) wraz z odpowiednikiem w `CreateDefault`.
* Zastosuj opacity TYLKO do tła, nie do tekstu (tekst ma pozostać czytelny). Zrekomendowane podejście: zmodyfikuj kanał alfa pędzla tła `OverlayChrome.Background`, albo dodaj dedykowaną warstwę tła (`Border` pod treścią) z kontrolowanym `Opacity`. NIE ustawiaj `Opacity` na całym `OverlayChrome` (bo wyblaknie też tekst).
* Dodaj `OverlayWindow.ApplyBackgroundOpacity(double)` (clamp 0.0–1.0) i wywołaj je w konstruktorze; upewnij się, że współgra z `ApplyTheme` (zmiana motywu nie może resetować opacity).
* Uwaga o wartości domyślnej: obecne tło to `#E60A1014` (alfa 0xE6 ≈ 0.90) plus `DropShadowEffect`. Dobierz domyślne `OverlayBackgroundOpacity` tak, by wygląd po zmianie był wizualnie spójny z obecnym (czyli ~0.9, nie 1.0), chyba że zdecydujesz inaczej — wtedy to uzasadnij.

### 3. UI (Quick Settings) i ViewModel
* W `MainWindowViewModel` dodaj property w stylu istniejącego `OverlayFontScaleInput` (parsowanie `InvariantCulture`, clamp, zapis przez `_settingsStore.SaveAsync`, wywołanie metody overlay, `OnPropertyChanged`): jedno dla skali (lub zaktualizuj istniejące), jedno dla opacity tła (np. `OverlayBackgroundOpacityInput`, zakres 0.0–1.0).
* W `MainWindow.xaml` w sekcji "⚙ Quick Settings" dodaj kontrolkę dla opacity obok istniejącego pola skali; etykiety opisz po polsku spójnie z resztą menu (np. „Skala overlay”, „Przezroczystość tła”).
* Jeśli w `MainWindowViewModel.RefreshSettingsInputs` (lub analogicznym miejscu — ok. linii 1012 jest `OnPropertyChanged(nameof(OverlayFontScaleInput))`) odświeżane są inputy, dodaj tam nowe property.

## Do not:
* Do not refactor unrelated code.
* Do not touch `bin/`, `obj/`, diagnostics, generated files.
* Do not change detection thresholds or any detection logic.
* Do not change public behavior outside this issue (licznik, detekcja, hotkeye, historia bossów, persistencja innych pól).
* Do not break backward compatibility wczytywania istniejących plików `AppSettings` (JSON).
* Do not add game memory reads / injection / hooks (zgodnie z AGENTS.md detekcja zostaje screen-based).

## Acceptance criteria:
* Zmiana skali w Quick Settings natychmiast i proporcjonalnie skaluje CAŁE okno overlay (tekst, odstępy, ramki, divider, sekcja bossa, timer), nie tylko licznik „Śmierci”.
* Zmiana opacity tła w Quick Settings natychmiast zmienia przezroczystość tła overlay, a tekst pozostaje w pełni nieprzezroczysty/czytelny.
* Oba ustawienia są zapisywane do `AppSettings` i poprawnie wczytywane po restarcie aplikacji; clamp do zakresów działa.
* Istniejące zapisane pliki ustawień (bez nowego pola opacity / ze starym `OverlayFontScale`) wczytują się bez błędu i dają sensowne wartości domyślne.
* Zmiana motywu (`ApplyTheme`) nie resetuje ani skali, ani opacity.
* Add/update focused tests only (zakresy i wartości domyślne w `AppSettingsTests`; pokrycie nowych property w `SettingsMenuCoverageTests` jeśli pasuje do istniejącego wzorca).

## Verification:
* Run the smallest relevant test first (np. `dotnet test EldenDeathCounter.sln --filter FullyQualifiedName~AppSettingsTests`).
* Run `dotnet test EldenDeathCounter.sln` only at the end if needed.
