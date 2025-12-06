using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;

public static class PlayTimeCounter
{
    private static float _playTime;
    private static bool _isOpened = false;
    private static GameWindow _gameWindow;
    private static string _filePath;
    
    public static void Start(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playtime.txt");
        Console.WriteLine(_filePath);
        
        if (!File.Exists(_filePath))
        {
            using (var fileStream = File.Create(_filePath)) {}
        }
        
        string fileContent = File.ReadAllText(_filePath);
        if (fileContent != string.Empty) _playTime = float.Parse(fileContent);
    }
    
    public static void Update()
    {
        _playTime += Time.DeltaTime;
        
        if (Input.IsKeyDown(Keys.LeftControl) && Input.IsKeyPressed(Keys.G))
        {
            _isOpened = !_isOpened;
        }
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        if (!_isOpened) return;
        
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        Rectangle windowBounds = _gameWindow.ClientBounds;
        Vector2 position = new Vector2(windowBounds.Width - font.MeasureString(PlayTimeToString()).X - 20 * EditorMain.ScaleModifier,     
                                       windowBounds.Height - font.MeasureString(PlayTimeToString()).Y - 20 * EditorMain.ScaleModifier);
        
        spriteBatch.DrawString(font, PlayTimeToString(), position, Color.White);
    }
    
    public static void SavePlayTime()
    {
        File.WriteAllText(_filePath, _playTime.ToString());
    }
    
    private static string PlayTimeToString()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(_playTime);
        return timeSpan.Hours > 0 ? $"{timeSpan.Hours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                                  : $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}";
    }
}