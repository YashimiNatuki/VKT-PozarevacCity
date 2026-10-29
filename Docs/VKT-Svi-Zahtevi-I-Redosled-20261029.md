# VKT — svi zahtevi, stvarno stanje i redosled rada

Datum: 29. oktobar 2026. Aktivni izvor: `C:\Users\jamal\Documents\VKT-PozarevacCity`.

Ovaj dokument sabira zahteve iz razgovora od početka razvoja. **POSTOJI** znači da postoje implementacija i ranija navedena provera. **DELIMIČNO** znači da podsistemi postoje, ali cela zamišljena funkcija nije isporučena. **SLEDEĆE** znači plan ili kandidat. Raniji test nije automatski dokaz kvaliteta novog paketa.

## Tačka nastavka posle prekida

Poslednje objavljeno izdanje pre ove dorade je `6e3f027`, Windows Hand Contact od 28. oktobra: šake na šipci, stabilniji boksački gard, 255 uspešnih testova i jedan opcioni preskočen. U njemu je nova tridesetosekundna **remontaža postojećeg materijala**. Nije napravljen novi realistični AI film.

Prekid je ostavio nedovršen pregled ljudskog 3D modela u garderoberu. Sadašnja celina uvodi zaseban MPFB/MakeHuman kandidat, PBR materijale i pregled sa svih strana. To **još nije novi igrivi avatar**, facijalna animacija, niti zamena za NPC. Tačne završne provere vode se u izveštaju ove isporuke, ne u obećanjima plana.

Windows je prva platforma. Telefon sada nije povezan. Starija korisnička potvrda dodira i zvuka uvoda na Androidu ostaje važeća samo za tadašnji paket; novu verziju ne proglašavamo testiranom na telefonu.

## 1. Početak, telefon i korisnički interfejs

**POSTOJI:** uvod, kreiranje lika, pomoć, novac i osnovni HUD, telefon, mapa, tri perspektive, kompaktni muzički plejer i povratak u igru. Putanja dokaza: `Gameplay-Recovery-20261020.md`, `Camera-Perspectives-20261016.md`, `Verification-Hand-Contact-20261028.json`.

**SLEDEĆE:** jedan jasan prvi dan: kreiranje → kontrola hoda/kamere → objašnjenje novca, života, hrane, reputacije i potere → telefon/kontakt → prva kratka misija → štek → trening. Objašnjenja prikazati u trenutku potrebe, sa mogućnošću preskakanja i ponovnog otvaranja. Pomoć mora navoditi stvarna dugmad, uključujući P, M, F1 i F6. Ne uvoditi nove paralelne prozore koji ostavljaju pauzu ili fokus zaglavljenim.

Unaprediti tipografiju, kontrast, veličinu ciljeva za dodir, navigaciju tastaturom, rezolucije i povratak pomoću ESC. Po-Oglasi, foto-reputacija, korisnički GPS waypoint i dodatni radar ostaju zasebne funkcije; nisu završeni samim postojanjem mape.

**Prihvatanje:** od novog profila do prve isplate bez konzole; deset otvaranja/zatvaranja telefona, mape, garderobera i muzike bez gubitka kontrole. Muzika ne prekida misiju.

## 2. Lik, telo, odeća i kontakt

**POSTOJI:** stilizovani Kenney avatar, tri izgleda, promena građe kroz trening, osnovni izrazi/treptanje i ograničen morf vilice; koordinacija suprotne ruke i noge, ograničeni oslonac stopala, hvat šipke i boksački gard.

**DELIMIČNO:** novi ljudski kandidat ima stvarnu geometriju tela, oči, obrve, kosu, teksture i skelet. Prvo ga ocenjujemo u odvojenom 3D pregledu. AiRođa/AiGad Noir iz druge aplikacije je kvalitetna 2D WebP vizuelna referenca, nema prenosiv kompletan skelet i 3D telo. Privatni materijali te aplikacije ostaju tamo.

