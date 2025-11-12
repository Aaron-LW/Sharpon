using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System.IO;

public static class Terminal
{
    public static bool IsOpened = false;
    public static int LineIndex = 0;
    public static int CharIndex = 0;
    public static string Text = "";

    private static TerminalProcess _terminalProcess;
    private static GameWindow _gameWindow;
    private static float _terminalHeight = 350;
    private static Vector2 _terminalPosition;
    private static List<string> _lines = new List<string>();
    private static float _spacing = 20;
    private static Vector2 _cursorPosition;
    private static float _scrollAmount;
    private static float _scrollSpeed = 600;
    private static List<string> _commandHistory = new List<string>();
    private static int _commandHistoryIndex;

    public static void Start(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
        _terminalProcess = new TerminalProcess();
        _terminalProcess.Start();
    }
    
    public static void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        //I hate this
        
        Rectangle windowBounds = _gameWindow.ClientBounds;
        float terminalSpeed = 20;
        
        if (!IsOpened)
        {
            if (_terminalPosition.Y > windowBounds.Height) return;
            _terminalPosition.Y = MathHelper.Lerp(_terminalPosition.Y, windowBounds.Height + 100 * EditorMain.ScaleModifier, terminalSpeed * Time.DeltaTime);
        }
        else
        {
            _terminalPosition.Y = MathHelper.Lerp(_terminalPosition.Y, windowBounds.Height - _terminalHeight * EditorMain.ScaleModifier, terminalSpeed * Time.DeltaTime);
        }
        
        
        spriteBatch.FillRectangle(new RectangleF(_terminalPosition.X, 
                                                 _terminalPosition.Y,
                                                 windowBounds.Width,
                                                 windowBounds.Height - (windowBounds.Height - _terminalHeight * EditorMain.ScaleModifier)),
                                                 Color.Black);

        int lineThickness = 3;
        spriteBatch.DrawLine(_terminalPosition, _terminalPosition + new Vector2(windowBounds.Width, 0), Color.LightBlue, lineThickness);
        
        SpriteFontBase font = EditorMain.FontSystem.GetFont(EditorMain.BaseFontSize * EditorMain.ScaleModifier);
        
        Vector2 binBashPosition = _terminalPosition + new Vector2(0, _terminalHeight) * EditorMain.ScaleModifier - new Vector2(0, font.MeasureString("$").Y) * EditorMain.ScaleModifier + new Vector2(10, -9) * EditorMain.ScaleModifier;
        Vector2 linePosition = new Vector2(0, binBashPosition.Y - 5 * EditorMain.ScaleModifier);
        Vector2 textPosition = binBashPosition + new Vector2(font.MeasureString("$").X, 0) + new Vector2(2, 0) * EditorMain.ScaleModifier;

        spriteBatch.End();
        Rectangle terminalBounds = new Rectangle((int)_terminalPosition.X, (int)_terminalPosition.Y + lineThickness / 2, windowBounds.Width, (int)_terminalHeight);
        RasterizerState rasterizerState = new RasterizerState() { ScissorTestEnable = true };
        graphicsDevice.ScissorRectangle = terminalBounds;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
        
        for (int i = 0; i < _lines.Count; i++)
        {
            Vector2 outputPosition = binBashPosition - new Vector2(0, 11) * EditorMain.ScaleModifier + new Vector2(0, _spacing * i) * EditorMain.ScaleModifier - new Vector2(0, _scrollAmount) * EditorMain.ScaleModifier;
            spriteBatch.DrawString(font, _lines[i], outputPosition, Color.White);
        }
        
        spriteBatch.FillRectangle(new RectangleF(linePosition.X,
                                                 linePosition.Y,
                                                 windowBounds.Width - linePosition.X,
                                                 windowBounds.Height - linePosition.Y),
                                                 new Color(10, 10, 10));
                                                 
        spriteBatch.DrawString(font, "$", binBashPosition, Color.White);
        spriteBatch.DrawLine(linePosition, linePosition + new Vector2(windowBounds.Width, 0), Color.White);
        
        spriteBatch.DrawString(font, Text, textPosition, Color.White);
        
        int cursorSpeed = 50;
        _cursorPosition.X = MathHelper.Lerp(_cursorPosition.X,
                                            textPosition.X + font.MeasureString(Text.Substring(0, CharIndex)).X - font.MeasureString("|").X / 2,
                                            cursorSpeed * Time.DeltaTime);
                                            
        _cursorPosition.Y = MathHelper.Lerp(_cursorPosition.Y,
                                            textPosition.Y,
                                            cursorSpeed * Time.DeltaTime);

