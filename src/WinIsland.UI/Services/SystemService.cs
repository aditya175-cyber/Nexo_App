using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinIsland.UI.Services;

/// <summary>
/// Power and system control actions via native APIs.
/// </summary>
public static class SystemService
{
    [DllImport("user32.dll")] private static extern bool LockWorkStation();
    [DllImport("Powrprof.dll")] private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    public static void Lock() => LockWorkStation();

    public static void Sleep() => SetSuspendState(false, false, false);

    public static void Restart()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/r /t 3") { CreateNoWindow = true, UseShellExecute = false });
    }

    public static void Shutdown()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/s /t 3") { CreateNoWindow = true, UseShellExecute = false });
    }

    // ── Media key simulation ────────────────────────────────
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void PressKey(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void MediaPlayPause() => PressKey(VK_MEDIA_PLAY_PAUSE);
    public static void MediaNext() => PressKey(VK_MEDIA_NEXT_TRACK);
    public static void MediaPrevious() => PressKey(VK_MEDIA_PREV_TRACK);
}

