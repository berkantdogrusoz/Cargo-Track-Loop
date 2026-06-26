using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorCargoLoop
{
    public sealed class SplashSceneLoader : MonoBehaviour
    {
        const string SplashSceneName = "SplashScene";
        const string FallbackNextSceneName = "ColorCargoLoopPrototype";
        const float SplashDuration = 4f;

        int nextBuildIndex = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartOnSplashScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != SplashSceneName) return;
            if (Object.FindFirstObjectByType<SplashSceneLoader>() != null) return;

            var loaderObject = new GameObject(nameof(SplashSceneLoader));
            var loader = loaderObject.AddComponent<SplashSceneLoader>();
            loader.nextBuildIndex = activeScene.buildIndex >= 0 ? activeScene.buildIndex + 1 : 1;
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(SplashDuration);
            LoadNextScene();
        }

        void LoadNextScene()
        {
            if (nextBuildIndex > 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex, LoadSceneMode.Single);
                return;
            }

            SceneManager.LoadScene(FallbackNextSceneName, LoadSceneMode.Single);
        }
    }
}