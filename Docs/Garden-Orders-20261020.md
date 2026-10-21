# Štek → Kure → potvrda → isplata

## Ugovor funkcionalnosti

Posle sva tri glavna ugovora, priđi novom pultu **PORUDŽBINE / KURE** u šteku. V menja izabrani strejn. E pakuje **2 komada iz stvarnog kataloškog skladišta**. Nema besplatnog stvaranja robe niti korišćenja stare probne saksije kao zamene za katalog.

Paket se skida pri prihvatanju. Rok je 180 sekundi aktivne igre. Izađi na ulicu, prati marker 2 i pritisni E kod kontakta. Preuzimaš potvrdu, ne novac. Vrati se na pult PORUDŽBINE u šteku i E naplaćuje ugovorenu nagradu i 3 REP. Telefonski dnevnik prikazuje stanje, preostali rok, uslove i otkazivanje. Mapa zadržava postojeće ulice i marker igrača; nije dodat novi sistem GPS putanja.

Otkazivanje, smrt ili istek roka gube paket/potvrdu, **bez povraćaja robe i bez isplate**. To je nameran rizik ovog posla. Aktivna potera, HEAT preko 45 i trening blokiraju kontakt. Nije moguće paralelno preuzeti drugi ulični posao; benč, boks i kondicija su blokirani tokom porudžbine. Telefon i pauza zamrzavaju rok, aplikaciona suspenzija čuva postojeći checkpoint.

Kure trenutno označava kontakt na postojećem fizičkom markeru 2. Ova iteracija ne donosi novi humanoidni model, prirodno snimljen glas ili obavezni cilj odvlačenja pažnje. Prethodni zvižduk ostaje dostupan na ulici. Takav cilj treba dodati uz stvarno pripremljen susret i čuvara, da ne zavisi od slučajno dostupnog NPC-a.

## Arhitektura i ponovna upotreba

| Odgovornost | Vlasnik |
| --- | --- |
| Kataloško skladište, berba, kupovina | postojeći `CityGarden`; novi interni `TryTakeStock` samo uklanja raspoloživu količinu |
| Paket, potvrda, rok, ugovorena nagrada, završeni poslovi | novi plain C# `CityGardenOrder` i kopirani DTO `CityGardenOrderData` |
| Profil, migracija i objedinjeni snapshot | postojeći `CityProgression`; profil v8 dodaje `gardenOrder` |
| Atomski zapis, rezervna kopija, zabrana starog pisca | postojeći `CitySaveStore`, bez promene njegovog formata omotača |
| Gameplay vreme, markeri, HUD i smrt | postojeći `CityMissionDirector` |
| Fizičko pakovanje i naplata | `CityHideoutOrderDesk` preko postojećeg `CityPlayerInteraction` / `CityInteractable` |
| Trenutno čuvanje prelaza i periodično čuvanje roka | postojeći `CityProgressionController` i događaj `GardenOrder.Changed` |
| Dnevnik i otkazivanje | postojeći `CityPhonePanel` |

Nema novog event bus-a, paralelnog sistema čuvanja, mrežne zavisnosti ili paketa. Stare determinističke ponude i njihovi seed-ovi nisu menjani. `CityStreetJobs` samo dobija postojeći obrazac vezivanja radi međusobne zabrane dostava.

Pre isplate faza se menja u prazno stanje, uklanjaju se paket/potvrda i ugovorena nagrada, upisuje završeni broj i cooldown, pa se menja novčanik. Posmatrač promene novca zato može odmah sačuvati koherentan terminalni snapshot. Ponovljeni poziv ne isplaćuje. Pun novčanik ostavlja potvrdu aktivnom dok nagrada ne može da stane. Prihvaćena cena se čuva i ne preračunava tokom učitavanja.

Prihvatanje, predaja, otkaz i isplata čuvaju odmah. Vreme se snima periodično kroz postojeći interval i pri suspenziji/izlasku; iznenadni prekid procesa nastavlja poslednji uspešni checkpoint. Neuspeh diska se prijavljuje postojećim statusom čuvanja; ne tvrdi se da nepersistirane promene prežive kvar diska.

## Kompatibilnost i granice

Profili v1–v7 zadržavaju novac, katalog, odeću, trening i postojeće misije, pa sledeći snapshot postaje v8. Nedostajući novi DTO znači da nema porudžbine. Neispravne faze, preostali teret u neaktivnoj fazi, nepoznate sorte, negativni/ne-konačni rokovi, porudžbine pre završene kampanje i dve aktivne dostave odbijaju se validacijom.

Stariji build v7 ne podržava novi profil v8. Postojeći čuvar novijeg formata ga čuva od automatskog prepisivanja, ali to nije povratna migracija. Sačuvaj backup pre ručnog vraćanja na stari build. Testovi koriste izolovane privremene profile; lični save i telefon se ne koriste.

Nije dodata nova grafika lika, teren, prirodni glasovi ili nova špica. Windows je primarna meta; nova fizička Android/iPhone provera i novi APK/IPA nisu deo ove isporuke. Kratak native smoke nije dokaz dugotrajnih performansi.

## Kriterijumi završetka

- Stvarne zalihe se smanjuju tačno jednom pri pakovanju.
- Paket i potvrda nastavljaju iz sačuvanog profila bez ponovnog skidanja robe.
- Isplata se ne ponavlja ni kada se čuva iz posmatrača promene novca.
- Otkaz/istek ne stvaraju robu niti novac.
- Potera, trening i druga dostava imaju dosledne zabrane.
- Fizički pult i kontakt, dnevnik, pauza i otkaz rade u native Windows toku.
- Prethodne kontrole, muzika, zvižduk, benč i druge misije prolaze regresiju.

Tačni konačni rezultati, obim testiranja i hash Windows paketa beleže se u `Verification-Garden-Orders-20261020.json` posle završnih provera.
