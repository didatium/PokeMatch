
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PokeMatch.WPF.ViewModels
{
    public partial class GamePlayViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isPaused;

        [ObservableProperty]
        private PauseMenuViewModel _pauseMenu = null!;

        // Thông báo cho MainViewModel khi cần quay về Menu.
        public event Action? MainMenuRequested;

        public GamePlayViewModel()
        {
            PauseMenu = new PauseMenuViewModel();

            PauseMenu.ResumeRequested += Resume;

            // Nhận yêu cầu quay về Main Menu.
            PauseMenu.MainMenuRequested += ReturnToMainMenu;
        }

        [RelayCommand]
        private void Pause()
        {
            IsPaused = true;
        }

        [RelayCommand]
        private void Resume()
        {
            IsPaused = false;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        private void ReturnToMainMenu()
        {
            MainMenuRequested?.Invoke();
        }
    }
}
