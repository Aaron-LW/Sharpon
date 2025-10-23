using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

public static class UISystem
{
    public static float BaseFontSize = 20;
    public static float ScaleModifier => MathF.Round((float)_gameWindow.ClientBounds.Width / 1920, 2);

    private static FontSystem _fontSystem = new FontSystem();
    private static GameWindow _gameWindow;
    private static SimpleFps _fps = new SimpleFps();
    private static float _lineSpacing => (float)(1 * BaseFontSize);
    private static Queue<char> _charQueue = new Queue<char>();

    public static List<string> Lines = new List<string>() { "" };
    public static int LineIndex = 0;
        
    public static void Start(GameWindow gameWindow)
    {
        string fontPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fonts", "JetBrainsMonoNLNerdFont-Bold.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        _gameWindow = gameWindow;
        _gameWindow.TextInput += TextInputHandler;
    }

    public static void Update(GameTime gameTime)
    {
        _fps.Update(gameTime);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char? character in _charQueue)
        {
            stringBuilder.Append(character);
        }
        string pressedKeys = stringBuilder.ToString();

        if (Input.IsKeyDown(Keys.Right))
        {
            BaseFontSize += 100 * Time.DeltaTime;
        }
        if (Input.IsKeyDown(Keys.Left))
        {
            if (BaseFontSize > 1)
            {
                BaseFontSize -= 100 * Time.DeltaTime;
            }

            if (BaseFontSize < 1)
            {
                BaseFontSize = 1;
            }
        }

        if (Input.IsKeyPressed(Keys.Down))
        {
            if (LineIndex < Lines.Count - 1)
            {
                LineIndex++;
            }
        }

        if (Input.IsKeyPressed(Keys.Up))
        {
            if (LineIndex > 0)
            {
                LineIndex--;
            }
        }

        if (Input.IsKeyPressed(Keys.Enter))
        {
            Lines.Insert(LineIndex + 1, "");
            LineIndex++;
        }

        
        Lines[LineIndex] += pressedKeys;
        _charQueue.Clear();
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = _fontSystem.GetFont(BaseFontSize * ScaleModifier);

        for (int i = 0; i < Lines.Count; i++)
        {
            spriteBatch.DrawString(font, Lines[i], new Vector2(100, 100 + (i * _lineSpacing)) * ScaleModifier, Color.White);
        }

        spriteBatch.DrawString(font, _fps.msg, new Vector2(20, 20) * ScaleModifier, Color.White);
        _fps.frames++;

        spriteBatch.DrawString(font, $"Lines: {Lines.Count}", new Vector2(150, 20) * ScaleModifier, Color.White);
        spriteBatch.DrawString(font, $"Current Line: {LineIndex}", new Vector2(250, 20) * ScaleModifier, Color.White);
    }


    private static void TextInputHandler(object sender, TextInputEventArgs e)
    {
        char c = e.Character;
        if (char.IsControl(c))
        {
            if (c == '\b') HandleBackspace();
            //else if (c == '\r' || c == '\n') HandleEnter();
            //else if (c == '\t') HandleTab();
            return;
        }
        else
        {
            _charQueue.Enqueue(e.Character);
        }
    }

    private static void HandleBackspace()
    {
        if (Lines[LineIndex].Length > 0)
        {
            Lines[LineIndex] = Lines[LineIndex].Substring(0, Lines[LineIndex].Length - 1);
        }
    }
}