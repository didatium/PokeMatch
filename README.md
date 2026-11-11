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



#### Link Clone dự án:

```
git clone https://github.com/didatium/PokeMatch.git
git checkout develop
git pull origin develop
```

**Phân công file cụ thể để tránh conflict:**

- **Thành viên 1:** Tạo nhánh `feature/ui-menu`, chỉ làm việc trên thư mục `Views/MainMenuView.xaml` và `ViewModels/MenuViewModel.cs`.
- **Thành viên 2:** Tạo nhánh `feature/ui-gameplay`, chỉ làm việc trên thư mục `Views/GamePlayView.xaml` và `ViewModels/GamePlayViewModel.cs`.
- **Thành viên 3:** Tạo nhánh `feature/core-algorithm`, chỉ tạo file trong thư mục `Core/` và `Models/` (chưa cần đụng vào giao diện).
- **Quy định PR:** Làm xong task thì đẩy lên nhánh feature của mình, tạo Pull Request trỏ vào nhánh `develop` và tag bạn vào review. Tuyệt đối không push thẳng lên `develop` hay `main`
