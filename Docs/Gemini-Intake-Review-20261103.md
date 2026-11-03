# Gemini / AI Studio pregled za selektivno preuzimanje ideja — 2026-11-03

**Status: pregled izvora, bez integracije.** Gemini rezultat ostaje zaseban React/Three.js prototip. Kanonski Unity projekat ostaje jedini vlasnik fizičkog igrača, misija, novca, XP-a, statistike, treninga i trajnog sejva. Zajedničko ime projekta i oznaka profila `v9` ne znače da su implementacije ili formati sejva međusobno zamenljivi.

## Izvor i identitet arhive

| Stavka | Proverena vrednost |
| --- | --- |
| Arhiva | `C:\Users\jamal\Downloads\VKT-Gemini-Review-20261103.zip` |
| Veličina | 39.547.491 bajt |
| Broj ZIP unosa | 235, uključujući direktorijume |
| SHA-256 | `0C41CEE888234F347B58293BB9DA08CDE274BCC0912559A316B1CA18467F7BC1` |
| Kanonski Unity izvor | `C:\Users\jamal\Documents\VKT-3D-PO-BLOK-C\VKT-PozarevacCity` |
| Način ZIP pregleda | Čitanje unosa i izvora direktno iz arhive; bez raspakivanja, instalacije zavisnosti ili izvršavanja sadržaja |

Reference oblika `src/putanja.tsx:123` u ovom dokumentu označavaju redove **unutar pregledanog ZIP-a**, a ne fajlove dodate Unity projektu. Unity reference su označene putanjom `Assets/CityGame/Runtime/...`.

## Šta je viđeno u AI Studiju, a šta potvrđuje kod

