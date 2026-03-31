using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Collections.Generic;

namespace Ink.UnityIntegration
{
    /// <summary>
    /// Automatically compiles .ink assets each time they are imported, and creates an InkFile asset.
    /// </summary>
    [ScriptedImporter(1, "ink")]
    public class InkImporter : ScriptedImporter
    {
        [SerializeField]
        [Tooltip("Set this to false to stop the Ink file from being compiled on import. This is intended for " +
        "Ink files that aren't intended to be compiled as standalone files (aka master files) because they are included in other " +
        "Ink files and require other files to be compiled correctly e.g. global variables are defined in another file.")]
        private bool isMaster = true;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                /** Declare dependencies on all nested includes so that Unity correctly propagates changes.
                We cannot guarantee the order in which ScriptedImporters run, so we need to capture this
                information for all .ink assets, regardless of whether we compile the files standalone. **/
                var pathsToAllIncludes = new HashSet<string>();
                GetIncludesRecursively(ctx.assetPath, pathsToAllIncludes);
                foreach (var path in pathsToAllIncludes)
                {
                    ctx.DependsOnSourceAsset(path);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (!isMaster)
            {
                return;
            }

            var inkFile = ScriptableObject.CreateInstance<InkFile>();
            var absolutePath = InkEditorUtils.UnityRelativeToAbsolutePath(ctx.assetPath);
            var inputString = File.ReadAllText(ctx.assetPath);
            var compiler = new Compiler(inputString, new Compiler.Options
            {
                countAllVisits = true,
                fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(absolutePath)),
                errorHandler = (string message, ErrorType type) =>
                {
                    InkCompilerLog log;
                    if (InkCompilerLog.TryParse(message, out log))
                    {
                        if (string.IsNullOrEmpty(log.relativeFilePath)) log.relativeFilePath = Path.GetFileName(absolutePath);
                        switch (log.type)
                        {
                            case ErrorType.Error:
                                inkFile.errors.Add(log);
                                Debug.LogError("Ink " + log.type + " for " + Path.GetFileName(absolutePath) + ": " + log.content + " (at " + log.relativeFilePath + ":" + log.lineNumber + ")", inkFile);
                                break;
                            case ErrorType.Warning:
                                inkFile.warnings.Add(log);
                                Debug.LogWarning("Ink " + log.type + " for " + Path.GetFileName(absolutePath) + ": " + log.content + " (at " + log.relativeFilePath + " " + log.lineNumber + ")", inkFile);
                                break;
                            case ErrorType.Author:
                                inkFile.todos.Add(log);
                                if (InkSettings.instance.printInkLogsInConsoleOnCompile)
                                {
                                    Debug.Log("Ink Log for " + Path.GetFileName(absolutePath) + ": " + log.content + " (at " + log.relativeFilePath + " " + log.lineNumber + ")", inkFile);
                                }
                                break;
                        }

                    }
                    else
                    {
                        Debug.LogWarning("Couldn't parse log " + message);
                    }
                }
            });

            try
            {
                var compiledStory = compiler.Compile();

                if (compiledStory != null)
                    inkFile.Initialize(compiledStory.ToJson());
            }
            catch (System.SystemException e)
            {
                Debug.LogException(e);
            }

            ctx.AddObjectToAsset("InkFile", inkFile);
            ctx.SetMainObject(inkFile);
        }
        
        /// <summary>
        /// Adds project-relative paths to all include files found while recursively searching in the specified file.
        /// </summary>
        /// <param name="currentFilePath"></param>
        /// <param name="allFoundIncludePaths"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private void GetIncludesRecursively(string currentFilePath, HashSet<string> allFoundIncludePaths)
        {
            var currentDirectory = Path.GetDirectoryName(currentFilePath);
            if (currentDirectory == null)
            {
                throw new DirectoryNotFoundException($"Could not find directory for file path {currentFilePath}.");
            }

            var currentFileContents = File.ReadAllText(currentFilePath);
            var includesForCurrentFile = InkFile.InkIncludeParser.ParseIncludes(currentFileContents);
            foreach (var include in includesForCurrentFile)
            {
                var includePath = InkEditorUtils.CombinePaths(currentDirectory, include);
                if (!allFoundIncludePaths.Add(includePath))
                {
                    // We have already processed this include
                    continue;
                }

                // Examine this include for any nested includes
                GetIncludesRecursively(includePath, allFoundIncludePaths);
            }
        }
    }
}