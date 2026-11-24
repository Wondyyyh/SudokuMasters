using System;
using UnityEngine;

// Local gameplay preferences. Both assists are opt-in and persist between sessions.
public static class SudokuAssistSettings
{
    const string HighlightKey = "Sudoku.Assists.HighlightPeers";
    const string CleanupKey = "Sudoku.Assists.CleanNotes";
    public static event Action Changed;
    public static bool HighlightPeers
    {
        get => PlayerPrefs.GetInt(HighlightKey, 0) != 0;
        set => Set(HighlightKey, value);
    }
    public static bool CleanNotes
    {
        get => PlayerPrefs.GetInt(CleanupKey, 0) != 0;
        set => Set(CleanupKey, value);
    }
    static void Set(string key, bool value)
    {
        if ((PlayerPrefs.GetInt(key, 0) != 0) == value) return;
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
