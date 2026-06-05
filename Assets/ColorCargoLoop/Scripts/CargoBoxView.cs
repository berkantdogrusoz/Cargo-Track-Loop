using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// FAZ 2 - Pixel Flow tarzi KUTU (ici oyuk container).
    /// Buyukce, ici bos, renkli duvarli kutu. Kupler ICINE dizilerek dolar.
    /// Tek renk + kapasite. Kapasiteye ulasinca FULL olur.
    /// Faz 1'e hic dokunmaz - tamamen ayri sistem.
    /// </summary>
    public sealed class CargoBoxView : MonoBehaviour
    {
        private ColorCargoLoopGame game;
        private CargoColor boxColor;
        private int capacity;
        private int currentFill;

        private Transform slotRoot;
        private readonly List<GameObject> fillCubes = new List<GameObject>();

        // Grid yerlesimi (kareye yakin footprint)
        private int cols;
        private int rows;
        private float cellStep;
        private float boxWidth;

        public CargoColor BoxColor { get { return boxColor; } }
        public int Capacity { get { return capacity; } }
        public int CurrentFill { get { return currentFill; } }
        public int Remaining { get { return Mathf.Max(0, capacity - currentFill); } }
        public bool IsFull { get { return currentFill >= capacity; } }

        public void Initialize(ColorCargoLoopGame owner, CargoColor color, int cap, float width, GameObject modelPrefab = null)
        {
            game = owner;
            boxColor = color;
            capacity = Mathf.Max(1, cap);
            currentFill = 0;
            boxWidth = width;
            if (modelPrefab != null) BuildFromModel(modelPrefab);
            else BuildVisual();
        }

        /// <summary>
        /// Senin 3D kutu modelini kullanir: instantiate eder, genislige gore olceklendirir,
        /// govdeyi KUTU RENGINE boyar. Kupler icine dizilir.
        /// </summary>
        private void BuildFromModel(GameObject prefab)
        {
            GameObject model = Instantiate(prefab);
            model.name = "BoxModel";
            model.transform.SetParent(transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Renderer[] rends = model.GetComponentsInChildren<Renderer>();
            float floorLocalY = 0.08f;
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float modelW = Mathf.Max(b.size.x, b.size.z);
                float scale = (modelW > 0.0001f) ? (boxWidth / modelW) : 1f;
                model.transform.localScale = Vector3.one * scale;

                // Govdeyi kutu rengine boya (tek tintable materyal)
                Material colMat = game.GetCargoMaterial(boxColor);
                foreach (Renderer r in rends) r.sharedMaterial = colMat;

                // Yeni bounds (olcekten sonra) -> ic taban Y'si
                rends = model.GetComponentsInChildren<Renderer>();
                Bounds b2 = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b2.Encapsulate(rends[i].bounds);
                floorLocalY = (b2.min.y - transform.position.y) + boxWidth * 0.10f;
            }

            // Ic kup gridi (kutu icine dizilir)
            cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(capacity)));
            rows = Mathf.CeilToInt(capacity / (float)cols);
            cellStep = (boxWidth * 0.66f) / cols;

            GameObject root = new GameObject("BoxSlots");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, floorLocalY, 0f);
            slotRoot = root.transform;
        }

        private void BuildVisual()
        {
            // Kareye yakin grid: cols ~ sqrt(capacity)
            cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(capacity)));
            rows = Mathf.CeilToInt(capacity / (float)cols);
            cellStep = boxWidth / cols;

            float interiorX = cellStep * cols;
            float interiorZ = cellStep * rows;

            Material colorMat = game.GetCargoMaterial(boxColor);
            Material floorMat = game.GetRuntimeMaterial("BoxFloor", new Color(0.90f, 0.90f, 0.93f));

            float wall = Mathf.Max(0.06f, cellStep * 0.20f);
            float wallH = cellStep * 1.15f;   // duvar yuksek -> hollow container hissi
            float floorH = 0.05f;

            // Taban (acik)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "BoxFloor";
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0f, floorH * 0.5f, 0f);
            floor.transform.localScale = new Vector3(interiorX + 2f * wall, floorH, interiorZ + 2f * wall);
            DestroyImmediateSafe(floor.GetComponent<Collider>());
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 4 duvar (kutunun rengi) - ust acik
            float halfX = interiorX * 0.5f + wall * 0.5f;
            float halfZ = interiorZ * 0.5f + wall * 0.5f;
            float wy = floorH + wallH * 0.5f;
            CreateWall(new Vector3(0f, wy, halfZ), new Vector3(interiorX + 2f * wall, wallH, wall), colorMat);
            CreateWall(new Vector3(0f, wy, -halfZ), new Vector3(interiorX + 2f * wall, wallH, wall), colorMat);
            CreateWall(new Vector3(halfX, wy, 0f), new Vector3(wall, wallH, interiorZ), colorMat);
            CreateWall(new Vector3(-halfX, wy, 0f), new Vector3(wall, wallH, interiorZ), colorMat);

            // Ic kupler bu koke dizilir
            GameObject root = new GameObject("BoxSlots");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, floorH, 0f);
            slotRoot = root.transform;
        }

        private void CreateWall(Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "BoxWall";
            w.transform.SetParent(transform, false);
            w.transform.localPosition = localPos;
            w.transform.localScale = scale;
            DestroyImmediateSafe(w.GetComponent<Collider>());
            w.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private Vector3 CellLocalPos(int index)
        {
            int c = index % cols;
            int r = index / cols;
            float x = (c - (cols - 1) * 0.5f) * cellStep;
            float z = (r - (rows - 1) * 0.5f) * cellStep;
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Bir kup ekle (kutu icine dizilir). true donerse kutu FULL oldu.
        /// </summary>
        public bool AddCube()
        {
            if (IsFull) return false;

            Material colorMat = game.GetCargoMaterial(boxColor);
            float cubeSize = cellStep * 0.80f;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Fill_" + currentFill;
            cube.transform.SetParent(slotRoot, false);
            cube.transform.localPosition = CellLocalPos(currentFill) + new Vector3(0f, cubeSize * 0.5f, 0f);
            cube.transform.localScale = Vector3.one * cubeSize;
            DestroyImmediateSafe(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().sharedMaterial = colorMat;
            fillCubes.Add(cube);

            currentFill++;
            return IsFull;
        }

        private static void DestroyImmediateSafe(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
