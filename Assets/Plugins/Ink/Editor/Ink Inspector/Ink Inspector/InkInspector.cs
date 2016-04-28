using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEditorInternal;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

namespace Ink.UnityIntegration {
	public class InkInspector : ObjectInspector {

		private InkFile inkFile;
		private ReorderableList includesFileList;
		private ReorderableList errorList;
		private ReorderableList warningList;
		private ReorderableList todosList;

		public override bool IsValid(string assetPath) {
			if(Path.GetExtension(assetPath) == InkEditorUtils.inkFileExtension) {
				return true;
			}
			return false;
		}

		public override void OnEnable () {
//			InkLibrary.Refresh();
			string assetPath = AssetDatabase.GetAssetPath(target);
			inkFile = InkLibrary.GetInkFileWithPath(assetPath);
			if(inkFile == null) 
				return;

			if(inkFile.includes != null) {
				CreateIncludeList();
			}
			CreateErrorList();
			CreateWarningList();
			CreateTodoList();

			InkCompiler.OnCompileInk += OnCompileInk;
		}

		public override void OnDisable () {
			InkCompiler.OnCompileInk -= OnCompileInk;
		}

		void OnCompileInk (string inkAbsoluteFilePath, TextAsset compiledJSONTextAsset) {
			InkCompiler.OnCompileInk -= OnCompileInk;
			OnEnable();
		}

