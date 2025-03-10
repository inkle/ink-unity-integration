using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using Ink.UnityIntegration;
using UnityEditor;
using Ink;

/// <summary>
/// Automatically compiles .ink assets each time they are imported, and creates an InkFile asset.
/// </summary>
[ScriptedImporter(1, "ink")]
public class InkImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var inkFile = ScriptableObject.CreateInstance<InkFile>();
        var absolutePath = InkEditorUtils.UnityRelativeToAbsolutePath(ctx.assetPath);
        var inputString = File.ReadAllText(ctx.assetPath);
        var compiler = new Compiler(inputString, new Compiler.Options
        {
            countAllVisits = true,
            fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(absolutePath)),
            errorHandler = (string message, ErrorType type) => {
                InkCompilerLog log;
                if(InkCompilerLog.TryParse(message, out log)) {
                    if(string.IsNullOrEmpty(log.relativeFilePath)) log.relativeFilePath = Path.GetFileName(absolutePath);
                    switch (log.type)
                    {
                        case ErrorType.Error: 
                            inkFile.errors.Add(log); 
                            Debug.LogError("Ink "+log.type+" for "+Path.GetFileName(absolutePath)+": "+log.content + " (at "+log.relativeFilePath+":"+log.lineNumber+")", inkFile);
                            break;
                        case ErrorType.Warning: 
                            inkFile.warnings.Add(log); 
                            Debug.LogWarning("Ink "+log.type+" for "+Path.GetFileName(absolutePath)+": "+log.content + " (at "+log.relativeFilePath+" "+log.lineNumber+")", inkFile);
                            break;
                        case ErrorType.Author: 
                            inkFile.todos.Add(log);
                            if (InkSettings.instance.printInkLogsInConsoleOnCompile)
                            {
                                Debug.Log("Ink Log for "+Path.GetFileName(absolutePath)+": "+log.content + " (at "+log.relativeFilePath+" "+log.lineNumber+")", inkFile);
                            }
                            break;
                    }
                    
                } else {
                    Debug.LogWarning("Couldn't parse log "+message);
                }
            }
        });
        
        try {
            var compiledStory = compiler.Compile();

            if (compiledStory != null)
                inkFile.Initialize(compiledStory.ToJson());
        } catch (System.SystemException e) {
            Debug.LogException(e);
        }

        /** inform Unity about dependencies defined in ink files so that if included
        files are modified, the main file is also reimported.**/
        var includes = InkFile.InkIncludeParser.ParseIncludes(inputString);
        foreach (var include in includes) {
            // Unity wants the path relative to the Assets folder
            var assetPath = Path.Combine(Path.GetDirectoryName(ctx.assetPath), include);
            ctx.DependsOnSourceAsset(assetPath);
        }

        ctx.AddObjectToAsset("InkFile", inkFile);
        ctx.SetMainObject(inkFile);
    }
}