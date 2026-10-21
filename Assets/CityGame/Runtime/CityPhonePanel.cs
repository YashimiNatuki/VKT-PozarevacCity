using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityPhonePanel : MonoBehaviour
    {
        private CityProgressionController owner;
        private GameObject content;
        private GameObject economy;
        private GameObject backdrop;
        private GameObject ownedEventSystem;
        private InputField callsign;
        private Text summary;
        private Text status, catalogStatus;
        private Button create;
        private Button recruit, claim;
        private float refreshCountdown;
        private string feedback;
        private float feedbackUntil;
        private GameObject catalog;
        private readonly Text[] catalogRows = new Text[8];
        private readonly Button[] catalogButtons = new Button[8];
        private int selectedPlant, buyerIndex, districtIndex;
        private Text buyerText, districtText;
        private Button buyCatalog, sellCatalog;
        private GameObject encyclopedia;
        private RawImage botanical;
        private Text plantDetail;
        private int selectedCard;
        private Texture2D botanicalAtlas;
        private GameObject map;
        private CityMapView mapView;
        private GameObject journal;
        private Text jobDetails;
        private Button acceptJob, cancelJob;
        private ScrollRect jobScroll;
        public bool IsOpen => (map != null && map.activeSelf) || (content != null && content.activeSelf) || (catalog != null && catalog.activeSelf) || (encyclopedia != null && encyclopedia.activeSelf) || (journal != null && journal.activeSelf);

        public void Initialize(CityProgressionController controller)
        {
            owner = controller;
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
                ownedEventSystem = new GameObject("VKTInputEvents", typeof(EventSystem), typeof(InputSystemUIInputModule));
            RectTransform shade = Rect("PhoneBackdrop", transform, Vector2.zero, Vector2.zero);
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            backdrop = shade.gameObject;
            backdrop.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            content = Rect("PhonePanel", transform, new Vector2(720, 880), Vector2.zero).gameObject;
            content.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.98f);
            Label("Title", content.transform, "VKT / Velika Kradja Točkova\nPo-Blok-C presents", 31, 25, 100);
            Label("Signature", content.transform, "what bike / since 1995", 17, 125, 30);
            RectTransform field = Rect("Callsign", content.transform, new Vector2(640, 58), new Vector2(0, 245));
            field.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f);
            callsign = field.gameObject.AddComponent<InputField>();
            callsign.characterLimit = 24;
            Text value = ChildText(field, "Value", "", 24);
            value.supportRichText = false;
            callsign.textComponent = value;
            callsign.placeholder = ChildText(field, "Placeholder", "Tvoj nadimak (2–24 slova/broja)", 20);
            create = Button("Create", content.transform, "Kreni u Požarevac", new Vector2(0, 165), () =>
            {
                if (!owner.CreateCharacter(callsign.text)) ShowFeedback("Use 2-24 letters, numbers, spaces, - or _. Check save status.");
            });
            summary = Label("Summary", content.transform, "", 23, 180, 180);
            summary.supportRichText = false;
            economy = Rect("Economy", content.transform, new Vector2(640, 250), new Vector2(0, -90)).gameObject;
            recruit = Button("Recruit", economy.transform, "Angažuj / 250 cash", new Vector2(-160, 40), () => Act(owner.Progression.Recruit));
            claim = Button("Claim", economy.transform, "Preuzmi blok / 750 cash", new Vector2(160, 40), () => Act(owner.Progression.ClaimTerritory));
            status = Label("Status", content.transform, "", 18, 645, 35);
            Button("Catalog", content.transform, "KATALOG / BAŠTA", new Vector2(-160, -290), OpenCatalog);
            Button("StreetJob", content.transform, "DNEVNIK / POSLOVI", new Vector2(160, -290), OpenJobs);
            Button("Map", content.transform, "MAPA", new Vector2(-160, -350), OpenMap);
            Button("Close", content.transform, "NAZAD U IGRU", new Vector2(160, -350), owner.ClosePanel);
            Label("Help", content.transform, "Štek: vrata kod trećeg markera. E: interakcija / V: strejn.\nE: pokupi opremu / Q: promeni / LMB: napad. Ekipa: REP 30.", 16, 820, 45);
            BuildCatalog();
            BuildEncyclopedia();
            BuildJobs();
            BuildMap();
            gameObject.AddComponent<CityUiSkin>().ApplyTree();
            Close();
        }

        private void BuildMap()
        {
            map=Rect("MapPanel",transform,new Vector2(720,880),Vector2.zero).gameObject;
            map.AddComponent<Image>().color=new Color(.025f,.04f,.065f,.99f);
            Label("MapTitle",map.transform,"POŽAREVAC / MAPA ULICA",25,12,40);
            mapView=map.AddComponent<CityMapView>();
            Button("BackFromMap",map.transform,"NAZAD U TELEFON",new Vector2(0,-395),Open);
            map.SetActive(false);
        }
        public void OpenMap()
        {
            if(gameObject==null||map==null)return;
            if(!owner.Progression.IsCreated)return;
            if(!IsOpen)owner.OpenPanel();
            content.SetActive(false);catalog.SetActive(false);encyclopedia.SetActive(false);journal.SetActive(false);
            backdrop.SetActive(true);map.SetActive(true);mapView.Initialize();mapView.RefreshPosition();
        }        private void Act(Func<bool> action) { if (!owner.Perform(action)) ShowFeedback("Akcija nije dostupna: proveri uslove, prostor i novac."); Refresh(); }
        private void ShowFeedback(string message) { feedback = message; feedbackUntil = Time.unscaledTime + 5; status.text = message; }
        public void Open() { if(map!=null) map.SetActive(false); backdrop.SetActive(true); journal.SetActive(false); encyclopedia.SetActive(false); catalog.SetActive(false); content.SetActive(true); Refresh(); }
        public void Close() { if(map!=null) map.SetActive(false); if (content != null) content.SetActive(false); if (catalog != null) catalog.SetActive(false); if (encyclopedia != null) encyclopedia.SetActive(false); if (journal != null) journal.SetActive(false); if (backdrop != null) backdrop.SetActive(false); }
        private void Update()
        {
            if (!IsOpen) return;
            refreshCountdown -= Time.unscaledDeltaTime;
            if (refreshCountdown <= 0) { Refresh(); refreshCountdown = 0.25f; }
        }
        private void Refresh()
        {
            CityProgression p = owner.Progression;
            callsign.gameObject.SetActive(!p.IsCreated);
            create.gameObject.SetActive(!p.IsCreated);
            summary.gameObject.SetActive(p.IsCreated);
            int seeds = 0, bud = 0;
            for (int i = 0; i < CityPlantCatalog.Count; i++) { seeds += p.Garden.Seeds(i); bud += p.Garden.Stock(i); }
            summary.text = $"{p.Callsign} / CASH {p.Session.Cash} / REP {p.Session.Reputation}\nUgovori {p.CompletedContracts}/3 / Ekipa {p.Crew}/3 / Blokovi {p.Territories}/3\nŠtek: 4 saksije / seme {seeds} / bud {bud}\nUkupno berbi: {p.Garden.Harvests}\nKATALOG: seme, cene i kontakti za otkup" + (p.Territories == 3 ? "\nPo-Blok-C: kampanja blokova završena." : "");
            economy.SetActive(p.PhoneUnlocked);
            recruit.interactable = p.Session.Reputation >= 30 && p.Crew < 3 && p.Session.Cash >= CityProgression.CrewPrice;
            claim.interactable = p.CompletedContracts == 3 && p.Crew >= 2 && p.Territories < 3 && p.Session.Cash >= CityProgression.TerritoryPrice;
            status.text = Time.unscaledTime < feedbackUntil ? feedback : owner.SaveStatus;
            if (catalog != null && catalog.activeSelf) RefreshCatalog();
            if (journal != null && journal.activeSelf) RefreshJobs();
        }

        public void OpenCatalog()
        {
            if (!owner.Progression.PhoneUnlocked) { ShowFeedback("Poslovni katalog se otključava posle prvog ugovora. MAPA je već dostupna.");return; }
            encyclopedia.SetActive(false);
            journal.SetActive(false);
            content.SetActive(false); catalog.SetActive(true); RefreshCatalog();
        }
        public void OpenJobs()
        {
            if (!owner.Progression.IsCreated) return;
            map.SetActive(false);
            content.SetActive(false); catalog.SetActive(false); encyclopedia.SetActive(false);
            journal.SetActive(true); RefreshJobs();Canvas.ForceUpdateCanvases();jobScroll.verticalNormalizedPosition=1;
        }
        private void BuildJobs()
        {
            journal = Rect("JobsPanel", transform, new Vector2(720, 880), Vector2.zero).gameObject;
            journal.AddComponent<Image>().color = new Color(.025f, .04f, .065f, .99f);
            Label("JobsTitle", journal.transform, "DNEVNIK / ULIČNI POSLOVI", 28, 25, 50);
            jobDetails = Label("JobDetails", journal.transform, "", 20, 100, 560);
            jobDetails.alignment = TextAnchor.UpperLeft; jobDetails.supportRichText = false;
            RectTransform viewport=(RectTransform)jobDetails.transform.parent;
            viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            jobScroll=viewport.gameObject.AddComponent<ScrollRect>();jobScroll.viewport=viewport;jobScroll.content=jobDetails.rectTransform;
            jobScroll.horizontal=false;jobScroll.vertical=true;jobScroll.inertia=false;jobScroll.movementType=ScrollRect.MovementType.Clamped;jobScroll.scrollSensitivity=24;
            RectTransform detail=jobDetails.rectTransform;detail.anchorMin=new Vector2(0,1);detail.anchorMax=Vector2.one;detail.pivot=new Vector2(.5f,1);detail.sizeDelta=new Vector2(-20,0);detail.anchoredPosition=Vector2.zero;
            jobDetails.verticalOverflow=VerticalWrapMode.Overflow;detail.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Label("JobsScrollHint",journal.transform,"Prevuci tekst / točkić miša: ceo brifing.",16,660,24);
            acceptJob = Button("AcceptJob", journal.transform, "PRIHVATI POSAO", new Vector2(-160, -280), () =>
            {
                CityMissionDirector director = owner.GetComponent<CityMissionDirector>();
                owner.ClosePanel();
                if (director != null && director.TryAcceptStreetJob()) return;
                owner.OpenPanel(); OpenJobs(); ShowFeedback("Ponuda trenutno nije dostupna. Proveri ugovore, rok i stanje lika.");
            });
            cancelJob = Button("CancelJob", journal.transform, "OTKAŽI / BEZ ISPLATE", new Vector2(160, -280), () =>
            {
                CityGameBootstrap game = owner.GetComponent<CityGameBootstrap>();
                if (game != null && game.Vitals.CanAct)
                {
                    if(owner.Progression.GardenOrder.IsActive)
                    { if(owner.Progression.GardenOrder.Cancel())ShowFeedback(owner.Progression.GardenOrder.Message); }
                    else if(game.Director.Jobs.Cancel())ShowFeedback("Posao otkazan; bez isplate.");
                }
                RefreshJobs();
            });
            Button("BackFromJobs", journal.transform, "NAZAD U TELEFON", new Vector2(0, -365), Open);
            journal.SetActive(false);
        }
        private void RefreshJobs()
        {
            CityMissionDirector director = owner.GetComponent<CityMissionDirector>();
            CityStreetJobs jobs = director != null ? director.Jobs : null;
            if (jobs == null) { jobDetails.text = "Učitavanje dnevnika..."; acceptJob.interactable = cancelJob.interactable = false; return; }
            bool unlocked = director.StreetJobsUnlocked;
            var order=owner.Progression.GardenOrder;
            if(order.IsActive)
            {
                string phase=order.HasCargo?"1 / PREDAJA: izađi iz šteka i predaj paket Kuretu kod markera 2.":"2 / POTVRDA: vrati se u štek, pult PORUDŽBINE, i pritisni E za isplatu.";
                jobDetails.text=$"PORUDŽBINA IZ ŠTEKA / KURE\n{CityGarden.VarietyName(order.Variety)} × 2 / roba je već skinuta iz zaliha\n\n{phase}\n\nRok: {Math.Ceiling(order.RemainingSeconds)} s igre\nIsplata tek u šteku: {order.CashReward} DIN + 3 REP\n\n{order.BlockReason}\n{order.Message}\n\nOTKAZ, SMRT I ISTEK ROKA gube paket/potvrdu; nema povraćaja robe ni isplate. Telefon pauzira rok.\n\nNapredak i preostalo vreme nastavljaju posle učitavanja.";
                acceptJob.interactable=false;cancelJob.interactable=true;return;
            }
            if(!unlocked)
            {
                var run=director.Run;
                jobDetails.text=$"GLAVNI UGOVOR {director.ContractNumber}/{director.ContractCount}\n{run.Contract.Title}\n\n{run.Contract.Briefing}\n\nCilj {Math.Min(3,run.ObjectiveIndex+1)}/3 / {run.State}\n{run.Message}\n\nE: prihvati ili potvrdi cilj kod plavog markera.\nM: mapa ulica / P: telefon / ESC: povratak.\nN: sledeći ugovor posle završetka / R: novi pokušaj posle neuspeha.\n\nTrening je blokiran tokom aktivne dostave.\nSporedni poslovi dolaze posle sva 3 glavna ugovora.\nTelefon pauzira igru dok čitaš.";
                acceptJob.interactable=cancelJob.interactable=false;return;
            }
            string route = "";
            for (int i = 0; i < 3; i++)
            {
                string state = !jobs.IsActive ? "PONUDA" : i < jobs.ObjectiveIndex ? "GOTOVO" : i == jobs.ObjectiveIndex ? "SLEDEĆE" : "ČEKA";
                route += $"[{state}] {i + 1}. {jobs.Offer.ObjectiveTitle(i)} / marker {jobs.Offer.StationAt(i) + 1}\n";
            }
            string availability = !unlocked ? "Završi sva 3 glavna ugovora da otključaš posao." : jobs.IsActive
                ? $"Aktivno / preostalo {Math.Ceiling(jobs.RemainingSeconds)} sekundi igre"
                : jobs.CooldownSeconds > 0 ? $"Novi poziv za {Math.Ceiling(jobs.CooldownSeconds)} sekundi igre" : "Ponuda je spremna za prihvatanje.";
            if (Time.unscaledTime < feedbackUntil) availability = feedback;
            else if(unlocked&&jobs.BlockReason.Length>0)availability=jobs.BlockReason+(jobs.IsActive?" Rok nastavlja kada zatvoriš telefon.":"");
            jobDetails.text = $"Glavni ugovori: {owner.Progression.CompletedContracts}/3\nZavršeni poslovi: {jobs.CompletedJobs} / poglavlje {jobs.StoryChapter}/3\n{jobs.PhaseSummary}\n\n{jobs.Offer.Title}\n{jobs.Offer.Briefing}\n\n{route}\nNagrada: {jobs.Offer.CashReward} cash + {jobs.Offer.ReputationReward} REP\nRok ponude: {jobs.Offer.TimeLimit:0}s\n\n{availability}\nTelefon pauzira rok; zatvori ga za nastavak.\n\nPORUDŽBINE IZ ŠTEKA: {order.Completed} završeno. Pult PORUDŽBINE u šteku pakuje 2 komada izabranog strejna iz zaliha. V menja strejn. Prihvatanje skida robu; otkaz/istek roka je gube. Predaja kod markera 2, isplata nazad u šteku.";
            acceptJob.interactable = unlocked && !jobs.IsActive && jobs.CooldownSeconds <= 0 && jobs.BlockReason.Length==0;
            cancelJob.interactable = jobs.IsActive;
        }
        private void BuildCatalog()
        {
            catalog = Rect("CatalogPanel", transform, new Vector2(720, 880), Vector2.zero).gameObject;
            catalog.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.99f);
            Label("CatalogTitle", catalog.transform, "STREJN KATALOG / SEME I BUD", 25, 18, 30);
            catalogStatus = Label("CatalogStatus", catalog.transform, "", 15, 50, 18);
            for (int i = 0; i < catalogRows.Length; i++)
            {
                int index = i;
                Button row = Button("Variety_" + i, catalog.transform, "", new Vector2(0, 330 - 65 * i), () => { selectedPlant = index; owner.GetComponent<CityHideout>()?.SelectVariety(index); RefreshCatalog(); });
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 58);
                catalogRows[i] = row.GetComponentInChildren<Text>(); catalogRows[i].fontSize = 17;
                catalogRows[i].rectTransform.sizeDelta = new Vector2(630, 54); catalogButtons[i] = row;
            }
            Button buyer = Button("Buyer", catalog.transform, "", new Vector2(0, -205), () => { buyerIndex = (buyerIndex + 1) % CityGardenMarket.BuyerCount; RefreshCatalog(); });
            buyer.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 50); buyerText = buyer.GetComponentInChildren<Text>();
            buyerText.rectTransform.sizeDelta = new Vector2(630, 46);
            Button district = Button("District", catalog.transform, "", new Vector2(0, -265), () => { districtIndex = (districtIndex + 1) % CityGardenMarket.DistrictCount; RefreshCatalog(); });
            district.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 50); districtText = district.GetComponentInChildren<Text>();
            districtText.rectTransform.sizeDelta = new Vector2(630, 46);
            buyCatalog = Button("BuySeed", catalog.transform, "KUPI SEME", new Vector2(-160, -330), () => Act(() => owner.Progression.Garden.BuySeed(selectedPlant)));
            sellCatalog = Button("SellStock", catalog.transform, "PRODAJ 1", new Vector2(160, -330), () => Act(() => owner.Progression.Garden.Sell(selectedPlant, buyerIndex, districtIndex, 1)));
            Button("Back", catalog.transform, "NAZAD", new Vector2(-160, -400), () => { catalog.SetActive(false); content.SetActive(true); Refresh(); });
            Button("Identity", catalog.transform, "LIČNE KARTE / 16", new Vector2(160, -400), () => { selectedCard = selectedPlant; catalog.SetActive(false); encyclopedia.SetActive(true); RefreshCard(); });
            catalog.SetActive(false);
        }
        private void BuildEncyclopedia()
        {
            encyclopedia = Rect("PlantCards", transform, new Vector2(960, 880), Vector2.zero).gameObject;
            encyclopedia.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.99f);
            RectTransform art = Rect("BotanicalPortrait", encyclopedia.transform, new Vector2(420, 350), new Vector2(-235, 130));
            botanical = art.gameObject.AddComponent<RawImage>(); botanical.raycastTarget = false;
            botanicalAtlas = Resources.Load<Texture2D>("Art/BotanicalAtlas"); botanical.texture = botanicalAtlas;
            RectTransform detail = Rect("BotanicalDetails", encyclopedia.transform, new Vector2(410, 610), new Vector2(235, 80));
            plantDetail = ChildText(detail, "Text", "", 19); plantDetail.alignment = TextAnchor.UpperLeft; plantDetail.supportRichText = false;
            RectTransform notice = Rect("ImageProvenance", encyclopedia.transform, new Vector2(420, 110), new Vector2(-235, -145));
            ChildText(notice, "Text", "Reprezentativna AI ilustracija kanabisa.\nNije fotografija potvrđenog fenotipa.\nCene i ciklusi su izmišljena pravila igre.", 19);
            Button("PreviousCard", encyclopedia.transform, "PRETHODNA", new Vector2(-160, -300), () => { selectedCard = (selectedCard + CityPlantEncyclopedia.Count - 1) % CityPlantEncyclopedia.Count; RefreshCard(); });
            Button("NextCard", encyclopedia.transform, "SLEDEĆA", new Vector2(160, -300), () => { selectedCard = (selectedCard + 1) % CityPlantEncyclopedia.Count; RefreshCard(); });
            Button("BackToCatalog", encyclopedia.transform, "NAZAD U KATALOG", new Vector2(0, -400), OpenCatalog);
            encyclopedia.SetActive(false);
        }
        private void RefreshCard()
        {
            CityPlantCard card = CityPlantEncyclopedia.Get(selectedCard);
            bool illustrated = botanicalAtlas != null && card.AtlasIndex >= 0;
            botanical.enabled = illustrated;
            if (illustrated) botanical.uvRect = new Rect((card.AtlasIndex % 2) * 0.5f + .10f, (3 - card.AtlasIndex / 2) * 0.25f, .30f, .25f);
            plantDetail.text = $"{selectedCard + 1}/{CityPlantEncyclopedia.Count}  {card.Name}\n{card.ScientificName}\n\nPoreklo: {card.Region}\n\n{card.Story}\n\n{(card.IsTradable ? "Dostupnost i cena: katalog semena." : "Informativna botanička karta.")}\n\nIzvor: {card.SourceUrl}" + (illustrated ? "" : "\n\nIlustracija za ovu vrstu još nije dodata.");
        }
        private void RefreshCatalog()
        {
            catalogStatus.text = Time.unscaledTime < feedbackUntil ? feedback : owner.SaveStatus;
            CityGarden garden = owner.Progression.Garden;
            for (int i = 0; i < catalogRows.Length; i++)
            {
                CityPlantDefinition p = CityPlantCatalog.Get(i);
                catalogRows[i].text = $"{(i == selectedPlant ? "> " : "")}{p.Name} / seme {p.SeedPrice} / {p.GrowSeconds / 3600:0}h / prinos {p.Yield}\n" +
                    (garden.IsUnlocked(i) ? $"Seme {garden.Seeds(i)} / bud {garden.Stock(i)}" : $"ZAKLJUČANO / REP {p.RequiredReputation}");
                catalogRows[i].color = garden.IsUnlocked(i) ? Color.white : Color.gray;
                catalogButtons[i].GetComponent<Image>().color = i == selectedPlant ? new Color(.10f, .43f, .35f) : garden.IsUnlocked(i) ? new Color(.06f, .24f, .30f) : new Color(.09f, .12f, .16f);
            }
            buyerText.text = $"Kupac: {CityGardenMarket.BuyerName(buyerIndex)} / REP {CityGardenMarket.RequiredReputation(buyerIndex)}";
            districtText.text = $"Mesto: {CityGardenMarket.DistrictName(districtIndex)} / ponuda {garden.Quote(selectedPlant, buyerIndex, districtIndex)} po komadu";
            buyCatalog.interactable = garden.IsUnlocked(selectedPlant) && owner.Progression.Session.Cash >= CityPlantCatalog.Get(selectedPlant).SeedPrice;
            sellCatalog.interactable = garden.Stock(selectedPlant) > 0 && owner.Progression.Session.Reputation >= CityGardenMarket.RequiredReputation(buyerIndex);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 offset)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = offset;
            return rect;
        }
        private static Text ChildText(RectTransform parent, string name, string value, int size)
        {
            RectTransform rect = Rect(name, parent, parent.sizeDelta - new Vector2(20, 4), Vector2.zero);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = size; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }
        private static Text Label(string name, Transform parent, string value, int size, float top, float height)
        {
            RectTransform rect = Rect(name, parent, new Vector2(650, height), new Vector2(0, 440 - top - height / 2));
            return ChildText(rect, name + "Text", value, size);
        }
        private static Button Button(string name, Transform parent, string title, Vector2 offset, Action action)
        {
            RectTransform rect = Rect(name, parent, new Vector2(300, 56), offset);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.06f, 0.32f, 0.38f);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ChildText(rect, "Text", title, 19);
            button.onClick.AddListener(() => action());
            return button;
        }
        private void OnDestroy() { if (ownedEventSystem != null) Destroy(ownedEventSystem); }
    }
}
