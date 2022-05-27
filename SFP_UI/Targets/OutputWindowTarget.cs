using NLog;
using NLog.Targets;

using SFP_UI.ViewModels;

namespace SFP_UI.Targets
{
    [Target("OutputControl")]
    public sealed class OutputControlTarget : TargetWithLayout
    {
        public static MainPageViewModel? MainPageViewModel { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            if (MainPageViewModel is not null)
            {
                MainPageViewModel.PrintLine(logEvent.Level, logEvent.FormattedMessage);
            }
        }
    }
}
