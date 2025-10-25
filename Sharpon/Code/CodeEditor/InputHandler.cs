using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Particles.Modifiers;

public static class InputHandler
{
    private static GameWindow _gameWindow;
    private static List<char> _charQueue = new List<char>();
        
    public static void Start(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
        _gameWindow.TextInput += TextInputHandler;
    }

    public static void Update()
    {
        int charIndex = UISystem.CharIndex;
        List<string> lines = UISystem.Lines;
        string line = lines[UISystem.LineIndex];
        int lineIndex = UISystem.LineIndex;

        StringBuilder stringBuilder = new StringBuilder();
        foreach(char e in _charQueue)
        {
            stringBuilder.Append(e);
        }

        string pressedKeys = stringBuilder.ToString();
        _charQueue.Clear();

        for (int i = 0; i < pressedKeys.Length; i++)
        {
            switch (pressedKeys[i])
            {
                case '(':
                    _charQueue.Add(')');
                    break;

                case '{':
                    _charQueue.Add('}');
                    break;

                case '[':
                    _charQueue.Add(']');
                    break;
            }
        }

        line = line.Insert(charIndex, pressedKeys);
        charIndex += pressedKeys.Length;

        if (pressedKeys.Contains(')') ||
            pressedKeys.Contains('}') ||
            pressedKeys.Contains(']'))
        {
            charIndex--;
        }

        WriteToUISystem(line, lineIndex, lineIndex, charIndex);
        KeybindHandler.HandleKeybinds();
    }

    private static void TextInputHandler(object sender, TextInputEventArgs e)
    {
        char c = e.Character;
        if (char.IsControl(c))
        {
            if (c == '\b') HandleBackspace();
            else if (c == '\r' || c == '\n') HandleEnter();
            else if (c == '\t') HandleTab();
            return;
        }
        else
        {
            _charQueue.Add(e.Character);
        }
    }

    private static void HandleBackspace()
    {
        string line = UISystem.Lines[UISystem.LineIndex];
        int charIndex = UISystem.CharIndex;
        int lineIndex = UISystem.LineIndex;

        if (charIndex == 0)
        {
            if (lineIndex != 0)
            {
                string temp = line;
                UISystem.Lines.RemoveAt(lineIndex);
                lineIndex--;
                charIndex = UISystem.Lines[lineIndex].Length;
                UISystem.Lines[lineIndex] += temp;

                WriteToUISystem(UISystem.Lines[lineIndex], lineIndex, lineIndex, charIndex);
            }

            charIndex = VerifyCharIndex(charIndex, UISystem.Lines[lineIndex]);
            return;
        }
        
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = KeybindHandler.NextControlLeftArrowIndex(charIndex, line);
            line = UISystem.Lines[lineIndex].Remove(nextIndex, charIndex - nextIndex);
            charIndex = VerifyCharIndex(nextIndex, line);

            WriteToUISystem(line, lineIndex, lineIndex, charIndex);
            return;
        }

        line = line.Remove(charIndex - 1, 1);
        charIndex--;

        WriteToUISystem(line, lineIndex, lineIndex, charIndex);
    }

    private static void HandleTab()
    {
        string line = UISystem.Lines[UISystem.LineIndex];
        int charIndex = UISystem.CharIndex;
        int lineIndex = UISystem.LineIndex;
        int lineLength = line.Length;

        if (Input.IsKeyDown(Keys.LeftShift))
        {
            if (charIndex != 0)
            {
                if (lineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (UISystem.Lines[lineIndex][i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    UISystem.Lines[lineIndex] = UISystem.Lines[lineIndex].Substring(spaces, lineLength - spaces);
                    charIndex -= spaces;
                }

                UISystem.CharIndex = charIndex;
            }
            else
            {
                if (lineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (UISystem.Lines[lineIndex][i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    UISystem.Lines[lineIndex] = UISystem.Lines[lineIndex].Substring(spaces, lineLength - spaces);
                }
            }

        }
        else
        {
            _charQueue.Add(' ');
            _charQueue.Add(' ');
            _charQueue.Add(' ');
            _charQueue.Add(' ');
        }

    }

    private static void HandleEnter()
    {
        int lineIndex = UISystem.LineIndex;
        string line = UISystem.Lines[lineIndex];
        int charIndex = UISystem.CharIndex;

        string insert = line.Substring(charIndex, line.Length - charIndex);
        line = line.Substring(0, charIndex);
        UISystem.Lines.Insert(lineIndex + 1, insert);
        lineIndex++;

        charIndex = 0;
        WriteToUISystem(line, lineIndex - 1, lineIndex, charIndex);
    }

    public static void WriteToUISystem(string line, int lineIndex, int newLineIndex, int charIndex)
    {
        UISystem.Lines[lineIndex] = line;
        UISystem.LineIndex = newLineIndex;
        UISystem.CharIndex = charIndex;
    }

    public static int VerifyCharIndex(int charIndex, string line)
    {
        if (charIndex > line.Length)
        {
            charIndex = line.Length;
        }

        if (charIndex < 0)
        {
            charIndex = 0;
        }

        return charIndex;
    }
}