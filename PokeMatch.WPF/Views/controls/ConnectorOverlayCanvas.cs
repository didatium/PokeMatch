using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PokeMatch.WPF.Core;

namespace PokeMatch.WPF.Views.controls
{
    /// <summary>
    /// Lớp Canvas trong suốt phủ đè lên bảng cờ để vẽ đường nối tia sét phát sáng (<see cref="Polyline"/>)
    /// và hiệu ứng hạt nổ pixel khi các thẻ bài bị triệt tiêu.
    /// </summary>
    public class ConnectorOverlayCanvas : Canvas
    {
        private static readonly Brush GlowBrush = CreateFrozenBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
        private static readonly Brush CoreBrush = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFA, 0xD1));
        private static readonly DropShadowEffect LightningGlowEffect = CreateFrozenGlowEffect();
        private static readonly Color[] DefaultExplosionColors =
        {
            Color.FromRgb(0xFF, 0xE0, 0x66),
            Color.FromRgb(0xFF, 0x9F, 0x1C),
            Color.FromRgb(0x00, 0xE5, 0xFF),
            Color.FromRgb(0xFF, 0xFF, 0xFF)
        };

        public ConnectorOverlayCanvas()
        {
            Background = Brushes.Transparent;
            IsHitTestVisible = false;
            // Cho phép đường tia sét đi qua viền ảo bên ngoài bảng cờ (Row/Col = -1 hoặc Rows/Columns).
            ClipToBounds = false;
        }

        /// <summary>
        /// Quy đổi danh sách <see cref="GridPoint"/> từ <see cref="PathFinder"/> sang tọa độ pixel trên Canvas
        /// (tâm của từng ô trên lưới).
        /// </summary>
        public static List<Point> ToCanvasPoints(
            IReadOnlyList<GridPoint> gridPoints,
            double cellWidth,
            double cellHeight,
            Point originOffset = default)
        {
            ArgumentNullException.ThrowIfNull(gridPoints);

            var result = new List<Point>(gridPoints.Count);
            foreach (var gp in gridPoints)
            {
                double x = originOffset.X + (gp.Col + 0.5) * cellWidth;
                double y = originOffset.Y + (gp.Row + 0.5) * cellHeight;
                result.Add(new Point(x, y));
            }
            return result;
        }

        /// <summary>
        /// Vẽ đường tia sét đa giác phát sáng đi qua danh sách điểm <paramref name="points"/> và tự động mờ dần.
        /// </summary>
        public Task DrawLightningPathAsync(IReadOnlyList<Point> points, int durationMs = 250)
        {
            if (points == null || points.Count < 2)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();

            var glowLine = new Polyline
            {
                Points = new PointCollection(points),
                Stroke = GlowBrush,
                StrokeThickness = 8,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0.75,
                Effect = LightningGlowEffect,
                IsHitTestVisible = false
            };

            var jaggedPoints = BuildJaggedLightningPoints(points);
            var coreLine = new Polyline
            {
                Points = jaggedPoints,
                Stroke = CoreBrush,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 1.0,
                IsHitTestVisible = false
            };

            Children.Add(glowLine);
            Children.Add(coreLine);

            var duration = TimeSpan.FromMilliseconds(Math.Max(50, durationMs));
            var fadeGlow = new DoubleAnimation(0.75, 0.0, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            var fadeCore = new DoubleAnimation(1.0, 0.0, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            void OnCompleted(object? s, EventArgs e)
            {
                fadeCore.Completed -= OnCompleted;
                Children.Remove(glowLine);
                Children.Remove(coreLine);
                tcs.TrySetResult(true);
            }

            fadeCore.Completed += OnCompleted;
            glowLine.BeginAnimation(OpacityProperty, fadeGlow);
            coreLine.BeginAnimation(OpacityProperty, fadeCore);

            return tcs.Task;
        }

        /// <summary>Phiên bản fire-and-forget tiện gọi từ code đồng bộ.</summary>
        public void DrawLightningPath(IReadOnlyList<Point> points, int durationMs = 250)
        {
            _ = DrawLightningPathAsync(points, durationMs);
        }

        /// <summary>
        /// Tạo hiệu ứng nổ hạt pixel tại tọa độ <paramref name="center"/> khi triệt tiêu thẻ bài.
        /// </summary>
        public void SpawnExplosion(Point center, int particleCount = 12, int durationMs = 320)
        {
            if (particleCount <= 0) return;

            var duration = TimeSpan.FromMilliseconds(Math.Max(80, durationMs));
            var rng = Random.Shared;

            for (int i = 0; i < particleCount; i++)
            {
                double angle = (2.0 * Math.PI * i / particleCount) + (rng.NextDouble() - 0.5) * 0.4;
                double distance = 18.0 + rng.NextDouble() * 26.0;
                double size = rng.Next(4, 8);

                var color = DefaultExplosionColors[i % DefaultExplosionColors.Length];
                var particle = new Rectangle
                {
                    Width = size,
                    Height = size,
                    Fill = CreateFrozenBrush(color),
                    IsHitTestVisible = false,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                var translate = new TranslateTransform(0, 0);
                var scale = new ScaleTransform(1, 1);
                var group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(translate);
                particle.RenderTransform = group;

                SetLeft(particle, center.X - size / 2.0);
                SetTop(particle, center.Y - size / 2.0);
                Children.Add(particle);

                var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
                var animX = new DoubleAnimation(0, Math.Cos(angle) * distance, duration) { EasingFunction = easeOut };
                var animY = new DoubleAnimation(0, Math.Sin(angle) * distance, duration) { EasingFunction = easeOut };
                var animScale = new DoubleAnimation(1.1, 0.2, duration);
                var animOpacity = new DoubleAnimation(1.0, 0.0, duration);

                void OnParticleDone(object? s, EventArgs e)
                {
                    animOpacity.Completed -= OnParticleDone;
                    Children.Remove(particle);
                }

                animOpacity.Completed += OnParticleDone;
                translate.BeginAnimation(TranslateTransform.XProperty, animX);
                translate.BeginAnimation(TranslateTransform.YProperty, animY);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);
                particle.BeginAnimation(OpacityProperty, animOpacity);
            }
        }

        /// <summary>Xóa toàn bộ hiệu ứng đang hiển thị trên Canvas (dùng khi Restart hoặc thoát màn).</summary>
        public void ClearEffects()
        {
            Children.Clear();
        }

        private static PointCollection BuildJaggedLightningPoints(IReadOnlyList<Point> points)
        {
            var jagged = new PointCollection();
            var rng = Random.Shared;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                jagged.Add(p1);

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double segmentLength = Math.Sqrt(dx * dx + dy * dy);
                if (segmentLength < 12) continue;

                int subdivisions = Math.Clamp((int)(segmentLength / 22), 2, 6);
                // Vector pháp tuyến để tạo độ lệch răng cưa vuông góc với đoạn nối
                double nx = -dy / segmentLength;
                double ny = dx / segmentLength;

                for (int s = 1; s < subdivisions; s++)
                {
                    double t = (double)s / subdivisions;
                    double offset = (rng.NextDouble() * 2.0 - 1.0) * 5.0;
                    jagged.Add(new Point(
                        p1.X + dx * t + nx * offset,
                        p1.Y + dy * t + ny * offset));
                }
            }

            jagged.Add(points[^1]);
            return jagged;
        }

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static DropShadowEffect CreateFrozenGlowEffect()
        {
            var effect = new DropShadowEffect
            {
                Color = Color.FromRgb(0x00, 0xE5, 0xFF),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.95
            };
            effect.Freeze();
            return effect;
        }
    }
}

