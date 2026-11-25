using PokeMatch.WPF.Models;

namespace PokeMatch.WPF.Core
{
    /// <summary>Một bước của lời giải: ăn hai ô tại hai toạ độ này (theo [hàng, cột]).</summary>
    public readonly record struct SolutionStep(GridPoint First, GridPoint Second);

    /// <summary>Bàn cờ vừa sinh cùng một lời giải hợp lệ (thứ tự ăn từng cặp để dọn sạch bàn).</summary>
    public sealed record GeneratedBoard(BoardModel Board, IReadOnlyList<SolutionStep> Solution);

    /// <summary>
    /// Sinh bàn cờ ĐẢM BẢO GIẢI ĐƯỢC HẾT VÁN.
    ///
    /// Ý tưởng: mô phỏng một ván chơi thật rồi dùng chính nó làm đáp án.
    ///  1. Bắt đầu từ bàn đầy các ô "chưa có loại".
    ///  2. Lặp: chọn ngẫu nhiên hai ô đang nối được nhau (≤ 2 góc rẽ, theo <see cref="PathFinder"/>)
    ///     rồi ăn chúng, ghi lại thành một cặp.
    ///  3. Sau khi ăn hết, gán mỗi cặp một loại Pokemon (hai ô của cặp cùng Id).
    ///
    /// Khi chơi đúng theo thứ tự đã ghi, tại lượt thứ k các ô của k-1 cặp trước đã biến mất,
    /// đúng như lúc mô phỏng nên cặp thứ k luôn nối được => dọn sạch bàn cờ.
    /// Kết quả không phụ thuộc loại Pokemon, vì khả năng nối chỉ phụ thuộc vị trí ô còn lại.
    ///
    /// Lưu ý: đảm bảo này đúng tại thời điểm sinh/xáo. Nếu người chơi ăn "lệch" thứ tự đáp án,
    /// bàn có thể trở nên bí; khi đó dùng <see cref="PathFinder.CheckAvailableMoves"/> để phát hiện
    /// và <see cref="ShuffleRemaining"/> để xếp lại thành bàn giải được.
    /// </summary>
    public static class BoardGenerator
    {
        // Thực tế mô phỏng không bao giờ bị kẹt; vòng thử lại chỉ là lưới an toàn.
        private const int MaxPairingAttempts = 50;

        // ---------- Sinh bàn cờ mới ----------

        /// <summary>Sinh bàn cờ giải được, dùng các loại Pokemon có Id từ 1 đến <paramref name="typeCount"/>.</summary>
        public static BoardModel GenerateBoard(int rows, int columns, int typeCount, Random? random = null) =>
            GenerateBoardWithSolution(rows, columns, typeCount, random).Board;

        /// <summary>Sinh bàn cờ giải được từ danh sách Id Pokemon cho phép (vd: tên file sprite, không cần liên tục).</summary>
        public static BoardModel GenerateBoard(int rows, int columns, IReadOnlyList<int> typeIds, Random? random = null) =>
            GenerateBoardWithSolution(rows, columns, typeIds, random).Board;

        public static GeneratedBoard GenerateBoardWithSolution(int rows, int columns, int typeCount, Random? random = null)
        {
            if (typeCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(typeCount), "typeCount phải > 0.");

            return GenerateBoardWithSolution(rows, columns, Enumerable.Range(1, typeCount).ToList(), random);
        }

