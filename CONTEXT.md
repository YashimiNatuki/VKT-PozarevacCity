# VKT — rečnik arhitekture

## Native provera

Razvojni Windows proces pokrenut eksplicitnim `-vktSmoke` argumentom, sa izolovanim profilom koji pravi `CityGameBootstrap`. Učitava stvarni grad, uGUI, kontroler i modele. Native provera nije fizički Android/iPhone test, ljudska estetska potvrda ili merenje dugotrajnih performansi.

## Native scenario fixture

Zajednički razvojni modul u `CityPlayerValidation.HumanFixture.cs`, iza postojećeg `CityPlayerValidation` interface-a. Njegov privatni `HumanValidationScope` čuva životni vek virtuelne tastature, omogućeno/onemogućeno stanje kretanja i kursor. Pri zatvaranju neutralizuje pogled i prolazno stanje kontrolera postojećim `ResetView()` pozivom; ne obećava vraćanje početnog nenultog nagiba. Poredi identitet kamera, audio-listenera i kontrolera, uključujući neaktivne objekte. `HumanProfileCheckpoint` poredi memorijski snapshot i sadržaj sačuvanog profila neposredno oko izbora modela.

Fixture ne poseduje nagrade, zdravlje, fizičko kretanje, trening, kreiranje ili brisanje profila. Scenariji lice/šake/lik zadržavaju svoja merenja i čišćenje specifičnih resursa. Zajednički modul se kompajlira samo za `DEVELOPMENT_BUILD` ili `UNITY_EDITOR`; bez eksplicitnog native toka ne izvršava se u običnom igranju.

## Potpis profila za poređenje prikaza

Serijalizovan, nezavisan primerak `CityProfileData`. Samo UTC sat bašte i proteklo vreme rasta/vlage pojedinačnih parcela izuzimaju se iz semantičkog poređenja, jer bašta napreduje tokom pauze. Novac, identitet biljke i ostala gameplay polja ostaju deo poređenja. Ulazni snapshot ostaje netaknut; neposredno poređenje izbora modela dodatno zahteva identičan sadržaj sejva na disku.
