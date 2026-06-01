# Changelog

## 2026-05-31

- Zawezono pionowy pas przechwytywania death/boss-victory na podstawie pomiaru realnych zrzutow gry (a nie recznych szacunkow), co zmniejsza powierzchnie OCR i ogranicza falszywe trafienia z innych okien (np. notatnika). Frakcja `captureHeight` spadla `0.26 -> 0.15` (na 2560x1440 wysokosc pasa 374 -> 216 px, pas ~636..852 zamiast 547..921, ~89/69 px mniej u gory/dolu), `centerY` doprecyzowano `0.51 -> 0.517`, a floor wysokosci obnizono `260 -> 160` px. Zmierzone pasmo tekstu (`YOU DIED` / `NIE ZYJESZ` / `ENEMY FELLED` / `POKONANO WROGA`) to ~693..797 px (frac ~0.481..0.553). Crop referencyjny templatu (wczesniej `0.43..0.58` death i `0.43..0.62` victory) zawezono i scentralizowano w nowym `DeathTextTemplateReferenceRegion` (y `0.476..0.558`), tak by template pozostal mniejszy niz ROI analizatora i zachowal zapas na jitter pozycji tekstu. Oba detektory (`TemplateDeathTextImageSignalDetector`, `TemplateBossVictoryTextImageSignalDetector`) i testy korzystaja z tego samego cropu. Zaktualizowano stale/progi w testach regresyjnych; pelny zestaw 195/195. Wersje podniesiono do 1.8.2.

- Dodano globalny skrot `F6` do wlaczania/wylaczania detekcji oraz `Ctrl+Shift+P` do pomijania aktywnego bossa. Pomijanie anuluje aktualne podejscie: odejmuje smierci aktywnego bossa od globalnego licznika, usuwa aktywna nazwe/czas i nie tworzy wpisu historii; obok `BOSS DEFEATED` dodano przycisk `SKIP`. Wersje podniesiono do 1.8.1.

- Dodano reczne dodawanie pokonanych bossow w zakladce Bosses przez ten sam modal co edycja historii: `ADD RECORD` otwiera formularz z polami Name, Attempts, Duration, Recorded at i Completed by, a `DELETE` jest widoczny tylko przy edycji istniejacego wpisu. Nowe rekordy zapisuja `manual-entry`, trimuja dane i przeliczaja czas walki tak jak edycja; wersje podniesiono do 1.8.0.

- Przebudowano zakladke Settings do ukladu 2:3:1: Overlay + Detection, Character + Language + Hotkeys oraz Profile / Save game. Usunieto edycje Detection Phrases z Settings; frazy detekcji i zwyciestwa bossa sa teraz hardcoded w kodzie i nie zapisuja sie do appsettings JSON. Wersje podniesiono do 1.7.1.

- Overlay skaluje sie teraz jako cala calosc przez `ScaleTransform` (`LayoutTransform` na `OverlayChrome`) zamiast recznego skalowania per-element, wiec tekst, odstepy, ramki, divider, sekcja bossa i timer skaluja sie proporcjonalnie; `OverlayFontScale` zachowuje nazwe pola (kompatybilnosc JSON), a `ApplyFontScale` zmieniono na `ApplyScale`. Dodano `AppSettings.OverlayBackgroundOpacity` (domyslnie 0.9, zakres 0.0–1.0) sterujace alfa tla overlaya bez wyblakniecia tekstu, z kontrolka „Przezroczystosc tla" w Quick Settings i zywa aktualizacja niezalezna od zmiany motywu. Dodano testy domyslnych/persistencji i podniesiono wersje do 1.7.0.
- Graficzna przebudowa UI (tylko XAML/layout, bez zmian logiki). Zakladka Detection oczyszczona z sekcji konfiguracyjnych (Configuration, Active Detection Phrases, Global Hotkeys) — pokazuje teraz wylacznie status, przycisk Toggle Detection oraz Detection Log/diagnostyke na pelnej szerokosci. Wszystkie ustawienia detekcji pozostaja dostepne w Settings (bez duplikatow).
- W Settings dodano graficzne (placeholder, bez logiki) selektory Character name i Save game oraz selektor APP LANGUAGE pod istniejacym wyborem jezyka OCR. Okno poszerzono o 25px (`Width` 1400→1425, `MinWidth` 1040→1065).
- Odswiezono zestaw ikon `Assets/` przez `tools/generate-icons.ps1` (spojny ciemny outline, te same nazwy i wymiary); dodano generowanie `Logo.png` (132x121). Screeny i listy bossow nietkniete. Wersje podniesiono do 1.6.0.

