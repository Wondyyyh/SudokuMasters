using System;
using System.Collections.Generic;
using UnityEngine;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}
public struct DifficultyReport
{
    public float rating;
    public int nakedSingles;
    public int hiddenSingles;
    public int nakedPairs;
    public int advancedSteps;

    public override string ToString()
    {
        return $"Rating: {rating:F2} nS:{nakedSingles} hS:{hiddenSingles} nP:{nakedPairs} advS:{advancedSteps}";
    }
}
public struct DifficultyProfile
{
    public int minRemovals;
    public int maxRemovals;
    public float targetScore;
}

public class SudokuGenerator
{
    private bool[,,] candidates = new bool[9, 9, 10];
    private const int Size = 9;
    private System.Random rng = new System.Random();

    #region PUBLIC API
    public (int[,] solution, int[,] puzzle, int removals) Generate(Difficulty difficulty)
    {
        int[,] solution = new int[Size, Size];
        FillBoard(solution);

        int[,] puzzle = (int[,])solution.Clone();

        int removals = RemoveNumbers(puzzle, difficulty);

        DifficultyReport report = AnalyzeDifficulty(puzzle);

        Debug.Log($"Removed: {removals}");
        Debug.Log(report.ToString());

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
        DifficultyProfile profile = GetProfile(difficulty);

        List<Vector2Int> cells = new List<Vector2Int>();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                cells.Add(new Vector2Int(r, c));

        ShuffleCells(cells);

        int[,] bestBoard = (int[,])board.Clone();
        int removals = 0;

        foreach (var cell in cells)
        {
            int backup = board[cell.x, cell.y];
            board[cell.x, cell.y] = 0;

            if (CountSolutions((int[,])board.Clone()) != 1)
            {
                board[cell.x, cell.y] = backup;
                continue;
            }

            removals++;

            DifficultyReport report = AnalyzeDifficulty(board);

            if (removals >= profile.minRemovals)
            {
                if (report.rating > profile.targetScore)
                {
                    board[cell.x, cell.y] = backup;
                    removals--;
                    break;
                }

                bestBoard = (int[,])board.Clone();
            }

            if (removals >= profile.maxRemovals)
                break;
        }

        Array.Copy(bestBoard, board, bestBoard.Length);

        return removals;
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

    #region Difficulty mapping
    private DifficultyProfile GetProfile(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return new DifficultyProfile
                {
                    minRemovals = 32,
                    maxRemovals = 40,
                    targetScore = 80f
                };

            case Difficulty.Medium:
                return new DifficultyProfile
                {
                    minRemovals = 40,
                    maxRemovals = 52,
                    targetScore = 220f
                };

            case Difficulty.Hard:
                return new DifficultyProfile
                {
                    minRemovals = 50,
                    maxRemovals = 60,
                    targetScore = 450f
                };
        }

        return default;
    }
    #endregion

    #region DIFFICULTY ANALYZER

    public DifficultyReport AnalyzeDifficulty(int[,] original)
    {
        int[,] board = (int[,])original.Clone();

        DifficultyReport report = new DifficultyReport();
        float score = 0f;

        BuildCandidates(board);

        bool progress;

        do
        {
            progress = false;

            while (ApplyNakedSingles(board))
            {
                report.nakedSingles++;
                score += 1f;
                progress = true;
            }

            while (ApplyHiddenSingles(board))
            {
                report.hiddenSingles++;
                score += 2f;
                progress = true;
            }

            while (ApplyNakedPairs())
            {
                report.nakedPairs++;
                score += 6f;
                progress = true;
            }

            while (ApplyHiddenPairs())
            {
                report.advancedSteps++;
                score += 8f;
                progress = true;
            }

            while (ApplyPointing())
            {
                report.advancedSteps++;
                score += 10f;
                progress = true;
            }

            while (ApplyXWing())
            {
                report.advancedSteps++;
                score += 25f;
                progress = true;
            }

        } while (progress);

        if (!IsSolved(board))
        {
            score += 50f;
            report.advancedSteps++;
        }

        report.rating = score;
        return report;
    }

    private bool IsSolved(int[,] board)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (board[r, c] == 0)
                    return false;

