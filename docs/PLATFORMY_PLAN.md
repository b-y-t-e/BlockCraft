# Plan: Blokolo na PC, w przeglądarce i na telefonie

**Cel:** jeden kod w .NET (silnie typowany), wersje na PC i w przeglądarce, później Android, z obsługą pada
i dotyku oraz grafiką ładniejszą niż w Minecrafcie. Etapy grafiki: `docs/GRAFIKA_PLAN.md`.

## Render

WebGPU jako jeden render dla PC (wgpu-native: Direct3D 12 / Vulkan / Metal) i przeglądarki (`navigator.gpu`),
shadery pisane raz w WGSL. Obecny OpenGL 3.3 zostaje jako tryb zgodności dla starych komputerów.

Dlaczego:
- WebGPU sam zarządza wyświetlaniem klatek (tryb FIFO bez rwania, także w laptopach z dwiema kartami);
  w przeglądarce klatki synchronizuje przeglądarka. Koniec problemów z VSync w OpenGL.
- Jedno API i jeden język shaderów na wszystkie platformy; compute shadery (chmury, odbicia, culling na GPU).
- Ograniczenie: brak ray tracingu (ewentualnie później Vulkan RT tylko na PC).
- Biblioteki .NET do WebGPU są młode (WgpuSharp alpha, Istok.WebGPU preview, Silk.NET 3.0 bez gwarancji
  przeglądarki), więc własna cienka warstwa: w przeglądarce `[JSImport]`, na PC P/Invoke do wgpu-native,
  jeden interfejs C#.

## Struktura kodu

```
Blokolo.Core        – logika gry: świat, bloki, encje, przedmioty, protokół sieci, logika GUI
                         (bez OpenTK; System.Numerics)
Blokolo.Client      – pętla klienta, ekrany, mesher, opis przebiegów renderu; interfejsy:
                         IGpu (w stylu WebGPU), IInput (pad + dotyk), IAudio, IStorage, ITransport, IJobs
Blokolo.Gpu.WebGpu  – backend PC (wgpu-native) i przeglądarki (navigator.gpu)
Blokolo.Gpu.OpenGL  – obecny render jako tryb zgodności
Blokolo.Desktop     – okno, OpenAL, pliki, wątki, Tailscale/TCP
Blokolo.Web         – net10.0-browser: canvas, requestAnimationFrame, Gamepad API, dotyk, WebAudio,
                         OPFS, WebSocket, PWA
Blokolo.Android     – (opcjonalnie) net-android + wgpu (Vulkan)
```

## Kroki

| # | Krok | Po co |
|---|------|-------|
| 0 | Commit obecnej pracy (etapy grafiki 1–2, wybór karty, VSync przez DWM) | czysty punkt startu |
| 1 | Prototyp (1–2 dni): teren z chunkami w przeglądarce, .NET + WebGPU, pomiar FPS i czasu meshowania | decyzja, czy kierunek ma sens |
| 2 | Wydzielenie `Core` i interfejsów platformy; wspólne wejście: pad + sterowanie dotykowe na ekranie; gra dalej na OpenGL | gra działa identycznie, testy bez okna |
| 3 | Warstwa renderu w stylu WebGPU; obecny OpenGL przeniesiony pod nią | jedna ścieżka renderu |
| 4 | Backend WebGPU na PC jako domyślny (OpenGL = „Zgodność”), etapy grafiki 1–2 w WGSL | koniec problemów z VSync |
| 5 | Wersja przeglądarkowa: Gamepad API, dotyk, zapis w OPFS, WebAudio, WebSocket (przekaźnik), PWA | gra w przeglądarce i na telefonie |
| 6 | Etapy grafiki 3–7 raz dla wszystkich: niebo, cienie, woda z odbiciami, bloom, promienie słońca, chmury | piękna grafika wszędzie |
| 7 | (opcjonalnie) natywny Android; ray tracing na RTX (tylko PC) | dodatki |

