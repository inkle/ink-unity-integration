using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ink.UnityIntegration {

	[CustomEditor(typeof(InkLibrary))]
	public class InkLibraryEditor : Editor {

		#pragma warning disable
		protected InkLibrary data;
		public Vector2 scrollPosition;
		public void OnEnable() {
			data = (InkLibrary) target;
		}
		
		protected override void OnHeaderGUI () {
			GUILayout.BeginHorizontal();
			GUILayout.Space(38f);
			GUILayout.BeginVertical();
			GUILayout.Space(19f);
			GUILayout.BeginHorizontal();

			GUILayoutUtility.GetRect(10f, 10f, 16f, 16f, EditorStyles.layerMaskField);
			GUILayout.FlexibleSpace();


            EditorGUI.BeginDisabledGroup(InkCompiler.executingCompilationStack);
			if (GUILayout.Button(new GUIContent("Rebuild Library", "Rebuilds the ink library. Do this if you're getting unusual errors"), EditorStyles.miniButton)) {
				InkLibrary.Rebuild();
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

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
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			EditorGUI.BeginDisabledGroup(InkCompiler.executingCompilationStack);
			if (GUILayout.Button(new GUIContent("Rebuild Library", "Rebuilds the ink library. Do this if you're getting unusual errors"), EditorStyles.miniButton)) {
				InkLibrary.Rebuild();
			}
			EditorGUI.EndDisabledGroup();

			if(InkCompiler.executingCompilationStack) {
				Rect r = EditorGUILayout.BeginVertical();
				EditorGUI.ProgressBar(r, InkCompiler.GetEstimatedCompilationProgress(), "Compiling...");
				GUILayout.Space(EditorGUIUtility.singleLineHeight);
				EditorGUILayout.EndVertical();
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
			} else {
				var filesRequiringRecompile = InkLibrary.GetFilesRequiringRecompile();
				if(filesRequiringRecompile.Any()) {
					var files = string.Join("\n", filesRequiringRecompile.Select(x => x.filePath).ToArray());
					if(EditorApplication.isPlaying && InkSettings.instance.delayInPlayMode) {
                        EditorGUILayout.HelpBox("Some Ink files marked to compile on exiting play mode.\n"+files, MessageType.Info);
                    } else {
                        EditorGUILayout.HelpBox("Some Ink files marked to compile automatically are not compiled! Check they don't have compile errors, or else try compiling now.\n"+files, MessageType.Warning);
                    }
				}  else {
					EditorGUILayout.HelpBox("All Ink files marked to compile automatically are compiled", MessageType.Info);
				}
			}
			EditorGUI.BeginDisabledGroup(InkCompiler.executingCompilationStack);
			if (GUILayout.Button(new GUIContent("Recompile All", "Recompiles all files marked to compile automatically."))) {
				InkEditorUtils.RecompileAll();
			}

            if(EditorApplication.isPlaying && InkSettings.instance.delayInPlayMode) {
                var filesRequiringRecompile = InkLibrary.GetFilesRequiringRecompile();
                if(filesRequiringRecompile.Any()) {
                    var files = string.Join("\n", filesRequiringRecompile.Select(x => x.filePath).ToArray());
                    if (GUILayout.Button(new GUIContent("Recompile Pending", "Recompiles all files marked to compile on exiting play mode."))) {
                        InkEditorUtils.RecompileAll();
                    }
                }
            }

			DrawSerializedProperty(serializedObject.FindProperty("inkLibrary"));
			
			void DrawSerializedProperty (SerializedProperty _serializedProperty) {
				var serializedProperty = _serializedProperty.Copy();
				int startingDepth = serializedProperty.depth;
				EditorGUI.indentLevel = startingDepth;
				DrawPropertyField(serializedProperty);
				while (serializedProperty.NextVisible(serializedProperty.isExpanded/* && !EditorGUIX.PropertyTypeHasDefaultCustomDrawer(serializedProperty.propertyType) */) && serializedProperty.depth > startingDepth) {
					EditorGUI.indentLevel = serializedProperty.depth;
					DrawPropertyField(serializedProperty);
				}
				EditorGUI.indentLevel = startingDepth;
			}

			void DrawPropertyField (SerializedProperty serializedProperty) {
				if(serializedProperty.propertyType == SerializedPropertyType.Generic) {
					if(serializedProperty.isArray) {
						serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, serializedProperty.displayName, true);
					} else {
						if(serializedProperty.type == "InkFile") {
							var inkFile = serializedProperty.FindPropertyRelative("inkAsset");
							if(inkFile.objectReferenceValue != null) serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, inkFile.objectReferenceValue.name, true);
							else {
								EditorGUILayout.TextArea("MISSING!");
								Debug.LogError("Ink file missing!");
							}
						} else {
							serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, serializedProperty.displayName, true);
						}
					}
				} else {
					EditorGUILayout.PropertyField(serializedProperty);
				}
			}

			EditorGUILayout.EndScrollView();

			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}