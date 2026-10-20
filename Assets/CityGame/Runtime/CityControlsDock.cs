using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    [DefaultExecutionOrder(-200)]
    public sealed class CityControlsDock : MonoBehaviour
    {
        private CityGameBootstrap game;
        private CityProgressionController progress;
        private CityLifestyleRuntime life;
        private GameObject canvas,dock,help;
        private Text pageText,nextText;
        private int page;
        private bool helpOwned;
        private bool touchLayout;
        public bool HelpOpen=>helpOwned;
        private static readonly string[] Pages={
            "1 / DOBRO DOŠAO U POŽAREVAC\n\nGORE DESNO: tvoj novac u DIN, sat i ŽIVOT.\nLEVO: reputacija, HEAT i trenutni cilj misije.\nISPOD CILJA: nivo, XP, hrana i energija.\nDOLE: dugmad za telefon, mapu, lik i pomoć.\n\nW A S D: hod / miš: pogled / SHIFT: trčanje\nSPACE: skok / C: promeni perspektivu / E: koristi.\nPrati plavi marker. Rok kreće tek kad prihvatiš posao tasterom E.",
            "2 / TELEFON I MAPA\n\nP ili TELEFON: profil, mapa i dnevnik\nM ili MAPA: direktno ulice i tvoj položaj\nL ili LIK: garderober, hrana i trening\nESC ili NAZAD: zatvori otvoreni meni\n\nTelefon i lik pauziraju svet. Katalog trgovine se otključava prvim ugovorom; mapa je dostupna odmah. Na Windowsu TAB oslobađa miš za dugmad, ponovni TAB vraća pogled.",
            "3 / TRENING I MISIJE\n\nPriđi klupi na manje od 3,5 m. E ili L otvara trening. BENČ: F jednom za spuštanje, sačekaj GURAJ, pa F za potisak. Potrebno je 8 celih ponavljanja. BOKS: sačekaj SAD, pa J za udarac ili K za gard.\n\nAktivna dostava i potera blokiraju trening. Završi ili otkaži posao. Jedi kod kioska i sačekaj odmor; prevremeni prekid ne daje XP.",
            "4 / ZVUK I POVRATAK U IGRU\n\nF6: mali muzički plejer\nF7: pusti / pauziraj muziku bez otvaranja\nF8: sledeća numera\nZVUK u plejeru: jačina i podešavanja\n\nMuzika ne pauzira misiju niti preuzima miš. ESC prvo zatvara plejer; naredni ESC pauzira igru. F1 vraća ovu pomoć. NPC glasovi još nisu odobreni; razgovori imaju titlove."
        };
        private static readonly string[] TouchPages={
            "1 / DOBRO DOŠAO U POŽAREVAC\n\nGORE DESNO: novac u DIN, sat i ŽIVOT.\nLEVO: reputacija, HEAT i cilj misije.\nISPOD CILJA: nivo, XP, hrana i energija.\nGORE U SREDINI: TELEFON, MAPA, LIK i POMOĆ.\n\nDrži strelice levo za hod. Prevuci prstom preko slobodnog desnog dela ekrana za pogled.\nTRČI: sprint / SKOK: skok / KAMERA: perspektiva.\nPrati plavi marker i dodirni KORISTI. Rok počinje tek kada preuzmeš posao.",
            "2 / TELEFON I MAPA\n\nTELEFON: profil, mapa i dnevnik\nMAPA: direktno ulice i tvoj položaj\nLIK ili BLOK / LIK: odeća, hrana i trening\nNAZAD U IGRU: zatvori meni i nastavi\n\nTelefon i lik pauziraju svet. Katalog trgovine se otključava prvim ugovorom; mapa je dostupna odmah. Narandžasta tačka na mapi označava tvoj položaj; sever je gore.",
            "3 / TRENING I MISIJE\n\nPriđi klupi na manje od 3,5 m. KORISTI ili LIK otvara izbor treninga.\nBENČ: dodirni TAP za spuštanje, sačekaj GURAJ, pa ponovo TAP za potisak. Uradi 8 celih ponavljanja.\nBOKS: sačekaj SAD, pa dodirni UDARAC ili GARD prema prikazanom zadatku.\n\nAktivna dostava i potera blokiraju trening. Prvo završi posao, jedi kod kioska i sačekaj odmor. PREKINI završava seriju bez XP.",
            "4 / ZVUK I POVRATAK U IGRU\n\nMUZIKA: otvara mali plejer\nPUSTI: pušta ili pauzira muziku\n< i >: prethodna / sledeća numera\nZVUK: jačina i podešavanja\nX: zatvara plejer\n\nMuzika ne pauzira misiju; strelice za kretanje ostaju aktivne. POMOĆ vraća ova objašnjenja. NPC glasovi još nisu odobreni; razgovori imaju titlove."
        };
        public void Initialize(CityGameBootstrap owner)
        {
            game=owner;progress=owner.GetComponent<CityProgressionController>();life=owner.GetComponent<CityLifestyleRuntime>();
            canvas=new GameObject("VKT_Controls",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=110;
            var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
            dock=Rect("QuickControls",canvas.transform,new Vector2(456,40),Vector2.zero).gameObject;
            bool touch=Application.isMobilePlatform||Array.IndexOf(Environment.GetCommandLineArgs(),"-vktTouchPreview")>=0;
            touchLayout=touch;
            var row=(RectTransform)dock.transform;row.anchorMin=row.anchorMax=touch?new Vector2(.5f,1):Vector2.zero;row.pivot=touch?new Vector2(.5f,1):Vector2.zero;row.anchoredPosition=touch?new Vector2(0,-64):new Vector2(16,16);
            Button(dock.transform,touch?"TELEFON":"TELEFON / P",new Vector2(-171,0),new Vector2(108,40),progress.TogglePanel);
            Button(dock.transform,touch?"MAPA":"MAPA / M",new Vector2(-57,0),new Vector2(108,40),progress.OpenMap);
            Button(dock.transform,touch?"LIK":"LIK / L",new Vector2(57,0),new Vector2(108,40),life.ToggleMenu);
            Button(dock.transform,touch?"POMOĆ":"POMOĆ / F1",new Vector2(171,0),new Vector2(108,40),ShowHelp);
            help=Rect("HelpBackdrop",canvas.transform,Vector2.zero,Vector2.zero).gameObject;var shade=(RectTransform)help.transform;shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;help.AddComponent<Image>().color=new Color(0,0,0,.8f);
            var card=Rect("HelpCard",help.transform,new Vector2(660,510),Vector2.zero);card.gameObject.AddComponent<Image>().color=new Color(.025f,.055f,.075f,.99f);
            pageText=Label(card,"",new Vector2(600,376),new Vector2(0,40),20);pageText.alignment=TextAnchor.UpperLeft;
            Button(card,"PRETHODNO",new Vector2(-205,-205),new Vector2(180,44),()=>{page=Mathf.Max(0,page-1);RefreshPage();});
            var next=Button(card,"DALJE",new Vector2(0,-205),new Vector2(180,44),()=>{if(page==Pages.Length-1)CloseHelp();else{page++;RefreshPage();}});nextText=next.GetComponentInChildren<Text>();
            Button(card,touch?"NAZAD U IGRU":"NAZAD / ESC",new Vector2(205,-205),new Vector2(180,44),CloseHelp);
            canvas.AddComponent<CityUiSkin>().ApplyTree();help.SetActive(false);
        }
        public void ShowHelp()
        {
            if(game==null||!game.WorldReady||!game.Progression.IsCreated||!game.Vitals.IsAlive||life.Life.IsTraining||life.Life.IsBoxing)return;
            progress.ClosePanel();life.CloseMenu();life.LocalStory.Close();life.Wardrobe.Close();game.GetComponent<CityMusicPanel>().Close();
            page=0;helpOwned=true;help.SetActive(true);game.Director.SetOverlayOpen(true);RefreshPage();
        }
        public void CloseHelp(){if(!helpOwned)return;helpOwned=false;help.SetActive(false);game.Director.SetOverlayOpen(false);}
        private void RefreshPage(){pageText.text=(touchLayout?TouchPages:Pages)[page];nextText.text=page==Pages.Length-1?"KRENI U GRAD":"DALJE";}
        private void Update()
        {
            if(game==null)return;
            bool ready=game.WorldReady&&game.Progression.IsCreated&&game.Vitals.IsAlive&&(game.Intro==null||!game.Intro.IsActive);
            dock.SetActive(ready&&!helpOwned&&!game.Director.IsPaused&&!life.Life.IsTraining&&!life.Life.IsBoxing);
            var keys=Keyboard.current;if(!ready||keys==null)return;
            if(helpOwned&&keys.escapeKey.wasPressedThisFrame){CloseHelp();game.Director.SuppressPauseThisFrame();return;}
            if(keys.f1Key.wasPressedThisFrame){if(helpOwned)CloseHelp();else ShowHelp();}
            if(!game.Director.IsPaused&&!Application.isMobilePlatform&&keys.tabKey.wasPressedThisFrame)
            {bool locked=Cursor.lockState==CursorLockMode.Locked;Cursor.lockState=locked?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=locked;}
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        private static Text Label(Transform parent,string title,Vector2 size,Vector2 position,int font)
        {
            var text=Rect("Label",parent,size,position).gameObject.AddComponent<Text>();text.text=title;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=font;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        private static Button Button(Transform parent,string title,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(title,parent,size,position);rect.gameObject.AddComponent<Image>().color=new Color(.06f,.18f,.22f,.94f);var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(action);Label(rect,title,size-Vector2.one*8,Vector2.zero,15);return button;
        }
        private void OnDisable(){if(game!=null)CloseHelp();}
        private void OnDestroy(){if(canvas!=null)Destroy(canvas);}
    }
}