Orientacyjnie (duża niepewność): krok 2 – 1–2 tygodnie, kroki 3–4 – 2–4 tygodnie, krok 5 – 2–3 tygodnie.
Cienie, wodę i bloom robimy dopiero po kroku 4, żeby nie pisać ich dwa razy (GLSL i WGSL).

## Wejście: pad i dotyk

- Pad: na PC przez GLFW, w przeglądarce przez Gamepad API (Android Chrome, iOS Safari; standardowy układ
  Xbox/PS, wibracje w Chromium). Logika (`PadBindings`, nawigacja krzyżakiem, klawiatura ekranowa) wspólna.
- Dotyk tworzy ten sam stan co pad (gałki + przyciski), więc każda akcja dostępna z pada działa też na dotyku.
- Układ jak w Bedrock: lewa strona – wirtualna gałka (tam, gdzie palec), prawa – obrót kamery; stuknięcie
  = postaw/użyj, przytrzymanie = kop/atakuj; przyciski: skok, skradanie, ekwipunek, czat, pauza; pasek
  przedmiotów stuknięciem. W menu i ekwipunku stuknięcie = kliknięcie, przeciąganie przedmiotów palcem.
- Pojawia się po pierwszym dotknięciu, chowa się po podłączeniu pada albo użyciu klawiatury. Ustawienia:
  auto/wł./wył., rozmiar, przezroczystość, później przesuwanie przycisków.
- PWA: pełny ekran, poziomy obrót, blokada wygaszania, bez przewijania i powiększania gestami.
- Dotyk można zrobić już w kroku 2 na PC (laptopy z ekranem dotykowym, podgląd ze zrzutami jak przy padzie).

## Zasady od teraz

- Pętla gry sterowana przez host: `Frame(dt)`; VSync należy do hosta i backendu, nie do logiki gry.
- Zadania w tle przez `IJobs`, bez `new Thread` (PC: wątki; przeglądarka: porcje na klatkę / Web Worker).
- Zapis i sieć asynchronicznie (`IStorage`, `ITransport`); przekaźnik sieci przyjmuje też WebSocket.
- Każda akcja dostępna z pada, a przez to także z dotyku.
- Nowe efekty graficzne pisane raz, w WGSL (po kroku 4).

## Ograniczenia

- .NET w przeglądarce: w .NET 11 (premiera listopad 2026) WebAssembly przechodzi z Mono na CoreCLR jako
  podgląd, stabilnie w .NET 12; wielowątkowość budowana na CoreCLR, realnie .NET 12 (~listopad 2027).
  Do tego czasu jeden wątek + ewentualnie Web Worker (szablon w .NET 11). Potwierdzić w oficjalnych
  notatkach wydania .NET 11 przed startem.
- Przeglądarka: brak zwykłych plików (OPFS/IndexedDB), sieć tylko WebSocket/WebRTC (bez Tailscale),
  paczki zasobów przez wczytanie pliku zip (źródła git tylko na PC), rozmiar pobieranego pliku .wasm.
- WebGPU: Chrome/Edge 113+, Safari 26, Firefox 141+ (Windows); Android Chrome na nowszych urządzeniach;
  starsze urządzenia → tryb zgodności.
- WebGPU nie ma ray tracingu.

## Źródła

- WebGPU w przeglądarkach: https://web.dev/blog/webgpu-supported-major-browsers,
  https://github.com/gpuweb/gpuweb/wiki/implementation-status
- .NET 11 / WebAssembly: https://ironsoftware.com/news/industry-news/dotnet-11-aspnet-core-blazor-agentic-web/,
  https://aminsafaei.info/blog/coreclr-webassembly-dotnet,
  https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/runtime.md
- Biblioteki: https://www.nuget.org/packages/WgpuSharp/0.5.0-alpha, https://www.nuget.org/packages/Istok.WebGPU,
  https://github.com/dotnet/Silk.NET
