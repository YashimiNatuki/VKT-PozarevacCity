using UnityEngine;

namespace Pozarevac.CityGame
{
    // Reuses the player's licensed skin and skeleton; no per-frame mesh baking or allocations.
    public sealed class CityBenchVisual : MonoBehaviour
    {
        private Transform bar, athlete, leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand;
        private CityCharacterLook look;
        private int strength;
        private CityOutfit outfit;
        public CityOutfit CurrentOutfit => outfit;
        private readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2];
        private CityCharacterPhysique physique;
        public void SetStrength(int value) { strength=value;if(look!=null)look.Apply(outfit,strength); }
        public void SetOutfit(CityOutfit value){outfit=value;if(look!=null)look.Apply(outfit,strength);}
        public void SetAppearance(CityOutfit value,int power){outfit=value;strength=power;if(look!=null)look.Apply(outfit,strength);}
        private float gripSide = -1, lowerHeight = .94f, upperHeight = 1.2f, barZ = -.35f;
        public bool RigReady { get; private set; }
        public float HandError { get; private set; }
        public float FootError { get; private set; }
        private int plantedFeet;
        public bool FeetReady => plantedFeet == 2;
        public Vector3 CameraPosition => transform.TransformPoint(new Vector3(2.5f, 2.2f, 2.3f));
        public Vector3 CameraTarget => transform.TransformPoint(new Vector3(0, .9f, -.1f));

