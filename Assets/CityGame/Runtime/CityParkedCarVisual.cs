using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityParkedCarVisual : MonoBehaviour
    {
        private enum Finish { Paint, Rubber, Glass, Metal, Headlamp, TailLamp }

        private Mesh carMesh;
        private Material[] finishes;

        public static void Build(Transform root)
        {
            var child = new GameObject("CarPresentation", typeof(MeshFilter), typeof(MeshRenderer), typeof(CityParkedCarVisual));
            child.transform.SetParent(root, false);
            child.GetComponent<CityParkedCarVisual>().BuildMesh();
        }

        private void BuildMesh()
        {
            Mesh box = CityVisualMeshes.RoundedBox(.13f, 3);
            Mesh tire = CityVisualMeshes.Lathe(new[]
            {
                new Vector2(0, -.10f), new Vector2(.23f, -.10f), new Vector2(.273f, -.079f),
                new Vector2(.285f, -.045f), new Vector2(.285f, .045f), new Vector2(.273f, .079f),
                new Vector2(.23f, .10f), new Vector2(0, .10f)
            }, 20);
            Mesh hub = CityVisualMeshes.Lathe(new[]
            {
                new Vector2(0, 0), new Vector2(.174f, 0), new Vector2(.184f, .009f),
                new Vector2(.17f, .019f), new Vector2(.105f, .025f), new Vector2(0, .025f)
            }, 20);

            var geometry = new CarGeometry();
            try
            {
                // The narrow underbody sits inside the wheel faces. The side panels have open arches.
                geometry.Add(box, Finish.Rubber, new Vector3(0, .44f, -.025f), new Vector3(1.22f, .30f, 3.10f), Quaternion.identity);
                geometry.Add(box, Finish.Paint, new Vector3(0, .746f, -.025f), new Vector3(1.57f, .286f, 3.28f), Quaternion.identity);
                geometry.Add(box, Finish.Paint, new Vector3(0, .551f, 1.545f), new Vector3(1.53f, .21f, .22f), Quaternion.identity);
                geometry.Add(box, Finish.Paint, new Vector3(0, .551f, -1.545f), new Vector3(1.53f, .21f, .22f), Quaternion.identity);
                geometry.Add(box, Finish.Paint, new Vector3(0, .855f, 1.205f), new Vector3(1.48f, .12f, .83f), Quaternion.Euler(3, 0, 0));
                geometry.Add(box, Finish.Paint, new Vector3(0, 1.389f, -.296f), new Vector3(1.274f, .068f, 1.263f), Quaternion.identity);
                AddCabin(geometry);

                for (int side = -1; side <= 1; side += 2)
                {
                    Quaternion axle = Quaternion.Euler(0, 0, -90 * side);
                    geometry.WheelArchSide(side);
                    for (int end = -1; end <= 1; end += 2)
                    {
                        geometry.Add(tire, Finish.Rubber, new Vector3(side * .75f, .285f, end * 1.05f), Vector3.one, axle);
                        geometry.Add(hub, Finish.Metal, new Vector3(side * .847f, .285f, end * 1.05f), Vector3.one, axle);
                        geometry.Add(hub, Finish.Rubber, new Vector3(side * .853f, .285f, end * 1.05f), new Vector3(.54f, 1f, .54f), axle);
                    }
                    geometry.Add(box, Finish.Rubber, new Vector3(side * .782f, .669f, -.045f), new Vector3(.025f, .032f, 2.90f), Quaternion.identity);
                    geometry.Add(box, Finish.Rubber, new Vector3(side * .794f, .952f, .56f), new Vector3(.17f, .085f, .12f), Quaternion.identity);
                    geometry.Add(box, Finish.Glass, new Vector3(side * .816f, .952f, .499f), new Vector3(.105f, .052f, .02f), Quaternion.identity);
                    geometry.Add(box, Finish.Metal, new Vector3(side * .786f, .802f, -.34f), new Vector3(.04f, .03f, .15f), Quaternion.identity);
                    geometry.Add(box, Finish.Rubber, new Vector3(side * .548f, .739f, 1.631f), new Vector3(.36f, .17f, .085f), Quaternion.identity);
                    geometry.Add(box, Finish.Headlamp, new Vector3(side * .548f, .739f, 1.673f), new Vector3(.302f, .124f, .044f), Quaternion.identity);
                    geometry.Add(box, Finish.Rubber, new Vector3(side * .578f, .745f, -1.669f), new Vector3(.36f, .168f, .077f), Quaternion.identity);
                    geometry.Add(box, Finish.TailLamp, new Vector3(side * .578f, .767f, -1.708f), new Vector3(.304f, .095f, .04f), Quaternion.identity);
                    geometry.Add(box, Finish.Headlamp, new Vector3(side * .578f, .702f, -1.71f), new Vector3(.30f, .027f, .039f), Quaternion.identity);
                }

                geometry.Add(box, Finish.Rubber, new Vector3(0, .462f, 1.645f), new Vector3(1.59f, .145f, .222f), Quaternion.identity);
                geometry.Add(box, Finish.Rubber, new Vector3(0, .462f, -1.661f), new Vector3(1.59f, .145f, .225f), Quaternion.identity);
                geometry.Add(box, Finish.Metal, new Vector3(0, .49f, -1.773f), new Vector3(1.40f, .026f, .01f), Quaternion.identity);
                geometry.Add(box, Finish.Rubber, new Vector3(0, .72f, 1.625f), new Vector3(.75f, .177f, .089f), Quaternion.identity);
                for (int slat = -1; slat <= 1; slat++)
                    geometry.Add(box, Finish.Metal, new Vector3(0, .72f + slat * .041f, 1.674f), new Vector3(.67f, .012f, .018f), Quaternion.identity);
                geometry.Add(box, Finish.Metal, new Vector3(0, .507f, 1.754f), new Vector3(.38f, .086f, .024f), Quaternion.identity);
                geometry.Add(box, Finish.Rubber, new Vector3(0, .701f, -1.668f), new Vector3(.43f, .12f, .055f), Quaternion.identity);
                geometry.Add(box, Finish.Headlamp, new Vector3(0, .701f, -1.698f), new Vector3(.365f, .078f, .015f), Quaternion.identity);
                for (int mark = -2; mark <= 2; mark++)
                    geometry.Add(box, Finish.Rubber, new Vector3(mark * .046f, .702f, -1.706f), new Vector3(.019f, .031f, .006f), Quaternion.identity);

                carMesh = geometry.Create();
                GetComponent<MeshFilter>().sharedMesh = carMesh;
                GetComponent<MeshRenderer>().sharedMaterials = CreateFinishes();
            }
            finally
            {
                Release(box);
                Release(tire);
                Release(hub);
            }
        }

        private static void AddCabin(CarGeometry geometry)
        {
            const float sill = .875f, roof = 1.38f;
            var frontLeft = new Vector3(-.77f, sill, .83f);
            var frontRight = new Vector3(.77f, sill, .83f);
            var rearLeft = new Vector3(-.77f, sill, -1.43f);
            var rearRight = new Vector3(.77f, sill, -1.43f);
            var upperFrontLeft = new Vector3(-.618f, roof, .315f);
            var upperFrontRight = new Vector3(.618f, roof, .315f);
            var upperRearLeft = new Vector3(-.618f, roof, -.905f);
            var upperRearRight = new Vector3(.618f, roof, -.905f);

            geometry.CabinWall(frontLeft, frontRight, upperFrontLeft, upperFrontRight, Vector3.forward);
            geometry.CabinWall(rearLeft, rearRight, upperRearLeft, upperRearRight, Vector3.back);
            geometry.CabinWall(rearLeft, frontLeft, upperRearLeft, upperFrontLeft, Vector3.left, true);
            geometry.CabinWall(rearRight, frontRight, upperRearRight, upperFrontRight, Vector3.right, true);
        }

        private Material[] CreateFinishes()
        {
            Material template = Resources.Load<Material>("CityMissionMarker");
            finishes = new Material[6];
            SetFinish(Finish.Paint, template, new Color(.72f, .133f, .10f), .52f, .14f);
            SetFinish(Finish.Rubber, template, new Color(.048f, .055f, .063f), .19f);
            SetFinish(Finish.Glass, template, new Color(.142f, .285f, .365f), .77f, .12f);
            SetFinish(Finish.Metal, template, new Color(.67f, .70f, .71f), .52f, .24f);
            SetFinish(Finish.Headlamp, template, new Color(.95f, .88f, .73f), .45f);
            SetFinish(Finish.TailLamp, template, new Color(.94f, .114f, .071f), .53f);
            return finishes;
        }

        private void SetFinish(Finish finish, Material template, Color color, float gloss, float metal = 0)
        {
            var material = new Material(template) { name = "VKT hatchback " + finish, color = color };
            material.SetFloat("_Smoothness", gloss);
            material.SetFloat("_Metallic", metal);
            finishes[(int)finish] = material;
        }

        private void OnDestroy()
        {
            Release(carMesh);
            if (finishes == null) return;
            foreach (Material material in finishes) Release(material);
        }

        private static void Release(Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }

        // One renderer, grouped by material. The vehicle root owns all colliders.
        private sealed class CarGeometry
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<int>[] triangles =
            {
                new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>()
            };

            public void Add(Mesh source, Finish finish, Vector3 center, Vector3 scale, Quaternion rotation)
            {
                int first = vertices.Count;
                var inverseScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
                foreach (Vector3 vertex in source.vertices)
                    vertices.Add(center + rotation * Vector3.Scale(vertex, scale));
                foreach (Vector3 normal in source.normals)
                    normals.Add(rotation * Vector3.Scale(normal, inverseScale).normalized);
                foreach (int index in source.triangles)
                    triangles[(int)finish].Add(first + index);
            }

            public void CabinWall(Vector3 lowerA, Vector3 lowerB, Vector3 upperA, Vector3 upperB, Vector3 outward, bool side = false)
            {
                AddQuad(Finish.Paint, lowerA, lowerB, upperA, upperB, outward);
                if (side)
                {
                    AddWindow(.064f, .51f);
                    AddWindow(.547f, .937f);
                }
                else AddWindow(.105f, .895f);

                void AddWindow(float left, float right)
                {
                    AddQuad(Finish.Rubber,
                        Point(left, .135f, .006f), Point(right, .135f, .006f),
                        Point(left + .024f, .921f, .006f), Point(right - .024f, .921f, .006f), outward);
                    AddQuad(Finish.Glass,
                        Point(left + .014f, .166f, .011f), Point(right - .014f, .166f, .011f),
                        Point(left + .039f, .891f, .011f), Point(right - .039f, .891f, .011f), outward);
                }

                Vector3 Point(float across, float height, float offset)
                {
                    return Vector3.Lerp(Vector3.Lerp(lowerA, lowerB, across), Vector3.Lerp(upperA, upperB, across), height)
                        + outward * offset;
                }
            }

            public void WheelArchSide(int side)
            {
                const float axleHeight = .285f, axleDepth = 1.05f;
                const float railBottom = .322f, railTop = .639f, sideX = .772f;
                const float archRadius = .309f, trimWidth = .025f;
                const int segments = 9;
                float minAngle = Mathf.Asin((railBottom - axleHeight) / archRadius);
                float halfOpening = archRadius * Mathf.Cos(minAngle);
                Vector3 outward = Vector3.right * side;
                AddRail(-1.625f, -axleDepth - halfOpening);
                AddRail(-axleDepth + halfOpening, axleDepth - halfOpening);
                AddRail(axleDepth + halfOpening, 1.625f);

                for (int end = -1; end <= 1; end += 2)
                {
                    float center = end * axleDepth;
                    for (int step = 0; step < segments; step++)
                    {
                        float from = Mathf.Lerp(Mathf.PI - minAngle, minAngle, (float)step / segments);
                        float to = Mathf.Lerp(Mathf.PI - minAngle, minAngle, (float)(step + 1) / segments);
                        Vector3 a = Arc(from, archRadius, sideX, center);
                        Vector3 b = Arc(to, archRadius, sideX, center);
                        AddQuad(Finish.Paint, a, b, new Vector3(a.x, railTop, a.z), new Vector3(b.x, railTop, b.z), outward);
                        AddQuad(Finish.Rubber, a + outward * .011f, b + outward * .011f,
                            Arc(from, archRadius + trimWidth, sideX + .011f, center),
                            Arc(to, archRadius + trimWidth, sideX + .011f, center), outward);
                    }
                }

                void AddRail(float start, float end)
                {
                    float x = side * sideX;
                    AddQuad(Finish.Paint, new Vector3(x, railBottom, start), new Vector3(x, railBottom, end),
                        new Vector3(x, railTop, start), new Vector3(x, railTop, end), outward);
                }

                Vector3 Arc(float angle, float radius, float x, float center)
                {
                    return new Vector3(side * x, axleHeight + radius * Mathf.Sin(angle), center + radius * Mathf.Cos(angle));
                }
            }

            private void AddQuad(Finish finish, Vector3 lowerA, Vector3 lowerB, Vector3 upperA, Vector3 upperB, Vector3 outward)
            {
                int first = vertices.Count;
                Vector3 normal = Vector3.Cross(lowerB - lowerA, upperA - lowerA).normalized;
                bool reverse = Vector3.Dot(normal, outward) < 0;
                if (reverse) normal = -normal;
                vertices.Add(lowerA);
                vertices.Add(lowerB);
                vertices.Add(upperA);
                vertices.Add(upperB);
                for (int i = 0; i < 4; i++) normals.Add(normal);
                var indices = triangles[(int)finish];
                if (reverse) indices.AddRange(new[] { first, first + 2, first + 1, first + 1, first + 2, first + 3 });
                else indices.AddRange(new[] { first, first + 1, first + 2, first + 1, first + 3, first + 2 });
            }

            public Mesh Create()
            {
                var mesh = new Mesh { name = "VKT compact hatchback", subMeshCount = triangles.Length };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i, false);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