## 2026-05-30

- Podmieniono zestaw ikon w `Assets/` (Edit, Settings, Status, Detection, Detection_settings, Quick_Settings, Quick_Reminders, DashBoard, Open_Folder) z wypelnionych sylwetek na spojny zestaw outline (charcoal, jednolita grubosc linii ~18px @256px, zaokraglone konce, przezroczyste tlo, renderowane 256x256 z czytelnoscia w 16/24 px). `Logo.png` (sylwetka czaszki) i `PL_Death_Screen.png` (szablon detekcji) celowo nietkniete. Ikony nie sa referowane w XAML/kodzie, wiec build i zasoby pozostaja sprawne; dodano generator `tools/generate-icons.ps1`. Wersje podniesiono do 1.5.1.

- Zmniejszono domyslny rozmiar czcionki overlaya (licznik 32→26, nazwa bossa 25→20, licznik bossa 16→14, timer 25→20) tak, by overlay byl mniej nachalny, ale wyraznie czytelny. Dodano pole `OverlayFontScale` w `AppSettings` (domyslnie 1.0, zakres 0.6–1.6, normalizowane/clampowane przez `AppSettingsStore`) oraz kontrolke "Overlay font size" w sekcji Quick Settings; zmiana jest stosowana na overlay na zywo (`OverlayWindow.ApplyFontScale`), zapisywana od razu i przezywa restart. Dodano testy zakresu/persistencji; wersje podniesiono do 1.5.0.

- Naprawiono rozmieszczenie przycisku `OPEN DATA FOLDER` w pasku bocznym `MainWindow.xaml`. Stopka (Toggle Overlay + Open Data Folder) byla przyklejona do samego dolu okna przez wiersz `*` nad nia, co dawalo duza pusta przerwe i efekt "oderwania". Wiersz nawigacji zmieniono na `Auto`, a wiersz `*` przeniesiono pod stopke jako elastyczny odstep, wiec kontrolki sa zgrupowane pod nawigacja i poprawnie widoczne przy roznych rozmiarach okna. Wersje podniesiono do 1.4.1.

- Przebudowano zakladke Settings na czytelnie pogrupowane sekcje (Overlay, Detection, OCR Language, Hotkeys, Profile / Save game) w istniejacym stylu paneli, bez zmiany logiki ani domyslnych progow. Wszystkie pola pozostaja podpiete do `AppSettings` i mechanizmu zapisu (`SaveSettingsCommand`), wiec przezywaja restart; rozmiar czcionki overlaya pominieto, bo overlay nie ma takiego pola w `AppSettings` (wartosci `FontSize` sa zaszyte w `OverlayWindow.xaml`). Wersje podniesiono do 1.4.0.

- Dodano konfigurowalny globalny skrot przelaczajacy widocznosc overlaya (domyslnie `Ctrl+Shift+O`), rejestrowany przez istniejacy `GlobalHotkeyService` (WinAPI `RegisterHotKey`), wiec dziala niezaleznie od fokusu okna. Skrot `OverlayToggleHotkey` jest edytowalny w zakladce Settings (sekcja HOTKEYS), zapisywany przez `AppSettingsStore` i ponownie rejestrowany po zapisie bez restartu; dodano testy serializacji/normalizacji oraz pokrycia menu i podniesiono wersje do 1.3.0.

