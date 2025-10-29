using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public static class FileDialog
{
    public static bool IsOpened = false;
    public static string Text { get; private set; } = "Katzi";
    public static int CharIndex { get; private set; } = Text.Length;

    private static GameWindow _gameWindow;
    private static Vector2 _cursorPosition;

    private static float _keyTimer = 0;
    private static bool _keyPressed = false;
    private static float _baseKeyTimer = 0.2f;
    private static float _baseFastKeyTimer = 0.04f;

    public static void Start(GameWindow window)
    {
        _gameWindow = window;
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        if (!IsOpened) return;
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        Vector2 position = new Vector2(_gameWindow.ClientBounds.Width / 2 - (font.MeasureString(Text).X * EditorMain.ScaleModifier / 2),
                                       _gameWindow.ClientBounds.Height / 2 - (font.MeasureString("|").Y * EditorMain.ScaleModifier / 2));

        spriteBatch.DrawString(font, Text, position, Color.White);

        int cursorSpeed = 50;
        _cursorPosition = new Vector2(MathHelper.Lerp(_cursorPosition.X, (position.X + font.MeasureString(Text.Substring(0, CharIndex)).X) / EditorMain.ScaleModifier - font.MeasureString("|").X / 2, cursorSpeed * Time.DeltaTime),
                                      position.Y / EditorMain.ScaleModifier);

        spriteBatch.DrawString(font, "|", _cursorPosition * EditorMain.ScaleModifier, Color.White);
    }

    public static void SetText(string line)
    {
        Text = line;
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

        Text = Text.Remove(CharIndex - 1, 1);
        AddToCharIndex(-1);
    }

    public static void HandleEnter()
    {
        InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
        IsOpened = false;
        CharIndex = 0;
        EditorMain.LoadFile(Text);
        Text = "";
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

    public static void Toggle()
    {
        IsOpened = !IsOpened;
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
}