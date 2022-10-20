using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using AssetStoreTools.Utility.Json;

namespace AssetStoreTools.Uploader
{
    internal static class PackageExporter
    {
        private const string ExportMethodWithoutDependencies = "UnityEditor.PackageUtility.ExportPackage";
        private const string ExportMethodWithDependencies = "UnityEditor.PackageUtility.ExportPackageAndPackageManagerManifest";

        private const string ProgressBarTitle = "Exporting Package";
        private const string ProgressBarStep1 = "Saving Assets...";
        private const string ProgressBarStep2 = "Gathering files...";
        private const string ProgressBarStep3 = "Compressing package...";

        private const string TemporaryExportPathName = "CustomExport";
        private const string PackagesLockPath = "Packages/packages-lock.json";
        private const string ManifestJsonPath = "Packages/manifest.json";

        internal class ExportResult
        {
            public bool Success;
            public string ExportedPath;
            public ASError Error;

            public static implicit operator bool(ExportResult value)
            {
                return value != null && value.Success;
            }
        }

        public static async Task<ExportResult> ExportPackage(string[] exportPaths, string outputFilename,
            bool includeDependencies, bool isCompleteProject, bool useCustomExporter = false)
        {
            if (exportPaths == null || exportPaths.Length == 0)
                return new ExportResult() { Success = false, Error = ASError.GetGenericError(new ArgumentException("Package Exporting failed: received an invalid export paths array")) };

            EditorUtility.DisplayProgressBar(ProgressBarTitle, ProgressBarStep1, 0.1f);
            AssetDatabase.SaveAssets();

            if (isCompleteProject)
                exportPaths = IncludeProjectSettings(exportPaths);

            try
            {
                if (!useCustomExporter)
                    await ExportPackageNative(exportPaths, outputFilename, includeDependencies);
                else
                    ExportPackageCustom(exportPaths, outputFilename, includeDependencies);

                ASDebug.Log($"Package file has been created at {outputFilename}");
                return new ExportResult() { Success = true, ExportedPath = outputFilename };
            }
            catch (Exception e)
            {
                return new ExportResult() { Success = false, Error = ASError.GetGenericError(e) };
            }
            finally
            {
                PostExportCleanup();
            }
        }

        private static string[] IncludeProjectSettings(string[] exportPaths)
        {
            var updatedExportPaths = new string[exportPaths.Length + 1];
            exportPaths.CopyTo(updatedExportPaths, 0);
            updatedExportPaths[updatedExportPaths.Length - 1] = "ProjectSettings";
            return updatedExportPaths;
        }

        private static async Task ExportPackageNative(string[] exportPaths, string outputFilename, bool includeDependencies)
        {
            ASDebug.Log("Using native package exporter");
            var guids = GetGuids(exportPaths, out bool onlyFolders);

            if (guids.Length == 0 || onlyFolders)
                throw new ArgumentException("Package Exporting failed: provided export paths are empty or only contain empty folders");

            string exportMethod = ExportMethodWithoutDependencies;
            if (includeDependencies)
                exportMethod = ExportMethodWithDependencies;

            var split = exportMethod.Split('.');
            var assembly = Assembly.Load(split[0]); // UnityEditor
            var typeName = $"{split[0]}.{split[1]}"; // UnityEditor.PackageUtility
            var methodName = split[2]; // ExportPackage or ExportPackageAndPackageManagerManifest

            var type = assembly.GetType(typeName);
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null, new Type[] { typeof(string[]), typeof(string) }, null);

            ASDebug.Log("Invoking native export method");

            method?.Invoke(null, new object[] { guids, outputFilename });

            // The internal exporter methods are asynchronous, therefore
            // we need to wait for exporting to finish before returning
            await Task.Run(() =>
            {
                while (!File.Exists(outputFilename))
                    Thread.Sleep(100);
            });
        }

        private static string[] GetGuids(string[] exportPaths, out bool onlyFolders)
        {
            var guids = new List<string>();
            onlyFolders = true;

            foreach (var exportPath in exportPaths)
            {
                var assetPaths = GetAssetPaths(exportPath);

                foreach (var assetPath in assetPaths)
                {
                    var guid = GetAssetGuid(assetPath);
                    if (string.IsNullOrEmpty(guid))
                        continue;

                    guids.Add(guid);
                    if (onlyFolders == true && (File.Exists(assetPath)))
                        onlyFolders = false;
                }
            }

            return guids.ToArray();
        }

