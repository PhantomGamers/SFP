using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace SFP_UI.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        public readonly TrayIcon trayIcon;

        private bool _isStarting = true;

        public MainWindow()
        {
            Instance = this;
            Opened += MainWindow_Opened;

            if (SFP.Properties.Settings.Default.StartMinimized)
            {
                WindowState = WindowState.Minimized;
            }

            InitializeComponent();

            trayIcon = new()
            {
                Icon = Icon,
                ToolTipText = "Steam Friends Patcher"
            };

            // Workaround for https://github.com/AvaloniaUI/Avalonia/issues/7588
            var icons = new TrayIcons
            {
                trayIcon
            };
            TrayIcon.SetIcons(Application.Current!, icons);

            InitializeTrayIcon();
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            if (_isStarting
               && SFP.Properties.Settings.Default.MinimizeToTray && SFP.Properties.Settings.Default.StartMinimized
               && SFP.Properties.Settings.Default.ShowTrayIcon)
            {
                _isStarting = false;
                Hide();
            }
        }

        protected override void HandleWindowStateChanged(WindowState state)
        {
            if (state == WindowState.Minimized && SFP.Properties.Settings.Default.MinimizeToTray && SFP.Properties.Settings.Default.ShowTrayIcon)
            {
                Hide();
            }

            base.HandleWindowStateChanged(state);
        }

        public void InitializeTrayIcon()
        {
            trayIcon.Clicked += (s, e) => ShowWindow();

            var showButton = new NativeMenuItem("Show Window");
            showButton.Click += (s, e) => ShowWindow();

            var exitButton = new NativeMenuItem("Exit");
            exitButton.Click += (s, e) =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            };

            trayIcon.Menu = new()
            {
                showButton,
                exitButton
            };

            trayIcon.IsVisible = SFP.Properties.Settings.Default.ShowTrayIcon;
        }

        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
