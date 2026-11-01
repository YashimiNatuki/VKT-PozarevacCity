# Šake, gard i udarac ljudskog lika — 1. novembar 2026.

Novi ljudski model sada pokreće svih **30 zglobova prstiju**: pet nezavisnih prstiju sa tri zgloba na obe šake. Miran položaj i hod imaju blagu opuštenu fleksiju; gard zatvara pesnice, poravnava ručne zglobove i okreće dlanove ka telu. Deo aksijalnog okreta prenosi se na podlakticu. Nisu dodavani novi modeli, teksture ili biblioteke.

**Provere: 8/8 ciljanih testova; ceo Unity skup 290 uspešnih, 0 neuspešnih, 1 opcioni ArcGIS preskočen.** Windows build, native šake, lice, hod/kamere, garderober, desktop/touch-preview i oba toka prethodne špice imaju izlaz 0. [Završna evidencija](Verification-Human-Hands-20261101.json) razdvaja te dokaze od fizičkog telefona i ljudske estetske ocene.

## Kako se koristi

**L → ODEĆA/GARDEROBER → NOVI 3D LIK/PREGLED → POKRET/GARD** prikazuje nove pesnice. **IGRAJ U GRADU** bira model za tekuću sesiju. Kada su izabrane **PESNICE**, postojeći **LMB / NAPAD** pokreće desni udarac i zaštitnu levu ruku. Q menja dostupnu opremu; šake se vraćaju u opušten položaj posle udarca. Telefon/pauza zamrzava prikaz i blokira novi napad. Teleport uklanja prethodnu borbenu pozu. Izbor modela ostaje sesijski; profil ostaje v9.

## Arhitektura i stvarni dokazi

`CityHumanHandPose` validira jedinstvene, kompletne lance svih pet prstiju i kešira rest rotacije i ose fleksije. Smer se izvodi iz neutralne zakrivljenosti postojećeg riga, bez pretpostavke o znaku FBX ose. `CityHumanPose` ponovo koristi postojeće `CityGaitState`, `RelaxArm` i `PoseBoxingArm`; eksplicitni garderober gard ostaje neposredan, a borbeni ulaz/izlaz se blenduje. Nepotpun finger rig isključuje celu adaptaciju šaka i ostavlja hod dostupan.

`CityCombatRuntime.UnarmedPunch` je samo čitljiv signal već prihvaćenog napada i njegovog postojećeg vizuelnog sata. Izbor druge opreme ne izmišlja novi udarac. Bootstrap povezuje postojeći combat sa kamerom, a kamera prosleđuje signal ljudskom prikazu. Nisu uvedeni novi vlasnici inputa, fizičkog kontrolera, pogotka, zdravlja, cooldown-a, nagrade ili snimanja.

Provera zatvaranja koristi proxy kraja treće falange u odnosu na bazu prsta i smer dlana, sa dozvoljenim opsegom od -5 do 28 mm. Precizan kontakt površine kože i predmeta zahteva zasebne kontaktne podatke i pregled.

Sačuvani RED dokazi reprodukovali su otvorene prste, pogrešno okrenute dlanove, regresiju prvog garda, zadržanu pozu posle teleporta i odsutan prikaz stvarnog udarca. Završna native provera meri trideset zglobova, deformisani mesh u istom kadru, obe šake, desnu podlakticu, cooldown, pauzu, oporavak i nepromenjene vlasnike/profil. Poređenje profila radi se neposredno oko izbora, pre nego što ponovo pokrenuti svet opravdano napreduje.

## Pregled stvarnog Windows rendera

Asistent je pregledao sledeće snimke; korisnikova estetska potvrda nije dobijena.

- [human-hands-guard-front-windows-20261101.png](Screenshots/human-hands-guard-front-windows-20261101.png)
- [human-hands-left-windows-20261101.png](Screenshots/human-hands-left-windows-20261101.png)
- [human-hands-right-windows-20261101.png](Screenshots/human-hands-right-windows-20261101.png)
- [human-hands-world-attack-windows-20261101.png](Screenshots/human-hands-world-attack-windows-20261101.png)

## Granice i sledeći koraci

Ovo je prezentacija šaka i postojećeg borbenog napada, bez novih kontaktnih hitbox-ova ili anatomskog motion-capture-a. Benč i boks još koriste prethodne trening-modele. Držanje telefona, precizan hvat šipke/predmeta, puna animacija nove ljudske trening-poze, dug FPS/termalni test i fizički Android/iPhone nisu potvrđeni. Nova AI špica i novi glasovi nisu dodati.

Slede: precizan kontakt šake sa telefonom i predmetom; benč/boks novog riga; kosine i stepenice; kvalitet centralnog bloka Požarevca; glas/vizemi; povezane misije i posledice. [Širi plan sa 18 stavki](Human-Face-20261101.md) i [prethodno lice/pogled](Human-Face-20261101.md) ostaju kontekst. Postojeći [CC0 model i targeti](Faceunits01-CC0-LICENSE.md) ostaju jedini izvor ljudskog asseta.
