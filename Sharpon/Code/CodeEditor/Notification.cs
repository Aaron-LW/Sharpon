using Microsoft.Xna.Framework;

public class Notification
{
    public string Text;
    public float Duration;
    public float YPosition;
    public NotificationType NotificationType;
    
    public Notification(string text, float duration, NotificationType notificationType)
    {
        Text = text;
        Duration = duration;
        YPosition = NotificationManager.GetNotificationStartY();
        NotificationType = notificationType;
    }
}