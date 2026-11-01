# Lice i pogled ljudskog lika — 1. novembar 2026.

Novi MPFB lik dobio je stvarne facijalne blendshape podatke: kapke, vilicu, osmeh, obrve i pomeranje očiju. Ovo je nastavak [igrivog ljudskog modela](Human-Playable-20261101.md). Oblik tela, odeća, topologija, skinning i rest skelet sačuvani su u nultoj pozi; lice se deformiše kroz pripremljene targete, bez heurističkog pomeranja vertexa tokom igre.

**Završena Windows provera:** **9/9 facijalnih + 4/4 feedback testova; ceo Unity skup: 282 uspešna, 0 neuspešnih, 1 opcioni preskočen**. Windows build, native lice/lik/pregled, desktop i touch-preview regresije i oba toka špice prošli su izlazom **0**. [Završna evidencija](Verification-Human-Face-20261101.json) odvaja testove, asset produkciju i granice pregleda. Asistent je pregledao svih sedam stvarnih Windows snimaka; to nije korisnikova estetska potvrda niti fizički mobilni test.

## Kako se koristi

Otvori **L → ODEĆA / GARDEROBER → NOVI 3D LIK / PREGLED**. Dugme **LICE / CEO LIK** bira portret ili celo telo. **IZRAZ LICA** menja režime **MIRAN → FOKUS → NAPOR → USPEH → POVREDA**; **POKRET** i dalje bira miran položaj, hod, trčanje ili gard. Treptanje i pogled u ovom izdvojenom pregledu koriste neumanjeno vreme dok je grad pauziran.

**IGRAJ U GRADU** bira novi model za tekuću sesiju. U gradu lice dobija postojeće signale povrede, uspeha, opreza i fizičkog napora; pogled glave i očiju prati smer pogleda kamere. Pauza zaustavlja gradski facial update. Prvo lice i trening zadržavaju ranije definisana pravila vidljivosti i povratka. Benč i boks još koriste postojeće trening-modele.

Otvaranje vilice prati stanje i napor. **Ovo još nije sinhronizacija govora sa fonemima ili zvučnim zapisom.** Model je generički izmišljeni odrasli lik; nema privatnog portreta, skeniranja korisnika ili novog snimljenog glasa. Izbor izgleda ostaje sesijski, a profil ostaje v9.

## Šta je u assetu

Model ima **7 mesh-eva, 46.064 trougla i 53 kosti**. Nema novih eye/jaw kostiju; lice koristi **15 kontrola** raspoređenih kroz **44 FBX Shape objekta** na relevantnim mesh-evima.

| Grupa | Kontrole | Stvarni podatak iz produkcije |
| --- | --- | --- |
| Kapci | `eyeBlinkLeft`, `eyeBlinkRight` | Po 718 pomerenih body vertexa; najveća delta oko 12,61 mm pre normalizacije |
| Vilica | `jawOpen` | 2.239 body vertexa i 1.884 vertexa zuba |
| Osmeh | `mouthSmileLeft`, `mouthSmileRight` | 1.627 odnosno 1.628 body vertexa |
| Obrve | `browDownLeft`, `browDownRight` | Po 499 body vertexa, uz interpolaciju zasebnih obrva |
| Pogled očiju | `eyeLookUp/Down/In/OutLeft/Right` | Osam zasebnih targeta, svaki pomera 48 vertexa stvarnog ocular mesh-a |

MakeHuman Faceunits 01 paket sadrži širi skup jedinica; ova isporuka koristi samo navedenih 15. Nije implementirano svih 52 ARKit kontrola. Blink, brow, hair, teeth i ocular targeti imaju različite skupove vertexa; različiti mesh-evi ne moraju imati svaki target.

Nezavisno čitanje GLB pozicija i morph accessora potvrđuje i smer svih osam ocular kontrola u izvornom desnorukom Y-up prostoru sa licem prema +Z. `eyeLookOutLeft` i `eyeLookInRight` zajedno pomeraju prednji deo oba oka ka +X; njihove suprotne kontrole idu ka -X. Oba Up targeta idu ka +Y, a oba Down targeta ka -Y. FBX importer može promeniti znak ose pri konverziji u Unity prostor. Zato je ovo statički dokaz izvornog asseta; runtime poravnanje zahteva zasebnu proveru potpisanog pomeraja deformisanih vertexa u okviru lika, uz vizuelni pregled.

