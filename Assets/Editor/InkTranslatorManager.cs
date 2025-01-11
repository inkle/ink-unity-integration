using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.TerrainTools;

public class InkTranslatorManager : EditorWindow
{
    private string rootDirectory = "Assets/Dialogs/Ink";
    private List<MissingFileInfo> missingFiles = new List<MissingFileInfo>();
    private List<ComparatorInfo> comparatorFiles = new List<ComparatorInfo>();
    private Vector2 scrollPosition;
    private FileSystemWatcher fileWatcher;

    [MenuItem("Window/Ink Translator Manager %#t", false, 2300)]
    public static void ShowWindow()
    {
        GetWindow<InkTranslatorManager>("Ink Translator Manager");
    }


    private void OnEnable()
    {
        if (Directory.Exists(rootDirectory))
        {
            fileWatcher = new FileSystemWatcher(rootDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                Filter = "*.ink"
            };

            fileWatcher.Changed += OnFilesChanged;
            fileWatcher.Created += OnFilesChanged;
            fileWatcher.Deleted += OnFilesChanged;
            fileWatcher.Renamed += OnFilesChanged;

            fileWatcher.EnableRaisingEvents = true;
        }
    }

    private void OnDisable()
    {
        if (fileWatcher != null)
        {
            fileWatcher.Changed -= OnFilesChanged;
            fileWatcher.Created -= OnFilesChanged;
            fileWatcher.Deleted -= OnFilesChanged;
            fileWatcher.Renamed -= OnFilesChanged;

            fileWatcher.Dispose();
            fileWatcher = null;
        }
    }

