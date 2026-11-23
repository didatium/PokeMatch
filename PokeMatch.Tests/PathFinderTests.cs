using PokeMatch.WPF.Core;
using PokeMatch.WPF.Models;
using Xunit;

namespace PokeMatch.Tests
{
    public class PathFinderTests
    {
        // Kiểm tra độc lập: đường phải bắt đầu/kết thúc đúng ô, ≤ 2 góc,
        // các đoạn thẳng hàng/cột, và mọi ô đi qua đều trống.
        private static void AssertValidPath(BoardModel board, TileModel a, TileModel b, PathResult path)
        {
            var pts = path.Points;
            Assert.Equal(new GridPoint(a.Y, a.X), pts[0]);
            Assert.Equal(new GridPoint(b.Y, b.X), pts[^1]);
            Assert.InRange(path.Turns, 0, PathFinder.MaxTurns);

            for (int i = 1; i < pts.Count - 1; i++)
                Assert.True(board.IsFree(pts[i].Row, pts[i].Col), $"Góc {pts[i]} không trống");

            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p = pts[i];
                var q = pts[i + 1];
                Assert.True(p.Row == q.Row || p.Col == q.Col, $"Đoạn {p}->{q} không thẳng");

                int dr = Math.Sign(q.Row - p.Row), dc = Math.Sign(q.Col - p.Col);
                for (int r = p.Row + dr, c = p.Col + dc; r != q.Row || c != q.Col; r += dr, c += dc)
                    Assert.True(board.IsFree(r, c), $"Ô ({r},{c}) trên đường bị chắn");
            }
        }

        // ---------- I-path ----------

        [Fact]
        public void StraightLine_ReturnsZeroTurnPath()
        {
            var board = BoardModel.FromLayout(TestBoards.StraightLine);
            var a = board[1, 0];
            var b = board[1, 3];

            var path = PathFinder.FindPath(board, a, b);

            Assert.NotNull(path);
            Assert.Equal(0, path!.Turns);
            Assert.Equal(new[] { new GridPoint(1, 0), new GridPoint(1, 3) }, path.Points.ToArray());
            AssertValidPath(board, a, b, path);
        }

        [Fact]
        public void AdjacentTiles_AreConnected()
        {
            var board = BoardModel.FromLayout("11");

            var path = PathFinder.FindPath(board, board[0, 0], board[0, 1]);

            Assert.NotNull(path);
            Assert.Equal(0, path!.Turns);
        }

        [Fact]
        public void VerticalStraightLine_IsConnected()
        {
            var board = BoardModel.FromLayout("1", ".", ".", "1");

            var path = PathFinder.FindPath(board, board[0, 0], board[3, 0]);

            Assert.NotNull(path);
            Assert.Equal(0, path!.Turns);
        }

        [Fact]
        public void StraightLine_BlockedByTile_IsNotZeroTurns()
        {
            var board = BoardModel.FromLayout("1.2.1");

            var path = PathFinder.FindPath(board, board[0, 0], board[0, 4]);

            // Vẫn nối được qua viền ảo, nhưng không phải đường thẳng.
            Assert.NotNull(path);
            Assert.Equal(2, path!.Turns);
        }

        // ---------- L-path ----------

        [Fact]
        public void OneCorner_ReturnsOneTurnPath()
        {
            var board = BoardModel.FromLayout(TestBoards.OneCorner);
            var a = board[0, 0];
            var b = board[2, 2];

            var path = PathFinder.FindPath(board, a, b);

            Assert.NotNull(path);
            Assert.Equal(1, path!.Turns);
            Assert.Contains(path.Points[1], new[] { new GridPoint(0, 2), new GridPoint(2, 0) });
            AssertValidPath(board, a, b, path);
        }

        [Fact]
        public void OneCorner_OneCornerBlocked_UsesTheOtherCorner()
        {
            var board = BoardModel.FromLayout("1.2", "...", "..1"); // chặn góc (0,2) bằng ô '2'
            var a = board[0, 0];
            var b = board[2, 2];

            var path = PathFinder.FindPath(board, a, b);

            Assert.NotNull(path);
            Assert.Equal(1, path!.Turns);
            Assert.Equal(new GridPoint(2, 0), path.Points[1]);
        }

        // ---------- Z/U-path ----------

        [Fact]
        public void ZPath_ReturnsTwoTurnPath()
        {
            var board = BoardModel.FromLayout(TestBoards.ZPath);
            var a = board[0, 0];
            var b = board[2, 3];

            var path = PathFinder.FindPath(board, a, b);

            Assert.NotNull(path);
            Assert.Equal(2, path!.Turns);
            AssertValidPath(board, a, b, path);
        }

        [Fact]
        public void TwoCorners_UPathAroundObstacle()
        {
            var board = BoardModel.FromLayout(TestBoards.TwoCorners);
            var a = board[0, 0];
            var b = board[0, 4];

            var path = PathFinder.FindPath(board, a, b);

            Assert.NotNull(path);
            Assert.Equal(2, path!.Turns);
            AssertValidPath(board, a, b, path);
        }

        [Fact]
        public void ViaOutsideBorder_PathGoesThroughVirtualBorder()
        {
            var board = BoardModel.FromLayout(TestBoards.ViaOutsideBorder);
            var a = board[0, 0];
            var b = board[0, 3];

            var path = PathFinder.FindPath(board, a, b);

            Assert.NotNull(path);
            Assert.Equal(2, path!.Turns);
            Assert.Equal(-1, path.Points[1].Row); // cả hai góc nằm ở hàng viền phía trên
            Assert.Equal(-1, path.Points[2].Row);
            AssertValidPath(board, a, b, path);
        }

