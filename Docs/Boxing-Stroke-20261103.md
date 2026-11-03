# Boks: udarac prati prihvaćen pritisak

CityBoxingRun sada pokreće udarac iz garda pri prihvaćenom unosu, dostiže vrh nakon 150 ms i vraća ruku nakon 300 ms. Odbijen unos i prekid ne daju udarac. Postojeći sat, prag 6/8 i jednokratne nagrade ostaju isti.

RED: 3 nova testa pala su na starom kodu. Odbijen unos davao je Punch=1, a prihvaćeni udarci nisu dostizali punu ekstenziju. GREEN: 7/7 ciljanih. Test ljudskog prikaza sada proverava gard pri unosu, ekstenziju posle 150 ms i odsustvo pomeranja sata od strane prikaza.

Završna Unity EditMode provera: 329 uspešnih, 0 neuspešnih, 1 opcioni ArcGIS preskočen; processExit=0.

Novi Windows build, native vizuelni pregled i telefoni nisu provereni za ovu izmenu. Prethodni EXE je stariji. Rad nogu i fizički kontakt sa vrećom nisu još dodati.

Slede: kontakt vreće, rad nogu, 3D teren, centar Požarevca, srpski glasovi, povezane misije.
