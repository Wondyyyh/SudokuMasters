using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneManager : MonoBehaviour
{
    public static SceneManager instance;
    [SerializeField] private List<GameObject> sceneObjects = new();
    [SerializeField] private GameObject BottomPanel;
    [SerializeField] private List<Button> bottom_btns = new();
    public Scene currScene;

    public enum Scene
    {
        MainMenu,
        GameView,
        WinScreen,
        Settings,
        GameTypeSelection,
        HardnessSelection,
        PauseMenu,
        ProfileView,
        MarketView,
        LeaderboardsView
    }
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

        // gameview -> toggle bottom bar off
        if (currScene == Scene.GameView)
            BottomPanel.SetActive(false);
        else if (BottomPanel.activeSelf == false)
            BottomPanel.SetActive(true);

        // exiting game view, destroy all objects in it
        if (currScene != Scene.GameView && sceneObjects[(int)Scene.GameView].transform.childCount > 0)
        {
            foreach (Transform item in sceneObjects[(int)Scene.GameView].transform)
                Destroy(item.gameObject);
        }

        ShowSelectedBottomButton(currScene);
    }
    void ShowSelectedBottomButton(Scene scene)
    {
        foreach (var btn in bottom_btns) btn.interactable = true;

        switch (currScene)
        {
            case Scene.MarketView:
                bottom_btns[0].interactable = false;
                break;
            case Scene.ProfileView:
                bottom_btns[1].interactable = false;
                break;
            case Scene.MainMenu:
                bottom_btns[2].interactable = false;
                break;
            case Scene.LeaderboardsView:
                bottom_btns[3].interactable = false;
                break;
            case Scene.Settings:
                bottom_btns[4].interactable = false;
                break;
            default:
                break;
        }
    }
    public void QuitGame() => Application.Quit();
}
