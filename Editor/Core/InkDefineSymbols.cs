using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Ink.UnityIntegration {
    /// <summary>
    /// Manages the INK_RUNTIME and INK_EDITOR scripting define symbols, handy for conditional compilation.
    /// Adds them for the active build target (and re-adds them when the active build target changes).
    /// </summary>
    public class InkDefineSymbols : IActiveBuildTargetChanged {
        public const string inkRuntimeDefine = "INK_RUNTIME";
        public const string inkEditorDefine = "INK_EDITOR";
        static readonly string[] inkDefines = {inkRuntimeDefine, inkEditorDefine};
        
        const char DEFINE_SEPARATOR = ';';
        
        public int callbackOrder => 0;
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget) {
            AddGlobalDefine();
        }
			
        /// <summary>Adds the ink define symbols to the active build target if they're not already present.</summary>
        public static void AddGlobalDefine() {
            Add(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), inkDefines);
        }

        /// <summary>Removes the ink define symbols from the active build target if they're present.</summary>
        public static void RemoveGlobalDefine() {
            Remove(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), inkDefines);
        }
			
        /// <summary>Returns true if the ink define symbols are present.</summary>
        public static bool HasGlobalDefines() {
            return Exists(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), inkDefines);
        }
			
			
        static void Add(BuildTargetGroup buildTargetGroup, params string[] defines) {
            var allDefines = new List<string>();
            string definesStr = GetDefines(buildTargetGroup);
            allDefines = definesStr.Split(DEFINE_SEPARATOR).ToList();
            allDefines.AddRange(defines.Except(allDefines));
            SetDefines(buildTargetGroup, string.Join(DEFINE_SEPARATOR.ToString(), allDefines.ToArray()));
        }
				
				
        static void Remove(BuildTargetGroup buildTargetGroup, params string[] defines) {
            string definesStr = GetDefines(buildTargetGroup);
            var existingDefines = definesStr.Split(DEFINE_SEPARATOR).ToList();
            var newDefines = existingDefines.Except(defines);
            SetDefines(buildTargetGroup, string.Join(DEFINE_SEPARATOR.ToString(), newDefines.ToArray()));
        }

        static bool Exists(BuildTargetGroup buildTargetGroup, params string[] defines) {
            string definesStr = GetDefines(BuildTargetGroup.Standalone);
            var existingDefines = definesStr.Split(DEFINE_SEPARATOR).ToList();
            return existingDefines.Contains("INK_RUNTIME") && existingDefines.Contains("INK_EDITOR");
        }

        static string GetDefines(BuildTargetGroup buildTargetGroup) {
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
        }

        static void SetDefines(BuildTargetGroup buildTargetGroup, string defines) {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), defines);
        }
        
        // This code works but it throws errors that can't be caught for non-installed build target groups. Instead I've switched to adding them when the active build target changes 
        // foreach (BuildTargetGroup buildTargetGroup in (BuildTargetGroup[]) Enum.GetValues(typeof(BuildTargetGroup))) {
        // if(BuildPipeline.IsBuildTargetSupported(buildTargetGroup, BuildTarget.NoTarget))
        // Add(BuildPipeline.GetBuildTargetGroup(buildTargetGroup), inkDefines);
        // }

    }
}