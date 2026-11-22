using PokeMatch.WPF.Models;
using Xunit;

namespace PokeMatch.Tests
{
    public class BoardModelTests
    {
        private static void AssertMatrixEqual(int[,] expected, int[,] actual)
        {
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));
            Assert.Equal(expected.Cast<int>().ToArray(), actual.Cast<int>().ToArray());
        }

        // ---------- Khởi tạo ----------

        [Fact]
        public void Constructor_CreatesEmptyBoardWithCorrectSize()
        {
            var board = new BoardModel(4, 6);

            Assert.Equal(4, board.Rows);
            Assert.Equal(6, board.Columns);
            Assert.Equal(0, board.RemainingCount);
            Assert.True(board.IsCleared);
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(5, 0)]
        [InlineData(-1, 3)]
        public void Constructor_InvalidSize_Throws(int rows, int cols)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardModel(rows, cols));
        }

        [Fact]
        public void Tiles_HaveCoordinatesMatchingTheirPosition_XIsColumn_YIsRow()
        {
            var board = new BoardModel(3, 5);

            var tile = board[row: 2, col: 4];

            Assert.Equal(4, tile.X); // X = cột
            Assert.Equal(2, tile.Y); // Y = hàng
        }

        // ---------- Truy cập ----------

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)]
        [InlineData(0, 5)]
        public void Indexer_OutsideBoard_Throws(int row, int col)
        {
            var board = new BoardModel(3, 5);

            Assert.Throws<ArgumentOutOfRangeException>(() => board[row, col]);
        }

        [Fact]
        public void TryGetTile_ReturnsFalseOutsideBoard()
        {
            var board = new BoardModel(2, 2);

            Assert.False(board.TryGetTile(-1, 0, out var outside));
            Assert.Null(outside);
            Assert.True(board.TryGetTile(1, 1, out var inside));
            Assert.NotNull(inside);
        }

        [Fact]
        public void SetTile_NonZeroId_MakesTileNormal_ZeroMakesItEmpty()
        {
            var board = new BoardModel(2, 2);

            board.SetTile(0, 0, 5);
            Assert.Equal(5, board[0, 0].Id);
            Assert.Equal(TileState.Normal, board[0, 0].State);

            board.SetTile(0, 0, 0);
            Assert.True(board[0, 0].IsDeleted);
        }

        // ---------- FromLayout / FromMatrix / ToMatrix ----------

        [Fact]
        public void FromLayout_ParsesIdsAndEmptyCells()
        {
            var board = BoardModel.FromLayout(
                "1.A",
                "0Z9");

            Assert.Equal(2, board.Rows);
            Assert.Equal(3, board.Columns);
            Assert.Equal(1, board[0, 0].Id);
            Assert.True(board[0, 1].IsDeleted);
            Assert.Equal(10, board[0, 2].Id);   // 'A'
            Assert.True(board[1, 0].IsDeleted); // '0'
            Assert.Equal(35, board[1, 1].Id);   // 'Z'
            Assert.Equal(9, board[1, 2].Id);
        }

        [Fact]
        public void FromLayout_RaggedRows_Throws()
        {
            Assert.Throws<ArgumentException>(() => BoardModel.FromLayout("111", "11"));
        }

        [Fact]
        public void FromLayout_InvalidCharacter_Throws()
        {
            Assert.Throws<ArgumentException>(() => BoardModel.FromLayout("1#1"));
        }

        [Fact]
        public void FromLayout_NoRows_Throws()
        {
            Assert.Throws<ArgumentException>(() => BoardModel.FromLayout());
        }

        [Fact]
        public void ToMatrix_RoundTripsWithFromMatrix()
        {
            var source = new[,]
            {
                { 1, 0, 2 },
                { 3, 3, 0 }
            };

            var result = BoardModel.FromMatrix(source).ToMatrix();

            AssertMatrixEqual(source, result);
        }

        [Fact]
        public void ToMatrix_DeletedTilesBecomeZero()
        {
            var board = BoardModel.FromLayout("11");
            board.RemovePair(board[0, 0], board[0, 1]);

            AssertMatrixEqual(new[,] { { 0, 0 } }, board.ToMatrix());
        }

        // ---------- IsFree (dành cho PathFinder) ----------

        [Fact]
        public void IsFree_FalseOnTile_TrueOnEmptyCell()
        {
            var board = BoardModel.FromLayout("1.");

            Assert.False(board.IsFree(0, 0));
            Assert.True(board.IsFree(0, 1));
        }

        [Fact]
        public void IsFree_TrueOnVirtualBorder_FalseBeyondIt()
        {
            var board = BoardModel.FromLayout("11", "11"); // bàn cờ kín

            // Viền ảo rộng 1 ô: đi qua được
            Assert.True(board.IsFree(-1, 0));
            Assert.True(board.IsFree(2, 1));
            Assert.True(board.IsFree(0, -1));
            Assert.True(board.IsFree(1, 2));
            Assert.True(board.IsFree(-1, -1)); // góc viền

            // Xa hơn viền: không hợp lệ
            Assert.False(board.IsFree(-2, 0));
            Assert.False(board.IsFree(0, 3));
        }

        [Fact]
        public void IsFree_BecomesTrueAfterTileIsRemoved()
        {
            var board = BoardModel.FromLayout("11");
            Assert.False(board.IsFree(0, 0));

            board.RemovePair(board[0, 0], board[0, 1]);

            Assert.True(board.IsFree(0, 0));
            Assert.True(board.IsFree(0, 1));
        }

        [Fact]
        public void IsInside_And_IsInsideExtended_Boundaries()
        {
            var board = new BoardModel(2, 3);

            Assert.True(board.IsInside(0, 0));
            Assert.True(board.IsInside(1, 2));
            Assert.False(board.IsInside(2, 0));
            Assert.False(board.IsInside(0, -1));

            Assert.True(board.IsInsideExtended(-1, -1));
            Assert.True(board.IsInsideExtended(2, 3));
            Assert.False(board.IsInsideExtended(3, 0));
            Assert.False(board.IsInsideExtended(0, -2));
        }

        // ---------- Ghép cặp ----------

        [Fact]
        public void RemovePair_SameId_DeletesBothTiles()
        {
            var board = BoardModel.FromLayout("1.1", "222");

            bool ok = board.RemovePair(board[0, 0], board[0, 2]);

            Assert.True(ok);
            Assert.True(board[0, 0].IsDeleted);
            Assert.True(board[0, 2].IsDeleted);
            Assert.Equal(3, board.RemainingCount);
        }

        [Fact]
        public void RemovePair_DifferentId_ReturnsFalseAndChangesNothing()
        {
            var board = BoardModel.FromLayout("12");

            bool ok = board.RemovePair(board[0, 0], board[0, 1]);

            Assert.False(ok);
            Assert.Equal(2, board.RemainingCount);
        }

        [Fact]
        public void RemovePair_SameTileTwice_ReturnsFalse()
        {
            var board = BoardModel.FromLayout("11");

            Assert.False(board.RemovePair(board[0, 0], board[0, 0]));
            Assert.Equal(2, board.RemainingCount);
        }

        [Fact]
        public void RemovePair_AlreadyDeletedTile_ReturnsFalse()
        {
            var board = BoardModel.FromLayout("111");
            board.RemovePair(board[0, 0], board[0, 1]);

            Assert.False(board.RemovePair(board[0, 0], board[0, 2]));
            Assert.Equal(1, board.RemainingCount);
        }

        [Fact]
        public void AreSameType_TwoEmptyCells_IsFalse()
        {
            var board = BoardModel.FromLayout("..");

            Assert.False(BoardModel.AreSameType(board[0, 0], board[0, 1]));
        }

        [Fact]
        public void RemovePair_SelectedTiles_AreDeleted()
        {
            var board = BoardModel.FromLayout("11");
            board[0, 0].Select();
            board[0, 1].Select();

            board.RemovePair(board[0, 0], board[0, 1]);

            Assert.True(board[0, 0].IsDeleted);
            Assert.False(board[0, 0].IsSelected);
        }

        // ---------- Thống kê ----------

        [Fact]
        public void RemainingCount_And_IsCleared_TrackProgress()
        {
            var board = BoardModel.FromLayout("11", "22");
            Assert.Equal(4, board.RemainingCount);
            Assert.False(board.IsCleared);

            board.RemovePair(board[0, 0], board[0, 1]);
            Assert.Equal(2, board.RemainingCount);

            board.RemovePair(board[1, 0], board[1, 1]);
            Assert.True(board.IsCleared);
        }

        [Fact]
        public void GetRemainingTiles_ExcludesEmptyAndDeleted()
        {
            var board = BoardModel.FromLayout("1.1");

            var remaining = board.GetRemainingTiles().ToList();

            Assert.Equal(2, remaining.Count);
            Assert.All(remaining, t => Assert.Equal(1, t.Id));
        }

        [Fact]
        public void Clone_IsIndependentFromOriginal()
        {
            var original = BoardModel.FromLayout("11");
            var copy = original.Clone();

            copy.RemovePair(copy[0, 0], copy[0, 1]);

            Assert.Equal(2, original.RemainingCount);
            Assert.Equal(0, copy.RemainingCount);
        }

        // ---------- Ma trận mẫu cho PathFinder ----------

        [Fact]
        public void StraightLine_Layout_HasFreeCellsBetweenTheTwoTiles()
        {
            var board = BoardModel.FromLayout(TestBoards.StraightLine);

            Assert.True(BoardModel.AreSameType(board[1, 0], board[1, 3]));
            Assert.True(board.IsFree(1, 1));
            Assert.True(board.IsFree(1, 2));
        }

        [Fact]
        public void OneCorner_Layout_HasFreeCornerCells()
        {
            var board = BoardModel.FromLayout(TestBoards.OneCorner);

            Assert.True(BoardModel.AreSameType(board[0, 0], board[2, 2]));
            // Đường qua góc (0,2)
            Assert.True(board.IsFree(0, 1));
            Assert.True(board.IsFree(0, 2));
            Assert.True(board.IsFree(1, 2));
            // Đường qua góc (2,0)
            Assert.True(board.IsFree(1, 0));
            Assert.True(board.IsFree(2, 0));
            Assert.True(board.IsFree(2, 1));
        }

        [Fact]
        public void TwoCorners_Layout_StraightLineIsBlockedButDetourRowIsFree()
        {
            var board = BoardModel.FromLayout(TestBoards.TwoCorners);

            Assert.True(BoardModel.AreSameType(board[0, 0], board[0, 4]));
            Assert.False(board.IsFree(0, 2)); // bị ô '2' chắn
            for (int c = 0; c < board.Columns; c++)
                Assert.True(board.IsFree(1, c)); // hàng đi vòng
        }

        [Fact]
        public void ViaOutsideBorder_Layout_OnlyRouteIsTheVirtualBorder()
        {
            var board = BoardModel.FromLayout(TestBoards.ViaOutsideBorder);

            Assert.True(BoardModel.AreSameType(board[0, 0], board[0, 3]));
            Assert.False(board.IsFree(0, 1));
            Assert.False(board.IsFree(0, 2));
            Assert.False(board.IsFree(1, 0)); // hàng 1 kín
            for (int c = -1; c <= board.Columns; c++)
                Assert.True(board.IsFree(-1, c)); // viền phía trên
        }

        [Fact]
        public void Blocked_Layout_FirstTileIsFullyEnclosed()
        {
            var board = BoardModel.FromLayout(TestBoards.Blocked);

            Assert.True(BoardModel.AreSameType(board[1, 1], board[1, 4]));
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                    if (dr != 0 || dc != 0)
                        Assert.False(board.IsFree(1 + dr, 1 + dc));
        }
    }
}
