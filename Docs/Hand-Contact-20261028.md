# Šake, benč i boks — 28. oktobar 2026.

## Problem i promena

Ranije je zglob šake stajao gotovo u osi šipke (reprodukcija: oko 0,00000195 m od ose), dok prsti nisu zatvarali hvat. `CityHandPose` jednom pronalazi kosti postojećeg Kenney modela i određuje pravac dlana, prstiju i tačku oslonca. `CityBenchVisual` sada rešava položaj zgloba iz tačke hvata, zatvara prste i proverava razmak od šipke. Koristi postojeći solver ruke i ne menja pravila treninga.

U boksu šake prate podlaktice, prsti se zatvaraju u gard i udarac, a položaj dlana se menja neprekidno. Obrada se izvršava nakon završnog pokreta grudnog koša. `CityActorMotion` čuva i vraća 24 kosti, uključujući obe šake i prste, u postojećem ciklusu animacije i pauze. Profil ostaje v9; nagrade i trajanje treninga zadržavaju iste vlasnike.

## Provera

Deset novih EditMode slučajeva koristi stvarni model i njegove kosti: ceo hod šipke na okrenutoj klupi, početnu i maksimalnu snagu, zatvoren gard, odvojen udarac i gard, kontinuitet rotacije kroz 201 uzorak, ponovljenu pozu, resamplovanje animacije i vlasništvo nad pauzom/isključivanjem. Nema novog sistema kretanja ili novčanika.

Native test izvodi osam ponavljanja benča i osam reakcija boksa, proverava +30 XP samo jednom, vraćanje kretanja, kontakt šaka, pauzu i postojeće tokove telefona, mape, muzike i porudžbine. Prva proba je otkrila grešku u samom uzorkovanju pauze: poređena je međufaza pre `LateUpdate` sa završenom prikazanom pozom. Provera sada uzorkuje kraj kadra pre i posle pauze, kao postojeći testovi hoda; prag od 0,1° nije oslabljen.

Tačni završni rezultati i granice su u [izveštaju](Verification-Hand-Contact-20261028.json). Izlaz pojedinačnog testa ili builda nije dokaz fizičkog telefonskog testa, ljudske procene zvuka ili dugotrajnog profilisanja.

## Granice

- Ovo je unapređenje postojećeg stilizovanog modela, a ne novi realističan lik.
- Kalibracija šaka namenjena je postojećem Kenney rig-u; proizvoljni drugi modeli nisu provereni.
- Gard i udarac su animacije treninga. Nije uveden fizički kontakt pesnice sa vrećom, deformacija vreće ili borbeni damage sistem.
- Postojeći testovi proveravaju ograničene statičke oslonce stopala; puna obrada stepenica, pokretnih platformi i prenosa težine karlice ostaje zaseban korak.
- Android/iPhone i dugotrajna stabilnost nisu potvrđeni ovom Windows isporukom.

## Nova špica

Windows paket ovog koraka koristi tridesetosekundnu remontažu sa novim VKT / Velika Krađa Točkova / Požarevac / Po-Blok naslovima. To su postojeći kadrovi sa novom montažom i ujednačenim zvukom, ne novi AI-generisani kadrovi. Originalni lokalni MP4 ostaje sačuvan. [Integracija i poreklo](Intro-Integration-20261028.md).

## Sledeći vizuelni korak

Potrebna je kvalitetnija 3D glava i telo sa rig-om, materijalima i blendshape izrazima. AiRođa/AiGad Noir može da vodi izgled, ali pregledani renderer prikazuje 2D WebP sa ambijentalnim efektima; on nije 3D model za hod, različite kamere i mimiku. [Konsolidovan pregled i plan](VKT-Stanje-I-Sledeci-Potezi-20261028.md) razdvaja gotov kod od kandidata i budućih zadataka.