        private static string[] GetAssetPaths(string rootPath)
        {
            // To-do: slight optimization is possible in the future by having a list of excluded folders/file extensions
            List<string> paths = new List<string>();

            // Add files within given directory
            var filePaths = Directory.GetFiles(rootPath).Select(p => p.Replace('\\', '/')).ToArray();
            paths.AddRange(filePaths);

            // Add directories within given directory
            var directoryPaths = Directory.GetDirectories(rootPath).Select(p => p.Replace('\\', '/')).ToArray();
            foreach (var nestedDirectory in directoryPaths)
                paths.AddRange(GetAssetPaths(nestedDirectory));

            // Add the given directory itself if it is not empty
            if (filePaths.Length > 0 || directoryPaths.Length > 0)
                paths.Add(rootPath);

            return paths.ToArray();
        }

        private static string GetAssetGuid(string assetPath)
        {
            // Skip meta files as they do not have guids
            if (assetPath.EndsWith(".meta"))
                return string.Empty;

            // Attempt retrieving guid from the Asset Database first
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (guid != string.Empty)
                return guid;

            // If guid was not retrieved, it's possible that the file is within a hidden folder (e.g Samples~).
            // We'll need to read its meta file to retrieve the guid
            // To-do: handle hidden folders without meta files
            var metaPath = $"{assetPath}.meta";

            if (!File.Exists(metaPath))
                return string.Empty;

            using (StreamReader reader = new StreamReader(metaPath))
            {
                string line;
                while ((line = reader.ReadLine()) != string.Empty)
                {
                    if (!line.StartsWith("guid:"))
                        continue;
                    var metaGuid = line.Substring("guid:".Length).Trim();
                    return metaGuid;
                }
            }

            return string.Empty;
        }

        private static void PostExportCleanup()
        {
            EditorUtility.ClearProgressBar();
            var tempExportPath = GetTemporaryExportPath();
            if (Directory.Exists(tempExportPath))
                Directory.Delete(tempExportPath, true);
        }

        #region Experimental

        private static void ExportPackageCustom(string[] exportPaths, string outputFilename, bool includeDependencies)
        {
            ASDebug.Log("Using custom package exporter");
            // Create a temporary export path
            var temporaryExportPath = GetTemporaryExportPath();
            if (!Directory.Exists(temporaryExportPath))
                Directory.CreateDirectory(temporaryExportPath);

            // Construct an unzipped package structure
            CreateTempPackageStructure(exportPaths, temporaryExportPath, includeDependencies);

            // Build a .unitypackage file from the temporary folder
            CreateUnityPackage(temporaryExportPath, outputFilename);

            EditorUtility.RevealInFinder(outputFilename);
        }

        private static string GetTemporaryExportPath()
        {
            return $"{AssetStoreCache.TempCachePath}/{TemporaryExportPathName}";
        }

        private static void CreateTempPackageStructure(string[] exportPaths, string tempOutputPath, bool includeDependencies)
        {
            EditorUtility.DisplayProgressBar(ProgressBarTitle, ProgressBarStep2, 0.4f);
            var pathGuidPairs = GetPathGuidPairs(exportPaths);

            // Caching asset previews takes time, so we'll start doing it as we
            // iterate through assets and only retrieve them after generating the rest
            // of the package structure
            AssetPreview.SetPreviewTextureCacheSize(pathGuidPairs.Count + 100);
            var pathObjectPairs = new Dictionary<string, UnityEngine.Object>();

            foreach (var pair in pathGuidPairs)
            {
                var originalAssetPath = pair.Key;
                var outputAssetPath = $"{tempOutputPath}/{pair.Value}";
                Directory.CreateDirectory(outputAssetPath);

                // Every exported asset has a pathname file
                using (StreamWriter writer = new StreamWriter($"{outputAssetPath}/pathname"))
                    writer.Write(originalAssetPath);

                // Only files (not folders) have an asset file
                if (File.Exists(originalAssetPath))
                    File.Copy(originalAssetPath, $"{outputAssetPath}/asset");

                // Most files and folders have an asset.meta file (but ProjectSettings folder assets do not)
                if (File.Exists($"{originalAssetPath}.meta"))
                    File.Copy($"{originalAssetPath}.meta", $"{outputAssetPath}/asset.meta");

                // To-do: handle previews in hidden folders as they are not part of the AssetDatabase
                var previewObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(originalAssetPath);
                if (previewObject == null)
                    continue;
                // Start caching the asset preview
                AssetPreview.GetAssetPreview(previewObject);
                pathObjectPairs.Add(outputAssetPath, previewObject);
            }

            WritePreviewTextures(pathObjectPairs);

            if (!includeDependencies)
                return;

            // At this time we only include either all or none of the UPM dependencies.
            // In the future, we'll be able to allow selective dependencies from the manifest.json
            var tempManifestDirectoryPath = $"{tempOutputPath}/packagemanagermanifest";
            Directory.CreateDirectory(tempManifestDirectoryPath);
            var tempManifestFilePath = $"{tempManifestDirectoryPath}/asset";

            // manifest.json may also need to be tweaked to exclude local packages, because once
            // the package gets imported on another machine, local paths will likely be invalid
            var manifestJsonString = File.ReadAllText(ManifestJsonPath);
            var manifestJson = JSONParser.SimpleParse(manifestJsonString);
            var dependencies = manifestJson["dependencies"].AsDict();

            var allPackages = GetAllLocalPackages();

            foreach (var package in allPackages)
            {
                var packageSource = package["source"].AsString();
                if (packageSource != "local" && packageSource != "embedded")
                    continue;

                var packageName = package["name"].AsString();

                // Print out a list of excluded packages - Asset Store Tools warning would always be printed, therefore is excluded
                if (packageName != "com.unity.asset-store-tools")
                    UnityEngine.Debug.LogWarning($"Found an unsupported Package Manager dependency type \"{packageSource}\". " +
                        $"These dependencies are not supported in the project's manifest.json and will be skipped: \"{packageName}\"");

                if (!dependencies.ContainsKey(packageName))
                    continue;

                dependencies.Remove(packageName);
            }

            File.WriteAllText(tempManifestFilePath, manifestJson.ToString());
        }

