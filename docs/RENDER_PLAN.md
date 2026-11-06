# Plan: migracja silnika grafiki (OpenGL → warstwa GPU → WebGPU)

Rozwinięcie kroków 3–4 z `docs/PLATFORMY_PLAN.md`. Cel: render bardzo wydajny, a silnik graficzny
(backend) wymienialny bez ruszania gry: dziś OpenGL 3.3, potem WebGPU (PC i przeglądarka), w przyszłości
np. bezpośrednio Vulkan, Direct3D 12 albo Metal.

## Stan obecny (analiza kodu, listopad 2026)

- Ok. 8 tys. linii w `src/Blokolo/Render`; 9 shaderów GLSL; 47 ustawień uniformów po nazwie
  (`shader.Set("uX", …)`); ok. 30 miejsc z wywołaniami rysowania.
- Wywołania `GL.*` są w kilkunastu plikach. Oprócz `Render/` także `GameRender.cs`, `GameSigns.cs`,
  `GameStressGpu.cs`, `Gui/Mobs18/MobScreens.cs`, `Commands/GameCommandHud.cs` (logika gry dotyka GPU).
- Stan GPU (`GL.Enable/Disable`, mieszanie, głębia, culling) ustawiany ręcznie w wielu miejscach. To
  źródło błędów („przeciekający” stan) i nie przenosi się na nowoczesne API.
- Dobre podstawy:
  - teren w dużych buforach regionów (`TerrainRegion`) rysowany przez `MultiDrawElementsBaseVertex`;
  - wspólny bufor indeksów quadów;
  - tablica tekstur (atlas jako warstwy);
  - instancjonowanie TNT;
  - jeden przebieg HDR + `PostFx`.
- Wierzchołek terenu ma 28 bajtów.
- Typy matematyczne z OpenTK (`Matrix4`, `Vector3`) są w 36 plikach.

## Architektura: trzy warstwy

```
Gra / GUI          → opisuje CO narysować (kamera, lista chunków, encje, sprite'y, napisy)
Renderer (wspólny) → JAK: przebiegi (teren, encje, niebo, cząsteczki, UI, PostFx), shadery WGSL
IGpu (RHI)         → cienki interfejs w stylu WebGPU, bez logiki gry
Backendy           → Gpu.OpenGL (zgodność), Gpu.WebGpu (PC: wgpu-native, przeglądarka: navigator.gpu),
                     Gpu.Null (testy bez okna); w przyszłości np. Gpu.Vulkan
```

Zasady:
- **Gra i GUI nie wołają GPU.** Wypełniają dane klatki (`FrameData`) albo używają `SpriteBatch`/`Font`,
  które same piszą do `IGpu`. `GL.*` występuje wyłącznie w `Gpu.OpenGL`.
- **IGpu wzorowane na WebGPU.**
  - WebGPU to wspólny podzbiór D3D12/Vulkan/Metal, więc mapuje się na każde nowoczesne API, a na OpenGL 3.3
    też da się go zrobić (to droga wgpu i bgfx).
  - API w stylu OpenGL („ustaw stan, rysuj”) dałoby się przenieść na WebGPU tylko wolno.
- **Niezmienne obiekty pipeline.** Shader, układ wierzchołków, mieszanie, głębia i culling to jeden obiekt
  tworzony przy starcie z pamięcią podręczną po opisie. Koniec z `GL.Enable` w kodzie renderu i koniec
  przycięć przy pierwszym użyciu (w przeglądarce `createRenderPipelineAsync`).
- **Uniformy jako typowane struktury C#** (`FrameUniforms`, `TerrainUniforms`) w buforach, w grupach:
  - grupa 0 – raz na klatkę: kamera, czas, mgła, wiatr;
  - grupa 1 – materiał: tekstury;
  - grupa 2 – na rysowanie: przesunięcie dynamiczne.

  Zamiast nazw jak `Set("uFoo")` mamy kontrolę kompilatora, a selftest sprawdza zgodność układu z WGSL.
- **Uchwyty jako struktury** (`GpuBuffer`, `GpuTexture`, `GpuPipeline`), a nie obiekty z wirtualnymi
  metodami. Koszt interfejsu jest pomijalny przy rysowaniu partiami; liczą się liczba rysowań, zmiany
  stanu i przesyłanie danych.
- **Możliwości karty** (`GpuCaps`) decydują o efektach i profilach grafiki. Przykłady: compute, zapytania
  czasu, rysowanie pośrednie (indirect), multi-draw-indirect, kompresja tekstur BC/ETC2/ASTC.
