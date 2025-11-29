using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class Updater
{
    public static bool KeybindsMenu = false;
    
    public static void Start(GameWindow gameWindow)
    {
        EditorMain.Start(gameWindow);
        FileDialog.Start(gameWindow);
        NotificationManager.Start(gameWindow);
        Terminal.Start(gameWindow);
        Finder.Start(gameWindow);
        PlayTimeCounter.Start(gameWindow);
    }

    public static void Update(GameTime gameTime)
    {
        Input.Update();
        Time.Update(gameTime);
        InputHandler.Update();
        FileDialog.Update();
        NotificationManager.Update();
        PlayTimeCounter.Update();
        Input.SwitchStates();
    }

    public static void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        if (!KeybindsMenu) EditorMain.Draw(spriteBatch);
        else KeybindScreen.Draw(spriteBatch);
        FileDialog.Draw(spriteBatch);
        Terminal.Draw(spriteBatch, graphicsDevice);
        NotificationManager.Draw(spriteBatch);
        Finder.Draw(spriteBatch);
        PlayTimeCounter.Draw(spriteBatch);
    }
}