**SLEDEĆE:** adapter novog skeleta na postojećeg vlasnika pokreta; idle, hod, trčanje, zaustavljanje, okretanje, benč i boks. Zatim kapci i očne jabučice, smer pogleda, obrve, obrazi, vilica, osmeh i vizeme za govor. Svaki novi rig mora očuvati stopala, šake i proporcije. Potrebni su LOD, izbor odeće bez probijanja tela i profilisanje skinned mesh renderera.

Dalje: reakcija na sudar, pad, udarac, povratak ravnoteže, nagib karlice na kosini, stepenice i pokretne podloge. Jedna kapsula/kontroler ostaje autoritet; dekorativna fizika ne sme gurati igrača kroz zid.

**Prihvatanje:** poređenje sprijeda/sa strane/otpozadi i krupan kadar bez skrivanja filterima; ista test ruta i trening sa oba riga; bez skaliranja kostiju koje kvari animaciju, klizanja ili proboja šipke.

## 3. Požarevac, reljef i proširenje sveta

**POSTOJI:** OSM ulice, lokalni nazivi, fasade, mapa i registar lokacija. **Nije završen** prepoznatljiv centralni blok niti stvarni provereni reljef. Trenutna OSM osnova je ravna; alat za audit visina ne uključuje teren automatski.

Zahtevani orijentiri: Načelstvo/opština, Gradski park, spomenik Milošu Obrenoviću, fontane i klupe, Korzo/Tabačka čaršija, Centar za kulturu, Narodni muzej, Čačalica, Ljubičevo i hipodrom. Zahtevani delovi i okolina: Tulba, Kruška brdo, Busije, Burjan, Sopot, Seovac, Radna mala, Trijangl, Meminac, Zabela, Kravlji do i ostali lokalni nazivi iz korisnikovog registra. Lokalni nadimak naselja sam po sebi nije proverena administrativna granica.

**SLEDEĆE:** prvo jedan pešački centralni blok sa merama, referencama, trotoarima, parkovskim stazama, kioskom, svetlima i kvalitetnim kolizijama. Zatim mali visinski sektor Čačalice/Tulbe. Pinovi iz razgovora su kandidati: koordinata koja je nazvana Ljubičevo/Hipodrom ne sme se preuzeti kao proverena tačka bez kontrole izvora. Koristiti jedan WGS84 origin i doslednu konverziju u metre; lat/lon nisu Unity X/Y koordinate za culling. Culling granice računati iz geometrije sektora.

DEM/DSM: sačuvati izvor, licencu, rezoluciju, vertikalni datum i kontrolne tačke. DSM krov nije visina ulice. Nivelisati raskrsnice i prolaze, povezati kolizije i navigaciju; tek zatim uključiti streaming, LOD i occlusion. Cesium, Mapbox, TomTom ili satelitski servis nisu obavezni ako ne donose konkretno licencirano poboljšanje. Ne kopirati tuđe fotografije/pločice bez prava redistribucije.

**Kasnije:** drugi gradovi u Srbiji kroz isti sistem sektora, autobus/voz, kratka tranzicija i lokalne misije; izbor početnog grada tek kada Požarevac ima kompletan igrivi krug.

## 4. Priča, kontakti, misije i posledice

**POSTOJI:** kampanja, ulični poslovi, sačuvane aktivne isporuke i profil v9, porudžbina iz šteka sa potvrdom i jednom isplatom. Postojeće oznake kontakata nisu dokaz punog vidljivog NPC-a. Izvor: `Mission-System.md`, `Garden-Orders-20261020.md`, `Player-Stats-20261021.md`.

**SLEDEĆE:** autorski prvi lanac Rođa → Kure → povratak u štek. Svaka faza ima razlog, cilj, reakciju sveta, uslove neuspeha i jasno sledeće odredište. Dodati vidljive kontakte i obične prolaznike sa prohodnim rutama. Zvižduk, dobacivanje ili provokacija treba da promene ponašanje konkretnog NPC-a; kasnija misija proverava taj događaj, a ne samo približavanje markeru.

