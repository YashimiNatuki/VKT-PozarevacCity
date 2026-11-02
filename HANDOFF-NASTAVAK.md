# VKT: nastavak rada, 2026-11-01

## Najnovije: zajednički native fixture — 2. novembar 2026.

Tri ljudska native scenarija sada dele privatni scope za virtuelni input, enabled/kursor stanje, neutralizaciju pogleda i identitet vlasnika. Jedan checkpoint poredi snapshot i sejv neposredno oko izbora modela. Potpis profila više ne menja ulazni snapshot. Nezavisan pregled je zatvorio P2 o pogledu i ojačao osetljivost testa.

**300 Unity testova prošlo, 0 palo, 1 opcioni preskočen; 8/8 ciljanih testova.** Windows build, četiri native scenarija, desktop/touch-preview i oba toka prethodne špice prošli su izlazom 0. Sva tri report ugovora i 51 fajl modela ostali su isti. [Arhitektura i provere](Docs/Native-Verification-Framework-20261101.md) | [Dokazi](Docs/Verification-Native-Fixture-20261102.json).

Ovaj korak unapređuje verifikacionu infrastrukturu. Fizički hvat telefona, novi trening-rig, teren, glasovi i novi AI film ostaju naredni gameplay rad. Fizički Android/iPhone i dug FPS/termalni test nisu deo prihvata. Jedan pad shader compilera sačuvan je odvojeno od uspešnih završnih provera.

## Prethodno: mapa i katalog telefona — 1. novembar 2026.

Ispravljen je prelaz mapa → otključani katalog: mapa se gasi kada se katalog otvori. Zaključani katalog ostavlja mapu dostupnom. Dva stvarna uGUI/progresija testa proveravaju oba stanja i nepromenjen profil v9.

**292 Unity testa prošla, 0 palo, 1 opcioni preskočen; 2/2 ciljana testa.** Windows build, native šake/lice/lik/pregled, desktop/touch-preview i oba toka prethodne špice prošli su izlazom 0. [Izmene](Docs/Phone-Screens-20261101.md) | [Dokazi](Docs/Verification-Phone-Screens-20261101.json).

Ovo je uska popravka postojećeg telefona. Fizički hvat telefona, novi trening-rig, teren, glasovi i novi AI film ostaju otvoreni. Sledeći arhitektonski kandidat je kontakt šake/telefona uz odvojen sat prikaza tokom pauze; širi refaktor još nije primenjen.

## Prethodno: šake i stvarni udarac — 1. novembar 2026.

Šake novog lika sada koriste svih 30 zglobova prstiju. Gard zatvara pesnice i poravnava dlanove/zglobove; postojeći unarmed LMB/NAPAD pokreće desni udarac i levi gard. Pauza i cooldown ostaju autoritativni; teleport i izlazak iz napada oslobađaju pozu. Nema novih modela, biblioteka ili promene profila v9.

**290 Unity testova prošlo, 0 palo, 1 opcioni preskočen; 8/8 ciljanih testova.** Windows build, native šake/lice/lik/pregled, desktop/touch-preview i oba toka prethodne špice prošli su izlazom 0. [Izmene](Docs/Human-Hands-20261101.md) | [Dokazi](Docs/Verification-Human-Hands-20261101.json).

Benč/boks još koriste prethodne trening-modele. Držanje telefona, novi hvat šipke, fizički mobilni i dugi FPS test, novi glasovi i novi AI film ostaju otvoreni. Slede kontakt šake/predmeta → novi trening-rig → teren/kontakti → centar → govor → misije.

## Prethodno: lice, mimika i pogled — 1. novembar 2026.

Kapci, vilica, obrve, osmeh i pogled sada koriste 15 stvarnih CC0 facial kontrola. U garderoberu IZRAZ LICA menja pet stanja; u gradu reaguje na povredu, uspeh i napor. Oči su poravnate prema stvarnom Unity/FBX okviru. Pauza zamrzava gradski izraz; pregled radi dok je grad pauziran. Izbor modela ostaje sesijski, profil v9. Benč/boks koriste prethodne trening-modele.

**282 Unity testa prošla, 0 pala, 1 opcioni preskočen; 9/9 facijalnih + 4/4 feedback testova.** Windows build, native lice/lik/pregled, desktop/touch-preview i oba toka špice prošli su izlazom 0. [Izmene i 18 planiranih unapređenja](Docs/Human-Face-20261101.md) | [Dokazi](Docs/Verification-Human-Face-20261101.json).

