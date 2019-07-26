using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.Text;
using Ink.UnityIntegration;
using System.Linq;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

class InkPreBuildValidationCheck : 
#if UNITY_2019_1_OR_NEWER
IPreprocessBuildWithReport
#else
IPreprocessBuild
#endif
{
	public int callbackOrder { get { return 0; } }
	
    #if UNITY_2019_1_OR_NEWER
    public void OnPreprocessBuild(BuildReport report) {
        PreprocessValidationStep();
    }
    #else
    public void OnPreprocessBuild(BuildTarget target, string path) {
		PreprocessValidationStep();
	}
    #endif

    static void PreprocessValidationStep () {
        AssertNotCompiling();
        CheckForInvalidFiles();
    }

    static void AssertNotCompiling () {
        if(InkCompiler.compiling) {
            StringBuilder sb = new StringBuilder("Ink is currently compiling!");
            var errorString = sb.ToString();
            InkCompiler.buildBlocked = true;
            if(UnityEditor.EditorUtility.DisplayDialog("Ink Build Error!", errorString, "Ok")) {
                Debug.LogError(errorString);
            }
        }
    }
    // When syncronous compilation is allowed we should try to replace this error with a compile.
    static void CheckForInvalidFiles () {
        var filesToRecompile = InkLibrary.GetFilesRequiringRecompile();
        if(filesToRecompile.Any()) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("There are Ink files which should be compiled, but appear not to be. You can resolve this by either:");
            sb.AppendLine(" - Compiling your files via 'Assets/Recompile Ink'");
            var resolveStep = " - Disabling 'Compile Automatically' "+(InkSettings.Instance.compileAutomatically ? "in your Ink Settings file" : "for each of the files listed below");
            sb.AppendLine(resolveStep);
            sb.AppendLine();
            sb.AppendLine("Files:");
            var filesAsString = string.Join(", ", filesToRecompile.Select(x => x.filePath).ToArray());
            sb.AppendLine(filesAsString);
            var errorString = sb.ToString();
            if(!UnityEditor.EditorUtility.DisplayDialog("Ink Build Error!", errorString, "Build anyway", "Cancel build")) {
                Debug.LogError(errorString);
            } else {
                Debug.LogWarning(errorString);
            }
        }
    } 
}