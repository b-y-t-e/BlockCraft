# Blokolo — zasady pracy

Blokolo: gra wywodząca się z BlockCraft (klonu Minecrafta 1.8, github.com/fkliczewski/BlockCraft), rozwijana jako osobny projekt:
MC 1.8 to punkt wyjścia, nie ograniczenie (pad, profile grafiki, efekty, nowe menu, mody). C# (.NET 10, OpenTK 4.8, OpenGL 3.3). Jeden projekt: `src/Blokolo`. Teksty gry, komentarze
i dokumentacja po polsku.

## Budowanie, uruchamianie, testy

- `python run.py` — kompilacja Release i gra; argumenty dla gry po `--` (np. `python run.py -- --selftest`).
- `dotnet build src/Blokolo -c Release` — sama kompilacja (stare ostrzeżenia: CS0108 ×2, CS8321, CA2014).
- Selftest (logika, bez okna gry): `Blokolo.exe --selftest` (cały, ~80 s) oraz zawężone: `--packs-only`, `--pad-only`,
  `--search-only`, `--cmd-only`, `--audio-only`. Nowa funkcja = nowy plik `*SelfTest.cs` zarejestrowany w `SelfTest.cs`.
- Podglądy ze zrzutami do `bin/Release/net10.0/showcase/`: `--showcase` (media README), `--showcase --packs-preview`
  (paczki zasobów), `--showcase --pad-preview` (pad podstawiany przez `Gamepad.Inject`, bez sprzętu), `--showcase --menus-preview` (menu i ustawienia), `--autotest`.
- Zmianę widoczną na ekranie sprawdzamy zrzutem z podglądu, nie tylko selftestem.

## Zasady projektowe

- **Każda funkcja musi być w pełni dostępna z pada** (Xbox / PlayStation / 8BitDo), nie tylko z myszy i klawiatury:
  - nowy ekran: przyciski jako widżety (krzyżak do nich przeskakuje) albo `PadTargets()`; listy przez `IPadLists`/`PadList`;
    zakładki `IPadTabs`; własne akcje slotów `IPadQuickMove`; własne podpowiedzi `IPadHints`; ekrany zbierające
    przyciski `IPadCapture` (wszystko w `Gui/PadScreens.cs`);
  - pole tekstowe: `TextField` albo `WantsText`/`TextY`, żeby pokazała się klawiatura ekranowa;
  - nowa akcja w grze: `PadAction` z domyślnym przyciskiem w `Core/PadBindings.cs` (da się ją przypisać w „Przyciski...”);
  - nic tylko na klawisz bez odpowiednika na padzie; sprawdzić w `--showcase --pad-preview` i `--selftest --pad-only`.
- Bazowa rozgrywka jak w MC 1.8; nowe funkcje mogą od niej odchodzić — opisać to w komentarzu i dokumentacji.
- Repozytorium: b-y-t-e/BlockCraft (remote `origin`, praca na `main`); projekt źródłowy fkliczewski/BlockCraft jako `upstream` tylko do odczytu, bez PR.
- Paczki zasobów: bez paczki gra wygląda jak dotąd; dane z paczek przez `ResourcePacks`, plan w `docs/PACZKI_ZASOBOW_PLAN.md`.
- Menu: najwyżej kilka opcji naraz, jedna główna akcja na górze. Nowe ustawienie trafia do właściwej kategorii
  `OptionsScreen` (najwyżej 6 opcji w kategorii) z jednozdaniowym opisem w `Hint`.
- Wejście (mysz, klawiatura, pad) przez `RunOrDefer` — zdarzenia w trakcie dzielonego ticku są odkładane.
- Pliki mają mieszane końce linii (CRLF i LF) — zachować istniejące w edytowanym pliku.

## Dokumentacja

- `README.md` — funkcje, sterowanie, flagi uruchomienia; aktualizować przy nowych funkcjach i flagach.
- `docs/PACZKI_ZASOBOW_PLAN.md` — etapy paczek zasobów, pady (zadanie B), lista TODO.
- `docs/MC18_*.md` — raporty integracji 1.8, sieć (`MC18_NET.md`).
