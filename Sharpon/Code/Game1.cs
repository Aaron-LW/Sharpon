using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sharpon;

public class Game1 : Game
{
    private SimpleFps _fps = new SimpleFps();
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Color _backgroundColor = new Color(30, 28, 37);

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 180.0);
        _graphics.SynchronizeWithVerticalRetrace = false;
        IsFixedTimeStep = true;

        _graphics.ApplyChanges();

        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        InputHandler.Start(Window);
        Updater.Start(Window);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _fps.Update(gameTime);
        Updater.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();


        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_backgroundColor);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize);

        _spriteBatch.DrawString(font, _fps.msg, new Vector2((Window.ClientBounds.Width - font.MeasureString(_fps.msg).X - 20) * EditorMain.ScaleModifier, 20), Color.White);
        Updater.Draw(_spriteBatch);

        _fps.frames++;

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
