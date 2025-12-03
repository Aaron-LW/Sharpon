using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

public class RoslynCompletionEngine : IDisposable
{
    private readonly AdhocWorkspace _workspace;
    private Project _project;
    private DocumentId _documentId;
    private readonly string _projectName = "Sharpon";

    private static readonly MetadataReference CSharpFeaturesReference = MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly.Location);
    //private static readonly MetadataReference WorkspacesFeaturesReference = MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.Features.Document).Assembly.Location);

    public RoslynCompletionEngine()
    {
        _workspace = new AdhocWorkspace();
        InitializeProject();
    }
    
    private void InitializeProject()
    {
        var projectId = ProjectId.CreateNewId(debugName: _projectName);
        var version = VersionStamp.Create();
        
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                              .WithUsings(new[] { "System", "System.Collections.Generic", "System.Linq" })
                                                              .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                                                             .WithMetadataImportOptions(MetadataImportOptions.All);
                                                              
        var projectInfo = ProjectInfo.Create(projectId, version, "SharponProj", "SharponAssembly", LanguageNames.CSharp)
                                             .WithCompilationOptions(compilationOptions);
        _workspace.AddProject(projectInfo);
        _project = _workspace.CurrentSolution.GetProject(projectId);
        
        
        var refs = GetAllDefaultReferences()
        .Concat(new[] {
            CSharpFeaturesReference,
            });;
        _project = _project.AddMetadataReferences(refs);
        _project = _project.WithParseOptions(parseOptions);
        _workspace.TryApplyChanges(_project.Solution);
    }
    
    public void OpenDocument(string initialText)
    {
        if (_documentId != null)
        {
            var document = _workspace.CurrentSolution.GetDocument(_documentId);
            var sourceText = SourceText.From(initialText);
            var newDocument = document.WithText(sourceText);
            _workspace.TryApplyChanges(newDocument.Project.Solution);
            return;
        }
        
        var documentId = DocumentId.CreateNewId(_project.Id, debugName: "EditorBuffer.cs");
        var documentInfo = DocumentInfo.Create(documentId, "EditorBuffer.cs", null, SourceCodeKind.Regular, TextLoader.From(TextAndVersion.Create(SourceText.From(initialText), VersionStamp.Create())));
        _workspace.AddDocument(documentInfo);
        _documentId = documentId;
        _project = _workspace.CurrentSolution.GetProject(_project.Id);
    }
    
    public async Task<IReadOnlyList<CompletionResult>> GetCompletionsAsync(int position, CancellationToken cancellationToken)
    {
        var document = _workspace.CurrentSolution.GetDocument(_documentId);
        var completionService = CompletionService.GetService(document);
        if (completionService == null) return Array.Empty<CompletionResult>();

        var src = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        int start = position;
        while (start > 0 && char.IsLetterOrDigit(src[start - 1])) start--;
        var prefix = src.ToString(new TextSpan(start, position - start));

        var results = await completionService.GetCompletionsAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (results == null) return Array.Empty<CompletionResult>();

        var list = new List<CompletionResult>(results.ItemsList.Count);
        foreach (var item in results.ItemsList)
        {
            var filterText = (item.FilterText ?? item.DisplayText) ?? string.Empty;
            if (!string.IsNullOrEmpty(prefix) &&
                !filterText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var display = item.DisplayText;
            var description = await completionService.GetDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false);
            var change = await completionService.GetChangeAsync(document, item, null, cancellationToken).ConfigureAwait(false);

            var newText = change.TextChange.NewText;
            var span = change.TextChange.Span;

            list.Add(new CompletionResult
            {
                DisplayText = display,
                FilterText = filterText,
                InsertText = newText,
                Description = description.Text,
                SpanStart = span.Start,
                SpanLength = span.Length,
                CompletionItem = item
            });
        }

        return list;
    }

    
    static IEnumerable<MetadataReference> GetAllDefaultReferences()
    {
        return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));
    }
    
    public void Dispose()
    {
        _workspace.Dispose();
    }
}