## 2026-05-29

- Zmieniono nazwe aplikacji w panelu bocznym z `Tarnished Utility` na `Death Counter` i zsynchronizowano etykiete wersji w pasku bocznym z wersja pokazywana na overlay. Dodano do `AGENTS.md` regule wersjonowania (kazda zmiana podnosi wersje). Wersje podniesiono do 1.2.2.

- Zawezono pionowy pas przechwytywania uzywany do wykrywania napisow smierci i pokonania bossa: frakcja `captureHeight` w `DeathTextCaptureRegionCalculator` spadla z 0.32 do 0.26 (centerY 0.51, szerokosc 0.66 i progi malego ekranu bez zmian). Na 2560x1440 pas to teraz 547..921 px (~19% mniej wierszy), nadal z marginesem ~72/86 px wokol znanego pasma tekstu 619..835; dodano test blokujacy ciasniejszy pas i podniesiono wersje do 1.2.1.

- Przebudowano wykrywanie nazwy bossa, aby usunac falszywe trafienia i zepsute napisy. OCR nazwy uruchamia sie teraz tylko w obszarze nad wykrytym paskiem HP bossa, a kandydat zostaje przyjety dopiero po dopasowaniu (fuzzy) do listy bossow z `Assets/ENG_BossList.txt` lub `Assets/PL_BossList.txt`, wiec teksty typu `Talk`, `Sit`, `Rest`, `Read message`, `Bloody Slash` oraz smieci OCR sa odrzucane. Nazwa jest publikowana i zamrazana raz na walke (maszyna stanow enkountera), obsluguje 1-3 paski bossow laczone przez ` + ` i nie nadpisuje recznie ustawionej nazwy; dodano liste PL, testy regresyjne i podniesiono wersje do 1.2.0.

- Odswiezono wyglad overlaya dla motywu Elden Ring: widoczna zlota ramka 1px (`OverlayBorder` = `#EAC36D`) oraz tlo bardziej przezroczyste (`OverlayBackground` = `#7F000000`, alpha ~0.50). Tlo overlaya rysuje teraz subtelny pionowy gradient generowany z koloru motywu w `ApplyTheme`, wiec pozostale motywy (np. Dark Souls 3) dzialaja bez zmian; wersje podniesiono do 1.1.2.

- Naprawiono zamykanie aplikacji: graceful shutdown (zatrzymanie detekcji, pauza timera, zapis stanu, zamkniecie overlaya) biegnie teraz w `MainWindow.OnClosing` przy zywym dispatcherze, co usuwa zakleszczenie sync-over-async w `App.OnExit`, ktore zostawialo proces w tle, niezwolniony mutex single-instance i osierocone pliki `deaths.json.<guid>.tmp`. `DeathCounterStore` czysci przy starcie stare pliki tymczasowe pasujace do wzorca aplikacji (`deaths.json.*.tmp`, `deaths.json.progression`) bez ruszania `deaths.json` ani backupow `*.corrupt-*.json`; dodano testy i podniesiono wersje do 1.1.1.

- Dodano sortowanie historii bossow w zakladce Bosses: pola `Sort by` (Default/Time/Deaths) oraz `Direction` (Descending/Ascending). Domyslny tryb zachowuje dotychczasowa kolejnosc od najnowszych, sortowanie dziala razem z wyszukiwarka, a rekordy bez czasu walki trafiaja na koniec listy. Logika trafila do `BossHistoryDisplayOrder` z testami regresyjnymi; wersja aplikacji podniesiona do 1.1.0.

## 2026-05-26

- Dodano w prawym gornym rogu glownego okna statyczne przyciski skrotow gier: `DS`, `DS2`, `DS3` i `ER`. Na razie nie podpinaja logiki przelaczania gier.

