#if WIN_PLATFORM
using NAudio.CoreAudioApi;
using NAudio.Wave;
#endif

namespace OverlayEngine.Core.Services.Audio;

#if WIN_PLATFORM

/// <summary>
/// Real-time mic peak meter via WasapiCapture. Reads volume/mute from AudioEndpointVolume.
/// </summary>
public sealed class WindowsAudioService : IAudioService
{
    private MMDeviceEnumerator? _enumerator;
    private MMDevice?           _device;
    private WasapiCapture?      _capture;
    private volatile float      _peak;
    private bool                _disposed;

    public WindowsAudioService()
    {
        try
        {
            _enumerator = new MMDeviceEnumerator();
            _device     = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

            _capture                   = new WasapiCapture(_device);
            _capture.DataAvailable    += OnData;
            _capture.RecordingStopped += OnStopped;
            _capture.StartRecording();
        }
        catch { /* no mic or audio subsystem unavailable */ }
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;
        var   fmt = _capture!.WaveFormat;
        float max = 0f;

        if (fmt.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            for (int i = 0; i + 3 < e.BytesRecorded; i += 4)
            {
                float s = Math.Abs(BitConverter.ToSingle(e.Buffer, i));
                if (s > max) max = s;
            }
        }
        else
        {
            int bytesPerSample = Math.Max(1, fmt.BitsPerSample / 8);
            int stride         = bytesPerSample * Math.Max(1, fmt.Channels);
            for (int i = 0; i + bytesPerSample - 1 < e.BytesRecorded; i += stride)
            {
                float s = fmt.BitsPerSample switch
                {
                    16 => Math.Abs(BitConverter.ToInt16(e.Buffer, i) / 32768f),
                    32 => Math.Abs(BitConverter.ToInt32(e.Buffer, i) / 2147483648f),
                    _  => 0f
                };
                if (s > max) max = s;
            }
        }

        // Fast attack, slow decay (~0.85 per 100 ms buffer)
        _peak = max > _peak ? max : _peak * 0.85f;
    }

    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        if (_disposed) return;
        try { _capture?.StartRecording(); } catch { }
    }

    public double PeakLevel => _device != null ? _peak : -1.0;

    public Task<AudioSnapshot> ReadAsync()
    {
        try
        {
            if (_device is null) return Task.FromResult(new AudioSnapshot(-60, false));
            float  vol   = _device.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 0f;
            bool   muted = _device.AudioEndpointVolume?.Mute ?? false;
            double db    = vol > 0f ? 20.0 * Math.Log10(vol) : -60.0;
            return Task.FromResult(new AudioSnapshot(db, muted));
        }
        catch { return Task.FromResult(new AudioSnapshot(-60, false)); }
    }

    public void Dispose()
    {
        _disposed = true;
        try { _capture?.StopRecording(); } catch { }
        try { _capture?.Dispose();       } catch { }
        try { _device?.Dispose();        } catch { }
        try { _enumerator?.Dispose();    } catch { }
    }
}

#else

// Stub for non-Windows compilation.
public sealed class WindowsAudioService : IAudioService
{
    public double PeakLevel => -1.0;
    public Task<AudioSnapshot> ReadAsync() => Task.FromResult(new AudioSnapshot(-60, false));
    public void Dispose() { }
}

#endif
