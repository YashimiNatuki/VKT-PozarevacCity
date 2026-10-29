# Ljudski 3D kandidat u garderoberu — 29. oktobar 2026.

Dodali smo stvaran teksturisan 3D model za poređenje sa sadašnjim stilizovanim igračem. Model ima telo, oči, obrve, kosu, zube, košulju/farmerke i cipele, 53 kosti i 46.064 trougla. Ovo je **opcion pregled**, ne zamena igrivog avatara. Hod, trening, policija i ostali NPC-i još koriste prethodni rig. Kandidat nema aktivnu mimiku, govor ili animaciju.

## Kako otvoriti

U igri pritisni **L → ODEĆA / GARDEROBER → NOVI 3D LIK / PREGLED**. Strelice okreću model, **LICE / CEO LIK** menja kadar. **NAZAD NA TVOG LIKA** vraća originalni pregled, a **NAZAD U GRAD / ESC** vraća igru. Izbor odeće i izraza originalnog lika ne primenjuje se na kandidata. Ponovno otvaranje garderobera počinje originalnim likom.

## Arhitektura i poreklo

- `CityWardrobePanel.cs` zadržava postojeći RenderTexture, kameru, sloj 30 i vlasništvo pauze. Novi model se učitava tek na izbor; dalje se koristi isti primerak. Uvezena umetnička sredstva ne mogu dodati ponašanja, kolizije, kameru ili audio vlasnika. Ako kandidat nedostaje, originalni pregled ostaje dostupan.
- `CityHumanCandidateImport.cs` je eksplicitan importer samo za `Resources/Models/RodjaHuman`. Uvozi teksture do 2048 px, URP Lit materijale, clipping kose/obrva i prefab bez animacionog kontrolera. Ne prepravlja postojeće Kenney modele ili globalne render postavke.
- `CityHumanCandidateTests.cs` proverava dostupnost i očuvan izvor igrača, rig/teksture/geometrijski budžet, normalizaciju na udaljenoj sceni i čišćenje neuspelog pregleda.
- `CityPlayerValidation.cs` i `Tools/Test-HumanPreview.py` proveravaju stvaran Windows player, četiri snimka, ponovljeno korišćenje istog modela, očuvan profil i kontrolu nakon izlaska; zatim postojeći trening.
- Blender 4.5.9 + MPFB 2.0.18 su korišćeni lokalno. Izvorni MakeHuman sistemski resursi imaju CC0 izjave; [provenance i hash vrednosti](RodjaHuman-Provenance-20261029.json) i [tekst licence](RodjaHuman-CC0-LICENSE.md) prate paket. MPFB alat ima zasebnu GPL licencu; njegov programski kod nije ugrađen u igru. [Zvanična dokumentacija izvoza](https://static.makehumancommunity.org/mpfb/docs/exporting/index.html).

`Tools/Character/build_human.py` je skripta koja je napravila geometriju iz pregledanih izvora. Za ponavljanje zahteva izolovan produkcioni direktorijum sa `source/mpfb2-2.0.18/src/mpfb` i `selected-assets`; ne preuzima proizvoljne dodatke. Primer: Blender `--background --factory-startup --disable-autoexec --threads 4 --python-exit-code 1 --python Tools/Character/build_human.py -- <production-directory>`. Izlaz FBX/GLB/manifest i radni .blend ostaju u produkcionom direktorijumu dok se eksplicitno ne uvezu. Osnovni podaci i odeća se pripreme pre izvoza; slojevi pomoći i pokrivena geometrija uklanjaju se, a skinning ostaje.

## Provere i granice

Pre implementacije fokusirani test je pao zato što kandidat nije postojao. Posle uvoza: **4/4 fokusirana testa**. Puna Unity provera: **259 uspešnih, 0 neuspešnih, 1 opcioni ArcGIS skip**. Windows build: izlaz 0, 0 prijavljenih build grešaka. Native pregled i naknadni trening: izlaz 0. [Završna mašinski čitljiva evidencija](Verification-Human-Preview-20261029.json) navodi regresije i paket.

Vizuelno su pregledani Windows snimci [lica](Screenshots/human-face-windows-20261029.png), [celog tela](Screenshots/human-front-windows-20261029.png), [profila](Screenshots/human-side-windows-20261029.png) i [leđa](Screenshots/human-back-windows-20261029.png). To je detaljniji početni ljudski model, ne tvrdnja o fotorealizmu ili završnoj umetničkoj oceni korisnika. Izraz je za sada neutralan, poza statična, a odeća probna. Nema LOD-a ili dokaza performansi mnoštva takvih NPC-a.

Android/iPhone, ljudsko slušanje glasova, dvadesetominutni test performansi i animacioni adapter novog modela nisu provereni ovom celinom. Špica ostaje ranija nova remontaža; nisu generisani novi AI video kadrovi.

Sledeći rad: mapiranje novog skeleta → hod/idle i proporcije → benč/boks/kontakt → lice i vizeme → odeća/LOD. [Celokupan registar zahteva i osam narednih isporuka](VKT-Svi-Zahtevi-I-Redosled-20261029.md) zadržava ostale ideje i njihove zavisnosti.
