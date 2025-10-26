using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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

        EditorMain.SetSelectedLine(EditorMain.Line.Insert(EditorMain.CharIndex, pressedKeys));
        EditorMain.AddToCharIndex(pressedKeys.Length);

        if (pressedKeys.Contains(')') ||
            pressedKeys.Contains('}') ||
            pressedKeys.Contains(']'))
        {
            EditorMain.AddToCharIndex(-1);
        }

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
        if (EditorMain.CharIndex == 0)
        {
            if (EditorMain.LineIndex != 0)
            {
                string temp = EditorMain.Line;
                EditorMain.RemoveLine(EditorMain.LineIndex);
                EditorMain.AddToLineIndex(-1);
                EditorMain.SetSelectedLine(EditorMain.Line + temp);
                EditorMain.SetCharIndex(EditorMain.LineLength);
            }

            return;
        }
        
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = KeybindHandler.NextControlLeftArrowIndex(EditorMain.CharIndex, EditorMain.Line);
            EditorMain.SetSelectedLine(EditorMain.Line.Remove(nextIndex, EditorMain.CharIndex - nextIndex));
            return;
        }

        EditorMain.SetSelectedLine(EditorMain.Line.Remove(EditorMain.CharIndex - 1, 1));
    }

    private static void HandleTab()
    {
        if (Input.IsKeyDown(Keys.LeftShift))
        {
            if (EditorMain.CharIndex != 0)
            {
                if (EditorMain.LineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (EditorMain.Line[i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    EditorMain.SetSelectedLine(EditorMain.Line.Substring(spaces, EditorMain.LineLength - spaces));
                    EditorMain.AddToCharIndex(-spaces);
                }
            }
            else
            {
                if (EditorMain.LineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (EditorMain.Line[i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    EditorMain.SetSelectedLine(EditorMain.Line.Substring(spaces, EditorMain.LineLength - spaces));
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
        string insert = EditorMain.Line.Substring(EditorMain.CharIndex, EditorMain.LineLength - EditorMain.CharIndex);
        EditorMain.SetSelectedLine(EditorMain.Line.Substring(0, EditorMain.CharIndex));
        EditorMain.Lines.Insert(EditorMain.LineIndex + 1, insert);
        EditorMain.AddToLineIndex(1);
        EditorMain.SetCharIndex(0);
    }
}