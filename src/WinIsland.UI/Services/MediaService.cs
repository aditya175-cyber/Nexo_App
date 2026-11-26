using Windows.Media.Control;

namespace WinIsland.UI.Services;

public enum PlaybackState { None, Playing, Paused, Stopped }

public class MediaInfo
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public PlaybackState State { get; set; } = PlaybackState.None;
}

/// <summary>
/// Wraps Windows SMTC (System Media Transport Controls) for detecting
/// currently playing media and controlling playback.
/// </summary>
public class MediaService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;

    public event Action<MediaInfo>? OnMediaChanged;

    public async Task<bool> InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            AttachSession(_manager.GetCurrentSession());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args)
    {
        AttachSession(sender.GetCurrentSession());
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession? session)
    {
        // Detach old
        if (_session != null)
        {
            _session.MediaPropertiesChanged -= OnPropsChanged;
            _session.PlaybackInfoChanged -= OnPlaybackChanged;
        }

        _session = session;

        if (_session != null)
        {
            _session.MediaPropertiesChanged += OnPropsChanged;
            _session.PlaybackInfoChanged += OnPlaybackChanged;
            _ = PushUpdate();
        }
        else
        {
            OnMediaChanged?.Invoke(new MediaInfo());
        }
    }

    private async void OnPropsChanged(GlobalSystemMediaTransportControlsSession s, MediaPropertiesChangedEventArgs a)
        => await PushUpdate();

    private async void OnPlaybackChanged(GlobalSystemMediaTransportControlsSession s, PlaybackInfoChangedEventArgs a)
        => await PushUpdate();

    private async Task PushUpdate()
    {
        if (_session == null) return;
        try
        {
            var props = await _session.TryGetMediaPropertiesAsync();
            var pb = _session.GetPlaybackInfo();

            var state = pb.PlaybackStatus switch
            {
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlaybackState.Playing,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlaybackState.Paused,
                _ => PlaybackState.Stopped,
            };

            OnMediaChanged?.Invoke(new MediaInfo
            {
                Title = props.Title ?? "",
                Artist = props.Artist ?? "",
                Album = props.AlbumTitle ?? "",
                State = state,
            });
        }
        catch { }
    }

    public async Task PlayPauseAsync()
    {
        if (_session != null) await _session.TryTogglePlayPauseAsync();
    }

    public async Task NextAsync()
    {
        if (_session != null) await _session.TrySkipNextAsync();
    }

    public async Task PreviousAsync()
    {
        if (_session != null) await _session.TrySkipPreviousAsync();
    }

    public void Dispose()
    {
        if (_session != null)
        {
            _session.MediaPropertiesChanged -= OnPropsChanged;
            _session.PlaybackInfoChanged -= OnPlaybackChanged;
        }
    }
}

