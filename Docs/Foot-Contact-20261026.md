# Oslonac stopala: ograničena korekcija postojećeg riga

## Ugovor i postojeća arhitektura

Osnova je commit `53d6154`: suprotno upareni udovi, faza prema pređenom putu, smirivanje pri stajanju, reset pri teleportu i očuvanje poze tokom pauze. Nova celina koristi isti normalizovani Kenney model, postojeći `CharacterController` i `CityActorMotion`. Profil v9, novčanik, misije i autoritet kretanja ne prelaze u animaciju.

`CityFootContact` kalibriše orijentaciju cipele i visinu skočnog zgloba iz stojeće poze prema osnovi igrača. Poza modela nije standardno Y-up u lokalnim osama stopala. Korekcija koristi dve postojeće kosti noge, bez menjanja root položaja, skale ili dužine segmenata.

## Granice korekcije

- Uključuje je samo `CityCameraRig` za prikaz igrača. Policija i ostali korisnici `CityActorMotion.Initialize` ostaju bez tog uključivanja.
- Upiti rade samo kada je kontroler omogućen i oslonjen na podlogu, bez boksa, teleporta ili pauze. Benč ima svoj zaseban model i solver.
- Statička podloga: najbliži rezultat dva ograničena `RaycastNonAlloc` upita. Sopstveni kolajderi, trigger-i i svi Rigidbody oslonci se ignorišu. Pun bafer ne proizvodi pretpostavljeni kontakt.
- Najviše 20 cm podizanja, 15 cm bočne korekcije i nagib do 40°. Nedostižan cilj i nevažeći brojevi odbacuju se pre menjanja rotacija.
- Podignuta noga se ne vuče nadole. Ovo nije zaključavanje stopala za svet, prenos težine karlice, animacija penjanja ili puna simulacija stopala na pokretnim platformama.
- Keševi animacije sada obuhvataju 12 zglobova, uključujući oba stopala. Nema novog pravljenja nizova, pretraživanja riga ili pečenja mesh-a u svakom kadru.

## Dokaz i provere

Prva fokusirana provera, pre implementacije, imala je tri očekivana pada: levi/desni oslonac na ivičnjaku i orijentacija na nagibu. Dodatna provera je zatim reprodukovala prelivanje dužine oštećenog segmenta; dodata je zaštita pre quaternion proračuna.

Četrnaest novih testova proverava stvarni normalizovani rig na 12 cm podignutoj podlozi, nagib od 20°, očuvanje dužina/root položaja, swing nogu, nebezbedne normale, nedostižan cilj, prelivanje dužine, odsutnu podlogu, sopstvene kolajdere, trigger-e, Rigidbody i pun bafer. Provera cipela bira stvarne vertexe sa najmanje 75% težine stopala i ograničava prodiranje/lebdenje prema ravni. `BakeMesh` se koristi samo u EditMode testu.

Puna završna EditMode provera: **245 uspešnih testova, 0 neuspešnih, 1 preskočen** (246 ukupno). Preskočen je opcioni ArcGIS test. Windows build je završen sa 0 grešaka. Desktop i touch-preview tokovi prošli su sa izlazom 0; detaljne vrednosti su u [evidenciji](Verification-Foot-Contact-20261026.json).

Prva Windows proba otkrila je 0 kontakata na kosini iako je kontroler bio grounded. Dva nova testa sa stvarnom kapsulom reprodukovala su pad. Kapsula drži fizički koren oko 8,7 cm iznad podloge. `CityGroundingOffset` zato postepeno usklađuje samo vizuelni wrapper sa najbližom statičkom ravni, najviše ±12 cm. Autoritet kretanja, root, kamera i statistika nisu pomereni. Korekcija se zamrzava u pauzi, poništava pri teleportu/isključivanju i postepeno uklanja u vazduhu. Nema per-frame alokacija; tri ograničena upita ukupno za prikaz i oba stopala.

Test sa pravim kontrolerom sada proverava kosine od ±12°, geometriju cipela, nepromenjen fizički koren, pauzu i povratak prikaza. Native provera traži najmanje pet stvarnih kontaktnih kadrova i merljiv vizuelni pomeraj. Zadržani su svi raniji uslovi za hod, menije, benč, statistiku i jednokratnu isplatu. Fizički Android test je odložen na zahtev korisnika; APK kandidat od 24. oktobra prethodi ovoj poslednjoj popravci i nije novo završno izdanje.

Native razvojna proba koristi privremenu rampu od 12°, kontrolisano oslanjanje, otvaranje telefona i odvajanje od tla. Probni objekat se uklanja i igrač vraća na sačuvani položaj. Ta rampa ne postaje novi teren grada. Postojeći tok proverava i hod, telefon/mapu/muziku, benč, statistiku i porudžbinu sa jednom isplatom.

## Ograničenja i naredni koraci

Nisu napravljeni novi lice/model, pokretni oslonci, stepenišni traversal, novi glasovi ili DEM teren. Nema dokaza o svim mogućim neravnim površinama niti dugotrajnom FPS/memorijskom ponašanju. AiGad iz druge aplikacije nije kopiran ili ubačen u VKT.

1. Hvat šipke i ručni zglobovi, zatim gard/reakcije u boksu.
2. Namenski prelazak preko ivičnjaka i stepenika, uz karlicu i reakciju celog tela.
3. Skejt sa preuzimanjem, kočenjem, skokom i bezbednim silaskom.
4. Vidljivi kontakti/pešaci i zadatak za odvlačenje pažnje.
5. Jedan prepoznatljiv centralni blok, proverene visine i zvuk koraka prema podlozi.
