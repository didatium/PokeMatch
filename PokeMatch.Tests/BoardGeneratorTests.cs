using PokeMatch.WPF.Core;
using PokeMatch.WPF.Models;
using Xunit;

namespace PokeMatch.Tests
{
    public class BoardGeneratorTests
    {
        // Chạy lại lời giải bằng PathFinder + RemovePair (độc lập với BoardGenerator.IsValidSolution).
        private static void AssertSolutionClearsBoard(BoardModel board, IReadOnlyList<SolutionStep> solution)
        {
            var copy = board.Clone();
            Assert.Equal(copy.RemainingCount / 2, solution.Count);

            for (int i = 0; i < solution.Count; i++)
            {
                var a = copy[solution[i].First.Row, solution[i].First.Col];
                var b = copy[solution[i].Second.Row, solution[i].Second.Col];

                Assert.True(BoardModel.AreSameType(a, b), $"Bước {i}: hai ô khác loại hoặc đã bị ăn");
                Assert.NotNull(PathFinder.FindPath(copy, a, b));
                Assert.True(copy.RemovePair(a, b));
            }
            Assert.True(copy.IsCleared);
        }

        // ---------- Cấu trúc bàn cờ ----------

        [Theory]
        [InlineData(4, 4, 8)]
        [InlineData(6, 8, 12)]
        [InlineData(8, 16, 36)]
        public void GenerateBoard_FillsEveryCell_AndEachIdAppearsAnEvenNumberOfTimes(int rows, int cols, int types)
        {
            var board = BoardGenerator.GenerateBoard(rows, cols, types, new Random(42));

            Assert.Equal(rows, board.Rows);
            Assert.Equal(cols, board.Columns);
            Assert.Equal(rows * cols, board.RemainingCount);
            Assert.All(board.GetAllTiles(), t => Assert.InRange(t.Id, 1, types));

            foreach (var group in board.GetAllTiles().GroupBy(t => t.Id))
                Assert.True(group.Count() % 2 == 0, $"Id {group.Key} xuất hiện {group.Count()} lần (lẻ)");
        }

        [Fact]
        public void GenerateBoard_TilesAreNormalAndCoordinatesMatch()
        {
            var board = BoardGenerator.GenerateBoard(4, 4, 4, new Random(1));

            for (int r = 0; r < board.Rows; r++)
                for (int c = 0; c < board.Columns; c++)
                {
                    Assert.Equal(TileState.Normal, board[r, c].State);
                    Assert.Equal(r, board[r, c].Y);
                    Assert.Equal(c, board[r, c].X);
                }
        }

        [Fact]
        public void GenerateBoard_SameSeed_GivesSameBoard()
        {
            var a = BoardGenerator.GenerateBoard(6, 6, 9, new Random(123));
            var b = BoardGenerator.GenerateBoard(6, 6, 9, new Random(123));

            Assert.Equal(a.ToMatrix().Cast<int>(), b.ToMatrix().Cast<int>());
        }

        [Fact]
        public void GenerateBoard_DifferentSeeds_UsuallyGiveDifferentBoards()
        {
            var a = BoardGenerator.GenerateBoard(6, 6, 9, new Random(1));
            var b = BoardGenerator.GenerateBoard(6, 6, 9, new Random(2));

            Assert.NotEqual(a.ToMatrix().Cast<int>(), b.ToMatrix().Cast<int>());
        }

        [Fact]
        public void GenerateBoard_UsesOnlyGivenTypeIds()
        {
            var ids = new[] { 11, 42, 99, 127 };

            var board = BoardGenerator.GenerateBoard(4, 4, ids, new Random(5));

            Assert.All(board.GetAllTiles(), t => Assert.Contains(t.Id, ids));
            Assert.Equal(4, board.GetAllTiles().Select(t => t.Id).Distinct().Count());
        }

        [Fact]
        public void GenerateBoard_MoreTypesThanPairs_UsesOnlyAsManyTypesAsPairs()
        {
            // 2x2 = 2 cặp, nhưng cho 10 loại.
            var board = BoardGenerator.GenerateBoard(2, 2, 10, new Random(3));

            Assert.Equal(2, board.GetAllTiles().Select(t => t.Id).Distinct().Count());
        }

        // ---------- Đảm bảo giải được ----------

