using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] Scrollbar scrollbar;
    [SerializeField] TMP_Text movesCount_tmp;    

    void OnEnable()
    {
        scrollbar.size = 0f;
    }

    public void UpdateProgressVisuals(float maxValue, float currentValue)
    {
        scrollbar.size = currentValue / maxValue;
        movesCount_tmp.text = $"{currentValue} / {maxValue}";
    }
}
