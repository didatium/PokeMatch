using System.Collections.Generic;
using System.Linq;

namespace PokeMatch.WPF.Models
{
    /// <summary>
    /// Bàn cờ dạng ma trận 2 chiều. Chỉ chứa dữ liệu, không phụ thuộc UI.
    ///
    /// Quy ước:
    ///  - Truy cập theo [row, col] = [Y, X].
    ///  - Có một "viền ảo" rộng 1 ô bao quanh bàn cờ (row/col = -1 và Rows/Columns).
    ///    Viền này luôn trống để đường nối được đi vòng ngoài bàn cờ
    ///    (luật chuẩn của game Pikachu). Xem <see cref="IsFree"/>.
    /// </summary>
    public class BoardModel
    {
        private readonly TileModel[,] _tiles;

        public int Rows { get; }
        public int Columns { get; }

        /// <summary>Tạo bàn cờ rỗng (mọi ô đều Deleted).</summary>
        public BoardModel(int rows, int columns)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "Rows phải > 0.");
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns), "Columns phải > 0.");

            Rows = rows;
            Columns = columns;
            _tiles = new TileModel[rows, columns];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    _tiles[r, c] = new TileModel(x: c, y: r);
        }

        // ---------- Truy cập ô ----------

        /// <summary>Lấy ô tại [hàng, cột]. Ném lỗi nếu nằm ngoài bàn cờ.</summary>
        public TileModel this[int row, int col]
        {
            get
            {
                if (!IsInside(row, col))
                    throw new ArgumentOutOfRangeException(nameof(row), $"Ô ({row},{col}) nằm ngoài bàn cờ {Rows}x{Columns}.");
                return _tiles[row, col];
            }
        }

        public bool TryGetTile(int row, int col, out TileModel? tile)
        {
            tile = IsInside(row, col) ? _tiles[row, col] : null;
            return tile != null;
        }

        /// <summary>Gán Pokemon cho ô. id = 0 sẽ làm ô trở thành ô trống.</summary>
        public void SetTile(int row, int col, int id)
        {
            var tile = this[row, col];
            tile.Id = id;
            tile.State = id == TileModel.EmptyId ? TileState.Deleted : TileState.Normal;
        }

        // ---------- Kiểm tra vị trí (dùng cho PathFinder) ----------

        /// <summary>Ô nằm trong bàn cờ thật.</summary>
        public bool IsInside(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < Columns;

        /// <summary>Ô nằm trong bàn cờ hoặc trong viền ảo rộng 1 ô.</summary>
        public bool IsInsideExtended(int row, int col) =>
            row >= -1 && row <= Rows && col >= -1 && col <= Columns;

        /// <summary>
        /// Đường nối có đi qua được ô này không?
        /// True nếu là ô trống/đã ăn, hoặc là viền ảo. False nếu có Pokemon hoặc nằm quá xa bàn cờ.
        /// </summary>
        public bool IsFree(int row, int col)
        {
            if (!IsInsideExtended(row, col)) return false;
            if (!IsInside(row, col)) return true; // viền ảo
            return _tiles[row, col].IsDeleted;
        }

        // ---------- Luật ghép cặp ----------

        /// <summary>Hai ô khác nhau, đều còn trên bàn và cùng loại Pokemon.</summary>
        public static bool AreSameType(TileModel a, TileModel b)
        {
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);
            return !ReferenceEquals(a, b)
                   && !a.IsDeleted && !b.IsDeleted
                   && a.Id != TileModel.EmptyId
                   && a.Id == b.Id;
        }

        /// <summary>Ăn một cặp ô. Trả về false (và không đổi gì) nếu không hợp lệ.</summary>
        public bool RemovePair(TileModel a, TileModel b)
        {
            if (!AreSameType(a, b)) return false;
            a.Delete();
            b.Delete();
            return true;
        }

        // ---------- Thống kê ----------

        public IEnumerable<TileModel> GetAllTiles()
        {
            foreach (var tile in _tiles) yield return tile;
        }

        /// <summary>Các ô còn trên bàn (chưa bị ăn).</summary>
        public IEnumerable<TileModel> GetRemainingTiles() => GetAllTiles().Where(t => !t.IsDeleted);

        public int RemainingCount => GetRemainingTiles().Count();

        /// <summary>Thắng khi không còn ô nào.</summary>
        public bool IsCleared => RemainingCount == 0;

        // ---------- Chuyển đổi (tiện cho test, lưu game, shuffle) ----------

        /// <summary>Xuất ra ma trận Id. Ô trống/đã ăn = 0.</summary>
        public int[,] ToMatrix()
        {
            var m = new int[Rows, Columns];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    m[r, c] = _tiles[r, c].IsDeleted ? TileModel.EmptyId : _tiles[r, c].Id;
            return m;
        }

        /// <summary>Tạo bàn cờ từ ma trận Id. 0 = ô trống.</summary>
        public static BoardModel FromMatrix(int[,] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);
            var board = new BoardModel(ids.GetLength(0), ids.GetLength(1));
            for (int r = 0; r < board.Rows; r++)
                for (int c = 0; c < board.Columns; c++)
                    board.SetTile(r, c, ids[r, c]);
            return board;
        }

        /// <summary>
        /// Tạo bàn cờ từ các dòng chữ để viết test cho dễ nhìn.
        /// '.' hoặc '0' = ô trống, '1'-'9' = Id 1-9, 'A'-'Z' = Id 10-35.
        /// Ví dụ: FromLayout("1..1", "2222")
        /// </summary>
        public static BoardModel FromLayout(params string[] rows)
        {
            if (rows == null || rows.Length == 0)
                throw new ArgumentException("Layout phải có ít nhất 1 dòng.", nameof(rows));

            int cols = rows[0].Length;
            if (rows.Any(r => r.Length != cols))
                throw new ArgumentException("Mọi dòng của layout phải có cùng độ dài.", nameof(rows));

            var ids = new int[rows.Length, cols];
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < cols; c++)
                    ids[r, c] = ParseLayoutChar(rows[r][c]);

            return FromMatrix(ids);
        }

        private static int ParseLayoutChar(char ch) => ch switch
        {
            '.' or '0' => TileModel.EmptyId,
            >= '1' and <= '9' => ch - '0',
            >= 'A' and <= 'Z' => ch - 'A' + 10,
            _ => throw new ArgumentException($"Ký tự layout không hợp lệ: '{ch}'.")
        };

        /// <summary>Bản sao độc lập (để Hint/Shuffle thử mà không đụng bàn cờ thật).</summary>
        public BoardModel Clone()
        {
            var copy = new BoardModel(Rows, Columns);
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                {
                    copy._tiles[r, c].Id = _tiles[r, c].Id;
                    copy._tiles[r, c].State = _tiles[r, c].State;
                }
            return copy;
        }
    }
}
