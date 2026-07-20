using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SoundPatcher.Audio;

namespace SoundPatcher;

public sealed partial class MainWindow : Window
{
    private static readonly Windows.UI.Color ButtonBgColor =
        Windows.UI.Color.FromArgb(255, 251, 251, 251);
    private static readonly Windows.UI.Color ButtonBorderColor =
        Windows.UI.Color.FromArgb(255, 229, 229, 229);
    private static readonly Windows.UI.Color TextColor =
        Windows.UI.Color.FromArgb(255, 26, 26, 26);

    private const double ItemHeight = 72;
    private const double ItemSpacing = 18;
    private const double IndicatorHeight = 36;

    private readonly AudioManager _audio = new();
    private readonly AppSettings _settings;
    private readonly AppWindow _appWindow;
    private readonly DispatcherTimer _pollTimer;
    private bool _editMode;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        ConfigureWindow();
        RestorePosition();

        RefreshList();

        Activated += (_, _) => RefreshList();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += (_, _) => RefreshList();
        _pollTimer.Start();

        _appWindow.Closing += OnWindowClosing;
    }

    private void ConfigureWindow()
    {
        Title = "Sound Patcher";

        SetupIcon();

        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = true;
        presenter.IsMinimizable = false;
        presenter.IsMaximizable = false;
        _appWindow.SetPresenter(presenter);

        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        SetTitleBar(TitleBarGrid);

        _appWindow.Resize(new Windows.Graphics.SizeInt32(_settings.WindowWidth, _settings.WindowHeight));
    }

    private void SetupIcon()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundSwitcher");
            Directory.CreateDirectory(dir);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            string icoPath = Path.Combine(dir, "app.ico");
            using (var stream = assembly.GetManifestResourceStream("SoundPatcher.app.ico"))
            using (var file = File.Create(icoPath))
            {
                stream?.CopyTo(file);
            }
            _appWindow.SetIcon(icoPath);

            string pngPath = Path.Combine(dir, "app.png");
            using (var stream = assembly.GetManifestResourceStream("SoundPatcher.icons8-audio-cable-96.png"))
            using (var file = File.Create(pngPath))
            {
                stream?.CopyTo(file);
            }
            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(pngPath));
            TitleBarIcon.Source = bitmap;
        }
        catch { }
    }

    private void RestorePosition()
    {
        if (_settings.WindowX < 0 || _settings.WindowY < 0) return;

        var area = DisplayArea.Primary.WorkArea;
        int x = Math.Clamp(_settings.WindowX, area.X, area.X + area.Width - 100);
        int y = Math.Clamp(_settings.WindowY, area.Y, area.Y + area.Height - 100);
        _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }

    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        _settings.WindowX = _appWindow.Position.X;
        _settings.WindowY = _appWindow.Position.Y;
        _settings.WindowWidth = _appWindow.Size.Width;
        _settings.WindowHeight = _appWindow.Size.Height;
        _settings.Save();
    }

    private void RefreshList()
    {
        List<AudioDeviceInfo> devices;
        try
        {
            devices = _audio.GetRenderDevices();
        }
        catch
        {
            return;
        }

        foreach (var d in devices)
        {
            d.IsHidden = _settings.HiddenDeviceIds.Contains(d.Id);
        }

        var visible = _editMode ? devices : devices.Where(d => !d.IsHidden).ToList();

        DevicePanel.Children.Clear();

        UpdateIndicatorClip(visible.Count);

        int activeIndex = -1;
        if (!_editMode)
        {
            for (int i = 0; i < visible.Count; i++)
            {
                if (visible[i].IsActive) { activeIndex = i; break; }
            }
        }
        MoveIndicator(activeIndex);

        if (visible.Count == 0)
        {
            DevicePanel.Children.Add(new TextBlock
            {
                Text = "没有可显示的设备\n点击右上角 ✎ 管理设备列表",
                FontSize = 15,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 150, 150, 150)),
                TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0),
                LineHeight = 26
            });
            return;
        }

        foreach (var device in visible)
        {
            DevicePanel.Children.Add(CreateDeviceButton(device));
        }
    }

    private static readonly Windows.UI.Color WindowBgColor =
        Windows.UI.Color.FromArgb(255, 243, 243, 243);

    private double? _indicatorY;

    private void UpdateIndicatorClip(int buttonCount)
    {
        for (int i = IndicatorLayer.Children.Count - 1; i >= 0; i--)
        {
            if (IndicatorLayer.Children[i] is Border b && b.Tag as string == "GapMask")
            {
                IndicatorLayer.Children.RemoveAt(i);
            }
        }

        for (int i = 0; i < buttonCount - 1; i++)
        {
            IndicatorLayer.Children.Add(new Border
            {
                Tag = "GapMask",
                Height = ItemSpacing,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, (i + 1) * ItemHeight + i * ItemSpacing, 0, 0),
                Background = new SolidColorBrush(WindowBgColor)
            });
        }
    }

    private void MoveIndicator(int activeIndex)
    {
        if (activeIndex < 0)
        {
            ActiveIndicator.Visibility = Visibility.Collapsed;
            _indicatorY = null;
            return;
        }

        double targetY = activeIndex * (ItemHeight + ItemSpacing) + (ItemHeight - IndicatorHeight) / 2;

        if (_indicatorY.HasValue && Math.Abs(_indicatorY.Value - targetY) < 0.5
            && ActiveIndicator.Visibility == Visibility.Visible)
        {
            return;
        }

        ActiveIndicator.Visibility = Visibility.Visible;

        if (!_indicatorY.HasValue)
        {
            IndicatorTransform.Y = targetY;
        }
        else
        {
            var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = targetY,
                Duration = new Duration(TimeSpan.FromMilliseconds(220)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase
                {
                    EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut
                }
            };
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            storyboard.Children.Add(animation);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, IndicatorTransform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Y");
            storyboard.Begin();
        }
        _indicatorY = targetY;
    }

    private Button CreateDeviceButton(AudioDeviceInfo device)
    {
        var label = new TextBlock
        {
            Text = device.Name,
            FontSize = 16,
            Foreground = new SolidColorBrush(TextColor),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
            Margin = new Thickness(16, 0, 0, 0)
        };

        var content = new Grid { Margin = new Thickness(16, 0, 20, 0) };
        content.Children.Add(label);

        var button = new Button
        {
            Style = (Style)RootGrid.Resources["DeviceButtonStyle"],
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(ButtonBorderColor),
            Background = new SolidColorBrush(ButtonBgColor),
            Content = content,
            Tag = device
        };

        if (_editMode && device.IsHidden)
        {
            button.Opacity = 0.35;
        }

        button.Click += OnDeviceButtonClick;
        return button;
    }

    private void OnDeviceButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AudioDeviceInfo device }) return;

        if (_editMode)
        {
            if (device.IsHidden)
            {
                _settings.HiddenDeviceIds.Remove(device.Id);
            }
            else
            {
                _settings.HiddenDeviceIds.Add(device.Id);
            }
            _settings.Save();
            RefreshList();
            return;
        }

        try
        {
            _audio.SetDefault(device.Id);
        }
        catch { }
        RefreshList();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        _editMode = !_editMode;
        EditHint.Visibility = _editMode ? Visibility.Visible : Visibility.Collapsed;
        EditButton.Background = _editMode
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240))
            : new SolidColorBrush(Colors.Transparent);
        RefreshList();
    }
}
