using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.IO;
using System;
using MonoGame.Extended.Graphics;

public static class UISystem
{
    public static float BaseFontSize = 20;
    public static float ScaleModifier => MathF.Round((float)_gameWindow.ClientBounds.Width / 1920, 2);

    private static FontSystem _fontSystem = new FontSystem();
    private static GameWindow _gameWindow;
    private static SimpleFps _fps = new SimpleFps();
    private static string _text;
        
    public static void Start(GameWindow gameWindow)
    {
        string fontPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fonts", "JetBrainsMonoNLNerdFont-Bold.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        _gameWindow = gameWindow;
    }

    public static void Update(GameTime gameTime)
    {
        _fps.Update(gameTime);

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

        _text = Input.GetPressedKeys(_text);
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = _fontSystem.GetFont(BaseFontSize * ScaleModifier);
        spriteBatch.DrawString(font, _text, new Vector2(100, 100) * ScaleModifier, Color.White);
        spriteBatch.DrawString(font, _fps.msg, new Vector2(50, 50) * ScaleModifier, Color.White);
        _fps.frames++;
    }
}