**Unity smer pogleda je posebno potvrđen:** FBX konverzija odražava X osu. Adapter jednom čita stvarne delta vektore najmanjeg kompletnog ocular mesh-a, pa kešira znake osa. Za +X u okviru ovog lika aktivni su `eyeLookInLeft` i `eyeLookOutRight`, a suprotni par ostaje nula. Potpisani srednji pomeraj deformisanog oka iznosi **1.721 mm prema +X**. Sačuvani native RED prethodne pretpostavke išao je u suprotnom smeru; konačni test zahteva pozitivan pomeraj veći od 0,1 mm. Nepotpun ili nečitljiv ocular skup isključuje samo pogled očiju, uz očuvane osnovne izraze. Nema čitanja geometrije ili BakeMesh poziva u produkcionom per-frame adapteru.

## Reprodukcija i očuvanje osnove

[build_human_face.py](../Tools/Character/build_human_face.py) čita sačuvani početni `.blend`, zatim regeneriše isti macro oblik, isti `game_engine` rig i istih šest dodatnih mesh-eva. Koristi zvanične MPFB `TargetService.load_target`, `FaceService.interpolate_targets` i `ExportService.bake_modifiers_remove_helpers`. Scene-local `second_root` omogućava provereno nalaženje MHCLO izvora. Interpolacija se prekida ako neki izvor nedostaje.

Facijalni targeti učitavaju se dok postoji originalno MakeHuman numerisanje vertexa. MPFB zatim primenjuje maskiranje uz očuvanje shape keys preko copy/bake/apply-delta postupka. FBX se izvozi sa `use_mesh_modifiers=False` tek nakon tog pečenja, da izvozni korak ne ukloni facial targete. Postojeći `build_human.py` i početni izvoz ostaju sačuvani.

Proizvodni ugovor je prošao: **nulti pomeraj geometrije 0,0 m**, **pomeraj rest matrica 0,0**, iste polygon liste, iste skin weights, isti broj kostiju i isti zbir trouglova. SHA-256 geometrije i rest kostiju pre i posle dodavanja targeta jednak je `04cf6d7199c8926214fd71a794bd18a6d03253514219fc28d4e0bef28caf9f0f`.

Istorijski [RodjaHuman-Provenance-20261029.json](RodjaHuman-Provenance-20261029.json) ostaje zapis početnog asseta. Novi [RodjaHuman-Face-Provenance-20261101.json](RodjaHuman-Face-Provenance-20261101.json) vezuje taj baseline sa target paketom i aktuelnim distribuiranim fajlovima. Njegovi hashovi su završni za distribuirane source assete nakon proverene normalizacije generisanih fajlova.

## Runtime vlasnici i granice

`CityHumanFace` primenjuje postojeće blendshape podatke preko instanciranih `SkinnedMeshRenderer` komponenti i ograničava pogled glave i vrata. Ne menja uvezeni shared mesh niti materijale. Reference i indeksi kontrolišu se pri kreiranju adaptera; update koristi već pronađene slotove.

`CityHumanPose` ostaje vlasnik prikaza hoda i delegira lice. `CityHumanActor` povezuje postojeće `CityPlayerVitals`, audio cue događaje, kameru i `CityFaceState`; odjavljuje događaje pri ponovnom vezivanju i uklanjanju. Fizički kontroler, zdravlje, misije, isplate i profil ne prelaze u facial adapter. Ako kompatibilni facial podaci nedostaju, kretanje zadržava postojeću putanju, a garderober prikazuje da facijalne kontrole nisu dostupne.

FBX koristi centimetarske jedinice i skaliranje model nodova. Zato se veličina imported blendshape delte proverava kroz `renderer.transform.TransformVector(delta)` u metrima, uz stvarnu normalizaciju lika. Sam lokalni `delta.sqrMagnitude` može pogrešno proglasiti mali, ali vidljivi target praznim. Native provera dodatno meri stvarno deformisan renderer i čuva Windows render za pregled.

Pregledani stvarni Windows snimci:

- [face-calm](Screenshots/human-face-calm-windows-20261101.png)
- [face-blink](Screenshots/human-face-blink-windows-20261101.png)
- [face-effort](Screenshots/human-face-effort-windows-20261101.png)
- [face-success](Screenshots/human-face-success-windows-20261101.png)
- [face-gaze-portrait](Screenshots/human-face-gaze-portrait-windows-20261101.png)
- [face-gaze-world](Screenshots/human-face-gaze-world-windows-20261101.png)
- [face-success-world](Screenshots/human-face-success-world-windows-20261101.png)

