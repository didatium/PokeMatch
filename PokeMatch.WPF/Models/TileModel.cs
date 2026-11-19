namespace PokeMatch.WPF.Models
{
    /// <summary>
    /// Trạng thái của một ô trên bàn cờ.
    /// </summary>
    public enum TileState
    {
        /// <summary>Ô còn trên bàn, chưa được chọn.</summary>
        Normal = 0,

        /// <summary>Ô đang được người chơi chọn.</summary>
        Selected = 1,

        /// <summary>Ô đã bị ăn (hoặc là ô trống). Đường nối được phép đi qua.</summary>
        Deleted = 2
    }

    /// <summary>
    /// Một ô trên bàn cờ.
    /// Quy ước toạ độ: X = cột, Y = hàng (gốc ở góc trên trái).
    /// Trong <see cref="BoardModel"/> ô nằm ở vị trí [Y, X] = [hàng, cột].
    /// </summary>
    public class TileModel
    {
        /// <summary>Id = 0 nghĩa là ô trống (không có Pokemon).</summary>
        public const int EmptyId = 0;

        /// <summary>Cột của ô (bắt đầu từ 0).</summary>
        public int X { get; }

        /// <summary>Hàng của ô (bắt đầu từ 0).</summary>
        public int Y { get; }

        /// <summary>
        /// Loại Pokemon, trùng với tên file sprite: Assets/Sprites/{Id}.png.
        /// Hai ô ăn được nhau khi cùng Id.
        /// </summary>
        public int Id { get; set; }

        public TileState State { get; set; }

        public bool IsSelected => State == TileState.Selected;

        /// <summary>True nếu ô đã bị ăn hoặc là ô trống.</summary>
        public bool IsDeleted => State == TileState.Deleted;

        public TileModel(int x, int y, int id = EmptyId, TileState state = TileState.Deleted)
        {
            X = x;
            Y = y;
            Id = id;
            State = state;
        }

        public void Select()
        {
            if (!IsDeleted) State = TileState.Selected;
        }

        public void Deselect()
        {
            if (State == TileState.Selected) State = TileState.Normal;
        }

        public void Delete() => State = TileState.Deleted;

        public TileModel Clone() => new TileModel(X, Y, Id, State);

        public override string ToString() => $"Tile(X={X}, Y={Y}, Id={Id}, {State})";
    }
}