        return true;
    }

    #region Solving techniques
    private bool ApplyNakedSingles(int[,] board)
    {
        bool changed = false;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c] != 0) continue;

                int count = 0;
                int last = 0;

                for (int n = 1; n <= 9; n++)
                {
                    if (candidates[r, c, n])
                    {
                        count++;
                        last = n;
                    }
                }

                if (count == 1)
                {
                    PlaceNumber(board, r, c, last);
                    changed = true;
                }
            }
        }

        return changed;
    }
    private bool ApplyHiddenSingles(int[,] board)
    {
        bool changed = false;

        for (int row = 0; row < 9; row++)
        {
            for (int n = 1; n <= 9; n++)
            {
                int count = 0;
                int colIndex = -1;

                for (int col = 0; col < 9; col++)
                {
                    if (candidates[row, col, n])
                    {
                        count++;
                        colIndex = col;
                    }
                }

                if (count == 1)
                {
                    PlaceNumber(board, row, colIndex, n);
                    changed = true;
                }
            }
        }

        return changed;
    }
    private bool ApplyNakedPairs()
    {
        bool changed = false;

        for (int row = 0; row < 9; row++)
        {
            Dictionary<string, List<int>> pairs = new();

            for (int col = 0; col < 9; col++)
            {
                List<int> list = new();

                for (int n = 1; n <= 9; n++)
                    if (candidates[row, col, n])
                        list.Add(n);

                if (list.Count == 2)
                {
                    string key = list[0] + "," + list[1];
                    if (!pairs.ContainsKey(key))
                        pairs[key] = new List<int>();

                    pairs[key].Add(col);
                }
            }

            foreach (var pair in pairs)
            {
                if (pair.Value.Count == 2)
                {
                    var nums = pair.Key.Split(',');
                    int n1 = int.Parse(nums[0]);
                    int n2 = int.Parse(nums[1]);

                    for (int col = 0; col < 9; col++)
                    {
                        if (!pair.Value.Contains(col))
                        {
                            if (candidates[row, col, n1]) { candidates[row, col, n1] = false; changed = true; }
                            if (candidates[row, col, n2]) { candidates[row, col, n2] = false; changed = true; }
                        }
                    }
                }
            }
        }

        return changed;
    }
    private bool ApplyHiddenPairs()
    {
        bool changed = false;

        for (int row = 0; row < 9; row++)
        {
            for (int n1 = 1; n1 <= 8; n1++)
            {
                for (int n2 = n1 + 1; n2 <= 9; n2++)
                {
                    List<int> cols = new();

                    for (int col = 0; col < 9; col++)
                        if (candidates[row, col, n1] && candidates[row, col, n2])
                            cols.Add(col);

                    if (cols.Count == 2)
                    {
                        for (int n = 1; n <= 9; n++)
                        {
                            if (n == n1 || n == n2) continue;

                            if (candidates[row, cols[0], n]) { candidates[row, cols[0], n] = false; changed = true; }
                            if (candidates[row, cols[1], n]) { candidates[row, cols[1], n] = false; changed = true; }
                        }
                    }
                }
            }
        }

        return changed;
    }
    private bool ApplyPointing()
    {
        bool changed = false;

        for (int box = 0; box < 9; box++)
        {
            int br = (box / 3) * 3;
            int bc = (box % 3) * 3;

            for (int n = 1; n <= 9; n++)
            {
                List<Vector2Int> cells = new List<Vector2Int>();

                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        if (candidates[br + r, bc + c, n])
                            cells.Add(new Vector2Int(br + r, bc + c));

                if (cells.Count >= 2)
                {
                    bool sameRow = cells.TrueForAll(x => x.x == cells[0].x);
                    bool sameCol = cells.TrueForAll(x => x.y == cells[0].y);

                    if (sameRow)
                    {
                        int row = cells[0].x;
                        for (int col = 0; col < 9; col++)
                        {
                            if (col < bc || col >= bc + 3)
                                if (candidates[row, col, n]) { candidates[row, col, n] = false; changed = true; }
                        }
                    }

                    if (sameCol)
                    {
                        int col = cells[0].y;
                        for (int row = 0; row < 9; row++)
                        {
                            if (row < br || row >= br + 3)
                                if (candidates[row, col, n]) { candidates[row, col, n] = false; changed = true; }
                        }
                    }
                }
            }
        }

        return changed;
    }
    private bool ApplyXWing()
    {
        bool changed = false;

        for (int n = 1; n <= 9; n++)
        {
            for (int r1 = 0; r1 < 8; r1++)
            {
                var cols1 = GetCandidateCols(r1, n);
                if (cols1.Count != 2) continue;

                for (int r2 = r1 + 1; r2 < 9; r2++)
                {
                    var cols2 = GetCandidateCols(r2, n);
                    if (cols2.Count != 2) continue;

                    if (cols1[0] == cols2[0] && cols1[1] == cols2[1])
                    {
                        foreach (int col in cols1)
                        {
                            for (int r = 0; r < 9; r++)
                            {
                                if (r != r1 && r != r2 && candidates[r, col, n])
                                {
                                    candidates[r, col, n] = false;
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return changed;
    }

    #endregion

    private List<int> GetCandidates(int[,] board, int row, int col)
    {
        List<int> candidates = new List<int>();

        for (int num = 1; num <= 9; num++)
            if (IsValid(board, row, col, num))
                candidates.Add(num);

        return candidates;
    }
    private List<int> GetCandidateCols(int row, int n)
    {
        List<int> cols = new();
        for (int col = 0; col < 9; col++)
            if (candidates[row, col, n])
                cols.Add(col);

        return cols;
    }
    private List<int> GetCandidateRows(int col, int number)
    {
        List<int> rows = new List<int>();

        for (int row = 0; row < 9; row++)
        {
            if (candidates[row, col, number])
                rows.Add(row);
        }

        return rows;
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
    private void BuildCandidates(int[,] board)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                for (int n = 1; n <= 9; n++)
                {
                    candidates[r, c, n] = board[r, c] == 0 && IsValid(board, r, c, n);
                }
            }
        }
    }
    private void PlaceNumber(int[,] board, int row, int col, int num)
    {
        board[row, col] = num;

        for (int n = 1; n <= 9; n++)
            candidates[row, col, n] = false;

        for (int i = 0; i < 9; i++)
        {
            candidates[row, i, num] = false;
            candidates[i, col, num] = false;
        }

        int br = row / 3 * 3;
        int bc = col / 3 * 3;

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                candidates[br + r, bc + c, num] = false;
    }
}