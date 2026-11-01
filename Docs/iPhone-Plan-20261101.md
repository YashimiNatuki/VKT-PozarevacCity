# VKT na iPhone 13 i novijem — 1. novembar 2026.

**Da, nativni iOS cilj je moguć. Trenutno nemamo potvrđen iOS build.** Korisnik nema Mac, iPhone će biti dostupan za kasniji test. Samo povezivanje iPhone-a USB kablom na Windows ne rešava Xcode kompilaciju i potpisivanje.

## Provereno u ovom projektu

- Editor je Unity 6000.6.5f1 na Windowsu. Lokalno su Android, Linux, WebGL, UWP i Windows moduli; **iOS Build Support nije instaliran**.
- Cesium 1.26.0 paket u lokalnom PackageCache sadrži `Plugins/iOS/libCesiumForUnityNative.a`. To potvrđuje prisustvo datoteke, ne uspešan IL2CPP/Xcode link.
- `CityTouchControls` uključuje mobilni režim preko `Application.isMobilePlatform`, koristi `Screen.safeArea` i ima hod, pogled, akcije i kameru. Ovo je dobra osnova, ali nije dokaz ergonomije na iPhone-u.
- Zapis koristi `Application.persistentDataPath`. Postoji lokalni direktorijum Music; budući iOS uvoz treba vezati za korisnički odabir datoteke, a ne Windows putanju.
- `CityPortableBuild` trenutno izlaže Windows i Android komande. iOS izvoz još treba dodati i proveriti na hostu sa odgovarajućim modulom.
- Android trenutno koristi autorske AI replike kada desktop lokalni servis nije dostupan; za iOS treba uvesti isti eksplicitan mobilni status. Ne tumačiti telefonski `127.0.0.1` kao desktop Ollama servis.

[Mašinski čitljiv lokalni audit](iOS-Readiness-20261101.json) nastao je pokretanjem `Tools/Audit-iOS.py`. Njegov izlaz 0 znači da je audit završen; `iosBuildVerified=false` i `localBuildPrerequisitesPresent=false` ostaju jasni.

## Putanja isporuke

1. Dodati iOS Build Support za istu verziju Unity-ja na izabrani build host. Za automatski izvoz zadržati postojeću offline scenu i profil; koristiti iOS/ARM64/IL2CPP i Metal, uz vraćanje izmenjenih editor postavki.
2. Za završnu aplikaciju obezbediti **macOS + kompatibilan Xcode**, lokalno na pozajmljenom Mac-u ili kroz podešen cloud Mac/Unity Build Automation. Cloud usluga može imati cenu i nije aktivirana ovom doradom.
3. Na tom hostu podesiti bundle ID, nalog i potpisivanje. Za lični uređaj Xcode podržava besplatnu Personal Team prijavu sa ograničenjima, uključujući profile koji ističu posle sedam dana. Za TestFlight/App Store potreban je Apple Developer Program. Ne tražiti lozinku ili sertifikate u četu/repozitorijumu.
4. Izvesti Xcode projekat, kompilirati i proveriti native biblioteke, shader varijante, video, lokalni zapis, orijentaciju i safe area. `Build succeeded` za Windows ne zatvara ovaj korak.
5. Na stvarnom iPhone 13 testirati prvo 30 FPS kao početni cilj: dodir i istovremeni gestovi, pogled, UI tekst, pozadinu/povratak aplikacije, zvuk/mute, video skip, save/load i najmanje dvadeset minuta iste gradske rute. Sačuvati frame time percentile, memoriju i termalno ponašanje. Viši FPS i kvalitet uključivati po merenju.

## Budžet i praktičan sledeći potez

Najjednostavniji put bez nove pretplate je kasniji pristup Mac-u sa Xcode-om i lična probna instalacija. Do tada gradimo i proveravamo na Windowsu, čuvamo mobilne kontrole i pripremamo kvalitet grafike koji može da se skalira. Native iOS, stream Windows igre i web verzija su različiti proizvodi/provere; u ovoj doradi nije isporučen stream ili Safari build.

Alternativa za raniji pregled bez Mac-a je besplatan Sunshine host na Windows računaru i Moonlight klijent sa iPhone App Store-a. Tada igra radi na računaru, telefon prima sliku/zvuk i šalje ulaz preko mreže. To zahteva zasebno uparivanje i proveru VKT upravljanja; posebno ne pretpostavljati da virtuelni gamepad već odgovara trenutnim tastatura/touch kontrolama. Nijedan streaming servis ili mrežni pristup nije sada instaliran/uključen. [Zvanični Moonlight](https://moonlight-stream.org/) i [iOS klijent](https://github.com/moonlight-stream/moonlight-ios).

Prvi grafički ciljevi za mobilni profil: niža razdaljina senki, ograničen broj svetala/NPC-a, kompresovane teksture i LOD. Novi ljudski kandidat trenutno ima 46.064 trougla i nema LOD; ne množiti ga po gradu bez merenja. Još nisu promenjene globalne postavke kvaliteta samo na osnovu modela telefona.

## Zvanični izvori

- [Unity 6.6: iOS environment setup](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/iphone/getting-started/ios-environment-setup): iOS modul, Xcode/macOS i cloud putanja.
- [Apple: Developer account overview](https://developer.apple.com/help/account/basics/about-your-developer-account): Personal Team ograničenja i mogućnosti članstva.

Nijedan servis za naplatu, cloud build, Apple nalog ili javni pristup lokalnom AI servisu nije podešen ovom proverom.
