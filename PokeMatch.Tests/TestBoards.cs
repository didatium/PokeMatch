namespace PokeMatch.Tests
{
    /// <summary>
    /// Các ma trận mẫu dùng chung để test PathFinder sau này.
    /// Ký hiệu: '.' = ô trống, '1'-'9' = Id Pokemon. Toạ độ ghi theo (hàng, cột).
    /// Bên ngoài bàn cờ luôn có viền ảo trống rộng 1 ô.
    /// </summary>
    public static class TestBoards
    {
        /// <summary>Hai ô '1' cùng hàng, 0 góc rẽ: (1,0) -> (1,3).</summary>
        public static readonly string[] StraightLine =
        {
            "....",
            "1..1",
            "...."
        };

        /// <summary>Hai ô '1' cần đúng 1 góc rẽ: (0,0) -> (0,2) -> (2,2) hoặc qua (2,0).</summary>
        public static readonly string[] OneCorner =
        {
            "1...",
            "....",
            "..1."
        };

        /// <summary>Ô '2' chắn giữa hai ô '1', phải đi vòng qua hàng 1: 2 góc rẽ.</summary>
        public static readonly string[] TwoCorners =
        {
            "1.2.1",
            ".....",
            "....."
        };

        /// <summary>Hàng 0 bị chắn bởi '2', hàng 1 kín: chỉ đi được qua viền ảo phía trên (2 góc rẽ).</summary>
        public static readonly string[] ViaOutsideBorder =
        {
            "1221",
            "2222"
        };

        /// <summary>Ô '1' ở (1,1) bị vây kín bởi '2': không có đường tới '1' ở (1,4).</summary>
        public static readonly string[] Blocked =
        {
            "222..",
            "212.1",
            "222.."
        };

        /// <summary>
        /// Z-path: hai ô '1' ở (0,0) và (2,3). Hai góc của L-path là (0,3) và (2,0) đều bị '2' chiếm,
        /// nên phải đi 2 góc, ví dụ (0,0) -> (0,1) -> (2,1) -> (2,3).
        /// </summary>
        public static readonly string[] ZPath =
        {
            "1..2",
            "....",
            "2..1"
        };

        /// <summary>
        /// Cần 3 góc rẽ nên KHÔNG nối được. '1' ở (1,1) chỉ thoát ra được về phía đông,
        /// '1' ở (5,5) chỉ vào được từ phía bắc, và (1,5) bị chặn để không có L-path.
        /// </summary>
        public static readonly string[] RequiresThreeTurns =
        {
            ".2.....",
            "21...2.",
            ".2.....",
            ".......",
            ".......",
            "....212",
            ".....2."
        };

        /// <summary>Mọi ô đều khác loại: không có cặp nào, hết nước đi.</summary>
        public static readonly string[] AllUnique =
        {
            "123",
            "456",
            "789"
        };

        /// <summary>
        /// Cặp '1' ở (0,0) và (1,1), nhưng '1' ở giữa bị vòng ngoài vây kín: hết nước đi.
        /// Nếu xoá ô (0,1) thì có L-path (0,0) -> (0,1) -> (1,1).
        /// </summary>
        public static readonly string[] SealedPair =
        {
            "1AB",
            "C1D",
            "EFG"
        };
    }
}