        [Theory]
        [InlineData(2, 2, 2)]
        [InlineData(2, 3, 3)]
        [InlineData(3, 4, 6)]
        [InlineData(4, 4, 8)]
        [InlineData(5, 6, 15)]
        [InlineData(6, 8, 24)]
        [InlineData(8, 8, 32)]
        [InlineData(8, 16, 36)]
        [InlineData(8, 16, 4)]   // rất ít loại: nhiều ô trùng Id
        [InlineData(8, 16, 64)]  // mỗi loại đúng 1 cặp
        public void GenerateBoardWithSolution_SolutionClearsTheWholeBoard(int rows, int cols, int types)
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var generated = BoardGenerator.GenerateBoardWithSolution(rows, cols, types, new Random(seed));

                AssertSolutionClearsBoard(generated.Board, generated.Solution);
                Assert.True(BoardGenerator.IsValidSolution(generated.Board, generated.Solution));
            }
        }

        [Fact]
        public void GenerateBoardWithSolution_SolutionCoversEveryCellExactlyOnce()
        {
            var generated = BoardGenerator.GenerateBoardWithSolution(6, 8, 12, new Random(11));

            var cells = generated.Solution
                .SelectMany(s => new[] { s.First, s.Second })
                .ToList();

            Assert.Equal(48, cells.Count);
            Assert.Equal(48, cells.Distinct().Count());
            Assert.All(cells, p => Assert.True(generated.Board.IsInside(p.Row, p.Col)));
        }

        [Fact]
        public void GenerateBoard_AlwaysHasAvailableMoves()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var board = BoardGenerator.GenerateBoard(6, 8, 24, new Random(seed));
                Assert.True(PathFinder.CheckAvailableMoves(board), $"seed {seed} bị bí ngay từ đầu");
            }
        }

        [Fact]
        public void GenerateBoard_DoesNotNeedRandomSeed()
        {
            var board = BoardGenerator.GenerateBoard(8, 16, 36); // Random.Shared

            Assert.Equal(128, board.RemainingCount);
            Assert.True(PathFinder.CheckAvailableMoves(board));
        }

        [Fact]
        public void IsValidSolution_RejectsTamperedOrIncompleteSolutions()
        {
            var generated = BoardGenerator.GenerateBoardWithSolution(4, 4, 8, new Random(3));
            var board = generated.Board;

            // Thiếu bước => bàn chưa sạch.
            Assert.False(BoardGenerator.IsValidSolution(board, generated.Solution.Skip(1).ToList()));

            // Rỗng trên bàn có ô.
            Assert.False(BoardGenerator.IsValidSolution(board, Array.Empty<SolutionStep>()));

            // Ghép hai ô khác loại.
            var first = generated.Solution[0];
            var other = generated.Solution.First(s => board[s.First.Row, s.First.Col].Id != board[first.First.Row, first.First.Col].Id);
            var wrong = new List<SolutionStep> { new SolutionStep(first.First, other.First) };
            Assert.False(BoardGenerator.IsValidSolution(board, wrong));

            // Toạ độ ngoài bàn.
            var outside = new List<SolutionStep> { new SolutionStep(new GridPoint(-1, 0), new GridPoint(0, 0)) };
            Assert.False(BoardGenerator.IsValidSolution(board, outside));
        }

        [Fact]
        public void IsValidSolution_DoesNotModifyTheBoard()
        {
            var generated = BoardGenerator.GenerateBoardWithSolution(4, 4, 8, new Random(3));
            var before = generated.Board.ToMatrix().Cast<int>().ToArray();

            BoardGenerator.IsValidSolution(generated.Board, generated.Solution);

            Assert.Equal(16, generated.Board.RemainingCount);
            Assert.Equal(before, generated.Board.ToMatrix().Cast<int>().ToArray());
        }

        // ---------- Tham số sai ----------

        [Fact]
        public void GenerateBoard_OddCellCount_Throws()
        {
            Assert.Throws<ArgumentException>(() => BoardGenerator.GenerateBoard(3, 3, 4));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void GenerateBoard_InvalidTypeCount_Throws(int typeCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardGenerator.GenerateBoard(4, 4, typeCount));
        }

        [Fact]
        public void GenerateBoard_InvalidSize_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardGenerator.GenerateBoard(0, 4, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardGenerator.GenerateBoard(4, -2, 4));
        }

        [Fact]
        public void GenerateBoard_InvalidTypeIds_Throw()
        {
            Assert.Throws<ArgumentException>(() => BoardGenerator.GenerateBoard(4, 4, new int[0]));
            Assert.Throws<ArgumentException>(() => BoardGenerator.GenerateBoard(4, 4, new[] { 1, 1, 2 }));
            Assert.Throws<ArgumentException>(() => BoardGenerator.GenerateBoard(4, 4, new[] { 0, 1 }));
        }

        // ---------- ShuffleRemaining ----------

        [Fact]
        public void ShuffleRemaining_KeepsSameMultisetOfIds_AndKeepsDeletedCellsDeleted()
        {
            var board = BoardGenerator.GenerateBoard(6, 6, 9, new Random(7));
            var move = PathFinder.FindAvailableMove(board)!;
            board.RemovePair(move.First, move.Second);

            var deletedBefore = board.GetAllTiles().Where(t => t.IsDeleted).Select(t => (t.Y, t.X)).ToHashSet();
            var idsBefore = board.GetRemainingTiles().Select(t => t.Id).OrderBy(i => i).ToList();

            BoardGenerator.ShuffleRemaining(board, new Random(9));

            var deletedAfter = board.GetAllTiles().Where(t => t.IsDeleted).Select(t => (t.Y, t.X)).ToHashSet();
            var idsAfter = board.GetRemainingTiles().Select(t => t.Id).OrderBy(i => i).ToList();
            Assert.Equal(deletedBefore, deletedAfter);
            Assert.Equal(idsBefore, idsAfter);
        }

        [Fact]
        public void ShuffleRemaining_ReturnsSolutionThatClearsTheShuffledBoard()
        {
            var board = BoardGenerator.GenerateBoard(6, 8, 12, new Random(4));
            var move = PathFinder.FindAvailableMove(board)!;
            board.RemovePair(move.First, move.Second);

            var solution = BoardGenerator.ShuffleRemaining(board, new Random(4));

            AssertSolutionClearsBoard(board, solution);
        }

        [Fact]
        public void ShuffleRemaining_FixesAStuckBoard()
        {
            // Bàn 2x2 bí: hai cặp nằm chéo nhau, mỗi cặp bị ô còn lại chắn.
            var board = BoardModel.FromLayout("12", "21");
            Assert.False(PathFinder.CheckAvailableMoves(board));

            var solution = BoardGenerator.ShuffleRemaining(board, new Random(0));

            Assert.True(PathFinder.CheckAvailableMoves(board));
            AssertSolutionClearsBoard(board, solution);
        }

        [Fact]
        public void ShuffleRemaining_AfterPlayerMakesBadMoves_StillGivesSolvableBoard()
        {
            // Người chơi ăn bừa theo nước đi hợp lệ đầu tiên, có thể làm bàn không còn giải được.
            // Dù vậy mỗi lần xáo đều phải cho ra bàn giải được hết.
            for (int seed = 0; seed < 20; seed++)
            {
                var rng = new Random(seed);
                var board = BoardGenerator.GenerateBoard(8, 16, 36, rng);

                // Ăn 20 cặp ngẫu nhiên.
                for (int k = 0; k < 20; k++)
                {
                    var moves = PathFinder.FindAvailableMove(board);
                    if (moves == null) break;
                    board.RemovePair(moves.First, moves.Second);
                }

                var solution = BoardGenerator.ShuffleRemaining(board, rng);

                AssertSolutionClearsBoard(board, solution);
            }
        }

        [Fact]
        public void ShuffleRemaining_ClearsSelection()
        {
            var board = BoardGenerator.GenerateBoard(4, 4, 4, new Random(2));
            board[0, 0].Select();

            BoardGenerator.ShuffleRemaining(board, new Random(2));

            Assert.All(board.GetAllTiles(), t => Assert.False(t.IsSelected));
        }

        [Fact]
        public void ShuffleRemaining_EmptyBoard_ReturnsEmptySolution()
        {
            var solution = BoardGenerator.ShuffleRemaining(new BoardModel(2, 2));

            Assert.Empty(solution);
        }

        [Fact]
        public void ShuffleRemaining_OddIdCount_Throws()
        {
            var board = BoardModel.FromLayout("12"); // mỗi Id chỉ có 1 ô

            Assert.Throws<InvalidOperationException>(() => BoardGenerator.ShuffleRemaining(board));
        }

        [Fact]
        public void ShuffleRemaining_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BoardGenerator.ShuffleRemaining(null!));
        }
    }
}