- Przyspieszono petle detekcji do domyslnych 300 ms, ograniczono detekcje nazw bossow do dolnego pasa ekranu i uruchamiania co 2 sekundy. Zawezono obszar szukania death textu oraz dodano ladowanie `ENG_Death_Screen_v2.jpg` jako angielskiego template'u wraz z testami regresyjnymi.

- Dodano wybor jezyka gry `PL`/`ENG` w ustawieniach i podpieto go pod template'y detekcji smierci oraz pokonania bossa z folderu `Assets`. Projekt kopiuje teraz assety z nowej lokalizacji, a stabilizacja nazwy bossa toleruje pojedynczy brak OCR podczas walki.

- Poprawiono wykrywanie paska bossa, gdy czerwony fragment HP jest krotszy niz minimalna szerokosc pelnego boss bara. Dodano test regresyjny na screenach `ENG_Boss_bar.jpg` i `ENG_Boss_bar_v2.jpg`, a pelny zestaw testow przeszedl 84/84.

- Dodano lokalizowanie tekstu licznika smierci w aplikacji i overlayu na podstawie `GameLanguage`: `PL` pokazuje `Smierci`, a `ENG`/`en` pokazuje `Deaths`. Overlay odswieza etykiete po zapisaniu ustawien jezyka; dodano testy formattera i zaktualizowano test zasobow overlaya.

- Przyspieszono wykrywanie napisow `YOU DIED` i `GREAT ENEMY FELLED`, przenoszac dopasowanie szablonow na OpenCV `matchTemplate` z maska kolorow i krawedziami. Dodano testy regresyjne dla dopasowania ekranu smierci i ochrony przed falszywymi trafieniami.

- Poprawiono wykrywanie zwyciestwa nad bossem, aby fraza `GREAT ENEMY FELLED` nie byla odrzucana przez filtr tekstu wlasnego okna aplikacji. Filtr nadal chroni wykrywanie `YOU DIED`, a sciezka boss victory ma osobny test regresyjny.

- Rozdzielono diagnostyke na lekkie `log.txt`, strukturalne `detection-events.jsonl`, snapshot `diagnostics-latest.json` oraz pakiety dowodowe w folderze `diagnostics`. Dodano rotacje logow i przycisk `DIAG 10M` do czasowego wlaczania pelnej diagnostyki klatek.

- Zmniejszono opoznienie automatycznego odczytu nazwy bossa: sprawdzanie paska HP odbywa sie co 500 ms, a domyslny stabilizator publikuje nazwe po jednym stabilnym odczycie. Zaktualizowano test regresyjny dla tego zachowania.

- Naprawiono pauzowanie timera aktywnego bossa przy zatrzymaniu detekcji i zamknieciu aplikacji oraz wznawianie go po starcie detekcji. Overlay korzysta teraz z zapisanego aktywnego czasu, a historia bossa zapisuje czas walki z uwzglednieniem pauz; dodano testy regresyjne dla pauzy, wznowienia i czasu zabicia.

- Dodano przelacznik trybu `DS3`, ktory zmienia tytul aplikacji na `Dark Souls 3 Death Counter`, kolorystyke glownego okna i overlay na palete DS3. Naprawiono blad startu WPF zwiazany z modyfikacja zamrozonych pedzli, podmieniajac zasoby motywu przez `DynamicResource`; dodano testy regresyjne dla palety i sposobu aktualizacji zasobow.

- Podsumowano naprawe zamykania aplikacji: `DeathDetectionService.StopAsync()` czeka na zakonczenie petli detekcji, a `OnExit` korzysta z tej sciezki. Dodano test regresyjny dla zatrzymania detekcji przy wychodzeniu z aplikacji.

- Ujednolicono czcionke interfejsu na `EB Garamond`, ustawiajac na nia zasoby `AppFontFamily` i `HeaderFontFamily`. Dotyczy to glownego okna oraz overlaya.