Nema vizema/lip sync-a, novog srpskog glasa, novog AI filma, fizičkog mobilnog testa ili potvrđenih dugih FPS testova. Špica je prethodna remontaža. Slede šake/hvat, kontakti na neravnom terenu, novi trening-rig, glas/vizemi, gradski centar i misije.

## Prethodno: ljudski lik u gradu — 1. novembar 2026.

L → ODEĆA / GARDEROBER → NOVI 3D LIK / PREGLED → IGRAJ U GRADU bira novi model za tekuću sesiju. WASD i Shift pomeraju isti fizički kontroler; noge i suprotne ruke prate stvarno pomeranje. C menja tri perspektive; prvo lice skriva telo. VRATI ORIGINALNI LIK vraća original. Benč i boks privremeno koriste postojeće trening-modele, pa vraćaju odabrani izgled. Profil ostaje v9; izbor nije trajno zapisan.

269 Unity testova prošlo, 0 palo, 1 opcioni skip. Windows build, stvarni native tok novog lika, garderober pregled, desktop/touch-preview regresije i oba toka špice prošli su izlazom 0. Provereni su pauza, teleport, jedna fizička kapsula, kamere, đon na ravnoj podlozi i povratak iz treninga. [Izmene](Docs/Human-Playable-20261101.md) | [Dokazi](Docs/Verification-Human-Playable-20261101.json).

Nisu potvrđeni kosine/stepenice novog riga, novi hvat/prsti, mimika, nove glasovne replike, LOD, dugi FPS test ili fizički telefon. Slede stopala/karlica na neravnom terenu, benč/boks novog riga, lice/glas, kontakti/misije i optimizacija.

## Prethodno: prvi pokreti ljudskog riga i iPhone plan — 1. novembar 2026.

U pregledu novog modela dodato je dugme POKRET: miran, hod, trčanje i gard. Adapter koristi postojeće funkcije hoda i poza. Ovo je animacija u garderoberu; novi model još nije igrivi avatar, nema mimiku niti zatvorene šake. Svet ostaje pauziran, profil v9 i nagrade se ne menjaju.

264 uspešna Unity testa, 0 neuspešnih, 1 opcioni skip; build, native režimi, desktop/touch-preview regresije i oba toka špice prošli su izlazom 0. [Izmene](Docs/Human-Motion-20261101.md) | [Dokazi](Docs/Verification-Human-Motion-20261101.json) | [iPhone 13+ plan](Docs/iPhone-Plan-20261101.md).

iPhone audit potvrđuje prisutnu Cesium iOS biblioteku i postojeće mobilne kontrole; lokalno nedostaju iOS Build Support i Mac/Xcode. Nema IPA ili fizičkog testa. Slede: stvarni igrivi adapter, stopala/karlica, benč/prsti, mimika/glas i LOD; zatim priča i centralni blok.

## Prethodna dorada: ljudski 3D kandidat i objedinjen plan

Novi MPFB/MakeHuman model je opcioni pregled: **L → ODEĆA / GARDEROBER → NOVI 3D LIK / PREGLED**. Ima 46.064 trougla, 53 kosti, oči, kosu, obrve, telo, odeću i URP materijale. Postojeći igrivi model i profil v9 ostaju aktivni. Kandidat još nema povezane animacije, mimiku ili izbor odeće; naredni korak je adapter njegovog skeleta.

**259 uspešnih Unity testova, 0 neuspešnih, 1 opcioni skip; Windows build, pregled, desktop/touch-preview regresije i oba toka špice — izlaz 0.** Pregled proverava četiri ugla, ponovno korišćenje modela, očuvan save i povratak kontrole. [Izmene](Docs/Human-Preview-20261029.md) | [Dokazi](Docs/Verification-Human-Preview-20261029.json) | [Svi zahtevi i redosled](Docs/VKT-Svi-Zahtevi-I-Redosled-20261029.md) | [TXT kopija](Docs/VKT-Svi-Zahtevi-I-Redosled-20261029.txt).

Špica ostaje nova remontaža postojećeg materijala, ne novi AI film. Fizički Android/iPhone, prirodnost glasova i dugotrajan FPS test nisu potvrđeni. Slede: animirani ljudski rig → kontakti/povezana misija → centralni blok/visine → glasovi/miks → skejt → štek i retka transformišuća klupa.

