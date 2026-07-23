using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSaveData : MonoBehaviour
{
    public static PlayerSaveData instance; // <-- Singleton instance
    public int Level, XP, Coins, LevelUpTreshold, prevLUTH;
    public string playerName = "Player1";
    public GameHistory gameHistory {get; private set;} = new();

    void Start()
    {
        instance = this;        
        gameHistory = LoadGameHistory();        

        Debug.Log($"Loaded game history with {gameHistory.playedSoloGames.Count} games.");
        LoadPlayerData();
    }
    public void AddPlayedGame(TimeSpan time, int moves_count, int score, int mistakes, int stars, string game_mode, string difficulty)
    {
        PlayedSoloGame newGame = new()
        {            
            time = time.ToString(@"hh\:mm\:ss\.fff"),
            date = DateTime.Now.ToString("dd-MM-yyyy HH:mm"), // Automatically set the date to the current time
            moves_count = moves_count,
            score = score,
            mistakes = mistakes,
            stars = stars,
            game_mode = game_mode,
            difficulty = difficulty
        };        
        gameHistory.playedSoloGames.Add(newGame);
        SaveGameHistory();

        //GiveXp(score); // Give XP for playing a game
    }
    public void GiveXp(int xp = 0)
    {
        XP += xp;
        if (XP >= LevelUpTreshold)
        {
            Level++;
            print($"Player leveled up to {Level} lvl");
            prevLUTH = LevelUpTreshold;
            LevelUpTreshold += (LevelUpTreshold / 10) + 100;
            print($"XP is {XP} and new treshold is {LevelUpTreshold}xp");
        }

        if (XP >= LevelUpTreshold)
            GiveXp(0); // Check again in case of multiple level-ups
        else
            SavePlayerData(this);
    }
    public void SavePlayerData(PlayerSaveData data)
    {
        PlayerPrefs.SetInt("Level", data.Level);
        PlayerPrefs.SetInt("LevelUpTreshold", data.LevelUpTreshold);
        PlayerPrefs.SetInt("prevLUTH", data.prevLUTH);
        PlayerPrefs.SetInt("XP", data.XP);
        PlayerPrefs.SetInt("Coins", data.Coins);
        PlayerPrefs.Save();
    }
    public void LoadPlayerData()
    {
        PlayerSaveData.instance.Level = PlayerPrefs.GetInt("Level", 1);
        PlayerSaveData.instance.LevelUpTreshold = PlayerPrefs.GetInt("LevelUpTreshold", 100);
        PlayerSaveData.instance.prevLUTH = PlayerPrefs.GetInt("prevLUTH", 0);
        PlayerSaveData.instance.XP = PlayerPrefs.GetInt("XP", 0);
        PlayerSaveData.instance.Coins = PlayerPrefs.GetInt("Coins", 0);               
    }
    public void SaveGameHistory()
    {
        string json = JsonUtility.ToJson(gameHistory);
        PlayerPrefs.SetString("GameHistory", json);
        PlayerPrefs.Save();
    }
    public GameHistory LoadGameHistory()
    {
        string json = PlayerPrefs.GetString("GameHistory", "{}");
        return JsonUtility.FromJson<GameHistory>(json);
    }
}
[Serializable]
public class GameHistory
{
    public List<PlayedSoloGame> playedSoloGames = new();
}
[Serializable]
public class PlayedSoloGame
{
    public string date;
    public string time;
    public int moves_count, score, mistakes, stars;
    public string game_mode, difficulty;
}