		void CreateIncludeList () {
			List<Object> includeTextAssets = inkFile.includes;
			includesFileList = new ReorderableList(includeTextAssets, typeof(Object), false, false, false, false);
			includesFileList.elementHeight = 16;
			includesFileList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Included Files");
			};
			includesFileList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				EditorGUI.BeginDisabledGroup(true);
//				new GUIContent(InkBrowserIcons.inkFileIcon), 
				EditorGUI.ObjectField(rect, ((List<Object>)includesFileList.list)[index], typeof(Object), false);
				EditorGUI.EndDisabledGroup();
			};
		}

		void CreateErrorList () {
			errorList = new ReorderableList(inkFile.errors, typeof(string), false, false, false, false);
			errorList.elementHeight = 18;
			errorList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.errorIcon), new GUIContent("Errors"));
			};
			errorList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				InkFile.InkFileLog log = ((List<InkFile.InkFileLog>)errorList.list)[index];
				InkFile logInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)log.file);
				string label = log.content;
				GUI.Label(labelRect, label);
				string openLabel = "Open"+ (log.lineNumber == -1 ? "" : " ("+log.lineNumber+")");
				if(GUI.Button(buttonRect, openLabel)) {
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(logInkFile.filePath, log.lineNumber);
//					AssetDatabase.OpenAsset(masterInkFile.inkFile, lineNumber);
				}
			};
		}

		void CreateWarningList () {
			warningList = new ReorderableList(inkFile.warnings, typeof(string), false, false, false, false);
			warningList.elementHeight = 18;
			warningList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.warningIcon), new GUIContent("Warnings"));
			};
			warningList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				InkFile.InkFileLog log = ((List<InkFile.InkFileLog>)warningList.list)[index];
				InkFile logInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)log.file);
				string label = log.content;
				GUI.Label(labelRect, label);
				string openLabel = "Open"+ (log.lineNumber == -1 ? "" : " ("+log.lineNumber+")");
				if(GUI.Button(buttonRect, openLabel)) {
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(logInkFile.filePath, log.lineNumber);
//					AssetDatabase.OpenAsset(masterInkFile.inkFile, lineNumber);
				}
			};
		}

		void CreateTodoList () {
			todosList = new ReorderableList(inkFile.todos, typeof(string), false, false, false, false);
			todosList.elementHeight = 18;
			todosList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "To do");
			};
			todosList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				InkFile.InkFileLog log = ((List<InkFile.InkFileLog>)todosList.list)[index];
				InkFile logInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)log.file);
				string label = log.content;
				GUI.Label(labelRect, label);
				string openLabel = "Open"+ (log.lineNumber == -1 ? "" : " ("+log.lineNumber+")");
				if(GUI.Button(buttonRect, openLabel)) {
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(logInkFile.filePath, log.lineNumber);
//					AssetDatabase.OpenAsset(masterInkFile.inkFile, lineNumber);
				}
			};
		}

		public override void OnInspectorGUI () {
			serializedObject.Update();
			if(inkFile == null) 
				return;

			InkFile masterInkFile = inkFile;
			if(inkFile.master == null) {
				DrawMasterFileHeader();
			} else {
				masterInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)inkFile.master);
				DrawSubFileHeader(masterInkFile);
			}

			DrawEditAndCompileDates(masterInkFile);
			if(inkFile.master == null && !editedAfterLastCompile)
				DrawCompileButton(masterInkFile);
			DrawIncludedFiles();
			DrawErrors();
			DrawWarnings();
			DrawTODOList();
			DrawFileContents ();

			serializedObject.ApplyModifiedProperties();
		}

		void DrawMasterFileHeader () {
			EditorGUILayout.LabelField("Master File", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("JSON Asset", inkFile.jsonAsset, typeof(TextAsset), false);
			EditorGUI.EndDisabledGroup();

			if(inkFile.jsonAsset != null && inkFile.errors.Count == 0 && GUILayout.Button("Play")) {
				InkPlayerWindow.LoadAndPlay(inkFile.jsonAsset);
			}

//				if(!checkedStoryForErrors) {
//					if(GUILayout.Button("Check for errors")) {
//						GetStoryErrors();
//					}
//				} else {
//					if(exception != null) {
//						EditorGUILayout.HelpBox("Story is invalid\n"+exception.ToString(), MessageType.Error);
//					} else {
//						EditorGUILayout.HelpBox("Story is valid", MessageType.Info);
//					}
//				}
		}

		void DrawSubFileHeader(InkFile masterInkFile) {
			EditorGUILayout.LabelField("Sub File", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(true);
			if(masterInkFile.hasErrors) {
				GUILayout.Label(new GUIContent(InkBrowserIcons.errorIcon), GUILayout.Width(20));
			} else if(masterInkFile.hasWarnings) {
				GUILayout.Label(new GUIContent(InkBrowserIcons.warningIcon), GUILayout.Width(20));
			}
			EditorGUILayout.ObjectField("Master Ink File", masterInkFile.inkFile, typeof(Object), false);
			EditorGUI.EndDisabledGroup();
			if(GUILayout.Button("Select", GUILayout.Width(60))) {
				Selection.activeObject = masterInkFile.inkFile;
			}
			EditorGUILayout.EndHorizontal();
		}


		bool editedAfterLastCompile = false;
		void DrawEditAndCompileDates (InkFile masterInkFile) {
			editedAfterLastCompile = false;
			string editAndCompileDateString = "";
			DateTime lastEditDate = File.GetLastWriteTime(inkFile.absoluteFilePath);
			editAndCompileDateString += "Last edit date "+lastEditDate.ToString();
			if(inkFile.master == null && inkFile.jsonAsset != null) {
				DateTime lastCompileDate = File.GetLastWriteTime(Path.Combine(Application.dataPath, AssetDatabase.GetAssetPath(masterInkFile.jsonAsset).Substring(7)));
				editAndCompileDateString += "\nLast compile date "+lastCompileDate.ToString();
				if(lastEditDate > lastCompileDate) {
					editedAfterLastCompile = true;
					EditorGUILayout.HelpBox(editAndCompileDateString, MessageType.Warning);
					if(GUILayout.Button("Recompile")) {
						InkCompiler.CompileInk(masterInkFile);
					}
				} else {
					EditorGUILayout.HelpBox(editAndCompileDateString, MessageType.None);
				}
			} else {
				EditorGUILayout.HelpBox(editAndCompileDateString, MessageType.None);
			}
		}

		void DrawIncludedFiles () {
			if(includesFileList != null && includesFileList.count > 0) {
				includesFileList.DoLayoutList();
			}
		}

		void DrawCompileButton (InkFile masterInkFile) {
			bool drawButton = false;
			if(masterInkFile.errors.Count > 0) {
				EditorGUILayout.HelpBox("Last compiled failed", MessageType.Error);
				drawButton = true;
			} else if(masterInkFile.jsonAsset == null) {
				EditorGUILayout.HelpBox("Ink file has not been compiled", MessageType.Warning);
				drawButton = true;
			} else if(masterInkFile.jsonAsset == null) {
				EditorGUILayout.HelpBox("Ink file has not been compiled", MessageType.Warning);
				drawButton = true;
			}
			if(drawButton && GUILayout.Button("Compile")) {
				InkCompiler.CompileInk(masterInkFile);
			}
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
			int maxCharacters = 16000;
			string trimmedStory = inkFile.fileContents.Substring(0, Mathf.Min(inkFile.fileContents.Length, maxCharacters));
			if(inkFile.fileContents.Length >= maxCharacters)
				trimmedStory += "...\n\n<...etc...>";
			float width = EditorGUIUtility.currentViewWidth-50;
			float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(trimmedStory), width);
			EditorGUILayout.BeginVertical(EditorStyles.textArea);
			EditorGUILayout.SelectableLabel(trimmedStory, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.Width(width), GUILayout.Height(height));
			EditorGUILayout.EndVertical();
		}
	}
}