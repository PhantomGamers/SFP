#region

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;
using SFP.Models;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI.Views;

public partial class MainWindow : AppWindow
{
    private bool _isStarting = true;

    public MainWindow()
    {
        Instance = this;

        if (Settings.Default.StartMinimized)
        {
            WindowState = WindowState.Minimized;
        }

        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        Application.Current!.ActualThemeVariantChanged += ApplicationActualThemeVariantChanged;

        App.SetApplicationTheme(Settings.Default.AppTheme);
    }

    public static MainWindow? Instance { get; private set; }

    private void ApplicationActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // TODO: add Windows version to CoreWindow
        if (IsWindows11 && ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
        {
            Log.Logger.Info("Enabling Mica effect");
            TryEnableMicaEffect();
        }
        else if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
        {
            // Clear the local value here, and let the normal styles take over for HighContrast theme
            SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
        }
    }

    private void TryEnableMicaEffect()
    {
        // The background colors for the Mica brush are still based around SolidBackgroundFillColorBase resource
        // BUT since we can't control the actual Mica brush color, we have to use the window background to create
        // the same effect. However, we can't use SolidBackgroundFillColorBase directly since its opaque, and if
        // we set the opacity the color become lighter than we want. So we take the normal color, darken it and
        // apply the opacity until we get the roughly the correct color
        // NOTE that the effect still doesn't look right, but it suffices. Ideally we need access to the Mica
        // CompositionBrush to properly change the color but I don't know if we can do that or not
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            Color2 color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Dark, out object? value)
                ? (Color)value!
                : new Color2(32, 32, 32);

            color = color.LightenPercent(-0.8f);

            Background = new ImmutableSolidColorBrush(color, 0.78);
        }
        else if (ActualThemeVariant == ThemeVariant.Light)
        {
            // Similar effect here
            Color2 color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Light, out object? value)
                ? (Color)value!
                : new Color2(243, 243, 243);

            color = color.LightenPercent(0.5f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (!_isStarting)
        {
            return;
        }

        _isStarting = false;

        if (Settings.Default.MinimizeToTray
            && Settings.Default is { StartMinimized: true, ShowTrayIcon: true })
        {
            Hide();
        }

        ThemeVariant thm = ActualThemeVariant;

        // Enable Mica on Windows 11
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // TODO: add Windows version to CoreWindow
        if (!IsWindows11 || thm == FluentAvaloniaTheme.HighContrastTheme)
        {
            return;
        }

        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = WindowTransparencyLevel.Mica;

        TryEnableMicaEffect();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!Settings.Default.CloseToTray || !Settings.Default.ShowTrayIcon)
        {
            base.OnClosed(e);
            return;
        }

        Log.Logger.Debug("Closing to tray");
        Hide();
    }

    protected override void HandleWindowStateChanged(WindowState state)
    {
        if (state == WindowState.Minimized && Settings.Default.MinimizeToTray && Settings.Default.ShowTrayIcon)
        {
            Hide();
        }

        base.HandleWindowStateChanged(state);
    }

    public static void ShowWindow()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.Show();
        Instance.WindowState = WindowState.Normal;
        Instance.Activate();
    }
}
