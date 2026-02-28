using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class sudoku_base : MonoBehaviour
{
    int[,] solution = new int[9, 9];
    int[,] puzzle = new int[9, 9];

    [SerializeField] private GameObject cellPrefab;    
    public int removals = 40; // legacy field (kept for inspector compatibility)
    public int MovesCount = 0;
    public RectTransform gridParent;
    OnScreenTimer timer => GetComponentInChildren<OnScreenTimer>();
    ProgressBar progressBar => GetComponentInChildren<ProgressBar>();

    public Dictionary<sudoku_cell, int> cellValues = new();

    SudokuGenerator SudokuGenerator = new SudokuGenerator();

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
    void Start()
    {       
        StartCoroutine(GenerateSudoku());
    }
    IEnumerator GenerateSudoku()
    {
        yield return null; // anna yksi frame väliin

        // pick difficulty from GameSettings
        Difficulty diff = (Difficulty)GameSettings.SelectedDifficulty;

        var (genSolution, genPuzzle, genRemovals) = SudokuGenerator.Generate(diff);
        solution = genSolution;
        puzzle = genPuzzle;
        removals = genRemovals;

        Debug.Log("Sudoku valmis!");

        yield return null;

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
                var img = obj.GetComponent<Image>() ?? obj.GetComponentInChildren<Image>();

                // get or add sudoku_cell component (must exist on prefab ideally)
                var cellComp = obj.GetComponent<sudoku_cell>();
                if (cellComp == null) cellComp = obj.AddComponent<sudoku_cell>();


                int value = puzzle[r, c];
                cellValues.Add(cellComp, value);
                bool isGiven = value != 0;
                if (value == 0) emptycounter++;
                cellComp.Initialize(this, r, c, value, isGiven, label, img);

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

        // optional: could do more bookkeeping here
        if (IsSolved())
        {
            Debug.Log($"Sudoku solved in time of: {timer.StopTimer()}");
            SceneManager.instance.ActivateView(SceneManager.Scene.WinScreen);
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
        var keys = cellValues.Keys.ToList();
        keys.Remove(sudoku_cell.Selected); // exclude currently selected cell
        if (value == null)
        {
            // clear all highlights
            keys.ForEach(kv => kv.SetSelected(false));
            //sudoku_cell.Selected?.SetSelected(true); // keep current selected highlighted
            return;
        }
        keys.Where(kv => kv.currentValue == value).ToList().ForEach(kv => kv.SetSelected(true));
        keys.Where(kv => kv.currentValue != value).ToList().ForEach(kv => kv.SetSelected(false));
        //sudoku_cell.Selected?.SetSelected(true); // keep current selected highlighted
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