        [Fact]
        public void ViaOutsideBorder_SideBorder_ForTilesOnLeftAndRightEdges()
        {
            var board = BoardModel.FromLayout("1", "2", "1");

            var path = PathFinder.FindPath(board, board[0, 0], board[2, 0]);

            Assert.NotNull(path);
            Assert.Equal(2, path!.Turns);
            AssertValidPath(board, board[0, 0], board[2, 0], path);
        }

        [Fact]
        public void FindPath_PrefersShortestTwoCornerPath()
        {
            // Có thể đi qua hàng 1 (ngắn) hoặc qua viền ảo (dài hơn).
            var board = BoardModel.FromLayout("1.2.1", ".....");

            var path = PathFinder.FindPath(board, board[0, 0], board[0, 4]);

            Assert.NotNull(path);
            Assert.Equal(6, path!.Length); // xuống 1, sang 4, lên 1
        }

        // ---------- Không nối được ----------

        [Fact]
        public void EnclosedTile_HasNoPath()
        {
            var board = BoardModel.FromLayout(TestBoards.Blocked);

            Assert.Null(PathFinder.FindPath(board, board[1, 1], board[1, 4]));
        }

        [Fact]
        public void PathNeedingThreeTurns_IsRejected()
        {
            var board = BoardModel.FromLayout(TestBoards.RequiresThreeTurns);

            Assert.Null(PathFinder.FindPath(board, board[1, 1], board[5, 5]));
        }

        [Fact]
        public void DifferentIds_AreNeverConnected()
        {
            var board = BoardModel.FromLayout("12");

            Assert.Null(PathFinder.FindPath(board, board[0, 0], board[0, 1]));
        }

        [Fact]
        public void SameTile_IsNeverConnectedToItself()
        {
            var board = BoardModel.FromLayout("11");

            Assert.Null(PathFinder.FindPath(board, board[0, 0], board[0, 0]));
        }

        [Fact]
        public void DeletedTile_IsNeverConnected()
        {
            var board = BoardModel.FromLayout("111");
            board.RemovePair(board[0, 0], board[0, 1]);

            Assert.Null(PathFinder.FindPath(board, board[0, 0], board[0, 2]));
        }

        [Fact]
        public void FindPath_IsSymmetric()
        {
            var board = BoardModel.FromLayout(TestBoards.ZPath);

            Assert.True(PathFinder.CanConnect(board, board[0, 0], board[2, 3]));
            Assert.True(PathFinder.CanConnect(board, board[2, 3], board[0, 0]));
        }

        [Fact]
        public void RemovingBlockingTiles_OpensANewPath()
        {
            var board = BoardModel.FromLayout("1", "2", "2", "1");
            // Cột 0 bị chặn nhưng vẫn nối được qua viền ảo (2 góc).
            var before = PathFinder.FindPath(board, board[0, 0], board[3, 0]);
            Assert.Equal(2, before!.Turns);

            board.RemovePair(board[1, 0], board[2, 0]);

            var after = PathFinder.FindPath(board, board[0, 0], board[3, 0]);
            Assert.Equal(0, after!.Turns);
        }

        [Fact]
        public void FindPath_NullArguments_Throw()
        {
            var board = BoardModel.FromLayout("11");

            Assert.Throws<ArgumentNullException>(() => PathFinder.FindPath(null!, board[0, 0], board[0, 1]));
            Assert.Throws<ArgumentNullException>(() => PathFinder.FindPath(board, null!, board[0, 1]));
            Assert.Throws<ArgumentNullException>(() => PathFinder.FindPath(board, board[0, 0], null!));
        }

        // ---------- CheckAvailableMoves ----------

        [Fact]
        public void CheckAvailableMoves_BoardWithConnectablePair_IsTrue()
        {
            var board = BoardModel.FromLayout(TestBoards.OneCorner);

            Assert.True(PathFinder.CheckAvailableMoves(board));
        }

        [Fact]
        public void CheckAvailableMoves_NoPairsAtAll_IsFalse()
        {
            var board = BoardModel.FromLayout(TestBoards.AllUnique);

            Assert.False(PathFinder.CheckAvailableMoves(board));
        }

        [Fact]
        public void CheckAvailableMoves_PairIsSealedIn_IsFalse_ThenTrueOnceOpened()
        {
            var board = BoardModel.FromLayout(TestBoards.SealedPair);
            Assert.False(PathFinder.CheckAvailableMoves(board));

            board.SetTile(0, 1, 0); // xoá ô 'A' để mở góc (0,1)

            Assert.True(PathFinder.CheckAvailableMoves(board));
        }

        [Fact]
        public void CheckAvailableMoves_EmptyBoard_IsFalse()
        {
            Assert.False(PathFinder.CheckAvailableMoves(new BoardModel(3, 3)));
        }

        [Fact]
        public void FindAvailableMove_ReturnsValidMove()
        {
            var board = BoardModel.FromLayout(TestBoards.ZPath);

            var move = PathFinder.FindAvailableMove(board);

            Assert.NotNull(move);
            Assert.True(BoardModel.AreSameType(move!.First, move.Second));
            AssertValidPath(board, move.First, move.Second, move.Path);
        }

        [Fact]
        public void FindAvailableMove_NoMoves_ReturnsNull()
        {
            var board = BoardModel.FromLayout(TestBoards.AllUnique);

            Assert.Null(PathFinder.FindAvailableMove(board));
        }
    }
}
