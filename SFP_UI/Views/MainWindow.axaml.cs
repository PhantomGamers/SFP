using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;

using SFP.Models;

using SFP_UI.Models;

namespace SFP_UI.Views
{
    public partial class MainWindow : AppWindow
    {
        public static MainWindow? Instance { get; private set; }

        private readonly TrayIcon _trayIcon;

        private bool _isStarting = true;

        public MainWindow()
        {
            Instance = this;

            if (SFP.Properties.Settings.Default.StartMinimized)
            {
                WindowState = WindowState.Minimized;
            }

            InitializeComponent();

            _trayIcon = new TrayIcon
            {
                //Icon = (WindowIcon)Icon,
                ToolTipText = "Steam Friends Patcher"
            };

            // Workaround for https://github.com/AvaloniaUI/Avalonia/issues/7588
            var icons = new TrayIcons
            {
                _trayIcon
            };
            TrayIcon.SetIcons(Application.Current!, icons);

            InitializeTrayIcon();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (!_isStarting)
            {
                return;
            }

            _isStarting = false;

            if (SFP.Properties.Settings.Default.MinimizeToTray
                && SFP.Properties.Settings.Default is { StartMinimized: true, ShowTrayIcon: true })
            {
                Hide();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!SFP.Properties.Settings.Default.CloseToTray || !SFP.Properties.Settings.Default.ShowTrayIcon)
            {
                base.OnClosed(e);
                return;
            }

            Log.Logger.Debug("Closing to tray");
            Hide();
        }

        protected override void HandleWindowStateChanged(WindowState state)
        {
            if (state == WindowState.Minimized && SFP.Properties.Settings.Default.MinimizeToTray && SFP.Properties.Settings.Default.ShowTrayIcon)
            {
                Hide();
            }

            base.HandleWindowStateChanged(state);
        }

        private void InitializeTrayIcon()
        {
            _trayIcon.Clicked += (s, e) => ShowWindow();

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

            _trayIcon.Menu = new()
            {
                showButton,
                exitButton
            };

            _trayIcon.IsVisible = SFP.Properties.Settings.Default.ShowTrayIcon;
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        public static bool IsValidRequestedTheme(string thm) => FluentAvaloniaTheme.LightModeString.Equals(thm, StringComparison.OrdinalIgnoreCase) ||
                FluentAvaloniaTheme.DarkModeString.Equals(thm, StringComparison.OrdinalIgnoreCase) ||
                FluentAvaloniaTheme.HighContrastModeString.Equals(thm, StringComparison.OrdinalIgnoreCase);

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
