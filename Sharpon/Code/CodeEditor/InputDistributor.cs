using System;
using System.Diagnostics.Contracts;

public static class InputDistributor
{
    public enum InputReceiver
    {
        Editor,
        FileDialog
    }

    public static string SelectedLine => GetSelectedLine();
    public static int CharIndex => GetCharIndex();
    public static int LineIndex => GetLineIndex();
    public static int LineLength = GetSelectedLine().Length;

    private static InputReceiver _inputReceiver = InputReceiver.Editor;

    public static void SetSelectedLine(string line)
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.SetSelectedLine(line);
                return;

            case InputReceiver.FileDialog:
                FileDialog.SetText(line);
                return;
        }

        throw new NotImplementedException($"Couldn't set selected line of {_inputReceiver}; Wasn't set up for SetSelectedLine");
    }

    public static string GetSelectedLine()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                return EditorMain.Line;

            case InputReceiver.FileDialog:
                return FileDialog.Text;
        }

        throw new NotImplementedException($"Couldn't get selected line of {_inputReceiver}; Wasn't set up for GetSelectedLine");
    }

    public static void RemoveSelectedLine()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.RemoveLine(EditorMain.LineIndex);
                return;

            case InputReceiver.FileDialog:
                FileDialog.SetText("");
                return;
        }

        throw new NotImplementedException($"Couldn't remove selected line of {_inputReceiver}; Wasn't set up for RemoveSelectedLine");
    } 

    public static int GetLineIndex()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                return EditorMain.LineIndex;

            case InputReceiver.FileDialog:
                return 0;
        }

        throw new NotImplementedException($"Couldn't get line index of {_inputReceiver}; Wasn't set up for GetLineIndex()");
    }

    public static void AddToLineIndex(int amount)
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.AddToLineIndex(amount);
                return;

            case InputReceiver.FileDialog:
                return;
        }

        throw new NotImplementedException($"Couldn't add to line index of {_inputReceiver}; Wasn't set up for AddToLineIndex()");
    }

    public static void SetCharIndex(int charIndex)
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.SetCharIndex(charIndex);
                return;

            case InputReceiver.FileDialog:
                FileDialog.SetCharIndex(charIndex);
                return;
        }

        throw new NotImplementedException($"Couldn't set character index of {_inputReceiver}; Wasn't set up for SetCharIndex()");
    }

    public static int GetCharIndex()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                return EditorMain.CharIndex;

            case InputReceiver.FileDialog:
                return FileDialog.CharIndex;
        }

        throw new NotImplementedException($"Couldn't get character index of {_inputReceiver}; Wasn't set up for GetCharIndex()");
    }

    public static void AddToCharIndex(int amount)
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.AddToCharIndex(amount);
                return;

            case InputReceiver.FileDialog:
                FileDialog.AddToCharIndex(amount);
                return;
        }

        throw new NotImplementedException($"Couldn't add to character index of {_inputReceiver}; Wasn't set up for AddToCharIndex()");
    }

    public static void HandleBackspace()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.HandleBackspace();
                return;

            case InputReceiver.FileDialog:
                FileDialog.HandleBackspace();
                return;
        }

        throw new NotImplementedException($"Couldn't handle backspace of {_inputReceiver}; Wasn't set up for HandleBackspace()");
    }

    public static void HandleEnter()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.HandleEnter();
                return;

            case InputReceiver.FileDialog:
                FileDialog.HandleEnter();
                return;
        }

        throw new NotImplementedException($"Couldn't handle enter of {_inputReceiver}; Wasn't set up for HandleEnter()");
    }

    public static void HandleTab()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.HandleTab();
                return;

            case InputReceiver.FileDialog:
                FileDialog.HandleTab();
                return;
        }

        throw new NotImplementedException($"Couldn't handle tab of {_inputReceiver}; Wasn't set up for HandleTab()");
    }
    
    public static void HandleKeybinds()
    {
        switch (_inputReceiver)
        {
            case InputReceiver.Editor:
                EditorMain.HandleKeybinds();
                return;

            case InputReceiver.FileDialog:
                FileDialog.HandleKeybinds();
                return;
        }
        
        throw new NotImplementedException($"Couldn't handle keybinds of {_inputReceiver}; Wasn't set up for HandleKeybinds()");
    }
    
    public static void SetInputReceiver(InputReceiver inputReceiver)
    {
        if (inputReceiver == InputReceiver.FileDialog && !FileDialog.IsOpened) throw new Exception("Tried to set FileDialog as InputReceiver but FileDialog wasn't opened"); 
        _inputReceiver = inputReceiver;
    }
}