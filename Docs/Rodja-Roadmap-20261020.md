# Rođa: sledeći razvojni potezi

Master Blueprint je pročitan i prenet iz paralelnog checkout-a. [Provera stanja, rizici integracije i redosled narednih celina](Blueprint-Integration-Review-20261021.md) razlikuju planirane funkcije od potvrđene Windows isporuke.

Prioritet: Windows. Android podrška ostaje u istom Unity projektu; telefon se u ovoj iteraciji ne koristi. iOS je posebna naredna validacija, ne obećani gotov build.

## Pravac i granice

VKT je urbana akcija sa humorom, Požarevcem, kontaktima, treningom, štekom i posledicama odluka. Napredak treba da bude primetan kroz ono što igrač vidi i radi, a ne kroz broj instaliranih biblioteka. Postojeći ugovori, profil v8, katalog, muzika i originalna špica ostaju osnova. Ne menjati determinističko generisanje već prihvaćenih poslova: njihove sačuvane rute moraju ostati iste.

## 1. Ulični zvuk i akcija → reakcija

Prva implementaciona celina ove iteracije:

- G ili vidljivo ZVIŽDI: prostorni autorski zvižduk, domet 24 m, odmor 4 s.
- Neprekidan zid blokira signal. Puna physics bafer-lista odbija nejasan rezultat.
- Patrola bez potere istražuje poslednje mesto zvuka do 6 s. Ne dobija magično novi položaj igrača. Zvižduk nije krivično delo, ne stvara HEAT i ne briše poteru.
- Postojeći lokalni protivnici mogu da priđu zvuku unutar svog područja; normalni uslovi napada i dalje važe.
- Meni, smrt, uvod, štek i trening blokiraju novu akciju. Zvučni mute ne uklanja gameplay posledicu zvižduka.
- Štek dobija tih originalni instrumental; lični plejer ima prednost, uključujući njegovu pauziranu numeru.

Dokaz završetka: EditMode testovi sluha i audio signala; native Windows test tastature, zida, patrolne reakcije, cooldown-a, HEAT-a, povratka iz menija, muzike u šteku i mute-a. Ljudsko preslušavanje je zasebna provera kvaliteta.

Sledeći korak ove oblasti: autoritativan cilj misije „odvuci pažnju“, snimljene ulične replike sa titlovima i stvarno kretanje vozila sa zvukom vezanim za brzinu. Trenutni ambijentalni šum nije simulacija saobraćaja niti snimak Požarevca.

## 2. Štek, bašta i povezani poslovi

Isporučena prva povezana celina: pult PORUDŽBINE / KURE → 2 komada iz stvarnih zaliha → predaja kod markera 2 → potvrda → povratak na pult → jedna isplata. Prihvatanje troši robu; otkaz, smrt i istek je gube bez povraćaja i nagrade. Profil v8 čuva fazu, cenu i rok, uz podržan prelazak starih profila. Stare rute nisu menjane. [Arhitektura, tok i granice](Garden-Orders-20261020.md).

Sledeće: sažet pregled četiri saksije, suvih/spremnih mesta i skladišta, zatim dodatni autorski zadaci i namerno pripremljen susret za odvlačenje pažnje. Kure u ovoj celini koristi postojeći fizički marker, ne novi humanoidni model. Prirodni glas još nije dodat. Koristiti apstraktne parametre igre, bez realnih uputstava za uzgoj.

## 3. Lik, dodiri i trening

Redosled: proporcije i neutralna poza → stopala/kolena pri hodu → šake na šipci i položaj lopatica → gard/reakcija u boksu → kapci, pogled, obrve, vilica i mimika. Prvo otkloniti proklizavanje i međusobno prodiranje modela; onda detalji kože i odeće. Trenutni low-poly model nema resurse za fotorealističan rezultat samo dodatnim kodom.

Za novi model: licenca, prava na referentnu sliku, humanoidni rig, odgovarajući blendshape-ovi, LOD, collider i budžet materijala. Ne tvrditi da je model portret korisnika bez njegove odgovarajuće reference. Testirati hod, trčanje, promenu kamere, benč, boks, garderober i povratak iz svake animacije.

## 4. Centar i teren

