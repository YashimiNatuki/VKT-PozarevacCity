# Početak kampanje: stvarno stanje i deset scenarija za zatvaranje

Datum: 3. novembar 2026. Ovo je pregled izvora i plan prihvatanja; tabela nije tvrdnja da deset autorskih misija već postoji ili da su odigrane u Windows buildu.

## Šta je stvarno povezano

- `CityMissionDirector.CreateContracts` (400–407) definiše **tri** glavna ugovora, sada lokalizovana kao Prva noć / Mila, Roba za blok / Kure i Noćni ekspres / Luka. Prethodni naslovi bili su First Night Courier, Street Supply i Night Express. Svi imaju tri koraka: preuzimanje → predaja → potvrda. Isplata dolazi nakon potvrde.
- `CityGameBootstrap` (156–174) postavlja iste tri stanice za sva tri ugovora, na lokalnim X/Z pomacima `(2,8)`, `(-5,22)`, `(4,36)`. To je mala početna zona, ne tri različite rute kroz ceo Požarevac.
- `CityStreetJobs` (165–184) zatim generiše šest vrsta ponuda, determinističku permutaciju te tri stanice, tri poglavlja i različite HEAT pragove. To su ponovljivi poslovi, ne sedam već napisanih narednih misija.
- `CityGardenOrder` ima zasebnu proverljivu petlju: prihvatanje skida dva komada iz zaliha → Kure kod stanice 2 → povratak sa potvrdom u štek → jedna isplata. Aktivna porudžbina i ulični posao međusobno se isključuju.
- `CityProgressionController` čuva glavne i sporedne checkpoint-e i porudžbine kroz postojeći profil. Postoje testovi za aktivnu rutu posle učitavanja i zaštitu od ponavljanja isplate. To ne zamenjuje hodanje kroz novu geometriju u native buildu.
- `CityLocalStoryPanel` već ima Milu, Vuka i Luku, autorske replike i opcioni lokalni Ollama odgovor. AI replika ne odobrava novac, XP ili promenu cilja; akcija dolazi iz trenutnih činjenica igre.

## Otkriveni problemi i uska dorada kontakata

Pre dorade, PRATI CILJ za glavnu kampanju samo zatvara kontakte, dok za aktivni posao otvara dnevnik. Kontakti u svoje činjenice ne uključuju aktivnu porudžbinu iz šteka i zato mogu ponuditi novi posao dok je Kuretov paket već kod igrača. Sva tri offline kontakta imaju istu rečenicu.

Predloženi kriterijum dorade: PRATI CILJ otvara postojeći dnevnik glavnog ugovora ili aktivne dostave; aktivna porudžbina se računa kao postojeća ruta; tri kontakta imaju različite autorske glasove. Nijedna od tih operacija ne menja sejv, brojače ili novčanik. DTO prema lokalnom AI servisu ostaje isti. Dokazi izvršnih provera pripadaju završnom izveštaju ovog koraka, ne ovoj analizi izvora.

## Deset ciljnih misija / scenarija

| Broj | Priča i svrha | Stvarni temelj danas | Uslov prihvatanja pre proglašenja igrivim |
| --- | --- | --- | --- |
| 01 | **Prva noć / Mila:** upoznaj telefon, mapu, novac i pouzdanost kontakta. | Prva noć / Mila; 120 s, 600 DIN, 12 REP. | Uvod i kreiranje → pronađi prvi marker → preuzmi → predaj → potvrdi. Bez isplate pre kraja; učitaj na oba aktivna koraka; ne dupliraj isplatu. |
| 02 | **Roba za blok / Kure:** prenos delova za radionicu uz jasno prikazan rok i rizik. | Roba za blok / Kure; 90 s, 1.000 DIN, 18 REP. | N otključava tačno sledeći ugovor. Pogrešan marker ne pomera cilj; telefon pauzira; smrt/istek uklanjaju paket i dozvoljavaju novi pokušaj. |
| 03 | **Noćni ekspres / Luka:** pouzdan završetak prve gradske rute. | Noćni ekspres / Luka; 60 s, 1.500 DIN, 25 REP. | Posle trećeg ugovora otključavaju se ulični poslovi i porudžbine. Standardne tri isplate zajedno daju 3.100 DIN i 55 REP, uz početni novac. |
| 04 | **Ključ šteka:** upoznaj ulaz/izlaz, katalog, skladište i izbor sorte. | Fizički štek i katalog se već otključavaju nakon prvog ugovora. | Potrebno autorsko vođenje i evidentiranje zadatka; kupovina samo jednom skida tačnu cenu, ulazak/izlazak vraćaju igrača na ispravnu podlogu i mapu. |
| 05 | **Prva saksija:** kupi seme, posadi i zalij, pa nastavi život u gradu. | Katalog, seme, četiri saksije i trajni rast. | Zadatak se završava nakon validne sadnje/zalivanja, ne nakon šest sati čekanja. Rok misije i sat rasta ostaju različiti; ponovno učitavanje ne pravi seme ili vodu. |
| 06 | **Delovi za ekipu / Vuk:** prvi pun ulični posao, sa vidljivim povratkom potvrde. | Ponuda Delovi za bicikl i trofazni `CityStreetJobs`. | Potrebno odabrati stabilan autorski scenario i proveriti stvarnu pešačku rutu. Prihvatanje, sva tri koraka, rok, otkaz i reload rade; isplata i brojač samo jednom. |
| 07 | **Bez buke:** kontakt zahteva nizak HEAT; vežba reakcije sveta. | Ponuda Tihi dogovor, prag HEAT 30, postojeća policijska stanja. | Potrebna veza sa vidljivim NPC-jem i razlogom odbijanja. Kontakt ostaje blokiran iznad praga i tokom potere; rok nastavlja samo u aktivnoj igri. |
| 08 | **Snaga bloka:** obrok, benč ili boks kao priprema za naredni posao. | Postojeći trening, hrana, izdržljivost i jednokratni izazov. | Potrebno autorsko vođenje; početak/prekid/pauza/odmor provereni. Animacija i kontakt ne dodeljuju dodatni XP; aktivna dostava sprečava novi trening. |
| 09 | **Kuretova porudžbina:** zrela roba → paket → predaja → naplata u šteku. | `CityGardenOrder`, 180 s, količina 2, prag HEAT 45. | Dostupno tek kada stvarno postoje zalihe. Učitavanje tokom paketa i potvrde čuva rok; otkaz/smrt/istek gube paket bez povraćaja i bez isplate. |
| 10 | **Točkovi za blok:** preuzimanje vozila, vožnja i bezbedan izlazak uz završni dogovor. | Audit aktivnog Unity izvora potvrđuje da ulazak/izlazak, vozni kontroler i saobraćaj još nisu integrisani; Gemini web kod je odvojen. | Jedan vlasnik pomeranja vozila, prohodan ulaz/izlaz, pauza, kolizije, statistika vožnje i završni checkpoint. Dodati tek nakon stvarnog native testa. |

