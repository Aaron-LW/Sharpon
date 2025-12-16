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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

public class RoslynCompletionEngine : IDisposable
{
    private Workspace _workspace;
    private Project _project;
    private DocumentId _documentId;
    private readonly string _projectName = "Sharpon";
    private bool _isAdhoc;

    private static readonly MetadataReference CSharpFeaturesReference = MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly.Location);
    //private static readonly MetadataReference WorkspacesFeaturesReference = MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.Features.Document).Assembly.Location);

    public RoslynCompletionEngine(string filePath)
    {
        EnsureMSBuildRegistered();
        InitializeWorkspaceAsync(EditorMain.FilePath).GetAwaiter().GetResult();
    }
    
    private void InitializeProject()
    {
        var adhoc = _workspace as AdhocWorkspace;
        if (adhoc == null) throw new InvalidOperationException("Initialized project with a non-adhoc workspace");
        var projectId = ProjectId.CreateNewId(debugName: _projectName);
        var version = VersionStamp.Create();
        
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                              .WithUsings(new[] { "System", "System.Collections.Generic", "System.Linq" })
                                                              .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                                                             .WithMetadataImportOptions(MetadataImportOptions.All);
                                                              
        var projectInfo = ProjectInfo.Create(projectId, version, "SharponProj", "SharponAssembly", LanguageNames.CSharp)
                                             .WithCompilationOptions(compilationOptions);
             
        adhoc.AddProject(projectInfo);
        _project = adhoc.CurrentSolution.GetProject(projectId);
        
        _project = _project.WithParseOptions(parseOptions);
    }
    
    public void OpenDocument(string initialText)
    {
        if (!_isAdhoc)
        {
            OpenDocumentMSBuild();
            return;
        }
        
        var adhoc = (AdhocWorkspace)_workspace;
        
        if (_documentId != null)
        {
            var document = adhoc.CurrentSolution.GetDocument(_documentId);
            var sourceText = SourceText.From(initialText);
            var newDocument = document.WithText(sourceText);
            adhoc.TryApplyChanges(newDocument.Project.Solution);
            return;
        }
        
        var documentId = DocumentId.CreateNewId(_project.Id, debugName: "EditorBuffer.cs");
        var documentInfo = DocumentInfo.Create(documentId, "EditorBuffer.cs", null, SourceCodeKind.Regular, TextLoader.From(TextAndVersion.Create(SourceText.From(initialText), VersionStamp.Create())));
        adhoc.AddDocument(documentInfo);
        _documentId = documentId;
        _project = adhoc.CurrentSolution.GetProject(_project.Id);
    }
    
    private void OpenDocumentMSBuild()
    {
        string filePath = EditorMain.FilePath;
        var project = LoadOrGetProjectAsync(filePath).GetAwaiter().GetResult();
        _project = project;
        var document = project.Documents.First(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        _documentId = document.Id;
    }
    
    private async Task<Project> LoadOrGetProjectAsync(string filePath)
    {
        var existing = FindProjectContainingFile(filePath);
        if (existing != null) return existing;
        
        var csproj = GetNearestCsprojFilePath(filePath);
        if (csproj == null) throw new InvalidOperationException("File is not part of a c# project");
        
        var project = await ((MSBuildWorkspace)_workspace).OpenProjectAsync(csproj);
        return project;
    }
    
    private Project FindProjectContainingFile(string filePath)
    {
        return _workspace.CurrentSolution.Projects.FirstOrDefault(p => p.Documents.Any(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase)));
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
        var filteredItems = results.ItemsList
                                    .ToList();

        var list = new List<CompletionResult>(filteredItems.Count);
        
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var namespaceCache = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in filteredItems)
        {
            var filterText = (item.FilterText ?? item.DisplayText) ?? string.Empty;

            string normalizedFilter = filterText.TrimStart('_');
            string normalizedPrefix = prefix.TrimStart('_');

            if (!string.IsNullOrEmpty(normalizedPrefix) &&
                !normalizedFilter.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            var change = await completionService.GetChangeAsync(document, item, null, cancellationToken).ConfigureAwait(false);
            var newText = change.TextChange.NewText;
            if (!namespaceCache.TryGetValue(item.DisplayText, out var nameSpace))
            {
                nameSpace = GetNamespaceForCompletionItem(document, item, position, cancellationToken).GetAwaiter().GetResult();
                if (nameSpace == null) nameSpace = TryExtractNamespaceFromInsertText(newText);
                namespaceCache[item.DisplayText] = nameSpace;
            }

            var display = item.DisplayText;
            var description = await completionService.GetDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false);

            var span = change.TextChange.Span;
            
            list.Add(new CompletionResult
            {
                DisplayText = display,
                FilterText = filterText,
                InsertText = newText,
                Description = description.Text,
                SpanStart = span.Start,
                SpanLength = span.Length,
                CompletionItem = item,
                Namespace = nameSpace
            });
        }
        
        //list = list.OrderBy(item => GetItemPriority(item.CompletionItem))
                   //.ThenBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
                   //.ToList();

        return list;
    }
    
    public async Task InitializeWorkspaceAsync(string filePath)
    {
        string csprojPath = GetNearestCsprojFilePath(filePath);
        
        if (csprojPath != null)
        {
            EnsureMSBuildRegistered();
            
            var msbuildWorkspace = MSBuildWorkspace.Create();
            var project = await msbuildWorkspace.OpenProjectAsync(csprojPath);
            
            _workspace = msbuildWorkspace;
            _project = project;
            
            var document = project.Documents.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            _documentId = document?.Id;
            _isAdhoc = false;
        }
        else
        {
            _workspace = new AdhocWorkspace();
            InitializeProject();
            _isAdhoc = true;
            
            _documentId = null;
        }
    }
    
    private async Task<Project> LoadProjectFromFileAsync(string csprojPath)
    {
        var workspace = MSBuildWorkspace.Create();
        
        workspace.WorkspaceFailed += (s, e) =>
        {
            Console.WriteLine(e.Diagnostic.Message);
        };
        
        return await workspace.OpenProjectAsync(csprojPath);
    }
    
    private string GetNearestCsprojFilePath(string currentPath)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(currentPath)!);
        
        while (dirInfo != null)
        {
            FileInfo csproj = dirInfo.GetFiles("*.csproj").FirstOrDefault();
            if (csproj != null) return csproj.FullName;
            
            dirInfo = dirInfo.Parent;
        }
        
        return null;
    }
    
    private void EnsureMSBuildRegistered()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }

    public string TryExtractNamespaceFromInsertText(string insertText)
    {
        string[] lines = insertText.Split('\n');
        if (!lines[0].StartsWith("using") || lines[0].Length <= 5) return null;
        lines[0] = lines[0].Substring(6, lines[0].Length - 6);
        lines[0] = lines[0].TrimEnd(';');
        return lines[0];
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
    
    private static async Task<string> GetNamespaceForCompletionItem(Document document, CompletionItem item, int position, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return null;
        
        var symbols = semanticModel.LookupSymbols(position, name: item.DisplayText);
        
        var symbol = symbols.FirstOrDefault();
        if (symbol == null) return null;
        
        return symbol.ContainingNamespace?.ToDisplayString();
    }
    
    public HashSet<string> GetImportedNamespaces()
    {
        var document = _workspace.CurrentSolution.GetDocument(_documentId);
        var root = document.GetSyntaxRootAsync().GetAwaiter().GetResult();
        
        return root
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name != null)
            .Select(u => u.Name.ToString())
            .ToHashSet(StringComparer.Ordinal);
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