        /// <summary>
        /// Sinh bàn cờ giải được kèm lời giải.
        /// - rows * columns phải chẵn (mỗi Id xuất hiện theo cặp).
        /// - Các cặp được chia đều (vòng tròn) cho các loại; nếu nhiều loại hơn số cặp thì chỉ dùng một phần ngẫu nhiên.
        /// </summary>
        public static GeneratedBoard GenerateBoardWithSolution(int rows, int columns,
            IReadOnlyList<int> typeIds, Random? random = null)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "rows phải > 0.");
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns), "columns phải > 0.");
            if ((rows * columns) % 2 != 0)
                throw new ArgumentException("rows * columns phải chẵn để chia được thành các cặp.");
            ArgumentNullException.ThrowIfNull(typeIds);
            if (typeIds.Count == 0)
                throw new ArgumentException("Cần ít nhất 1 loại Pokemon.", nameof(typeIds));
            if (typeIds.Any(id => id <= TileModel.EmptyId))
                throw new ArgumentException("Id Pokemon phải > 0.", nameof(typeIds));
            if (typeIds.Distinct().Count() != typeIds.Count)
                throw new ArgumentException("Danh sách Id không được trùng lặp.", nameof(typeIds));

            random ??= Random.Shared;
            int pairCount = rows * columns / 2;

            // 1) Chia vị trí thành các cặp theo một ván mô phỏng.
            var allCells = new List<GridPoint>(rows * columns);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    allCells.Add(new GridPoint(r, c));
            var steps = BuildPairing(rows, columns, allCells, random);

            // 2) Gán loại Pokemon cho từng cặp.
            var types = typeIds.ToList();
            Shuffle(types, random);
            var pairIds = new List<int>(pairCount);
            for (int i = 0; i < pairCount; i++)
                pairIds.Add(types[i % types.Count]);
            Shuffle(pairIds, random);

            var board = new BoardModel(rows, columns);
            for (int i = 0; i < steps.Count; i++)
            {
                board.SetTile(steps[i].First.Row, steps[i].First.Col, pairIds[i]);
                board.SetTile(steps[i].Second.Row, steps[i].Second.Col, pairIds[i]);
            }

            return new GeneratedBoard(board, steps);
        }

        // ---------- Xáo trộn ----------

        /// <summary>
        /// Xáo lại các ô còn trên bàn thành một cách xếp GIẢI ĐƯỢC (dùng khi hết nước đi hoặc bấm Shuffle).
        /// Ô đã ăn giữ nguyên vị trí; tập hợp các Id còn lại không đổi; mọi ô còn lại về trạng thái Normal.
        /// Trả về lời giải mới (rỗng nếu bàn đã sạch).
        /// </summary>
        /// <exception cref="InvalidOperationException">Có Id xuất hiện số lẻ lần (dữ liệu bàn cờ không hợp lệ).</exception>
        public static IReadOnlyList<SolutionStep> ShuffleRemaining(BoardModel board, Random? random = null)
        {
            ArgumentNullException.ThrowIfNull(board);
            random ??= Random.Shared;

            var remaining = board.GetRemainingTiles().ToList();
            if (remaining.Count == 0) return Array.Empty<SolutionStep>();

            // Mỗi Id còn n ô => n/2 cặp.
            var pairIds = new List<int>(remaining.Count / 2);
            foreach (var group in remaining.GroupBy(t => t.Id))
            {
                int count = group.Count();
                if (count % 2 != 0)
                    throw new InvalidOperationException($"Id {group.Key} còn {count} ô (lẻ), không thể ghép thành cặp.");
                for (int i = 0; i < count / 2; i++) pairIds.Add(group.Key);
            }
            Shuffle(pairIds, random);

            var cells = remaining.Select(t => new GridPoint(t.Y, t.X)).ToList();
            var steps = BuildPairing(board.Rows, board.Columns, cells, random);

            for (int i = 0; i < steps.Count; i++)
            {
                foreach (var p in new[] { steps[i].First, steps[i].Second })
                {
                    var tile = board[p.Row, p.Col];
                    tile.Id = pairIds[i];
                    tile.State = TileState.Normal;
                }
            }
            return steps;
        }

        // ---------- Kiểm tra lời giải ----------

        /// <summary>
        /// Chạy lại <paramref name="solution"/> trên một bản sao của bàn cờ bằng đúng luật của <see cref="PathFinder"/>.
        /// True nếu mọi bước đều ăn được và cuối cùng bàn sạch hoàn toàn. Không làm thay đổi <paramref name="board"/>.
        /// </summary>
        public static bool IsValidSolution(BoardModel board, IReadOnlyList<SolutionStep> solution)
        {
            ArgumentNullException.ThrowIfNull(board);
            ArgumentNullException.ThrowIfNull(solution);

            var copy = board.Clone();
            foreach (var step in solution)
            {
                if (!copy.IsInside(step.First.Row, step.First.Col) || !copy.IsInside(step.Second.Row, step.Second.Col))
                    return false;

                var a = copy[step.First.Row, step.First.Col];
                var b = copy[step.Second.Row, step.Second.Col];
                if (PathFinder.FindPath(copy, a, b) == null) return false;
                copy.RemovePair(a, b);
            }
            return copy.IsCleared;
        }

        // ---------- Nội bộ ----------

        /// <summary>
        /// Mô phỏng ván chơi trên các vị trí <paramref name="cells"/> (số lượng chẵn, các ô còn lại coi như trống):
        /// liên tục chọn hai ô nối được rồi ăn, trả về các cặp theo đúng thứ tự ăn.
        /// </summary>
        private static List<SolutionStep> BuildPairing(int rows, int columns, IReadOnlyList<GridPoint> cells, Random random)
        {
            for (int attempt = 0; attempt < MaxPairingAttempts; attempt++)
            {
                // Bàn nháp: mọi ô cùng Id = 1 để chỉ xét khả năng nối theo vị trí.
                var scratch = new BoardModel(rows, columns);
                foreach (var cell in cells) scratch.SetTile(cell.Row, cell.Col, 1);

                var remaining = cells.Select(c => scratch[c.Row, c.Col]).ToList();
                var steps = new List<SolutionStep>(cells.Count / 2);
                bool stuck = false;

                while (remaining.Count > 0)
                {
                    Shuffle(remaining, random);

                    if (!TryFindConnectablePair(scratch, remaining, out int i, out int j))
                    {
                        stuck = true;
                        break;
                    }

                    var a = remaining[i];
                    var b = remaining[j];
                    steps.Add(new SolutionStep(new GridPoint(a.Y, a.X), new GridPoint(b.Y, b.X)));
                    a.Delete();
                    b.Delete();
                    remaining.RemoveAt(j); // j > i nên xoá j trước
                    remaining.RemoveAt(i);
                }

                if (!stuck) return steps;
            }

            throw new InvalidOperationException(
                $"Không dựng được lời giải cho bàn cờ {rows}x{columns} sau {MaxPairingAttempts} lần thử.");
        }

        private static bool TryFindConnectablePair(BoardModel scratch, IReadOnlyList<TileModel> tiles, out int i, out int j)
        {
            for (i = 0; i < tiles.Count; i++)
            {
                for (j = i + 1; j < tiles.Count; j++)
                {
                    if (PathFinder.CanConnect(scratch, tiles[i], tiles[j])) return true;
                }
            }
            i = j = -1;
            return false;
        }

        /// <summary>Fisher–Yates.</summary>
        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
