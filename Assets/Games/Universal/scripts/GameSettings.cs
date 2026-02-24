// small runtime holder for choices made on main menu (difficulty, optional custom removals)
public static class GameSettings
{
    public static SudokuGenerator.Difficulty SelectedDifficulty = SudokuGenerator.Difficulty.Hard;
    public static int? CustomRemovals = null; // set from menu if you want override
}