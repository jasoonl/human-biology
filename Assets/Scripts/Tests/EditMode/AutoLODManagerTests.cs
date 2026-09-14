using HumanBodyExplorer.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class AutoLODManagerTests
    {
        private static Mesh CreateGridMesh(int subdivisions)
        {
            // A simple flat grid, dense enough that clustering has room to merge vertices.
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            for (int y = 0; y <= subdivisions; y++)
            {
                for (int x = 0; x <= subdivisions; x++)
                {
                    vertices.Add(new Vector3(x, y, 0) * 0.1f);
                }
            }

            for (int y = 0; y < subdivisions; y++)
            {
                for (int x = 0; x < subdivisions; x++)
                {
                    int i0 = y * (subdivisions + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + subdivisions + 1;
                    int i3 = i2 + 1;

                    triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                    triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        [Test]
        public void Decimate_AggressiveRatio_ReducesTriangleCount()
        {
            var source = CreateGridMesh(20);
            int originalTriCount = source.triangles.Length / 3;

            var decimated = AutoLODManager.Decimate(source, 0.05f);
            int decimatedTriCount = decimated.triangles.Length / 3;

            Assert.Less(decimatedTriCount, originalTriCount);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(decimated);
        }

        [Test]
        public void Decimate_NoDegenerateTriangles()
        {
            var source = CreateGridMesh(10);
            var decimated = AutoLODManager.Decimate(source, 0.1f);

            int[] tris = decimated.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Assert.AreNotEqual(tris[i], tris[i + 1]);
                Assert.AreNotEqual(tris[i + 1], tris[i + 2]);
                Assert.AreNotEqual(tris[i], tris[i + 2]);
            }

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(decimated);
        }
    }
}
