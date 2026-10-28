# Nova montaža špice u Windows paketu

Kandidat od 23. oktobra je odabran kao `-vktIntroSource` za Windows isporuku od 28. oktobra. Postojeći `CityPortableBuild` ga kopira pod imenom `GameIntro_Pozarevac_ThugLife.mp4`, a postojeći `CityIntroPlayer` ga reprodukuje. Nema drugog video plejera niti izmene mehanike pauze.

- Sadržaj: 30 sekundi postojeće filmske građe, novi VKT / Velika Krađa Točkova / Požarevac / Po-Blok naslovi.
- Format: 1280 × 720, 30 fps, H.264, AAC stereo 48 kHz; 52.766.938 bajtova.
- SHA-256: `d612ff230f759326d7729a95f5f59eaed4111e2a116c956a05e6d01c2e58a6d2`.
- Izvorni video ostaje sačuvan; SHA-256 originala: `46737d77d43de985fccfc24e7cf32dff61beba2c429e08a7ba49e040ec2dc80c`.
- Pun FFmpeg decode ima exit 0. Završne provere dekodiranja u Unity-ju, prirodnog završetka, preskakanja i vraćanja igre vodi [izveštaj isporuke](Verification-Hand-Contact-20261028.json).

Ranije izmeren audio: −16,05 LUFS, −2,58 dBTP. To je tehničko merenje kandidata, ne nova ljudska potvrda kvaliteta zvuka. Muzika i snimci dolaze iz postojećeg korisnikovog projekta; poreklo/prava izvorne građe nisu nezavisno auditovani.

Nisu generisani novi fotorealistični kadrovi, novi 3D lik ili prepoznatljiva 3D rekonstrukcija centra. Pripremljeni scenario za potpuno novu špicu ostaje: patike/asfalt → prepoznatljiv centar → točkovi/patrola → ekipa → noćni grad → čist naslov. Najpre odobriti izgled jednog kadra i glavnog lika, zatim praviti ostale radi kontinuiteta. Plaćeni video zahtev nije poslat.

Lokalni kandidat: `C:\Users\jamal\Documents\VKT-Intro-20261023\VKT-PoBlok-Intro-Remontaza-v1.mp4`. MP4 se distribuira u Windows ZIP-u, ne u Git izvorima. Za drugi računar build zahteva eksplicitno prosleđen medijski izvor; Git clone sam ne preuzima ovaj video. Android izdanje sa ovom špicom još nije izrađeno/provereno.
