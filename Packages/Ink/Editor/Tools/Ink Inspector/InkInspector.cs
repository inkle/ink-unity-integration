using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace Ink.UnityIntegration {
    [CustomEditor(typeof(InkFile), true)]
	public class InkInspector : Editor {
		private InkFile inkFile;
		private ReorderableList errorList;
		private ReorderableList warningList;
		private ReorderableList todosList;
		private string cachedTrimmedFileContents;
		private const int maxCharacters = 16000;

		public bool IsValid(string assetPath) {
			return Path.GetExtension(assetPath) == InkEditorUtils.inkFileExtension;
		}

		protected override void OnHeaderGUI () {
			GUILayout.BeginHorizontal();
			GUILayout.Space(38f);
			GUILayout.BeginVertical();
			GUILayout.Space(19f);
			GUILayout.BeginHorizontal();

			GUILayoutUtility.GetRect(10f, 10f, 16f, 35f, EditorStyles.layerMaskField);
			GUILayout.FlexibleSpace();

			// EditorGUI.BeginDisabledGroup(inkFile == null);
			// if (GUILayout.Button("Open", EditorStyles.miniButton)) {
			// 	AssetDatabase.OpenAsset(inkFile, 3);
			// 	GUIUtility.ExitGUI();
			// }
			// EditorGUI.EndDisabledGroup();

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			Rect lastRect = GUILayoutUtility.GetLastRect();
			Rect rect = new Rect(lastRect.x, lastRect.y, lastRect.width, lastRect.height);
			Rect iconRect = new Rect(rect.x + 6f, rect.y + 6f, 32f, 32f);
			GUI.DrawTexture(iconRect, InkBrowserIcons.inkFileIconLarge);
			Rect childIconRect = new Rect(iconRect.x, iconRect.y, 16f, 16f);
			if(inkFile == null) {
				GUI.DrawTexture(childIconRect, InkBrowserIcons.unknownFileIcon, ScaleMode.ScaleToFit);
			}

			Rect titleRect = new Rect(rect.x + 44f, rect.y + 6f, rect.width - 44f - 38f - 4f, 16f);
			titleRect.yMin -= 2f;
			titleRect.yMax += 2f;
			GUI.Label(titleRect, target.name, EditorStyles.largeLabel);
		}

		public void OnEnable () {
			Rebuild();
		}

		void Rebuild () {
			cachedTrimmedFileContents = "";
			string assetPath = AssetDatabase.GetAssetPath(target);
			inkFile = AssetDatabase.LoadAssetAtPath<InkFile>(assetPath);
			if(inkFile == null) 
				return;

			errorList = CreateErrorList();
			warningList = CreateWarningList();
			todosList = CreateTodoList();
			
            var absoluteFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(assetPath);
			cachedTrimmedFileContents = File.ReadAllText(absoluteFilePath);
			cachedTrimmedFileContents = cachedTrimmedFileContents.Substring(0, Mathf.Min(cachedTrimmedFileContents.Length, maxCharacters));
			if(cachedTrimmedFileContents.Length >= maxCharacters)
				cachedTrimmedFileContents += "...\n\n<...etc...>";
		}

		ReorderableList CreateErrorList () {
			var reorderableList = new ReorderableList(inkFile.errors, typeof(string), false, true, false, false);
			reorderableList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.errorIcon), new GUIContent("Errors"));
			};
			reorderableList.elementHeight = 26;
			reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawLogItem(rect, index, isActive, isFocused, (List<InkCompilerLog>)reorderableList.list);
			return reorderableList;
		}

		ReorderableList CreateWarningList () {
			var reorderableList = new ReorderableList(inkFile.warnings, typeof(string), false, true, false, false);
			reorderableList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.warningIcon), new GUIContent("Warnings"));
			};
			reorderableList.elementHeight = 26;
			reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawLogItem(rect, index, isActive, isFocused, (List<InkCompilerLog>)reorderableList.list);
			return reorderableList;
		}

		ReorderableList CreateTodoList () {
			var reorderableList = new ReorderableList(inkFile.todos, typeof(string), false, true, false, false);
			reorderableList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "To do");
			};
			reorderableList.elementHeight = 26;
			reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawLogItem(rect, index, isActive, isFocused, (List<InkCompilerLog>)reorderableList.list);
			return reorderableList;
		}
		
		void DrawLogItem (Rect rect, int index, bool isActive, bool isFocused, List<InkCompilerLog> logsList) {
			Rect logRect = new Rect(rect.x, rect.y, rect.width - 80, 16);
			Rect locationRect = new Rect(rect.x, rect.y+16, rect.width - 80, 10);
			Rect buttonRect = new Rect(logRect.xMax, rect.y, 80, rect.height-2);
			InkCompilerLog log = logsList[index];
			GUI.Label(logRect, log.content);
			GUI.Label(locationRect, "("+log.relativeFilePath+":"+log.lineNumber+")", filePathAndLineNumberStyle);
			string openLabel = "Open";
			if(GUI.Button(buttonRect, openLabel)) {
				InkEditorUtils.OpenInEditor(inkFile, log);
			}
		}
		static GUIStyle _filePathAndLineNumberStyle;
		static GUIStyle filePathAndLineNumberStyle {
			get {
				if(_filePathAndLineNumberStyle == null) {
					_filePathAndLineNumberStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
					_filePathAndLineNumberStyle.alignment = TextAnchor.MiddleLeft;
				}
				return _filePathAndLineNumberStyle;
			}
		}

		public static void DrawLayoutInkLine (InkFile inkFile, int lineNumber, string label) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(label);
			string openLabel = "Open"+ (lineNumber == -1 ? "" : " ("+lineNumber+")");
			if(GUILayout.Button(openLabel, GUILayout.Width(80))) {
				InkEditorUtils.OpenInEditor(AssetDatabase.GetAssetPath(inkFile), null, lineNumber);
			}
			GUILayout.EndHorizontal();
		}

		public override void OnInspectorGUI () {
			Repaint();
			serializedObject.Update();
			
            GUI.enabled = true;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawMasterFileHeader();
            
            EditorGUILayout.Space();
            
            // There's no point letting users compile when recursive INCLUDE files exist, so hide anything else while they exist!
            if(inkFile.hasRecursiveIncludeErrorPaths) {
                EditorGUILayout.HelpBox("A recursive INCLUDE connection exists in this ink file's INCLUDE hierarchy.\nThe offending INCLUDE(s) can be found at the following file(s):\n"+string.Join("\n", inkFile.recursiveIncludeErrorPaths.Select(x => "• "+x)), MessageType.Error);
            } else {
                if(inkFile.hasUnhandledCompileErrors) {
                    EditorGUILayout.HelpBox("Last compiled failed", MessageType.Error);
                } if(inkFile.hasErrors) {
                    EditorGUILayout.HelpBox("Last compiled had errors", MessageType.Error);
                } else if(inkFile.hasWarnings) {
                    EditorGUILayout.HelpBox("Last compile had warnings", MessageType.Warning);
                }
                
                DrawCompileErrors();
                DrawErrors();
                DrawWarnings();
                DrawTODOList();
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
			
			DrawFileContents ();
			

			serializedObject.ApplyModifiedProperties();
		}

		void DrawMasterFileHeader () {
			EditorGUILayout.LabelField(new GUIContent("Master File", "This file is a master file and can be compiled"), EditorStyles.boldLabel);
			
			if(inkFile.errors.Count == 0 && GUILayout.Button("Play")) {
				InkPlayerWindow.LoadAndPlay(inkFile.storyJson);
			}
			
			EditorGUILayout.Space();
			
			DrawEditAndCompileDates(inkFile);
		}

		void DrawEditAndCompileDates (InkFile masterInkFile) {
			string editAndCompileDateString = "";
			DateTime lastEditDate = inkFile.lastEditDate;
			editAndCompileDateString += "Last edit date "+lastEditDate.ToString();
            EditorGUILayout.HelpBox(editAndCompileDateString, MessageType.None);
		}

		void DrawCompileErrors () {
			if(inkFile.unhandledCompileErrors.Count == 0) 
				return;
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.HelpBox("Compiler bug prevented compilation of JSON file. Please help us fix it by reporting this as a bug.", MessageType.Error);
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Report via Github")) {
				Application.OpenURL("https://github.com/inkle/ink-unity-integration/issues/new");
			}
			if(GUILayout.Button("Report via Email")) {
				Application.OpenURL("mailto:info@inklestudios.com");
			}
			EditorGUILayout.EndHorizontal();
			foreach(string compileError in inkFile.unhandledCompileErrors) {
				GUILayout.TextArea(compileError);
			}
			EditorGUILayout.EndVertical();
		}

		void DrawErrors () {
			if(errorList != null && errorList.count > 0) {
				errorList.DoLayoutList();
			}
		}

		void DrawWarnings () {
			if(warningList != null && warningList.count > 0) {
				warningList.DoLayoutList();
			}
		}

		void DrawTODOList () {
			if(todosList != null && todosList.count > 0) {
				todosList.DoLayoutList();
			}
		}

		void DrawFileContents () {
			float width = EditorGUIUtility.currentViewWidth-50;
			float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(cachedTrimmedFileContents), width);
			EditorGUILayout.BeginVertical(EditorStyles.textArea);
			EditorGUILayout.SelectableLabel(cachedTrimmedFileContents, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.Width(width), GUILayout.Height(height));
			EditorGUILayout.EndVertical();
		}
	}
}