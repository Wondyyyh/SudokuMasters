using System;
using System.Collections.Generic;
using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

public class SudokuGenerator_new
{
    private const int Size = 9;
    private System.Random rng = new System.Random();

    #region PUBLIC API

    public (int[,] solution, int[,] puzzle, int removals) Generate(Difficulty difficulty)
    {
        int[,] solution = new int[Size, Size];
        FillBoard(solution);

        int[,] puzzle = (int[,])solution.Clone();

        int removals = RemoveNumbers(puzzle, difficulty);

        return (solution, puzzle, removals);
    }

    #endregion

    #region FULL SOLUTION GENERATION

    private bool FillBoard(int[,] board)
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (board[row, col] == 0)
                {
                    List<int> numbers = GetShuffledNumbers();

                    foreach (int num in numbers)
                    {
                        if (IsValid(board, row, col, num))
                        {
                            board[row, col] = num;

                            if (FillBoard(board))
                                return true;

                            board[row, col] = 0;
                        }
                    }

                    return false;
                }
            }
        }

        return true;
    }

    private List<int> GetShuffledNumbers()
    {
        List<int> numbers = new List<int>();
        for (int i = 1; i <= 9; i++)
            numbers.Add(i);

        for (int i = 0; i < numbers.Count; i++)
        {
            int swapIndex = rng.Next(numbers.Count);
            int temp = numbers[i];
            numbers[i] = numbers[swapIndex];
            numbers[swapIndex] = temp;
        }

        return numbers;
    }

    #endregion

    #region REMOVE NUMBERS BY DIFFICULTY

    private int RemoveNumbers(int[,] board, Difficulty difficulty)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                cells.Add(new Vector2Int(r, c));

        ShuffleCells(cells);

        int removals = GetRemovalCount(difficulty);
        int removed = 0;

        foreach (var cell in cells)
        {
            if (removed >= removals)
                break;

            int backup = board[cell.x, cell.y];
            board[cell.x, cell.y] = 0;

            if (CountSolutions((int[,])board.Clone()) == 1)
            {
                removed++;
            }
            else
            {
                board[cell.x, cell.y] = backup;
            }
        }

        return removed;
    }

    private int GetRemovalCount(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return 35;    // 46 numbers left
            case Difficulty.Normal: return 45;  // 36 left
            case Difficulty.Hard: return 55;    // 26 left
        }
        return 40;
    }

    private int GetTargetScore(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return 50;
            case Difficulty.Normal: return 150;
            case Difficulty.Hard: return 300;
        }
        return 100;
    }

    #endregion

    #region VALIDATION

    private bool IsValid(int[,] board, int row, int col, int num)
    {
        for (int i = 0; i < Size; i++)
        {
            if (board[row, i] == num) return false;
            if (board[i, col] == num) return false;
        }

        int boxRow = row / 3 * 3;
        int boxCol = col / 3 * 3;

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (board[boxRow + r, boxCol + c] == num)
                    return false;

        return true;
    }

    #endregion

    #region SOLUTION COUNTER (UNIQUENESS CHECK)

    private int CountSolutions(int[,] board)
    {
        int count = 0;
        SolveCount(board, ref count);
        return count;
    }

    private void SolveCount(int[,] board, ref int count)
    {
        if (count > 1)
            return;

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (board[row, col] == 0)
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsValid(board, row, col, num))
                        {
                            board[row, col] = num;
                            SolveCount(board, ref count);
                            board[row, col] = 0;
                        }
                    }
                    return;
                }
            }
        }

        count++;
    }

    #endregion

    #region DIFFICULTY ANALYZER

    private int AnalyzeDifficulty(int[,] board)
    {
        int score = 0;
        bool progress;

        do
        {
            progress = false;

            while (ApplyNakedSingles(board))
            {
                score += 1;
                progress = true;
            }

            while (ApplyHiddenSingles(board))
            {
                score += 2;
                progress = true;
            }

            while (ApplyNakedPairs(board))
            {
                score += 5;
                progress = true;
            }

        } while (progress);

        return score;
    }

    private bool ApplyNakedSingles(int[,] board)
    {
        bool changed = false;

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if (board[r, c] == 0)
                {
                    List<int> candidates = GetCandidates(board, r, c);
                    if (candidates.Count == 1)
                    {
                        board[r, c] = candidates[0];
                        changed = true;
                    }
                }
            }
        }

        return changed;
    }

    private bool ApplyHiddenSingles(int[,] board)
    {
        bool changed = false;

        for (int row = 0; row < Size; row++)
        {
            for (int num = 1; num <= 9; num++)
            {
                int count = 0;
                int colIndex = -1;

                for (int col = 0; col < Size; col++)
                {
                    if (board[row, col] == 0 && GetCandidates(board, row, col).Contains(num))
                    {
                        count++;
                        colIndex = col;
                    }
                }

                if (count == 1)
                {
                    board[row, colIndex] = num;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ApplyNakedPairs(int[,] board)
    {
        // Yksinkertaistettu: tarkastellaan vain rivejä
        bool changed = false;

        for (int row = 0; row < Size; row++)
        {
            Dictionary<string, List<int>> pairs = new Dictionary<string, List<int>>();

            for (int col = 0; col < Size; col++)
            {
                if (board[row, col] == 0)
                {
                    var candidates = GetCandidates(board, row, col);
                    if (candidates.Count == 2)
                    {
                        string key = candidates[0] + "," + candidates[1];
                        if (!pairs.ContainsKey(key))
                            pairs[key] = new List<int>();

                        pairs[key].Add(col);
                    }
                }
            }

            foreach (var pair in pairs)
            {
                if (pair.Value.Count == 2)
                {
                    var nums = pair.Key.Split(',');
                    int n1 = int.Parse(nums[0]);
                    int n2 = int.Parse(nums[1]);

                    for (int col = 0; col < Size; col++)
                    {
                        if (!pair.Value.Contains(col) && board[row, col] == 0)
                        {
                            var candidates = GetCandidates(board, row, col);
                            if (candidates.Remove(n1) | candidates.Remove(n2))
                                changed = true;
                        }
                    }
                }
            }
        }

        return changed;
    }

    private List<int> GetCandidates(int[,] board, int row, int col)
    {
        List<int> candidates = new List<int>();

        for (int num = 1; num <= 9; num++)
            if (IsValid(board, row, col, num))
                candidates.Add(num);

        return candidates;
    }

    #endregion

    private void ShuffleCells(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            int swapIndex = rng.Next(cells.Count);
            var temp = cells[i];
            cells[i] = cells[swapIndex];
            cells[swapIndex] = temp;
        }
    }
}