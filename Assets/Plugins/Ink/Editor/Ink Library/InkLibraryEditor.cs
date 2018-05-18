using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ink.UnityIntegration {

	[CustomEditor(typeof(InkLibrary))]
	public class InkLibraryEditor : Editor {

		#pragma warning disable
		protected InkLibrary data;
		
		public void OnEnable() {
			data = (InkLibrary) target;
		}
		
		protected override void OnHeaderGUI () {
			GUILayout.BeginVertical();
			GUILayout.Space(49f);
			GUILayout.EndVertical();

			Rect lastRect = GUILayoutUtility.GetLastRect();
			Rect rect = new Rect(lastRect.x, lastRect.y, lastRect.width, lastRect.height);
			Rect iconRect = new Rect(rect.x + 6f, rect.y + 6f, 32f, 32f);
			GUI.DrawTexture(iconRect, InkBrowserIcons.inkFileIconLarge);

			Rect titleRect = new Rect(rect.x + 44f, rect.y + 6f, rect.width - 44f - 38f - 4f, 16f);
			titleRect.yMin -= 2f;
			titleRect.yMax += 2f;
			GUI.Label(titleRect, "Ink Library", EditorStyles.largeLabel);
			
			Rect infoRect = titleRect;
			infoRect.y += titleRect.height;
			GUI.Label(infoRect, "Caches information about ink files in your project", EditorStyles.miniLabel);
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			if(InkCompiler.compiling) {
				Rect r = EditorGUILayout.BeginVertical();
				EditorGUI.ProgressBar(r, InkCompiler.GetEstimatedCompilationProgress(), "Compiling...");
				GUILayout.Space(EditorGUIUtility.singleLineHeight);
				EditorGUILayout.EndVertical();
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
			} else {
				var filesRequiringRecompile = InkLibrary.GetFilesRequiringRecompile();
				if(filesRequiringRecompile.Any()) {
					var files = string.Join("\n", filesRequiringRecompile.Select(x => x.filePath).ToArray());
					EditorGUILayout.HelpBox("Some Ink files marked to compile automatically are not compiled! Check they don't have compile errors, or else try compiling now.\n"+files, MessageType.Warning);
				}  else {
					EditorGUILayout.HelpBox("All Ink files marked to compile automatically are compiled", MessageType.Info);
				}
			}
			EditorGUI.BeginDisabledGroup(InkCompiler.compiling);
			if (GUILayout.Button(new GUIContent("Rebuild Library", "Rebuilds the ink library. Do this if you're getting unusual errors"))) {
				InkLibrary.Rebuild();
			}
			if (GUILayout.Button(new GUIContent("Recompile All", "Recompiles all files marked to compile automatically."))) {
				InkEditorUtils.RecompileAll();
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("inkLibrary"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("pendingCompilationStack"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("compilationStack"), true);
			EditorGUI.EndDisabledGroup();
			
			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}