Korisnikova ekipa i porodični likovi: Rođa, Špaciren, Goran, Zoran, Dragan, Životinja, Kure, Gogaš, Dirigent, Milica, keva/baba i drugi nadimci mogu postati fikcionalni odrasli likovi sa odvojenim ulogama. Sleng i humor ostaju, ali identitet i glas stvarne osobe zahtevaju odgovarajuću dozvolu. Romantične epizode planirati kao dobrovoljan flert i komične nesporazume; informacija/izlaz iz misije ne uslovljavaju se seksualnom prinudom.

**Prihvatanje:** save/load u svakoj fazi, otkaz, smrt, hapšenje, istekao rok i ponovljen pokušaj naplate; najviše jedna isplata. Kontakti daju razumljiv sledeći zadatak, dnevnik odgovara stvarnom stanju.

## 5. Policija, civili i nepredviđeni događaji

**POSTOJI:** stanja patrola/istraga/potera/pretraga, poslednje poznato mesto, ograničen sluh/zvižduk sa zaklonom i cooldown-om. Potpuni raspored gradskih NPC-a i saobraćaja nije potvrđen.

**SLEDEĆE:** svedok → prijava → mesto događaja → potraga → smirivanje. Policija ne zna skriven položaj igrača. Kontakt, vidljivost, zvuk i reputacija imaju razdvojene posledice. NPC dnevne rutine i reakcije imaju ograničen budžet, pooling i udaljenu jednostavniju simulaciju.

Ideje ostaju u planu: komični susreti sa babom, grupama prolaznika, psima i rivalskom ekipom; jasno upozorenje, mogućnost izbegavanja, primerena reakcija i oporavak. Retka iznenađenja ne prekidaju onboarding ili ključnu transakciju bez pravila.

## 6. Štek, bašta, katalog i ekonomija igre

**POSTOJI:** osam strejnova u katalogu, cene/otključavanje, kartice biljaka, tržište i porudžbina koja povezuje zalihu, kontakt, potvrdu i jednokratnu nagradu. Ovo je **apstraktna simulacija igre**, ne uputstvo za stvarno gajenje ili prodaju.

**SLEDEĆE:** pregledniji status svake sadnice, redosled radnji, cena pre potvrde, razvoj prostora garaža/podrum/štek, skladište, oprema i otključavanje kroz misije. Biljke sa nazivima/poreklom/istorijom moraju imati izvore; slike, 3D modeli i animacije imaju licencu i ne predstavljaju pogrešnu vrstu. Proširenje sa 8 na više vrsta ne sme biti samo dupliranje parametara.

Tržište dobija čitljive razlike po kontaktu, delu grada, potražnji i kvalitetu. Cena za već prihvaćenu porudžbinu ostaje ugovorena. Offline vreme je ograničeno i otporno na promenu sata; učitavanje ne duplira žetvu, predmete ili nagradu. Atmosfera šteka uključuje zaseban tih instrumental i kontrolu jačine.

## 7. Razvoj igrača, hrana i treninzi

**POSTOJI:** život/šteta, hrana/energija, snaga, izdržljivost, XP, benč, boks, kondicioni zadaci, trajna statistika i odvojene nagrade. Benč ima pun ciklus ponavljanja, ne nagradu na svaki klik.

**SLEDEĆE:** udarac u vreću sa potvrđenim kontaktom, reakcijom vreće i zvukom; gard/reagovanje na signal; čitljiva kondicija i oporavak kroz dan. Trening treba da koristi kratku veštinu/tajming, a ne beskonačno nasumično pritiskanje. Telefon: velik cilj i odgovarajući gest, provereni na uređaju.

Hrana i krađa hrane ostaju sistem igre sa posledicama i mogućnošću bekstva. XP, novac i reputacija imaju jasne odvojene namene. Ne dodavati drugi zapis profila radi nove mini-igre.

## 8. Prevoz, automobili i krađa točkova

