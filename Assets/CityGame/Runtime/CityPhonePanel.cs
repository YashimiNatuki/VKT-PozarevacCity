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
        private Button buy, plant, harvest, sell, recruit, claim;
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
        public bool IsOpen => (content != null && content.activeSelf) || (catalog != null && catalog.activeSelf) || (encyclopedia != null && encyclopedia.activeSelf);

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
            buy = Button("Buy", economy.transform, "Seme / 40 cash", new Vector2(-160, 85), () => Act(owner.Progression.BuySeed));
            plant = Button("Plant", economy.transform, "Posadi / 30 sekundi igre", new Vector2(160, 85), () => Act(owner.Progression.Plant));
            harvest = Button("Harvest", economy.transform, "Uberi / 3 komada", new Vector2(-160, 10), () => Act(owner.Progression.Harvest));
            sell = Button("Sell", economy.transform, "Prodaj / 35 po komadu", new Vector2(160, 10), () => Act(owner.Progression.Sell));
            recruit = Button("Recruit", economy.transform, "Angažuj / 250 cash", new Vector2(-160, -65), () => Act(owner.Progression.Recruit));
            claim = Button("Claim", economy.transform, "Preuzmi blok / 750 cash", new Vector2(160, -65), () => Act(owner.Progression.ClaimTerritory));
            status = Label("Status", content.transform, "", 18, 645, 35);
            Button("Catalog", content.transform, "KATALOG / BAŠTA", new Vector2(-160, -290), OpenCatalog);
            Button("StreetJob", content.transform, "ULIČNI POSAO", new Vector2(160, -290), () =>
            {
                CityMissionDirector director = owner.GetComponent<CityMissionDirector>();
                owner.ClosePanel();
                if (director == null || !director.TryAcceptStreetJob())
                { owner.OpenPanel(); ShowFeedback("Ulični poslovi: završi 3 ugovora, zatim sačekaj da se ponuda obnovi."); }
            });
            Button("Close", content.transform, "Nazad u igru / P ili Esc", new Vector2(0, -350), owner.ClosePanel);
            Label("Help", content.transform, "Štek: vrata kod trećeg markera. Četiri saksije / E: nega / V: vrsta.\nBašta: stvarni sati. Probna biljka: vreme igre. Ekipa: REP 30.", 16, 820, 45);
            BuildCatalog();
            BuildEncyclopedia();
            Close();
        }

        private void Act(Func<bool> action) { if (!owner.Perform(action)) ShowFeedback("Akcija nije dostupna: proveri uslove, prostor i novac."); Refresh(); }
        private void ShowFeedback(string message) { feedback = message; feedbackUntil = Time.unscaledTime + 5; status.text = message; }
        public void Open() { backdrop.SetActive(true); encyclopedia.SetActive(false); catalog.SetActive(false); content.SetActive(true); Refresh(); }
        public void Close() { if (content != null) content.SetActive(false); if (catalog != null) catalog.SetActive(false); if (encyclopedia != null) encyclopedia.SetActive(false); if (backdrop != null) backdrop.SetActive(false); }
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
            summary.text = $"{p.Callsign} / CASH {p.Session.Cash} / REP {p.Session.Reputation}\nUgovori {p.CompletedContracts}/3 / Crew {p.Crew}/3 / Blocks {p.Territories}/3\nProbna biljka: seme {p.Seeds}/30 / zaliha {p.Stock}/30\n" +
                (p.HarvestReady ? "Spremno za berbu" : p.IsGrowing ? $"Rast: {Math.Ceiling(p.GrowSeconds)} sekundi igre do berbe" : "Probna saksija je prazna") +
                $"\nŠtek: 4 saksije / ukupno berbi {p.Garden.Harvests}" + (p.Territories == 3 ? "\nPo-Blok-C: kampanja blokova završena." : "");
            economy.SetActive(p.PhoneUnlocked);
            buy.interactable = p.Seeds < 30 && p.Session.Cash >= CityProgression.SeedPrice;
            plant.interactable = p.Seeds > 0 && !p.IsGrowing && !p.HarvestReady;
            harvest.interactable = p.HarvestReady && p.Stock <= 27;
            sell.interactable = p.Stock > 0;
            recruit.interactable = p.Session.Reputation >= 30 && p.Crew < 3 && p.Session.Cash >= CityProgression.CrewPrice;
            claim.interactable = p.CompletedContracts == 3 && p.Crew >= 2 && p.Territories < 3 && p.Session.Cash >= CityProgression.TerritoryPrice;
            status.text = Time.unscaledTime < feedbackUntil ? feedback : owner.SaveStatus;
            if (catalog != null && catalog.activeSelf) RefreshCatalog();
        }

        public void OpenCatalog()
        {
            if (!owner.Progression.PhoneUnlocked) return;
            encyclopedia.SetActive(false);
            content.SetActive(false); catalog.SetActive(true); RefreshCatalog();
        }
        private void BuildCatalog()
        {
            catalog = Rect("CatalogPanel", transform, new Vector2(720, 880), Vector2.zero).gameObject;
            catalog.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.99f);
            Label("CatalogTitle", catalog.transform, "KATALOG BAŠTE / IZABERI VRSTU", 25, 18, 30);
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
            ChildText(notice, "Text", "Originalna AI botanička ilustracija.\nFiktivni naziv iz igre; stvarna botanička analogija.\nInformativne vrste nisu roba za prodaju.", 19);
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
                    (garden.IsUnlocked(i) ? $"Seme {garden.Seeds(i)} / roba {garden.Stock(i)}" : $"ZAKLJUČANO / REP {p.RequiredReputation}");
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
