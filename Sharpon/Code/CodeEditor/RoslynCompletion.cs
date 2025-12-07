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
using Microsoft.CodeAnalysis.Features;
using System.IO;
using System.Collections.Concurrent;

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
        
        
        var refs = GetAllDefaultReferences();
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
    
    public void UpdateDocumentIncremental(string newText)
    {
        var document = _workspace.CurrentSolution.GetDocument(_documentId);
        var sourceText = document.GetTextAsync().GetAwaiter().GetResult();
        
        var newSourceText = SourceText.From(newText);
        
        var updated = document.WithText(newSourceText);
        _workspace.TryApplyChanges(updated.Project.Solution);
    }
    
    public async Task<IReadOnlyList<CompletionResult>> GetCompletionsAsync(int position, CancellationToken cancellationToken)
    {
        var document = _workspace.CurrentSolution.GetDocument(_documentId);
        var completionService = CompletionService.GetService(document);
        if (completionService == null) return Array.Empty<CompletionResult>();

        var src = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        int start = position;
        while (start > 0 && (char.IsLetterOrDigit(src[start - 1]) || src[start - 1] == '_'))
            start--;

        var prefix = src.ToString(new TextSpan(start, position - start));

        var results = await completionService.GetCompletionsAsync(
            document, position, cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (results == null) return Array.Empty<CompletionResult>();

        var list = new List<CompletionResult>(results.ItemsList.Count);

        foreach (var item in results.ItemsList)
        {
            var filterText = (item.FilterText ?? item.DisplayText) ?? string.Empty;

            string normalizedFilter = filterText.TrimStart('_');
            string normalizedPrefix = prefix.TrimStart('_');

            if (!string.IsNullOrEmpty(normalizedPrefix) &&
                !normalizedFilter.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
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
        
        list = list.OrderBy(item => GetItemPriority(item.CompletionItem))
                   .ThenBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
                   .ToList();

        return list;
    }


    
    static IEnumerable<MetadataReference> GetAllDefaultReferences()
    {
        var paths = new List<string>();

        // 1) Trusted platform assemblies (best for .NET Core / .NET 5+)
        try
        {
            var tpaObj = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrEmpty(tpaObj))
            {
                foreach (var p in tpaObj.Split(Path.PathSeparator))
                {
                    if (!string.IsNullOrEmpty(p) && File.Exists(p) && !paths.Contains(p))
                        paths.Add(p);
                }
            }
        }
        catch
        {
            // ignore if not available on this runtime
        }

        // 2) Assemblies currently loaded into the AppDomain (e.g. MonoGame)
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.IsDynamic) continue;
                    var loc = asm.Location;
                    if (string.IsNullOrEmpty(loc)) continue;
                    if (!paths.Contains(loc))
                        paths.Add(loc);
                }
                catch
                {
                    // some assemblies can throw on Location — ignore them
                }
            }
        }
        catch
        {
            // ignore
        }

        // 3) Ensure a few essentials are present (defensive)
        void TryAdd(Type t)
        {
            try
            {
                var loc = t.Assembly.Location;
                if (!string.IsNullOrEmpty(loc) && !paths.Contains(loc))
                    paths.Add(loc);
            }
            catch { }
        }

        TryAdd(typeof(object));
        TryAdd(typeof(Console));
        TryAdd(typeof(Enumerable));

        // Create MetadataReference list (deduplicated)
        var refs = new List<MetadataReference>();
        foreach (var p in paths)
        {
            try
            {
                refs.Add(MetadataReference.CreateFromFile(p));
            }
            catch
            {
                // skip invalid files
            }
        }

        return refs;
    }

    public void RefreshLoadedAssemblyReferences()
    {
        var refs = GetAllDefaultReferences();
        // Filter out ones already present to avoid duplicates (by display)
        var existing = new HashSet<string>(_project.MetadataReferences.Select(r => (r.Display ?? "").ToLowerInvariant()));
        var toAdd = refs.Where(r => !existing.Contains((r.Display ?? "").ToLowerInvariant())).ToArray();
        if (toAdd.Length > 0)
        {
            _project = _project.AddMetadataReferences(toAdd);
            _workspace.TryApplyChanges(_project.Solution);
        }
    }
    
    private static int GetItemPriority(CompletionItem item)
    {
        if (item.Tags.Contains("Local")) return 0;
        if (item.Tags.Contains("Parameter")) return 0;
        if (item.Tags.Contains("Field")) return 1;
        
        if (item.Tags.Contains("Keyword")) return 2;
        if (item.Tags.Contains("Method")) return 3;
        
        if (item.Tags.Contains("Class")) return 4;
        if (item.Tags.Contains("Struct")) return 4;
        if (item.Tags.Contains("Enum")) return 4;
        if (item.Tags.Contains("Interface")) return 4;
        return 100;
    }
    
    public void Dispose()
    {
        _workspace.Dispose();
    }
}