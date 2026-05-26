# Changelog

## 2026-05-26

- Dodano atomowy zapis plikow JSON i blokade pojedynczej instancji aplikacji, aby rownolegle uruchomione procesy nie ucinaly `deaths.json`. Dodano test regresyjny dla przerwanego zapisu.
- Dodano `AGENTS.md` i `TODO.md` do `.gitignore` oraz usunieto pozostawione markery konfliktu z tego pliku.
- Usunieto z indeksu Git lokalne pliki `AGENTS.md`, `TODO.md` oraz ustawienia `.vs/`.

## 2026-05-25

- Naprawiono zamykanie aplikacji tak, aby przy wyjsciu przez przycisk X oczekiwala na zakonczenie petli detekcji. Dodano test regresyjny dla awaitowalnego zatrzymania `DeathDetectionService`.

- Utworzono plik `CHANGELOG.md`, aby od teraz zapisywać krótkie informacje o zmianach w projekcie. Nie znaleziono lokalnej historii Git ani dostępu do poprzednich czatów, więc wcześniejsze zmiany nie zostały odtworzone.
- Dodano do `AGENTS.md` zasady aktualizowania `CHANGELOG.md` po każdej wykonanej zmianie w projekcie.
