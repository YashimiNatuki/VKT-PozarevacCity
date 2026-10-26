# VKT: usaglašeni zahtevi i naredne povezane celine

## Izvor plana

Plan objedinjuje zahteve ovog razgovora, [Rođa roadmap](Rodja-Roadmap-20261020.md), [Master Blueprint](VKT-Master-Upgrade-Blueprint.md) i [proveru paralelnih dodataka](Blueprint-Integration-Review-20261021.md). Status u starom blueprint-u nije dokaz isporuke: oznake „završeno” za skejt, pešake i dodatnu mini-mapu odnose se na predlog koji nije automatski uključen u aktivnu igru.

Aktivni izvor: `C:\Users\jamal\Documents\VKT-PozarevacCity`. Windows ostaje glavna platforma. Android se gradi iz istog izvora; potvrda builda, instalacije i dodira su odvojene provere. Originalni Unity projekat i paralelni Antigravity izvor se čuvaju. Privatna druga aplikacija AiBrat/AiGad nije deo ovog izvora.

## Pregled po sistemima

| Oblast zahteva | Postojeća osnova | Šta sledeće treba dokazati |
|---|---|---|
| Početak i UX | Uvod, unos lika, pomoć, vidljive komande, telefon/mapa, kompaktan plejer | Čitljivost na različitim rezolucijama, jasni sledeći ciljevi, bez zaglavljenog ulaza |
| Telo i kretanje | Stilizovani model, odeća, proceduralni udovi, stabilna pauza; nova ograničena korekcija oslonca | Karlica, stepenici, okretanje, kontakt šaka i celog tela; nov model tek uz rig/licencu i LOD |
| Lice | Postojeća ograničena mimika atlasa i oblik vilice | Pravi kapci, očne jabučice i facijalni rig/blendshape-ovi; kod sam ne pravi fotorealističnu kožu |
| Trening i razvoj | Benč sa osam punih ponavljanja, boks, hrana/energija, snaga i profil v9 statistika | Hvat, gard, reakcija na pogodak i smislen dnevni ritam bez duplih nagrada |
| Misije i štek | Ugovori, bašta/katalog, povezane porudžbine i jednokratna isplata uz čuvanje faze | Rođa → Kure → odvlačenje pažnje → povratak; otkaz, smrt, rok i nastavak svake faze |
| NPC i policija | Postojeća patrola/potera i istraživanje mesta zvižduka | Vidljivi kontakti, dnevne rutine, prepreke i posledice bez magičnog znanja o igraču |
| Grad i reljef | OSM osnova, lokalni nazivi i kandidati lokacija | Jedan autorski centralni blok, proverene visine, prohodnost i razmera; pojedinačni pin nije granica naselja |
| Vozila | Kandidati iz odvojenog predloga | Prvo skejt/trotinet, zatim bicikl/auto, sa sudarima, ulaskom/izlaskom i povezanim zadatkom |
| Audio | Prostorni signal, postojeći miks, lični plejer, instrumental šteka | Koraci prema podlozi, vozila prema brzini, pregledane srpske replike sa titlovima; robotizovani kandidat nije prihvaćen glas |
| AI | Lokalni Ollama sloj i autorski fallback | Jasna nedostupnost, ograničen izlaz i budžet; AI ne odlučuje o novcu/inventaru/ishodu misije |
| Špica i identitet | Originalna špica je sačuvana | Novi odvojeni kandidat sa tačnim naslovom, dozvoljenom muzikom i lokalnom montažom |
| Humoristični susreti | Ideja transformišuće klupe i novih kontakata | Ograničena scena koja ne prekida trening, ne zarobljava igrača i ima siguran završetak |
| Mobilne platforme | Raniji Android v8 i dozvoljen novi test na povezanom telefonu | Nova ARM64 instalacija koja čuva podatke, dodir/zvuk/povratak; iOS zahteva zaseban potpisan build i uređaj |

Katalog i bašta koriste apstraktne parametre igre. Nazivi i enciklopedijske kartice nisu uputstvo za realni uzgoj. Porodični/partnerski kontakti moraju imati jasno dobrovoljne, nenametnute odnose i smislen zadatak; tragovi se dobijaju kroz izbor/dijalog.

## Redosled implementacije

1. **Završiti oslonac stopala.** Proveriti stvarnu cipelu, pauzu i vazduh; zatim Windows i Android isporuku sa očuvanim profilom.
2. **Ruke i trening.** Ne menjati nagrade dok se ne potvrde hvat šipke, gard i povratak u normalan hod.
3. **Jedan povezan autorski lanac misije.** Koristiti postojeće vlasnike porudžbine/profila i jasno mapirati kontakte, uslove, posledice i save/load.
4. **Jedan centralni blok.** Uskladiti objekte, prepreke, prolaznost i provereni reljef pre širenja na više gradova.
5. **Saobraćaj i zvuk.** Prvo funkcionalno prevozno sredstvo i pešak, zatim zvuk koji prati njihove stvarne događaje.

Svaka celina: postojeći vlasnik i kriterijum → test koji reprodukuje nedostatak → mala implementacija → fokusirani testovi → puna Unity provera → build → native tok → evidencija → dokumentacija → commit/push. Broj alata nije kriterijum napretka. Dugotrajne performanse, prirodnost glasova i realizam modela traže svoje zasebne dokaze.

## Potvrđeno 26. oktobra 2026.

Završen je prvi korak za ograničene statičke oslonce: [kontakt stopala](Foot-Contact-20261026.md), 245 uspešnih testova i dva Windows toka. To nije puna obrada svih stepenika, karlice ili proizvoljnog terena.

Napravljen je [odvojen kandidat špice](Intro-Candidate-20261026.md): stvarna 30-sekundna remontaža postojećeg videa, sa novom tipografijom i ujednačenim zvukom. Novi AI kadrovi, realističan grad i nova lica nisu generisani. Sledeći kodni korak ostaje hvat šipke/gard, zatim povezana misija sa kontaktima i zvučnim odvlačenjem pažnje. Fizički mobilni testovi čekaju ponovno povezivanje uređaja.
