using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public static SceneManager instance;
    [SerializeField] private List<GameObject> sceneObjects = new();
    public enum Scene
    {
        MainMenu,
        GameView,
        WinScreen,
        Settings,
        GameTypeSelection,
        PauseMenu
    }
    public Scene currScene;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        ShowScene(Scene.MainMenu);
    }
    public void ActivateView(Scene scene) => sceneObjects[(int)scene].SetActive(true);
    public void DeactivateView(Scene scene) => sceneObjects[(int)scene].SetActive(false);
    public void ChangeScene(string str_scene)
    {
        if (System.Enum.TryParse(str_scene, out Scene scene))
            ShowScene(scene);
        else
            Debug.LogError($"SceneManager: Invalid scene name '{str_scene}'");
    }
    void ShowScene(Scene scene)
    {
        // Hide all scenes
        foreach (var s in sceneObjects)
        {
            s.SetActive(false);
        }
        // Show the requested scene
        currScene = scene;
        sceneObjects[(int)currScene].SetActive(true);

        if (currScene != Scene.GameView && sceneObjects[(int)Scene.GameView].transform.childCount > 0)
        {
            foreach (Transform item in sceneObjects[(int)Scene.GameView].transform)
                Destroy(item.gameObject);
        }
    }
    public void QuitGame() => Application.Quit();
}
