# Ljudski lik u gradu — 1. novembar 2026.

Ova dorada povezuje pregledani MPFB model sa fizičkim igračem kroz postojeći sistem kretanja. Završna provera: **269 uspešnih Unity testova, 0 neuspešnih, 1 opcioni skip; Windows build i native tok prošli izlazom 0**. Native provera meri stvarne MPFB kosti, odvojeno poredi hod/sprint na čistoj ravnoj podlozi i proverava trening fallback. [Završna evidencija](Verification-Human-Playable-20261101.json) čuva rezultate; [snimak u gradu](Screenshots/human-city-windows-20261101.png) prikazuje stvarni Windows render.

Otvori **L → ODEĆA / GARDEROBER → NOVI 3D LIK / PREGLED → IGRAJ U GRADU**. Izbor zatvara garderober i postavlja kameru iza novog lika. **WASD** pomera igrača, **Shift** uključuje trčanje, a **C** menja prvo lice, pogled preko ramena i kameru iza lika. U prvom licu telo se skriva. Za povratak otvori garderober, pređi na prikaz originalnog lika ako je potrebno i izaberi **VRATI ORIGINALNI LIK**.

Izbor važi za tekuću sesiju. Profil v9 nema novo polje avatara; novac, XP, inventar i misije zadržavaju postojeće vlasnike. Garderober prikazuje da odeća i izrazi originalnog modela pripadaju originalu. Odevni izbori novog ljudskog modela još nisu povezani.

`CityCameraRig` poseduje lenjo kreiran i ponovo korišćen `CityHumanActor`. Taj adapter je obična klasa za prikaz: čita stvarno pomeranje koje je već razrešio postojeći `CharacterController`, bez novog kontrolera, unosa ili isplata. `CityHumanPose.StepObserved` koristi isti `CityGaitState` i postojeće funkcije poza. Guranje u zid ne sme da napreduje fazu koraka kada se kapsula ne pomera. Pauza treba da sačuva prikazanu pozu; teleport, skrivanje i ponovno uključivanje zahtevaju kontrolisan reset faze i oslonca.

Priprema modela je zajednička kroz `CityCharacterModel.TryPrepareHuman`. Gradski prikaz koristi **layer 0**, dok izdvojeni studijski pregled koristi **layer 30**. Izvorni materijali ostaju materijali MPFB modela; model ne prolazi kroz Kenney promenu građe ili atlas odeće. Uvezeni model ne sme da doda ponašanja, kameru, AudioListener, Rigidbody ili kolizije.

Za oslonac se ponovo koriste `CityGroundingOffset` i `CityFootContact`, uz kalibraciju novog riga posle normalizacije. Solver ograničava podršku na statične podloge do 40° i odbija samog igrača, triggere, Rigidbody podloge, nedostižan kontakt i zasićen skup pogodaka. To su granice algoritma; **podrška konkretnog MPFB đona mora biti potvrđena merenjem stvarnih deformisanih vertexa**. Prisustvo komponente ili broj podržanih kostiju nije dovoljan dokaz za stepenice, kosine ili pokretne platforme.

Benč koristi postojeći zaseban trening-model. Boks koristi postojeći provereni rig. Tokom oba treninga ljudski prikaz u gradu se skriva; po završetku ili prekidu kamera treba da vrati izabrani ljudski lik. Time se ne tvrdi da novi skelet već ima hvat šipke, zatvorene pesnice ili precizan kontakt sa vrećom.

Prihvatne provere ove celine:

- Izabrati novi lik stvarnim dugmetom garderobera; proveriti jednu fizičku kapsulu, isti broj kamera/listenera i ponovno korišćenje istog modela.
- Hodom i sprintom pomeriti fizičkog igrača i meriti baš MPFB kolena/ruke kroz više kadrova. Posebno proveriti zaustavljanje i guranje u prepreku.
- Otvoriti telefon/mapu, izmeriti stabilnost poze, zatvoriti meni i potvrditi vraćenu kontrolu. Teleport proveriti i tokom pauze.
- Proći sve tri perspektive bez duplog vidljivog tela, zaostalog prikaza ili dodatne kamere.
- Na ravnoj podlozi izmeriti položaj deformisanih đonova prema stvarnoj koliziji. Kosine i ostale podloge voditi kao neproverene dok nemaju zaseban dokaz.
- Završiti ili prekinuti benč i boks, proveriti trening-prikaz, jednu nagradu i vraćanje odabranog lika. Za vreme selekcije ne menjati profil osim dozvoljenog postojećeg UTC napretka bašte; stvarni hod legitimno menja statistiku i misijsko vreme.

Fizički Android/iPhone, ljudska ocena glasova i dugotrajno profilisanje nisu potvrđeni ovom dokumentacijom. [Plan za iPhone 13+](iPhone-Plan-20261101.md) ostaje prethodno definisan: iOS modul, odgovarajući Mac/Xcode ili podešen cloud build, potpisivanje i provera na uređaju. Windows render i simulirani dodir nisu iOS test.

Narednih pet prioriteta posle prihvatanja ove celine:

1. Kalibrisati stopala i karlicu novog riga na kosinama, ivičnjacima i stepenicama, uz merenje đona.
2. Preneti benč i boks na novi skelet: prsti, hvat, zglobovi i reakcija vreće.
3. Dodati pogled, kapke, mimiku i vizeme uz pregledane srpske glasove.
4. Povezati vidljive kontakte, misije i policijske posledice u jedinstven tok kroz grad.
5. Uvesti LOD i mobilni grafički profil, zatim duži Windows test i stvarnu proveru uređaja.
