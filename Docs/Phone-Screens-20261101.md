# Telefon: mapa i katalog — 1. novembar 2026.

## Ispravka

Poziv `OpenMap()` pa `OpenCatalog()` za otključani poslovni telefon ostavljao je oba ekrana aktivna. `OpenCatalog()` sada isključuje mapu tek kada prođe postojeća provera otključavanja. Kada je katalog zaključan, mapa ostaje otvorena i igrač dobija prethodnu poruku. Uobičajeni tok preko glavnog ekrana uglavnom je već radio; ova ispravka zatvara konkretan prelaz između odredišta.

Nema promene sadržaja kataloga, cena, nagrada, pauze sveta, uvoza modela ili formata profila v9. Dva testa koriste stvarni uGUI telefon i stvarnu progresiju, bez pokretanja gradskog bootstrap-a ili ličnog sejva. Proveravaju oba stanja otključavanja, jedno aktivno odredište, zatvaranje telefona i nepromenjen snapshot progresije.

## Provere

- RED: 1 neuspešan / 1 uspešan slučaj, proces 2; otključani katalog ostavljao je mapu aktivnom.
- Posle ispravke: 2/2 ciljana testa, proces 0.
- Ceo Unity skup: 292 uspešna, 0 neuspešnih, 1 opcioni ArcGIS preskočen; proces 0.
- Windows build: proces 0.
- Stvarni Windows šake, lice, hod/kamere i garderober: četiri procesa 0.
- Desktop i touch-preview regresije: dva procesa 0; boks vraća kontrolu, bašta nema duplu isplatu.
- Prethodna špica: prirodan završetak i preskakanje, oba procesa 0.

[Mašinska evidencija](Verification-Phone-Screens-20261101.json) čuva konkretne rezultate. Dva nova slučaja jesu Unity uGUI testovi; za ovaj prelaz nije rađen zaseban vizuelni klik-test u standalone igri. Touch-preview na Windowsu nije fizički mobilni test. Svih 51 distribuiranih fajlova novog ljudskog modela ostalo je identično prethodnom izdanju.

## Pokretanje i granice

Raspakuj ceo Windows ZIP i pokreni `Launch-VKT.cmd`; Unity Editor nije potreban. P otvara telefon, M mapu, F1 pomoć. Poslovni katalog se otključava posle prvog ugovora. L → garderober → NOVI 3D LIK → IGRAJ U GRADU i dalje bira prethodno poboljšani ljudski model za tekuću sesiju.

Fizičko držanje telefona i hvatanje predmeta još nisu implementirani. Benč/boks koriste prethodne trening-modele. Nisu dodati novi glasovi, fontovi ili novi AI film; špica ostaje prethodna remontaža. Fizički Android/iPhone, dug FPS/termalni test i korisnikova estetska potvrda nisu deo ovog prihvata.

## Sledeći koraci

1. Fizički telefon u ruci uz prikaznu animaciju dok je svet pauziran.
2. Jedno pravilo za međusobno isključivanje svih telefonskih ekrana unutar postojećeg modula.
3. Benč i boks novog ljudskog riga, sa preciznim kontaktom šaka.
4. Kontakt stopala na kosinama i stepenicama, zatim proverene visine i prepoznatljiv centralni blok.
5. Srpski glasovi, vizemi i audio miks po lokaciji.
6. Povezane misije, NPC reakcije i policijske posledice.

[Prethodne šake/gard/udarac](Human-Hands-20261101.md), [lice i širi plan](Human-Face-20261101.md) i [licenca CC0 modela](Faceunits01-CC0-LICENSE.md) ostaju osnova.
