using System.ComponentModel;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media;

using SFP.Models;

using SFP_UI.Models;

namespace SFP_UI.Views
{
    public partial class MainWindow : CoreWindow
    {
        public static MainWindow? Instance { get; private set; }

        public readonly TrayIcon trayIcon;

        public FluentAvaloniaTheme? Theme;

        private bool _isStarting = true;

        public MainWindow()
        {
            Instance = this;

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

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (_isStarting)
            {
                _isStarting = false;

                if (SFP.Properties.Settings.Default.MinimizeToTray
                    && SFP.Properties.Settings.Default.StartMinimized
                    && SFP.Properties.Settings.Default.ShowTrayIcon)
                {
                    Hide();
                }

                Theme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
                if (Theme != null)
                {
                    Theme.RequestedThemeChanged -= OnRequestedThemeChanged;
                    Theme.RequestedThemeChanged += OnRequestedThemeChanged;

                    Theme.RequestedTheme = FluentAvaloniaTheme.DarkModeString; // Default to dark mode
                    if (IsValidRequestedTheme(SFP.Properties.Settings.Default.AppTheme))
                    {
                        Theme.RequestedTheme = SFP.Properties.Settings.Default.AppTheme;
                    }
                    else
                    {
                        Theme.InvalidateThemingFromSystemThemeChanged();
                    }

                    // Enable Mica on Windows 11
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // TODO: add Windows version to CoreWindow
                        if (IsWindows11 && Theme.RequestedTheme != FluentAvaloniaTheme.HighContrastModeString)
                        {
                            TransparencyBackgroundFallback = Brushes.Transparent;
                            TransparencyLevelHint = WindowTransparencyLevel.Mica;

                            TryEnableMicaEffect(Theme);
                        }
                        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
                        {
                            if (SFP.Properties.Settings.Default.AppTheme != "System Default")
                            {
                                return;
                            }

                            try
                            {
                                Theme.InvalidateThemingFromSystemThemeChanged();
                            }
                            catch (Exception err)
                            {
                                LogModel.Logger.Warn("Unable to detect system theme");
                                LogModel.Logger.Error(err);
                            }
                        };
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Models.ThemeChangeDetection.Linux.WatchForChanges();
                    }

                    Theme.ForceWin32WindowToTheme(this);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!SFP.Properties.Settings.Default.CloseToTray || !SFP.Properties.Settings.Default.ShowTrayIcon)
            {
                base.OnClosing(e);
                return;
            }

            LogModel.Logger.Debug("Closing to tray");
            Hide();
            e.Cancel = true;
        }

        private void OnRequestedThemeChanged(FluentAvaloniaTheme sender, RequestedThemeChangedEventArgs args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: add Windows version to CoreWindow
                if (IsWindows11 && args.NewTheme != FluentAvaloniaTheme.HighContrastModeString)
                {
                    TransparencyBackgroundFallback = Brushes.Transparent;
                    TransparencyLevelHint = WindowTransparencyLevel.Mica;

                    TryEnableMicaEffect(sender);
                }
                else if (args.NewTheme == FluentAvaloniaTheme.HighContrastModeString)
                {
                    // Clear the local value here, and let the normal styles take over for HighContrast theme
                    SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
                }
            }

            MainView.SetAppTitleColor();
            UpdateCheckModel.UpdateNotificationManagerColors();
        }

        private void TryEnableMicaEffect(FluentAvaloniaTheme thm)
        {

            // The background colors for the Mica brush are still based around SolidBackgroundFillColorBase resource
            // BUT since we can't control the actual Mica brush color, we have to use the window background to create
            // the same effect. However, we can't use SolidBackgroundFillColorBase directly since its opaque, and if
            // we set the opacity the color become lighter than we want. So we take the normal color, darken it and
            // apply the opacity until we get the roughly the correct color
            // NOTE that the effect still doesn't look right, but it suffices. Ideally we need access to the Mica
            // CompositionBrush to properly change the color but I don't know if we can do that or not
            if (thm.RequestedTheme == FluentAvaloniaTheme.DarkModeString)
            {
                Color2 color = this.TryFindResource("SolidBackgroundFillColorBase", out object? value) ? (Color2)(Color)value! : new Color2(32, 32, 32);

                color = color.LightenPercent(-0.8f);

                Background = new ImmutableSolidColorBrush(color, 0.78);
            }
            else if (thm.RequestedTheme == FluentAvaloniaTheme.LightModeString)
            {
                // Similar effect here
                Color2 color = this.TryFindResource("SolidBackgroundFillColorBase", out object? value) ? (Color2)(Color)value! : new Color2(243, 243, 243);

                color = color.LightenPercent(0.5f);

                Background = new ImmutableSolidColorBrush(color, 0.9);
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

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        public static bool IsValidRequestedTheme(string thm) => FluentAvaloniaTheme.LightModeString.Equals(thm, StringComparison.OrdinalIgnoreCase) ||
                FluentAvaloniaTheme.DarkModeString.Equals(thm, StringComparison.OrdinalIgnoreCase) ||
                FluentAvaloniaTheme.HighContrastModeString.Equals(thm, StringComparison.OrdinalIgnoreCase);
    }
}
