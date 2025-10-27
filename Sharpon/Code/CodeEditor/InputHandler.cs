using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public static class InputHandler
{
    private static GameWindow _gameWindow;
    private static List<char> _charQueue = new List<char>();

    private static bool _suppressQoutationMark = false;
        
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

                case '"':
                    if (_suppressQoutationMark) { _suppressQoutationMark = false; break; }
                    else _suppressQoutationMark = true;
                    _charQueue.Add('"');
                    break;
            }
        }

        EditorMain.SetSelectedLine(EditorMain.Line.Insert(EditorMain.CharIndex, pressedKeys));
        EditorMain.AddToCharIndex(pressedKeys.Length);

        if (pressedKeys.Contains(')') ||
            pressedKeys.Contains('}') ||
            pressedKeys.Contains(']') ||
            pressedKeys.Contains('"') && !_suppressQoutationMark)
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
                EditorMain.SetCharIndex(EditorMain.LineLength);
                EditorMain.SetSelectedLine(EditorMain.Line + temp);
            }

            return;
        }

        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = KeybindHandler.NextControlLeftArrowIndex(EditorMain.CharIndex, EditorMain.Line);
            EditorMain.SetSelectedLine(EditorMain.Line.Remove(nextIndex, EditorMain.CharIndex - nextIndex));
            EditorMain.SetCharIndex(nextIndex);
            return;
        }

        if (EditorMain.CharIndex != EditorMain.LineLength)
        {
            if (EditorMain.Line[EditorMain.CharIndex] == '}' ||
                EditorMain.Line[EditorMain.CharIndex] == ')' ||
                EditorMain.Line[EditorMain.CharIndex] == ']' ||
                EditorMain.Line[EditorMain.CharIndex] == '"')
            {
                EditorMain.SetSelectedLine(EditorMain.Line.Remove(EditorMain.CharIndex - 1, 2));
                if (EditorMain.Line[EditorMain.CharIndex - 1] != ' ') EditorMain.AddToCharIndex(-1);
                return;
            }
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
        bool tab = false;
        string insert = EditorMain.Line.Substring(EditorMain.CharIndex, EditorMain.LineLength - EditorMain.CharIndex);
        if (EditorMain.CharIndex != 0)
        {
            if (EditorMain.Line[EditorMain.CharIndex - 1] == '{')
            {
                tab = true;
            }
        }

        int spaces = 0;
        for (int i = 0; i < EditorMain.LineLength; i++)
        {
            if (EditorMain.Line[i] != ' ') break;
            spaces++;
        }

        string spacesString = "";
        for (int i = 0; i < spaces; i++)
        {
            spacesString += " ";
        }

        if (insert != String.Empty)
        {
            if (insert[0] == '}')
            {
                HandleBackspace();
                if (EditorMain.Line.Length > 0) EditorMain.SetSelectedLine(EditorMain.Line.Remove(EditorMain.Line.Length - insert.Length));
                EditorMain.Lines.Insert(EditorMain.LineIndex + 1, spacesString + '{');
                EditorMain.Lines.Insert(EditorMain.LineIndex + 2, spacesString);
                EditorMain.Lines.Insert(EditorMain.LineIndex + 3, spacesString + insert);
                EditorMain.AddToLineIndex(2);
                HandleTab();

                EditorMain.SetCharIndex(EditorMain.CharIndex);
                return;
            }

        }
        
        EditorMain.SetSelectedLine(EditorMain.Line.Substring(0, EditorMain.CharIndex));
        EditorMain.Lines.Insert(EditorMain.LineIndex + 1, spacesString + insert);
        EditorMain.AddToLineIndex(1);
        EditorMain.SetCharIndex(spaces);
        if (tab) HandleTab();
    }
}