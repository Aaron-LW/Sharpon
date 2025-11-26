using System;     
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static Color _fileDialogColor = new Color(40, 38, 47);
    private static Color _fileDialogTextBoxColor = new Color(50, 48, 57);

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

                if (filePaths[i].Substring(0, Text.Length).ToLower() != Text.ToLower())
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
        //holy bad code
        
        if (!IsOpened) return;
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        Vector2 position = new Vector2(_gameWindow.ClientBounds.Width / 2.6f,
                                       100 * EditorMain.ScaleModifier);
                                       
        float xWidth = 0;
        for (int i = 0; i < _filePaths.Length; i++)
        {
            if (File.Exists(_filePaths[i]))
            {
                if (font.MeasureString(Path.GetFileName(_filePaths[i])).X > xWidth)
                {
                    xWidth = font.MeasureString(Path.GetFileName(_filePaths[i])).X;
                }
            }
            else
            {
                if (font.MeasureString(Path.GetFileName(_filePaths[i]) + Path.DirectorySeparatorChar).X > xWidth)
                {
                    xWidth = font.MeasureString(Path.GetFileName(_filePaths[i]) + Path.DirectorySeparatorChar).X;
                }
            }
        }
        
        if (font.MeasureString(Text).X > xWidth)
        {
            xWidth = font.MeasureString(Text).X;
        }
        

        spriteBatch.FillRectangle(new RectangleF(position - new Vector2(5, 0) * EditorMain.ScaleModifier,
                                                 new SizeF(xWidth + 10 * EditorMain.ScaleModifier, 20 * EditorMain.ScaleModifier)),
                                                 _fileDialogTextBoxColor);

        if (_exists)
        {
            spriteBatch.FillRectangle(new RectangleF(position + new Vector2(0, _spacing * EditorMain.ScaleModifier) - new Vector2(5, 0) * EditorMain.ScaleModifier,
                                                     new SizeF(xWidth + 10 * EditorMain.ScaleModifier, _fileAmount * _spacing * EditorMain.ScaleModifier + 10 * EditorMain.ScaleModifier)),
                                                     _fileDialogColor);
        }

        if (!_canAccess && _existsFile) spriteBatch.DrawString(font, "Can't access file (No permission)", position - new Vector2(0, _spacing) * EditorMain.ScaleModifier, Color.Red);
        //else if (!_existsFile) spriteBatch.DrawString(font, "File doesn't exist", position - new Vector2(0, _spacing) * EditorMain.ScaleModifier, Color.Red);

        spriteBatch.DrawString(font, Text, position, Color.White);

        if (_exists && _filePaths.Length > 0)
        {
            for (int i = 0; i < _fileAmount; i++)
            {
                if (LineIndex == i)
                {
                    if (File.Exists(_filePaths[i]))
                    {
                        spriteBatch.FillRectangle(new RectangleF(position + (new Vector2(0, 5) * EditorMain.ScaleModifier) + new Vector2(0, (i + 1) * _spacing) * EditorMain.ScaleModifier,
                                                  new SizeF(font.MeasureString(Path.GetFileName(_filePaths[i])).X, font.MeasureString(Path.GetFileName(_filePaths[i])).Y)),
                                                  Color.LightBlue * 0.5f);
                    }
                    else
                    {
                        spriteBatch.FillRectangle(new RectangleF(position + (new Vector2(0, 5) * EditorMain.ScaleModifier) + new Vector2(0, (i + 1) * _spacing) * EditorMain.ScaleModifier,
                                                  new SizeF(font.MeasureString(Path.GetFileName(_filePaths[i]) + Path.DirectorySeparatorChar).X,
                                                  font.MeasureString(Path.GetFileName(_filePaths[i]) + Path.DirectorySeparatorChar).Y)),
                                                  Color.LightBlue * 0.5f);
                    }
                }

                if (File.Exists(_filePaths[i]))
                {
                    spriteBatch.DrawString(font, Path.GetFileName(_filePaths[i]), position + (new Vector2(0, 5) * EditorMain.ScaleModifier) + new Vector2(0, (i + 1) * _spacing) * EditorMain.ScaleModifier, Color.White);
                }
                else
                {
                    spriteBatch.DrawString(font, Path.GetFileName(_filePaths[i]) + Path.DirectorySeparatorChar, position + (new Vector2(0, 5) * EditorMain.ScaleModifier) + new Vector2(0, (i + 1) * _spacing) * EditorMain.ScaleModifier, Color.RoyalBlue);
                }
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

        if (Input.IsKeyDown(Keys.LeftControl))
        {
            for (int i = Text.Length - 1; i > 0; i--)
            {
                if (Text[i - 1] == Path.DirectorySeparatorChar)
                {
                    SetText(Text.Remove(i));
                    AddToCharIndex(Text.Length - i);
                    return;
                }
                
                if (i == 0)
                {
                    SetText("");
                    SetCharIndex(0);
                    return;
                }
            }
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
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            if (!File.Exists(Text))
            {
                using (var fileStream = File.Create(Text)) {}
                NotificationManager.CreateNotification($"Created new file at: {Text}", 5);
            }
        }
        
        InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
        IsOpened = false;
        CharIndex = 0;
        EditorMain.LoadFile(Text);
    }

    public static void HandleTab()
    {
        if (_filePaths.Length == 0) return;
        int lineIndex = LineIndex;

        SetText(_filePaths[lineIndex]);
        if (!File.Exists(_filePaths[lineIndex]))
        {
            SetText(Text + Path.DirectorySeparatorChar);
        }

        AddToCharIndex(_filePaths[lineIndex].Length);
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
        
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            if (Input.IsKeyPressed(Keys.T))
            {
                Close();
                Terminal.Toggle();
                InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Terminal);
            }
            
            if (Input.IsKeyDown(Keys.K) && _keyTimer <= 0)
            {
                LineIndex++;
                if (LineIndex > _filePaths.Length - 1)
                {
                    LineIndex--;
                }
                
                ResetKeyTimer();
                _keyPressed = true;
            }
            
            if (Input.IsKeyDown(Keys.I) && _keyTimer <= 0)
            {
                LineIndex--;
                if (LineIndex <= 0)
                {
                    LineIndex = 0;
                }
               
                ResetKeyTimer();
                _keyPressed = true;
            }
        }
        
        if (Input.IsKeyPressed(Keys.Escape))
        {
            Close();
            InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
        }

        if (!Input.IsKeyDown(Keys.Right) &&
            !Input.IsKeyDown(Keys.Left) &&
            !Input.IsKeyDown(Keys.Up) &&
            !Input.IsKeyDown(Keys.Down) &&
            !Input.IsKeyDown(Keys.X) &&
            !Input.IsKeyDown(Keys.K) &&
            !Input.IsKeyDown(Keys.I))
        {
            _keyPressed = false;
            _keyTimer = 0;
        }

        _keyTimer -= Time.DeltaTime;
    }

    public static void Open()
    {
        if (Text == String.Empty) SetText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (File.Exists(Text)) SetText(Directory.GetParent(Text).ToString() + Path.DirectorySeparatorChar);
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

    public static string TryGetSolutionDirectoryInfo(string currentPath = null)
    {
        var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }
        
        if (directory == null) return null;
        return directory.FullName;
    }
}