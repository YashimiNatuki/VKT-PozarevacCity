# Prvi adapter pokreta novog ljudskog modela

Novi model iz prethodnog izdanja dobija animirani pregled: **L → ODEĆA / GARDEROBER → NOVI 3D LIK / PREGLED → POKRET**. Dugme kruži kroz miran položaj, hod, trčanje i gard. Animacija se odvija u mestu dok garderober drži svet pauziranim. Povratak na originalni lik, zatvaranje ili ponovno otvaranje vraća početni režim.

`CityHumanPose` proverava potrebne MPFB kosti i njihove veze, pamti početne rotacije i koristi postojeće `CityGaitState`, `CityActorMotion.PoseLeg`, `RelaxArm` i `PoseBoxingArm`. Ne računa drugi fizički kontroler, ekonomiju ili nagrade. Nema alokacija u njegovoj per-frame `Step` putanji. Vreme prikaza je ograničeno na 50 ms po kadru; nevažeće vreme/brzina ne menjaju pozu. Gard prikazuje položaj ruku; šake su još otvorene, bez kalibrisanog boksačkog hvata novog riga.

Ovo je prvi test adaptera, ne izbor novog igrivog avatara. Potrebno je još povezati stvarno kretanje, terenski oslonac stopala i karlicu, kontakt sa šipkom/vrećom, zatvaranje prstiju, odeću, mimiku i LOD. Nije dodata nova facijalna animacija.

Pre implementacije sva četiri nova testa pala su zbog nedostajućeg adaptera. Posle implementacije prve fokusirane provere su 4/4. Dodat test je zatim reprodukovao previsoke prste u gardu; korekcija koristi niži položaj nadlaktice za duže ruke MPFB riga. Završna puna EditMode provera: 264 uspešna, 0 neuspešnih, 1 opcioni ArcGIS skip. Native, build i paket vode se u [završnoj evidenciji](Verification-Human-Motion-20261101.json); ne izvoditi zaključak o runtime-u samo iz EditMode testova.

Testovi na stvarnoj geometriji proveravaju obe noge, suprotno kretanje ruke/noge, očuvan položaj scene, nevažeće vreme, reset posle hoda/garda i odbijanje nepotpunog skeleta. Native provera dodatno bira sva četiri režima, meri pokret kolena u pauziranom svetu, snima položaje i proverava nepromenjen profil/povratak kontrole.

Provera zapisa upoređuje semantičke podatke posle eksplicitnog save-a. Postojeći UTC sat bašte, rast i vlaga smeju da napreduju dok je meni otvoren; novac, XP, inventar, stanje sadnje i misije ostaju u poređenju. Duži test je otkrio da ranije poređenje bajtova fajla pogrešno označava regularni autosave kao promenu izazvanu pregledom.

[iPhone 13+ plan](iPhone-Plan-20261101.md) i [audit](iOS-Readiness-20261101.json) daju tačne trenutne prepreke. Nema potpisanog IPA niti provere na fizičkom telefonu.
