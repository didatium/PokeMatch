using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PokeMatch.WPF.Views
{
    /// <summary>
    /// Interaction logic for TileItemControl.xaml
    /// </summary>
    public partial class TileItemControl : UserControl
    {
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(TileItemControl),
                new PropertyMetadata(false, OnIsSelectedChanged));

        public static readonly DependencyProperty AssetSourceProperty =
            DependencyProperty.Register("AssetSource", typeof(ImageSource), typeof(TileItemControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TileIdProperty =
            DependencyProperty.Register("TileId", typeof(int), typeof(TileItemControl),
                new PropertyMetadata(0));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public ImageSource AssetSource
        {
            get => (ImageSource)GetValue(AssetSourceProperty);
            set => SetValue(AssetSourceProperty, value);
        }

        public int TileId
        {
            get => (int)GetValue(TileIdProperty);
            set => SetValue(TileIdProperty, value);
        }

        public TileItemControl()
        {
            InitializeComponent();
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TileItemControl)d;
            if (control.TileBorder != null)
            {
                control.TileBorder.BorderBrush = (bool)e.NewValue ? 
                    new SolidColorBrush(Colors.Green) : 
                    new SolidColorBrush(Colors.Transparent);
            }
        }
    }
}
