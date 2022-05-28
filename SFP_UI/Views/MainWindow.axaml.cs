using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media;

using SFP_UI.ViewModels;

namespace SFP_UI.Views
{
    public partial class MainWindow : CoreWindow
    {
        public static MainWindow? Instance { get; private set; }

        public readonly TrayIcon trayIcon;

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

            if (_isStarting
               && SFP.Properties.Settings.Default.MinimizeToTray && SFP.Properties.Settings.Default.StartMinimized
               && SFP.Properties.Settings.Default.ShowTrayIcon)
            {
                _isStarting = false;
                Hide();
            }

            FluentAvaloniaTheme? thm = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
            if (thm != null)
            {
                thm.RequestedThemeChanged += OnRequestedThemeChanged;

                // Enable Mica on Windows 11
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // TODO: add Windows version to CoreWindow
                    if (IsWindows11 && thm.RequestedTheme != FluentAvaloniaTheme.HighContrastModeString)
                    {
                        TransparencyBackgroundFallback = Brushes.Transparent;
                        TransparencyLevelHint = WindowTransparencyLevel.Mica;

                        TryEnableMicaEffect(thm);
                    }
                }

                thm.ForceWin32WindowToTheme(this);
            }
        }

        private void OnRequestedThemeChanged(FluentAvaloniaTheme sender, RequestedThemeChangedEventArgs args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: add Windows version to CoreWindow
                if (IsWindows11 && args.NewTheme != FluentAvaloniaTheme.HighContrastModeString)
                {
                    TryEnableMicaEffect(sender);
                }
                else if (args.NewTheme == FluentAvaloniaTheme.HighContrastModeString)
                {
                    // Clear the local value here, and let the normal styles take over for HighContrast theme
                    SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
                }
            }
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
    }
}