        private static Dictionary<string, string> GetPathGuidPairs(string[] exportPaths)
        {
            var pathGuidPairs = new Dictionary<string, string>();

            foreach (var exportPath in exportPaths)
            {
                var assetPaths = GetAssetPaths(exportPath);

                foreach (var assetPath in assetPaths)
                {
                    var guid = GetAssetGuid(assetPath);
                    if (string.IsNullOrEmpty(guid))
                        continue;

                    pathGuidPairs.Add(assetPath, guid);
                }
            }

            return pathGuidPairs;
        }

        private static void WritePreviewTextures(Dictionary<string, UnityEngine.Object> pathObjectPairs)
        {
            foreach (var kvp in pathObjectPairs)
            {
                // Texture will likely not be loaded at first, so the first call starts the process.
                // In 2019.4, textures are loaded on import, but starting with 2020.3
                // textures are only loaded on demand, so it's expected to take longer
                AssetPreview.GetAssetPreview(kvp.Value);

                // Wait until the texture is no longer loading
                // IMPORTANT: AssetPreview.IsLoadingPreview currently never returns true in 2020.3 and 2021.3 (Issue 1323729)
                while (AssetPreview.IsLoadingAssetPreview(kvp.Value.GetInstanceID()))
                {
                    // 2019.4: AssetPreview.IsLoadingAssetPreview value only changes after invoking the getter method
                    if (AssetPreview.GetAssetPreview(kvp.Value))
                        break;
                    Thread.Sleep(5);
                }

                // Texture may have finished loading before being assigned so attempt to retrieve it one more time
                var tex = AssetPreview.GetAssetPreview(kvp.Value);
                if (tex != null)
                    File.WriteAllBytes(kvp.Key + "/preview.png", tex.EncodeToPNG());
            }
        }

        private static void CreateUnityPackage(string pathToArchive, string outputPath)
        {
            if (Directory.GetDirectories(pathToArchive).Length == 0)
                throw new InvalidOperationException("Unable to export package. The specified path is empty");

            EditorUtility.DisplayProgressBar(ProgressBarTitle, ProgressBarStep3, 0.5f);

            // Archiving process working path will be set to the
            // temporary package path so adjust the output path accordingly
            if (!Path.IsPathRooted(outputPath))
                outputPath = $"{Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)}/{outputPath}";

#if UNITY_EDITOR_WIN
            CreateUnityPackageUniversal(pathToArchive, outputPath);
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            CreateUnityPackageOsxLinux(pathToArchive, outputPath);
#endif
        }

        private static void CreateUnityPackageUniversal(string pathToArchive, string outputPath)
        {
            var _7zPath = EditorApplication.applicationContentsPath;
#if UNITY_EDITOR_WIN
            _7zPath = Path.Combine(_7zPath, "Tools", "7z.exe");
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            _7zPath = Path.Combine(_7zPath, "Tools", "7za");
#endif
            if (!File.Exists(_7zPath))
                throw new FileNotFoundException("Archiving utility was not found in your Unity installation directory");

            var argumentsTar = $"a -r -ttar -y -bd archtemp.tar .";
            var result = StartProcess(_7zPath, argumentsTar, pathToArchive);
            if (result != 0)
                throw new Exception("Failed to compress the package");

            // Create a GZIP archive
            var argumentsGzip = $"a -tgzip -bd -y \"{outputPath}\" archtemp.tar";
            result = StartProcess(_7zPath, argumentsGzip, pathToArchive);
            if (result != 0)
                throw new Exception("Failed to compress the package");
        }

