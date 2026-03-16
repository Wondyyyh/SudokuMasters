using UnityEngine;

public class SoloGameHistory_view : MonoBehaviour
{
    public GameObject historyCard;
    public Transform contentParent;
    private GameHistory gameHistory;
    void OnEnable()
    {
        gameHistory = PlayerSaveData.instance.gameHistory;
        PopulateHistory();
    }
    void PopulateHistory()
    {
        // Clear view
        foreach (Transform t in contentParent.transform)
            Destroy(t.gameObject);

        string playerName = PlayerSaveData.instance.playerName;
        foreach (PlayedSoloGame game in gameHistory.playedSoloGames)
        {            
            GameObject newCard = Instantiate(historyCard, contentParent);
            LB_stats_card cardComponent = newCard.GetComponent<LB_stats_card>();
            cardComponent.InitCard(playerName, game.difficulty, game.time);
        }
    }
}