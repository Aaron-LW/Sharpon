using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System.IO;

public static class Terminal
{
    public static bool IsOpened = false;
    public static int LineIndex = 0;
    public static int CharIndex = 0;
    public static string Text = "";

    private static TerminalProcess _terminalProcess;
    private static GameWindow _gameWindow;
    private static float _terminalHeight = 350;
    private static Vector2 _terminalPosition;
    private static List<string> _lines = new List<string>();
    private static float _spacing = 20;
    private static Vector2 _cursorPosition;

    public static void Start(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
        _terminalProcess = new TerminalProcess();
        _terminalProcess.Start();
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        Rectangle windowBounds = _gameWindow.ClientBounds;
        float terminalSpeed = 20;
        if (!IsOpened)
        {
            if (_terminalPosition.Y > windowBounds.Height) return;
            _terminalPosition.Y = MathHelper.Lerp(_terminalPosition.Y, windowBounds.Height + 100 * EditorMain.ScaleModifier, terminalSpeed * Time.DeltaTime);
        }
        else
        {
            _terminalPosition.Y = MathHelper.Lerp(_terminalPosition.Y, windowBounds.Height - _terminalHeight * EditorMain.ScaleModifier, terminalSpeed * Time.DeltaTime);
        }
        
        
        spriteBatch.FillRectangle(new RectangleF(_terminalPosition.X, 
                                                 _terminalPosition.Y,
                                                 windowBounds.Width,
                                                 windowBounds.Height - (windowBounds.Height - _terminalHeight * EditorMain.ScaleModifier)),
                                                 Color.Black);

        spriteBatch.DrawLine(_terminalPosition, _terminalPosition + new Vector2(windowBounds.Width, 0), Color.LightBlue, 3);
        
        int lineStartDrawIndex = _lines.Count - 15;
        if (lineStartDrawIndex < 0)
        {
            lineStartDrawIndex = 0;
        }
        
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        int index = 0;
        for (int i = lineStartDrawIndex; i < _lines.Count; i++)
        {
            spriteBatch.DrawString(font, _lines[i], _terminalPosition + new Vector2(10, 10 + (_spacing * index)) * EditorMain.ScaleModifier, Color.White);
            index++;
        }

        string adjustedText = Text.Insert(0, "$");
        spriteBatch.DrawString(font, adjustedText, _terminalPosition + new Vector2(10, 10 + (_spacing * index)) * EditorMain.ScaleModifier, Color.White);

        float cursorSpeed = 60;
        _cursorPosition.X = MathHelper.Lerp(_cursorPosition.X, _terminalPosition.X + (10 * EditorMain.ScaleModifier) + font.MeasureString(adjustedText.Substring(1, CharIndex)).X - (font.MeasureString("|") / 2).X + font.MeasureString("$").X, cursorSpeed * Time.DeltaTime);
        _cursorPosition.Y = MathHelper.Lerp(_cursorPosition.Y, _terminalPosition.Y + (10 + _spacing * index) * EditorMain.ScaleModifier, cursorSpeed * Time.DeltaTime);

        spriteBatch.DrawString(font, "|", _cursorPosition, Color.White);
    }

    public static void HandleKeybinds()
    {
        if (Input.IsKeyDown(Keys.LeftControl) && Input.IsKeyPressed(Keys.Z))
        {
            Toggle();
            InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
        }
    }
    
    public static void HandleBackspace()
    {
        if (CharIndex == 0) return;
        if (Text.Length <= 0) return;

        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = EditorMain.NextControlLeftArrowIndex(CharIndex, Text);
            SetText(Text.Remove(nextIndex, CharIndex - nextIndex));
            SetCharIndex(nextIndex);
            return;
        }

        if (CharIndex != Text.Length)
        {
            if (Text[CharIndex] == ')')
            {
                SetText(Text.Remove(CharIndex - 1, 2));
                AddToCharIndex(-1);
                return;
            }
        }

        SetText(Text.Remove(CharIndex - 1, 1));
        AddToCharIndex(-1);
    }
    
    public static void HandleEnter()
    {
        Print(Text.Insert(0, "$"));
        _terminalProcess.SendCommand(Text);
        
        SetText("");
        SetCharIndex(0);
    }

    public static int GetCharIndex()
    {
        return CharIndex;
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
    
    public static void SetText(string line)
    {
        Text = line;
    }
    
    public static void Toggle()
    {
        IsOpened = !IsOpened;
        if (IsOpened)
        {
            string solutionFilePath = FileDialog.TryGetSolutionDirectoryInfo(Path.GetDirectoryName(EditorMain.FilePath));
            if (solutionFilePath != null)
            {
                SendCommand($"cd {solutionFilePath}");
            }
            else
            {
                SendCommand($"cd {Path.GetDirectoryName(EditorMain.FilePath)}");
            }
        }
    }

    public static void Stop()
    {
        if (_terminalProcess != null)
        {
            _terminalProcess.Stop();
            _terminalProcess = null;
        }
    }

    public static void SendCommand(string command)
    {
        if (_terminalProcess != null)
        {
            _terminalProcess.SendCommand(command);
        }
    }
    
    public static void Print(string line)
    {
        _lines.Add(line);
    }
}