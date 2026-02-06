using System;
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
    public int removals = 40; // how many cells to remove (difficulty)
    public int MovesCount = 0;
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
    void Start()
    {
        GenerateSolution();
        CreatePuzzle(removals);
        BuildGrid();

        // init bar and count text
        progressBar.UpdateProgressVisuals(removals, MovesCount);
    }
    void GenerateSolution()
    {
        // start from empty
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                solution[r, c] = 0;

        FillBoard(0, 0);
    }
    bool FillBoard(int row, int col)
    {
        if (row == 9) return true; // done

        int nextRow = (col == 8) ? row + 1 : row;
        int nextCol = (col == 8) ? 0 : col + 1;

        // generate numbers 1..9 in random order
        var numbers = Enumerable.Range(1, 9).OrderBy(_ => UnityEngine.Random.value).ToArray();
        foreach (int num in numbers)
        {
            if (IsSafe(solution, row, col, num))
            {
                solution[row, col] = num;
                if (FillBoard(nextRow, nextCol)) return true;
                solution[row, col] = 0;
            }
        }

        return false;
    }
    bool IsSafe(int[,] board, int row, int col, int num)
    {
        // check row and col if num exists
        for (int i = 0; i < 9; i++)
        {
            if (board[row, i] == num) return false;
            if (board[i, col] == num) return false;
        }

        // 3x3 block row and col check
        // numb is index of cell so it allways caps to multiple of 3 one lower than itself
        // example: max i8 is going to be => 6
        int startR = (row / 3) * 3;
        int startC = (col / 3) * 3;
        for (int r = startR; r < startR + 3; r++)
            for (int c = startC; c < startC + 3; c++)
                if (board[r, c] == num) return false;

        return true;
    }
    void CreatePuzzle(int removalsCount)
    {
        //Debug.Log($"Starting puzzle creation {DateTime.Now.ToString("hh:mm:ss:fff")}");
        // copy solution into puzzle
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                puzzle[r, c] = solution[r, c];

        // create shuffled list of all cell positions
        List<(int r, int c)> cells = new();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                cells.Add((r, c));

        cells = cells.OrderBy(_ => UnityEngine.Random.value).ToList();

        int removed = 0;

        foreach (var (r, c) in cells)
        {
            if (removed >= removalsCount)
                break;

            int backup = puzzle[r, c];
            puzzle[r, c] = 0;

            // copy puzzle for solver
            int[,] testBoard = (int[,])puzzle.Clone();

            if (!HasUniqueSolution(testBoard))
            {
                // undo removal if multiple solutions
                puzzle[r, c] = backup;
            }
            else
            {
                removed++;
            }
        }

        //Debug.Log($"Puzzle creation completed at {DateTime.Now.ToString("hh:mm:ss:fff")}");
    }
    void BuildGrid()
    {
        // clear existing children if any
        cellValues.Clear();
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(gridParent.GetChild(i).gameObject);

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
                cellComp.Initialize(this, r, c, value, isGiven, label, img);

                // store reference for movement/select
                cells[r, c] = cellComp;
            }
        }
    }
    // called by cells when user changes a value
    public void OnCellValueChanged(int row, int col, int value, bool correct, sudoku_cell cellComp)
    {
        if (puzzle[row, col] == value)
            return;

        puzzle[row, col] = value;
        cellComp.isCorrect = value != 0 && correct;

        // optional: could do more bookkeeping here
        if (IsSolved())
        {
            Debug.Log($"Sudoku solved in time of: {timer.StopTimer()}");
            SceneManager.instance.ActivateView(SceneManager.Scene.WinScreen);
        }
        if (correct)
        {
            if (value != 0) MovesCount++;
            cellValues[cellComp] = value;
            progressBar.UpdateProgressVisuals(removals, MovesCount);
            ShowAllSameValuesOnGrid(value == 0 ? null : value);
        }
    }
    public void ShowAllSameValuesOnGrid(int? value)
    {
        if (value == null)
        {
            // clear all highlights
            cellValues.Keys.ToList().ForEach(kv => kv.SetSelected(false));
            sudoku_cell.Selected?.SetSelected(true); // keep current selected highlighted
            return;
        }
        cellValues.Where(kv => kv.Value == value).ToList().ForEach(kv => kv.Key.SetSelected(true));
        cellValues.Where(kv => kv.Value != value).ToList().ForEach(kv => kv.Key.SetSelected(false));
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

    bool HasUniqueSolution(int[,] board)
    {
        int solutions = 0;
        CountSolutions(board, ref solutions);
        return solutions == 1;
    }
    void CountSolutions(int[,] board, ref int solutions)
    {
        if (solutions > 1) return;

        // find first empty cell
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c] == 0)
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsSafe(board, r, c, num))
                        {
                            board[r, c] = num;
                            CountSolutions(board, ref solutions);
                            board[r, c] = 0;

                            if (solutions > 1) return;
                        }
                    }
                    return; // important: don't continue after empty cell
                }
            }
        }

        // no empty cells → found a solution
        solutions++;
    }
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
