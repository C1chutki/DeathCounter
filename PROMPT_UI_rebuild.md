# Prompt: graficzna przebudowa UI (Detection / Settings / ikony / okno / język)

**Goal:** Przebudować warstwę graficzną aplikacji (XAML) — wyczyścić zakładkę Detection, przenieść konfigurację do Settings, dodać graficzne (bez logiki) selektory postaci/save oraz języka aplikacji, podmienić zestaw ikon w `Assets/` i poszerzyć okno o 25px. **To są wyłącznie zmiany graficzne/layoutowe — nie buduj nowych funkcji, nie podpinaj logiki, nie pisz/uruchamiaj testów.**

**Context:** This is a .NET 9 WPF app. Follow `AGENTS.md`. Do not scan the whole repository. Start with the files listed below. If more files are needed, list them and explain why before reading many files. The navigation/tabs and all UI live almost entirely in one large XAML file.

**Start files:**

* `src/EldenDeathCounter/MainWindow.xaml`  — wszystkie zakładki: Dashboard (~l.620), Detection (~l.828), Bosses (~l.986), Settings (~l.1274); nawigacja (~l.540)
* `src/EldenDeathCounter/MainWindow.xaml.cs` — code-behind nawigacji (`NavigationRadio_Checked`)
* `src/EldenDeathCounter/ViewModels/MainWindowViewModel.cs` — bindingi (np. `GameLanguageOptions`, `SelectedGameLanguageValue`, `DetectionPhrasesText`, `ManualAddHotkeyText` itd.)
* `src/EldenDeathCounter/EldenDeathCounter.csproj` — `<Content Include="..\..\Assets\*.*" .../>` (kopiowanie assetów do output)
* `Assets/` — pliki ikon do podmiany
* `AGENTS.md` — zasady projektu

---

## Do:

### 1. Wyczyść zakładkę Detection (TabItem "Detection", ~l.828–984)
Z zakładki **Detection** usuń sekcje czysto konfiguracyjne:
* **Configuration** (nagłówek ~l.871) — usunąć cały blok.
* **ACTIVE DETECTION PHRASES** (~l.961–962, `TextBox` na `DetectionPhrasesText`) — usunąć z tej zakładki.
* **GLOBAL HOTKEYS** (~l.972–978) — usunąć z tej zakładki.

Po czyszczeniu zakładka **Detection ma pokazywać wyłącznie status, logi i diagnostykę** wykrywania: kółko/wskaźnik statusu (~l.842–854), przycisk Toggle Detection, oraz **Detection Log** (~l.917, `DetectionLogEntries`) i ewentualną diagnostykę. Ułóż pozostałe elementy tak, żeby layout był spójny i nie zostały puste ramki/marginesy po usuniętych sekcjach.

### 2. Konfiguracja Detection ma żyć w Settings (TabItem "Settings", ~l.1274+)
Rzeczy konfiguracyjne nie powinny być w Detection. W Settings już istnieją odpowiedniki (DETECTION PHRASES ~l.1368–1377, HOTKEYS ~l.1396–1418, OCR LANGUAGE ~l.1383–1390) — upewnij się, że **wszystkie ustawienia detekcji są dostępne w Settings**. Jeśli coś usuwanego z Detection nie ma odpowiednika w Settings, przenieś to do Settings (zachowując te same bindingi do ViewModelu — nie zmieniaj nazw właściwości). Nie duplikuj — docelowo konfiguracja istnieje tylko w Settings.

### 3. Dodaj graficzny wybór Character name i Save game (Settings, BEZ funkcjonalności)
W zakładce Settings dodaj nową sekcję (np. nad/obok OCR LANGUAGE) z:
* polem/`ComboBox` **Character name** (wybór postaci),
* polem/`ComboBox` **Save game / profil** (wybór zapisu gry).

To **tylko makieta graficzna** — bez bindingów do realnej logiki, bez nowych właściwości w ViewModelu (możesz użyć statycznych placeholderów/`ItemsSource` z paroma przykładowymi wpisami w XAML). Wizualnie zasugeruj, że dane będą rozdzielane per character / save (np. krótki opis/`MutedText`). Funkcjonalność dorobimy później.

