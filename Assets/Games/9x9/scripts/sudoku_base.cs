using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class sudoku_base : MonoBehaviour
{
    int[,] solution = new int[9, 9];
    int[,] puzzle = new int[9, 9];
    public int removals = 40; // legacy field (kept for inspector compatibility)
    public int MovesCount = 0;

    [SerializeField] private GameObject cellPrefab;
    public RectTransform gridParent;
    OnScreenTimer timer => GetComponentInChildren<OnScreenTimer>();
    ProgressBar progressBar => GetComponentInChildren<ProgressBar>();

    public Dictionary<sudoku_cell, int> cellValues = new();    

    // keep direct references for movement/select logic
    private sudoku_cell[,] cells = new sudoku_cell[9, 9];

    private void OnEnable()
    {
        InputActionsManager.PausePressed += PauseGame;
        InputActionsManager.MovePressed += OnMoveSelection;
    }
    private void OnDisable()
    {
        InputActionsManager.PausePressed -= PauseGame;
        InputActionsManager.MovePressed -= OnMoveSelection;
    }
    IEnumerator Start()
    {
        // wait until sudokugenerator finishes
        yield return new WaitUntil(() => SudokuGenerator.puzzle != null);

        solution = SudokuGenerator.solution;
        puzzle = SudokuGenerator.puzzle;
        removals = SudokuGenerator.removals;

        BuildGrid();
        // init bar and count text
        progressBar.UpdateProgressVisuals(removals, MovesCount);
    }

    void BuildGrid()
    {
        // clear existing children if any
        cellValues.Clear();
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(gridParent.GetChild(i).gameObject);

        int emptycounter = 0;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var obj = Instantiate(cellPrefab, gridParent);
                // try to find TMP_Text and Image on prefab (common convention)
                var label = obj.GetComponentInChildren<TMP_Text>();                

                // get or add sudoku_cell component (must exist on prefab ideally)
                var cellComp = obj.GetComponent<sudoku_cell>();
                if (cellComp == null) cellComp = obj.AddComponent<sudoku_cell>();


                int value = puzzle[r, c];
                cellValues.Add(cellComp, value);
                bool isGiven = value != 0;
                if (value == 0) emptycounter++;
                cellComp.Initialize(this, r, c, value, isGiven, label);

                // store reference for movement/select
                cells[r, c] = cellComp;
            }
        }

        Debug.Log($"Grid built with {emptycounter} empty cells.");
    }
    // called by cells when user changes a value
    public void OnCellValueChanged(int row, int col, int value, bool validMove, sudoku_cell cellComp)
    {
        if (puzzle[row, col] == value)
            return;

        puzzle[row, col] = value;
        cellComp.isCorrect = value != 0 && validMove;
        
        if (IsSolved())
        {
            WinGame();
        }
        if (validMove)
        {
            if (value != 0) MovesCount++;
            cellValues[cellComp] = value;
            progressBar.UpdateProgressVisuals(removals, MovesCount);
            ShowAllSameValuesOnGrid(value == 0 ? null : value);
        }
    }
    public void ShowAllSameValuesOnGrid(int? value)
    {
        // create temp list and exclude curr selection
        var keys = cellValues.Keys.ToList();
        keys.Remove(sudoku_cell.Selected);

        // empty cell selected, clear all highlights
        if (value == null)
        {
            keys.ForEach(kv => kv.SetSelected(false));
            keys.Where(kv => kv.cornerMarkers.Count > 0 || kv.middleMarkers.Count > 0).ToList().
                ForEach(kv => StartCoroutine(kv.HighLight_currNumb_Markers(null)));

            if(sudoku_cell.Selected != null)
                StartCoroutine(sudoku_cell.Selected.HighLight_currNumb_Markers(null));
        }

        // higlight all cells with same value as current selected cell
        else
        {
            keys.Where(kv => kv.currentValue == value).ToList().ForEach(kv => kv.SetSelected(true));
            keys.Where(kv => kv.currentValue != value).ToList().ForEach(kv => kv.SetSelected(false));
            keys.Where(kv => kv.cornerMarkers.Count > 0 || kv.middleMarkers.Count > 0).ToList().
                ForEach(kv => StartCoroutine(kv.HighLight_currNumb_Markers(value)));
        }
    }
    public bool ValidateMove(int row, int col, int value)
    {
        if (value == 0) return true; // clearing is always allowed
        return solution[row, col] == value;
    }
    bool IsSolved()
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (puzzle[r, c] == 0 || puzzle[r, c] != solution[r, c])
                    return false;
        return true;
    }
    // optional: expose solution for hint feature
    public int GetSolutionAt(int row, int col) => solution[row, col];

    void PauseGame()
    {
        SceneManager.instance.ActivateView(SceneManager.Scene.PauseMenu);
        timer.PauseTimer();
    }
    public void ResumeGame()
    {
        SceneManager.instance.DeactivateView(SceneManager.Scene.PauseMenu);
        timer.ResumeTimer();
    }
    public void WinGame()
    {
        TimeSpan timeTaken = timer.StopTimer();
        Debug.Log($"Sudoku solved in time of: {timeTaken}");
        SceneManager.instance.ActivateView(SceneManager.Scene.WinScreen);
        int mistakes = 0, stars = 5;
        int score = GameSettings.SelectedDifficulty switch
        {
            Difficulty.Easy => timeTaken < TimeSpan.FromMinutes(5) ? 200 : 100,
            Difficulty.Medium => timeTaken < TimeSpan.FromMinutes(10) ? 300 : 100,
            Difficulty.Hard => timeTaken < TimeSpan.FromMinutes(20) ? 400 : 150,
            _ => 500
        };
        PlayerSaveData.instance.AddPlayedGame(timeTaken, MovesCount, score, mistakes, stars, "9x9", GameSettings.SelectedDifficulty.ToString());
    }
    // movement handler: dr = -1..1, dc = -1..1
    private void OnMoveSelection(int dr, int dc)
    {
        if (dr == 0 && dc == 0) return;

        var current = sudoku_cell.Selected;
        sudoku_cell target = null;

        if (current == null)
        {
            // if nothing selected, pick cell middle of grid
            target = cells[4, 4];
            if (target != null) sudoku_cell.Selected = target;
            ShowAllSameValuesOnGrid(target.currentValue != 0 ? target.currentValue : null);
            return;
        }

        // wrap-around movement: compute new coords modulo 9
        int newR = (current.Row + dr + 9) % 9;
        int newC = (current.Col + dc + 9) % 9;

        // set selection to wrapped cell (includes given cells)
        sudoku_cell.Selected = cells[newR, newC];

        int currVal = sudoku_cell.Selected.currentValue;
        ShowAllSameValuesOnGrid(currVal != 0 ? currVal : null);
    }
}
