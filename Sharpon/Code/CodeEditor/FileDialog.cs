using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

public static class FileDialog
{
    public static bool IsOpened = false;
    public static string Text { get; private set; } = "";
    public static int CharIndex { get; private set; } = Text.Length;
    public static int LineIndex { get; private set; } = 0;

    private static GameWindow _gameWindow;
    private static Vector2 _cursorPosition;
    private static int _fileAmount = 0;
    private static string[] _filePaths = new string[0];
    private static bool _exists = false;
    private static bool _existsFile = false;
    private static bool _canAccess = false;
    private static int _spacing = 20;
    private static bool _updatedText = true;
    private static int _maxListedDirectoryEntries = 100;

    private static float _keyTimer = 0;
    private static bool _keyPressed = false;
    private static float _baseKeyTimer = 0.2f;
    private static float _baseFastKeyTimer = 0.04f;

    public static void Start(GameWindow window)
    {
        _gameWindow = window;
    }

    public static void Update()
    {
        if (!_updatedText) return;

        string currentDirectoryPath = Path.GetDirectoryName(Text);

        if (currentDirectoryPath != String.Empty && currentDirectoryPath != null && Directory.Exists(currentDirectoryPath))
        {
            _exists = true;
            List<string> filePaths = Directory.GetFileSystemEntries(currentDirectoryPath).Take(_maxListedDirectoryEntries).ToList();
            for (int i = 0; i < filePaths.Count;)
            {
                if (filePaths[i].Length < Text.Length)
                {
                    filePaths.RemoveAt(i);
                    continue;
                }

                if (filePaths[i].Substring(0, Text.Length) != Text)
                {
                    filePaths.RemoveAt(i);
                    continue;
                }

                i++;
            }

            _existsFile = File.Exists(Text);
            _canAccess = CanAccessPath(Text);
            _fileAmount = filePaths.Count;
            _filePaths = filePaths.ToArray();
        }
        else if (!Directory.Exists(currentDirectoryPath))
        {
            _exists = false;
        }

        _updatedText = false;
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        if (!IsOpened) return;
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        Vector2 position = new Vector2(_gameWindow.ClientBounds.Width / 3,
                                       _gameWindow.ClientBounds.Height / 2 - (font.MeasureString("|").Y * EditorMain.ScaleModifier / 2));

        if (!_canAccess && _existsFile) spriteBatch.DrawString(font, "Can't access file (No permission)", position - new Vector2(0, _spacing) * EditorMain.ScaleModifier, Color.Red);
        else if (!_existsFile) spriteBatch.DrawString(font, "File doesn't exist", position - new Vector2(0, _spacing) * EditorMain.ScaleModifier, Color.Red);

        spriteBatch.DrawString(font, Text, position, Color.White);

        if (_exists && _filePaths.Length > 0)
        {
            for (int i = 0; i < _fileAmount; i++)
            {
                if (LineIndex == i)
                {
                    spriteBatch.FillRectangle(new RectangleF(position + new Vector2(0, (i + 1) * _spacing) * EditorMain.ScaleModifier,
                                              new SizeF(font.MeasureString(_filePaths[i]).X, font.MeasureString(_filePaths[i]).Y)),
                                                Color.LightBlue * 0.5f);
                }

                spriteBatch.DrawString(font, _filePaths[i], position + new Vector2(0, (i + 1) * _spacing) * EditorMain.ScaleModifier, Color.White);
            }
        }

        int cursorSpeed = 50;
        _cursorPosition = new Vector2(MathHelper.Lerp(_cursorPosition.X, (position.X + font.MeasureString(Text.Substring(0, CharIndex)).X) / EditorMain.ScaleModifier - font.MeasureString("|").X / 2, cursorSpeed * Time.DeltaTime),
                                      position.Y / EditorMain.ScaleModifier);

        spriteBatch.DrawString(font, "|", _cursorPosition * EditorMain.ScaleModifier, Color.White);
    }

    public static void SetText(string line)
    {
        if (line != Text)
        {
            LineIndex = 0;
            _updatedText = true;
            Text = line;
        }
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
        if (Text.Length <= 0) return;

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
        InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
        IsOpened = false;
        CharIndex = 0;
        EditorMain.LoadFile(Text);
    }

    public static void HandleTab()
    {
        SetText(_filePaths[LineIndex]);
        AddToCharIndex(_filePaths[LineIndex].Length);
    }

    public static void HandleKeybinds()
    {
        if (Input.IsKeyDown(Keys.Right) && _keyTimer <= 0)
        {
            AddToCharIndex(1);
            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Left) && _keyTimer <= 0)
        {
            AddToCharIndex(-1);
            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Down) && _keyTimer <= 0)
        {
            LineIndex++;
            if (LineIndex > _fileAmount - 1) LineIndex = _fileAmount - 1;
            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Up) && _keyTimer <= 0)
        {
            LineIndex--;
            if (LineIndex <= 0) LineIndex = 0;
            ResetKeyTimer();
            _keyPressed = true;
        }

        if (!Input.IsKeyDown(Keys.Right) &&
            !Input.IsKeyDown(Keys.Left) &&
            !Input.IsKeyDown(Keys.Up) &&
            !Input.IsKeyDown(Keys.Down) &&
            !Input.IsKeyDown(Keys.X))
        {
            _keyPressed = false;
            _keyTimer = 0;
        }

        _keyTimer -= Time.DeltaTime;
    }

    public static void Open()
    {
        if (Text == String.Empty) SetText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        IsOpened = true;
        CharIndex = Text.Length;
    }

    public static void Close()
    {
        IsOpened = false;
    }

    private static void ResetKeyTimer()
    {
        if (_keyPressed)
        {
            _keyTimer = _baseFastKeyTimer;
        }
        else
        {
            _keyTimer = _baseKeyTimer;
        }
    }

    public static bool CanAccessPath(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}