## Prethodna dorada: šake, gard i nova montaža špice

Hvat šipke sada odvaja zglob od metala i zatvara prste; boksački gard poravnava zglob sa podlakticom i čuva pozu tokom pauze. Profil ostaje v9. **255 uspešnih Unity testova, 0 neuspešnih, 1 opcioni skip; Windows build, oba native toka i dve provere špice imaju izlaz 0.**

Nova 30-sekundna remontaža je uključena u Windows paket; original je sačuvan. To nisu novi AI kadrovi ili novi realistični model. Provereni su prirodan završetak špice, preskakanje i povratak u igru. Fizički Android/iPhone i ljudska ocena zvuka ostaju odvojeni.

[Izmene i granice](Docs/Hand-Contact-20261028.md) | [Evidencija](Docs/Verification-Hand-Contact-20261028.json) | [Konsolidovan plan](Docs/VKT-Stanje-I-Sledeci-Potezi-20261028.md) | [Poreklo špice](Docs/Intro-Integration-20261028.md).

Sledeći veći vizuelni korak je prava 3D glava/telo i adapter rig-a. Pregledani AiRođa/AiGad Noir koristi 2D WebP, pa je vizuelna referenca, ne model koji se direktno prenosi u Unity. Zatim: povezana misija sa kontaktima, prepoznatljiv centralni blok, pregledani srpski glasovi, skejt i retka transformišuća klupa.


## Aktivni izvor i poreklo saveta

Aktivni projekat: `C:\Users\jamal\Documents\VKT-PozarevacCity`.
Predlog Antigravity saradnika i njegov izvorni handoff pročitani su u `C:\Users\jamal\Documents\antigravity\radiant-kepler\VKT-PozarevacCity`. Tamo su commit-i `fcfd5b8` i `53c10f1`, posle prebacivanja na bazu `75401b3`. Taj checkout je sačuvan; ovo nije tvrdnja da su svi njegovi dodaci funkcionalni u aktivnoj igri.

Plan: [Master Blueprint](Docs/VKT-Master-Upgrade-Blueprint.md).
Pregled rizika: [Integracija paralelnih dodataka](Docs/Blueprint-Integration-Review-20261021.md).

## Prethodna dorada: stopala i margina fizičkog kontrolera

`CityFootContact` podešava dve kosti noge i orijentaciju cipele prema ograničenoj statičkoj podlozi. `CityGroundingOffset` uklanja vizuelno lebdenje koje stvara kontaktna margina kapsule, bez pomeranja fizičkog igrača ili kamere. Aktivira ih samo avatar igrača; pauza, vazduh, teleport, boks i disable imaju eksplicitne granice. Profil ostaje v9.

Završno: **245 uspešnih Unity testova, 0 neuspešnih, 1 opcioni skip**. Windows build i oba native toka imaju izlaz 0. Nova reprodukcija koristila je stvarni CharacterController: pre popravke oba testa kosine su pala, posle nje prolaze uz proveru stvarnih vertexa cipela. [Dokumentacija](Docs/Foot-Contact-20261026.md) i [izveštaj](Docs/Verification-Foot-Contact-20261026.json) vode tačne rezultate.

Špica ima [odvojeni montažni kandidat](Docs/Intro-Candidate-20261026.md), bez novih AI kadrova i bez zamene originala u igri. Android kandidat od 24. oktobra je stariji od završne korekcije; ne instalirati ga kao ovu isporuku. Fizički telefon se sada ne testira, po zahtevu korisnika.

Sledeće: hvat šipke/gard → povezana misija/kontakti → centralni blok i proverene visine → skejt → srpski glasovi i miks. [Konsolidovan plan](Docs/Delivery-Plan-20261022.md) razlikuje postojeće sisteme od kandidata. Paralelni Antigravity izvor ostaje odvojen.

## Prethodna dorada: koordinacija hoda i stabilna pauza

`CityGaitState` računa fazu iz pređenog horizontalnog puta i postepeno smiruje pozu pri stajanju. Ruke sada prate suprotne noge. `CityActorMotion` poništava stanje pri teleportu, čuva završne rotacije tokom pauze i proverava `Time.timeScale` već u kadru otvaranja telefona. Nema novog vlasnika kretanja, izdržljivosti ili profila.

