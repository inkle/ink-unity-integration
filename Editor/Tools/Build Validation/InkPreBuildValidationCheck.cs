using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.Text;
using Ink.UnityIntegration;
using System.Linq;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

class InkPreBuildValidationCheck : 
#if UNITY_2018_1_OR_NEWER
IPreprocessBuildWithReport
#else
IPreprocessBuild
#endif
{
	public int callbackOrder { get { return 0; } }
	
    #if UNITY_2018_1_OR_NEWER
    public void OnPreprocessBuild(BuildReport report) {
        PreprocessValidationStep();
    }
    #else
    public void OnPreprocessBuild(BuildTarget target, string path) {
		PreprocessValidationStep();
	}
    #endif

    static void PreprocessValidationStep () {
        // If we're compiling, we've throw an error to cancel the build. Exit out immediately.
        if(!AssertNotCompiling()) return;
        EnsureInkIsCompiled();
    }
    
    // Prevent building if ink is currently compiling. 
    // Ideally we'd force it to complete instantly. 
    // It seems you can do this with WaitHandle.WaitAll but I'm out of my depth!
    // Info here - https://stackoverflow.com/questions/540078/wait-for-pooled-threads-to-complete
    static bool AssertNotCompiling () {
        if(InkCompiler.executingCompilationStack) {
            StringBuilder sb = new StringBuilder("Ink is currently compiling!");
            var errorString = sb.ToString();
            InkCompiler.SetBuildBlocked();
            if(UnityEditor.EditorUtility.DisplayDialog("Ink Build Error!", errorString, "Ok")) {
                Debug.LogError(errorString);
            }
            return false;
        }
        return true;
    }
    
    // Immediately compile any files that aren't compiled and should be.
    static void EnsureInkIsCompiled () {
        var filesToRecompile = InkLibrary.GetFilesRequiringRecompile();
        if(filesToRecompile.Any()) {
            if(InkSettings.instance.compileAllFilesAutomatically) {
                InkCompiler.CompileInk(filesToRecompile.ToArray(), true, null);
            }
        }
    }
}