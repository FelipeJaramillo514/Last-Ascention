using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartupSceneRouter
{
    private const string MainMenuSceneName = "MainMenu";
    private static bool initialSceneChecked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        initialSceneChecked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RouteInitialSceneToMainMenu()
    {
        if (initialSceneChecked)
        {
            return;
        }

        initialSceneChecked = true;
        if (!Application.isPlaying)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == MainMenuSceneName)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            Debug.LogWarning("StartupSceneRouter could not find MainMenu in build settings.");
            return;
        }

        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
    }
}