Prvo jedan prepoznatljiv blok, pa širenje. Proveriti stvarne lokacije i nadmorske visine; korisničke koordinate su polazni podaci, ne potvrđena geodezija. Posebno razlikovati hipodrom i Ljubičevo. OSM ulice, prava na prikaze/objekte, provereni DEM uzorci, kolizije, nagibi, NavMesh i saobraćajni čvorovi imaju odvojene provere.

Isporuka: pešačka ruta kroz centar sa 2–3 orijentira, uverljivom skalom 1 Unity jedinica ≈ 1 m, bez propadanja i neprohodnih ivičnjaka. Tulba i Čačalica tek sa proverljivim izvorom reljefa. Naziv naselja u opisu misije ne znači da je naselje izgrađeno.

## 5. Kontakti, misije i posledice

Uvesti Rođu, Kureta, Gogaša i Dirigenta pojedinačno, sa različitim ciljevima, rasporedom i odnosom prema igraču. Ostala imena ostaju backlog dok ne dobiju ulogu. Porodične situacije i odnosi odraslih likova su dobrovoljni; tragovi i nagrade dolaze iz dijaloga i zadataka.

Prvi lanac: Rođin poziv → Kuretov izgubljeni predmet → skretanje pažnje → predaja u šteku → posledica za odnos/policiju. Svaki korak mora imati uslov, marker, otkaz, neuspeh i nastavak posle učitavanja. Ollama može menjati repliku, ali ne odlučuje o novcu, inventaru ili završetku misije. Autorski razgovor ostaje dostupan kada AI ne radi.

## 6. Početak, vizuelni identitet i špica

Urbani identitet od prvog ekrana: sažet naslov, jasna uputstva, čitljiv novac/život/cilj, kontakti kroz telefon, prijatne tranzicije i dosledna tipografija. Bez prekrivanja kretanja ili zamrzavanja zbog plejera.

Nova špica se pravi kao odvojen kandidat, ne automatska zamena originalnog snimka. Predlog kadrova: centralni blok u sumrak → točkovi i asfalt → Rođa dobija poziv → svetlo šteka → naslov Velika Krađa Točkova / Požarevac / Po-Blok C presents. Naslov montirati lokalno radi tačnog pisanja. Originalna muzika ili jasno dozvoljen snimak; bez imitiranja poznatih pesama.

Gemini pretplata ne znači da je API povezan sa ovim projektom. Proveriti dostupnost u prijavljenom nalogu pre generisanja; ne trošiti kredite nepoznatog servisa. Nije generisan novi video u ovoj implementacionoj celini.

## 7. Vozila, neobične scene i platforme

Posle stabilnog pešačkog toka: trotinet/skejt → ulazak/izlazak → kočenje/nagib → animacija i sudari → misija dostave. Bicikl zatim. Transformišuća klupa je retka, deterministički ograničena humoristična scena; ne sme da prekine trening, obori profil ili zarobi igrača. Potrebni su odgovarajući model/animacija i bezbedan povratak.

Za iPhone: pregled paketa/platformskih API-ja, touch safe area, Metal/IL2CPP, memorija i audio prekidi. Unity generiše Xcode projekat; lokalno završno kompajliranje, potpisivanje i stvarni test zahtevaju macOS/Xcode. APK ne radi na iPhone-u. iOS build i uređaj nisu provereni.

## Isporuka svakog poteza

Analiza postojećeg vlasnika → mala povezana izmena → test rubnih slučajeva → Windows build → native user-flow → pregled slike i zvuka → evidencija i README → commit/push. Ne dodavati platformu ili cloud servis bez jasne funkcije i poznatog troška. Dugi test performansi mora odvojeno meriti rutu, broj NPC-a, p95 frame time i stabilnost memorije; kratki smoke nije dokaz dugotrajne stabilnosti.

Izvori za platforme/video:
- https://docs.unity3d.com/6000.6/Documentation/Manual/iphone-BuildProcess.html
- https://developer.apple.com/help/account/basics/about-your-developer-account
- https://support.google.com/gemini/answer/16126339?hl=en

Status pojedinačnih provera ove iteracije vodi se u zasebnoj evidenciji; ovaj plan sam po sebi nije potvrda da su buduće oblasti implementirane.
