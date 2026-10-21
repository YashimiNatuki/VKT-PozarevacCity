# VKT: nastavak rada, 2026-10-21

## Aktivni izvor i poreklo saveta

Aktivni projekat: `C:\Users\jamal\Documents\VKT-PozarevacCity`.
Predlog Antigravity saradnika i njegov izvorni handoff pročitani su u `C:\Users\jamal\Documents\antigravity\radiant-kepler\VKT-PozarevacCity`. Tamo su commit-i `fcfd5b8` i `53c10f1`, posle prebacivanja na bazu `75401b3`. Taj checkout je sačuvan; ovo nije tvrdnja da su svi njegovi dodaci funkcionalni u aktivnoj igri.

Plan: [Master Blueprint](Docs/VKT-Master-Upgrade-Blueprint.md).
Pregled rizika: [Integracija paralelnih dodataka](Docs/Blueprint-Integration-Review-20261021.md).

## Prva izdvojena celina: statistika

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

1. Hod, proporcije tela i kontakt ruku sa benčom.
2. Potpuna vožnja skejta, fizičko preuzimanje i bezbedan silazak.
3. Vidljivi pešaci, prepreke i reakcija na zvuk/opasnost.
4. Mini-mapa sa ispravnim smerom i svim aktivnim ciljevima.
5. Jedan prepoznatljiv centralni blok i provereni reljef.
6. Prirodne srpske replike, zvučne podloge i povezan zadatak sa odvlačenjem pažnje.

Dokazi ove celine: [tok i granice](Docs/Player-Stats-20261021.md), [izveštaj provera](Docs/Verification-Player-Stats-20261021.json). 222 uspešna testa, 0 neuspešnih, 1 preskočen. Završni Windows prolazi sa tastaturom i rasporedom za dodir su prošli; telefon se u ovoj iteraciji ne koristi. Ovaj handoff ne zamenjuje izlaz testova, izveštaj iz igre ili proveru uređaja.
