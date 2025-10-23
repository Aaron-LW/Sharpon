using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Linq;

public static class UISystem
{
    public static float BaseFontSize = 20;
    public static float ScaleModifier => MathF.Round((float)_gameWindow.ClientBounds.Width / 1920, 2);

    private static FontSystem _fontSystem = new FontSystem();
    private static GameWindow _gameWindow;
    private static SimpleFps _fps = new SimpleFps();
    private static float _lineSpacing => (float)(1 * BaseFontSize);
    private static Queue<char> _charQueue = new Queue<char>();
    private static float _arrowKeyTimer = 0;
    private static bool _arrowKeyPressed = false;

    public static List<string> Lines = new List<string>() { "" };
    public static int LineIndex = 0;
    public static int CharIndex = 0;
        
    public static void Start(GameWindow gameWindow)
    {
        string fontPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fonts", "JetBrainsMonoNLNerdFont-Bold.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        _gameWindow = gameWindow;
        _gameWindow.TextInput += TextInputHandler;
    }

    public static void Update(GameTime gameTime)
    {
        _fps.Update(gameTime);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char? character in _charQueue)
        {
            stringBuilder.Append(character);
        }
        string pressedKeys = stringBuilder.ToString();


        if (Input.IsKeyDown(Keys.Right) && _arrowKeyTimer <= 0)
        {
            if (CharIndex < Lines[LineIndex].Length)
            {
                int charIndex = CharIndex + 1;
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    for (int i = CharIndex + 1; i < Lines[LineIndex].Length; i++)
                    {
                        if (Lines[LineIndex][i] == ' ')
                        {
                            while (Lines[LineIndex][i] == ' ' && i < Lines[LineIndex].Length)
                            {
                                i++;
                            }

                            charIndex = i;
                            break;
                        }

                        if (i == Lines[LineIndex].Length - 1)
                        {
                            charIndex = Lines[LineIndex].Length;
                            break;
                        }
                    }
                }


                CharIndex = charIndex;
                if (_arrowKeyPressed)
                {
                    _arrowKeyTimer = 0.045f;
                }
                else
                {
                    _arrowKeyTimer = 0.12f;
                }
                _arrowKeyPressed = true;
            }
        }

        if (Input.IsKeyDown(Keys.Left) && _arrowKeyTimer <= 0)
        {
            if (CharIndex > 0)
            {
                int charIndex = CharIndex - 1;
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    for (int i = CharIndex - 1; i > 0; i--)
                    {
                        if (Lines[LineIndex][i] == ' ')
                        {
                            while (Lines[LineIndex][i] == ' ' && i > 0)
                            {
                                i--;
                            }

                            if (i != 0)
                            {
                                i++;
                            }

                            charIndex = i;
                            break;
                        }

                        if (i == 1)
                        {
                            charIndex = 0;
                            break;
                        }
                    }
                }

                CharIndex = charIndex;
                if (_arrowKeyPressed)
                {
                    _arrowKeyTimer = 0.045f;
                }
                else
                {
                    _arrowKeyTimer = 0.12f;
                }
                _arrowKeyPressed = true;
            }
        }


        if (Input.IsKeyDown(Keys.Down) && _arrowKeyTimer <= 0)
        {
            if (LineIndex < Lines.Count - 1)
            {
                LineIndex++;
                if (CharIndex > Lines[LineIndex].Length)
                {
                    CharIndex = Lines[LineIndex].Length;
                }

                if (_arrowKeyPressed)
                {
                    _arrowKeyTimer = 0.045f;
                }
                else
                {
                    _arrowKeyTimer = 0.3f;
                }
                _arrowKeyPressed = true;
            }
        }

        if (Input.IsKeyDown(Keys.Up) && _arrowKeyTimer <= 0)
        {
            if (LineIndex > 0)
            {
                LineIndex--;
            }

            if (CharIndex > Lines[LineIndex].Length)
            {
                CharIndex = Lines[LineIndex].Length;
            }

            if (_arrowKeyPressed)
            {
                _arrowKeyTimer = 0.045f;
            }
            else
            {
                _arrowKeyTimer = 0.3f;
            }
            _arrowKeyPressed = true;
        }

        if (!Input.IsKeyDown(Keys.Right) && !Input.IsKeyDown(Keys.Left) && !Input.IsKeyDown(Keys.Up) && !Input.IsKeyDown(Keys.Down) && !Input.IsKeyDown(Keys.X))
        {
            _arrowKeyPressed = false;
            _arrowKeyTimer = 0;
        }

        if (Input.IsKeyPressed(Keys.Enter))
        {
            string insert = "";
            if (Lines[LineIndex].Length > CharIndex)
            {
                insert = Lines[LineIndex].Substring(CharIndex, Lines[LineIndex].Length - CharIndex);
                Lines[LineIndex] = Lines[LineIndex].Substring(0, CharIndex);
            }
            Lines.Insert(LineIndex + 1, insert);
            LineIndex++;
            CharIndex = 0;
        }


        if (Input.IsKeyDown(Keys.LeftControl))
        {
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (pressedKeys[i] == '+')
                {
                    BaseFontSize += 1;
                    break;
                }

                if (pressedKeys[i] == '-')
                {
                    BaseFontSize -= 1;
                    break;
                }

                if (pressedKeys[i] == '#')
                {
                    LoadFile("/media/MonoGame/beemoviepart.txt");
                }
            }

            if (Input.IsKeyDown(Keys.X) && _arrowKeyTimer <= 0)
            {
                if (Lines.Count > 1)
                {
                    Lines.RemoveAt(LineIndex);
                    if (CharIndex > Lines[LineIndex].Length)
                    {
                        CharIndex = Lines[LineIndex].Length;
                    }
                }
                
                if (_arrowKeyPressed)
                {
                    _arrowKeyTimer = 0.045f;
                }
                else
                {
                    _arrowKeyTimer = 0.3f;
                }
                _arrowKeyPressed = true;
            }

            pressedKeys = String.Empty;
        }


        Lines[LineIndex] = Lines[LineIndex].Insert(CharIndex, pressedKeys);
        CharIndex += pressedKeys.Length;
        _charQueue.Clear();

        _arrowKeyTimer -= Time.DeltaTime;
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = _fontSystem.GetFont(BaseFontSize * ScaleModifier);

        for (int i = 0; i < Lines.Count; i++)
        {
            spriteBatch.DrawString(font, Lines[i], new Vector2(100, 100 + (i * _lineSpacing)) * ScaleModifier, Color.White);
        }
        spriteBatch.DrawString(font, "|", new Vector2(100 + font.MeasureString(Lines[LineIndex].Substring(0, CharIndex)).X / ScaleModifier - (font.MeasureString("|") / 2).X, 100 + (LineIndex * _lineSpacing)) * ScaleModifier, Color.White);

        spriteBatch.DrawString(font, _fps.msg, new Vector2(20, 20) * ScaleModifier, Color.White);
        _fps.frames++;

        //spriteBatch.DrawString(font, $"Lines: {Lines.Count}", new Vector2(150, 20) * ScaleModifier, Color.White);
        //spriteBatch.DrawString(font, $"Current Line: {LineIndex}", new Vector2(250, 20) * ScaleModifier, Color.White);
    }


    private static void TextInputHandler(object sender, TextInputEventArgs e)
    {
        char c = e.Character;
        if (char.IsControl(c))
        {
            if (c == '\b') HandleBackspace();
            else if (c == '\t') HandleTab();
            //else if (c == '\r' || c == '\n') HandleEnter();
            return;
        }
        else
        {
            _charQueue.Enqueue(e.Character);
        }
    }

    private static void HandleBackspace()
    {
        if (Lines[LineIndex].Length > 0 && CharIndex > 0)
        {
            string deletionArea = Lines[LineIndex].Substring(0, CharIndex);
            string tempArea;
            if (CharIndex >= Lines[LineIndex].Length)
            {
                tempArea = String.Empty;
            }
            else
            {
                tempArea = Lines[LineIndex].Substring(CharIndex, Lines[LineIndex].Length - CharIndex);
            }

            int deletionLength = 1;
            if (Input.IsKeyDown(Keys.LeftControl))
            {
                for (int i = deletionArea.Length - 1; i > 0; i--)
                {
                    if (deletionArea[i] == ' ')
                    {
                        while (deletionArea[i] == ' ' && i > 0)
                        {
                            i--;
                        }

                        if (i != 0)
                        {
                            i++;
                        }

                        deletionLength = deletionArea.Length - i;
                        break;
                    }

                    if (i == 1)
                    {
                        deletionLength = deletionArea.Length;
                        break;
                    }
                }
            }

            deletionArea = deletionArea.Substring(0, deletionArea.Length - deletionLength);
            Lines[LineIndex] = deletionArea + tempArea;
            CharIndex -= deletionLength;
        }
        else if (CharIndex == 0 && Lines.Count > 1 && LineIndex > 0)
        {
            string temp = Lines[LineIndex];
            Lines.RemoveAt(LineIndex);
            LineIndex--;
            CharIndex = Lines[LineIndex].Length;
            Lines[LineIndex] += temp;
        }
        else if (CharIndex == 0 && LineIndex == 0 && Lines.Count > 1 && Lines[LineIndex].Length <= 0)
        {
            Lines.RemoveAt(LineIndex);
        }
    }

    private static void HandleTab()
    {
        if (Input.IsKeyDown(Keys.LeftShift))
        {
            if (Lines[LineIndex].Length > 0)
            {
                int spaces = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (Lines[LineIndex][i] == ' ')
                    {
                        spaces++;
                    }
                    else
                    {
                        break;
                    }
                }

                Lines[LineIndex] = Lines[LineIndex].Substring(spaces, Lines[LineIndex].Length - spaces);
                CharIndex -= spaces;
            }

        }
        else
        {
            _charQueue.Enqueue(' ');
            _charQueue.Enqueue(' ');
            _charQueue.Enqueue(' ');
            _charQueue.Enqueue(' ');
        }

    }

    private static void LoadFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            if (Path.GetExtension(filePath) == ".txt")
            {
                Lines = File.ReadAllText(filePath).Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
                if (Lines.Count - 1 < LineIndex)
                {
                    LineIndex = Lines.Count - 1;
                }

                if (Lines[LineIndex].Length < CharIndex)
                {
                    CharIndex = Lines[LineIndex].Length;
                }
            }
        }   
        else
        {
            Console.WriteLine("the specified file doesn't exist");
        }    
    }
}