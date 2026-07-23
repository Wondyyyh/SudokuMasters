using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoards_view : MonoBehaviour
{
    public GameObject historyCard;
    public Transform contentParent;
    [SerializeField] private List<Button> tab_btns;
    private List<PlayedSoloGame> currSelectedList = new();
    private string currDifficulty = "Easy";

    [Header("Static Card")]
    [SerializeField] private TMPro.TMP_Text name_txt;
    [SerializeField] private TMPro.TMP_Text level_txt, date_txt, time_txt;
    void OnEnable()
    {
        currSelectedList = new(PlayerSaveData.instance.gameHistory.playedSoloGames);
        PopulateHistory(currSelectedList);

        tab_btns[0].onClick.AddListener(FriendsTabOpen);
        tab_btns[1].onClick.AddListener(GlobalTabOpen);
        tab_btns[2].onClick.AddListener(DailyTabOpen);
    }
    void PopulateHistory(List<PlayedSoloGame> gameHistory)
    {
        // Clear view
        foreach (Transform t in contentParent.transform)
            Destroy(t.gameObject);

        // Filter by difficulty
        gameHistory = gameHistory.Where(x => x.difficulty == currDifficulty).OrderBy(x => x.time).ToList();

        // Create card for each game
        string playerName = PlayerSaveData.instance.playerName;
        foreach (PlayedSoloGame game in gameHistory)
        {
            GameObject newCard = Instantiate(historyCard, contentParent);
            LB_stats_card cardComponent = newCard.GetComponent<LB_stats_card>();
            cardComponent.InitCard(playerName, game.difficulty, game.date, game.time);
        }

        string avgTime = "00:00:00.000";
        if (gameHistory.Count > 0)
            avgTime = TimeSpan.FromSeconds(gameHistory.Select(x => TimeSpan.Parse(x.time).TotalSeconds).Average()).ToString(@"hh\:mm\:ss\.fff");
        SetupStaticCard(avgTime);
    }
    void SetupStaticCard(string avgTime)
    {
        name_txt.text = PlayerSaveData.instance.playerName;
        level_txt.text = currDifficulty;
        date_txt.text = "AVG time";
        time_txt.text = avgTime;
    }
    void DailyTabOpen()
    {
        // Filter by date -> show only games played today
        currSelectedList = new(PlayerSaveData.instance.gameHistory.playedSoloGames.Where(x => x.date.Contains(DateTime.Now.ToString("dd-MM-yyyy"))).ToList());
        PopulateHistory(currSelectedList);
    }
    void GlobalTabOpen()
    {
        currSelectedList = new(PlayerSaveData.instance.gameHistory.playedSoloGames);
        PopulateHistory(currSelectedList);
    }
    void FriendsTabOpen()
    {
        // Come change later when we have friends
        currSelectedList = new(PlayerSaveData.instance.gameHistory.playedSoloGames.Where(x => x.game_mode == "9x9"));
        PopulateHistory(currSelectedList);
    }
    public void DifficultyChanged(int newDifficulty)
    {
        switch (newDifficulty)
        {
            case 0:
                currDifficulty = "Easy";
                break;
            case 1:
                currDifficulty = "Medium";
                break;
            case 2:
                currDifficulty = "Hard";
                break;
        }

        PopulateHistory(currSelectedList);
    }
}