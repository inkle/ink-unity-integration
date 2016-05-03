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
		private ReorderableList circularIncludeReferencesList;

		public override bool IsValid(string assetPath) {
			if(Path.GetExtension(assetPath) == InkEditorUtils.inkFileExtension) {
				return true;
			}
			return false;
		}

		public override void OnEnable () {
			string assetPath = AssetDatabase.GetAssetPath(target);
			inkFile = InkLibrary.GetInkFileWithPath(assetPath);
			if(inkFile == null) 
				return;

			if(inkFile.includes.Count > 0) {
				CreateIncludeList();
			}
			CreateCircularIncludeReferencesList();
			CreateErrorList();
			CreateWarningList();
			CreateTodoList();

			InkCompiler.OnCompileInk += OnCompileInk;
		}

		public override void OnDisable () {
			InkCompiler.OnCompileInk -= OnCompileInk;
		}

		void OnCompileInk (InkFile inkFile) {
			InkCompiler.OnCompileInk -= OnCompileInk;
			OnEnable();
		}

		void CreateIncludeList () {
			List<DefaultAsset> includeTextAssets = inkFile.includes;
			includesFileList = new ReorderableList(includeTextAssets, typeof(DefaultAsset), false, false, false, false);
			includesFileList.elementHeight = 16;
			includesFileList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Included Files");
			};
			includesFileList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				DefaultAsset childAssetFile = ((List<DefaultAsset>)includesFileList.list)[index];
				InkFile childInkFile = InkLibrary.GetInkFileWithFile(childAssetFile);
				Rect iconRect = new Rect(rect.x, rect.y, 0, rect.height);
				if(childInkFile.hasErrors || childInkFile.hasWarnings) {
					iconRect.width = 20;
				}
				Rect objectFieldRect = new Rect(iconRect.xMax, rect.y, rect.width - iconRect.width - 80, rect.height);
				Rect selectRect = new Rect(objectFieldRect.xMax, rect.y, 80, rect.height);
				if(childInkFile.hasErrors) {
					EditorGUI.LabelField(iconRect, new GUIContent(InkBrowserIcons.errorIcon));
				} else if(childInkFile.hasWarnings) {
					EditorGUI.LabelField(iconRect, new GUIContent(InkBrowserIcons.warningIcon));
				}
				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.ObjectField(objectFieldRect, childAssetFile, typeof(Object), false);
				EditorGUI.EndDisabledGroup();
				if(GUI.Button(selectRect, "Select")) {
					Selection.activeObject = childAssetFile;
				}
			};
		}

		void CreateCircularIncludeReferencesList () {
			circularIncludeReferencesList = new ReorderableList(inkFile.circularIncludeReferences, typeof(DefaultAsset), false, false, false, false);
			circularIncludeReferencesList.elementHeight = 16;
			circularIncludeReferencesList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.errorIcon), new GUIContent("Circular include references"));
			};
			circularIncludeReferencesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				DefaultAsset includedInkFile = ((List<DefaultAsset>)circularIncludeReferencesList.list)[index];
				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.ObjectField(rect, includedInkFile, typeof(DefaultAsset), false);
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
				string label = log.content;
				GUI.Label(labelRect, label);
				string openLabel = "Open"+ (log.lineNumber == -1 ? "" : " ("+log.lineNumber+")");
				if(GUI.Button(buttonRect, openLabel)) {
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(inkFile.filePath, log.lineNumber);
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
				string label = log.content;
				GUI.Label(labelRect, label);
				string openLabel = "Open"+ (log.lineNumber == -1 ? "" : " ("+log.lineNumber+")");
				if(GUI.Button(buttonRect, openLabel)) {
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(inkFile.filePath, log.lineNumber);
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
				string label = log.content;
				GUI.Label(labelRect, label);
				string openLabel = "Open"+ (log.lineNumber == -1 ? "" : " ("+log.lineNumber+")");
				if(GUI.Button(buttonRect, openLabel)) {
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(inkFile.filePath, log.lineNumber);
//					AssetDatabase.OpenAsset(masterInkFile.inkFile, lineNumber);
				}
			};
		}

		public override void OnInspectorGUI () {
			editor.Repaint();
			serializedObject.Update();
			if(inkFile == null) 
				return;

			if(InkLibrary.GetCompilationStackItem(inkFile) != null) {
				EditorGUILayout.HelpBox("File is compiling...", MessageType.Info);
				return;
			}
			InkFile masterInkFile = inkFile;
			if(inkFile.isMaster) {
				DrawMasterFileHeader();
			} else {
				masterInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)inkFile.master);
				DrawSubFileHeader(masterInkFile);
			}

			DrawEditAndCompileDates(masterInkFile);
			if(inkFile.isMaster && !editedAfterLastCompile)
				DrawCompileButton(masterInkFile);
			DrawIncludedFiles();

			DrawCircularIncludeReferences();
			DrawErrors();
			DrawWarnings();
			DrawTODOList();
			DrawFileContents ();

			serializedObject.ApplyModifiedProperties();
		}

		void DrawMasterFileHeader () {
			EditorGUILayout.LabelField("Master File", EditorStyles.boldLabel);
			if(!InkLibrary.Instance.compileAutomatically)
				inkFile.compileAutomatically = EditorGUILayout.Toggle("Compile Automatially", inkFile.compileAutomatically);
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
			if(masterInkFile.hasErrors) {
				GUILayout.Label(new GUIContent(InkBrowserIcons.errorIcon), GUILayout.Width(20));
			} else if(masterInkFile.hasWarnings) {
				GUILayout.Label(new GUIContent(InkBrowserIcons.warningIcon), GUILayout.Width(20));
			}
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Master Ink File", masterInkFile.inkAsset, typeof(Object), false);
			EditorGUI.EndDisabledGroup();
			if(GUILayout.Button("Select", GUILayout.Width(80))) {
				Selection.activeObject = masterInkFile.inkAsset;
			}
			EditorGUILayout.EndHorizontal();
		}


		bool editedAfterLastCompile = false;
		void DrawEditAndCompileDates (InkFile masterInkFile) {
			editedAfterLastCompile = false;
			string editAndCompileDateString = "";
			DateTime lastEditDate = File.GetLastWriteTime(inkFile.absoluteFilePath);
			editAndCompileDateString += "Last edit date "+lastEditDate.ToString();
			if(inkFile.isMaster && inkFile.jsonAsset != null) {
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
			if(masterInkFile.hasErrors) {
				EditorGUILayout.HelpBox("Last compiled failed", MessageType.Error);
				drawButton = true;
			} else if(masterInkFile.hasWarnings) {
				EditorGUILayout.HelpBox("Last compile had errors", MessageType.Warning);
				drawButton = true;
			} else if(masterInkFile.jsonAsset == null) {
				EditorGUILayout.HelpBox("Ink file has not been compiled", MessageType.Warning);
				drawButton = true;
			}
			if(drawButton && GUILayout.Button("Compile")) {
				InkCompiler.CompileInk(masterInkFile);
			}
		}


		void DrawCircularIncludeReferences () {
			if(circularIncludeReferencesList != null && circularIncludeReferencesList.count > 0) {
//				EditorGUILayout.HelpBox("Files contain circular INCLUDE references. This must be fixed before files can be compiled.", MessageType.Error);
				circularIncludeReferencesList.DoLayoutList();
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