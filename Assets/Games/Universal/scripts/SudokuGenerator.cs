using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SudokuGenerator
{
    private static System.Random rng = new System.Random();
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    // ================= PUBLIC API =================
    public static (int[,] solution, int[,] puzzle, int removals) Generate
        (Difficulty difficulty, int maxAttempts = 1000, bool debugDumpNearMisses = true)
    {
        // defaults for minimum empty cells per difficulty (used when settings == null)
        const int DefaultMinEmptyEasy = 20;
        const int DefaultMinEmptyMedium = 30;
        const int DefaultMinEmptyHard = 40;

        int[,] solution = new int[9, 9];
        FillGridRandom(solution);

        int[,] puzzle = (int[,])solution.Clone();
        int removals = 0;

        // Otherwise do attempts to find puzzle matching difficulty + minimum empty cells
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            puzzle = (int[,])solution.Clone();
            var order = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToList();

            foreach (int idx in order)
            {
                int r = idx / 9, c = idx % 9;
                if (puzzle[r, c] == 0) continue;

                int backup = puzzle[r, c];
                puzzle[r, c] = 0;

                if (!HasUniqueSolution(puzzle))
                {
                    puzzle[r, c] = backup;
                    continue;
                }

                var profile = LogicalSolver.Evaluate(puzzle);

                // count empty cells (in-game moves)
                int emptyCount = 0;
                for (int rr = 0; rr < 9; rr++)
                    for (int cc = 0; cc < 9; cc++)
                        if (puzzle[rr, cc] == 0) emptyCount++;

                // get threshold from settings or defaults
                int minEmpty = difficulty switch
                {
                    Difficulty.Easy => DefaultMinEmptyEasy,
                    Difficulty.Medium => DefaultMinEmptyMedium,
                    Difficulty.Hard => DefaultMinEmptyHard,
                    _ => DefaultMinEmptyMedium
                };

                if (Accept(profile, difficulty) && emptyCount >= minEmpty)
                    return (solution, puzzle, removals);

                // otherwise continue carving
            }

            // final safe-carve pass (no acceptance checks here)
            for (int idx = 0; idx < 81; idx++)
            {
                int r = idx / 9, c = idx % 9;
                if (puzzle[r, c] == 0) continue;
                int backup = puzzle[r, c];
                puzzle[r, c] = 0;
                if (!HasUniqueSolution(puzzle))
                {
                    puzzle[r, c] = backup;
                    continue;
                }
            }

            if (debugDumpNearMisses && (attempt % 100 == 0))
            {
                Debug.Log($"SudokuGenerator: attempts {attempt}/{maxAttempts} so far, no matching puzzle yet.");
            }
        }

        // fallback: return last carved puzzle
        return (solution, puzzle, removals);
    }

    // ================= DIFFICULTY ACCEPTANCE =================
    private static bool Accept(DifficultyProfile p, Difficulty d)
    {
        return d switch
        {
            // require at least two logical assignments for easy by solver logic (kept)
            Difficulty.Easy =>
                p.usedPairs == false && p.stalled == false && p.assignmentCount >= 2,

            Difficulty.Medium =>
                p.usedPairs == true && p.stalled == false,

            Difficulty.Hard =>
                p.stalled == true,

            _ => false
        };
    }

    // ================= GRID GENERATION =================
    private static bool FillGridRandom(int[,] grid)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (grid[r, c] != 0) continue;

                var nums = Enumerable.Range(1, 9).OrderBy(_ => rng.Next()).ToList();
                foreach (int n in nums)
                {
                    if (IsSafe(grid, r, c, n))
                    {
                        grid[r, c] = n;
                        if (FillGridRandom(grid)) return true;
                        grid[r, c] = 0;
                    }
                }
                return false;
            }
        }
        return true;
    }
    private static bool IsSafe(int[,] grid, int r, int c, int n)
    {
        for (int i = 0; i < 9; i++)
        {
            if (grid[r, i] == n || grid[i, c] == n)
                return false;
        }

        int br = (r / 3) * 3;
        int bc = (c / 3) * 3;
        for (int rr = br; rr < br + 3; rr++)
            for (int cc = bc; cc < bc + 3; cc++)
                if (grid[rr, cc] == n)
                    return false;

        return true;
    }

    // ================= UNIQUE SOLUTION =================
    private static bool HasUniqueSolution(int[,] grid)
    {
        int count = 0;
        SolveCount((int[,])grid.Clone(), ref count);
        return count == 1;
    }
    private static void SolveCount(int[,] grid, ref int count)
    {
        if (count > 1) return;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (grid[r, c] == 0)
                {
                    for (int n = 1; n <= 9; n++)
                    {
                        if (IsSafe(grid, r, c, n))
                        {
                            grid[r, c] = n;
                            SolveCount(grid, ref count);
                            grid[r, c] = 0;
                        }
                    }
                    return;
                }
            }
        }
        count++;
    }
}

