using Avalonia.Notification;

namespace SFP_UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static INotificationMessageManager Manager { get; } = new NotificationMessageManager();
    }
}
