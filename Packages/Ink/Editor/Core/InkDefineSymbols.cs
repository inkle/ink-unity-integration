using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Ink.UnityIntegration {
    // This class adds define symbols for the ink runtime and editor. Handy for conditional compilation.
    public class InkDefineSymbols : IActiveBuildTargetChanged {
        public const string inkRuntimeDefine = "INK_RUNTIME";
        public const string inkEditorDefine = "INK_EDITOR";
        static readonly string[] inkDefines = {inkRuntimeDefine, inkEditorDefine};
        
        const char DEFINE_SEPARATOR = ';';
        
        public int callbackOrder => 0;
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget) {
            AddGlobalDefine();
        }
			
        // Adds the default define symbols to the active build target if they're not already present. 
        public static void AddGlobalDefine() {
            Add(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), inkDefines);
        }

        // Removes the default define symbols from the active build target if they're present. 
        public static void RemoveGlobalDefine() {
            Remove(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), inkDefines);
        }
			
        // Checks if all the default define symbols are present. 
        public static bool HasGlobalDefines() {
            return Exists(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), inkDefines);
        }
			
			
        static void Add(BuildTargetGroup buildTargetGroup, params string[] defines) {
            var allDefines = new List<string>();
            string definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            allDefines = definesStr.Split(DEFINE_SEPARATOR).ToList();
            allDefines.AddRange(defines.Except(allDefines));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(DEFINE_SEPARATOR.ToString(), allDefines.ToArray()));
        }
				
				
        static void Remove(BuildTargetGroup buildTargetGroup, params string[] defines) {
            string definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            var existingDefines = definesStr.Split(DEFINE_SEPARATOR).ToList();
            var newDefines = existingDefines.Except(defines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(DEFINE_SEPARATOR.ToString(), newDefines.ToArray()));
        }

        static bool Exists(BuildTargetGroup buildTargetGroup, params string[] defines) {
            string definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var existingDefines = definesStr.Split(DEFINE_SEPARATOR).ToList();
            return existingDefines.Contains("INK_RUNTIME") && existingDefines.Contains("INK_EDITOR");
        }
        
        // This code works but it throws errors that can't be caught for non-installed build target groups. Instead I've switched to adding them when the active build target changes 
        // foreach (BuildTargetGroup buildTargetGroup in (BuildTargetGroup[]) Enum.GetValues(typeof(BuildTargetGroup))) {
        // if(BuildPipeline.IsBuildTargetSupported(buildTargetGroup, BuildTarget.NoTarget))
        // Add(BuildPipeline.GetBuildTargetGroup(buildTargetGroup), inkDefines);
        // }

    }
}