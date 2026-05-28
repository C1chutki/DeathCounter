# Changelog

## 2026-05-29

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
