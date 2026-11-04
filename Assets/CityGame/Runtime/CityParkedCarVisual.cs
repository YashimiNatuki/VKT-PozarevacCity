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
            Mesh box = CityVisualMeshes.RoundedBox(.13f, 4);
            Mesh tire = CityVisualMeshes.Lathe(new[]
            {
                new Vector2(0, -.10f), new Vector2(.23f, -.10f), new Vector2(.273f, -.079f),
                new Vector2(.285f, -.045f), new Vector2(.285f, .045f), new Vector2(.273f, .079f),
                new Vector2(.23f, .10f), new Vector2(0, .10f)
            }, 20);
            Mesh hub = CityVisualMeshes.Lathe(new[]
            {
                new Vector2(0, 0), new Vector2(.153f, 0), new Vector2(.164f, .009f),
                new Vector2(.15f, .019f), new Vector2(.10f, .025f), new Vector2(0, .025f)
            }, 20);

            var geometry = new CarGeometry();
            try
            {
                geometry.Add(box, Finish.Paint, new Vector3(0, .59f, -.025f), new Vector3(1.57f, .58f, 3.28f), Quaternion.identity);
                geometry.Add(box, Finish.Paint, new Vector3(0, .855f, 1.205f), new Vector3(1.48f, .12f, .83f), Quaternion.Euler(3, 0, 0));
                geometry.Add(box, Finish.Paint, new Vector3(0, 1.396f, -.32f), new Vector3(1.29f, .07f, 1.32f), Quaternion.identity);
                AddCabin(geometry);

                for (int side = -1; side <= 1; side += 2)
                {
                    Quaternion axle = Quaternion.Euler(0, 0, -90 * side);
                    for (int end = -1; end <= 1; end += 2)
                    {
                        geometry.Add(tire, Finish.Rubber, new Vector3(side * .75f, .285f, end * 1.05f), Vector3.one, axle);
                        geometry.Add(hub, Finish.Metal, new Vector3(side * .847f, .285f, end * 1.05f), Vector3.one, axle);
                    }
                    geometry.Add(box, Finish.Rubber, new Vector3(side * .785f, .585f, -.06f), new Vector3(.035f, .075f, 2.78f), Quaternion.identity);
                    geometry.Add(box, Finish.Rubber, new Vector3(side * .794f, .952f, .56f), new Vector3(.17f, .085f, .12f), Quaternion.identity);
                    geometry.Add(box, Finish.Metal, new Vector3(side * .786f, .776f, -.34f), new Vector3(.04f, .035f, .18f), Quaternion.identity);
                    geometry.Add(box, Finish.Headlamp, new Vector3(side * .546f, .725f, 1.63f), new Vector3(.32f, .166f, .105f), Quaternion.identity);
                    geometry.Add(box, Finish.TailLamp, new Vector3(side * .59f, .732f, -1.662f), new Vector3(.29f, .167f, .085f), Quaternion.identity);
                }

                geometry.Add(box, Finish.Rubber, new Vector3(0, .49f, 1.651f), new Vector3(1.57f, .17f, .195f), Quaternion.identity);
                geometry.Add(box, Finish.Rubber, new Vector3(0, .49f, -1.67f), new Vector3(1.57f, .17f, .16f), Quaternion.identity);
                geometry.Add(box, Finish.Rubber, new Vector3(0, .72f, 1.625f), new Vector3(.75f, .177f, .089f), Quaternion.identity);
                for (int slat = -1; slat <= 1; slat++)
                    geometry.Add(box, Finish.Metal, new Vector3(0, .72f + slat * .041f, 1.674f), new Vector3(.67f, .012f, .018f), Quaternion.identity);
                geometry.Add(box, Finish.Metal, new Vector3(0, .507f, 1.754f), new Vector3(.38f, .086f, .024f), Quaternion.identity);
                geometry.Add(box, Finish.Metal, new Vector3(0, .705f, -1.68f), new Vector3(.38f, .086f, .024f), Quaternion.identity);

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
            var upperFrontLeft = new Vector3(-.63f, roof, .312f);
            var upperFrontRight = new Vector3(.63f, roof, .312f);
            var upperRearLeft = new Vector3(-.63f, roof, -.95f);
            var upperRearRight = new Vector3(.63f, roof, -.95f);

            geometry.CabinWall(frontLeft, frontRight, upperFrontLeft, upperFrontRight, Vector3.forward);
            geometry.CabinWall(rearLeft, rearRight, upperRearLeft, upperRearRight, Vector3.back);
            geometry.CabinWall(rearLeft, frontLeft, upperRearLeft, upperFrontLeft, Vector3.left, true);
            geometry.CabinWall(rearRight, frontRight, upperRearRight, upperFrontRight, Vector3.right, true);
        }

        private Material[] CreateFinishes()
        {
            Material template = Resources.Load<Material>("CityMissionMarker");
            finishes = new Material[6];
            SetFinish(Finish.Paint, template, new Color(.57f, .095f, .078f), .46f, .12f);
            SetFinish(Finish.Rubber, template, new Color(.041f, .046f, .052f), .18f);
            SetFinish(Finish.Glass, template, new Color(.065f, .16f, .218f), .76f, .16f);
            SetFinish(Finish.Metal, template, new Color(.57f, .59f, .58f), .48f, .24f);
            SetFinish(Finish.Headlamp, template, new Color(.95f, .84f, .62f), .42f);
            SetFinish(Finish.TailLamp, template, new Color(.57f, .042f, .031f), .42f);
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
                    AddWindow(.07f, .50f);
                    AddWindow(.556f, .928f);
                }
                else AddWindow(.085f, .915f);

                void AddWindow(float left, float right)
                {
                    AddQuad(Finish.Glass, Point(left, .12f), Point(right, .12f), Point(left, .89f), Point(right, .89f), outward);
                }

                Vector3 Point(float across, float height)
                {
                    return Vector3.Lerp(Vector3.Lerp(lowerA, lowerB, across), Vector3.Lerp(upperA, upperB, across), height)
                        + outward * .004f;
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
