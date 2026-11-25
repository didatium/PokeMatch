using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PokeMatch.WPF.Views.controls
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
                new PropertyMetadata((ImageSource?)null));

        public static readonly DependencyProperty TileIdProperty =
            DependencyProperty.Register("TileId", typeof(int), typeof(TileItemControl),
                new PropertyMetadata(0));

        public static readonly DependencyProperty IsExplodingProperty =
            DependencyProperty.Register("IsExploding", typeof(bool), typeof(TileItemControl),
                new PropertyMetadata(false, OnIsExplodingChanged));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public ImageSource? AssetSource
        {
            get => GetValue(AssetSourceProperty) as ImageSource;
            set => SetValue(AssetSourceProperty, value);
        }

        public int TileId
        {
            get => (int)GetValue(TileIdProperty);
            set => SetValue(TileIdProperty, value);
        }

        public bool IsExploding
        {
            get => (bool)GetValue(IsExplodingProperty);
            set => SetValue(IsExplodingProperty, value);
        }

        public TileItemControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Chạy hiệu ứng nổ tan biến (phóng nhẹ -> thu nhỏ + mờ dần) khi thẻ bài bị triệt tiêu.
        /// </summary>
        public Task PlayExplodeAnimationAsync(int durationMs = 250)
        {
            if (TileBorder == null || TileScale == null)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();
            int popMs = Math.Max(40, durationMs / 3);
            int shrinkMs = Math.Max(60, durationMs - popMs);

            var scaleXAnim = new DoubleAnimationUsingKeyFrames();
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.22, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(popMs))));
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0.15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(popMs + shrinkMs)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            });

            var scaleYAnim = new DoubleAnimationUsingKeyFrames();
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.22, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(popMs))));
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0.15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(popMs + shrinkMs)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            });

            var opacityAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(popMs + shrinkMs))
            {
                BeginTime = TimeSpan.FromMilliseconds(popMs / 2)
            };

            void OnCompleted(object? s, EventArgs e)
            {
                opacityAnim.Completed -= OnCompleted;
                tcs.TrySetResult(true);
            }

            opacityAnim.Completed += OnCompleted;
            TileScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            TileScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
            TileBorder.BeginAnimation(OpacityProperty, opacityAnim);

            return tcs.Task;
        }

        /// <summary>
        /// Hiệu ứng lắc nhẹ theo chiều ngang khi người chơi chọn sai cặp ô.
        /// </summary>
        public void PlayShakeAnimation()
        {
            if (TileTranslate == null) return;

            var shake = new DoubleAnimationUsingKeyFrames();
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(40))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(190))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230))));

            TileTranslate.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        /// <summary>
        /// Khôi phục trạng thái hiển thị ban đầu (dùng khi tái sử dụng ô lúc Restart hoặc Shuffle).
        /// </summary>
        public void ResetVisualState()
        {
            if (TileBorder != null)
            {
                TileBorder.BeginAnimation(OpacityProperty, null);
                TileBorder.Opacity = 1.0;
            }
            if (TileScale != null)
            {
                TileScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                TileScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                TileScale.ScaleX = 1.0;
                TileScale.ScaleY = 1.0;
            }
            if (TileTranslate != null)
            {
                TileTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                TileTranslate.X = 0.0;
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TileItemControl)d;
            if (control.TileBorder != null)
            {
                control.TileBorder.BorderBrush = (bool)e.NewValue
                    ? Brushes.Green
                    : Brushes.Transparent;
            }
        }

        private static void OnIsExplodingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TileItemControl)d;
            if ((bool)e.NewValue)
            {
                _ = control.PlayExplodeAnimationAsync();
            }
            else
            {
                control.ResetVisualState();
            }
        }
    }
}