        public void Initialize(Mesh rounded, Material steel, Material pad)
        {
            Part("BenchPad", transform, new Vector3(0, .42f, .05f), new Vector3(.56f, .14f, 1.7f), rounded, pad, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("BenchLeg", transform, new Vector3(0, .2f, side * .55f), new Vector3(.16f, .4f, .18f), rounded, steel, true);
                Part("StableFoot", transform, new Vector3(0, .08f, side * .55f), new Vector3(.95f, .12f, .3f), rounded, steel, true);
                Part("Rack", transform, new Vector3(side * .8f, .7f, -.55f), new Vector3(.1f, 1.4f, .12f), rounded, steel, true);
                Part("SafetyArm", transform, new Vector3(side * .7f, .72f, -.1f), new Vector3(.12f, .1f, 1f), rounded, steel, true);
            }
            bar = new GameObject("OlympicBar").transform; bar.SetParent(transform, false);
            Cylinder("Shaft", bar, Vector3.zero, new Vector3(.032f, 1.15f, .032f), steel);
            for (int side = -1; side <= 1; side += 2)
                for (int plate = 0; plate < 2; plate++)
                    Cylinder("RoundPlate", bar, new Vector3(side * (.83f + plate * .09f), 0, 0), new Vector3(.38f, .035f, .38f), steel);

            GameObject source = CityCharacterModel.LoadSource();
            if (source == null) return;
            athlete = new GameObject("BenchAthlete").transform; athlete.SetParent(transform, false);
            Transform wrapper = new GameObject("NormalizedCharacter").transform; wrapper.SetParent(athlete, false);
            GameObject model = Instantiate(source, wrapper);
            physique = new CityCharacterPhysique(model);
            AnimationClip pose = null;
            foreach (AnimationClip candidate in Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"))
                if (!candidate.name.StartsWith("__preview__", System.StringComparison.Ordinal)) { pose = candidate; break; }
            if (!CityCharacterModel.Normalize(wrapper, model, pose,physique)) { physique.Dispose();physique=null;Destroy(athlete.gameObject); athlete = null; return; }
            look=new CityCharacterLook(model,physique);
            foreach(SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
            Transform root = CityCharacterModel.AnimationRoot(model).transform;
            for(int i=0;i<2;i++)
            {
                string side=i==0?"Left":"Right";thighs[i]=root.Find("HipsCtrl/Hips/"+side+"UpLeg");
                knees[i]=thighs[i]!=null?thighs[i].Find(side+"Leg"):null;feet[i]=knees[i]!=null?knees[i].Find(side+"Foot"):null;
            }
            const string chest = "HipsCtrl/Hips/Spine/Chest/UpperChest/";
            leftArm = root.Find(chest + "LeftShoulder/LeftArm"); leftElbow = leftArm != null ? leftArm.Find("LeftForeArm") : null;
            leftHand = leftElbow != null ? leftElbow.Find("LeftHand") : null;
            rightArm = root.Find(chest + "RightShoulder/RightArm"); rightElbow = rightArm != null ? rightArm.Find("RightForeArm") : null;
            rightHand = rightElbow != null ? rightElbow.Find("RightHand") : null;
            RigReady = leftHand != null && rightHand != null;
            athlete.localPosition = new Vector3(0, .6f, .9f); athlete.localRotation = Quaternion.Euler(-90, 0, 0);
            ConfigureReach();
            athlete.gameObject.SetActive(false); Pose(false, 1);
        }
        private void ConfigureReach()
        {
            if (RigReady)
            {
                Vector3 shoulder = transform.InverseTransformPoint(leftArm.position);
                Vector3 other = transform.InverseTransformPoint(rightArm.position);
                gripSide = Mathf.Sign(shoulder.x);
                barZ = (shoulder.z + other.z) * .5f + .04f;
                lowerHeight = Mathf.Max(shoulder.y, other.y) + .1f;
                upperHeight = Mathf.Min(MaximumBarHeight(leftArm, leftElbow, leftHand, gripSide * .43f),
                    MaximumBarHeight(rightArm, rightElbow, rightHand, -gripSide * .43f));
            }
            FootError=0;plantedFeet=0;
            PlantLeg(0,-.3f);PlantLeg(1,.3f);
        }
        private void PlantLeg(int index,float x)
        {
            Transform thigh=thighs[index],knee=knees[index],foot=feet[index];
            if (foot != null)
            {
                Vector3 hip = transform.InverseTransformPoint(thigh.position);
                float reach = (Vector3.Distance(thigh.position, knee.position) + Vector3.Distance(knee.position, foot.position)) * .98f;
                float vertical = hip.y - .14f, across = hip.x - x;
                float forward = Mathf.Min(.45f, Mathf.Sqrt(Mathf.Max(0, reach * reach - vertical * vertical - across * across)));
                Vector3 target = transform.TransformPoint(new Vector3(x, .14f, hip.z + forward));
                Solve(thigh, knee, foot, target, transform.up);
                plantedFeet++;
                FootError = Mathf.Max(FootError, Vector3.Distance(foot.position, target));
            }
        }
        public void Pose(bool active, float extension)
        {
            if(look!=null)look.Face.SetEffort(active?Mathf.Lerp(.9f,.4f,Mathf.Clamp01(extension)):0);
            if (bar == null) return;
            if (athlete != null) athlete.gameObject.SetActive(active);
            bar.localPosition = new Vector3(0, Mathf.Lerp(lowerHeight, upperHeight, extension), barZ);
            if (!active || !RigReady) return;
            ConfigureReach();
            bar.localPosition = new Vector3(0, Mathf.Lerp(lowerHeight, upperHeight, extension), barZ);
            Vector3 left = bar.TransformPoint(new Vector3(gripSide * .43f, 0, 0)), right = bar.TransformPoint(new Vector3(-gripSide * .43f, 0, 0));
            Solve(leftArm, leftElbow, leftHand, left, transform.right * gripSide + transform.forward * .4f);
            Solve(rightArm, rightElbow, rightHand, right, -transform.right * gripSide + transform.forward * .4f);
            HandError = Mathf.Max(Vector3.Distance(leftHand.position, left), Vector3.Distance(rightHand.position, right));
        }
        private float MaximumBarHeight(Transform upper, Transform lower, Transform hand, float gripX)
        {
            Vector3 shoulder = transform.InverseTransformPoint(upper.position);
            float reach = (Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position)) * .96f;
            float horizontal = (gripX - shoulder.x) * (gripX - shoulder.x) + (barZ - shoulder.z) * (barZ - shoulder.z);
            return shoulder.y + Mathf.Sqrt(Mathf.Max(.01f, reach * reach - horizontal));
        }
        private static void Solve(Transform upper, Transform lower, Transform tip, Vector3 target, Vector3 hint)
        {
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, tip.position);
            Vector3 delta = target - upper.position; float reach = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            if (a < .001f || b < .001f || delta.sqrMagnitude < .000001f) return;
            Vector3 direction = delta.normalized, bend = Vector3.ProjectOnPlane(hint, direction).normalized;
            if (bend.sqrMagnitude < .001f) bend = Vector3.Cross(direction, Vector3.forward).normalized;
            float along = (a * a - b * b + reach * reach) / (2 * reach);
            Vector3 elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(tip.position - lower.position, target - lower.position) * lower.rotation;
        }
        private static void Part(string name, Transform parent, Vector3 at, Vector3 size, Mesh mesh, Material material, bool collide)
        {
            GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = at; obj.transform.localScale = size;
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collide) obj.AddComponent<BoxCollider>();
        }
        private static void Cylinder(string name, Transform parent, Vector3 at, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = at; obj.transform.localRotation = Quaternion.Euler(0, 0, 90); obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material; obj.GetComponent<Collider>().enabled = false; obj.layer = 2;
        }
        private void OnDestroy() { if(look!=null)look.Dispose();else physique?.Dispose(); }
    }
}
