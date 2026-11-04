# Gemini i Copilot: stvarno poslati zadaci i granice rezultata

Dana 4. novembra 2026. zadaci su poslati kroz dva odvojena browser razgovora. Postojeća Gemini web aplikacija i otvoreni Copilot desktop projekat nisu uređivani. Ovo je evidencija saradničkog pregleda; odgovori nisu dokaz da je sistem vozila implementiran ili testiran.

## Gemini / Google AI Studio

Otvoren je novi Playground razgovor u ugrađenom browseru. UI je prikazao Gemini 3.8 Flash, a Thinking Level je postavljen na High. Google Search grounding je isključen za pregled izvora; code execution i function calling nisu uključivani. Nije biran novi API ključ, plaćeni plan ili nova dozvola.

Prvi zadatak traži state machine, jednog vlasnika pomeranja, bezbedan izlazak iz vozila, pauzu, statistiku i adversarial testove. Posle prvog odgovora poslate su korekcije: projekat koristi CityCameraRig, CityInteractable i CityPlayerStats; ne pretpostavljati Cinemachine, izmišljene Session metode ili drugog vlasnika E unosa.

Zatim je poslat tekstualni paket tačno pet javnih C# fajlova iz commita `304f1abeed150eaec7658b52e8f17205275a7088`:

- CityPlayerController.cs
- CityPlayerInteraction.cs
- CityInteractable.cs
- CityCameraRig.cs
- CityPlayerStats.cs

Paket ima 30.564 bajta; lokalni manifest beleži SHA-256 svakog izvornog fajla. Nisu slati korisnički sejvovi, ključevi, lokalne konfiguracije, modeli ili ostatak diska.

Treći odgovor koristi stvarne potpise TryInteract(CitySession), Prompt i instancni RecordMovement(distance, sprinting, riding), uz predlog osam testova. Ostaje ilustrativan: primer menja SetCanAct predicate na false/true, dodaje sopstveni E handler i nema završen oporavak kamere/vlasništva. Direktan uvoz bi zaobišao postojeće gate-ove. Kod nije uvezen, pokrenut ili proglašen produkcionim. Korisni su testovi za jednog vlasnika kretanja, blokirane izlaze i razdvajanje pešačke/vozne statistike; integracija zahteva poseban Unity korak.

## GitHub Copilot

Native prozor je pronađen, ali accessibility nije bio dostupan, a pokušaj aktiviranja prijavio je Windows desktop-composition grešku. Korišćen je zaseban Copilot web razgovor sa eksplicitno izabranim repozitorijumom YashimiNatuki/VKT-PozarevacCity. Drugi otvoreni projekat nije menjan.

UI je prikazao Model Auto i nalog YashimiNatuki sa oznakom Free. To nije provera celokupne korisnikove pretplate ili drugog naloga. Razgovor je uspešno poslat i dobio je odgovor.

Copilot je prijavio čitanje izvora na commitu 304f1ab i dao konkretne linkove na CityProgression, CitySaveStore, CityGarden, CityHideout i testove. Pregledao je predlog profila v10 i dva milestone podatka. Nije imao trenutne nekomitovane v10 izmene i nije tvrdio da ih je testirao.

Prepoznati zahtevi su uključeni u lokalne provere: samo uspešno eksplicitno zalivanje katalog-biljke; voda i milestone u istom snapshot-u; bez izmišljanja istorije pri migraciji; duboke kopije DTO-a; odvojena tri glavna ugovora; očuvani checksum, backup i stale-writer guard. Lokalni izvršni rezultati pripadaju zasebnom završnom izveštaju.

U nastavku istog razgovora Copilot je dobio zadatak da napiše konkretan C# diff i NUnit regresiju za noge početnog Kenney lika na benču. Vratio je dva predloga u Workbench-u, za `CityBenchVisual.cs` i `CityBenchPressTests.cs`, sa bočnim usmerenjem kolena i proverom obuće u više faza. Predlog nije primenjen direktno. Lokalna provera koristi celu skinned mrežu nogu i obuće prema stvarnim collider-ima, a postojeća kalibracija visine stopala ostaje izvor kontakta s podom.

## Dokazi i nastavak

Sirovi DOM odgovori, screenshot-i i paket izvora sa manifestom čuvaju se privatno van Git repozitorijuma u `VKT-External-AI-Review-20261104` uz projektnu porodicu. Sadrže okolni UI naloga i nisu stavljeni u javni repozitorijum. Otvoreni razgovori su ostavljeni korisniku u browseru.

Nije napravljen PR, branch, commit ili push kroz spoljne asistente; nije sinhronizovana AI Studio aplikacija. Kanonski Unity kod i izvršne provere vodi lokalni Codex. Sledeći predlog vozila prihvata se tek posle pregleda tačnog diff-a i Unity/native testova.
