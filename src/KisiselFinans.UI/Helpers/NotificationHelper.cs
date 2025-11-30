using DevExpress.XtraBars.ToastNotifications;

namespace KisiselFinans.UI.Helpers;

public static class NotificationHelper
{
    private static ToastNotificationsManager? _manager;

    public static void Initialize(System.Windows.Forms.Form form)
    {
        _manager = new ToastNotificationsManager { ApplicationId = "KisiselFinans" };
        _manager.Notifications.Add(CreateTemplate("Success", "Başarılı", ToastNotificationTemplate.Text02));
        _manager.Notifications.Add(CreateTemplate("Warning", "Uyarı", ToastNotificationTemplate.Text02));
        _manager.Notifications.Add(CreateTemplate("Error", "Hata", ToastNotificationTemplate.Text02));
        _manager.Notifications.Add(CreateTemplate("Info", "Bilgi", ToastNotificationTemplate.Text02));
    }

    private static ToastNotification CreateTemplate(string id, string header, ToastNotificationTemplate template)
    {
        return new ToastNotification(id, template, header) { };
    }

    public static void ShowSuccess(string message)
    {
        _manager?.ShowNotification("Success", message);
    }

    public static void ShowWarning(string message)
    {
        _manager?.ShowNotification("Warning", message);
    }

    public static void ShowError(string message)
    {
        _manager?.ShowNotification("Error", message);
    }

    public static void ShowInfo(string message)
    {
        _manager?.ShowNotification("Info", message);
    }
}

