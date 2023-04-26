#region

using NLog;
using NLog.Targets;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Targets;

[Target("OutputControl")]
public sealed class OutputControlTarget : TargetWithLayout
{
    protected override void Write(LogEventInfo logEvent)
    {
        if (MainPageViewModel.Instance is MainPageViewModel mainPageViewModel)
        {
            mainPageViewModel.PrintLine(logEvent.Level, logEvent.FormattedMessage);
        }
    }
}
