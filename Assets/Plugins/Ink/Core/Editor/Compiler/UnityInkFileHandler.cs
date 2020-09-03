using Ink;
using System.IO;

// Utility class for the ink compiler, used to work out how to find include files and their contents
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