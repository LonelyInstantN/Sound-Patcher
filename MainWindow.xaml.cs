using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SoundSwitcher.Audio;

namespace SoundSwitcher;

public sealed partial class MainWindow : Window
{
    private static readonly Windows.UI.Color AccentColor =
        Windows.UI.Color.FromArgb(255, 81, 43, 212);

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
        Title = "SoundSwitcher";

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

    private Button CreateDeviceButton(AudioDeviceInfo device)
    {
        var accent = new SolidColorBrush(AccentColor);
        var white = new SolidColorBrush(Colors.White);
        bool active = device.IsActive && !_editMode;

        var ring = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            BorderThickness = new Thickness(3),
            BorderBrush = active ? white : accent,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        if (active)
        {
            ring.Child = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = white
            };
        }

        var label = new TextBlock
        {
            Text = device.Name,
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = active ? white : accent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis
        };

        var content = new Grid { Margin = new Thickness(26, 0, 26, 0) };
        content.Children.Add(ring);
        content.Children.Add(label);

        var button = new Button
        {
            Style = (Style)RootGrid.Resources["DeviceButtonStyle"],
            Height = 84,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(active ? 0 : 3),
            BorderBrush = accent,
            Background = active ? accent : white,
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
