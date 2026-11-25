using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PokeMatch.WPF.Services
{
    /// <summary>
    /// Quản lý phát nhạc nền (BGM) lặp và Sound Pool cho hiệu ứng âm thanh (SFX)
    /// sử dụng <see cref="MediaPlayer"/>.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private const int DefaultPoolSizePerSound = 4;

        private readonly MediaPlayer _bgmPlayer;
        private readonly Dictionary<string, SfxChannelPool> _sfxPools = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _audioBaseDirectory;

        private double _musicVolume = 0.7;
        private double _sfxVolume = 0.7;
        private bool _isMuted;
        private bool _isBgmLooping = true;
        private string? _currentBgmPath;
        private bool _disposed;

        /// <summary>Âm lượng nhạc nền (0.0 đến 1.0).</summary>
        public double MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Math.Clamp(value, 0.0, 1.0);
                UpdateBgmVolume();
            }
        }

        /// <summary>Âm lượng hiệu ứng âm thanh (0.0 đến 1.0).</summary>
        public double SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Math.Clamp(value, 0.0, 1.0);
                UpdateAllSfxVolumes();
            }
        }

        /// <summary>Tắt/bật toàn bộ âm thanh.</summary>
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                UpdateBgmVolume();
                UpdateAllSfxVolumes();
            }
        }

        public AudioService(string? audioBaseDirectory = null)
        {
            _audioBaseDirectory = audioBaseDirectory
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");

            _bgmPlayer = new MediaPlayer();
            _bgmPlayer.MediaEnded += OnBgmMediaEnded;
            _bgmPlayer.MediaFailed += OnMediaFailed;
            UpdateBgmVolume();
        }

        // ---------- Điều chỉnh âm lượng theo thang 0-100 (khớp với MenuViewModel) ----------

        public void SetMusicVolumePercent(double percent) => MusicVolume = percent / 100.0;

        public void SetSfxVolumePercent(double percent) => SfxVolume = percent / 100.0;

        // ---------- Nhạc nền (BGM) ----------

        /// <summary>
        /// Phát nhạc nền từ đường dẫn tương đối trong Assets/Audio hoặc đường dẫn tuyệt đối.
        /// Nếu file chưa tồn tại, hàm bỏ qua an toàn để không làm crash ứng dụng.
        /// </summary>
        public void PlayBgm(string filePath, bool loop = true)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(filePath)) return;

            string? resolvedPath = ResolveAudioPath(filePath);
            if (resolvedPath == null) return;

            RunOnUiThread(() =>
            {
                if (_disposed) return;
                _isBgmLooping = loop;

                if (string.Equals(_currentBgmPath, resolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _bgmPlayer.Position = TimeSpan.Zero;
                    _bgmPlayer.Play();
                    return;
                }

                _currentBgmPath = resolvedPath;
                _bgmPlayer.Open(new Uri(resolvedPath, UriKind.Absolute));
                UpdateBgmVolume();
                _bgmPlayer.Play();
            });
        }

        public void PauseBgm()
        {
            if (_disposed) return;
            RunOnUiThread(() =>
            {
                if (!_disposed) _bgmPlayer.Pause();
            });
        }

        public void ResumeBgm()
        {
            if (_disposed || _currentBgmPath == null) return;
            RunOnUiThread(() =>
            {
                if (!_disposed) _bgmPlayer.Play();
            });
        }

        public void StopBgm()
        {
            if (_disposed) return;
            RunOnUiThread(() =>
            {
                if (!_disposed)
                {
                    _bgmPlayer.Stop();
                    _bgmPlayer.Position = TimeSpan.Zero;
                }
            });
        }

        // ---------- Hiệu ứng âm thanh (SFX Sound Pool) ----------

        /// <summary>
        /// Nạp trước một hiệu ứng vào Sound Pool với số lượng player cố định để phát không độ trễ.
        /// </summary>
        public void PreloadSfx(string key, string filePath, int poolSize = DefaultPoolSizePerSound)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(filePath)) return;

            string? resolvedPath = ResolveAudioPath(filePath);
            if (resolvedPath == null) return;

            RunOnUiThread(() =>
            {
                if (_disposed) return;
                GetOrCreatePool(key, resolvedPath, Math.Max(1, poolSize));
            });
        }

        /// <summary>
        /// Phát SFX theo key đã preload hoặc đường dẫn file. Tái sử dụng player trong pool theo vòng tròn (round-robin).
        /// </summary>
        public void PlaySfx(string keyOrFilePath)
        {
            if (_disposed || string.IsNullOrWhiteSpace(keyOrFilePath)) return;
            if (_isMuted || _sfxVolume <= 0.0) return;

            RunOnUiThread(() =>
            {
                if (_disposed) return;

                if (!_sfxPools.TryGetValue(keyOrFilePath, out var pool))
                {
                    string? resolvedPath = ResolveAudioPath(keyOrFilePath);
                    if (resolvedPath == null) return;
                    pool = GetOrCreatePool(keyOrFilePath, resolvedPath, DefaultPoolSizePerSound);
                }

                pool.PlayNext(_isMuted ? 0.0 : _sfxVolume);
            });
        }

        // ---------- Quản lý tài nguyên & Helpers ----------

        private SfxChannelPool GetOrCreatePool(string key, string resolvedPath, int poolSize)
        {
            if (_sfxPools.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var pool = new SfxChannelPool(new Uri(resolvedPath, UriKind.Absolute), poolSize, OnMediaFailed);
            _sfxPools[key] = pool;
            return pool;
        }

        private string? ResolveAudioPath(string filePath)
        {
            if (Path.IsPathRooted(filePath))
            {
                return File.Exists(filePath) ? filePath : null;
            }

            string candidate = Path.Combine(_audioBaseDirectory, filePath);
            if (File.Exists(candidate)) return candidate;

            string baseRelative = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            return File.Exists(baseRelative) ? baseRelative : null;
        }

        private void UpdateBgmVolume()
        {
            RunOnUiThread(() =>
            {
                if (!_disposed)
                {
                    _bgmPlayer.Volume = _isMuted ? 0.0 : _musicVolume;
                }
            });
        }

        private void UpdateAllSfxVolumes()
        {
            double effectiveVolume = _isMuted ? 0.0 : _sfxVolume;
            RunOnUiThread(() =>
            {
                if (_disposed) return;
                foreach (var pool in _sfxPools.Values)
                {
                    pool.SetVolume(effectiveVolume);
                }
            });
        }

        private void OnBgmMediaEnded(object? sender, EventArgs e)
        {
            if (_disposed || !_isBgmLooping) return;
            _bgmPlayer.Position = TimeSpan.Zero;
            _bgmPlayer.Play();
        }

        private static void OnMediaFailed(object? sender, ExceptionEventArgs e)
        {
            // Bỏ qua lỗi giải mã media để tránh crash game loop khi file âm thanh bị lỗi.
            System.Diagnostics.Debug.WriteLine($"[AudioService] MediaFailed: {e.ErrorException?.Message}");
        }

        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            void Cleanup()
            {
                _bgmPlayer.MediaEnded -= OnBgmMediaEnded;
                _bgmPlayer.MediaFailed -= OnMediaFailed;
                _bgmPlayer.Stop();
                _bgmPlayer.Close();

                foreach (var pool in _sfxPools.Values)
                {
                    pool.Dispose();
                }
                _sfxPools.Clear();
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                Cleanup();
            }
            else
            {
                dispatcher.Invoke(Cleanup);
            }
        }

        /// <summary>
        /// Pool các <see cref="MediaPlayer"/> cho một âm thanh SFX cụ thể,
        /// tránh việc cấp phát mới liên tục và không cắt ngang tiếng đang phát.
        /// </summary>
        private sealed class SfxChannelPool : IDisposable
        {
            private readonly List<MediaPlayer> _players;
            private readonly EventHandler<ExceptionEventArgs> _failedHandler;
            private int _nextIndex;

            public SfxChannelPool(Uri sourceUri, int poolSize, EventHandler<ExceptionEventArgs> failedHandler)
            {
                _failedHandler = failedHandler;
                _players = new List<MediaPlayer>(poolSize);

                for (int i = 0; i < poolSize; i++)
                {
                    var player = new MediaPlayer();
                    player.MediaFailed += _failedHandler;
                    player.Open(sourceUri);
                    _players.Add(player);
                }
            }

            public void PlayNext(double volume)
            {
                if (_players.Count == 0) return;

                var player = _players[_nextIndex];
                _nextIndex = (_nextIndex + 1) % _players.Count;

                player.Volume = volume;
                player.Position = TimeSpan.Zero;
                player.Play();
            }

            public void SetVolume(double volume)
            {
                foreach (var player in _players)
                {
                    player.Volume = volume;
                }
            }

            public void Dispose()
            {
                foreach (var player in _players)
                {
                    player.MediaFailed -= _failedHandler;
                    player.Stop();
                    player.Close();
                }
                _players.Clear();
            }
        }
    }
}