**SLEDEĆE / KANDIDATI:** skejt, dodatna minimapa i pešaci iz Antigravity checkout-a nisu automatski integrisani. Prvi kompletan prevoz je skejt: pickup/mount na E, ubrzanje, kočenje, ollie na Space, silazak/pad i dostava. Proveriti zidove, kosine, kontrolu kamere, pauzu i statistiku distance. Zatim trotinet i bicikl preko istog ugovora kretanja.

Vozni park iz blueprint-a: Yugo/Koral, Golf II, Stojadin, policijska Škoda/Dacia i FAP/šlep. Potrebni su model sa pravima, ulazak/izlazak, točkovi/ogibljenje, kočenje, zvuk, svetla, NPC saobraćaj, raskrsnice i reakcija policije. NavMesh sam po sebi ne rešava prvenstvo prolaza.

Tematska mini-igra krađe točkova je fikcionalna mehanika sa animacijom, trajanjem, patrolnim rizikom i prodajom kod kontakta; nije završena. Slingshot/vodeni pištolj, petarde i ekstremna vozila ostaju kasniji arkadni sadržaj. Postojeći kamen/prozor ne predstavljaju kompletan borbeni sistem.

## 9. Audio, glasovi i radio

**POSTOJI:** prostorni zvuk, zvučni zaklon, koraci prema podlozi, zvižduk, signalni efekti, lični plejer i instrumental šteka. Audio test potvrđuje događaj/jačinu/pauzu, ne prirodan srpski izgovor.

**NEDOSTAJE:** prihvaćena banka 20–30 prirodnih srpskih replika za više likova. Piper uzorak korisniku zvuči robotski; VITS uzorak nije poslušan/odobren. Raniji problematični Coqui/VITS put ne uključivati ponovo bez kompatibilne bezbednosne provere; postojeće modele i WAV čuvati. Novi nalaz bezbednosti mora biti svež, ne nasleđen iz starog izveštaja.

**SLEDEĆE:** kratki uzorci → ljudsko slušanje → potpisane/odobrene replike → titlovi → prostorni izvor → ducking muzike → sinhronizacija usta. Saobraćaj dobija motor prema brzini/opterećenju, kočnice, sirenu i prolazak tek uz stvarna vozila. Pešaci imaju ograničen broj dobacivanja, pauzu i varijacije, bez beskonačnog preklapanja.

Radio Po-Blok, Morava Narodni i Kafana Chill su kreativni predlozi stanica; nisu potvrđene licencirane emiterske frekvencije. Za paket koristiti odobrenu muziku, originalne beatove ili dozvoljene stream-ove sa offline rezervom. Lokalna lična muzika ostaje privatna; ne gurati korisnikovu kolekciju na GitHub i ne izvlačiti tuđi glas radi imitacije bez dozvole.

## 10. AI, objekti koji pričaju i transformišuća klupa

**POSTOJI:** lokalni AI put sa proverom dostupnosti i autorskim rezervnim replikama. AI ne dodeljuje novac niti određuje autoritativan ishod misije.

**SLEDEĆE:** jasni statusi lokalno/spremno/nedostupno, vremensko ograničenje, otkaz pri zatvaranju, red zahteva, cache i mali kontekst po liku. Opcioni sopstveni API ključ ostaje van repozitorijuma i distributivnog paketa. Android ne pristupa automatski desktop Ollami; potreban je eksplicitan privatni povezani uređaj ili autorski offline odgovor.

„Optimistična klupa” može imati kratko retko transformisanje, repliku i vraćanje u klupu. Prvo autorska state machine, tek zatim opciona AI replika. Zabranjen okidač usred treninga/isplate; pauza, save/load i nestanak modela imaju bezbedan povratak. Isto pravilo važi za duhovite pse ili predmete: vizuelna reakcija ne preuzima kontroler igre.

## 11. Špica i identitet igre

**POSTOJI:** 30-sekundna nova remontaža integrisana u Windows paket od 28. oktobra; original sačuvan; prirodan kraj i preskakanje provereni.