- Przebudowano overlay licznika na widok inspirowany dostarczonym screenshotem: dodano status detekcji z wersja aplikacji, sekcje `TOTAL DEATHS`, aktywnego bossa, licznik smierci bossa i timer. Dodano formatowanie nazw dwoch bossow rozdzielonych `+` oraz testy formattera tekstu licznika.

- Naprawiono ucinanie panelu `Configuration` w widoku Detection, dodajac przewijanie jego zawartosci. Dodano test regresyjny dla przewijania konfiguracji przy niskim oknie.

- Wycentrowano liste `CONQUERED FOES` w zakladce Bosses, aby po rozszerzeniu okna wolna przestrzen nie zostawala glownie po prawej stronie.

- Poprawiono numerację pokonanych bossów w zakładce Bosses: pierwszy zabity boss ma teraz `#1`, a najnowszy najwyższy numer. Dodano test regresyjny dla numerowania historii bossów.

- Dodano przycisk `EDIT` obok etykiety `FELLED` na kartach pokonanych bossow. Formularz edycji pozwala zmienic nazwe, liczbe prob, czas walki, date zapisu oraz metode ukonczenia rekordu.
- Dodano zatwierdzanie edycji rekordu klawiszem Enter oraz przycisk `DELETE` usuwajacy caly rekord historii bossa. Zmiany sa zapisywane przez serwis historii i objete testami regresyjnymi.

- Dodano `captureTarget` jako zapisywalne ustawienie wyboru monitora dla detekcji, zamiast statycznego tekstu `EldenRing.exe (Main Window)`. Dropdown `Capture Region` dostal ciemny styl z czarnym tlem i jasnym tekstem dla pola oraz listy.
- Dodano numerację kart pokonanych bossów w historii (`#1`, `#2` itd.) zamiast stałego napisu `FELLED`. Uzupełniono brakujący `Death_Screen.png`, dzięki czemu build aplikacji ponownie aktualizuje pliki w katalogu `bin`.

- Poprawiono tekst i uklad kart w zakladce Bosses: pod nazwa pokonanego bossa pokazuje sie liczba podejsc obok czasu walki, a nizej osobna linia `Recorded`. Dodano test regresyjny sprawdzajacy rozdzielenie tych danych.

- Naprawiono polski tekst licznika śmierci w overlay, aby po odświeżeniu licznika nie pojawiały się uszkodzone znaki. Dodano test regresyjny sprawdzający poprawny napis `Śmierci` i brak mojibake w plikach overlay.

- Naprawiono wyszukiwarkę bossów w prawym górnym rogu widoku Bosses: statyczny placeholder zastąpiono edytowalnym polem z filtrowaniem po fragmencie nazwy. Dodano testy regresyjne dla pola wyszukiwania i dopasowania nazw bossów.

- Dodano timer bossa w trzeciej linii overlaya, widoczny tylko podczas aktywnego spotkania i formatowany jako `HH:MM:SS`. Czas zabicia bossa jest zapisywany w historii jako `KillDuration` w momencie oznaczenia bossa jako pokonanego.

- Naprawiono ucinanie panelu `Binding Reminders` na Dashboardzie, dodajac przewijanie zawartosci tej zakladki. Dodano test regresyjny potwierdzajacy, ze Dashboard ma `ScrollViewer` i zawiera przypomnienia skrotow.

- Dodano globalny, nowoczesny styl scrollbara w `MainWindow.xaml`, dopasowany do ciemno-zlotego motywu aplikacji. Styl obejmuje zaokraglony tor, zloty uchwyt oraz stany hover i przeciagania.

- Poprawiono dolny status bar glownego okna na kompaktowy pasek HUD z osobnymi segmentami `STATUS`, `SMIERCI`, `OVERLAY`, `DETECTION`, `HOTKEYS` i `DOCUMENTATION`. Dodano test regresyjny sprawdzajacy strukture i styl paska.

- Dodano test regresyjny sprawdzajacy, ze pole fraz detekcji w Settings nie wymusza jasnego tla i ciemnego tekstu.

