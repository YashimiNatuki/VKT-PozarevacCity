# VKT: nastavak rada, 2026-10-29

## Najnovija dorada: ljudski 3D kandidat i objedinjen plan

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