    private void OnFilesChanged(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"File {e.ChangeType}: {e.FullPath}");
        CheckMissingFiles();
        Repaint();
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.MouseDown)
            Repaint();

        GUILayout.Label("Ink Manager", EditorStyles.boldLabel);

        GUILayout.Label("Root Directory:");
        rootDirectory = GUILayout.TextField(rootDirectory);

        if (GUILayout.Button("Force File Checking"))
            CheckMissingFiles();

        GUILayout.Space(5);
        DrawHorizontalLine();
        GUILayout.Space(5);

        DisplayMissingFiles();

        GUILayout.Space(5);
        DrawHorizontalLine();
        GUILayout.Space(5);

        DisplayComparatorFiles();

        GUILayout.Space(5);
        DrawHorizontalLine();
        GUILayout.Space(5);

        if (GUILayout.Button("Select and Compare Files"))
            ShowForceCompareWindow();
        GUILayout.EndScrollView();
    }

    private void DrawHorizontalLine(Color? color = null)
    {
        Color originalColor = GUI.color;
        GUI.color = color ?? Color.black;
        GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUI.color = originalColor;
    }

    private void CheckMissingFiles()
    {
        missingFiles.Clear();
        comparatorFiles.Clear();

        if (!Directory.Exists(rootDirectory))
        {
            Debug.LogError($"Directory not found: {rootDirectory}");
            return;
        }

        var directories = Directory.GetDirectories(rootDirectory);
        if (directories.Length < 2)
        {
            Debug.LogError("There must be at least two language folders in the root directory.");
            return;
        }

        var directoryFiles = directories.ToDictionary(
            dir => dir,
            dir => Directory.GetFiles(dir, "*.ink", SearchOption.TopDirectoryOnly)
                            .Select(Path.GetFileName)
                            .ToHashSet()
        );

        var allFiles = new HashSet<string>(directoryFiles.Values.SelectMany(files => files));

        foreach (var file in allFiles)
        {
            foreach (var dir in directories)
            {
                if (!directoryFiles[dir].Contains(file))
                {
                    if (!missingFiles.Any(m => m.FileName == file && m.Directory == Path.GetFileName(dir)))
                    {
                        missingFiles.Add(new MissingFileInfo
                        {
                            FileName = file,
                            Directory = Path.GetFileName(dir),
                            FilePath = Path.Combine(dir, file)
                        });
                    }
                }
            }
        }

        foreach (var file in allFiles)
        {
            var filePaths = directories
                .Where(dir => directoryFiles[dir].Contains(file))
                .Select(dir => Path.Combine(dir, file))
                .ToList();

            if (filePaths.Count > 1)
            {
                var lineCounts = filePaths.ToDictionary(
                    path => path,
                    path => File.ReadAllLines(path).Length
                );

                var mostLines = lineCounts.Values.Max();
                var leastLines = lineCounts.Values.Min();

                if (mostLines != leastLines)
                {
                    comparatorFiles.Add(new ComparatorInfo
                    {
                        FileName = file,
                        DirectoryWithMostLines = Path.GetFileName(Path.GetDirectoryName(lineCounts.First(kvp => kvp.Value == mostLines).Key)),
                        DirectoryWithLeastLines = Path.GetFileName(Path.GetDirectoryName(lineCounts.First(kvp => kvp.Value == leastLines).Key)),
                        MostLines = mostLines,
                        LeastLines = leastLines,
                        FilePath1 = lineCounts.First(kvp => kvp.Value == mostLines).Key,
                        FilePath2 = lineCounts.First(kvp => kvp.Value == leastLines).Key
                    });
                }
            }
        }

        if (missingFiles.Count == 0 && comparatorFiles.Count == 0)
            Debug.Log("No missing files or line differences found.");
    }

    private void DisplayMissingFiles()
    {
        GUILayout.Label("Missing Files:", EditorStyles.boldLabel);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Missing", GUILayout.Width(200));
        GUILayout.Label("Directory", GUILayout.Width(200));
        GUILayout.EndHorizontal();

        foreach (var missingFile in missingFiles)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(missingFile.FileName, GUILayout.Width(200));
            GUILayout.Label(missingFile.Directory, GUILayout.Width(200));

            if (GUILayout.Button("Create Copy"))
                ShowCopyOptions(missingFile.FilePath, false);

            if (GUILayout.Button("Create Copy and Compare"))
                ShowCopyOptions(missingFile.FilePath, true);

            GUILayout.EndHorizontal();
        }
    }

    private void DisplayComparatorFiles()
    {
        GUILayout.Label("Comparator:", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Ink File", GUILayout.Width(200));
        GUILayout.Label("Most Lines File", GUILayout.Width(200));
        GUILayout.Label("Least Lines File", GUILayout.Width(200));
        GUILayout.EndHorizontal();

        foreach (var comparator in comparatorFiles)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(comparator.FileName, GUILayout.Width(200));
            GUILayout.Label($"{comparator.DirectoryWithMostLines} ({comparator.MostLines})", GUILayout.Width(200));
            GUILayout.Label($"{comparator.DirectoryWithLeastLines} ({comparator.LeastLines})", GUILayout.Width(200));

            if (GUILayout.Button("Check Difference"))
                ShowComparisonWindow(comparator.FilePath1, comparator.FilePath2);

            GUILayout.EndHorizontal();
        }

    }

    private void ShowCopyOptions(string filePath, bool copyAndCompare)
    {
        var directories = Directory.GetDirectories(rootDirectory);
        var directoriesWithFile = directories
            .Where(dir => File.Exists(Path.Combine(dir, Path.GetFileName(filePath))))
            .ToList();

        if (!directoriesWithFile.Any())
        {
            Debug.LogError($"File not found in any directories: {filePath}");
            return;
        }

        GenericMenu menu = new GenericMenu();
        foreach (var dir in directoriesWithFile)
        {
            string buttonText = $"Copy from {Path.GetFileName(dir)}";
            menu.AddItem(new GUIContent(buttonText), false, () => CopyFile(filePath, dir, copyAndCompare));
        }
        menu.ShowAsContext();
    }

    private void CopyFile(string filePath, string sourceDir, bool copyAndCompare)
    {
        var fileName = Path.GetFileName(filePath);
        var sourceFile = Path.Combine(sourceDir, fileName);
        var destinationFile = Path.Combine(Path.GetDirectoryName(filePath), fileName);

        if (File.Exists(sourceFile))
        {
            File.Copy(sourceFile, destinationFile, true);
            Debug.Log($"Copied {fileName} from {sourceDir} to {Path.GetDirectoryName(filePath)}");

            if (copyAndCompare)
                ShowComparisonWindow(sourceFile, destinationFile);

            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError($"Failed to copy file from {sourceDir}: file does not exist.");
        }
    }

    private void ShowComparisonWindow(string filePath1, string filePath2)
    {
        ComparisonWindow.Open(filePath1, filePath2);
    }

    private void ShowForceCompareWindow()
    {
        string filePath1 = EditorUtility.OpenFilePanel("Select First Ink File", rootDirectory, "ink");
        if (string.IsNullOrEmpty(filePath1)) return;

        string filePath2 = EditorUtility.OpenFilePanel("Select Second Ink File", rootDirectory, "ink");
        if (string.IsNullOrEmpty(filePath2)) return;

        ShowComparisonWindow(filePath1, filePath2);
    }

    private class MissingFileInfo
    {
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string FilePath { get; set; }
    }

    private class ComparatorInfo
    {
        public string FileName { get; set; }
        public string DirectoryWithMostLines { get; set; }
        public string DirectoryWithLeastLines { get; set; }
        public int MostLines { get; set; }
        public int LeastLines { get; set; }
        public string FilePath1 { get; set; }
        public string FilePath2 { get; set; }
    }
}

public class ComparisonWindow : EditorWindow
{
    private string fileContent1;
    private string fileContent2;
    private string filePath1;
    private string filePath2;
    private Vector2 scrollPosition1;
    private Vector2 scrollPosition2;

    public static void Open(string path1, string path2)
    {
        ComparisonWindow window = GetWindow<ComparisonWindow>("Compare Ink Files");
        window.filePath1 = path1;
        window.filePath2 = path2;
        window.LoadFiles();
    }

    private void LoadFiles()
    {
        fileContent1 = File.ReadAllText(filePath1);
        fileContent2 = File.ReadAllText(filePath2);
    }

    private void OnGUI()
    {
        GUILayout.Label("File Comparison", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label(filePath1, GUILayout.Width(320));
        GUILayout.Label(filePath2, GUILayout.Width(320));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1, GUILayout.Width(300), GUILayout.Height(400));
        fileContent1 = GUILayout.TextArea(fileContent1, GUILayout.Width(300), GUILayout.Height(400));
        GUILayout.EndScrollView();

        GUILayout.Space(20);

        scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, GUILayout.Width(300), GUILayout.Height(400));
        fileContent2 = GUILayout.TextArea(fileContent2, GUILayout.Width(300), GUILayout.Height(400));
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Save"))
        {
            SaveFiles();
            Close();
        }
    }

    private void SaveFiles()
    {
        File.WriteAllText(filePath1, fileContent1);
        File.WriteAllText(filePath2, fileContent2);
        AssetDatabase.Refresh();
        Debug.Log("Files saved successfully.");
    }
}