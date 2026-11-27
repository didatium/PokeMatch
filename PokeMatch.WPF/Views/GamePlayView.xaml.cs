
using System.Windows.Controls;
using System.Windows.Input;
using PokeMatch.WPF.ViewModels;

namespace PokeMatch.WPF.Views
{
    public partial class GamePlayView : UserControl
    {
        public GamePlayView()
        {
            InitializeComponent();

            Loaded += GamePlayView_Loaded;
            PreviewKeyDown += GamePlayView_PreviewKeyDown;
        }

        private void GamePlayView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Focusable = true;
            Focus();
        }

        private void GamePlayView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape &&
                DataContext is GamePlayViewModel viewModel)
            {
                viewModel.TogglePause();
                e.Handled = true;
            }
        }
    }
}
