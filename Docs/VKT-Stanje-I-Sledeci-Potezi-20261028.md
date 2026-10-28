# VKT: stanje i sledeći povezani potezi — 28. oktobar 2026.

Aktivni izvor je `C:\Users\jamal\Documents\VKT-PozarevacCity`. Windows ostaje prva platforma. Ovo je pregled proverljivog stanja i zahteva; broj konektora, prisutna skripta ili predlog saradnika nisu dokaz da funkcija radi u isporučenoj igri.

## Gde smo stali

Poslednja završena celina u [handoff-u](../HANDOFF-NASTAVAK.md) bila je oslonac stopala na ograničene statičke podloge. Sada su dodati hvat šipke, savijanje prstiju i stabilniji boksački gard kroz postojeće `CityBenchVisual` i `CityActorMotion`, uz `CityHandPose`. Profil ostaje v9; nema drugog vlasnika kretanja, treninga, novca ili zapisa.

Završna provera: **255 uspešnih EditMode testova, 0 neuspešnih, 1 opcioni skip; Windows build bez grešaka; desktop i touch-preview tokovi, prirodan završetak i preskakanje špice — izlaz 0.** [Izveštaj isporuke](Verification-Hand-Contact-20261028.json) vodi tačna merenja i granice. Testovi rasporeda za dodir koriste sintetički Windows input, ne fizički Android.

Nova špica **postoji**: `VKT-PoBlok-Intro-Remontaza-v1.mp4`, tridesetosekundna remontaža postojećih kadrova. Novi Windows build eksplicitno je zapakovao kandidat od **52.766.938 bajtova**. To nisu novi AI-generisani kadrovi ili novi realistični likovi. Original se čuva; puštanje, prirodan završetak i preskakanje u upakovanom igraču proveravaju se odvojeno. [Poreklo i integracija](Intro-Integration-20261028.md).

## Šta postoji, a šta još nedostaje

| Oblast | Stanje zasnovano na izvoru | Preostala granica |
|---|---|---|
| Početak, telefon, mapa, muzika | Postoje uvod, kreiranje lika, pomoć, komande, telefon, mapa i kompaktni plejer; `CityPhonePanel`, `CityMapView`, `CityMusicPanel` koriste postojeći tok igre. | Čitljivost i povratak kontrole proveravati u svakom novom paketu. Dodatni radar, Po-Oglasi i foto-zadaci nisu završeni. |
| Glava i lice | `CityCharacterModel` učitava Kenney model; `CityFaceMorph` dodaje jedan ograničen morf vilice `Rodja_JawRelax`, a `CityFaceExpression` koristi atlas izraza i treptanja. | Nema realističnog modela glave, punog facijalnog riga, očnih jabučica/kapaka ili artikulacije govora. Dodatni kod ne može nadoknaditi nedostajuću geometriju. |
| Telo i kontakt | Postoje odeća, građa, usklađen hod, ograničeni oslonac stopala; nova celina dodaje prste, hvat i gard. | Karlica, stepenici, okretanje u mestu, dinamičke podloge i precizan udarac u vreću ostaju zasebni poslovi. Novi hvat nije opšti adapter za svaki rig. |
| Trening i razvoj | Benč, boks, kondicija, hrana/energija i statistika postoje kroz `CityLifestyle`, `CityBoxingRun`, `CityConditioningRun` i profil v9. | Potvrditi završetak/pauzu/prekid i jednu nagradu; potom povezati vizuelni udarac, reakciju mete i dnevni ritam. Transformišuća klupa još je ideja. |
| Bašta, katalog i misije | `CityPlantCatalog` sadrži osam strejnova, sa cenama i otključavanjem; enciklopedija i tržište postoje. [Porudžbina](Garden-Orders-20261020.md) povezuje štek, kontakt, potvrdu i jednokratnu isplatu. | Parametri su apstraktna ekonomija igre. Kure je postojeći marker; novi vidljivi lik i obavezno odvlačenje pažnje nisu isporučeni. Širi autorski lanac Rođa → Kure tek treba povezati. |
| Policija i stanovnici | `CityPoliceInvestigation` razdvaja patrolu, istragu, poteru i pretragu; skriven položaj ne osvežava poslednje poznato mesto. Zvižduk ima domet, zaklon i cooldown. | Potrebni su vidljivi kontakti i obični pešaci sa prohodnim rutama; ne postoji potvrđen kompletan gradski raspored NPC-a/saobraćaja. |
| Požarevac i visine | OSM ulice, lokalni nazivi, fasade i registar lokacija postoje. `CityOsmStreets` i dalje pravi ravnu osnovu; alat `audit_elevation.py` proverava raster i ostavlja `runtimeEnabled=false`. | Prepoznatljivo Načelstvo/park/korzo i provereni reljef nisu završeni. Pin nije granica naselja; DSM/DEM uzorak nije automatski visina kolovoza. |
| Vozila | Skejt, dodatna minimapa i pešaci iz Antigravity predloga vode se kao kandidati u kanonskom handoff-u. | Nema potvrđene završene vožnje skejta, trotineta, bicikla ili balkanskog voznog parka u aktivnom izvoru. Oznake „Završeno” u starom [blueprint-u](VKT-Master-Upgrade-Blueprint.md) nisu važeći dokaz integracije. |
| Audio i AI | Postoje prostorni zvuk, podloge koraka, zvižduk, lični plejer i originalni instrumental šteka. Lokalni AI ima proveru dostupnosti, ograničene odgovore i autorske rezervne replike. | Prirodni srpski glasovi nisu prihvaćena glasovna banka. Zvučni događaji automobila zahtevaju stvarno vozilo. Mobilni AI nije dokazan pristup desktop Ollama servisu; AI nije autoritet za isplatu ili ishod misije. |
| Platforme | Windows build postoji; raspored za dodir može da se proverava na Windowsu. | To nije fizički Android test. Telefon je isključen iz ove iteracije. iPhone nema potvrđen potpisan build ili test na uređaju. |

