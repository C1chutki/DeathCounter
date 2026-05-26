# Game Profile Storage Design

The app will store game-specific files under one desktop root folder:

`<Desktop>\DeathCounter\EldenRing`
`<Desktop>\DeathCounter\DarkSouls3`

Each game profile owns its own `appsettings.json`, `deaths.json`, `log.txt`, and screenshot subfolders. Switching the header buttons changes the theme and loads that profile's settings, counter state, and log target. Elden Ring and Dark Souls 3 no longer share death data or logs.

Implementation notes:
- Add a core `AppGameProfile` definition with folder names and associated themes.
- Change default settings to use a profile-specific folder.
- Make logging and death data targets switchable so existing services can keep their references.
- Update `ER` and `DS3` buttons to call the profile switch instead of theme-only switching.
- Verify with focused unit tests and the full test suite.
