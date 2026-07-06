using Ink;
using System.IO;

/// <summary>
/// IFileHandler for the ink compiler: resolves and loads INCLUDEd files, relative to the master
/// (root) file's directory — which is how ink resolves all INCLUDE paths.
/// </summary>
public class UnityInkFileHandler : IFileHandler {
    private readonly string rootDirectory;

    public UnityInkFileHandler(string rootDirectory)
    {
        this.rootDirectory = rootDirectory;
    }
    
    public string ResolveInkFilename(string includeName)
    {
        // Convert to Unix style, and then use FileInfo.FullName to parse any ..\
        return new FileInfo(Path.Combine(rootDirectory, includeName).Replace('\\', '/')).FullName;
    }

    public string LoadInkFileContents(string fullFilename)
    {
        return File.ReadAllText(fullFilename);
    }
}