using CommunityToolkit.Mvvm.ComponentModel;

namespace PokeMatch.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject _currentView;

        public MainViewModel()
        {
            // Khởi động vào Menu chính
            CurrentView = new MenuViewModel();
        }
    }
}