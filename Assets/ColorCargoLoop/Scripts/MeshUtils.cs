using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Prosedurel mesh yardimcilari. Yuvarlak (oval kenarli) kup -> cartoon yumusak gorunum.
    /// </summary>
    public static class MeshUtils
    {
        private static Mesh _roundedCube;

        /// <summary>Birim (-0.5..0.5) YUVARLATILMIS kup. Tek sefer uretilir, paylasilir (scale ile boyutlanir).</summary>
        public static Mesh RoundedCube()
        {
            if (_roundedCube == null) _roundedCube = BuildRoundedCube(6, 0.15f);
            return _roundedCube;
        }

        // n: kenar basina segment, round: kupten kureye lerp (0=kup, 1=kure) -> kose yuvarlama
        private static Mesh BuildRoundedCube(int n, float round)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var lookup = new Dictionary<long, int>(); // pozisyon -> index (kaynastir -> yumusak normal)

            // 6 yuz: (normal, axisA, axisB) -> cross(axisA,axisB)=normal (dis yon)
            Vector3[] fn = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            Vector3[] fa = { Vector3.forward, Vector3.right, Vector3.up, Vector3.forward, Vector3.right, Vector3.up };
            Vector3[] fb = { Vector3.right, Vector3.forward, Vector3.forward, Vector3.up, Vector3.up, Vector3.right };

            for (int f = 0; f < 6; f++)
            {
                Vector3 nrm = fn[f], a = fa[f], b = fb[f];
                int[,] idx = new int[n + 1, n + 1];
                for (int y = 0; y <= n; y++)
                {
                    for (int x = 0; x <= n; x++)
                    {
                        float u = x / (float)n - 0.5f;
                        float v = y / (float)n - 0.5f;
                        Vector3 cube = nrm * 0.5f + a * u + b * v;        // kup yuzeyi
                        Vector3 sphere = cube.normalized * 0.5f;          // kure
                        Vector3 p = Vector3.Lerp(cube, sphere, round);    // yuvarlatilmis
                        idx[y, x] = AddWelded(verts, lookup, p);
                    }
                }
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        int v0 = idx[y, x], v1 = idx[y, x + 1], v2 = idx[y + 1, x + 1], v3 = idx[y + 1, x];
                        tris.Add(v0); tris.Add(v1); tris.Add(v2);
                        tris.Add(v0); tris.Add(v2); tris.Add(v3);
                    }
                }
            }

            Mesh m = new Mesh { name = "RoundedCube" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static int AddWelded(List<Vector3> verts, Dictionary<long, int> lookup, Vector3 p)
        {
            long key = Key(p);
            int existing;
            if (lookup.TryGetValue(key, out existing)) return existing;
            int i = verts.Count;
            verts.Add(p);
            lookup[key] = i;
            return i;
        }

        private static long Key(Vector3 p)
        {
            long qx = Mathf.RoundToInt(p.x * 2048f);
            long qy = Mathf.RoundToInt(p.y * 2048f);
            long qz = Mathf.RoundToInt(p.z * 2048f);
            return (qx * 73856093L) ^ (qy * 19349663L) ^ (qz * 83492791L);
        }
    }
}
