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
    private static TextBlock[] _occurences = [];
    private static int _occurenceIndex = -1;
    
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
        
        for (int i = 0; i < _occurences.Length; i++)
        {
            if (Text == string.Empty) break;
            Vector2 additivePosition = new Vector2(0, _finderHeight * (i + 1));
            
            spriteBatch.FillRectangle(new RectangleF(_finderPosition.X,
                                      _finderPosition.Y + additivePosition.Y,
                                      _finderWidth,
                                      _finderHeight),
                                      _finderBackgroundColor);
            
            spriteBatch.DrawRectangle(new RectangleF(_finderPosition.X,
                                                     _finderPosition.Y + additivePosition.Y,
                                                     _finderWidth,
                                                     _finderHeight),
                                                     i == _occurenceIndex ? Color.Gray : _finderOutlineColor,
                                                     2f);
                                                    
            if (_occurences[i].LineIndex != null)
            {
                Vector2 textOccurencePosition = new Vector2(textPosition.X, textPosition.Y + additivePosition.Y);
                Vector2 lineTextPosition = new Vector2(_finderPosition.X + _finderWidth - font.MeasureString(_occurences[i].LineIndex.ToString()).X - 10 * EditorMain.ScaleModifier, textPosition.Y + additivePosition.Y);
                string text = EditorMain.Lines[(int)_occurences[i].LineIndex].Substring(_occurences[i].Start, _occurences[i].End - _occurences[i].Start);
                
                spriteBatch.DrawString(font, text, textOccurencePosition, Color.White);
                spriteBatch.DrawString(font, _occurences[i].LineIndex.ToString(), lineTextPosition, Color.White);
            }
        }
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
                textBlocks.Add(new TextBlock(i, i + Text.Length, null, null));
            }
        }
        
        return textBlocks.ToArray();
    }
    
    public static void SetText(string text)
    {
        string previousText = Text;
        Text = text;
        if (previousText != Text) RegenerateOccurences();
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
            if (Text == "")
            {
                Close();
                return;
            }
            
            if (Input.IsKeyDown(Keys.LeftShift))
            {
                Text = "";
                SetCharIndex(0);
                Close();
            }
        }
        
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            if (Input.IsKeyPressed(Keys.K))
            {
                _occurenceIndex++;
                if (_occurenceIndex > _occurences.Length - 1) _occurenceIndex = _occurences.Length - 1;
                UpdateEditorLineIndexByOccurence();
            }
            
            if (Input.IsKeyPressed(Keys.I))
            {
                _occurenceIndex--;
                if (_occurenceIndex < -1) _occurenceIndex = -1;
                UpdateEditorLineIndexByOccurence();
            }
        }
    }
    
    private static void UpdateEditorLineIndexByOccurence()
    {
        if (_occurenceIndex < 0 || _occurenceIndex > _occurences.Length) return;
        if (_occurences[_occurenceIndex].LineIndex == null) return;
        if (_occurences[_occurenceIndex].CharIndex == null) return;
        
        EditorMain.SetLineIndex((int)_occurences[_occurenceIndex].LineIndex);
        EditorMain.SetCharIndex((int)_occurences[_occurenceIndex].CharIndex);
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
    
    public static void RegenerateOccurences()
    {
        if (!IsOpened) return;
        List<TextBlock> textBlocks = new List<TextBlock>();
        _occurenceIndex = -1;
        
        //fuck you
        int index = 0;
        foreach (string line in EditorMain.Lines)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (i + Text.Length > line.Length) break;
                string substring = line.Substring(i, Text.Length);
                
                if (substring == Text)
                {
                    textBlocks.Add(new TextBlock(i, i + Text.Length, index, i));
                }
            }
            
            index++;
        }
        
        _occurences = textBlocks.ToArray();
    }
}