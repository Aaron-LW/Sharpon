using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using System.Collections.Generic;

public static class KeybindScreen
{
    private static List<string> _lines = new List<string>()
    {
        "Ctrl+Shift+P: Open file selector",
        "--When in file selector--",
        "Enter: Open file at specified filepath",
        "Ctrl+Enter: Create file at specified filepath",
        "Ctrl+I/K/Arrow keys: Move up/down",
        "",
        "Ctrl+T: Open integrated terminal",
        "Ctrl+X: Remove entire line",
        "Ctrl+S: Save file",
        "Ctrl+R: Reload file",
        "Ctrl+B: Move to next closing bracket",
        "Ctrl+F: Toggle search",
        "Ctrl+Shift+F: Remove all text in search and close",
        "Ctrl+M: Open this keybinds menu",
        "",
        "Escape/Capslock: Switch editing modes (Editing = white cursor, Moving = orange cursor)",
        "--While in moving mode--",
        "I/K/J/L: Move up/down/left/right",
        "Q/E: Move one character left/right",
        "W/E: Move up/down quickly",
        "You cant type in moving mode"
    };
    private static Vector2 _basePosition = new Vector2(50, 50);
    private static int _spacing = 20;
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        
        for (int i = 0; i < _lines.Count; i++)
        {
            spriteBatch.DrawString(font, _lines[i], _basePosition + new Vector2(0, (i * _spacing) * EditorMain.ScaleModifier), Color.White);
        }
    }
}