using System.Collections.Generic;
using System.IO;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SpaceInvaders.Wpf.Services;

/// <summary>
/// Simple audio manager for the game: one looping music channel and many short SFX.
/// Uses NAudio + Vorbis so .ogg works reliably.
/// </summary>
public sealed class AudioManager : IDisposable
{
    private readonly object _gate = new();

    private bool _isMuted;
    private float _musicVolume = 0.35f;
    private float _sfxVolume = 0.85f;

    private IWavePlayer? _musicOut;
    private VorbisWaveReader? _musicReader;

    // Keep track of currently playing SFX so we can dispose them when finished.
    private readonly List<IWavePlayer> _liveSfx = new();

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            ApplyVolumes();
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Clamp01(value);
            ApplyVolumes();
        }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Clamp01(value);
    }

    public void PlaySfx(string absolutePath)
    {
        // Fire-and-forget SFX.
        try
        {
            if (_isMuted) return;
            if (!File.Exists(absolutePath)) return;

            var reader = new VorbisWaveReader(absolutePath);

            // VorbisWaveReader is an ISampleProvider.
            var volumeProvider = new VolumeSampleProvider(reader) { Volume = _sfxVolume };

            var outDevice = new WaveOutEvent();
            outDevice.Init(volumeProvider);

            outDevice.PlaybackStopped += (_, _) =>
            {
                lock (_gate)
                {
                    _liveSfx.Remove(outDevice);
                }

                outDevice.Dispose();
                reader.Dispose();
            };

            lock (_gate)
            {
                _liveSfx.Add(outDevice);
            }

            outDevice.Play();
        }
        catch
        {
            // Swallow audio errors.
        }
    }

    public void PlayMusicLoop(string absolutePath)
    {
        try
        {
            StopMusic();
            if (!File.Exists(absolutePath)) return;

            _musicReader = new VorbisWaveReader(absolutePath);
            var loop = new LoopStream(_musicReader);

            _musicOut = new WaveOutEvent();
            _musicOut.Init(loop);

            ApplyVolumes();

            _musicOut.Play();
        }
        catch
        {
            StopMusic();
        }
    }

    public void StopMusic()
    {
        try { _musicOut?.Stop(); } catch { }

        _musicOut?.Dispose();
        _musicOut = null;

        _musicReader?.Dispose();
        _musicReader = null;
    }

    private void ApplyVolumes()
    {
        if (_musicOut is WaveOutEvent wo)
            wo.Volume = _isMuted ? 0f : _musicVolume;
    }

    public void Dispose()
    {
        StopMusic();

        lock (_gate)
        {
            foreach (var sfx in _liveSfx.ToArray())
            {
                try { sfx.Stop(); } catch { }
                try { sfx.Dispose(); } catch { }
            }

            _liveSfx.Clear();
        }
    }

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    private sealed class LoopStream : WaveStream
    {
        private readonly WaveStream _source;

        public LoopStream(WaveStream source)
        {
            _source = source;
            EnableLooping = true;
        }

        public bool EnableLooping { get; set; }

        public override WaveFormat WaveFormat => _source.WaveFormat;

        public override long Length => _source.Length;

        public override long Position
        {
            get => _source.Position;
            set => _source.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            if (read == 0 && EnableLooping)
            {
                _source.Position = 0;
                read = _source.Read(buffer, offset, count);
            }

            return read;
        }
    }
}
