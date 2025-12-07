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
using System.Text.RegularExpressions;
using MonoGame.Extended.Graphics;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using System.Threading.Tasks;

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
    private static bool _modeSwitchPressed;
    private static RoslynCompletionEngine _roslynCompleter = new RoslynCompletionEngine();
    private static IReadOnlyList<CompletionResult> _completions;
    private static CancellationTokenSource _completionCts;
    private static Color _completionBackgroundColor = new Color(20, 18, 27);
    private static Color _completionBackgroundLineColor = new Color(25, 23, 32);
    private static bool _started = false;
    private static int _completionIndex = 0;
    private static bool _completionsOutdated = false;

    private static float _keyTimer = 0;
    private static bool _keyPressed = false;
    private static float _baseKeyTimer = 0.2f;
    private static float _baseFastKeyTimer = 0.04f;
    private static float _baseVeryFastKeyTimer = 0.015f;

    public static EditorMode EditorMode { get; private set; } = EditorMode.Editing;
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

    public static void Start(GameWindow gameWindow, string fileToOpen = null)
    {
        if (_started) return;
        
        string fontPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "JetBrainsMonoNLNerdFont-Bold.ttf"));
        FontSystem.AddFont(File.ReadAllBytes(fontPath));
        _gameWindow = gameWindow;
        
        if (fileToOpen != null)
        {
            if (File.Exists(fileToOpen))
            {
                LoadFile(fileToOpen);
                _started = true;
            }
        }
        
        string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LastOpenedFile.txt");
        if (File.Exists(file))
        {
            LoadFile(File.ReadAllText(file));
            //LoadFile("/media/MonoGame/Geometry_Smash/Geometry_Smash/Game1.cs");
        }
        else
        {
            FileDialog.Open();
            InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.FileDialog);
        }
        
        _started = true;
    }
    
    public static void Draw(SpriteBatch spriteBatch)
    {
        SpriteFontBase font = FontSystem.GetFont(BaseFontSize * ScaleModifier);
        int cursorSpeed = 50;

        for (int i = 0; i < Lines.Count; i++)
        {
            float lineY = _codePosition.Y + (i * _lineSpacing);
            if (lineY < -10 || lineY * ScaleModifier > _gameWindow.ClientBounds.Height) continue;
            
            int lineCount = i + 1;
            spriteBatch.DrawString(font, lineCount.ToString(), 
                                   new Vector2(_codePosition.X - font.MeasureString(lineCount.ToString()).X / ScaleModifier - 15 * ScaleModifier, _codePosition.Y + (i * _lineSpacing)) * ScaleModifier, 
                                   Color.White);
            
            
            if (i == LineIndex)
            {
                Vector2 position = new Vector2(_codePosition.X - font.MeasureString(lineCount.ToString()).X / ScaleModifier - 15 * ScaleModifier,
                                               _codePosition.Y + (LineIndex * _lineSpacing)) * ScaleModifier;

                _lineBlockPosition = new Vector2(MathHelper.Lerp(_lineBlockPosition.X, position.X, cursorSpeed * Time.DeltaTime),
                                                 MathHelper.Lerp(_lineBlockPosition.Y, position.Y, cursorSpeed * Time.DeltaTime));
                
                spriteBatch.FillRectangle(new RectangleF(_lineBlockPosition,
                                                         new SizeF(font.MeasureString(lineCount.ToString()).X,
                                                         font.MeasureString(lineCount.ToString()).Y)),
                                                         Color.White * 0.5f);
            }
            
            TextBlock[] foundBlocks = Finder.Find(Lines[i]);
            if (foundBlocks.Length != 0)
            {
                foreach (TextBlock block in foundBlocks)
                {
                    Vector2 startPosition = new Vector2(_codePosition.X + font.MeasureString(Lines[i].Substring(0, block.Start)).X / ScaleModifier, lineY);
                    Vector2 endPosition = new Vector2(_codePosition.X + font.MeasureString(Lines[i].Substring(0, block.End)).X / ScaleModifier, lineY + font.MeasureString(Lines[i].Substring(0, block.End)).Y);
                    
                    spriteBatch.FillRectangle(new RectangleF(startPosition.X * ScaleModifier,
                                                             startPosition.Y * ScaleModifier,
                                                             MathF.Abs(endPosition.X - startPosition.X) * ScaleModifier,
                                                             MathF.Abs(endPosition.Y - startPosition.Y) * ScaleModifier),
                                                             new Color(84, 8, 99));
                }
            }
            
            //spriteBatch.DrawString(font, Lines[i], new Vector2(_codePosition.X, _codePosition.Y + (i * _lineSpacing)) * ScaleModifier, Color.White);
            string[] words = Regex.Matches(Lines[i], @"\s+|'[^']*'|""[^""]*""|[^\w\s]|[\p{L}\p{N}_]+").Select(m => m.Value).ToArray();
            
            for (int j = 0; j < words.Length; j++)
            {
                string leadingString = string.Join("", words.Take(j));
                
                TextToColor.Lookup.TryGetValue(words[j], out Color color);
                if (color == Color.Transparent)
                {
                    if (words[j][0] == '"' || words[j][0].ToString() == "'")
                    {
                        color = Color.Orange;
                    }
                    else
                    {
                        color = Color.White;
                    }
                }
                
                spriteBatch.DrawString(font, words[j], new Vector2(_codePosition.X + font.MeasureString(leadingString).X / ScaleModifier, _codePosition.Y + (i * _lineSpacing)) * ScaleModifier, color);
            }
        }
        _cursorPosition = new Vector2(MathHelper.Lerp(_cursorPosition.X, _codePosition.X + font.MeasureString(Lines[LineIndex].Substring(0, CharIndex)).X / ScaleModifier - (font.MeasureString("|") / 2).X, 
                                                      cursorSpeed * Time.DeltaTime), 
                                                      MathHelper.Lerp(_cursorPosition.Y, _codePosition.Y + (LineIndex * _lineSpacing), cursorSpeed * Time.DeltaTime));
                                                      
        Color cursorColor = EditorMode == EditorMode.Editing ? Color.White : Color.Orange;
        spriteBatch.DrawString(font, "|", _cursorPosition * ScaleModifier, cursorColor);
        
        Vector2 completionPosition = _cursorPosition * ScaleModifier + new Vector2(15 * ScaleModifier, 14 * ScaleModifier);
        
        if (_completions != null && EditorMode == EditorMode.Editing && CharIndex != 0 && !_completionsOutdated)
        {
            if (_completions.Count > 0)
            {
                spriteBatch.FillRectangle(new RectangleF(completionPosition.X - 5 * ScaleModifier,
                                                         completionPosition.Y - 2 * ScaleModifier,
                                                         250 * ScaleModifier,
                                                         18 * _completions.Count * ScaleModifier + (5 * ScaleModifier)),
                                                         _completionBackgroundColor);
                                                     
                spriteBatch.DrawRectangle(new RectangleF(completionPosition.X - 5 * ScaleModifier,
                                                         completionPosition.Y - 2 * ScaleModifier,
                                                         250 * ScaleModifier,
                                                         18 * _completions.Count * ScaleModifier + (7 * ScaleModifier)),
                                                         Color.RoyalBlue, 2); 
                                                     
                for (int i = 0; i < _completions.Count; i++)
                {
                    if (i > _completions.Count) break;
                    Vector2 actualCompletionPosition = completionPosition + new Vector2(0, (i * 18) * ScaleModifier);
                    if (actualCompletionPosition.Y > _gameWindow.ClientBounds.Height) continue;
                    
                    if (i == _completionIndex)
                    {
                        spriteBatch.FillRectangle(new RectangleF(actualCompletionPosition.X,
                                                                 actualCompletionPosition.Y + 3 * ScaleModifier,
                                                                 font.MeasureString(_completions[i].DisplayText).X,
                                                                 font.MeasureString(_completions[i].DisplayText).Y - 3 * ScaleModifier),
                                                                 Color.Yellow * 0.7f);
                    }
                    
                    spriteBatch.DrawString(font, _completions[i].DisplayText, actualCompletionPosition, Color.White);
                }
            }
        }

        //spriteBatch.DrawString(font, $"Lines: {Lines.Count}", new Vector2(150, 20) * ScaleModifier, Color.White);
        //spriteBatch.DrawString(font, $"Current Line: {LineIndex}", new Vector2(250, 20) * ScaleModifier, Color.White);
    }

    public static void SetCharIndex(int charIndex)
    {
        int prevCharIndex = CharIndex;
        charIndex = VerifyCharIndex(charIndex);
        CharIndex = charIndex;
        
        if (prevCharIndex != CharIndex)
        {
            _completionsOutdated = true;
            InitiateCompletions();
        }
    }

    public static void AddToCharIndex(int number)
    {
        int prevCharIndex = CharIndex;
        CharIndex += number;
        CharIndex = VerifyCharIndex(CharIndex);
        
        if (prevCharIndex != CharIndex)
        {
            _completionsOutdated = true;
            InitiateCompletions();
        }
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
        int prevLineIndex = LineIndex;
        lineIndex = VerifyLineIndex(lineIndex);
        LineIndex = lineIndex;
        SetCharIndex(Line.Length);
        
        if (prevLineIndex != lineIndex)
        {
            _completionsOutdated = true;
            InitiateCompletions();
        }
    }

    public static void AddToLineIndex(int number)
    {
        int prevLineIndex = LineIndex;
        LineIndex += number;
        LineIndex = VerifyLineIndex(LineIndex);
        
        if (prevLineIndex != LineIndex)
        {
            _completionsOutdated = true;
            InitiateCompletions();
        }
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
    
    public static void SetFilePath(string path)
    {
        FilePath = path;
    }

    public static void SetSelectedLine(string line, string pressedKeys = null)
    {
        if (Line != line) UnsavedChanges = true;
        string prevLine = Line;
        Line = line;
        if (prevLine != line) _completionsOutdated = true;
        
        if (pressedKeys == null) return;
        if (prevLine == Line) return;
        
        InitiateCompletions();
    }
    
    private static void InitiateCompletions()
    {
        if (string.IsNullOrWhiteSpace(Line)) return;
        if (CharIndex == 0) return;
        if (InputDistributor.PreviousChar == ';') return;
        
        _ = Task.Run(async () =>
        {
            _completionCts?.Cancel();
            _completionCts?.Dispose();
            
            _completionCts = new CancellationTokenSource();
            var token = _completionCts.Token;
            
            try
            {
                var items = await GetCompletionsAsync(token);
                _completions = items;
            }
            catch (OperationCanceledException)
            {
                
            }
            
            _completionsOutdated = false;
            _completionIndex = 0;
        });
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
            if (!File.Exists(file)) using (var fileStream = File.Create(file)) {}
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
            _roslynCompleter.OpenDocument(string.Join("\n", Lines));
            _roslynCompleter.RefreshLoadedAssemblyReferences();
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
        if (EditorMode == EditorMode.Moving) EditorMode = EditorMode.Editing;
        
        if (CharIndex == 0)
        {
            if (LineIndex != 0)
            {
                if (LineIndex == Lines.Count - 1)
                {
                    RemoveLine(LineIndex);
                    SetCharIndex(LineLength);
                    InitiateCompletions();
                    return;
                }
                
                string temp = Line;
                RemoveLine(LineIndex);
                AddToLineIndex(-1);
                SetCharIndex(LineLength);
                SetSelectedLine(Line + temp);
            }

            InitiateCompletions();
            return;
        }

        if (Input.IsKeyDown(Keys.LeftControl))
        {
            int nextIndex = NextControlLeftArrowIndex();
            SetSelectedLine(Line.Remove(nextIndex, CharIndex - nextIndex));
            SetCharIndex(nextIndex);
            InitiateCompletions();
            return;
        }

        if (CharIndex != LineLength)
        {
            if (Line[CharIndex] == '}' && Line[CharIndex - 1] == '{' ||
                Line[CharIndex] == ')' && Line[CharIndex - 1] == '(' ||
                Line[CharIndex] == ']' && Line[CharIndex - 1] == '[' ||
                Line[CharIndex] == '"' && Line[CharIndex - 1] == '"')
            {
                SetSelectedLine(Line.Remove(CharIndex - 1, 2));
                AddToCharIndex(-1);
                InitiateCompletions();
                return;
            }
        }

        SetSelectedLine(Line.Remove(CharIndex - 1, 1));
        AddToCharIndex(-1);
        InitiateCompletions();
    }

    public static void HandleTab(bool noCompletion = false)
    {
        if (_completions != null && EditorMode != EditorMode.Moving && !noCompletion && CharIndex != 0)
        {
            if (_completions.Count > 0)
            {
                DoCodeCompletion();
                return;
            }
        }
        
        if (EditorMode == EditorMode.Moving) EditorMode = EditorMode.Editing;
        
        
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
        if (EditorMode == EditorMode.Moving) EditorMode = EditorMode.Editing;
        
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
                if (Line.Length > 0 && InputDistributor.PreviousChar == ' ') SetSelectedLine(Line.Remove(Line.Length - insert.Length));
                Lines.Insert(LineIndex + 1, spacesString + '{');
                Lines.Insert(LineIndex + 2, spacesString);
                Lines.Insert(LineIndex + 3, spacesString + insert);
                AddToLineIndex(2);
                HandleTab(true);

                SetCharIndex(CharIndex);
                UnsavedChanges = true;
                return;
            }

        }

        SetSelectedLine(Line.Substring(0, CharIndex));
        Lines.Insert(LineIndex + 1, spacesString + insert);
        AddToLineIndex(1);
        SetCharIndex(spaces);
        if (tab) HandleTab(true);
        UnsavedChanges = true;
    }

    public static void HandleKeybinds()
    {
        if (Input.IsKeyDown(Keys.Escape) || Input.IsKeyDown(Keys.CapsLock))
        {
            if (!_modeSwitchPressed)
            {
                if (EditorMode == EditorMode.Editing)
                {
                    EditorMode = EditorMode.Moving;
                }
                else
                {
                    EditorMode = EditorMode.Editing;
                }
                
                _modeSwitchPressed = true;
            }
        }
        else
        {
            _modeSwitchPressed = false;
        }
        
        if (Input.IsKeyDown(Keys.Space))
        {
            EditorMode = EditorMode.Editing;
        }
        
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
            
            if (Input.IsKeyPressed(Keys.T))
            {
                Terminal.Toggle();
                InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Terminal);
            }
            
            if (Input.IsKeyPressed(Keys.F))
            {
                if (!Finder.IsOpened) Finder.Open();
                
                if (Input.IsKeyDown(Keys.LeftShift))
                {
                    Finder.SetText("");
                    Finder.Close();
                }
                else
                {
                    InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Finder);
                }
            }
            
            
            //ResetKeyTimer();
            //_keyPressed = true;
        }

        if (EditorMode == EditorMode.Moving)
        {
            HandleKeybindsMoving();
        }
        else
        {
            HandleKeybindsEditing();
        }

        if (!Input.IsKeyDown(Keys.Up) &&
            !Input.IsKeyDown(Keys.Down) &&
            !Input.IsKeyDown(Keys.Left) &&
            !Input.IsKeyDown(Keys.Right) &&
            !Input.IsKeyDown(Keys.X) &&
            !Input.IsKeyDown(Keys.I) &&
            !Input.IsKeyDown(Keys.K) &&
            !Input.IsKeyDown(Keys.J) &&
            !Input.IsKeyDown(Keys.L) &&
            !Input.IsKeyDown(Keys.Q) &&
            !Input.IsKeyDown(Keys.E) &&
            !Input.IsKeyDown(Keys.W) &&
            !Input.IsKeyDown(Keys.S) &&
            !Input.IsKeyDown(Keys.A) &&
            !Input.IsKeyDown(Keys.D))
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
    
    private static void HandleKeybindsMoving()
    {
        if (Input.IsKeyDown(Keys.A) && _keyTimer <= 0)
        {
            AddToCharIndex(-1);
            
            ResetKeyTimer();
            _keyPressed = true;
        }

        if (Input.IsKeyDown(Keys.D) && _keyTimer <= 0)
        {
            AddToCharIndex(1);
            
            ResetKeyTimer();
            _keyPressed = true;
        }
        
        if (Input.IsKeyDown(Keys.W) && _keyTimer <= 0)
        {
            AddToLineIndex(-1);
            SetCharIndex(LineLength);
            
            ResetKeyTimer(true);
            _keyPressed = true;
        }
        
        if (Input.IsKeyDown(Keys.S) && _keyTimer <= 0)
        {
            AddToLineIndex(1);
            SetCharIndex(LineLength);
            
            ResetKeyTimer(true);
            _keyPressed = true;
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
            if (CharIndex != 0)
            {
                SetCharIndex(NextControlLeftArrowIndex());
            }
            
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

        if (Input.IsKeyPressed(Keys.E))
        {
            SetCharIndex(LineLength);
        }
        
        if (Input.IsKeyPressed(Keys.Q))
        {
            SetCharIndex(GetFirstNonSpaceCharacterIndex());
        }
    }
    
    private static void HandleKeybindsEditing()
    {
        if (Input.IsKeyDown(Keys.LeftControl) && _keyTimer <= 0)
        {
            if (Input.IsKeyDown(Keys.K) && _keyTimer <= 0)
            {
                _completionIndex++;
                if (_completionIndex > _completions.Count - 1) _completionIndex = _completions.Count - 1;
                
                ResetKeyTimer();
                _keyPressed = true;
            }
            
            if (Input.IsKeyDown(Keys.I) && _keyTimer <= 0)
            {
                _completionIndex--;
                
                if (_completionIndex < 0) _completionIndex = 0;
                ResetKeyTimer();
                _keyPressed = true;
            }
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
    public static int NextControlLeftArrowIndex(string line = null, int charIndex = -1)
    {
        if (line == null) line = Line;
        if (charIndex == -1) charIndex = CharIndex;
        
        if (charIndex == 0) return 0;
        
        for (int i = charIndex; i > 0; i--)
        {
            if (i - 1 <= 0) return 0;
            
            if (!_stopCharsLeft.Contains(line[i - 1]))
            {
                for (int j = i; j > 0; j--)
                {
                    if (line[j - 1] == 0) return 0;
                    
                    if (_stopCharsLeft.Contains(line[j - 1]))
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

    private static int GetAbsoluteCharIndex()
    {
        int chars = 0;
        for (int i = 0; i < LineIndex; i++)
        {
            chars += Lines[i].Length + 1;
        }

        chars += CharIndex;
        return chars;
    }
    
    private static int GetUnAbsoluteCharIndex(int absoluteCharIndex)
    {
        int totalChars = 0;
        for (int i = 0; i < Lines.Count; i++)
        {
            totalChars += Lines[i].Length + 1;
            
            if (totalChars >= absoluteCharIndex)
            {
                return totalChars - absoluteCharIndex - 1;
            }
            
        }
        
        return -1;
    }

    private static async Task<IReadOnlyList<CompletionResult>> GetCompletionsAsync(CancellationToken cancelToken)
    {
        string code = string.Join("\n", Lines);
        _roslynCompleter.UpdateDocumentIncremental(code);
        
        int caret = GetAbsoluteCharIndex();
        var items = await _roslynCompleter.GetCompletionsAsync(caret, cancelToken);
        return items;
    }

    private static void DoCodeCompletion()
    {
        if (_completionIndex > _completions.Count - 1) { Console.WriteLine("Completionindex out of bounds"); return; }
        if (CharIndex == 0) return;
        if (_completionsOutdated) return;
        int spanStart = GetUnAbsoluteCharIndex(_completions[_completionIndex].SpanStart);
        if (spanStart == 1) spanStart--;
        int spanLength = _completions[_completionIndex].SpanLength;
        spanStart += CharIndex - spanLength * 2;
        spanStart -= Line.Substring(CharIndex, LineLength - CharIndex).Length;
        string displayText = _completions[_completionIndex].DisplayText;
        
        SetSelectedLine(Line.Remove(spanStart, spanLength));
        AddToCharIndex(-spanLength);
        SetSelectedLine(Line.Insert(spanStart, displayText));
        AddToCharIndex(displayText.Length);
        return;
    }
}