### 4. Podmień zestaw ikon w `Assets/`
Podmień **wszystkie ikony aplikacji** w folderze `Assets/` na spójny zestaw:
* dotyczy plików ikon: `DashBoard.png`, `Detection.png`, `Detection_settings.png`, `Edit.png`, `Open_Folder.png`, `Quick_Reminders.png`, `Quick_Settings.png`, `Settings.png`, `Status.png`, `Logo.png`.
* **NIE dotyczy** zrzutów referencyjnych bossów ani list bossów: `ENG_*.jpg`, `PL_*.jpg`/`PL_*.png` (screeny Death/Win/Boss bar), `ENG_BossList.txt`, `PL_BossList.txt` — tych nie ruszaj.
* Styl: **ciemny, minimalistyczny, profesjonalny, outline (bez wypełnienia)**, spójna grubość linii, czytelne w małym rozmiarze.
* Zachowaj te same nazwy plików i wymiary, żeby istniejące referencje/`Content Include` nadal działały. Jeśli ikony są obecnie tylko glyphami w nawigacji (`▦ ◎ ⚔ ⚙` ~l.540–557), nie zmieniaj sposobu renderowania bez potrzeby — skup się na podmianie plików w `Assets/`.

### 5. Poszerz całe okno o 25px
W `MainWindow.xaml` (Window, ~l.5–7):
* `Width="1400"` → `Width="1425"`
* `MinWidth="1040"` → `MinWidth="1065"`
* `Height`/`MinHeight` bez zmian. Sprawdź, że layout (lewy panel `Width="255"` + `*`) poprawnie wykorzystuje dodatkową szerokość.

### 6. Dodaj graficzny wybór języka aplikacji (Settings, BEZ funkcjonalności)
W sekcji OCR LANGUAGE (~l.1383–1390) **poniżej** istniejącego `ComboBox` języka OCR dodaj nowy blok **APP LANGUAGE / Język aplikacji** z `ComboBox` (np. English / Polski). To **tylko element graficzny** — bez bindingu do realnej logiki i bez nowych właściwości w ViewModelu (statyczny `ItemsSource` w XAML jest OK). Styl/format dopasuj 1:1 do istniejącego selektora OCR.

---

## Do not:
* Nie refaktoruj niezwiązanego kodu.
* Nie ruszaj `bin/`, `obj/`, diagnostyki, plików generowanych (`*.g.cs`).
* Nie zmieniaj progów/parametrów detekcji ani logiki wykrywania.
* Nie zmieniaj publicznego zachowania poza zakresem tych zadań.
* Nie dodawaj realnej funkcjonalności do nowych selektorów (Character/Save/App language) — mają być wyłącznie graficzne.
* Nie zmieniaj nazw istniejących właściwości ViewModelu używanych w bindingach.

## Acceptance criteria:
* Zakładka Detection nie zawiera już sekcji Configuration, Active Detection Phrases ani Global Hotkeys — pokazuje status, przycisk toggle, logi i diagnostykę, bez pustych ramek po usunięciu.
* Wszystkie ustawienia detekcji są dostępne w Settings (bez duplikatów).
* W Settings są graficzne selektory Character name i Save game (placeholdery, bez logiki).
* W Settings, pod selektorem języka OCR, jest graficzny selektor języka aplikacji (placeholder, bez logiki).
* Ikony w `Assets/` (lista z pkt. 4) podmienione na spójny ciemny/outline zestaw; te same nazwy i wymiary; referencje działają; screeny bossów i listy bossów nietknięte.
* Okno: `Width="1425"`, `MinWidth="1065"`; layout poprawnie korzysta z dodatkowych 25px.
* Aplikacja kompiluje się i uruchamia, XAML jest poprawny (brak błędów bindingu w runtime dla zmienionych miejsc).

## Verification:
* To są zmiany graficzne — **nie pisz nowych testów**.
* Na koniec, jeśli potrzeba, zweryfikuj kompilację: `dotnet build EldenDeathCounter.sln` (zgodnie z AGENTS.md dla zmian projektu/UI). `dotnet test EldenDeathCounter.sln` uruchom tylko jeśli ruszałeś kod poza XAML.
