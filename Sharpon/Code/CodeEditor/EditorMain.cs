using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

public static class EditorMain
{
    public static float BaseFontSize = 20;
    public static float ScaleModifier => MathF.Round((float)_gameWindow.ClientBounds.Width / 1920, 2);
    public static string FilePath { get; private set; } = "";
    public static bool UnsavedChanges = false;

    private static GameWindow _gameWindow;
    private static float _lineSpacing => (float)(1 * BaseFontSize);
    private static Queue<char> _charQueue = new Queue<char>();
    private static float _codeMaxY = 60;
    private static Vector2 _codePosition = new Vector2(50, _codeMaxY);
    private static Vector2 _cursorPosition;
    private static float _textOpacity = 1;
    private static Vector2 _lineBlockPosition;

    private static float _keyTimer = 0;
    private static bool _keyPressed = false;
    private static float _baseKeyTimer = 0.2f;
    private static float _baseFastKeyTimer = 0.04f;
    private static float _baseVeryFastKeyTimer = 0.015f;

    public static FontSystem FontSystem = new FontSystem();
    public static List<string> Lines = new List<string>() { "" };
    public static int LineIndex { get; private set; }
    public static int CharIndex { get; private set; }
    public static int LineLength => Lines[LineIndex].Length;
    public static string Line
    {
        get { return Lines[LineIndex]; }
        private set { Lines[LineIndex] = value; }    
    }

