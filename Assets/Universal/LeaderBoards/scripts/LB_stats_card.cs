using UnityEngine;

public class LB_stats_card : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text name_txt, level_txt, date_txt, time_txt;

    public void InitCard(string name, string level, string date, string time)
    {
        name_txt.text = name;
        level_txt.text = level;
        date_txt.text = date;
        time_txt.text = time;
    }
}