- Dodano widok Bosses oparty o dane z `deaths.json`, pokazujacy aktywnego bossa z `activeBoss` i pokonanych bossow z `bossHistory`. Odblokowano nawigacje do tej zakladki i poprawiono kolorystyke kart pokonanych bossow, aby nazwy byly czytelne w ciemnym motywie.

- Naprawiono przewijanie logow w widoku Detection tak, aby przewijal sie panel logow, a nie cale okno. Usunieto zewnetrzny `ScrollViewer` z tej zakladki i zostawiono przewijanie przy samej liscie logow.

- Ujednolicono panel statusu monitora w widokach Dashboard i Detection: dodano jeden przycisk `START`/`STOP` zmieniajacy kolor z zielonego na czerwony oraz czytelniejsze oznaczenie stanu. Dodano testy regresyjne dla obu miejsc.

- Naprawiono ołówek edycji aktywnego bossa tak, aby zmiana nazwy nie zerowała licznika śmierci bossa. Dodano edytowalne pole nazwy aktywnego bossa oraz ręczne przyciski `-` i `+` przy aktywnym spotkaniu.
- Naprawiono zamykanie aplikacji po kliknieciu X w glownym oknie, ustawiajac `ShutdownMode="OnMainWindowClose"` w WPF. Dodano test regresyjny sprawdzajacy ten tryb zamykania.
- Dodano atomowy zapis plikow JSON i blokade pojedynczej instancji aplikacji, aby rownolegle uruchomione procesy nie ucinaly `deaths.json`. Dodano test regresyjny dla przerwanego zapisu.
- Dodano `AGENTS.md` i `TODO.md` do `.gitignore` oraz usunieto pozostawione markery konfliktu z tego pliku.
- Usunieto z indeksu Git lokalne pliki `AGENTS.md`, `TODO.md` oraz ustawienia `.vs/`.
- Przebudowano wyglad glownego okna na ciemny motyw inspirowany Elden Ring, z boczna nawigacja oraz widokami Dashboard, Detection i Settings. Dodano obsluge przelaczania widokow z menu bocznego.
- Naprawiono obsluge ustawien z menu Settings: dodano przelacznik overlay, stosowanie pozycji i widocznosci overlay po zapisie, walidacje folderu danych oraz restart dzialajacej detekcji po zmianie parametrow. Dodano test sprawdzajacy, ze zakladka Settings zawiera bindingi do edytowalnych ustawien.
- Podpieto `Detection Log` pod rzeczywiste wpisy loggera zamiast statycznych przykladow w XAML. Dodano czyszczenie logu w UI, wyroznianie zdarzen detekcji oraz test regresyjny dla emitowania wpisow loga.

- Usunieto z naglowka glownego okna ikony trybika, odswiezania i pomocy. Dashboard nie jest juz zawiniety w pionowy `ScrollViewer`, aby nie pokazywal prawego suwaka.
- Poprawiono pole `Detection Phrases` w ustawieniach tak, aby uzywalo ciemnego tla i jasnego tekstu jak pozostale pola. Usunieto lokalne jasne nadpisanie kolorow z `MainWindow.xaml`, a testy `dotnet test EldenDeathCounter.sln` przeszly: 49/49.

## 2026-05-25

- Naprawiono zamykanie aplikacji tak, aby przy wyjsciu przez przycisk X oczekiwala na zakonczenie petli detekcji. Dodano test regresyjny dla awaitowalnego zatrzymania `DeathDetectionService`.

- Utworzono plik `CHANGELOG.md`, aby od teraz zapisywać krótkie informacje o zmianach w projekcie. Nie znaleziono lokalnej historii Git ani dostępu do poprzednich czatów, więc wcześniejsze zmiany nie zostały odtworzone.
- Dodano do `AGENTS.md` zasady aktualizowania `CHANGELOG.md` po każdej wykonanej zmianie w projekcie.
