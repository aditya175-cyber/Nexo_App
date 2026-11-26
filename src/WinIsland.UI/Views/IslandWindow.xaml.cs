using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinIsland.UI.ViewModels;

namespace WinIsland.UI.Views;

public partial class IslandWindow : Window
{
    private readonly IslandViewModel _viewModel;
    private bool _isExpanded;
    private bool _isAnimating;
    private IntPtr _hwnd;

    private const double CompactWidth = 240;
    private const double CompactHeight = 42;
    private const double ExpandedWidth = 460;
    private const double ExpandedHeight = 470;
    private static readonly TimeSpan AnimDuration = TimeSpan.FromMilliseconds(350);

    [DllImport("user32.dll")] private static extern bool AddClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    public IslandWindow()
    {
        InitializeComponent();
        _viewModel = new IslandViewModel();
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow(CompactWidth);

        // Register for clipboard notifications
        _hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);
        AddClipboardFormatListener(_hwnd);

        await _viewModel.InitializeAsync();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_CLIPBOARDUPDATE = 0x031D;
        if (msg == WM_CLIPBOARDUPDATE)
        {
            _viewModel.OnClipboardChanged();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void PositionWindow(double width)
    {
        var screen = SystemParameters.WorkArea;
        Left = (screen.Width - width) / 2;
        Top = 8;
    }

    // ── Expand / Collapse ───────────────────────────────────
    private void OnIslandClick(object sender, MouseButtonEventArgs e)
    {
        if (_isAnimating) return;
        if (!_isExpanded)
            ToggleExpand();
    }

    private void ToggleExpand()
    {
        _isExpanded = !_isExpanded;
        _isAnimating = true;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        double targetW = _isExpanded ? ExpandedWidth : CompactWidth;
        double targetH = _isExpanded ? ExpandedHeight : CompactHeight;
        double targetR = _isExpanded ? 10 : 10;

        var wAnim = new DoubleAnimation(targetW, AnimDuration) { EasingFunction = easing };
        var hAnim = new DoubleAnimation(targetH, AnimDuration) { EasingFunction = easing };

        double screenW = SystemParameters.WorkArea.Width;
        var leftAnim = new DoubleAnimation((screenW - targetW) / 2, AnimDuration) { EasingFunction = easing };

        var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140)) { EasingFunction = easing };
        var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = easing,
            BeginTime = TimeSpan.FromMilliseconds(150)
        };

        if (_isExpanded)
        {
            CompactView.BeginAnimation(OpacityProperty, fadeOut);
            ExpandedView.BeginAnimation(OpacityProperty, fadeIn);
            ExpandedView.IsHitTestVisible = true;
        }
        else
        {
            ExpandedView.BeginAnimation(OpacityProperty, fadeOut);
            CompactView.BeginAnimation(OpacityProperty, fadeIn);
            ExpandedView.IsHitTestVisible = false;
        }

        IslandBorder.CornerRadius = new CornerRadius(targetR);

        hAnim.Completed += (_, _) => _isAnimating = false;
        BeginAnimation(WidthProperty, wAnim);
        BeginAnimation(HeightProperty, hAnim);
        BeginAnimation(LeftProperty, leftAnim);
    }

    // ── Hover glow (red accent) ─────────────────────────────
    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (_isExpanded) return;
        var brush = new SolidColorBrush();
        IslandBorder.BorderBrush = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(Color.FromArgb(0x44, 0xED, 0x1C, 0x24),
                TimeSpan.FromMilliseconds(200)));
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_isExpanded) return;
        IslandBorder.BorderBrush?.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
                TimeSpan.FromMilliseconds(300)));
    }

    // ── Collapse on deactivate ──────────────────────────────
    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_isExpanded && !_isAnimating)
        {
            _viewModel.NavigateToHome();
            ToggleExpand();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        RemoveClipboardFormatListener(_hwnd);
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