Pre popravke Windows provera našla je 33 pogrešna para od 33 uzorka i neuspešno poništavanje brzine posle teleporta. Završni desktop prolaz ima 24 merljiva para, 0 pogrešnih i 0° pomeranja oba kolena tokom pauze. Puna EditMode provera: **231 uspešan test, 0 neuspešnih, 1 preskočen**. Windows build i završni desktop/touch-preview prolazi imaju izlaz 0. Benč, statistika, telefon/mapa/muzika i porudžbina sa jednom isplatom ostali su u istoj proveri.

[Opis, komande i granice](Docs/Locomotion-20261021.md) | [Evidencija](Docs/Verification-Locomotion-20261021.json). Ovo je dorada postojećeg stilizovanog modela; novi model, oslonac na proizvoljnom terenu, glasovi i dugotrajno profilisanje ostaju naredne celine. Fizički mobilni uređaj se ne koristi.

## Prethodna dorada: oslonac stopala i kadar benča

`CityBenchVisual` sada koristi ugao i visinu stopala iz normalizovanog stojećeg modela. Prednji metalni oslonac pomeren je van prostora cipela, a kamera na slobodnu stranu klupe. Novi test proverava 40 položaja na okrenutoj klupi, ugao/visinu stopala, razmak od kolajdera i zadržan hvat šipke.

Puna provera: 223 uspešna testa, 0 neuspešnih, 1 preskočen. Profil v9 i nagrade treninga ostaju isti. [Opis i granice](Docs/Bench-Contact-20261021.md) i [završna evidencija](Docs/Verification-Bench-Contact-20261021.json) vode ishode Windows provera. Hod, ručni zglobovi, lice i ostali kandidati imaju zasebne naredne korake.

## Prethodna povezana celina: statistika

- Preuzet `CityPlayerStats` i njegovi osnovni testovi; dopunjene zaštite od prelivanja brojeva i nevažećeg trajanja.
- Vlasnik statistike je `CityProgression`; nema drugog novčanika, životnog sistema ili rukovaoca misijama.
- Profil v9 čuva statistiku i postojeće porudžbine/profil. Stari profili do v8 dobijaju početne nule. Starije verzije igre ne mogu da učitaju profil v9; zaštita postojećeg spremišta sprečava prepisivanje novijeg formata.
- Hod/trčanje se mere iz horizontalnog pomeranja neposredno pre i posle `CharacterController.Move`. Teleport i bolnica ne ulaze u razmak između uzoraka.
- Aktivno vreme i niz preživljavanja napreduju dok je svet spreman, lik napravljen i živ, bez pauze/uvoda. Smrt prekida trenutni niz.
- Telefon: P → MOJA STATISTIKA → NAZAD U TELEFON / ESC. Isti postojeći sistem pauze i tipografije.
- Brojači novca, ishoda misija, hapšenja, bekstava i vožnje postoje kao priprema u preuzetom modelu podataka, ali u ovoj celini nisu povezani niti prikazani kao potvrđena statistika. Nema naknadnog izmišljanja istorijskih podataka.

## Šta ostaje kandidat

Pešaci, dodatna minimapa, skejt i efekti ekrana iz `fcfd5b8` nisu automatski uključeni. Skejt zahteva stvarno pomeranje i skok, pešaci vidljive modele i prohodne putanje, a povratne informacije razlikovanje smrti i hapšenja i pravilno odjavljivanje događaja. Minimap mora da poštuje nove ciljeve iz bašte i postojeći raspored komandi.

## Sledeći potezi

1. Kvalitetniji 3D model glave/tela, zatim kompatibilan adapter rig-a; ograničeni oslonac stopala i hvat/gard već su obrađeni.
2. Potpuna vožnja skejta, fizičko preuzimanje i bezbedan silazak.
3. Vidljivi pešaci, prepreke i reakcija na zvuk/opasnost.
4. Mini-mapa sa ispravnim smerom i svim aktivnim ciljevima.
5. Jedan prepoznatljiv centralni blok i provereni reljef.
6. Prirodne srpske replike, zvučne podloge i povezan zadatak sa odvlačenjem pažnje.

Dokazi ove celine: [tok i granice](Docs/Player-Stats-20261021.md), [izveštaj provera](Docs/Verification-Player-Stats-20261021.json). 222 uspešna testa, 0 neuspešnih, 1 preskočen. Završni Windows prolazi sa tastaturom i rasporedom za dodir su prošli; telefon se u ovoj iteraciji ne koristi. Ovaj handoff ne zamenjuje izlaz testova, izveštaj iz igre ili proveru uređaja.
