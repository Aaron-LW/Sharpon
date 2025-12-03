using Microsoft.CodeAnalysis.Completion;

public class CompletionResult
{
    public string DisplayText { get; set; }
    public string FilterText { get; set; }
    public string InsertText { get; set; }
    public string Description { get; set; }
    public int SpanStart { get; set; }
    public int SpanLength { get; set; }
    public CompletionItem CompletionItem { get; set; }
}