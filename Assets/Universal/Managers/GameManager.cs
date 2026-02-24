using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int GameIndex = 0;
    [SerializeField] private List<GameObject> gameTypes = new();
    [SerializeField] private Transform gameView;
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public void CreateGame(int hardness)
    {
        GameSettings.SelectedDifficulty = (SudokuGenerator.Difficulty)hardness;        
        // clear previous game if any
        foreach (Transform child in gameView)        
            Destroy(child.gameObject);
        
        Instantiate(gameTypes[GameIndex], gameView);
    }
    public void SetGameType(int type_i)
    {
        GameIndex = type_i;
    }
}
