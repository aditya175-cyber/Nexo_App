using System.Runtime.InteropServices;

namespace WinIsland.UI.Services;

/// <summary>
/// System volume control via Windows Core Audio COM API.
/// </summary>
public class AudioService : IDisposable
{
    private IAudioEndpointVolume? _volume;
    private bool _disposed;

    public AudioService()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out var device);
            var iid = typeof(IAudioEndpointVolume).GUID;
            device.Activate(ref iid, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out var obj);
            _volume = (IAudioEndpointVolume)obj;
        }
        catch
        {
            _volume = null;
        }
    }

    public bool IsAvailable => _volume != null;

    public float GetVolume()
    {
        if (_volume == null) return 0;
        _volume.GetMasterVolumeLevelScalar(out float level);
        return level;
    }

    public void SetVolume(float level)
    {
        if (_volume == null) return;
        var guid = Guid.Empty;
        _volume.SetMasterVolumeLevelScalar(Math.Clamp(level, 0f, 1f), ref guid);
    }

    public bool GetMute()
    {
        if (_volume == null) return false;
        _volume.GetMute(out bool mute);
        return mute;
    }

    public void SetMute(bool mute)
    {
        if (_volume == null) return;
        var guid = Guid.Empty;
        _volume.SetMute(mute, ref guid);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_volume != null)
            Marshal.ReleaseComObject(_volume);
    }

    // ── COM Interop Declarations ────────────────────────────

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(uint dataFlow, uint dwStateMask, out IntPtr ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(uint dataFlow, uint role, out IMMDevice ppEndpoint);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams,
                                   [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int GetChannelCount(out uint pnChannelCount);
        [PreserveSig] int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float pfLevelDB);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float pfLevel);
        [PreserveSig] int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }
}

