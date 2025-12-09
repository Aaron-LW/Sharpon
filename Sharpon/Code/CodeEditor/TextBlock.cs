public class TextBlock
{
    public int Start;
    public int End;
    public int? LineIndex;
    public int? CharIndex;
    
    public TextBlock(int start, int end, int? lineIndex, int? charIndex)
    {
        Start = start;
        End = end;
        LineIndex = lineIndex;
        CharIndex = charIndex;
    }
}