#region

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;

using SFP.Models;
using SFP.Properties;

using SFP_UI.Models;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Instance = this;
        Title += $" v{UpdateChecker.Version}";
#if DEBUG
        this.AttachDevTools();
#endif
        PropertyChanged += (_, args) =>
        {
            if (args.Property == WindowStateProperty)
            {
                HandleWindowStateChanged(WindowState);
            }
        };

        Application.Current!.ActualThemeVariantChanged += OnActualThemeVariantChanged;

        App.SetApplicationTheme(Settings.Default.AppTheme);
    }

    public static MainWindow? Instance { get; private set; }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!IsWindows11)
        {
            return;
        }

        if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
        {
            TryEnableMicaEffect();
        }
        else
        {
            ClearValue(BackgroundProperty);
            ClearValue(TransparencyBackgroundFallbackProperty);
        }
    }

    private void TryEnableMicaEffect()
    {
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.None];

        // The background colors for the Mica brush are still based around SolidBackgroundFillColorBase resource
        // BUT since we can't control the actual Mica brush color, we have to use the window background to create
        // the same effect. However, we can't use SolidBackgroundFillColorBase directly since its opaque, and if
        // we set the opacity the color become lighter than we want. So we take the normal color, darken it and
        // apply the opacity until we get the roughly the correct color
        // NOTE that the effect still doesn't look right, but it suffices. Ideally we need access to the Mica
        // CompositionBrush to properly change the color but I don't know if we can do that or not
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Dark, out var value) ? (Color2)(Color)value! : new Color2(32, 32, 32);

            color = color.LightenPercent(-0.8f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
        else if (ActualThemeVariant == ThemeVariant.Light)
        {
            // Similar effect here
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Light, out var value) ? (Color2)(Color)value! : new Color2(243, 243, 243);

            color = color.LightenPercent(0.5f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var thm = ActualThemeVariant;
        if (IsWindows11 && thm != FluentAvaloniaTheme.HighContrastTheme)
        {
            TryEnableMicaEffect();
        }
    }

#pragma warning disable EPC27
    protected override async void OnClosing(WindowClosingEventArgs e)
#pragma warning restore EPC27
    {
        try
        {
            base.OnClosing(e);
            if (Instance is null)
            {
                return;
            }
            Instance = null;
            if (OperatingSystem.IsMacOS())
            {
                return;
            }
            if (Settings.Default.CloseToTray)
            {
                return;
            }
            if (WindowState == WindowState.Minimized && Settings.Default is { MinimizeToTray: true })
            {
                return;
            }
            await Task.Run(App.QuitApplication);
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in OnClosing event handler");
            Log.Logger.Debug(ex);
        }
    }

    private void HandleWindowStateChanged(WindowState state)
    {
        if (state == WindowState.Minimized && Settings.Default is { MinimizeToTray: true })
        {
            Close();
        }
    }

    public static void ShowSettings()
    {
        ShowWindow();
        if (MainView.Instance == null)
        {
            Log.Logger.Error("Main view is null, cannot open settings");
            return;
        }
        var frameView = MainView.Instance.FrameView;
        var navView = MainView.Instance.NavView;
        var menuItems = navView.FooterMenuItemsSource.Cast<NavigationViewItem>();
        frameView.NavigateFromObject(menuItems.ElementAt(0).Tag);
    }

    public static void ShowWindow()
    {
        if (Instance == null)
        {
            App.StartMainWindow();
        }

        Instance!.Show();
        Instance.WindowState = WindowState.Normal;
        Instance.Activate();
    }
}