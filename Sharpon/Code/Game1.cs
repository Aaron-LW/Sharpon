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

    public Game1(string fileToOpen = null)
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 180.0);
        _graphics.SynchronizeWithVerticalRetrace = true;
        IsFixedTimeStep = true;

        _graphics.ApplyChanges();

        Window.AllowUserResizing = true;
        
        if (fileToOpen != null) EditorMain.Start(Window, fileToOpen);
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
        base.Update(gameTime);
        Mouse.SetCursor(MouseCursor.No);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_backgroundColor);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);

        _spriteBatch.DrawString(font, EditorMain.FilePath, new Vector2((Window.ClientBounds.Width - font.MeasureString(EditorMain.FilePath).X - 200), 20), Color.White);
        _spriteBatch.DrawString(font, _fps.msg, new Vector2((Window.ClientBounds.Width - font.MeasureString(_fps.msg).X - 20), 20), Color.White);

        if (EditorMain.UnsavedChanges) _spriteBatch.DrawString(font, "Unsaved changes",
                                new Vector2((Window.ClientBounds.Width - font.MeasureString(EditorMain.UnsavedChanges.ToString()).X - font.MeasureString(EditorMain.FilePath).X - 350), 20), Color.White);

        Updater.Draw(_spriteBatch, GraphicsDevice);

        _fps.frames++;

        _spriteBatch.End();
        //base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        base.OnExiting(sender, args);
        
        PlayTimeCounter.SavePlayTime();
        Terminal.Stop();
    }
}