- **Konwencje różniące API** chowa backend:
  - zakres głębi (WebGPU 0..1, GL −1..1) – backend daje macierz korekcji;
  - na WebGPU odwrócone Z z `depth32float` dla dokładności na dużych odległościach;
  - początek układu tekstur i odwrócenie przy zrzutach ekranu.
- **VSync i tempo klatek należą do backendu i hosta** (tryb prezentacji Fifo/Mailbox), nie do gry.

Szkic interfejsu (do dopracowania w prototypie):

```csharp
public interface IGpu
{
    GpuCaps Caps { get; }
    GpuBuffer CreateBuffer(in BufferDesc d);            // vertex/index/uniform/storage/indirect
    GpuTexture CreateTexture(in TextureDesc d);         // 2D, tablica, render target, głębia
    GpuPipeline CreatePipeline(in PipelineDesc d);      // shader + wierzchołki + blend/depth/cull, cache
    GpuBindGroup CreateBindGroup(in BindGroupDesc d);
    void Write<T>(GpuBuffer b, int offset, ReadOnlySpan<T> data) where T : unmanaged;
    void WriteTexture(GpuTexture t, int layer, int mip, ReadOnlySpan<byte> px);
    IFrame BeginFrame();                                 // null = pomiń (np. zminimalizowane okno)
}
public interface IFrame : IDisposable                    // Dispose = wyślij i pokaż
{
    TransientRing Transient { get; }                     // pamięć na klatkę: sprite'y, cząsteczki, encje
    IRenderPass BeginPass(in PassDesc d);                // cele, czyszczenie, głębia
}
public interface IRenderPass : IDisposable
{
    void SetPipeline(GpuPipeline p);
    void SetBindGroup(int slot, GpuBindGroup g, uint dynamicOffset = 0);
    void SetVertexBuffer(int slot, GpuBuffer b, long offset = 0);
    void SetIndexBuffer(GpuBuffer b, IndexFormat f);
    void DrawIndexed(int indices, int instances = 1, int firstIndex = 0, int baseVertex = 0, int firstInstance = 0);
    void DrawIndexedIndirect(GpuBuffer args, long offset);
}
```

## Shadery: jedno źródło

- Piszemy w **WGSL** (natywny język WebGPU).
- Dla OpenGL kompilator **naga** (z projektu wgpu) generuje przy budowaniu GLSL 330, docelowo też SPIR-V,
  HLSL i MSL. Kolejny backend nie wymaga więc przepisywania shaderów.
- Wygenerowane pliki trafiają do repo (budowanie nie wymaga narzędzi Rust).
- Selftest kompiluje wszystkie warianty i sprawdza układ struktur.
- Warianty (fale, mgła, jakość) przez stałe nadpisywane przy tworzeniu pipeline (`override` w WGSL),
  a nie przez sklejanie tekstu.
- Zasada z GLSL zostaje: same znaki ASCII w kodzie shaderów.

## Wydajność: założenia i budżety

| Obszar | Rozwiązanie | Budżet |
|---|---|---|
| Rysowanie terenu | Bufory regionów jak dziś. Na WebGPU `DrawIndexed` z `baseVertex` na każdy widoczny ciągły zakres. Sekcje kolumny chunka w buforze obok siebie, więc jeden zakres na kolumnę. | ≤ 1500 rysowań na klatkę przy zasięgu 12 |
| Później: GPU-driven | Compute shader odrzuca chunki poza widokiem (frustum, potem okluzja Hi-Z) i zapisuje argumenty rysowania. `DrawIndexedIndirect`, a na PC multi-draw-indirect (jedno wywołanie na region). | CPU renderu < 1 ms |
| Wierzchołek terenu | Kompresja 28 → 16 bajtów: pozycja `uint16` w układzie regionu, UV + warstwa, światło, flagi. Ważne na telefonach (pamięć i przepustowość). | −40% pamięci terenu |
| Dane dynamiczne | Jeden pierścieniowy bufor na klatkę (`TransientRing`, np. 3 × 4 MB) dla sprite'ów, napisów, cząsteczek i encji; jeden zapis na klatkę. | 0 alokacji GC na klatkę |
| Encje i moby | Instancjonowanie według modelu (jak `TntInstancer`): macierze w buforze storage. | 1 rysowanie na typ modelu |
| Przesyłanie chunków | Meshowanie w tle (`IJobs`); limit przesyłu na klatkę, żeby nie było przycięć. | ≤ 4 MB/klatkę |
| UI | `SpriteBatch` łączy wszystko w partie po teksturze (atlas). | ≤ 20 rysowań UI |
| Pipeline | Wszystkie tworzone przy starcie / ładowaniu, nigdy w trakcie gry. | 0 przycięć |
| Pomiar | Zapytania czasu GPU (gdy `Caps` pozwala) i czas CPU na przebieg w nakładce F3 i w podglądzie. | — |

