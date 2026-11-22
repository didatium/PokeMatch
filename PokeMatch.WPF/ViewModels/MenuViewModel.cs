using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PokeMatch.WPF.ViewModels
{
    public partial class MenuViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSettingsOpen;

        [ObservableProperty]
        private double _musicVolume = 70;

        [ObservableProperty]
        private double _brightness = 100;

        public event Action? NewGameRequested;
        public event Action? ContinueRequested;

        [RelayCommand]
        private void Settings()
        {
            IsSettingsOpen = true;
        }

        [RelayCommand]
        private void CloseSettings()
        {
            IsSettingsOpen = false;
        }

        [RelayCommand]
        private void NewGame()
        {
            NewGameRequested?.Invoke();
        }

        [RelayCommand]
        private void Continue()
        {
            ContinueRequested?.Invoke();
        }

        [RelayCommand]
        private void Quit()
        {
            Application.Current?.Shutdown();
        }
    }
}