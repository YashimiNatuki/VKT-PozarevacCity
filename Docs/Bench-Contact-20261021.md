# Benč: pravilan oslonac stopala

## Pronađeni problem

Postojeći benč je rešavao položaj skočnog zgloba, ali posle savijanja noge nije vraćao ugao tabana prema podu. Provera udaljenosti do zadate tačke prolazila je, a cipela je ostajala izvrnuta. Nezavisan test sa stvarnim uvezenim modelom, poređenim sa njegovim normalnim stojećim položajem, izmerio je oko **50,39°** nagiba levog tabana pre popravke.

## Izmena

`CityBenchVisual` sada pamti ugao i visinu oslonca oba stopala posle normalizacije stojećeg modela, pre njegovog postavljanja na klupu. Postojeći proračun nogu cilja tu visinu; zatim poravnava stopalo prema sačuvanom osloncu u koordinatama klupe. Položaj šaka i šipke ostaje u postojećem postupku. Nema nove biblioteke, novog modela ili dodatnog pretraživanja skeleta u svakom kadru.

Vrednosti se koriste na postojećoj ravnoj teretani. Ovo nije sistem prilagođavanja stopala proizvoljnom nagnutom terenu. Hod, kontakt sa drugim predmetima i izrazi lica imaju zasebne naredne korake.

Pregled prvog Windows snimka otkrio je i zaklonjen kadar: kamera na pozitivnoj X strani ulazila je iza postojećeg bokserskog nosača i džaka. Autorski kadar benča premešten je na suprotnu stranu, uz nižu tačku pogleda radi pregleda tela i stopala. Završni snimak se proverava posebno od numeričkih rezultata kontakta.

Prednji metalni oslonac klupe pomeren je sa 0,55 m na 0,85 m duž njene ose, ispod kraja jastuka. Tako ostavlja prostor za stopala na podlozi. Test proverava i razmak skočnih zglobova od stvarnih kolajdera klupe; to nije zamena za potpuni proračun sudara kože/odeće.

## Provere

- Novi test: stvarni licencirani model, klupa okrenuta za 65°, 40 položaja pokreta. Oba tabana zadržavaju stojeći ugao sa odstupanjem manjim od 0,1°, ciljnu visinu sa odstupanjem manjim od 3 cm; postojeće provere šaka i stopala ostaju ispod 3 cm.
- Postojeći test i dalje proverava snagu 5 i ceo raspon položaja šipke.
- Puna provera: **223 uspešna testa, 0 neuspešnih, 1 preskočen** (224 ukupno).
- Razvojna provera u Windows programu posmatra kontakte tokom osam pravih ponavljanja, uz postojeći tok unosa komandi, telefon/mapu/muziku, statistiku i porudžbine. Završne vrednosti i ishodi nalaze se u [izveštaju provera](Verification-Bench-Contact-20261021.json).

Provere koriste probni profil i sintetičke komande. Fizički Android/iPhone uređaj i dugotrajne performanse nisu obuhvaćeni. Profil v9 i pravila treninga, nagrade i odmora ostaju isti.

## Narednih pet koraka

1. Kontakt stopala i ritam nogu pri hodu, promeni smera i povratku posle teleporta.
2. Položaj ručnih zglobova i hvat šipke; zatim gard i reakcije u boksu.
3. Skejt sa stvarnim pomeranjem, skokom i bezbednim silaskom.
4. Vidljivi pešaci i mini-mapa povezana sa svim ciljevima.
5. Prepoznatljiv centralni blok, provereni reljef i prirodne srpske replike.
