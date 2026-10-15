using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityGardenVendor : CityInteractable
    {
        private CityHideout hideout;
        private bool selling;
        public void Configure(CityHideout owner, bool sell) { hideout = owner; selling = sell; }
        public override string Prompt => selling ? hideout.MarketPrompt : hideout.SeedPrompt;
        public override bool TryInteract(CitySession session) => ReferenceEquals(session, hideout.Progression.Session) && hideout.Trade(selling);
    }
    public sealed class CityHideout : MonoBehaviour
    {
        private CityGameBootstrap game;
        private CityProgressionController progression;
        private GameObject room;
        private Vector3 roomSpawn;
        private Material wallMaterial, floorMaterial, potMaterial, leafMaterial;
        private readonly Transform[] plants = new Transform[CityGarden.PlotCount];
        private readonly TextMesh[] labels = new TextMesh[CityGarden.PlotCount];
        private float refreshCountdown;
        private Transform view;
        private TextMesh catalogBoard;
        public int SelectedBuyer { get; private set; }
        public int SelectedDistrict { get; private set; }
        public bool IsInside { get; private set; }
        public int SelectedVariety { get; private set; }
        public Transform Entrance { get; private set; }
        public Transform Exit { get; private set; }
        public Transform PlotTransform(int index) => labels[index].transform.parent;
        public CityProgression Progression => progression.Progression;

        public void Initialize(CityGameBootstrap owner, CityProgressionController controller, Vector3 contactGround)
        {
            game = owner; progression = controller;
            view = game.Player.GetComponentInChildren<Camera>().transform;
            Material template = Resources.Load<Material>("CityMissionMarker");
            if (template == null) throw new InvalidOperationException("Hideout requires the existing marker material.");
            wallMaterial = Material(template, new Color(0.13f, 0.19f, 0.23f));
            floorMaterial = Material(template, new Color(0.22f, 0.25f, 0.28f));
            potMaterial = Material(template, new Color(0.38f, 0.22f, 0.12f));
            leafMaterial = Material(template, new Color(0.16f, 0.65f, 0.35f));
            Transform globeRoot = game.Player.parent;
            Entrance = Box("VKT_Stek_Ulaz", globeRoot, contactGround + new Vector3(4, 1.1f, 0), new Vector3(1.4f, 2.2f, 0.3f), wallMaterial).transform;
            Entrance.gameObject.AddComponent<CityHideoutDoor>().Configure(this, true);
            Entrance.gameObject.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
            Text(Entrance, "ŠTEK / BAŠTA\n[E] Ulaz", new Vector3(0, 1.6f, 0));
            room = new GameObject("VKT_Stek_Interior");
            room.transform.SetParent(globeRoot, false);
            room.transform.position = contactGround + Vector3.up * 1000;
            room.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
            Box("Pod", room.transform, Vector3.zero, new Vector3(10, 0.3f, 12), floorMaterial, true);
            Box("ZidLevo", room.transform, new Vector3(-5, 1.6f, 0), new Vector3(0.3f, 3.2f, 12), wallMaterial, true);
            Box("ZidDesno", room.transform, new Vector3(5, 1.6f, 0), new Vector3(0.3f, 3.2f, 12), wallMaterial, true);
            Box("ZidPozadi", room.transform, new Vector3(0, 1.6f, 6), new Vector3(10, 3.2f, 0.3f), wallMaterial, true);
            Box("ZidNapred", room.transform, new Vector3(0, 1.6f, -6), new Vector3(10, 3.2f, 0.3f), wallMaterial, true);
            Box("Plafon", room.transform, new Vector3(0, 3.3f, 0), new Vector3(10, 0.3f, 12), floorMaterial, true);
            GameObject lamp = new GameObject("Svetlo"); lamp.transform.SetParent(room.transform, false); lamp.transform.localPosition = new Vector3(0, 2.7f, 0);
            Light light = lamp.AddComponent<Light>(); light.type = LightType.Point; light.range = 16; light.intensity = 6; light.color = new Color(0.8f, 0.95f, 1);
            Exit = Box("Izlaz", room.transform, new Vector3(0, 1.25f, -5.7f), new Vector3(1.4f, 2.2f, 0.3f), wallMaterial, true).transform;
            Exit.gameObject.AddComponent<CityHideoutDoor>().Configure(this, false);
            Text(Exit, "[E] Nazad u Požarevac", new Vector3(0, 1.3f, 0));
            roomSpawn = new Vector3(0, 0.2f, -3.5f);
            for (int i = 0; i < CityGarden.PlotCount; i++)
            {
                Vector3 at = new Vector3(i % 2 == 0 ? -2 : 2, 0.6f, i < 2 ? 0 : 3);
                GameObject pot = Box("Saksija_" + (i + 1), room.transform, at, new Vector3(1.2f, 0.9f, 1.2f), potMaterial, true);
                pot.AddComponent<CityGardenPlot>().Configure(this, i);
                GameObject stem = Box("Biljka", pot.transform, new Vector3(0, 1, 0), new Vector3(0.3f, 1.2f, 0.3f), leafMaterial, true);
                plants[i] = stem.transform;
                for (int leafIndex = 0; leafIndex < 3; leafIndex++)
                {
                    GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaf.name = "List_" + leafIndex; leaf.transform.SetParent(stem.transform, false);
                    leaf.transform.localPosition = new Vector3(leafIndex % 2 == 0 ? 0.7f : -0.7f, leafIndex * 0.25f, 0);
                    leaf.transform.localScale = new Vector3(2.2f, 0.2f, 1.1f);
                    leaf.GetComponent<Renderer>().sharedMaterial = leafMaterial;
                    Destroy(leaf.GetComponent<Collider>());
                }
                labels[i] = Text(pot.transform, "", new Vector3(0, i < 2 ? 1.7f : 2.6f, 0));
            }
            GameObject seedCounter = Box("KatalogSemena", room.transform, new Vector3(-3.5f, 0.7f, -3), new Vector3(1.2f, 1.3f, 1), potMaterial, true);
            seedCounter.AddComponent<CityGardenVendor>().Configure(this, false);
            Text(seedCounter.transform, "KATALOG / [E] Kupi seme\n[V] vrsta", new Vector3(0, 1.1f, 0));
            GameObject marketCounter = Box("OtkupCveca", room.transform, new Vector3(3.5f, 0.7f, -3), new Vector3(1.2f, 1.3f, 1), potMaterial, true);
            marketCounter.AddComponent<CityGardenVendor>().Configure(this, true);
            Text(marketCounter.transform, "OTKUP / [E] Prodaj izabrano\n[B] kupac / [J] lokacija", new Vector3(0, 1.1f, 0));
            catalogBoard = Text(room.transform, "", new Vector3(0, 2.6f, 5.6f));
            catalogBoard.characterSize = 0.022f;
            room.SetActive(false);
        }

        public void SelectVariety(int index)
        {
            CityPlantCatalog.Get(index); // Locked varieties remain selectable for catalog previews.
            SelectedVariety = index;
        }

        public void ResetAfterRespawn()
        {
            IsInside = false;
            if (room != null) room.SetActive(false);
        }

        public bool Travel(bool enter)
        {
            if ((game.Vitals != null && !game.Vitals.CanAct) || game.Director.IsPaused || enter == IsInside || (enter && !Progression.PhoneUnlocked)) return false;
            if (enter) { room.SetActive(true); IsInside = true; Teleport(room.transform.TransformPoint(roomSpawn)); }
            else { Teleport(Entrance.position + new Vector3(0, -0.9f, -2.5f)); IsInside = false; room.SetActive(false); }
            game.Audio.Speak(enter ? CityVoice.Mila : CityVoice.Vuk);
            return true;
        }

        private void Teleport(Vector3 point)
        {
            CharacterController body = game.Player.GetComponent<CharacterController>();
            body.enabled = false; game.Player.position = point; body.enabled = true;
            game.Player.GetComponent<CityPlayerController>().ResetView();
            Physics.SyncTransforms();
        }

        public bool Act(int index)
        {
            if (!IsInside || game.Director.IsPaused || !Progression.PhoneUnlocked) return false;
            CityGarden garden = Progression.Garden;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            garden.Advance(now);
            bool success;
            if (!garden.Plot(index).planted) success = progression.Perform(() => garden.PlantFromInventory(index, SelectedVariety, now));
            else if (garden.IsReady(index)) success = progression.Perform(() => garden.Harvest(index, now));
            else success = progression.Perform(() => garden.Water(index, now));
            progression.Notify(success ? "Mila: Završeno. Stanje bašte je sačuvano." : "Mila: Nega nije potrebna, nema kupljenog semena ili je skladište puno.");
            Refresh();
            return success;
        }

        public string Prompt(int index)
        {
            CityGarden garden = Progression.Garden;
            CityGardenPlotData p = garden.Plot(index);
            if (!p.planted) return $"[E] Posadi: {CityGarden.VarietyName(SelectedVariety)} / semena: {garden.Seeds(SelectedVariety)} | V: vrsta / seme kupi na pultu";
            if (garden.IsReady(index)) return p.catalogPlant ? $"[E] Uberi {CityGarden.VarietyName(p.variety)} / +{CityPlantCatalog.Get(p.variety).Yield} u skladište" : $"[E] Uberi {CityGarden.VarietyName(p.variety)} / +{CityGarden.HarvestValue(p.variety)} cash";
            if (garden.NeedsWater(index)) return "[E] Zalij — suva zemlja, rast je zaustavljen";
            return $"Zaliveno još {Math.Ceiling(p.moistureSeconds / 60)} min | Rast {p.grownSeconds / CityGarden.GrowDuration(p.variety):P0}";
        }

        private void Update()
        {
            if (!IsInside) return;
            if (!game.Director.IsPaused && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
            { SelectVariety((SelectedVariety + 1) % CityPlantCatalog.Count); progression.Notify("Vrsta: " + CityGarden.VarietyName(SelectedVariety)); }
            if (!game.Director.IsPaused && UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame) { SelectedBuyer = (SelectedBuyer + 1) % CityGardenMarket.BuyerCount; progression.Notify(MarketPrompt); }
                if (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame) { SelectedDistrict = (SelectedDistrict + 1) % CityGardenMarket.DistrictCount; progression.Notify(MarketPrompt); }
            }
            refreshCountdown -= Time.unscaledDeltaTime;
            if (refreshCountdown <= 0) { Refresh(); refreshCountdown = 0.25f; }
        }
        public string SeedPrompt => $"[E] Kupi {CityGarden.VarietyName(SelectedVariety)} / {CityPlantCatalog.Get(SelectedVariety).SeedPrice} cash / REP {CityPlantCatalog.Get(SelectedVariety).RequiredReputation} / semena {Progression.Garden.Seeds(SelectedVariety)}";
        public string MarketPrompt => $"[E] Prodaj {CityGarden.VarietyName(SelectedVariety)} / {CityGardenMarket.BuyerName(SelectedBuyer)} / {CityGardenMarket.DistrictName(SelectedDistrict)} / {Progression.Garden.Quote(SelectedVariety, SelectedBuyer, SelectedDistrict)} cash/kom / zaliha {Progression.Garden.Stock(SelectedVariety)} / REP {CityGardenMarket.RequiredReputation(SelectedBuyer)}";
        public bool Trade(bool sell)
        {
            if (!IsInside || game.Director.IsPaused || !Progression.PhoneUnlocked) return false;
            CityGarden garden = Progression.Garden;
            bool success = progression.Perform(() => sell ? garden.Sell(SelectedVariety, SelectedBuyer, SelectedDistrict, garden.Stock(SelectedVariety)) : garden.BuySeed(SelectedVariety));
            progression.Notify(success ? (sell ? "Prodaja završena. Novac i skladište sačuvani." : "Seme kupljeno. Posadi ga u praznu saksiju.") : "Trgovina odbijena: proveri novac, reputaciju i zalihe.");
            Refresh();
            return success;
        }
        private void Refresh()
        {
            System.Text.StringBuilder catalog = new System.Text.StringBuilder("UKRASNE BILJKE / KATALOG\n");
            for (int variety = 0; variety < CityPlantCatalog.Count; variety++)
            {
                CityPlantDefinition plant = CityPlantCatalog.Get(variety);
                catalog.Append(variety == SelectedVariety ? "> " : "  ").Append(plant.Name).Append(" / ").Append(plant.SeedPrice).Append(" cash / ")
                    .Append(plant.GrowSeconds / 3600).Append("h / rod ").Append(plant.Yield).Append(" / seme ").Append(Progression.Garden.Seeds(variety)).Append(" / roba ").Append(Progression.Garden.Stock(variety))
                    .Append(Progression.Garden.IsUnlocked(variety) ? " / dostupno" : " / REP " + plant.RequiredReputation).Append('\n');
            }
            catalog.Append("V: izbor / levi pult: kupovina / desni: prodaja\n").Append(CityGardenMarket.BuyerName(SelectedBuyer)).Append(" / ").Append(CityGardenMarket.DistrictName(SelectedDistrict))
                .Append(" / ").Append(Progression.Garden.Quote(SelectedVariety, SelectedBuyer, SelectedDistrict)).Append(" cash/kom / B: kupac / J: lokacija");
            catalogBoard.text = catalog.ToString();
            catalogBoard.transform.rotation = view.rotation;
            for (int i = 0; i < plants.Length; i++)
            {
                CityGardenPlotData p = Progression.Garden.Plot(i);
                plants[i].gameObject.SetActive(p.planted);
                float plantHeight = 0.2f + (float)(p.grownSeconds / CityGarden.GrowDuration(p.variety));
                plants[i].localScale = new Vector3(0.3f, plantHeight, 0.3f);
                plants[i].localPosition = new Vector3(0, 0.5f + plantHeight * 0.5f, 0);
                string state = !p.planted ? "Prazno / E: sadnja" : Progression.Garden.IsReady(i) ? "SPREMNO / E: berba" :
                    Progression.Garden.NeedsWater(i) ? "SUVO / E: zalivanje" : $"Rast {p.grownSeconds / CityGarden.GrowDuration(p.variety):P0} / voda {Math.Ceiling(p.moistureSeconds / 60)} min";
                labels[i].text = $"{i + 1:00} / {(p.planted ? CityGarden.VarietyName(p.variety) : CityGarden.VarietyName(SelectedVariety))}\n{state}";
                labels[i].color = !p.planted ? Color.gray : Progression.Garden.IsReady(i) ? Color.cyan : Progression.Garden.NeedsWater(i) ? new Color(1, 0.75f, 0.25f) : Color.white;
                labels[i].transform.rotation = view.rotation;
            }
        }
        private static Material Material(Material source, Color color)
        { Material m = new Material(source); m.color = color; m.SetColor("_EmissionColor", color * 0.15f); return m; }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool local = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(parent, false);
            if (local) obj.transform.localPosition = position; else obj.transform.position = position;
            obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
        private static TextMesh Text(Transform parent, string text, Vector3 at)
        {
            GameObject obj = new GameObject("Natpis"); obj.transform.SetParent(parent, false); obj.transform.localPosition = at;
            TextMesh mesh = obj.AddComponent<TextMesh>(); mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 32; mesh.characterSize = 0.035f; mesh.anchor = TextAnchor.MiddleCenter; mesh.text = text;
            obj.GetComponent<Renderer>().sharedMaterial = mesh.font.material;
            return mesh;
        }
        private void OnDestroy()
        {
            if (room != null) Destroy(room);
            if (Entrance != null) Destroy(Entrance.gameObject);
            foreach (Material m in new[] { wallMaterial, floorMaterial, potMaterial, leafMaterial }) if (m != null) Destroy(m);
        }
    }
}
