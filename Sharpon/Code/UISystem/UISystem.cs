using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;

public static class UISystem
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
    public static int LineIndex = 0;
    public static int CharIndex = 0;
    public static int LineLength => Lines[LineIndex].Length;
        
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
        InputHandler.Update();

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

    private static void HandleTab()
    {
        if (Input.IsKeyDown(Keys.LeftShift))
        {
            if (CharIndex != 0)
            {
                if (LineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (Lines[LineIndex][i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    Lines[LineIndex] = Lines[LineIndex].Substring(spaces, LineLength - spaces);
                    CharIndex -= spaces;
                }
            }
            else
            {
                if (LineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (Lines[LineIndex][i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    Lines[LineIndex] = Lines[LineIndex].Substring(spaces, LineLength - spaces);
                }
            }

        }
        else
        {
            _charQueue.Enqueue(' ');
            _charQueue.Enqueue(' ');
            _charQueue.Enqueue(' ');
            _charQueue.Enqueue(' ');
        }

    }

    private static void LoadFile(string filePath)
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

    private static void SaveFile(string filePath)
    {
        if (!File.Exists(filePath)) File.Create(filePath);
        File.WriteAllText(filePath, String.Join("\r\n", Lines));
    }

    private static void SetCharIndex(int charIndex)
    {
        if (charIndex > LineLength)
        {
            charIndex = LineLength;
        }

        CharIndex = charIndex;
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