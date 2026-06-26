using ColorCargoLoop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class CleanColorCargoSceneBuilder
{
    public static void BuildCleanScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.5f;
        camera.transform.position = new Vector3(0f, 10f, -8f);
        camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        camera.backgroundColor = new Color(0.72f, 0.62f, 0.84f, 1f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.05f;
        light.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

        var gameGo = new GameObject("ArrowsPixelGame");
        gameGo.AddComponent<ArrowsPixelGame>();

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ColorCargoLoopPrototype.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/ColorCargoLoopPrototype.unity", true) };
        AssetDatabase.SaveAssets();
    }
}