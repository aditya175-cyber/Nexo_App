using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using WinIsland.UI.Models;
using WinIsland.UI.Services;

namespace WinIsland.UI.ViewModels;

public class IslandViewModel : BaseViewModel, IDisposable
{
    // ── Services & timers ───────────────────────────────────
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _timerTick;
    private readonly DispatcherTimer _stopwatchTick;
    private readonly DispatcherTimer _noteSaveTimer;
    private readonly WebSocketClient _wsClient;
    private readonly AudioService _audioService;
    private readonly MediaService _mediaService;
    private readonly Stopwatch _stopwatch = new();

    private TimeSpan _timerRemaining;
    private TimeSpan _timerTotal = TimeSpan.FromMinutes(25);
    private bool _isVolumeFromPoll;
    private int _pollCounter;

    // ╔════════════════════════════════════════════════════════╗
    // ║                   NAVIGATION                          ║
    // ╚════════════════════════════════════════════════════════╝
    private string _currentPage = "home";
    public string CurrentPage
    {
        get => _currentPage;
        set { if (SetProperty(ref _currentPage, value)) { OnPropertyChanged(nameof(IsOnHomePage)); OnPropertyChanged(nameof(PageTitle)); } }
    }
    public bool IsOnHomePage => CurrentPage == "home";
    public string PageTitle => CurrentPage switch
    {
        "music" => "Music",
        "timer" => "Timer",
        "controls" => "Controls",
        "clipboard" => "Clipboard",
        "notes" => "Notes",
        "texttool" => "Text Tool",
        "calendar" => "Calendar",
        _ => ""
    };

    public RelayCommand NavigateCommand { get; }
    public RelayCommand NavigateHomeCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   CLOCK                               ║
    // ╚════════════════════════════════════════════════════════╝
    private string _currentTime = "--:--";
    private string _currentDate = "";
    public string CurrentTime { get => _currentTime; set => SetProperty(ref _currentTime, value); }
    public string CurrentDate { get => _currentDate; set => SetProperty(ref _currentDate, value); }

    // ╔════════════════════════════════════════════════════════╗
    // ║                SYSTEM MONITOR                         ║
    // ╚════════════════════════════════════════════════════════╝
    private double _cpuUsage, _ramUsage, _ramUsedGb, _ramTotalGb;
    private bool _isConnected;

