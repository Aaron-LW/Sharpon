using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using FontStashSharp;
using System.Collections.Generic;
using System;

public static class Finder
{
    public static bool IsOpened = false;
    public static string Text { get; private set; } = "";
    public static int CharIndex { get; private set; } = 0;
    
    private static GameWindow _gameWindow;
    private static Vector2 _finderPosition;
    private static float _finderWidth => 300 * EditorMain.ScaleModifier;
    private static float _finderHeight => 35 * EditorMain.ScaleModifier;
    private static Color _finderBackgroundColor = new Color(45, 43, 52);
    private static Color _finderOutlineColor = new Color(60, 58, 67);
    private static Vector2 _cursorPosition;
    
    public static void Start(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        Rectangle windowBounds = _gameWindow.ClientBounds;
        int finderSpeed = 30;
        
        if (!IsOpened)
        {
            if (_finderPosition.X < windowBounds.Width)
            {
                _finderPosition.X = MathHelper.Lerp(_finderPosition.X, windowBounds.Width, finderSpeed * Time.DeltaTime);
            }
            else
            {
                return;
            }
        }
        else
        {
            _finderPosition.X = MathHelper.Lerp(_finderPosition.X, windowBounds.Width - _finderWidth - 20 * EditorMain.ScaleModifier, finderSpeed * Time.DeltaTime);
            _finderPosition.Y = 0 + _finderHeight + 20;
        }
        
        
        spriteBatch.FillRectangle(new RectangleF(_finderPosition.X, _finderPosition.Y, _finderWidth, _finderHeight), _finderBackgroundColor);
        spriteBatch.DrawRectangle(new Rectangle((int)_finderPosition.X, (int)_finderPosition.Y, (int)_finderWidth, (int)_finderHeight), _finderOutlineColor, 2 * EditorMain.ScaleModifier);
        
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        Vector2 textPosition = _finderPosition + new Vector2(10 * EditorMain.ScaleModifier, _finderHeight / 2 - (font.MeasureString("|").Y / 2));
        spriteBatch.DrawString(font, Text, textPosition, Color.White);
        
        int cursorSpeed = 60;
        _cursorPosition.X = MathHelper.Lerp(_cursorPosition.X, textPosition.X + font.MeasureString(Text).X - font.MeasureString("|").X / 2, cursorSpeed * Time.DeltaTime);
        _cursorPosition.Y = textPosition.Y;
        
        
        if (InputDistributor.SelectedInputReceiver() == InputDistributor.InputReceiver.Finder) spriteBatch.DrawString(font, "|", _cursorPosition, Color.White);
    }
    
    public static TextBlock[] Find(string line)
    {
        if (Text == "") return new TextBlock[0];
        List<TextBlock> textBlocks = new();
        
        for (int i = 0; i < line.Length; i++)
        {
            if (i + Text.Length > line.Length) return textBlocks.ToArray();
            if (line.Substring(i, Text.Length) == Text)
            {
                textBlocks.Add(new TextBlock(i, i + Text.Length));
            }
        }
        
        return textBlocks.ToArray();
    }
    
    public static void SetText(string text)
    {
        Text = text;
    }
    
    public static void SetCharIndex(int charIndex)
    {
        if (charIndex > Text.Length) charIndex = Text.Length;
        if (charIndex < 0) charIndex = 0;
        
        CharIndex = charIndex;
    }
    
    public static void AddToCharIndex(int amount)
    {
        CharIndex += amount;
        
        if (CharIndex > Text.Length) CharIndex = Text.Length;
        if (CharIndex < 0) CharIndex = 0;
    }
    
    public static void HandleBackspace()
    {
        if (CharIndex == 0) return;
        
        if (CharIndex != Text.Length)
        {
            if (Text[CharIndex] == ')')
            {
                SetText(Text.Remove(CharIndex - 1, 2));
                AddToCharIndex(-1);
                return;
            }
        }
        
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = EditorMain.NextControlLeftArrowIndex(Text, CharIndex);
            SetText(Text.Remove(nextIndex, CharIndex - nextIndex));
            SetCharIndex(CharIndex - (CharIndex - nextIndex));
            return;
        }
        
        SetText(Text.Remove(CharIndex - 1, 1));
        AddToCharIndex(-1);
    }
    
    public static void HandleKeybinds()
    {
        if (Input.IsKeyPressed(Keys.Escape) || Input.IsKeyPressed(Keys.F) && Input.IsKeyDown(Keys.LeftControl))
        {
            InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
            if (Text == "") Close();
            
            if (Input.IsKeyDown(Keys.LeftShift))
            {
                Text = "";
                SetCharIndex(0);
                Close();
            }
        }
    }
    
    public static void Open()
    {
        _finderPosition.X = _gameWindow.ClientBounds.Width;
        IsOpened = true;
    }
    
    public static void Close()
    {
        IsOpened = false;
    }
}