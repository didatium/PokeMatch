
using CommunityToolkit.Mvvm.ComponentModel;

namespace PokeMatch.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject _currentView = null!;

        public MainViewModel()
        {
            ShowMainMenu();
        }

        // Hiển thị Main Menu.
        private void ShowMainMenu()
        {
            var menuViewModel = new MenuViewModel();

            // Khi nhấn NEW GAME, chuyển sang Gameplay.
            menuViewModel.NewGameRequested += OpenGamePlay;

            CurrentView = menuViewModel;
        }

        // Mở Gameplay.
        private void OpenGamePlay()
        {
            var gamePlayViewModel = new GamePlayViewModel();

            // Khi nhấn MAIN MENU, quay lại Menu.
            gamePlayViewModel.MainMenuRequested += ShowMainMenu;

            CurrentView = gamePlayViewModel;
        }
    }
}