    public double CpuUsage
    {
        get => _cpuUsage;
        set { if (SetProperty(ref _cpuUsage, value)) { OnPropertyChanged(nameof(CpuUsageText)); OnPropertyChanged(nameof(CpuDecimal)); OnPropertyChanged(nameof(CompactStatus)); } }
    }
    public double RamUsage
    {
        get => _ramUsage;
        set { if (SetProperty(ref _ramUsage, value)) { OnPropertyChanged(nameof(RamUsageText)); OnPropertyChanged(nameof(RamDecimal)); } }
    }
    public double RamUsedGb { get => _ramUsedGb; set => SetProperty(ref _ramUsedGb, value); }
    public double RamTotalGb { get => _ramTotalGb; set => SetProperty(ref _ramTotalGb, value); }
    public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }

    public string CpuUsageText => IsConnected ? $"{CpuUsage:F0}%" : "···";
    public string RamUsageText => IsConnected ? $"{RamUsage:F0}%" : "···";
    public double CpuDecimal => Math.Clamp(CpuUsage / 100.0, 0, 1);
    public double RamDecimal => Math.Clamp(RamUsage / 100.0, 0, 1);

    // ── Compact status (context-aware) ──────────────────────
    public string CompactStatus
    {
        get
        {
            if (_isTimerRunning) return $"⏱ {TimerDisplay}";
            if (_hasActiveMedia && _isMediaPlaying) return $"♪ {_mediaArtist}";
            return CpuUsageText;
        }
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   MEDIA                               ║
    // ╚════════════════════════════════════════════════════════╝
    private string _mediaTitle = "Not Playing";
    private string _mediaArtist = "";
    private bool _isMediaPlaying;
    private bool _hasActiveMedia;

    public string MediaTitle
    {
        get => _mediaTitle;
        set { if (SetProperty(ref _mediaTitle, value)) OnPropertyChanged(nameof(CompactStatus)); }
    }
    public string MediaArtist
    {
        get => _mediaArtist;
        set { if (SetProperty(ref _mediaArtist, value)) OnPropertyChanged(nameof(CompactStatus)); }
    }
    public bool IsMediaPlaying
    {
        get => _isMediaPlaying;
        set { if (SetProperty(ref _isMediaPlaying, value)) { OnPropertyChanged(nameof(PlayPauseIcon)); OnPropertyChanged(nameof(CompactStatus)); } }
    }
    public bool HasActiveMedia { get => _hasActiveMedia; set => SetProperty(ref _hasActiveMedia, value); }

    // Pause ⏸ : Play ▶
    public string PlayPauseIcon => IsMediaPlaying ? "\uE769" : "\uE768";

    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand NextTrackCommand { get; }
    public RelayCommand PrevTrackCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   TIMER                               ║
    // ╚════════════════════════════════════════════════════════╝
    private string _timerDisplay = "25:00";
    private bool _isTimerRunning;

    public string TimerDisplay { get => _timerDisplay; set => SetProperty(ref _timerDisplay, value); }
    public bool IsTimerRunning
    {
        get => _isTimerRunning;
        set { if (SetProperty(ref _isTimerRunning, value)) { OnPropertyChanged(nameof(TimerButtonText)); OnPropertyChanged(nameof(CompactStatus)); } }
    }
    public string TimerButtonText => IsTimerRunning ? "Pause" : "Start";

    public RelayCommand ToggleTimerCommand { get; }
    public RelayCommand ResetTimerCommand { get; }
    public RelayCommand SetTimerPresetCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                 STOPWATCH                             ║
    // ╚════════════════════════════════════════════════════════╝
    private string _stopwatchDisplay = "00:00.00";
    private bool _isStopwatchRunning;

    public string StopwatchDisplay { get => _stopwatchDisplay; set => SetProperty(ref _stopwatchDisplay, value); }
    public bool IsStopwatchRunning
    {
        get => _isStopwatchRunning;
        set { if (SetProperty(ref _isStopwatchRunning, value)) OnPropertyChanged(nameof(StopwatchButtonText)); }
    }
    public string StopwatchButtonText => IsStopwatchRunning ? "Stop" : "Start";

    public RelayCommand ToggleStopwatchCommand { get; }
    public RelayCommand ResetStopwatchCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   VOLUME                              ║
    // ╚════════════════════════════════════════════════════════╝
    private double _volume;
    private bool _isMuted;

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                OnPropertyChanged(nameof(VolumeText));
                OnPropertyChanged(nameof(VolumeIcon));
                if (!_isVolumeFromPoll && _audioService.IsAvailable)
                    _audioService.SetVolume((float)(value / 100.0));
            }
        }
    }
    public bool IsMuted
    {
        get => _isMuted;
        set { if (SetProperty(ref _isMuted, value)) { OnPropertyChanged(nameof(VolumeIcon)); OnPropertyChanged(nameof(MuteButtonText)); OnPropertyChanged(nameof(MuteButtonIcon)); } }
    }

    public string VolumeText => $"{Volume:F0}%";
    public string VolumeIcon => IsMuted || Volume < 1 ? "\uE74F" : Volume < 33 ? "\uE993" : Volume < 66 ? "\uE994" : "\uE995";
    public string MuteButtonText => IsMuted ? "Unmute" : "Mute";
    public string MuteButtonIcon => IsMuted ? "\uE74F" : "\uE767";

    public RelayCommand ToggleMuteCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   POWER                               ║
    // ╚════════════════════════════════════════════════════════╝
    public RelayCommand LockCommand { get; }
    public RelayCommand SleepCommand { get; }
    public RelayCommand RestartCommand { get; }
    public RelayCommand ShutdownCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                 CLIPBOARD (Phase 3)                   ║
    // ╚════════════════════════════════════════════════════════╝
    private bool _isSettingClipboard;
    public ObservableCollection<ClipboardItem> ClipboardHistory { get; } = new();

    public RelayCommand PasteClipboardItemCommand { get; }
    public RelayCommand TogglePinCommand { get; }
    public RelayCommand ClearClipboardCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   NOTES (Phase 3)                     ║
    // ╚════════════════════════════════════════════════════════╝
    private string _noteText = "";
    public string NoteText
    {
        get => _noteText;
        set
        {
            if (SetProperty(ref _noteText, value))
            {
                _noteSaveTimer.Stop();
                _noteSaveTimer.Start();
            }
        }
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                TEXT TOOL (Phase 3)                    ║
    // ╚════════════════════════════════════════════════════════╝
    private string _textInput = "";
    private string _textResult = "";

    public string TextInput { get => _textInput; set => SetProperty(ref _textInput, value); }
    public string TextResult { get => _textResult; set => SetProperty(ref _textResult, value); }

    public RelayCommand TextUpperCommand { get; }
    public RelayCommand TextLowerCommand { get; }
    public RelayCommand TextTitleCommand { get; }
    public RelayCommand TextCountCommand { get; }
    public RelayCommand TextJsonCommand { get; }
    public RelayCommand TextBase64EncCommand { get; }
    public RelayCommand TextBase64DecCommand { get; }
    public RelayCommand TextCopyResultCommand { get; }
    public RelayCommand TextFromClipboardCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                 CALENDAR (Phase 3)                    ║
    // ╚════════════════════════════════════════════════════════╝
    private DateTime _calendarMonth;
    private string _calendarTitle = "";

    public ObservableCollection<CalendarDay> CalendarDays { get; } = new();
    public string CalendarTitle { get => _calendarTitle; set => SetProperty(ref _calendarTitle, value); }

    public RelayCommand CalendarPrevCommand { get; }
    public RelayCommand CalendarNextCommand { get; }

    // ╔════════════════════════════════════════════════════════╗
    // ║                 CONSTRUCTOR                           ║
    // ╚════════════════════════════════════════════════════════╝
    public IslandViewModel()
    {
        // --- Services ---
        _wsClient = new WebSocketClient();
        _wsClient.OnSystemDataReceived += OnSystemData;
        _wsClient.OnConnectionChanged += c =>
            Application.Current.Dispatcher.BeginInvoke(() => IsConnected = c);

        _audioService = new AudioService();

        _mediaService = new MediaService();
        _mediaService.OnMediaChanged += OnMediaChanged;

        // --- Timers ---
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _clockTimer.Tick += (_, _) => OnClockTick();

        _timerTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timerTick.Tick += (_, _) => OnTimerTick();

        _stopwatchTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _stopwatchTick.Tick += (_, _) =>
            StopwatchDisplay = _stopwatch.Elapsed.ToString(@"mm\:ss\.ff");

        _noteSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _noteSaveTimer.Tick += (_, _) => { _noteSaveTimer.Stop(); SaveNotes(); };

        // --- Initial state ---
        _timerRemaining = _timerTotal;
        UpdateTimerDisplay();

        if (_audioService.IsAvailable)
        {
            _volume = Math.Round(_audioService.GetVolume() * 100);
            _isMuted = _audioService.GetMute();
        }

        // Calendar init
        _calendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        BuildCalendar();

        // --- Commands (Phase 1-2) ---
        NavigateCommand = new RelayCommand(p => CurrentPage = p?.ToString() ?? "home");
        NavigateHomeCommand = new RelayCommand(() => CurrentPage = "home");

        PlayPauseCommand = new RelayCommand(() => { _ = SafeMedia(_mediaService.PlayPauseAsync, SystemService.MediaPlayPause); });
        NextTrackCommand = new RelayCommand(() => { _ = SafeMedia(_mediaService.NextAsync, SystemService.MediaNext); });
        PrevTrackCommand = new RelayCommand(() => { _ = SafeMedia(_mediaService.PreviousAsync, SystemService.MediaPrevious); });

        ToggleTimerCommand = new RelayCommand(ToggleTimer);
        ResetTimerCommand = new RelayCommand(ResetTimer);
        SetTimerPresetCommand = new RelayCommand(p =>
        {
            if (int.TryParse(p?.ToString(), out int m)) SetTimerPreset(m);
        });

        ToggleStopwatchCommand = new RelayCommand(ToggleStopwatch);
        ResetStopwatchCommand = new RelayCommand(ResetStopwatch);

        ToggleMuteCommand = new RelayCommand(() =>
        {
            IsMuted = !IsMuted;
            if (_audioService.IsAvailable) _audioService.SetMute(IsMuted);
        });

        LockCommand = new RelayCommand(SystemService.Lock);
        SleepCommand = new RelayCommand(SystemService.Sleep);
        RestartCommand = new RelayCommand(SystemService.Restart);
        ShutdownCommand = new RelayCommand(SystemService.Shutdown);

        // --- Commands (Phase 3 — Clipboard) ---
        PasteClipboardItemCommand = new RelayCommand(p =>
        {
            if (p is ClipboardItem item)
            {
                _isSettingClipboard = true;
                try { Clipboard.SetText(item.Content); } catch { }
                _isSettingClipboard = false;
            }
        });

        TogglePinCommand = new RelayCommand(p =>
        {
            if (p is ClipboardItem item) item.IsPinned = !item.IsPinned;
        });

        ClearClipboardCommand = new RelayCommand(() =>
        {
            var pinned = ClipboardHistory.Where(i => i.IsPinned).ToList();
            ClipboardHistory.Clear();
            foreach (var p in pinned) ClipboardHistory.Add(p);
        });

        // --- Commands (Phase 3 — Text Tool) ---
        TextUpperCommand = new RelayCommand(() => TextResult = TextInput.ToUpperInvariant());
        TextLowerCommand = new RelayCommand(() => TextResult = TextInput.ToLowerInvariant());
        TextTitleCommand = new RelayCommand(() => TextResult = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(TextInput.ToLower()));
        TextCountCommand = new RelayCommand(() =>
        {
            var text = TextInput;
            var words = string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            var chars = text.Length;
            var lines = text.Split('\n').Length;
            TextResult = $"Words: {words}\nCharacters: {chars}\nLines: {lines}";
        });
        TextJsonCommand = new RelayCommand(() =>
        {
            try
            {
                using var doc = JsonDocument.Parse(TextInput);
                TextResult = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex) { TextResult = $"Invalid JSON: {ex.Message}"; }
        });
        TextBase64EncCommand = new RelayCommand(() =>
        {
            try { TextResult = Convert.ToBase64String(Encoding.UTF8.GetBytes(TextInput)); }
            catch (Exception ex) { TextResult = $"Error: {ex.Message}"; }
        });
        TextBase64DecCommand = new RelayCommand(() =>
        {
            try { TextResult = Encoding.UTF8.GetString(Convert.FromBase64String(TextInput.Trim())); }
            catch (Exception ex) { TextResult = $"Invalid Base64: {ex.Message}"; }
        });
        TextCopyResultCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(TextResult))
            {
                _isSettingClipboard = true;
                try { Clipboard.SetText(TextResult); } catch { }
                _isSettingClipboard = false;
            }
        });
        TextFromClipboardCommand = new RelayCommand(() =>
        {
            try { if (Clipboard.ContainsText()) TextInput = Clipboard.GetText(); } catch { }
        });

        // --- Commands (Phase 3 — Calendar) ---
        CalendarPrevCommand = new RelayCommand(() =>
        {
            _calendarMonth = _calendarMonth.AddMonths(-1);
            BuildCalendar();
        });
        CalendarNextCommand = new RelayCommand(() =>
        {
            _calendarMonth = _calendarMonth.AddMonths(1);
            BuildCalendar();
        });
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                 INITIALIZATION                        ║
    // ╚════════════════════════════════════════════════════════╝
    public async Task InitializeAsync()
    {
        UpdateClock();
        _clockTimer.Start();

        LoadNotes();

        _ = Task.Run(() => _wsClient.ConnectAsync());

        try { await _mediaService.InitializeAsync(); }
        catch { /* SMTC unavailable — media buttons will use keyboard fallback */ }
    }

    public void NavigateToHome() => CurrentPage = "home";

    // ╔════════════════════════════════════════════════════════╗
    // ║               CLOCK & POLLING                         ║
    // ╚════════════════════════════════════════════════════════╝
    private void OnClockTick()
    {
        UpdateClock();
        if (_audioService.IsAvailable && ++_pollCounter % 4 == 0)
            PollVolume();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm");
        CurrentDate = now.ToString("dddd, MMMM d");
    }

    private void PollVolume()
    {
        try
        {
            var vol = Math.Round(_audioService.GetVolume() * 100);
            if (Math.Abs(vol - Volume) > 0.5)
            {
                _isVolumeFromPoll = true;
                Volume = vol;
                _isVolumeFromPoll = false;
            }
            var muted = _audioService.GetMute();
            if (muted != IsMuted) IsMuted = muted;
        }
        catch { /* audio device may change */ }
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║             SYSTEM DATA (from Python)                 ║
    // ╚════════════════════════════════════════════════════════╝
    private void OnSystemData(SystemData data)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            CpuUsage = data.Cpu;
            RamUsage = data.Ram;
            RamUsedGb = data.RamUsed;
            RamTotalGb = data.RamTotal;
        });
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                    MEDIA                              ║
    // ╚════════════════════════════════════════════════════════╝
    private void OnMediaChanged(MediaInfo info)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            HasActiveMedia = !string.IsNullOrEmpty(info.Title);
            MediaTitle = HasActiveMedia ? info.Title : "Not Playing";
            MediaArtist = info.Artist;
            IsMediaPlaying = info.State == PlaybackState.Playing;
        });
    }

    private static async Task SafeMedia(Func<Task> primary, Action fallback)
    {
        try { await primary(); }
        catch { try { fallback(); } catch { } }
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                    TIMER                              ║
    // ╚════════════════════════════════════════════════════════╝
    private void ToggleTimer()
    {
        if (IsTimerRunning)
        {
            _timerTick.Stop();
            IsTimerRunning = false;
        }
        else
        {
            if (_timerRemaining <= TimeSpan.Zero)
            {
                _timerRemaining = _timerTotal;
                UpdateTimerDisplay();
            }
            _timerTick.Start();
            IsTimerRunning = true;
        }
    }

    private void ResetTimer()
    {
        _timerTick.Stop();
        IsTimerRunning = false;
        _timerRemaining = _timerTotal;
        UpdateTimerDisplay();
    }

    private void SetTimerPreset(int minutes)
    {
        _timerTick.Stop();
        IsTimerRunning = false;
        _timerTotal = TimeSpan.FromMinutes(minutes);
        _timerRemaining = _timerTotal;
        UpdateTimerDisplay();
    }

    private void OnTimerTick()
    {
        _timerRemaining -= TimeSpan.FromMilliseconds(100);
        if (_timerRemaining <= TimeSpan.Zero)
        {
            _timerRemaining = TimeSpan.Zero;
            _timerTick.Stop();
            IsTimerRunning = false;
        }
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        TimerDisplay = _timerRemaining.ToString(@"mm\:ss");
        OnPropertyChanged(nameof(CompactStatus));
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                  STOPWATCH                            ║
    // ╚════════════════════════════════════════════════════════╝
    private void ToggleStopwatch()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            _stopwatchTick.Stop();
            IsStopwatchRunning = false;
        }
        else
        {
            _stopwatch.Start();
            _stopwatchTick.Start();
            IsStopwatchRunning = true;
        }
    }

    private void ResetStopwatch()
    {
        _stopwatch.Reset();
        _stopwatchTick.Stop();
        IsStopwatchRunning = false;
        StopwatchDisplay = "00:00.00";
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║               CLIPBOARD (Phase 3)                     ║
    // ╚════════════════════════════════════════════════════════╝
    public void OnClipboardChanged()
    {
        if (_isSettingClipboard) return;
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Dedup: skip if same as most recent non-pinned entry
            var recent = ClipboardHistory.FirstOrDefault(i => !i.IsPinned);
            if (recent != null && recent.Content == text) return;

            var item = new ClipboardItem { Content = text, CopiedAt = DateTime.Now };
            ClipboardHistory.Insert(0, item);

            // Keep max 50 unpinned
            var unpinnedCount = ClipboardHistory.Count(i => !i.IsPinned);
            while (unpinnedCount > 50)
            {
                var last = ClipboardHistory.LastOrDefault(i => !i.IsPinned);
                if (last != null) { ClipboardHistory.Remove(last); unpinnedCount--; }
                else break;
            }
        }
        catch { /* clipboard may be locked by another app */ }
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                  NOTES (Phase 3)                      ║
    // ╚════════════════════════════════════════════════════════╝
    private static string NotesFilePath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nexo");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "notes.txt");
        }
    }

    private void LoadNotes()
    {
        try
        {
            if (File.Exists(NotesFilePath))
            {
                _noteText = File.ReadAllText(NotesFilePath);
                OnPropertyChanged(nameof(NoteText));
            }
        }
        catch { /* first run or permission issue */ }
    }

    private void SaveNotes()
    {
        try { File.WriteAllText(NotesFilePath, NoteText); }
        catch { /* permission issue */ }
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                CALENDAR (Phase 3)                     ║
    // ╚════════════════════════════════════════════════════════╝
    private void BuildCalendar()
    {
        CalendarTitle = _calendarMonth.ToString("MMMM yyyy");
        CalendarDays.Clear();

        var firstDay = _calendarMonth;
        // Monday = 0, Sunday = 6
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;
        var daysInMonth = DateTime.DaysInMonth(_calendarMonth.Year, _calendarMonth.Month);
        var today = DateTime.Today;

        // Previous month fill
        var prevMonth = _calendarMonth.AddMonths(-1);
        var daysInPrev = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        for (int i = startOffset - 1; i >= 0; i--)
            CalendarDays.Add(new CalendarDay { Day = daysInPrev - i, IsCurrentMonth = false, IsToday = false });

        // Current month
        for (int d = 1; d <= daysInMonth; d++)
        {
            bool isToday = _calendarMonth.Year == today.Year && _calendarMonth.Month == today.Month && d == today.Day;
            CalendarDays.Add(new CalendarDay { Day = d, IsCurrentMonth = true, IsToday = isToday });
        }

        // Next month fill to reach 42 cells (6 rows × 7)
        int remaining = 42 - CalendarDays.Count;
        for (int d = 1; d <= remaining; d++)
            CalendarDays.Add(new CalendarDay { Day = d, IsCurrentMonth = false, IsToday = false });
    }

    // ╔════════════════════════════════════════════════════════╗
    // ║                   CLEANUP                             ║
    // ╚════════════════════════════════════════════════════════╝
    public void Dispose()
    {
        _clockTimer.Stop();
        _timerTick.Stop();
        _stopwatchTick.Stop();
        _noteSaveTimer.Stop();
        SaveNotes();
        _wsClient.Dispose();
        _audioService.Dispose();
        _mediaService.Dispose();
    }
}