Kryterium każdego kroku: **FPS nie niższy niż przed zmianą** w tej samej scenie podglądu
(`--showcase --gfx-preview`, stały seed, ta sama pozycja kamery), na RTX i na Radeonie 610M.

## Wymiana silnika w przyszłości

- Nowy backend = implementacja `IGpu` (szacunkowo 1,5–2,5 tys. linii) + cel w generatorze shaderów (naga).
  Gra, renderer i shadery bez zmian.
- Wybór silnika przy starcie: `--gpu-api=auto|webgpu|opengl`. W ustawieniach „Silnik grafiki:
  Automatycznie / WebGPU / Zgodność (OpenGL)”. Gdy uruchomienie się nie powiedzie, przejście do następnego
  silnika z komunikatem.
- `Gpu.Null` zapisuje polecenia: testy renderu bez okna i bez karty (selftest).
- Porównanie zrzutów między backendami: ta sama scena na OpenGL i WebGPU, różnica pikseli poniżej progu.
  Wychwytuje błędy przy każdej wymianie.
- Nie bierzemy gotowej warstwy:
  - Veldrid nie jest rozwijany i nie ma WebGPU;
  - silniki (Stride, Evergine) są za ciężkie i narzucają strukturę;
  - powiązania wgpu dla .NET (Silk.NET WebGPU, WgpuSharp) najwyżej jako sam P/Invoke pod naszym `IGpu`.

## Kroki (każdy kończy się sprawdzeniem przez użytkownika i commitem)

| # | Krok | Gotowe, gdy |
|---|------|-------------|
| R0 | W prototypie (krok 1 planu platform): pierwsza wersja `IGpu` i backend WebGPU w przeglądarce. Prototyp sprawdza też interfejs, a nie tylko wydajność. | prototyp działa na `IGpu`, wnioski dopisane tutaj |
| R1 | Odcięcie gry od GL: `GameRender`, `GameSigns`, `MobScreens`, `GameCommandHud`, `GameStressGpu` bez `GL.*`; wszystko przez `Render/`. | zrzuty identyczne, FPS bez zmian |
| R2 | `IGpu` + backend OpenGL. Przeniesienie po kolei: `PostFx` → niebo → `SpriteBatch`/czcionki → encje → teren. Uniformy w strukturach (UBO), stan w pipeline. Na końcu `GL.*` tylko w `Gpu.OpenGL`. | zrzuty identyczne, FPS ≥ przed |
| R3 | Shadery w WGSL, GLSL generowany przez naga. | zrzuty identyczne |
| R4 | Backend WebGPU na PC (wgpu-native), na razie jako opcja; porównanie zrzutów i FPS z OpenGL. | różnica zrzutów < próg, FPS ≥ OpenGL |
| R5 | WebGPU domyślnie, OpenGL jako „Zgodność” (obejście VSync przez DWM zostaje tylko tam). | brak rwania na laptopie hybrydowym |
| R6 | Wydajność: kompresja wierzchołka, odrzucanie chunków na GPU, rysowanie pośrednie, czasy GPU w F3. | budżety z tabeli spełnione |

Potem wersja przeglądarkowa (krok 5 planu platform) używa tego samego renderu, a etapy grafiki 3–7
(cienie, woda, bloom, chmury) piszemy raz w WGSL.

## Ryzyka

- Rysowanie pośrednie z `firstInstance` i multi-draw-indirect to w WebGPU funkcje opcjonalne. Dlatego
  zawsze jest ścieżka zapasowa (`DrawIndexed` w pętli).
- Narzut walidacji WebGPU w przeglądarce przy wielu rysowaniach: stąd limit 1500 i łączenie zakresów.
- naga może źle przetłumaczyć rzadkie konstrukcje na GLSL 330: shadery piszemy prostym WGSL, a każdy
  wariant sprawdzamy zrzutem.
- Przejście z OpenTK.Mathematics na System.Numerics (36 plików) robimy w kroku 2 planu platform, nie
  przy okazji renderu.
