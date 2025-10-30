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
        foreach (char e in _charQueue)
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
        
        InputDistributor.SetSelectedLine(InputDistributor.SelectedLine.Insert(InputDistributor.CharIndex, pressedKeys));
        InputDistributor.AddToCharIndex(pressedKeys.Length);

        if (pressedKeys.Contains(')') ||
            pressedKeys.Contains('}') ||
            pressedKeys.Contains(']') ||
            pressedKeys.Contains('"') && !_suppressQoutationMark)
        {
            InputDistributor.AddToCharIndex(-1);
        }
    }
    
    public static void AddToCharQueue(char character)
    {
        _charQueue.Add(character);
    }
    
    private static void TextInputHandler(object sender, TextInputEventArgs e)
    {
        char c = e.Character;
        if (char.IsControl(c))
        {
            if (c == '\b') InputDistributor.HandleBackspace();
            else if (c == '\r' || c == '\n') InputDistributor.HandleEnter();
            else if (c == '\t') InputDistributor.HandleTab();
            return;
        }
        else
        {
            _charQueue.Add(e.Character);
        }
    }
}