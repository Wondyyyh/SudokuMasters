using UnityEngine;
using UnityEngine.UI;

public class PauseButtons : MonoBehaviour
{
    private void Start()
    {
        Button[] btns = GetComponentsInChildren<Button>();
        btns[0].onClick.AddListener(OnResumeClicked);
        btns[1].onClick.AddListener(OnQuitClicked);
    }
    void OnResumeClicked()
    {
        FindAnyObjectByType<sudoku_base>().ResumeGame();
    }
    void OnQuitClicked()
    {
        SceneManager.instance.ChangeScene("MainMenu");
    }
}