        private static void CreateUnityPackageOsxLinux(string pathToArchive, string outputPath)
        {
            var tarPath = "/usr/bin/tar";

            if (!File.Exists(tarPath))
            {
                // Fallback to the universal export method
                ASDebug.LogWarning("'/usr/bin/tar' executable not found. Falling back to 7za");
                CreateUnityPackageUniversal(pathToArchive, outputPath);
                return;
            }

            // Create a TAR archive
            var arguments = $"-czpf \"{outputPath}\" .";
            var result = StartProcess(tarPath, arguments, pathToArchive);
            if (result != 0)
                throw new Exception("Failed to compress the package");
        }

        private static int StartProcess(string processPath, string arguments, string workingDirectory)
        {
            var info = new ProcessStartInfo()
            {
                FileName = processPath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (Process process = Process.Start(info))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        #endregion

        #region Utility

        public static List<JsonValue> GetAllLocalPackages()
        {
            try
            {
#if !UNITY_2019_4_0
                if (File.Exists(PackagesLockPath))
                    return CollectPackagesFromPackagesLock();
#endif
                // Fallback for 2019.4.0f1 which does not have a packages-lock.json (or it is outdated if using a downgraded project)
                return CollectPackagesManual();
            }
            catch
            {
                return null;
            }
        }

        private static List<JsonValue> CollectPackagesFromPackagesLock()
        {
            string packageLockJsonString = File.ReadAllText(PackagesLockPath);
            JSONParser parser = new JSONParser(packageLockJsonString);
            var packageLockJson = parser.Parse();

            var packages = packageLockJson.Get("dependencies").AsDict();
            var localPackages = new List<JsonValue>();

            foreach (var kvp in packages)
            {
                var packageSource = kvp.Value.Get("source").AsString();

                if (!packageSource.Equals("embedded") && !packageSource.Equals("local"))
                    continue;

                var packagePath = kvp.Value.Get("version").AsString().Substring("file:".Length);

                if (packageSource.Equals("embedded"))
                    packagePath = $"Packages/{packagePath}";
                else if (packageSource.Equals("local") && packagePath.StartsWith("../"))
                    packagePath = packagePath.Substring("../".Length);

                JsonValue localPackage = new JsonValue
                {
                    ["name"] = JsonValue.NewString(kvp.Key),
                    ["source"] = JsonValue.NewString(kvp.Value.Get("source")),
                    ["path_absolute"] = JsonValue.NewString(packagePath),
                    ["path_assetdb"] = JsonValue.NewString($"Packages/{kvp.Key}")
                };

                localPackages.Add(localPackage);
            }

            return localPackages;
        }

        private static List<JsonValue> CollectPackagesManual()
        {
            // Scrape manifest.json for local packages
            string manifestJsonString = File.ReadAllText(ManifestJsonPath);
            JSONParser parser = new JSONParser(manifestJsonString);
            var manifestJson = parser.Parse();

            var packages = manifestJson.Get("dependencies").AsDict();
            var localPackages = new List<JsonValue>();

            foreach (var kvp in packages)
            {
                if (!kvp.Value.AsString().StartsWith("file:"))
                    continue;

                var packagePath = kvp.Value.AsString().Substring("file:".Length);
                if (packagePath.StartsWith("../"))
                    packagePath = packagePath.Substring("../".Length);

                JsonValue localPackage = new JsonValue
                {
                    ["name"] = JsonValue.NewString(kvp.Key),
                    ["source"] = JsonValue.NewString("local"),
                    ["path_absolute"] = JsonValue.NewString(packagePath),
                    ["path_assetdb"] = JsonValue.NewString($"Packages/{kvp.Key}")
                };

                localPackages.Add(localPackage);
            }

            // Scrape Packages folder for embedded packages
            foreach (var directory in Directory.GetDirectories("Packages"))
            {
                var path = directory.Replace("\\", "/");
                var packageManifestPath = $"{path}/package.json";

                if (!File.Exists(packageManifestPath))
                    continue;

                string packageManifestJsonString = File.ReadAllText(packageManifestPath);
                parser = new JSONParser(packageManifestJsonString);
                var packageManifestJson = parser.Parse();

                var packageName = packageManifestJson["name"].AsString();

                JsonValue embeddedPackage = new JsonValue()
                {
                    ["name"] = JsonValue.NewString(packageName),
                    ["source"] = JsonValue.NewString("embedded"),
                    ["path_absolute"] = JsonValue.NewString(path),
                    ["path_assetdb"] = JsonValue.NewString($"Packages/{packageName}")
                };

                localPackages.Add(embeddedPackage);
            }

            return localPackages;
        }

#endregion
    }
}