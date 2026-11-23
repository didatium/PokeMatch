using PokeMatch.WPF.Models;

namespace PokeMatch.WPF.Core
{
    /// <summary>Một điểm trên lưới. Row/Col có thể = -1 hoặc Rows/Columns (viền ảo ngoài bàn cờ).</summary>
    public readonly record struct GridPoint(int Row, int Col);

    /// <summary>
    /// Đường nối giữa hai ô, gồm điểm đầu, các điểm gấp khúc và điểm cuối.
    /// Hai điểm liên tiếp luôn nằm cùng hàng hoặc cùng cột.
    /// Dùng <see cref="Points"/> để vẽ tia sét trong ConnectorCanvas.
    /// </summary>
    public sealed class PathResult
    {
        public IReadOnlyList<GridPoint> Points { get; }

        /// <summary>Số góc rẽ: 0 (I-path), 1 (L-path), 2 (Z/U-path).</summary>
        public int Turns => Points.Count - 2;

        /// <summary>Tổng số bước đi dọc đường nối (dùng để chọn đường ngắn nhất).</summary>
        public int Length
        {
            get
            {
                int total = 0;
                for (int i = 1; i < Points.Count; i++)
                    total += Math.Abs(Points[i].Row - Points[i - 1].Row)
                           + Math.Abs(Points[i].Col - Points[i - 1].Col);
                return total;
            }
        }

        public PathResult(IReadOnlyList<GridPoint> points) => Points = points;
    }

    /// <summary>Một nước đi hợp lệ: hai ô ăn được nhau cùng đường nối (dùng cho Hint).</summary>
    public sealed record Move(TileModel First, TileModel Second, PathResult Path);

    /// <summary>
    /// Tìm đường nối giữa hai ô cùng loại với tối đa <see cref="MaxTurns"/> góc rẽ.
    /// Thứ tự thử: I-path (0 góc) -> L-path (1 góc) -> Z/U-path (2 góc).
    /// Đường được đi qua ô trống/đã ăn và viền ảo quanh bàn cờ (xem <see cref="BoardModel.IsFree"/>).
    /// </summary>
    public static class PathFinder
    {
        public const int MaxTurns = 2;

        // ---------- API chính ----------

        /// <summary>
        /// Tìm đường nối <paramref name="a"/> và <paramref name="b"/>.
        /// Trả về null nếu hai ô không ăn được nhau (khác loại, đã bị ăn, cùng 1 ô)
        /// hoặc không có đường với ≤ 2 góc rẽ.
        /// </summary>
        public static PathResult? FindPath(BoardModel board, TileModel a, TileModel b)
        {
            ArgumentNullException.ThrowIfNull(board);
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);

            if (!BoardModel.AreSameType(a, b)) return null;

            var start = new GridPoint(a.Y, a.X);
            var end = new GridPoint(b.Y, b.X);

            return TryStraight(board, start, end)
                ?? TryOneCorner(board, start, end)
                ?? TryTwoCorners(board, start, end);
        }

        public static bool CanConnect(BoardModel board, TileModel a, TileModel b) =>
            FindPath(board, a, b) != null;

        /// <summary>Quét toàn bàn cờ, trả về nước đi hợp lệ đầu tiên tìm thấy (null nếu hết nước).</summary>
        public static Move? FindAvailableMove(BoardModel board)
        {
            ArgumentNullException.ThrowIfNull(board);

            // Chỉ so sánh các ô cùng Id với nhau để giảm số cặp phải thử.
            foreach (var group in board.GetRemainingTiles().GroupBy(t => t.Id))
            {
                var tiles = group.ToList();
                for (int i = 0; i < tiles.Count; i++)
                {
                    for (int j = i + 1; j < tiles.Count; j++)
                    {
                        var path = FindPath(board, tiles[i], tiles[j]);
                        if (path != null) return new Move(tiles[i], tiles[j], path);
                    }
                }
            }
            return null;
        }

        /// <summary>Còn ít nhất một cặp ăn được không? False nghĩa là bàn cờ bí, cần xáo trộn.</summary>
        public static bool CheckAvailableMoves(BoardModel board) => FindAvailableMove(board) != null;

        // ---------- I-path: 0 góc ----------

        private static PathResult? TryStraight(BoardModel board, GridPoint start, GridPoint end)
        {
            if (IsLineClear(board, start, end))
                return new PathResult(new[] { start, end });
            return null;
        }

        // ---------- L-path: 1 góc ----------

        private static PathResult? TryOneCorner(BoardModel board, GridPoint start, GridPoint end)
        {
            var corners = new[]
            {
                new GridPoint(start.Row, end.Col),
                new GridPoint(end.Row, start.Col)
            };

            foreach (var corner in corners)
            {
                // Nếu start/end thẳng hàng thì corner trùng một trong hai ô => IsFree = false, tự loại.
                if (board.IsFree(corner.Row, corner.Col)
                    && IsLineClear(board, start, corner)
                    && IsLineClear(board, corner, end))
                {
                    return new PathResult(new[] { start, corner, end });
                }
            }
            return null;
        }

        // ---------- Z-path / U-path: 2 góc ----------

        private static PathResult? TryTwoCorners(BoardModel board, GridPoint start, GridPoint end)
        {
            PathResult? best = null;

            // Đi ngang -> dọc -> ngang: quét mọi cột trung gian, kể cả 2 cột viền ảo.
            for (int col = -1; col <= board.Columns; col++)
            {
                best = Better(best, TryBridge(board, start, new GridPoint(start.Row, col),
                                                      new GridPoint(end.Row, col), end));
            }

            // Đi dọc -> ngang -> dọc: quét mọi hàng trung gian, kể cả 2 hàng viền ảo.
            for (int row = -1; row <= board.Rows; row++)
            {
                best = Better(best, TryBridge(board, start, new GridPoint(row, start.Col),
                                                      new GridPoint(row, end.Col), end));
            }

            return best;
        }

        /// <summary>Thử đường start -> c1 -> c2 -> end; null nếu bị chắn.</summary>
        private static PathResult? TryBridge(BoardModel board, GridPoint start, GridPoint c1, GridPoint c2, GridPoint end)
        {
            if (c1 == c2) return null; // suy biến thành đường thẳng, đã xét ở TryStraight
            if (!board.IsFree(c1.Row, c1.Col) || !board.IsFree(c2.Row, c2.Col)) return null;
            if (!IsLineClear(board, start, c1)) return null;
            if (!IsLineClear(board, c1, c2)) return null;
            if (!IsLineClear(board, c2, end)) return null;
            return new PathResult(new[] { start, c1, c2, end });
        }

        private static PathResult? Better(PathResult? current, PathResult? candidate)
        {
            if (candidate == null) return current;
            if (current == null || candidate.Length < current.Length) return candidate;
            return current;
        }

        // ---------- Tiện ích ----------

        /// <summary>
        /// Các ô nằm nghiêm ngặt giữa p và q (không tính p, q) có đều trống không?
        /// p và q phải cùng hàng hoặc cùng cột, nếu không trả về false.
        /// </summary>
        private static bool IsLineClear(BoardModel board, GridPoint p, GridPoint q)
        {
            if (p.Row != q.Row && p.Col != q.Col) return false;

            int dr = Math.Sign(q.Row - p.Row);
            int dc = Math.Sign(q.Col - p.Col);
            int r = p.Row + dr;
            int c = p.Col + dc;

            while (r != q.Row || c != q.Col)
            {
                if (!board.IsFree(r, c)) return false;
                r += dr;
                c += dc;
            }
            return true;
        }
    }
}
