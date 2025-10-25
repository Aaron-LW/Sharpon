using System;
using System.Data;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Particles.Modifiers;

public static class KeybindHandler
{
    private static float _keyTimer = 0;
    private static bool _keyPressed = false;

    private static float _baseKeyTimer = 0.2f;
    private static float _baseFastKeyTimer = 0.04f;

    public static void HandleKeybinds()
    {
        int charIndex = UISystem.CharIndex;
        int lineIndex = UISystem.LineIndex;
        string line = UISystem.Lines[lineIndex];
        int lineLength = line.Length;

        if (Input.IsKeyDown(Keys.Right) && _keyTimer <= 0)
        {
            if (charIndex != lineLength)
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    charIndex = NextControlRightArrowIndex(charIndex, line);
                }
                else
                {
                    charIndex++;
                }
            }

            charIndex = InputHandler.VerifyCharIndex(charIndex, line);
            ResetKeyTimer();
            _keyPressed = true;
            InputHandler.WriteToUISystem(line, lineIndex, lineIndex, charIndex);
        }

        if (Input.IsKeyDown(Keys.Left) && _keyTimer <= 0)
        {
            if (charIndex != 0)
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    charIndex = NextControlLeftArrowIndex(charIndex, line);
                }
                else
                {
                    charIndex--;
                }
            }

            charIndex = InputHandler.VerifyCharIndex(charIndex, line);
            ResetKeyTimer();
            _keyPressed = true;
            InputHandler.WriteToUISystem(line, lineIndex, lineIndex, charIndex);
        }

        if (Input.IsKeyDown(Keys.Up) && _keyTimer <= 0)
        {
            int lineIndexAdjust = 0;

            if (lineIndex != 0)
            {
                lineIndex--;
                charIndex = UISystem.Lines[lineIndex].Length;
                lineIndexAdjust = 1;
            }

            charIndex = InputHandler.VerifyCharIndex(charIndex, UISystem.Lines[lineIndex]);
            ResetKeyTimer();
            _keyPressed = true;
            InputHandler.WriteToUISystem(line, lineIndex + lineIndexAdjust, lineIndex, charIndex);
        }

        if (Input.IsKeyDown(Keys.Down) && _keyTimer <= 0)
        {
            int lineIndexAdjust = 0;

            if (lineIndex != UISystem.Lines.Count - 1)
            {
                lineIndex++;
                charIndex = UISystem.Lines[lineIndex].Length;
                lineIndexAdjust = 1;
            }

            charIndex = InputHandler.VerifyCharIndex(charIndex, UISystem.Lines[lineIndex]);
            ResetKeyTimer();
            _keyPressed = true;
            InputHandler.WriteToUISystem(line, lineIndex - lineIndexAdjust, lineIndex, charIndex);
        }
        
        if (Input.IsKeyDown(Keys.LeftControl) && _keyTimer <= 0)
        {
            if (Input.IsKeyDown(Keys.X))
            {
                if (UISystem.Lines.Count > 1)
                {
                    UISystem.Lines.RemoveAt(lineIndex);
                    if (UISystem.Lines.Count == lineIndex)
                    {
                        lineIndex = UISystem.Lines.Count - 1;
                    }
                }
                else
                {
                    UISystem.Lines[0] = "";

                }

                charIndex = InputHandler.VerifyCharIndex(charIndex, UISystem.Lines[lineIndex]);
                UISystem.CharIndex = charIndex;
                UISystem.LineIndex = lineIndex;
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
            for (int i = startIndex; i < line.Length; i++)
            {
                if (line[i] == ' ') return i;
            }
        }

        return line.Length;
    }

    public static int NextControlLeftArrowIndex(int startIndex, string line)
    {
        if (startIndex == line.Length && line.Length > 1)
        {
            startIndex--;
        }

        if (line[startIndex - 1] == ' ')
        {
            for (int i = startIndex - 1; i > 0; i--)
            {
                if (line[i] != ' ') return i + 1;
            }
        }
        else
        {
            for (int i = startIndex - 1; i > 0; i--)
            {
                if (line[i] == ' ') return i + 1;
            }
        }

        return 0;
    }
}