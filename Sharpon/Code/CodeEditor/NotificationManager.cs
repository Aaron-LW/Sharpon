using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using MonoGame.Extended;

public static class NotificationManager
{
    private static List<Notification> _notifications = new List<Notification>();
    private static GameWindow _gameWindow;
    private static int _spacing = 35;
    private static Color[] _notificationColors = new Color[3] { Color.RoyalBlue, Color.Yellow, new Color(178, 34, 32) };
    
    public static void CreateNotification(string text, float duration, NotificationType notificationType = NotificationType.Normal)
    {
        _notifications.Add(new Notification(text, duration, notificationType));
    }
    
    public static void Start(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
    }
    
    public static void Update()
    {
        foreach (Notification notification in _notifications)
        {
            notification.Duration -= Time.DeltaTime;
        }
        
        _notifications.RemoveAll(notification => notification.Duration <= 0);
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        if (_gameWindow is null) return;
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        Rectangle windowBounds = _gameWindow.ClientBounds;
        Vector2 offset = new Vector2(50, 30);
        float notificationSpeed = 20;
        
        for (int i = 0; i < _notifications.Count; i++)
        {
            string notificationText = _notifications[i].Text;
            Color notificationColor = _notificationColors[(int)_notifications[i].NotificationType];
            
            _notifications[i].YPosition = MathHelper.Lerp(_notifications[i].YPosition, 
                                                          windowBounds.Height - font.MeasureString(notificationText).Y - (offset.Y * EditorMain.ScaleModifier) - (i * _spacing) * EditorMain.ScaleModifier, 
                                                          notificationSpeed * Time.DeltaTime);
            
            Vector2 position = new Vector2(windowBounds.Width - font.MeasureString(notificationText).X - (offset.X * EditorMain.ScaleModifier),
                                           _notifications[i].YPosition);
                                           //windowBounds.Height - font.MeasureString(notificationText).Y - (offset.Y * EditorMain.ScaleModifier) - (i * _spacing) * EditorMain.ScaleModifier);
                                           
            spriteBatch.FillRectangle(new RectangleF(position.X - 5 * EditorMain.ScaleModifier, position.Y - 5 * EditorMain.ScaleModifier,
                                                     font.MeasureString(notificationText).X + 10 * EditorMain.ScaleModifier, font.MeasureString(notificationText).Y + 10 * EditorMain.ScaleModifier),
                                                     notificationColor);
                                                     
            spriteBatch.DrawRectangle(new RectangleF(position.X - 5 * EditorMain.ScaleModifier, position.Y - 5 * EditorMain.ScaleModifier,
                                                     font.MeasureString(notificationText).X + 10 * EditorMain.ScaleModifier, font.MeasureString(notificationText).Y + 10 * EditorMain.ScaleModifier),
                                                     Color.LightBlue);
                                                     
            spriteBatch.DrawString(font, notificationText, position, Color.White);
        }
    }
    
    public static float GetNotificationStartY()
    {
        if (_gameWindow is null) return 0;
        return _gameWindow.ClientBounds.Height + 100 * EditorMain.ScaleModifier;
    }
}