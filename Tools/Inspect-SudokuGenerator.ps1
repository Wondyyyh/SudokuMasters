param([ValidateRange(1, 10000)][int]$SamplesPerLevel = 30)
$ErrorActionPreference = 'Stop'
$generatorPath = Join-Path $PSScriptRoot '../Assets/Games/Universal/scripts/SudokuGenerator.cs'
$source = Get-Content -LiteralPath $generatorPath -Raw
# Run the actual generator without starting Unity. Only its logging and coordinate type are stubbed.
$shim = @'
namespace UnityEngine {
    public struct Vector2Int {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }
    public static class Debug { public static void Log(object value) {} }
}
'@
$checks = @'
public static class GeneratorInspection {
    public sealed class Result {
        public string level;
        public int samples, unique, singlesOnly, removalMismatches, orientationRatingDifferences;
        public int minClues = 81, maxClues;
        public double minRating = double.MaxValue, maxRating, elapsedMilliseconds;
    }
    static int Mask(int[,] board, int row, int col) {
        int mask = 0x3fe;
        for (int i = 0; i < 9; i++) {
            mask &= ~(1 << board[row,i]); mask &= ~(1 << board[i,col]);
            mask &= ~(1 << board[row/3*3+i/3,col/3*3+i%3]);
        }
        return mask;
    }
    static int Bits(int mask) { int count = 0; while (mask != 0) { count++; mask &= mask-1; } return count; }
    static int Digit(int mask) { for (int n=1;n<=9;n++) if ((mask & (1<<n)) != 0) return n; return 0; }
    // Independent MRV backtracking, stopping at two solutions.
    static int Count(int[,] board, int limit = 2) {
        int bestRow=-1,bestCol=-1,bestMask=0,bestCount=10;
        for (int r=0;r<9;r++) for(int c=0;c<9;c++) if(board[r,c]==0) {
            int mask=Mask(board,r,c), count=Bits(mask);
            if(count==0) return 0;
            if(count<bestCount) { bestRow=r; bestCol=c; bestMask=mask; bestCount=count; }
        }
        if(bestRow<0) return 1;
        int total=0;
        for(int n=1;n<=9 && total<limit;n++) if((bestMask & (1<<n))!=0) {
            board[bestRow,bestCol]=n; total+=Count(board,limit-total);
        }
        board[bestRow,bestCol]=0;
        return total;
    }
    // All 27 houses, one placement at a time; no guessing or candidate elimination.
    static bool Singles(int[,] original) {
        int[,] board=(int[,])original.Clone();
        for(int step=0;step<81;step++) {
            bool placed=false; int empty=0;
            for(int r=0;r<9 && !placed;r++) for(int c=0;c<9 && !placed;c++) if(board[r,c]==0) {
                empty++; int mask=Mask(board,r,c);
                if(Bits(mask)==1) { board[r,c]=Digit(mask); placed=true; }
            }
            if(empty==0) return true;
            if(placed) continue;
            for(int house=0;house<27 && !placed;house++) for(int n=1;n<=9 && !placed;n++) {
                int count=0,lastRow=0,lastCol=0;
                for(int i=0;i<9;i++) {
                    int r=house<9?house:house<18?i:(house-18)/3*3+i/3;
                    int c=house<9?i:house<18?house-9:(house-18)%3*3+i%3;
                    if(board[r,c]==0 && (Mask(board,r,c)&(1<<n))!=0) { count++; lastRow=r; lastCol=c; }
                }
                if(count==1) { board[lastRow,lastCol]=n; placed=true; }
            }
            if(!placed) return false;
        }
        foreach(int value in board) if(value==0) return false;
        return true;
    }
    public static Result[] Run(int samples) {
        var results=new System.Collections.Generic.List<Result>();
        foreach(Difficulty difficulty in System.Enum.GetValues(typeof(Difficulty))) {
            var result=new Result {level=difficulty.ToString(),samples=samples};
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int sample=0;sample<samples;sample++) {
                var generator=new SudokuGenerator();
                typeof(SudokuGenerator).GetField("rng",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                    .SetValue(generator,new System.Random(20261111+(int)difficulty*10000+sample));
                generator.Generate(difficulty);
                var board=SudokuGenerator.puzzle;
                int empty=0; foreach(int value in board) if(value==0) empty++;
                result.minClues=System.Math.Min(result.minClues,81-empty);
                result.maxClues=System.Math.Max(result.maxClues,81-empty);
                if(empty!=SudokuGenerator.removals) result.removalMismatches++;
                if(Count((int[,])board.Clone())==1) result.unique++;
                if(Singles(board)) result.singlesOnly++;
                var report=generator.AnalyzeDifficulty(board);
                result.minRating=System.Math.Min(result.minRating,report.rating);
                result.maxRating=System.Math.Max(result.maxRating,report.rating);
                var transposed=new int[9,9];
                for(int r=0;r<9;r++) for(int c=0;c<9;c++) transposed[c,r]=board[r,c];
                if(generator.AnalyzeDifficulty(transposed).rating!=report.rating) result.orientationRatingDifferences++;
            }
            result.elapsedMilliseconds=watch.Elapsed.TotalMilliseconds;
            results.Add(result);
        }
        return results.ToArray();
    }
}
'@
Add-Type -TypeDefinition ($source + $shim + $checks)
[GeneratorInspection]::Run($SamplesPerLevel) | ConvertTo-Json