Redosled 04–10 je predlog autorskog toka, ne postojeća numeracija u sejvu. Štek se može obići ranije nakon misije 01. Berba i porudžbina 09 su povratni zadatak nakon stvarnog rasta; aktivnosti 06–08 popunjavaju taj period.

## Granice proširenja i prihvat

Ne dodavati ugovore u niz bez migracionog plana. `CityMissionSaveData` (25–38) ograničava broj ugovora na 3 i rokove na 120/90/60 sekundi. `CityProgression` vezuje ponovljive poslove, porudžbine i teritorije za završena tačno tri ugovora. Naziv profila v9 nije dozvola da se novi format tiho uvede. Najpre ostaviti ova tri ugovora stabilnim, zatim uvesti stabilne identifikatore i migraciju za stvarne naredne autorske zadatke.

Katalog počinje rastom od šest sati; voda traje dva sata. To je postojeći balans igrice. Ne ubrzavati taj sat ili dodavati zalihe samo da bi test delovao završen. Testovi mogu koristiti izolovanu kontrolisanu vremensku osnovu; prava igra mora jasno prikazati čekanje i dostupne druge aktivnosti.

Za svaku novu misiju tražiti: vidljiv start i uslovi; razumljiv trenutni cilj; prava dostupna ruta; reakcija sveta; pauza/suspend; neuspeh i oporavak; checkpoint i učitavanje; jednokratna nagrada; završni native prolaz. Fizički telefon, audio pregled i duži FPS/termalni test ostaju zasebni dokazi. Izvorni unit testovi ne potvrđuju da NPC prolazi kroz raskrsnicu ili da auto bezbedno izlazi iz kolizije.

Najpre zatvoriti taj desetodelni prihvat u postojećoj centralnoj zoni. Nakon toga širiti teren i znamenitosti uz proverene visine, putne spojeve i kolizije, pa saobraćaj, glasove i bogatije misijske reakcije.


## Cilj narednih koraka

Prvi prag je pet zatvorenih scenarija: tri postojeće rute plus vođeni obilazak šteka i sadnja/zalivanje. Zatim sledi deset scenarija iz tabele. Brojevi u ovoj tabeli još nisu nova save šema.

1. Učvrstiti postojeće kontakte, srpsku priču, dnevnik i provere tri ugovora.
2. Izdvojiti stabilne ID-jeve autorskih zadataka i unapred testirati migraciju pre proširenja profila. Dodati vođeni štek i sadnju kao stvarne checkpoint zadatke, bez čekanja berbe za završetak.
3. Napraviti jedan fizički ulični kontakt i dve stvarno različite pešačke rute sa proverenim podom i preprekama.
4. Uvesti jedno vozilo: ulazak, jedan vlasnik vožnje, bezbedan izlazak, pauza i statistika. Tek potom AI saobraćaj.
5. Zatvoriti misije 6–10, napredak bašte, policijske posledice, prirodne glasove i audio/teren regresije.

Za svaki scenario prihvat znači: start dostupan i objašnjen; jasan cilj; završetak kroz pravu interakciju; kontrolisan neuspeh i ponovni pokušaj; pauza/suspend; nastavak sa diska u svakom aktivnom koraku; tačno jedna isplata; povratak kontrole posle svih ekrana; native prolaz kroz stvarnu rutu. Ne proglašavati celu igru „100% gotovom“ na osnovu unit testova ili ovog plana.

Sadašnje dorade ne zahtevaju novu plaćenu biblioteku ili instaliranje drugog engine-a. Spoljni AI može da predloži diff i testove u izolovanoj kopiji. [Spremni zadaci](External-AI-Tasks-20261103.md) još nisu poslati spoljnim servisima.
