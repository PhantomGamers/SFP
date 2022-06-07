using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAvalonia.Core;
using FluentAvalonia.Styling;

using SFP_UI.Views;

namespace SFP_UI.Models.ThemeChangeDetection
{
    internal class Linux
    {
        public static Process? MonitorProcess { get; private set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "Unsupported on some platforms")]
        public static void WatchForChanges() => _ = Task.Run(() =>
                                                         {
                                                             try
                                                             {
                                                                 Process process = new()
                                                                 {
                                                                     StartInfo = new ProcessStartInfo
                                                                     {
                                                                         WindowStyle = ProcessWindowStyle.Hidden,
                                                                         CreateNoWindow = true,
                                                                         UseShellExecute = false,
                                                                         RedirectStandardError = true,
                                                                         RedirectStandardOutput = true,
                                                                         FileName = "gsettings",
                                                                         Arguments = "monitor org.gnome.desktop.interface gtk-theme",
                                                                     },
                                                                 };
                                                                 process.OutputDataReceived += (sender, args) =>
                                                                 {
                                                                     if (!MainWindow.IsValidRequestedTheme(SFP.Properties.Settings.Default.AppTheme))
                                                                     {
                                                                         MainWindow.Instance?.Theme?.InvalidateThemingFromSystemThemeChanged();
                                                                     }
                                                                 };
                                                                 MonitorProcess = process;
                                                                 process.Start();
                                                             }
                                                             catch
                                                             {
                                                                 SFP.LogModel.Logger.Warn("Unable to detect system theme changes on this platform.");
                                                             }
                                                         });
    }
}