**UI opservacija glavnog agenta, bez nezavisnog ponavljanja od strane autora ZIP audita:** otvorena je [AI Studio aplikacija](https://aistudio.google.com/apps/cae3a3cf-98dc-45f2-9195-81f46385733e?showAssistant=true&showCode=true). Banner je prijavio da je importovana aplikacija spremna i da je Gemini značajno prepisao kod radi kompatibilnosti sa Google AI Studiom, uz oznaku „major rewrite“. Glavni agent je koristio **Code → Export → Download .zip** i sačuvao arhivu pod jedinstvenim imenom navedenim iznad. Preview je viđen pre prolaska početnog dijaloga; gameplay tok nije odigran. Screenshot tog UI pregleda nije sačuvan na disk.

**Sync to GitHub i Publish nisu kliknuti.** UI rad je obuhvatio pregled i preuzimanje; nije rađen web deployment, izmena projekta u AI Studiju ili sinhronizacija repozitorijuma.

Nezavisno čitanje ZIP-a potvrđuje sledeće:

- `package.json` definiše React 19, Three.js i Vite aplikaciju, sa skriptama za razvoj, TypeScript proveru i build. To nije Unity izvorni projekat.
- Nije pokrenut `npm install`, TypeScript provera, web build, lokalni server ili browser gameplay test. Postojanje build skripte nije dokaz uspešnog builda. U arhivi nema test suite-a ni dependency lockfile-a.
- Arhiva sadrži **nula `.fbx`, `.glb`, `.gltf`, `.mp4`, `.webm`, `.mp3`, `.wav` i `.ogg` fajlova**. Dokumentacija i screenshots ne predstavljaju modele ili medije koje aplikacija učitava.
- Istorijski Unity dokumenti i slike ostali su u `Docs/`. Pri poređenju sa trenutnim Unity checkoutom, 103 takva fajla imaju identičan hash, 84 se razlikuju i nijedan nedostaje. Ti raniji dokazi ne potvrđuju funkcionalnost novog React izvora.

## Lik, intro, audio i mapa: stvarna implementacija

| Oblast | Šta radi Gemini kod | Odnos prema Unity implementaciji |
| --- | --- | --- |
| Igrivi lik | `src/components/game/CityCanvas3D.tsx:1103–1151` sklapa nadlaktice, podlaktice i šake od `THREE.Group`, box i sphere geometrije. Nema FBX/GLTF loadera, `SkinnedMesh` ili skeletal adaptera. | Ovo nije učitan MakeHuman/MPFB ili Kenney rig. Unity koristi stvarne modele i `CityHumanActor` / `CityHumanPose` adaptere. |
| Garderoba | `modelType`, physique, girth i expression se ne čitaju u rendereru. Boje se čitaju samo pri kreiranju scene (`CityCanvas3D.tsx:1028–1030`); scene effect ima praznu listu zavisnosti na redu 1822. | Dugmad „rodja_human“ i „kenney_retro“ menjaju JSON izbor, bez učitavanja tih modela. Kasnije promene boja nisu ponovo primenjene na postojeću scenu. |
| Trčanje | `CityCanvas3D.tsx:1506–1511` rotira proceduralne nadlaktice, dok obe podlaktice imaju stalan ugao `-0.35 rad`, približno 20°. | To nije popravka laktova Unity riga i ne prenosi se automatski na MPFB ili legacy skelet. |
| Intro | `src/components/cinematics/CinematicIntro.tsx:59,78–82,236` prikazuje 14-sekundni 2D Canvas, naslovne kartice i proceduralne zvuke. App komentar ga naziva videom, ali video element i video fajl ne postoje. | `CityIntroPlayer` koristi MP4 `VideoPlayer`, audio track, pripremu, obradu greške i fallback. Ovaj pregled nije ponovo proverio Unity reprodukciju ili dostupnost njenog medija. |
| Audio | `src/services/audio.ts` generiše Web Audio oscilatore/noise; radio koristi udaljene SomaFM streamove (`:414–433`, adrese u `src/data/catalog.ts`). | Unity `CityGameAudio` već upravlja movement/dialogue/cue kanalima, pauzom, suspend stanjem i kontaktom podloge. Gemini ne donosi postojeće snimljene glasove ili audio assete. |
| Mapa i HEAT | `src/components/phone/apps/MapApp.tsx:162` postavlja marker na fiksne koordinate `(0,8)`. `profile.heat` menja veličinu i providnost crvenog prstena. | To je UI ideja za indikator, ne praćenje stvarnog igrača ili prostorna heatmap mapa. Unity `CityMapView.RefreshPosition` već računa marker iz stvarnog položaja i map anchor-a. |

## Šest ključnih regresija i rizika pri direktnom preuzimanju

1. **Stvarni rig i mediji zamenjeni su proceduralnim prikazom.** Renderer ne učitava modele iz Unity projekta, garderobni izbor modela nema renderer putanju, intro je Canvas, a zvuk je proceduralan ili udaljeni stream. Direktna zamena bi izgubila postojeće rig, animacione, video i audio integracije. Relevantni kod je naveden u prethodnoj tabeli; arhiva nema navedene model/media formate.

2. **Isto vozilo ima dva vlasnika pomeranja.** Blok vožnje igrača pomera vozilo u `CityCanvas3D.tsx:1402–1416`. Zatim petlja nad svim `trafficVehicles` na `:1520–1576` ponovo menja brzinu i položaj istog vozila, bez preskakanja vozila koje igrač vozi. AI tako može prepisati rezultat komandi igrača. Brojanje vožnje dodatno koristi `onRecordDistance(distance, false)` na redu 1416; App to knjiži kao pešačenje (`src/App.tsx:268–273`), umesto kao `totalDistanceRidden`.

3. **Pauza nije zajednička granica simulacije.** `src/App.tsx:343` pauzira Canvas za intro, telefon i menije, ali nezavisni interval na `:95–181` i dalje menja misijski rok, baštu, HEAT i staminu. Unity `CityMissionDirector` i `CityLifestyleRuntime.CanSimulate` već imaju svoje pause/suspend granice; novi UI ih mora poštovati. Politika wall-clock rasta bašte može ostati zasebna, ali ne sme slučajno određivati ponašanje misije i ostalih sistema.

4. **Sejv ima slabiji oporavak i zaštitu upisa.** `src/services/storage.ts:106–108` pri oštećenom primarnom JSON-u vraća podrazumevani profil, bez pokušaja čitanja postojeće rezervne kopije. Mount autosave zatim može prepisati validan backup oštećenim primarnim sadržajem (`:115–119`). Import (`:131–141`) nema proveru domenskih granica ili pune strukture, a greška upisa nije obavezni uslov uspeha importa. Nema checksum envelope-a, zaštite od starog/paralelnog pisca ili Unity transakcione putanje. `CitySaveStore` već ima lock, fingerprint proveru, checksum i atomsku zamenu fajla; ne zamenjivati ga browser storage servisom.

5. **Statistika i čuvanje vezani su za svaki renderovani korak.** `src/App.tsx:249–275` menja profil pri pozivu distance callback-a, a autosave na `:38–41` sinhrono upisuje primary i backup kroz `localStorage` pri svakoj promeni. Tokom kretanja to može značiti dva storage upisa po renderovanom koraku, uz React rerender. Oznaka `version: 9` nije kompatibilni ugovor sa Unity sejvom. Nema odobrene sinhronizacije ta dva profila; direktan zajednički import ili dvosmerni upis bi napravio konkurentske vlasnike statistike i progresije.

6. **Trening nagrade nemaju sačuvanu exactly-once granicu.** `src/components/minigames/BoxingModal.tsx:44–55` zakazuje audio i completion timeout unutar ugnježdenih React state updater-a. `React.StrictMode` je uključen u `src/main.tsx:7`, pa ovakvi side effect-i nisu bezbedna osnova za jednokratno knjiženje nagrade. Završetak osam rundi ne proverava prag uspešnih akcija. Timeout završetka u boksu, kao i u `BenchPressModal.tsx:90–94`, nije otkazan pri zatvaranju modala. App completion callback-i direktno povećavaju XP i brojače (`src/App.tsx:279–309`). Ovo nije isti nagradni ugovor kao postojeći Unity trening tok i ne treba ga prenositi kao vlasnika progresije.

Ovo su nalazi iz koda. Nisu predstavljeni kao odigrani ili reprodukovani browser bugovi.

## Ograničen pregled ključeva i mrežnih poziva

Pregled tekstualnih unosa ZIP-a nije našao korišćene obrasce API/private ključeva; nema `.env` unosa i u `src` nije pronađen AI HTTP klijent. `AiChatBratApp` trenutno prikazuje tekst Unity plugin-a i dugmad koja menjaju novac, reputaciju, HEAT i vitale (`:91–131`); to nije integracija AI servisa.

Ovaj zaključak važi **samo za pregledanu arhivu**. Ne govori ništa o ključevima, podešavanjima ili tajnama u hostovanom AI Studio okruženju. Nije tražen, kopiran ili upisan API ključ i nije doneta odluka o budućem cloud/local provajderu.

Prikazani `AiChatBratPlugin.cs` sadrži brisanje svog generisanog direktorijuma i prethodnog ZIP-a pre ponovnog pakovanja. Plugin nije izvršen niti uvezen; takav generator nije potreban za preuzimanje UI ili NPC ideja.

## Predlog preuzimanja u pet koraka

1. **Sačuvati odvojene izvore i ugovor vlasništva.** Ostaviti ovaj ZIP kao review artefakt sa navedenim hashom. Za svaku izabranu ideju zapisati ZIP referencu, ciljni Unity sistem i merljiv kriterijum prihvatanja. Ne kopirati browser `App`, storage, ekonomiju ili trening callback-e u Unity. Ne menjati sejv šemu i ne spajati profile.

2. **Prvo preneti read-only HEAT prikaz.** Primeniti ideju prstena/alerta uz postojeći `CityMapView` marker i postojeći session HEAT. UI čita autoritativno stanje; ne izračunava svoj HEAT i ne upisuje profil. Prihvatanje: marker prati stvarnog igrača u gradu i šteku; HEAT 0/50/100 menja samo prikaz; novac, XP, brojači i snapshot ostaju isti.

3. **Zatim izdvojiti civilni NPC FSM.** Korisne su ideje lutanja, reakcije na prolazak i odmora kod klupe/kafića. Potrebne su odvojene faze prilaska i dolaska: Gemini sada kopira cilj direktno u položaj NPC-a (`CityCanvas3D.tsx:1629/1636`). `WAITING_CROSSWALK` je samo deklarisan na redu 747 i nema ulazak u to stanje. Unity putanja treba da koristi postojeće pause/suspend granice, stvarnu geometriju i proveru podloge/prepreka. Postojeći `CityPoliceInvestigation` ostaje vlasnik policijskih faza. Prihvatanje: nema teleportovanja pri sedanju, prolaska kroz zidove ili izmene profila; pauza zamrzava ponašanje.

4. **Saobraćaj razviti kao zasebnu proverljivu celinu.** Preuzeti ideju kočenja pred igračem, stop-linijom i semaforom, uz stvarne Unity rute i kolizije. Svako vozilo ima eksplicitnog vlasnika `AI` ili `Player` i samo jedan movement tick. Prihvatanje: crveno zaustavlja vozilo pre linije; prelazak vlasništva ne udvostručuje kretanje; pauza/suspend zaustavljaju simulaciju; statistika vožnje knjiži se jednom kroz postojeću autoritativnu statistiku.

5. **Proveravati svaku ideju pre šire integracije.** Uvesti samo odabrani mali Unity slice, bez novih zavisnosti dok nisu stvarno potrebne. Uraditi odgovarajuće EditMode/proceduralne provere, pa native scenu i vizuelni pregled, uključujući regresije intro/audio/telefon/benč/boks/pauza/sejv. Posebno proveriti jednokratne nagrade i nepromenjen postojeći save snapshot. Web build, Android i fizički uređaj imaju odvojene dokaze. GitHub sync, objavljivanje ili zamena postojećeg proizvoda nisu deo ovog pregleda.

## Granica ovog rada

Ovaj dokument je jedini fajl napisan u okviru ZIP audita. Arhiva nije menjana; nije instalirana nova zavisnost, uvezen asset ili generator, pokrenut web build ili izvršena sinhronizacija. Paralelni Unity rad na trčanju i benču ima sopstvenu implementaciju i verifikaciju i nije dokaz da je Gemini prototip testiran.
