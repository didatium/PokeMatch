# PokeMatch

#### Project Structure

```
PokemonMatch.WPF/
│
├── Assets/
│   ├── Fonts/          # Font pixel (vd: PressStart2P.ttf)
│   ├── Sprites/        # Sprite quái vật 32x32 hoặc 48x48
│   ├── SFX/            # Tiếng click, match, explosion, win, lose
│   └── Music/          # BGM retro 8-bit
│
├── Models/
│   ├── Tile.cs         # X, Y, TypeID, IsDeleted, IsSelected
│   ├── Board.cs        # Ma trận 2D quản lý Tile[,]
│   └── GameSettings.cs # Cấu hình độ khó, âm lượng, volume
│
├── Core/ (Algorithms)
│   ├── PathFinder.cs   # Thuật toán tìm đường <= 2 góc rẽ
│   └── BoardGenerator.cs # Thuật toán tạo bàn cờ hợp lệ
│
├── ViewModels/
│   ├── MainViewModel.cs       # Điều hướng giữa Menu và Gameplay
│   ├── MenuViewModel.cs       # Xử lý sự kiện 4 nút Main Menu
│   └── GamePlayViewModel.cs   # Quản lý Timer, Score, Lưới game
│
├── Views/
│   ├── MainMenuView.xaml      # Giao diện chính 4 nút
│   ├── GamePlayView.xaml      # Bảng 70% + HUD góc trên trái
│   └── Controls/
│       └── ConnectorCanvas.cs # Canvas vẽ đường nối tia sét
│
├── Services/
│   ├── AudioService.cs        # Phát âm thanh không chặn UI Thread
│   └── StorageService.cs      # Lưu file savegame.json, highscore.json
│
└── App.xaml                   # Global Styles, Brushes, Palette
```

