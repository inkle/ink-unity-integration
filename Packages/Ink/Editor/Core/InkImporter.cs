using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using Ink.UnityIntegration;
using UnityEditor;
using Ink;

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
                            Debug.LogError(log.content);
                            break;
                        case ErrorType.Warning: 
                            inkFile.warnings.Add(log); 
                            Debug.LogWarning(log.content);
                            break;
                        case ErrorType.Author: 
                            inkFile.todos.Add(log); 
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

        ctx.AddObjectToAsset("InkFile", inkFile);
        ctx.SetMainObject(inkFile);
    }
}