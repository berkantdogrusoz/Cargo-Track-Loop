using ColorCargoLoop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorCargoLoopEditor
{
    public static class ColorCargoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ColorCargoLoopPrototype.unity";
        private const string RootName = "Color Cargo Loop Prototype";

        [MenuItem("Color Cargo Loop/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ColorCargoLoopPrototype";

            GameObject root = new GameObject(RootName);
            ColorCargoLoopGame game = root.AddComponent<ColorCargoLoopGame>();
            Selection.activeGameObject = root;

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.4f;
            camera.transform.position = new Vector3(0f, 12.0f, -5.0f);
            camera.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.085f, 0.06f, 0.18f);

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.45f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.transform.rotation = Quaternion.Euler(55f, -20f, 12f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtility.SetDirty(game);
            Debug.Log("Color Cargo Loop prototype scene created at " + ScenePath);
        }

        [MenuItem("Color Cargo Loop/Reset Prototype Component On Selection")]
        public static void ResetComponentOnSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("Önce hierarchy'den prototype root'unu seç.");
                return;
            }

            ColorCargoLoopGame game = selected.GetComponent<ColorCargoLoopGame>();
            if (game == null)
            {
                Debug.LogWarning("Seçili objede ColorCargoLoopGame component yok.");
                return;
            }

            Undo.RecordObject(game, "Reset CCL Component");
            Object.DestroyImmediate(game);
            selected.AddComponent<ColorCargoLoopGame>();
            EditorUtility.SetDirty(selected);
            Debug.Log("Prototype component sıfırlandı, yeni default değerler aktif.");
        }
    }
}