## Da li AiRođa/AiGad tehnologija pomaže liku?

Proveren Noir prikaz od 27. oktobra u drugoj aplikaciji koristi detaljnu transparentnu WebP sliku, prikazanu kao `img`, uz ambijentalni WebGL sloj. `noir-studio.js` učitava sliku; dokumentacija eksplicitno isključuje mimiku/let/dubinu za taj statični portret. Nema skeleta ili 3D tela koje možemo preneti u Unity i posmatrati iz svih uglova.

Možemo preuzeti **vizuelni pravac**: kvalitet odeće, kontrast, materijale i karakter portreta. Privatni izvor, slika i podaci druge aplikacije nisu kopirani u ovaj repozitorijum. Za igrivog junaka potreban je pravi 3D model sa rigom, teksturama i licem namenjenim animaciji. Kandidat za besplatan tok je **Blender + MPFB**, uz proveru licence svakog dodatnog resursa i probni izvoz Game Engine riga/PBR materijala. Taj model i integracija još nisu urađeni. [MPFB izvoz](https://static.makehumancommunity.org/mpfb/docs/exporting/index.html) i [licence](https://download.makehumancommunity.org/about/license.html) služe kao polazna dokumentacija, ne potvrda kompatibilnosti konkretnog budućeg modela. Preuzimanje planirati iz zvaničnih statičkih paketa/GitHub izvora: [obaveštenje zajednice od 21. oktobra](https://static.makehumancommunity.org/) navodi da su stari forum i repozitorijum korisničkih resursa privremeno isključeni.

## Narednih osam celina

Sadašnja isporuka proverava native tastaturu i touch-preview, osam ponavljanja benča i boksa, jednu nagradu, povratak u hod i špicu do kraja/preko preskakanja. Naredne celine nisu proglašene završenim tim proverama.

1. **Stvarno bolja glava i kompletan 3D kandidat.** Izraditi jedan ljudski model sa pravilnim proporcijama, kapcima, očima i osnovnim facijalnim morfovima. Uporediti sprijeda/sa strane/otpozadi pri neutralnom dnevnom svetlu, bez filtera koji skrivaju nedostatke. Prikazati licencu, broj trouglova, teksture i LOD. Uvesti kao odvojen kandidat, uz očuvan postojeći lik.
2. **Adapter tela i animacija.** Zadržati `CityPlayerController`, vlasnika poza i postojeći profil; prilagoditi novi skelet umesto paralelnog kontrolera. Proći hod/trčanje/okretanje, pauzu, teleport, stopala, benč i boks. Kriterijum: bez skokova skale, proklizavanja, prodora kroz šipku i izgubljene kontrole.
3. **Jedna cela misija sa vidljivim kontaktima.** Rođa šalje do Kureta; zvuk odvlači pripremljenog čuvara; igrač vraća potvrdu. Čuvati svaku fazu postojećim sistemom. Testirati učitavanje, smrt, otkaz, rok, hapšenje i ponovljen poziv naplate; isplata najviše jednom.
4. **Jedan prepoznatljiv centralni blok.** Načelstvo, park i deo korzoa sa pregledanim referencama, merama i kolizijama. Zatim mali provereni visinski sektor, uz označeno poreklo rastera, vertikalni datum i nivelisane raskrsnice. Proći celu rutu peške pre širenja grada.
5. **Prirodni glasovi i zvuk vezan za događaje.** Napraviti 20–30 pregledanih srpskih replika, titlove, prostorni izvor i podešavanje jačine; izabrati glas tek posle ljudskog slušanja. Proveriti miks pri govoru/muzici/koracima, mute, zatvoren prostor i prekid aplikacije. Broj prolaza audio testa nije potvrda prirodnosti izgovora.
6. **Skejt kao prva kompletna mikromobilnost.** Preuzimanje u svetu, ubrzanje, kočenje, ollie, pad/silazak i jedan dostavni zadatak. Bez teleportovanja kroz zidove, zaglavljenog ulaza i duplog računanja kilometraže. Trotinet/bicikl tek kroz isti potvrđen ugovor kretanja.
7. **Retka transformišuća klupa.** Kratak autorski susret sa jasnim signalom, izlazom i povratkom u prvobitno stanje. Nikada usred aktivnog ponavljanja ili naplate. Prekid/pauza/učitavanje ne smeju zarobiti igrača ili ukloniti pristup treningu.
8. **Duži Windows test, pa mobilna isporuka.** Ista ruta najmanje 20 minuta uz beleženje frame time percentila, memorije i grešaka; pragove vezati za navedeni hardver. Zatim ARM64 build i fizički Android dodir/zvuk/suspenzija/učitavanje. iOS planirati zasebno uz Mac/Xcode, potpisivanje i stvaran uređaj.

Svaka celina završava se dokazima iz svog obima: izvor → fokusirani test → regresija → build → runtime → vizuelni/slušni pregled kada je potreban. Ne uvoditi novu biblioteku ako postojeći vlasnik već rešava problem; prvo potvrditi konkretan nedostatak i kompatibilnost.
