using System.Collections;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static Difficulty SelectedDifficulty = Difficulty.Easy;
    public static int? CustomRemovals = null; // set from menu if you want override
    static SudokuGenerator sudokuGenerator = new();

    public static IEnumerator SelectDifficulty_and_InitGame(Difficulty diff)
    {
        SelectedDifficulty = diff;
        yield return null; // anna yksi frame väliin
        sudokuGenerator.Generate(diff);
    }
}