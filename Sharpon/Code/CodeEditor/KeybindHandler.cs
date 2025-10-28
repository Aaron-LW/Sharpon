using System;
using System.Data;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Primitives;

public static class KeybindHandler
{
    private static float _keyTimer = 0;
    private static bool _keyPressed = false;

    private static float _baseKeyTimer = 0.2f;
    private static float _baseFastKeyTimer = 0.04f;

    public static void HandleKeybinds()
    {
        if (Input.IsKeyDown(Keys.Right) && _keyTimer <= 0)
        {
            if (EditorMain.CharIndex != EditorMain.LineLength)
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    EditorMain.SetCharIndex(NextControlRightArrowIndex(EditorMain.CharIndex, EditorMain.Line));
                }
                else
                {
                    EditorMain.AddToCharIndex(1);
                }
            }

            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Left) && _keyTimer <= 0)
        {
            if (EditorMain.CharIndex != 0)
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    EditorMain.SetCharIndex(NextControlLeftArrowIndex(EditorMain.CharIndex, EditorMain.Line));
                }
                else
                {
                    EditorMain.AddToCharIndex(-1);
                }
            }

            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Up) && _keyTimer <= 0)
        {
            if (EditorMain.LineIndex != 0)
            {
                EditorMain.AddToLineIndex(-1);
                EditorMain.SetCharIndex(EditorMain.LineLength);
            }

            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Down) && _keyTimer <= 0)
        {
            if (EditorMain.LineIndex != EditorMain.Lines.Count - 1)
            {
                EditorMain.AddToLineIndex(1);
                EditorMain.SetCharIndex(EditorMain.LineLength);
            }

            ResetKeyTimer();
            _keyPressed = true;
        }
        
        if (Input.IsKeyDown(Keys.LeftControl) && _keyTimer <= 0)
        {
            if (Input.IsKeyDown(Keys.X))
            {
                
                EditorMain.RemoveLine(EditorMain.LineIndex);
            }

            if (Input.IsKeyPressed(Keys.S))
            {
                EditorMain.SaveFile("/media/C#/test/Program.cs");
            }

            if (Input.IsKeyPressed(Keys.L))
            {
                EditorMain.LoadFile("/media/C#/test/Program.cs");
            }

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

    public static int NextControlRightArrowIndex(int startIndex, string line)
    {
        if (line[startIndex] == ' ')
        {
            for (int i = startIndex; i < line.Length; i++)
            {
                if (line[i] != ' ') return i;
            }
        }
        else
        {
            for (int i = startIndex + 1; i < line.Length; i++)
            {
                if (line[i] == ' ' || line[i] == '.' || line[i] == ',') return i;
            }
        }

        return line.Length;
    }

    public static int NextControlLeftArrowIndex(int startIndex, string line)
    {
        startIndex--;

        if (line[startIndex] == ' ')
        {
            //Search for letters
            for (int i = startIndex - 1; i > 0; i--)
            {
                if (line[i] != ' ') return i + 1;
                if (line[i] == '.' || line[i] == ',') return i + 1;
            }
        }
        else if (line[startIndex] != ' ')
        {
            //Search for spaces
            for (int i = startIndex - 1; i > 0; i--)
            {
                if (line[i] == ' ') return i + 1;
                if (line[i] == '.' || line[i] == ',') return i + 1;
            }
        }

        return 0;
    }
}