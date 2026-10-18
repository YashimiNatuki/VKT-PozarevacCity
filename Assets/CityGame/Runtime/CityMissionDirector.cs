using System;
using CesiumForUnity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public sealed class CityMissionDirector : MonoBehaviour
    {
        private readonly CityMission[] stations = new CityMission[3];
        private readonly Renderer[] stationRenderers = new Renderer[3];
        private readonly TextMesh[] stationLabels = new TextMesh[3];
        private readonly Material[] materials = new Material[3];
        private CityMissionContract[] contracts;
        private CityHud hud;
        private Transform player;
        private Camera view;
        private CityMissionRun run;
        private int contractIndex;
        private int displayedRevision = -1;
        private float presentationCountdown;
        private float coolingCountdown;
        private float resumeTimeScale = 1f;
        private int pauseChangedFrame = -1;
        private bool overlayOpen;
        private bool overlayPreviousPause;
        private CityPlayerVitals vitals;
        private CityLifestyle lifestyle;
        private CityGameAudio audioFeedback;
        private bool applicationSuspended;
        private bool CanAct => !applicationSuspended&&(vitals == null || vitals.CanAct);
        public string ContactBlockReason=>lifestyle!=null&&lifestyle.Wanted?"POTERA / prvo izgubi policiju.":
            lifestyle!=null&&(lifestyle.IsTraining||lifestyle.IsConditioning||lifestyle.IsBoxing)?"TRENING / završi ili prekini seriju pre misije.":"";
        public void BindLifestyle(CityLifestyle state,CityGameAudio audio)
        {
            if(lifestyle!=null&&lifestyle!=state)throw new InvalidOperationException("Lifestyle already bound.");
            lifestyle=state??throw new ArgumentNullException(nameof(state));audioFeedback=audio;Jobs.BindLifestyle(state);
        }
        public void BindVitals(CityPlayerVitals value) { vitals = value; }
        public void FailCurrent(string reason) { Jobs?.Cancel(reason); if (run != null) { run.Abort(reason); RefreshPresentation(); ProgressChanged?.Invoke(); } }
        public event Action<int> ContractCompleted;
        public event Action ProgressChanged;

        public CitySession Session { get; private set; }
        public CityMissionRun Run => run;
        public CityStreetJobs Jobs { get; private set; }
        public bool StreetJobsUnlocked => run != null && run.State == CityMissionState.Completed && contractIndex == contracts.Length - 1;
        public int ContractNumber => contractIndex + 1;
        public int ContractCount => contracts?.Length ?? 0;
        public CityMissionSaveData CaptureMission() => run?.Snapshot(ContractNumber);
        public void RestoreMission(CityMissionSaveData saved)
        {
            if(saved==null)return;
            if(contracts==null)throw new InvalidOperationException("Initialize the director before restoring.");
            contractIndex=saved.contractNumber-1;
            if(contractIndex<0||contractIndex>=contracts.Length)throw new ArgumentException("Unknown saved contract.");
            run=CityMissionRun.Restore(contracts[contractIndex],Session,saved);
            displayedRevision=-1; RefreshPresentation();
        }
        public bool IsPaused { get; private set; }
        public bool PauseChangedThisFrame=>pauseChangedFrame==Time.frameCount;
        public void SuppressPauseThisFrame(){pauseChangedFrame=Time.frameCount;}

        public CityMission StationAt(int index)
        {
            if (index < 0 || index >= stations.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return stations[index];
        }

        public void Initialize(CitySession session, CityHud cityHud, Transform cityPlayer,
            Camera camera, Transform worldRoot, Vector3[] positions, int completedContracts = 0)
        {
            if (Session != null)
                throw new InvalidOperationException("The mission director is already initialized.");
            if (positions == null || positions.Length != 3)
                throw new ArgumentException("Three grounded stations are required.");
            Session = session ?? throw new ArgumentNullException(nameof(session));
            hud = cityHud != null ? cityHud : throw new ArgumentNullException(nameof(cityHud));
            player = cityPlayer != null ? cityPlayer : throw new ArgumentNullException(nameof(cityPlayer));
            view = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            contracts = CreateContracts();
            Jobs = new CityStreetJobs(Session);
            if (completedContracts < 0 || completedContracts > contracts.Length) throw new ArgumentOutOfRangeException(nameof(completedContracts));
            contractIndex = Math.Min(completedContracts, contracts.Length - 1);
            run = completedContracts == contracts.Length ? CityMissionRun.RestoreCompleted(contracts[contractIndex], Session) : new CityMissionRun(contracts[contractIndex], Session);
            CreateMaterials();
            for (int i = 0; i < stations.Length; i++)
                CreateStation(i, positions[i], worldRoot);
            SetCursor(true);
            RefreshPresentation();
        }

        public bool IsStationAvailable(int index) => CanAct && !IsPaused && ContactBlockReason.Length==0 && run != null && (Jobs.IsActive ? Jobs.BlockReason.Length==0&&index == Jobs.Offer.StationAt(Jobs.ObjectiveIndex) : run.CanVisit(index));
        public bool IsStationComplete(int index)
        {
            if (Jobs != null && Jobs.IsActive)
            {
                for (int objective = 0; objective < Jobs.ObjectiveIndex; objective++)
                    if (Jobs.Offer.StationAt(objective) == index) return true;
                return false;
            }
            return run != null && run.ObjectiveIndex > index;
        }

        public bool TryAcceptStreetJob()
        {
            if (!CanAct || IsPaused || !StreetJobsUnlocked) return false;
            if(!Jobs.TryAccept()){hud.ShowNotification(Jobs.Message,5);audioFeedback?.PlayCue(CityAudioCue.Failure);return false;}
            hud.ShowNotification(Jobs.Message, 6f);
            audioFeedback?.PlayCue(CityAudioCue.Checkpoint);
            RefreshPresentation();
            return true;
        }

        public string PromptFor(int index)
        {
            if (run == null) return "City terrain is loading";
            if (IsPaused) return "Paused - Esc to resume";
            if(ContactBlockReason.Length>0)return ContactBlockReason;
            if (Jobs.IsActive)
            {
                if(Jobs.BlockReason.Length>0)return Jobs.BlockReason;
                return index == Jobs.Offer.StationAt(Jobs.ObjectiveIndex) ? "[E] " + Jobs.Offer.ObjectiveTitle(Jobs.ObjectiveIndex) : "Sporedni posao: prati plavi marker";
            }
            if (run.State == CityMissionState.Failed) return run.Message;
            if (run.State == CityMissionState.Completed)
                return contractIndex + 1 < contracts.Length ? "Contract complete - N for next contract" : "Kampanja završena / telefon: SPOR. POSAO";
            if (Session.Heat >= run.EffectiveHeatLimit) return "Too much heat - wait to cool down";
            if (index != run.ObjectiveIndex) return "Follow the cyan objective marker first";
            return run.State == CityMissionState.Ready
                ? $"[E] Accept {run.Contract.Title} and collect cargo"
                : $"[E] {run.Contract.ObjectiveTitle(index)}";
        }

        public bool TryInteract(int index)
        {
            if(!CanAct||IsPaused)return false;
            if(ContactBlockReason.Length>0){hud.ShowNotification(ContactBlockReason,4);audioFeedback?.PlayCue(CityAudioCue.Failure);return false;}
            if (Jobs != null && Jobs.IsActive)
            {
                if (!Jobs.TryVisit(index)){if(Jobs.BlockReason.Length>0)hud.ShowNotification(Jobs.BlockReason+" Rok i dalje teče.",4);return false;}
                hud.ShowNotification(Jobs.Message, 5f);
                audioFeedback?.PlayCue(Jobs.IsActive?CityAudioCue.Checkpoint:CityAudioCue.Success);
                RefreshPresentation();
                return true;
            }
            if (!CanAct || IsPaused || run == null || !run.TryVisit(index))
                return false;
            hud.ShowNotification(run.Message, 4f);
            audioFeedback?.PlayCue(run.State==CityMissionState.Completed?CityAudioCue.Success:CityAudioCue.Checkpoint);
            if (run.State == CityMissionState.Completed) ContractCompleted?.Invoke(contractIndex + 1);
            ProgressChanged?.Invoke();
            RefreshPresentation();
            return true;
        }

        public bool TryNextContract()
        {
            if (!CanAct || IsPaused || run == null || run.State != CityMissionState.Completed || contractIndex + 1 >= contracts.Length)
                return false;
            contractIndex++;
            run = new CityMissionRun(contracts[contractIndex], Session);
            ProgressChanged?.Invoke();
            displayedRevision = -1;
            hud.ShowNotification(run.Contract.Briefing, 5f);
            RefreshPresentation();
            return true;
        }

        public bool TryRetry()
        {
            if (!CanAct || IsPaused || run == null || !run.Retry()) return false;
            ProgressChanged?.Invoke();
            hud.ShowNotification("New attempt. Return to the cyan pickup marker.", 4f);
            RefreshPresentation();
            return true;
        }

        public bool TrySelectChallenge(CityMissionChallenge challenge)
        {
            if (!CanAct || IsPaused || run == null || !run.TrySelectChallenge(challenge)) return false;
            ProgressChanged?.Invoke();
            hud.ShowNotification(run.Message, 4f);
            RefreshPresentation();
            return true;
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            if (paused)
            {
                resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else Time.timeScale = resumeTimeScale;
            IsPaused = paused;
            pauseChangedFrame = Time.frameCount;
            SetCursor(!paused);
            RefreshPresentation();
        }

        public void SetOverlayOpen(bool open)
        {
            if (overlayOpen == open) return;
            if (hud != null) hud.SetOverlayVisible(open);
            foreach (TextMesh label in stationLabels) if (label != null) label.gameObject.SetActive(!open);
            if (open) { overlayPreviousPause = IsPaused; overlayOpen = true; SetPaused(true); }
            else { overlayOpen = false; SetPaused(overlayPreviousPause); }
        }

        private void Update()
        {
            if (run == null || overlayOpen || applicationSuspended) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && pauseChangedFrame != Time.frameCount &&
                (lifestyle==null||(!lifestyle.IsTraining&&!lifestyle.IsConditioning&&!lifestyle.IsBoxing))) SetPaused(!IsPaused);
            if (IsPaused) return;
            if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame) TrySelectChallenge(CityMissionChallenge.Standard);
            if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame) TrySelectChallenge(CityMissionChallenge.Express);
            if (keyboard != null && keyboard.digit3Key.wasPressedThisFrame) TrySelectChallenge(CityMissionChallenge.Ghost);
            if (keyboard != null && keyboard.nKey.wasPressedThisFrame) TryNextContract();
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) TryRetry();

            bool streetJobWasActive = Jobs.IsActive;
            Jobs.Tick(Time.deltaTime);
            if (streetJobWasActive && !Jobs.IsActive) { hud.ShowNotification(Jobs.Message, 5f);audioFeedback?.PlayCue(CityAudioCue.Failure); RefreshPresentation(); }
            run.Tick(Time.deltaTime);
            // Cool down while waiting or recovering, once per game second.
            coolingCountdown += Time.deltaTime;
            if (coolingCountdown >= 1f)
            {
                if (run.State != CityMissionState.Active && Session.Heat > 0f)
                    Session.CoolDown(coolingCountdown);
                coolingCountdown = 0f;
            }
            if (run.Revision != displayedRevision) RefreshPresentation();
            presentationCountdown -= Time.unscaledDeltaTime;
            if (presentationCountdown <= 0f)
            {
                RefreshJournal();
                presentationCountdown = 0.1f;
            }
            foreach (TextMesh label in stationLabels)
                if (label != null && view != null) label.transform.rotation = view.transform.rotation;
        }

        private void RefreshPresentation()
        {
            if (run == null || hud == null) return;
            for (int i = 0; i < stations.Length; i++)
            {
                if (stationRenderers[i] == null) continue;
                bool target = Jobs.IsActive ? i == Jobs.Offer.StationAt(Jobs.ObjectiveIndex) : run.HasTarget && i == run.ObjectiveIndex;
                int appearance = IsStationComplete(i) ? 2 : target ? 1 : 0;
                stationRenderers[i].sharedMaterial = materials[appearance];
                if (stationLabels[i] != null)
                {
                    string title = run.Contract.ObjectiveTitle(i);
                    if (Jobs.IsActive)
                        for (int objective = 0; objective < 3; objective++)
                            if (Jobs.Offer.StationAt(objective) == i) title = Jobs.Offer.ObjectiveTitle(objective);
                    stationLabels[i].text = $"{i + 1:00}  {title}";
                    stationLabels[i].color = appearance == 1 ? Color.cyan : appearance == 2 ? Color.green : Color.gray;
                }
            }
            displayedRevision = run.Revision;
            RefreshJournal();
        }

        private void RefreshJournal()
        {
            if (run == null || hud == null) return;
            string heading = $"CONTRACT {ContractNumber}/{ContractCount}  |  {run.Contract.Title}  |  {run.Challenge}";
            if (IsPaused)
            {
                hud.SetMissionText($"{heading}\nPAUSED - Esc to resume");
                return;
            }
            if (Jobs.IsActive)
            {
                int station = Jobs.Offer.StationAt(Jobs.ObjectiveIndex);
                Vector3 streetDelta = stations[station].transform.position - player.position;
                float meters = new Vector2(streetDelta.x, streetDelta.z).magnitude;
                string next=Jobs.BlockReason.Length>0?Jobs.BlockReason+" Rok i dalje teče.":"E: interakcija / P: telefon";
                hud.SetMissionText($"SPOREDNI POSAO / {Jobs.Offer.Title}\n{Jobs.ObjectiveIndex + 1}/3 {Jobs.Offer.ObjectiveTitle(Jobs.ObjectiveIndex)} / marker {station + 1} / {meters:0}m {NavigationHint(player, streetDelta)}\nRok {Math.Ceiling(Jobs.RemainingSeconds)}s / nagrada {Jobs.Offer.CashReward} cash + {Jobs.Offer.ReputationReward} REP\n{next}");
                return;
            }
            if (run.State == CityMissionState.Completed)
            {
                string next = contractIndex + 1 < contracts.Length ? "Press N to unlock the next contract" : "KAMPANJA ZAVRŠENA / P: telefon i novi sporedni posao";
                hud.SetMissionText($"{heading}\n{run.Message}\n{next}");
                return;
            }
            if (run.State == CityMissionState.Failed)
            {
                hud.SetMissionText($"{heading}\nFAILED - {run.Message}\nCargo cleared; no reward paid. Heat {Session.Heat:0}/{run.EffectiveHeatLimit:0}");
                return;
            }

            Transform target = stations[run.ObjectiveIndex]?.transform;
            Vector3 delta = target != null ? target.position - player.position : Vector3.zero;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            string direction = NavigationHint(player, delta);
            string timer = run.State == CityMissionState.Ready ? $"Clock starts at pickup - {run.EffectiveTimeLimit:0}s" : $"Time left {Math.Ceiling(run.RemainingSeconds):0}s";
            string controls = run.State == CityMissionState.Ready
                ? "Before pickup: 1 Standard | 2 Express +50% | 3 Ghost +25%"
                : "WASD move - Shift sprint - E interact - Esc pause";
            if(ContactBlockReason.Length>0)controls=ContactBlockReason;
            hud.SetMissionText($"{heading}\n{run.ObjectiveIndex + 1}/3  {run.Contract.ObjectiveTitle(run.ObjectiveIndex)}  |  {distance:0}m {direction}\n" +
                $"{timer}  |  Heat {Session.Heat:0}/{run.EffectiveHeatLimit:0}  |  Cargo {run.Cargo}/1  |  Reward {run.EffectiveCashReward} cash\n{controls}");
        }

        private static string NavigationHint(Transform facing, Vector3 offset)
        {
            if (new Vector2(offset.x, offset.z).sqrMagnitude < 9f) return "NEARBY";
            Vector3 local = facing.InverseTransformDirection(offset);
            if (Mathf.Abs(local.x) > Mathf.Abs(local.z)) return local.x > 0 ? "RIGHT >" : "< LEFT";
            return local.z >= 0 ? "AHEAD ^" : "BEHIND v";
        }

        private void CreateMaterials()
        {
            Material template = Resources.Load<Material>("CityMissionMarker");
            Shader shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("CityMissionMarker must be included in Resources before building.");
            Color[] colors = { new Color(0.18f, 0.22f, 0.27f), new Color(0.02f, 0.75f, 0.9f), new Color(0.08f, 0.6f, 0.24f) };
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = template != null ? new Material(template) : new Material(shader);
                materials[i].color = colors[i];
                materials[i].EnableKeyword("_EMISSION");
                materials[i].SetColor("_EmissionColor", colors[i] * (i == 1 ? 1.8f : 0.2f));
            }
        }

        private void CreateStation(int index, Vector3 groundPosition, Transform worldRoot)
        {
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            station.name = $"MissionStation_{index + 1}";
            station.transform.SetParent(worldRoot, true);
            station.transform.position = groundPosition + Vector3.up * 0.85f;
            station.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f);
            stationRenderers[index] = station.GetComponent<Renderer>();
            stations[index] = station.AddComponent<CityMission>();
            stations[index].ConfigureStation(this, index);
            station.AddComponent<CesiumGlobeAnchor>();
            GameObject labelObject = new GameObject("ObjectiveLabel");
            labelObject.transform.SetParent(station.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            labelObject.GetComponent<Renderer>().sharedMaterial = label.font.material;
            stationLabels[index] = label;
        }

        public static CityMissionContract[] CreateContracts() => new[]
        {
            new CityMissionContract("First Night Courier", "Collect a parcel, deliver it and report to the contact. Payment comes after the full route.",
                120, 600, 12, 4f, 85f, "Collect the parcel", "Deliver to the shop", "Report to the contact"),
            new CityMissionContract("Street Supply", "Collect the shop order and close the deal at the contact. Keep the cargo intact and beat the deadline.",
                90, 1000, 18, 7f, 75f, "Collect the shop order", "Hand over the supplies", "Confirm the deal"),
            new CityMissionContract("Night Express", "A tighter deadline and a bigger payout. Complete every checkpoint before the clock runs out.",
                60, 1500, 25, 10f, 65f, "Collect the express package", "Deliver the express package", "Collect final confirmation")
        };

        private static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnDisable()
        {
            if (IsPaused) SetPaused(false);
            SetCursor(false);
        }

        private void OnEnable()
        {
            if (run != null && !IsPaused) SetCursor(true);
        }
        private void OnApplicationPause(bool paused){applicationSuspended=paused;}

        private void OnDestroy()
        {
            foreach (Material material in materials)
                if (material != null) Destroy(material);
        }
    }
}
