using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public static class Updater
{
    public static void Start(GameWindow gameWindow)
    {
        UISystem.Start(gameWindow);
    }

    public static void Update(GameTime gameTime)
    {
        Input.Update();
        Time.Update(gameTime);
        UISystem.Update(gameTime);
        Input.SwitchStates();
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        UISystem.Draw(spriteBatch);
    }
}