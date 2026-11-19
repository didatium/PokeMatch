using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PokeMatch.WPF.Views.controls;

namespace PokeMatch.WPF.Views
{
    /// <summary>
    /// Interaction logic for GamePlayView.xaml
    /// </summary>
    public partial class GamePlayView : UserControl
    {
        public GamePlayView()
        {
            InitializeComponent();
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement pause logic
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement restart logic
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement back to menu logic
        }
    }

    // Data model for tiles
    public class TileModel
    {
        public bool IsSelected { get; set; }
        public ImageSource? AssetSource { get; set; }
        public int TileId { get; set; }
    }
}