    public static void Start(GameWindow gameWindow)
    {
        string fontPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "JetBrainsMonoNLNerdFont-Bold.ttf"));
        FontSystem.AddFont(File.ReadAllBytes(fontPath));
        _gameWindow = gameWindow;
        
        string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LastOpenedFile.txt");
        if (File.Exists(file))
        {
            LoadFile(File.ReadAllText(file));
        }
        else
        {
            FileDialog.Open();
            InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.FileDialog);
        }
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = FontSystem.GetFont(BaseFontSize * ScaleModifier);
        int cursorSpeed = 50;

        for (int i = 0; i < Lines.Count; i++)
        {
            spriteBatch.DrawString(font, i.ToString(), 
                                   new Vector2(_codePosition.X - font.MeasureString(i.ToString()).X / ScaleModifier - 15 * ScaleModifier, _codePosition.Y + (i * _lineSpacing)) * ScaleModifier, 
                                   Color.White);
            
            
            if (i == LineIndex)
            {
                Vector2 position = new Vector2(_codePosition.X - font.MeasureString(i.ToString()).X / ScaleModifier - 15 * ScaleModifier,
                                               _codePosition.Y + (LineIndex * _lineSpacing)) * ScaleModifier;

                _lineBlockPosition = new Vector2(MathHelper.Lerp(_lineBlockPosition.X, position.X, cursorSpeed * Time.DeltaTime),
                                                 MathHelper.Lerp(_lineBlockPosition.Y, position.Y, cursorSpeed * Time.DeltaTime));
                
                spriteBatch.FillRectangle(new RectangleF(_lineBlockPosition,
                                                         new SizeF(font.MeasureString(i.ToString()).X,
                                                         font.MeasureString(i.ToString()).Y)),
                                                         Color.White * 0.5f);
            }
            
            spriteBatch.DrawString(font, Lines[i], new Vector2(_codePosition.X, _codePosition.Y + (i * _lineSpacing)) * ScaleModifier, Color.White);
        }
        _cursorPosition = new Vector2(MathHelper.Lerp(_cursorPosition.X, _codePosition.X + font.MeasureString(Lines[LineIndex].Substring(0, CharIndex)).X / ScaleModifier - (font.MeasureString("|") / 2).X, 
                                                      cursorSpeed * Time.DeltaTime), 
                                                      MathHelper.Lerp(_cursorPosition.Y, _codePosition.Y + (LineIndex * _lineSpacing), cursorSpeed * Time.DeltaTime));
                                                      
        spriteBatch.DrawString(font, "|", _cursorPosition * ScaleModifier, Color.White);

        //spriteBatch.DrawString(font, $"Lines: {Lines.Count}", new Vector2(150, 20) * ScaleModifier, Color.White);
        //spriteBatch.DrawString(font, $"Current Line: {LineIndex}", new Vector2(250, 20) * ScaleModifier, Color.White);
    }

    public static void SetCharIndex(int charIndex)
    {
        charIndex = VerifyCharIndex(charIndex);
        CharIndex = charIndex;
    }

    public static void AddToCharIndex(int number)
    {
        CharIndex += number;
        CharIndex = VerifyCharIndex(CharIndex);
    }

    public static int VerifyCharIndex(int charIndex)
    {
        if (charIndex > LineLength)
        {
            charIndex = LineLength;
        }

        if (charIndex < 0)
        {
            charIndex = 0;
        }

        return charIndex;
    }

    public static void SetLineIndex(int lineIndex)
    {
        lineIndex = VerifyLineIndex(lineIndex);
        LineIndex = lineIndex;
    }

    public static void AddToLineIndex(int number)
    {
        LineIndex += number;
        LineIndex = VerifyLineIndex(LineIndex);
    }

    public static int VerifyLineIndex(int lineIndex)
    {
        if (lineIndex >= Lines.Count)
        {
            lineIndex = Lines.Count - 1;
        }

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        return lineIndex;
    }

    public static void SetSelectedLine(string line)
    {
        if (Line != line) UnsavedChanges = true;
        Line = line;
    }

    public static void SetLine(string line, int lineIndex)
    {
        if (lineIndex > Lines.Count || lineIndex < 0) return;
        if (Lines[lineIndex]  != line) UnsavedChanges = true;
        Lines[lineIndex] = line;
    }

    public static void RemoveLine(int lineIndex)
    {
        if (lineIndex > Lines.Count || lineIndex < 0) return;
        if (Lines.Count > 1)
        {
            Lines.RemoveAt(lineIndex);
        }
        else
        {
            Lines[0] = "";
        }

        UnsavedChanges = true;
        LineIndex = VerifyLineIndex(LineIndex);
        CharIndex = VerifyCharIndex(CharIndex);
    }

    public static void LoadFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LastOpenedFile.txt");
            if (!File.Exists(file)) File.Create(file);
            File.WriteAllText(file, filePath);
            
            if (UnsavedChanges) SaveFile(FilePath);
            
            //Lines = Convert.ToHexString(File.ReadAllBytes(filePath)).Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
            Lines = File.ReadAllText(filePath).Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
            if (Lines.Count - 1 < LineIndex)
            {
                LineIndex = Lines.Count - 1;
            }
            
            //NotificationManager.CreateNotification("Loaded file: " + filePath, 3);

            SetCharIndex(LineLength);
            FilePath = filePath;
        }
        else
        {
            NotificationManager.CreateNotification("No file at " + filePath, 5, NotificationType.Error);
            //Console.WriteLine("File at path: " + filePath);
        }
    }

    public static void SaveFile(string filePath)
    {
        if (!File.Exists(filePath)) File.Create(filePath);
        File.WriteAllText(filePath, String.Join("\r\n", Lines));
        NotificationManager.CreateNotification("Saved File", 3);
        UnsavedChanges = false;
    }

    public static void HandleBackspace()
    {
        if (CharIndex == 0)
        {
            if (LineIndex != 0)
            {
                if (LineIndex == Lines.Count - 1)
                {
                    RemoveLine(LineIndex);
                    SetCharIndex(LineLength);
                    return;
                }
                
                string temp = Line;
                RemoveLine(LineIndex);
                AddToLineIndex(-1);
                SetCharIndex(LineLength);
                SetSelectedLine(Line + temp);
            }

            return;
        }

        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = NextControlLeftArrowIndex();
            SetSelectedLine(Line.Remove(nextIndex, CharIndex - nextIndex));
            SetCharIndex(nextIndex);
            return;
        }

        if (CharIndex != LineLength)
        {
            if (Line[CharIndex] == '}' && Line[CharIndex - 1] == '{' ||
                Line[CharIndex] == ')' && Line[CharIndex - 1] == '(' ||
                Line[CharIndex] == ']' && Line[CharIndex - 1] == '[' ||
                Line[CharIndex] == '"' && Line[CharIndex - 1] == '"' )
            {
                SetSelectedLine(Line.Remove(CharIndex - 1, 2));
                AddToCharIndex(-1);
                return;
            }
        }

        SetSelectedLine(Line.Remove(CharIndex - 1, 1));
        AddToCharIndex(-1);
    }

    public static void HandleTab()
    {
        if (Input.IsKeyDown(Keys.LeftShift))
        {
            if (CharIndex != 0)
            {
                if (LineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (Line[i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    SetSelectedLine(Line.Remove(0, spaces));
                    CharIndex = VerifyCharIndex(CharIndex - spaces);
                }
            }
            else
            {
                if (LineLength > 0)
                {
                    int spaces = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (Line[i] == ' ')
                        {
                            spaces++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    SetSelectedLine(Line.Substring(spaces, LineLength - spaces));
                }
            }

        }
        else
        {
            InputHandler.AddToCharQueue(' ');
            InputHandler.AddToCharQueue(' ');
            InputHandler.AddToCharQueue(' ');
            InputHandler.AddToCharQueue(' ');
        }

    }

    public static void HandleEnter()
    {
        bool tab = false;
        string insert = Line.Substring(CharIndex, LineLength - CharIndex);
        if (CharIndex != 0)
        {
            if (Line[CharIndex - 1] == '{')
            {
                tab = true;
            }
        }

        int spaces = 0;
        for (int i = 0; i < LineLength; i++)
        {
            if (Line[i] != ' ') break;
            spaces++;
        }

        string spacesString = "";
        for (int i = 0; i < spaces; i++)
        {
            spacesString += " ";
        }

        if (Input.IsKeyDown(Keys.LeftShift))
        {
            Lines.Insert(LineIndex + 1, spacesString);
            AddToLineIndex(1);
            SetCharIndex(CharIndex);
            UnsavedChanges = true;
            return;
        }

        if (insert != String.Empty)
        {
            if (insert[0] == '}' && InputDistributor.PreviousChar == '{')
            {
                HandleBackspace();
                if (Line.Length > 0) SetSelectedLine(Line.Remove(Line.Length - insert.Length));
                Lines.Insert(LineIndex + 1, spacesString + '{');
                Lines.Insert(LineIndex + 2, spacesString);
                Lines.Insert(LineIndex + 3, spacesString + insert);
                AddToLineIndex(2);
                HandleTab();

                SetCharIndex(CharIndex);
                UnsavedChanges = true;
                return;
            }

        }

        SetSelectedLine(Line.Substring(0, CharIndex));
        Lines.Insert(LineIndex + 1, spacesString + insert);
        AddToLineIndex(1);
        SetCharIndex(spaces);
        if (tab) HandleTab();
        UnsavedChanges = true;
    }

    public static void HandleKeybinds()
    {
        if (Input.IsKeyDown(Keys.Right) && _keyTimer <= 0)
        {
            if (CharIndex != LineLength)
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    SetCharIndex(NextControlRightArrowIndex());
                }
                else
                {
                    AddToCharIndex(1);
                }
            }

            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Left) && _keyTimer <= 0)
        {
            if (CharIndex != 0)
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    SetCharIndex(NextControlLeftArrowIndex());
                }
                else
                {
                    AddToCharIndex(-1);
                }
            }

            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Up) && _keyTimer <= 0)
        {
            if (LineIndex != 0)
            {
                if (Input.IsKeyDown(Keys.LeftAlt))
                {
                    string temp = Lines[LineIndex - 1];
                    SetLine(Line, LineIndex - 1);
                    SetSelectedLine(temp);
                }
                
                AddToLineIndex(-1);
                SetCharIndex(LineLength);
            }

            ResetKeyTimer(Input.IsKeyDown(Keys.LeftControl));
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.Down) && _keyTimer <= 0)
        {
            if (LineIndex != Lines.Count - 1)
            {
                if (Input.IsKeyDown(Keys.LeftAlt))
                {
                    string temp = Lines[LineIndex + 1];
                    SetLine(Line, LineIndex + 1);
                    SetSelectedLine(temp);
                }
                
                AddToLineIndex(1);
                SetCharIndex(LineLength);
            }
            
            ResetKeyTimer(Input.IsKeyDown(Keys.LeftControl));
            _keyPressed = true;
        }
        
        if (Input.IsKeyDown(Keys.LeftControl) && _keyTimer <= 0)
        {
            if (Input.IsKeyDown(Keys.X) && _keyTimer <= 0)
            {
                RemoveLine(LineIndex);
                
                ResetKeyTimer();
                _keyPressed = true;
            }

            if (Input.IsKeyPressed(Keys.S))
            {
                SaveFile(FilePath);
            }

            if (Input.IsKeyPressed(Keys.R))
            {
                LoadFile(FilePath);
                NotificationManager.CreateNotification("Reloaded file", 3);
            }

            if (Input.IsKeyDown(Keys.LeftShift))
            {
                if (Input.IsKeyPressed(Keys.P))
                {
                    FileDialog.SetText(FilePath);
                    FileDialog.Open();
                    InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.FileDialog);
                }
            }

            if (Input.IsKeyPressed(Keys.B))
            {
                SetCharIndex(GetNextNonBraceIndex());
            }

            if (Input.IsKeyPressed(Keys.M))
            {
                //BaseFontSize += 3;
            }
            
            if (Input.IsKeyPressed(Keys.N))
            {
                //BaseFontSize = Math.Clamp(BaseFontSize - 3, 1, 9999999);
            }
            
            if (Input.IsKeyPressed(Keys.A))
            {
                SetCharIndex(GetFirstNonSpaceCharacterIndex());
            }

            if (Input.IsKeyPressed(Keys.D))
            {
                SetCharIndex(LineLength);
            }
            
            if (Input.IsKeyPressed(Keys.Z))
            {
                Terminal.Toggle();
                InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Terminal);
            }
            
            //Up
            if (Input.IsKeyDown(Keys.I) && _keyTimer <= 0)
            {
                AddToLineIndex(-1);
                SetCharIndex(LineLength);
                
                ResetKeyTimer();
                _keyPressed = true;
            }
            
            //Down
            if (Input.IsKeyDown(Keys.K) && _keyTimer <= 0)
            {
                AddToLineIndex(1);
                SetCharIndex(LineLength);
                
                ResetKeyTimer();
                _keyPressed = true;
            }
            
            //Left
            if (Input.IsKeyDown(Keys.J) && _keyTimer <= 0)
            {
                SetCharIndex(NextControlLeftArrowIndex());
                
                ResetKeyTimer();
                _keyPressed = true;
            }
            
            //Right
            if (Input.IsKeyDown(Keys.L) && _keyTimer <= 0)
            {
                SetCharIndex(NextControlRightArrowIndex());
                
                ResetKeyTimer();
                _keyPressed = true;
            }
            
            //ResetKeyTimer();
            //_keyPressed = true;
        }

        if (!Input.IsKeyDown(Keys.Up) &&
            !Input.IsKeyDown(Keys.Down) &&
            !Input.IsKeyDown(Keys.Left) &&
            !Input.IsKeyDown(Keys.Right) &&
            !Input.IsKeyDown(Keys.X) &&
            !Input.IsKeyDown(Keys.I) &&
            !Input.IsKeyDown(Keys.K) &&
            !Input.IsKeyDown(Keys.J) &&
            !Input.IsKeyDown(Keys.L))
        {
            _keyPressed = false;
            _keyTimer = 0;
        }

        _keyTimer -= Time.DeltaTime;

        if (Input.IsKeyPressed(Keys.LeftAlt))
        {
            Random random = new Random();
            _textOpacity = (float)random.Next(0, 10) / 10;
        }

        if (_codePosition.Y <= _codeMaxY)
        {
            _codePosition.Y = MathHelper.Lerp(_codePosition.Y, -(LineIndex * _lineSpacing) + (_gameWindow.ClientBounds.Height / 2), 12 * Time.DeltaTime);
        }

        if (_codePosition.Y > _codeMaxY)
        {
            _codePosition.Y = _codeMaxY;
        }
    }
    
    private static void ResetKeyTimer(bool fast = false)
    {
        if (fast)
        {
            if (_keyPressed)
            {
                _keyTimer = _baseVeryFastKeyTimer;
            }
            else
            {
                _keyTimer = _baseFastKeyTimer;
            }
        }
        else
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
    }

    private static char[] _stopCharsRight = new char[] { ')', '}', ']', '.', ',', '/', ' '};
    private static int NextControlRightArrowIndex()
    {
        if (CharIndex == LineLength) return LineLength;
        
        char selectedChar = Line[CharIndex];
        for (int i = CharIndex; i < LineLength; i++)
        {
            if (!_stopCharsRight.Contains(Line[i]))
            {
                for (int j = i; j < LineLength; j++)
                {
                    if (_stopCharsRight.Contains(Line[j])) return j;
                }
            }
        }
        
        return LineLength;
    }

    private static char[] _stopCharsLeft = new char[] { '(', '.', ',', '[', '{', '/', ' '};
    public static int NextControlLeftArrowIndex()
    {
        if (CharIndex == 0) return 0;
        
        char previousChar = InputDistributor.PreviousChar;
        
        for (int i = CharIndex; i > 0; i--)
        {
            if (i - 1 <= 0) return 0;
            
            if (!_stopCharsLeft.Contains(Line[i - 1]))
            {
                for (int j = i; j > 0; j--)
                {
                    if (Line[j - 1] == 0) return 0;
                    
                    if (_stopCharsLeft.Contains(Line[j - 1]))
                    {
                        return j;
                    }
                }
            }
        }
        return 0;
    }
    
    private static int GetNextNonBraceIndex()
    {
        if (CharIndex == LineLength) return LineLength;
        if (LineLength == 0) return 0;
        
        int startIndex = CharIndex;
        
        for (int i = startIndex; i < LineLength; i++)
        {
            if (Line[i] == ')' ||
                Line[i] == '}' ||
                Line[i] == ']')
            {
                startIndex = i;
                break;
            }
        }

        for (int i = startIndex; i < LineLength + 1; i++)
        {
            if (i == LineLength) return LineLength;
            
            if (Line[i] != ')' &&
                Line[i] != '}' &&
                Line[i] != ']')
            {
                return i;
            }
       }
        
        return CharIndex;
    }
    
    private static int GetFirstNonSpaceCharacterIndex()
    {
        for (int i = 0; i < LineLength; i++)
        {
            if (Line[i] != ' ')
            {
                return i;
            }
        }
        
        return LineLength;
    }
}