**SLEDEĆE:** stvarno novi kadrovi sa doslednim junakom, centrom Požarevca, znakom „Velika Krađa Točkova”, „Po-Blok C-presentier”, kratkom pričom i originalnom muzikom. Prvo storyboard od 6–8 kadrova i referentni izgled, zatim kratka proba video generatora ili Blender/Unity sekvenca. Gemini Pro pretplata sama po sebi nije dokaz da dostupni API ovde ima video pravo ili kredit. Ne obećavati gotov novi AI film dok nemamo izlazni fajl, pregled i pravila korišćenja.

## 12. Platforme, performanse i isporuka

**POSTOJI:** Windows native paket i automatizovani tokovi. **ODLOŽENO:** fizički Android test novog sadržaja. Android: ARM64 APK, dodir, audio izlaz, FPS/temperatura, zaključavanje ekrana, povratak aplikacije i save/load. Windows touch-preview nije zamena.

iPhone je moguć budući cilj Unity projekta, ali zahteva kompatibilan iOS izvoz, macOS/Xcode, potpisivanje i stvaran uređaj. Nije dovoljan APK, Windows build ili PWA omot oko nativne igre.

Pre nove grafičke granice: ruta od bar 20 minuta, hardver u izveštaju, frame-time p50/p95/p99, vršna memorija, broj NPC-a i geometrije. Stabilnost proveravati kroz početak → misiju → pauzu → štek → trening → učitavanje → isplatu. Svaka isporuka ima commit, tačne testove, build, hash paketa i granice provere. Nema garantovanja „zero error” samo na osnovu uspešnog build-a.

## 13. Alati i odvojeni zahtevi

Za aktuelni 3D korak koriste se Blender/MPFB, Unity, C# testovi, lokalni renderer i Git. Aktiviranje svih konektora nije cilj. Cinemachine, Splines, AI Navigation, DOTween ili drugi paket uvode se samo kada zatvaraju konkretan nedostatak i imaju kompatibilnu licencu/verziju. Postojeći Unity projekat se ne migrira u Unreal ili web engine samo zato što je konektor dostupan.

GitHub pregled naloga/organizacija, sređivanje duplikata i Rođa/AIBrat/Konglomerat plugin/skill paketi su odvojeni prethodni zahtevi. Ne brišemo slične projekte zato što liče na duplikate. Dokument o aplikaciji od A do Š već postoji (`VKT-A-DO-SH-20261018.txt`); ovaj novi registar ažurira obim i prioritete. VKT ostaje glavni posao.

## Narednih osam isporučivih koraka

1. **Završiti pregled ljudskog kandidata:** model/teksture/licence → garderober → poređenje lica/tela → povratak kontrole → Windows dokaz. Igrivi model ostaje očuvan.
2. **Uvesti novi rig u igru:** mapiranje kostiju i materijala → hod/idle → stopala → benč/boks → odeća. Tek posle regresije ponuditi izbor avatara.
3. **Povezati prvu celu priču:** vidljivi Rođa/Kure, porudžbina, odvlačenje pažnje, posledice policije, sačuvane faze i jedna isplata.
4. **Napraviti centralni blok:** prepoznatljiv park/Načelstvo/korzo, proverene proporcije i prohodnost; zatim mali dokumentovan sektor visina.
5. **Prihvatiti glasove i završiti miks:** 3 probna glasa, ljudsko slušanje, 20–30 replika, titlovi, ducking i kontekstualni ambijent.
6. **Isporučiti skejt:** ceo ulazak/vožnja/skok/silazak sa jednom misijom; potom trotinet/bicikl i vozni park.
7. **Produbiti štek i dnevni ritam:** oprema, porudžbine, tržište, trening/ishrana, uz jednu retku autorsku „optimističnu klupu”.
8. **Profilisati i prilagoditi uređajima:** duga Windows ruta, novi APK i fizički Android test; iOS kao zasebna izvozna celina.

Zahtev se zatvara kada su kod, sadržaj i stvarni tok povezani. Prisustvo fajla, instaliranog paketa ili tuđeg handoff-a nije dovoljno.
