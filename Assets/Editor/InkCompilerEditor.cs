using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {

	[CustomEditor(typeof(InkCompiler))]
	public class InkCompilerEditor : Editor {

		#pragma warning disable
		protected InkCompiler data;
		
		public void OnEnable() {
			data = (InkCompiler) target;
		}
		
		public bool RequiresConstantRepaint() {
			return true;
		}
		
		public override void OnInspectorGUI() {
			serializedObject.Update();
			var type = typeof(InkCompiler);

			EditorGUILayout.Toggle("Executing Compilation Stack", InkCompiler.executingCompilationStack);
			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("pendingCompilationStack"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("compilationStack"));
			// this.DrawDefaultInspector();

			var buildBlockedInfo = type.GetField("buildBlocked", BindingFlags.NonPublic | BindingFlags.Static);
			bool buildBlocked = (bool)buildBlockedInfo.GetValue(null);
			EditorGUILayout.Toggle("Build Blocked", buildBlocked);
			
			var playModeBlockedInfo = type.GetField("playModeBlocked", BindingFlags.NonPublic | BindingFlags.Static);
			bool playModeBlocked = (bool)playModeBlockedInfo.GetValue(null);
			EditorGUILayout.Toggle("Play Mode Blocked", playModeBlocked);
			
			var hasLockedUnityCompilationInfo = type.GetField("hasLockedUnityCompilation", BindingFlags.NonPublic | BindingFlags.Static);
			bool hasLockedUnityCompilation = (bool)hasLockedUnityCompilationInfo.GetValue(null);
			EditorGUILayout.Toggle("Has Locked Compilation", hasLockedUnityCompilation);
			
			EditorGUILayout.BeginHorizontal();
			var disallowedAutoRefreshInfo = type.GetProperty("disallowedAutoRefresh", BindingFlags.NonPublic | BindingFlags.Static);
			bool disallowedAutoRefresh = (bool)disallowedAutoRefreshInfo.GetValue(null);
			EditorGUILayout.Toggle("DisallowedAutoRefresh", disallowedAutoRefresh);
			#if UNITY_2019_4_OR_NEWER
			if(GUILayout.Button("AllowAutoRefresh")) {
				AssetDatabase.AllowAutoRefresh();
			}
			EditorGUILayout.EndHorizontal();
			#endif
			
			
			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}