// ================= LOGICAL SOLVER =================
public struct DifficultyProfile
{
    public bool usedPairs;
    public bool stalled;
    // number of logical assignments
    // (naked/hidden singles assigned) performed by solver
    public int assignmentCount;
}
public static class LogicalSolver
{
    public static DifficultyProfile Evaluate(int[,] puzzle)
    {
        var cands = InitCandidates(puzzle);
        bool usedPairs = false;
        int assignmentCount = 0;

        while (true)
        {
            bool progress = false;

            // count assigned before singles
            int beforeAssigned = cands.Count(s => s.Count == 1);
            if (ApplyNakedSingles(cands))
            {
                int afterAssigned = cands.Count(s => s.Count == 1);
                assignmentCount += Math.Max(0, afterAssigned - beforeAssigned);
                progress = true;
                continue;
            }

            // hidden singles also produce assignments
            beforeAssigned = cands.Count(s => s.Count == 1);
            if (ApplyHiddenSingles(cands))
            {
                int afterAssigned = cands.Count(s => s.Count == 1);
                assignmentCount += Math.Max(0, afterAssigned - beforeAssigned);
                progress = true;
                continue;
            }

            if (ApplyNakedPairs(cands))
            {
                usedPairs = true;
                progress = true;
                continue;
            }

            // no further basic progress
            if (!progress) break;
        }

        bool stalled = cands.Any(s => s.Count > 1);
        return new DifficultyProfile
        {
            usedPairs = usedPairs,
            stalled = stalled,
            assignmentCount = assignmentCount
        };
    }
    private static HashSet<int>[] InitCandidates(int[,] grid)
    {
        var c = new HashSet<int>[81];
        for (int i = 0; i < 81; i++)
        {
            int r = i / 9, col = i % 9;
            if (grid[r, col] != 0)
                c[i] = new HashSet<int> { grid[r, col] };
            else
            {
                c[i] = new HashSet<int>();
                for (int n = 1; n <= 9; n++)
                    if (IsSafe(grid, r, col, n))
                        c[i].Add(n);
            }
        }
        return c;
    }

    private static bool ApplyNakedSingles(HashSet<int>[] cands)
    {
        bool changed = false;

        for (int i = 0; i < 81; i++)
        {
            if (cands[i].Count == 1)
            {
                int v = cands[i].First();
                int r = i / 9, c = i % 9;

                foreach (int p in Peers(r, c))
                {
                    if (cands[p].Remove(v))
                        changed = true;
                }
            }
        }

        return changed;
    }
    private static bool ApplyHiddenSingles(HashSet<int>[] cands)
    {
        bool changed = false;
        // rows
        for (int r = 0; r < 9; r++)
        {
            for (int digit = 1; digit <= 9; digit++)
            {
                var positions = new List<int>();
                for (int c = 0; c < 9; c++)
                    if (cands[r * 9 + c].Contains(digit)) positions.Add(r * 9 + c);
                if (positions.Count == 1)
                {
                    var set = cands[positions[0]];
                    if (set.Count > 1) { set.RemoveWhere(x => x != digit); changed = true; }
                }
            }
        }
        // cols
        for (int c = 0; c < 9; c++)
        {
            for (int digit = 1; digit <= 9; digit++)
            {
                var positions = new List<int>();
                for (int r = 0; r < 9; r++)
                    if (cands[r * 9 + c].Contains(digit)) positions.Add(r * 9 + c);
                if (positions.Count == 1)
                {
                    var set = cands[positions[0]];
                    if (set.Count > 1) { set.RemoveWhere(x => x != digit); changed = true; }
                }
            }
        }
        // boxes
        for (int br = 0; br < 3; br++)
            for (int bc = 0; bc < 3; bc++)
            {
                for (int digit = 1; digit <= 9; digit++)
                {
                    var positions = new List<int>();
                    for (int r = br * 3; r < br * 3 + 3; r++)
                        for (int c = bc * 3; c < bc + 3; c++)
                            if (cands[r * 9 + c].Contains(digit)) positions.Add(r * 9 + c);
                    if (positions.Count == 1)
                    {
                        var set = cands[positions[0]];
                        if (set.Count > 1) { set.RemoveWhere(x => x != digit); changed = true; }
                    }
                }
            }

        return changed;
    }
    private static bool ApplyNakedPairs(HashSet<int>[] cands)
    {
        bool changed = false;

        foreach (var unit in Units())
        {
            var pairs = unit
                .Where(i => cands[i].Count == 2)
                .GroupBy(i => string.Join(",", cands[i].OrderBy(x => x)))
                .Where(g => g.Count() == 2);

            foreach (var g in pairs)
            {
                var nums = cands[g.First()];
                foreach (int i in unit.Except(g))
                {
                    foreach (int n in nums)
                        if (cands[i].Remove(n))
                            changed = true;
                }
            }
        }

        return changed;
    }
    private static IEnumerable<int> Peers(int r, int c)
    {
        var set = new HashSet<int>();

        for (int i = 0; i < 9; i++)
        {
            set.Add(r * 9 + i);
            set.Add(i * 9 + c);
        }

        int br = (r / 3) * 3;
        int bc = (c / 3) * 3;
        for (int rr = br; rr < br + 3; rr++)
            for (int cc = bc; cc < bc + 3; cc++)
                set.Add(rr * 9 + cc);

        // exclude the cell itself
        set.Remove(r * 9 + c);

        return set;
    }
    private static IEnumerable<IEnumerable<int>> Units()
    {
        for (int i = 0; i < 9; i++)
        {
            yield return Enumerable.Range(0, 9).Select(c => i * 9 + c); // row
            yield return Enumerable.Range(0, 9).Select(r => r * 9 + i); // col
        }

        for (int br = 0; br < 3; br++)
            for (int bc = 0; bc < 3; bc++)
            {
                yield return Enumerable.Range(0, 9)
                    .Select(i => (br * 3 + i / 3) * 9 + (bc * 3 + i % 3));
            }
    }
    private static bool IsSafe(int[,] grid, int r, int c, int n)
    {
        for (int i = 0; i < 9; i++)
            if (grid[r, i] == n || grid[i, c] == n)
                return false;

        int br = (r / 3) * 3;
        int bc = (c / 3) * 3;
        for (int rr = br; rr < br + 3; rr++)
            for (int cc = bc; cc < bc + 3; cc++)
                if (grid[rr, cc] == n)
                    return false;

        return true;
    }
}
