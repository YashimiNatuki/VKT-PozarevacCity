# Statistika igrača: prva proverena integracija Antigravity predloga

Windows je glavna platforma. U telefonu igre pritisni **P → MOJA STATISTIKA**. Vrati se dugmetom **NAZAD U TELEFON**, zatim **ESC** u igru. Dostupno je odmah posle kreiranja lika, bez prvog ugovora.

## Šta se zaista meri

- Stvarno horizontalno pomeranje po hodu i trčanju: uzorak neposredno pre i posle postojeće provere sudara u `CityPlayerController`, bez novog rukovaoca kretanjem. Pritiskanje komande u prepreku ne dodaje zamišljeno pređeni put. Visina skoka nije dužina hoda.
- Aktivno vreme, trenutni i najduži niz preživljavanja. Merenje počinje posle kreiranja lika, u spremnom svetu dok je lik živ i igra nije pauzirana. Trening i mirovanje u aktivnoj igri ulaze u vreme. Telefon, uvod i pauza ne ulaze.
- Smrti prekidaju trenutni niz; najduži ostaje. Ovo nije brojač hapšenja: postojeći događaj smrti nije preimenovan u hapšenje.
- Teleport do šteka, postavljanje lika i povratak iz bolnice ne mere se kao razmak od prethodnog kadra.

Brojači novca, ishoda misija, hapšenja, bekstava i vožnje ostaju priprema u preuzetom modelu podataka. U ovoj celini nisu povezani niti predstavljeni kao aktivna statistika. Prikaz koristi samo povezane podatke. Ne rekonstruiše se istorija starih sesija.

## Vlasništvo i čuvanje

`CityProgression.Stats` poseduje stanje, `CityPlayerController` prijavljuje sopstveno pomeranje, `CityProgressionController` prijavljuje aktivno vreme i čuva postojeće kontrolne tačke, a `CityPhonePanel` prikazuje snimak podataka. Nisu dodati paralelni novčanik, novi sistem misija, internet servis ili biblioteka.

Profil **v9** dodaje polje `stats`. Stari profili do v8 dobijaju nule, uz očuvanje ugovora, bašte, novca i porudžbina. Noviji format sprečava starije izdanje da učita profil i izgubi nepoznato polje pri narednom snimanju; postojeća zaštita spremišta onemogućava automatsko prepisivanje novijeg profila. Povratna migracija iz v9 u v8 nije dodata. Omotač, kontrolni zbir, rezervna kopija i zaštita od zastarelog upisa ostaju u `CitySaveStore`.

Model iz predloga je dodatno popravljen: zbirovi i brojači ostaju u dozvoljenim granicama; nevažeći brojevi se ne beleže; beskonačno trajanje ne postaje rekord misije; snimak podataka je nezavisan od narednih izmena.

## Dokazi i granice

Početna usmerena provera pre popravke: 6 uspešnih i 3 neuspešna testa statistike. Reprodukovani su prelivanje brojača, beskonačan zbir i nevažeći rekord trajanja.

Puna provera nakon popravke: **222 uspešna, 0 neuspešnih, 1 preskočen test** (223 ukupno). Uključuje postojeće porudžbine i 13 novih/preuzetih testova statistike, starije profile, nezavisne snimke i stvarno čuvanje/učitavanje preko `CitySaveStore`.

Završni Windows program je izgrađen sa nula prijavljenih grešaka. Automatizovan prolaz kroz stvarni program proverava hod, trčanje, pauzu, teleport, prikaz i disk, zajedno sa postojećim telefonom/mapom/muzikom/benčom/zviždukom/porudžbinom. Koristi probni profil, sintetičke komande i kontrolisane promene položaja, pa nije zamena za ljudski obilazak grada. Pregledan je snimak ekrana statistike pri 1280×720.

[Mašinski čitljiv izveštaj](Verification-Player-Stats-20261021.json) sadrži završne rezultate i proverljive otiske. [Aktuelni handoff](../HANDOFF-NASTAVAK.md) razdvaja integrisanu statistiku od ostalih kandidata.

Nisu dodati novi skejt, vidljivi pešaci, nova minimapa, realistična glava, novi teren ili prirodni snimljeni glasovi. Nema nove provere fizičkog Android/iPhone uređaja niti tvrdnje o dugotrajnim performansama. Preuzeti Antigravity dodaci ostaju sačuvani u svom checkout-u za zasebne dorade.

## Narednih pet povezanih celina

1. Hod, stopala, proporcije i kontakt šaka sa benčom.
2. Skejt: stvarno kretanje/skok, preuzimanje i bezbedan silazak.
3. Vidljivi pešaci: prohodne putanje, prepreke i reakcije.
4. Mini-mapa: smerovi, sve faze aktivnog cilja i raspored komandi.
5. Prepoznatljiv centralni blok sa odvojeno proverenim reljefom; zatim prirodne replike i autorski zadatak odvlačenja pažnje.