        spriteBatch.DrawString(font, "|", _cursorPosition, Color.White);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    public static void HandleKeybinds()
    {
        if (Input.IsKeyDown(Keys.LeftControl))
        {
            if (Input.IsKeyPressed(Keys.Z))
            {
                Toggle();
                InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
            }
            
            if (Input.IsKeyDown(Keys.Down) || Input.IsKeyDown(Keys.K))
            {
                _scrollAmount += _scrollSpeed * Time.DeltaTime;
                if (_scrollAmount > _lines.Count * _spacing)
                {
                    _scrollAmount = _lines.Count * _spacing;
                }
            }
            
            if (Input.IsKeyDown(Keys.Up) || Input.IsKeyDown(Keys.I))
            {
                if (_lines.Count >= 16)
                {
                    _scrollAmount -= _scrollSpeed * Time.DeltaTime;
                    if (_scrollAmount < 310)
                    {
                        _scrollAmount = 310;
                    }
                }
            }
            
            if (Input.IsKeyDown(Keys.LeftShift))
            {
                if (Input.IsKeyPressed(Keys.P))
                {
                    Toggle();
                    FileDialog.Open();
                    InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.FileDialog);
                }
            }
        }
        
        
        if (Input.IsKeyPressed(Keys.Escape))
        {
            Toggle();
            InputDistributor.SetInputReceiver(InputDistributor.InputReceiver.Editor);
        }
        
        if (Input.IsKeyPressed(Keys.Up) && _commandHistory.Count > 0)
        {
            if (_commandHistoryIndex != 0) _commandHistoryIndex--;
            SetText(_commandHistory[_commandHistoryIndex]);
            SetCharIndex(Text.Length);
        }
        
        if (Input.IsKeyPressed(Keys.Down) && _commandHistoryIndex != _commandHistory.Count)
        {
            _commandHistoryIndex++;
            if (_commandHistoryIndex == _commandHistory.Count) 
            { 
                SetText(""); 
                SetCharIndex(0); 
            }
            else
            {
                SetText(_commandHistory[_commandHistoryIndex]);
                SetCharIndex(Text.Length);
            }
        }
    }
    
    public static void HandleBackspace()
    {
        if (CharIndex == 0) return;
        if (Text.Length <= 0) return;

        if (CharIndex != Text.Length)
        {
            if (Text[CharIndex] == ')')
            {
                SetText(Text.Remove(CharIndex - 1, 2));
                AddToCharIndex(-1);
                return;
            }
        }

        SetText(Text.Remove(CharIndex - 1, 1));
        AddToCharIndex(-1);
    }
    
    public static void HandleEnter()
    {
        if (Text == "clear")
        {
            _lines.Clear();
            SetText("");
            SetCharIndex(0);
            return;
        }
        
        _terminalProcess.SendCommand(Text);
        if (Text != String.Empty && Text is not null)
        {
            _commandHistory.Add(Text);
            _commandHistoryIndex = _commandHistory.Count;
        }
        
        SetText("");
        SetCharIndex(0);
    }

    public static int GetCharIndex()
    {
        return CharIndex;
    }
    
    public static void SetCharIndex(int charIndex)
    {
        if (charIndex > Text.Length) charIndex = Text.Length;
        if (charIndex < 0) charIndex = 0;
        CharIndex = charIndex;
    }
    
    public static void AddToCharIndex(int amount)
    {
        CharIndex += amount;
        if (CharIndex > Text.Length) CharIndex = Text.Length;
        if (CharIndex < 0) CharIndex = 0;
    }
    
    public static void SetText(string line)
    {
        Text = line;
    }
    
    public static void Toggle()
    {
        IsOpened = !IsOpened;
        if (IsOpened)
        {
            string solutionFilePath = FileDialog.TryGetSolutionDirectoryInfo(Path.GetDirectoryName(EditorMain.FilePath));
            if (solutionFilePath != null)
            {
                SendCommand($"cd {solutionFilePath}");
            }
            else
            {
                SendCommand($"cd {Path.GetDirectoryName(EditorMain.FilePath)}");
            }
        }
    }

    public static void Stop()
    {
        if (_terminalProcess != null)
        {
            _terminalProcess.Stop();
            _terminalProcess = null;
        }
    }

    public static void SendCommand(string command)
    {
        if (_terminalProcess != null)
        {
            _terminalProcess.SendCommand(command);
        }
    }
    
    public static void Print(string line)
    {
        _lines.Add(line);
        _scrollAmount = _lines.Count * _spacing;
    }
}