Još nisu potvrđeni prirodnost izraza ljudskim pregledom, vizemi, usklađen srpski glas, anatomski precizni svi ekstremni spojevi targeta, performanse na telefonu i duži termalni test. GLB inspekcija navodi nedostajuće tangent atribute i 512 px teksturu obrva; Unity FBX import ima sopstveno generisanje tangenti. Ovi nalazi ne zamenjuju pregled konačnog Windows rendera.

## PLAN: narednih 18 konkretnih unapređenja

Ovo je redosled budućeg rada, **nije spisak već implementiranih mogućnosti**. Svaka stavka dobija zasebnu proveru pre povezivanja sa kampanjom.

1. **Karakter — kvalitet lica izbliza:** pregledati oči, obrvu, rub kapka, kosu i skin roughness pod istim svetlom; zadržati pre/posle portrete i budžet tekstura.
2. **Karakter — vizemi i govor:** dodati odabrane zvanične mouth/viseme targete i vezati ih za pregledan audio sa vremenima; tišina mora vratiti vilicu i usne u neutralu.
3. **Karakter — precizne šake:** iskoristiti postojeće kosti prstiju za pesnicu, držanje telefona i hvat šipke; snimati kontakt iz više uglova bez dodatnog fizičkog kontrolera.
4. **Karakter — razvoj građe:** pripremiti ograničene body morph varijante za razvoj snage; proveriti odeću, kolizije i kameru pre trajnog zapisivanja novog izbora.
5. **Karakter — garderober novog modela:** dodati proverene materijalne i odevne varijante sa jasnim pregledom i povratkom, bez promene shared materijala ostalih likova.
6. **Karakter — reakcije tela:** povezati postojeću povredu, kočenje, nagli zaokret i oporavak sa ramenima, trupom i gardom; zadržati zasebnu autoritativnu logiku zdravlja.
7. **Kontakti — stopala i karlica:** proširiti merenje đona sa ravnog fixture-a na proverene kosine, ivičnjake i stepenice, uz odbijanje nedostižne podrške.
8. **Kontakti — prolaz i prepreke:** testirati uske prolaze, plafon, nizak predmet i guranje u zid u sve tri kamere; telo ne sme prolaziti kroz prikazanu prepreku.
9. **Kontakti — benč i boks novog riga:** preneti trening sa fallback modela na novog čoveka tek kada ručni zglob, šipka, pesnica i vreća imaju merljiv kontakt i bezbedan prekid treninga.
10. **Centar — prepoznatljivo gradsko jezgro:** pripremiti zasebne kvalitetne modele Načelstva, parka i lokalnih fasada sa proverljivim referencama i očuvanim oznakama izvora.
11. **Centar — teren i proporcije:** uvesti proverene visine i profile ulica za Čačalicu, Tulbu i centralni blok; vezati podatke za jedan geografski origin i metreski kontrolni raster.
12. **Centar — živ pešački prostor:** unaprediti rute NPC-a, prelaze, čekanje i reakcije na zvižduk ili patrolu kroz postojeći noise/event sistem, uz ograničen periodični budžet.
13. **Misije — krađa točkova kao kompletan tok:** povezati pripremu, mini-igru, teret, rizik patrole i predaju u jednu fikcionalnu misiju sa jasnim napretkom i prekidom.
14. **Misije — odnosi i posledice:** dati Rođi, Kuretu i ekipi različite motive, kontakte i grane dijaloga; reputacija i policijska pažnja treba da menjaju naredne mogućnosti.
15. **Misije — trajni granati zadaci:** proširiti postojeći nastavak misije i jednokratnu isplatu na nove grane; testirati save/load pre i posle svake ključne odluke.
16. **Audio — grad, štek i govor:** uskladiti saobraćaj, korake, ambijent, muziku zone i razumljivost replika u odvojenim miksevima; za svaki dodati izvor i dozvolu distribucije.
17. **Audio/video — nova uvodna scena:** napraviti novu VKT/Po-Blok špicu iz sopstvene gradske scene, likova i pregledanog zvuka; proveriti prirodan završetak, preskakanje i povratak kontrole.
18. **Performanse — merljiv skalabilni kvalitet:** dodati LOD/culling i grafičke profile, odraditi duži Windows FPS/memorija test, zatim stvarni Android i zasebno potpisani iPhone 13+ test.
