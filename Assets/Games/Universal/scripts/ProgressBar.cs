using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] Scrollbar scrollbar;
    [SerializeField] TMP_Text movesCount_tmp;
    [SerializeField] TMP_Text LevelHardness_tmp;

    void OnEnable()
    {
        scrollbar.size = 0f;

        // comeback to this when we have actual difficulty settings
        // for now just hardcode it to 40 removals
        string difficulty = 40 switch
        {
            <= 30 => "Easy",
            <= 50 => "Medium",
            _ => "Hard"
        };
        LevelHardness_tmp.text = difficulty;
    }

    public void UpdateProgressVisuals(float maxValue, float currentValue)
    {
        scrollbar.size = currentValue / maxValue;
        movesCount_tmp.text = $"{currentValue} / {maxValue}";
    }
}
