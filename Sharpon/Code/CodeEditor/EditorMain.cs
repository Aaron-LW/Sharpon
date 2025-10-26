using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public static class EditorMain
{
    public static float BaseFontSize = 20;
    public static float ScaleModifier => MathF.Round((float)_gameWindow.ClientBounds.Width / 1920, 2);

    private static FontSystem _fontSystem = new FontSystem();
    private static GameWindow _gameWindow;
    private static SimpleFps _fps = new SimpleFps();
    private static float _lineSpacing => (float)(1 * BaseFontSize);
    private static Queue<char> _charQueue = new Queue<char>();
    private static string _filePath = "/home/tatzi/C#/test/Program.cs";
    private static float _codeMaxY = 60;
    private static Vector2 _codePosition = new Vector2(50, _codeMaxY);
    private static Vector2 _cursorPosition;
    private static float _textOpacity = 1;

    public static List<string> Lines = new List<string>() { "" };
    public static int LineIndex { get; private set; }
    public static int CharIndex { get; private set; }
    public static int LineLength => Lines[LineIndex].Length;
    public static string Line
    {
        get { return Lines[LineIndex]; }
        private set { Lines[LineIndex] = value; }    
    }
        
    public static void Start(GameWindow gameWindow)
    {
        string fontPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "JetBrainsMonoNLNerdFont-Bold.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        _gameWindow = gameWindow;
        LoadFile(_filePath);
    }

    public static void Update(GameTime gameTime)
    {
        _fps.Update(gameTime);

        if (Input.IsKeyPressed(Keys.LeftAlt))
        {
            Random random = new Random();
            _textOpacity = (float)random.Next(0, 10) / 10;
        }

        if (_codePosition.Y <= _codeMaxY)
        {
            _codePosition.Y = MathHelper.Lerp(_codePosition.Y, -(LineIndex * _lineSpacing) + (_gameWindow.ClientBounds.Height / 2), 12 * Time.DeltaTime);
        }

        if (_codePosition.Y > _codeMaxY)
        {
            _codePosition.Y = _codeMaxY;
        }

        InputHandler.Update();
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = _fontSystem.GetFont(BaseFontSize * ScaleModifier);
        int cursorSpeed = 50;

        for (int i = 0; i < Lines.Count; i++)
        {
            spriteBatch.DrawString(font, Lines[i], new Vector2(_codePosition.X, _codePosition.Y + (i * _lineSpacing)) * ScaleModifier, Color.White * _textOpacity);
        }
        _cursorPosition = new Vector2(MathHelper.Lerp(_cursorPosition.X, _codePosition.X + font.MeasureString(Lines[LineIndex].Substring(0, CharIndex)).X / ScaleModifier - (font.MeasureString("|") / 2).X, cursorSpeed * Time.DeltaTime), MathHelper.Lerp(_cursorPosition.Y, _codePosition.Y + (LineIndex * _lineSpacing), cursorSpeed * Time.DeltaTime));
        spriteBatch.DrawString(font, "|", _cursorPosition * ScaleModifier, Color.White);

        spriteBatch.DrawString(font, _fps.msg, new Vector2(_gameWindow.ClientBounds.Width - font.MeasureString(_fps.msg).X, 20) * ScaleModifier, Color.White);
        _fps.frames++;

        //spriteBatch.DrawString(font, $"Lines: {Lines.Count}", new Vector2(150, 20) * ScaleModifier, Color.White);
        //spriteBatch.DrawString(font, $"Current Line: {LineIndex}", new Vector2(250, 20) * ScaleModifier, Color.White);
    }

    public static void SetCharIndex(int charIndex)
    {
        charIndex = VerifyCharIndex(charIndex);
        CharIndex = charIndex;
    }

    public static void AddToCharIndex(int number)
    {
        CharIndex += number;
        CharIndex = VerifyCharIndex(CharIndex);
    }

    public static int VerifyCharIndex(int charIndex)
    {
        if (charIndex > LineLength)
        {
            charIndex = LineLength;
        }

        if (charIndex < 0)
        {
            charIndex = 0;
        }

        return charIndex;
    }

    public static void SetLineIndex(int lineIndex)
    {
        lineIndex = VerifyLineIndex(lineIndex);
        LineIndex = lineIndex;
    }

    public static void AddToLineIndex(int number)
    {
        LineIndex += number;
        LineIndex = VerifyLineIndex(LineIndex);
    }

    public static int VerifyLineIndex(int lineIndex)
    {
        if (lineIndex >= Lines.Count)
        {
            lineIndex = Lines.Count - 1;
        }

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        return lineIndex;
    }

    public static void SetSelectedLine(string line)
    {
        Line = line;
        CharIndex = VerifyCharIndex(CharIndex);
    }

    public static void SetLine(string line, int lineIndex)
    {
        if (lineIndex > Lines.Count || lineIndex < 0) return;
        Lines[lineIndex] = line;
    }

    public static void RemoveLine(int lineIndex)
    {
        if (lineIndex > Lines.Count || lineIndex < 0) return;
        if (Lines.Count > 1)
        {
            Lines.RemoveAt(lineIndex);
        }
        else
        {
            Lines[0] = "";
        }

        LineIndex = VerifyLineIndex(LineIndex);
        CharIndex = VerifyCharIndex(CharIndex);
    }

    public static void LoadFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            if (Path.GetExtension(filePath) == ".txt" || Path.GetExtension(filePath) == ".cs")
            {
                Lines = File.ReadAllText(filePath).Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
                if (Lines.Count - 1 < LineIndex)
                {
                    LineIndex = Lines.Count - 1;
                }

                SetCharIndex(LineLength);
            }
        }
        else
        {
            throw new FileNotFoundException("File to read from not found");
        }
    }

    public static void SaveFile(string filePath)
    {
        if (!File.Exists(filePath)) File.Create(filePath);
        File.WriteAllText(filePath, String.Join("\r\n", Lines));
    }

    private static int GetFirstNonSpaceCharacterIndex(int lineIndex)
    {
        if (lineIndex < Lines.Count)
        {
            if (Lines[lineIndex].Length <= 0)
            {
                return -1;
            }

            int index = 0;
            while (Lines[lineIndex][index] == ' ')
            {
                index++;
                if (index == Lines[lineIndex].Length)
                {
                    return -1;
                }
            }

            return index;
        }

        return -1;
    }

    private static bool IsLineEmpty(int lineIndex)
    {
        if (lineIndex > Lines.Count) throw new ArgumentOutOfRangeException("LineIndex was higher than amount of lines");
        if (string.IsNullOrWhiteSpace(Lines[lineIndex]))
        {
            return true;
        }

        return false;
    }
}