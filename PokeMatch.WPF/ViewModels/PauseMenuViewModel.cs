using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PokeMatch.WPF.ViewModels
{
    public partial class PauseMenuViewModel : ObservableObject
    {
        public event Action? ResumeRequested;
        public event Action? RestartRequested;
        public event Action? SettingsRequested;
        public event Action? MainMenuRequested;

        [RelayCommand]
        private void Resume()
        {
            ResumeRequested?.Invoke();
        }

        [RelayCommand]
        private void Restart()
        {
            RestartRequested?.Invoke();
        }

        [RelayCommand]
        private void Settings()
        {
            SettingsRequested?.Invoke();
        }

        [RelayCommand]
        private void MainMenu()
        {
            MainMenuRequested?.Invoke();
        }
    }
}
