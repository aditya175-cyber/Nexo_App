using System.Diagnostics;
using System.IO;

namespace WinIsland.UI.Services;

/// <summary>
/// Manages the Python backend process lifecycle so the app can be launched
/// by double-clicking the .exe — no PowerShell/CMD needed.
/// </summary>
public sealed class BackendService : IDisposable
{
    private Process? _process;
    private readonly string _backendDir;
    private readonly string _venvPython;

    public BackendService()
    {
        // Backend lives relative to the exe: ../../src/WinIsland.Backend
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;

        // Try dev layout first (running from bin/Debug/...)
        _backendDir = Path.GetFullPath(Path.Combine(exeDir, @"..\..\..\..\WinIsland.Backend"));
        if (!Directory.Exists(_backendDir))
        {
            // Fallback: sibling folder
            _backendDir = Path.GetFullPath(Path.Combine(exeDir, @"..\WinIsland.Backend"));
        }

        _venvPython = Path.Combine(_backendDir, @".venv\Scripts\python.exe");
    }

    public bool CanStart => File.Exists(_venvPython) && File.Exists(Path.Combine(_backendDir, "main.py"));

    public void Start()
    {
        if (_process is { HasExited: false }) return;
        if (!CanStart) return;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _venvPython,
                Arguments = "main.py",
                WorkingDirectory = _backendDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true
        };

        try { _process.Start(); }
        catch { /* Python may not be available */ }
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(2000);
            }
            _process?.Dispose();
        }
        catch { }
    }
}
