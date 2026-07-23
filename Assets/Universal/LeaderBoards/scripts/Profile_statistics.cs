using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Profile_statistics : MonoBehaviour
{
    private List<PlayedSoloGame> currSelectedList = new();    
    [SerializeField] private TMPro.TMP_Text playedGames_txt, bestTime_txt, AVGTime_txt, name_txt;
    [Header("Profile Card")]
    [SerializeField] private Scrollbar level_slider;
    [SerializeField] private TMPro.TMP_Text slider_currlvl_txt, slider_nextlvl_txt, PC_currlvl_txt, XpNeeded_txt;
    
    void OnEnable()
    {
        currSelectedList = new(PlayerSaveData.instance.gameHistory.playedSoloGames);
        if (currSelectedList.Count > 0)
        {
            if(playedGames_txt.text == currSelectedList.Count.ToString())
                return; // early exit if the displayed stats are already up to date

            name_txt.text = PlayerSaveData.instance.playerName;
            playedGames_txt.text = currSelectedList.Count.ToString();
            bestTime_txt.text = TimeSpan.FromSeconds(currSelectedList.Select(x => TimeSpan.Parse(x.time).TotalSeconds).Min()).ToString(@"hh\:mm\:ss\.fff");
            AVGTime_txt.text = TimeSpan.FromSeconds(currSelectedList.Select(x => TimeSpan.Parse(x.time).TotalSeconds).Average()).ToString(@"hh\:mm\:ss\.fff");
            slider_currlvl_txt.text = PlayerSaveData.instance.Level.ToString();
            PC_currlvl_txt.text = PlayerSaveData.instance.Level.ToString();
            slider_nextlvl_txt.text = (PlayerSaveData.instance.Level+1).ToString();
            level_slider.size = 
                (PlayerSaveData.instance.XP - PlayerSaveData.instance.prevLUTH) / 
                (float)(PlayerSaveData.instance.LevelUpTreshold - PlayerSaveData.instance.prevLUTH);
            XpNeeded_txt.text = $"{(PlayerSaveData.instance.LevelUpTreshold - PlayerSaveData.instance.XP)}xp needed";
        }
        else
        {
            playedGames_txt.text = "0";
            bestTime_txt.text = "00:00:00.000";
            AVGTime_txt.text = "00:00:00.000";
            slider_currlvl_txt.text = "1";
            PC_currlvl_txt.text = "1";
            slider_nextlvl_txt.text = "2";
            level_slider.size = 0;
            XpNeeded_txt.text = $"{(PlayerSaveData.instance.LevelUpTreshold - PlayerSaveData.instance.XP)}xp needed";
        }

    }
}
