using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class Updater
{
    public static void Start(GameWindow gameWindow)
    {
        EditorMain.Start(gameWindow);
        FileDialog.Start(gameWindow);
    }

    public static void Update(GameTime gameTime)
    {
        Input.Update();
        Time.Update(gameTime);
        InputHandler.Update();
        InputDistributor.HandleKeybinds();
        Input.SwitchStates();
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        EditorMain.Draw(spriteBatch);
        FileDialog.Draw(spriteBatch);
    }
}