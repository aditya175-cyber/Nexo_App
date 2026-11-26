namespace WinIsland.UI.Models;

public class ClipboardItem : ViewModels.BaseViewModel
{
    public string Content { get; set; } = "";
    public DateTime CopiedAt { get; set; } = DateTime.Now;

    private bool _isPinned;
    public bool IsPinned
    {
        get => _isPinned;
        set { SetProperty(ref _isPinned, value); OnPropertyChanged(nameof(PinIcon)); }
    }

    public string Preview
    {
        get
        {
            var clean = Content.ReplaceLineEndings(" ");
            return clean.Length > 55 ? clean[..55] + "…" : clean;
        }
    }

    public string TimeText => CopiedAt.ToString("HH:mm");
    public string PinIcon => IsPinned ? "\uE840" : "\uE718";
}

public class CalendarDay
{
    public int Day { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
}
