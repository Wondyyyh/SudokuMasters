using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private GameObject Statsview, ProgressView, Levelup_popup;
    [SerializeField] private Scrollbar progressbar;
    [SerializeField] private TMP_Text lvlbarcurr_txt, lvlbarnext_txt, popup_lvl_txt, gameTime_txt;
    [SerializeField] private Sprite emptystar, goldenstar;
    [SerializeField] private List<Image> starPositions;
    [SerializeField] private Button continue_btn;

    private PlayedSoloGame lastGame => PlayerSaveData.instance.gameHistory.playedSoloGames.Last();

    private void OnEnable()
    {
        ProgressView.SetActive(false);
        Levelup_popup.SetActive(false);

        Statsview.SetActive(true);
        StatsViewInit();
    }
    private void StatsViewInit()
    {        
        for (int i = 0; i < starPositions.Count; i++)
        {
            if (i < lastGame.stars)
                starPositions[i].sprite = goldenstar;
            else
                starPositions[i].sprite = emptystar;
        }
        gameTime_txt.text = lastGame.time;
    }
    public void ContinueBtnPress()
    {
        if (ProgressView.activeInHierarchy) { SceneManager.instance.ChangeScene("MainMenu"); }
        else 
        {
            Statsview.SetActive(false); 
            Levelup_popup.SetActive(false);

            ProgressView.SetActive(true);
            continue_btn.interactable = false; // prevent continue during animation
            ProgressViewInit(); 
        }
    }
    private void ProgressViewInit()
    {
        // start state
        // match profile progress calculation: progress within current level = (XP - prevLUTH) / (LevelUpTreshold - prevLUTH)
        float progress = (float)(PlayerSaveData.instance.XP - PlayerSaveData.instance.prevLUTH) / (float)(PlayerSaveData.instance.LevelUpTreshold - PlayerSaveData.instance.prevLUTH);
        progressbar.size = progress;        

        // init labels
        lvlbarcurr_txt.text = PlayerSaveData.instance.Level.ToString();
        lvlbarnext_txt.text = (PlayerSaveData.instance.Level + 1).ToString();
        popup_lvl_txt.text = (PlayerSaveData.instance.Level + 1).ToString();

        // animate XP gain and level-ups using a coroutine so we can visualize progress + popups
        StartCoroutine(AnimateXpGain(lastGame.score));
    }

    System.Collections.IEnumerator AnimateXpGain(int xpGained)
    {
        // local snapshot so we can animate without mutating saved data until finished
        var pdata = PlayerSaveData.instance;
        int localLevel = pdata.Level;
        int localXP = pdata.XP;
        int localPrev = pdata.prevLUTH;
        int localThreshold = pdata.LevelUpTreshold;

        int remaining = xpGained;

        PlayerSaveData.instance.GiveXp(lastGame.score); // update saved data so we don't lose progress if the player quits during animation

        // small guard
        if (remaining <= 0) yield break;

        while (remaining > 0)
        {
            // update UI labels
            lvlbarcurr_txt.text = localLevel.ToString();
            lvlbarnext_txt.text = (localLevel + 1).ToString();
            popup_lvl_txt.text = (localLevel + 1).ToString();

            int xpToNext = localThreshold - localXP;

            if (xpToNext <= 0)
            {
                // already past threshold: advance level without animation
                localPrev = localThreshold;
                localLevel++;
                localThreshold += (localThreshold / 10) + 100;
                continue;
            }

            if (remaining >= xpToNext)
            {
                // animate from current normalized progress to full
                float from = (float)(localXP - localPrev) / (float)(localThreshold - localPrev);
                float to = 1f;
                float duration = Mathf.Clamp(xpToNext * 0.01f, 0.2f, 1.2f);
                yield return LerpProgressBar(from, to, duration);

                // consume xp and trigger levelup popup
                remaining -= xpToNext;
                localXP += xpToNext; // localXP is absolute
                localPrev = localThreshold;
                localLevel++;
                // show popup
                Levelup_popup.SetActive(true);
                popup_lvl_txt.text = localLevel.ToString();
                yield return new WaitForSeconds(1f);
                Levelup_popup.SetActive(false);

                // increase threshold like GiveXp does
                localThreshold += (localThreshold / 10) + 100;
            }
            else
            {
                // partial fill and finish
                int finalXP = localXP + remaining;
                float from = (float)(localXP - localPrev) / (float)(localThreshold - localPrev);
                float to = (float)(finalXP - localPrev) / (float)(localThreshold - localPrev);
                float duration = Mathf.Clamp(remaining * 0.01f, 0.2f, 1.2f);
                yield return LerpProgressBar(from, to, duration);

                localXP = finalXP;
                remaining = 0;
            }
        }

        // ensure UI labels reflect final state
        lvlbarcurr_txt.text = pdata.Level.ToString();
        lvlbarnext_txt.text = (pdata.Level + 1).ToString();

        continue_btn.interactable = true; // enable btn to home screen
    }

    System.Collections.IEnumerator LerpProgressBar(float from, float to, float duration)
    {
        float elapsed = 0f;
        progressbar.size = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float val = Mathf.Lerp(from, to, t);
            progressbar.size = val;
            yield return null;
        }
        progressbar.size = to;
    }
}
