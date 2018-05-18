using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace Ink.UnityIntegration {
	public class InkInspector : DefaultAssetInspector {

		private InkFile inkFile;
		private ReorderableList includesFileList;
		private ReorderableList errorList;
		private ReorderableList warningList;
		private ReorderableList todosList;
		private string cachedTrimmedFileContents;
		private const int maxCharacters = 16000;

		public override bool IsValid(string assetPath) {
			return Path.GetExtension(assetPath) == InkEditorUtils.inkFileExtension;
		}

		public override void OnHeaderGUI () {
			GUILayout.BeginHorizontal();
			GUILayout.Space(38f);
			GUILayout.BeginVertical();
			GUILayout.Space(19f);
			GUILayout.BeginHorizontal();

			GUILayoutUtility.GetRect(10f, 10f, 16f, 16f, EditorStyles.layerMaskField);
			GUILayout.FlexibleSpace();

			EditorGUI.BeginDisabledGroup(inkFile == null);
			if (GUILayout.Button("Open", EditorStyles.miniButton)) {
				AssetDatabase.OpenAsset(inkFile.inkAsset, 3);
				GUIUtility.ExitGUI();
			}
			EditorGUI.EndDisabledGroup();

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
			} else if(!inkFile.metaInfo.isMaster) {
				GUI.DrawTexture(childIconRect, InkBrowserIcons.childIconLarge, ScaleMode.ScaleToFit);
			}

			Rect titleRect = new Rect(rect.x + 44f, rect.y + 6f, rect.width - 44f - 38f - 4f, 16f);
			titleRect.yMin -= 2f;
			titleRect.yMax += 2f;
			GUI.Label(titleRect, editor.target.name, EditorStyles.largeLabel);
		}

		public override void OnEnable () {
			Rebuild();
			InkCompiler.OnCompileInk += OnCompileInk;
		}

		public override void OnDisable () {
			InkCompiler.OnCompileInk -= OnCompileInk;
		}

		void OnCompileInk (InkFile inkFile) {
			Rebuild();
		}

		void Rebuild () {
			cachedTrimmedFileContents = "";
			string assetPath = AssetDatabase.GetAssetPath(target);
			inkFile = InkLibrary.GetInkFileWithPath(assetPath);
			if(inkFile == null) 
				return;

			if (inkFile.jsonAsset != null && inkFile.metaInfo.includes.Count > 0)
				CreateIncludeList ();
			else
				includesFileList = null;

			CreateErrorList();
			CreateWarningList();
			CreateTodoList();
			cachedTrimmedFileContents = inkFile.metaInfo.GetFileContents();
			cachedTrimmedFileContents = cachedTrimmedFileContents.Substring(0, Mathf.Min(cachedTrimmedFileContents.Length, maxCharacters));
			if(cachedTrimmedFileContents.Length >= maxCharacters)
				cachedTrimmedFileContents += "...\n\n<...etc...>";
		}

		void CreateIncludeList () {
			List<DefaultAsset> includeTextAssets = inkFile.metaInfo.includes;
			includesFileList = new ReorderableList(includeTextAssets, typeof(DefaultAsset), false, false, false, false);
			includesFileList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Included Files");
			};
			includesFileList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				DefaultAsset childAssetFile = ((List<DefaultAsset>)includesFileList.list)[index];
				if(childAssetFile == null) {
					Debug.LogError("Ink file in include list is null. This should never occur. Use Assets > Recompile Ink to fix this issue.");
					EditorGUI.LabelField(rect, new GUIContent("Warning: Ink File in include list is null. Use Assets > Recompile Ink to fix this issue."));
					return;
				}
				InkFile childInkFile = InkLibrary.GetInkFileWithFile(childAssetFile);
				if(childInkFile == null) {
					Debug.LogError("Ink File for included file "+childAssetFile+" not found. This should never occur. Use Assets > Recompile Ink to fix this issue.");
					EditorGUI.LabelField(rect, new GUIContent("Warning: Ink File for included file "+childAssetFile+" not found. Use Assets > Recompile Ink to fix this issue."));
					return;
				}
				Rect iconRect = new Rect(rect.x, rect.y, 0, 16);
				if(childInkFile.metaInfo.hasErrors || childInkFile.metaInfo.hasWarnings) {
					iconRect.width = 20;
				}
				Rect objectFieldRect = new Rect(iconRect.xMax, rect.y, rect.width - iconRect.width - 80, 16);
				Rect selectRect = new Rect(objectFieldRect.xMax, rect.y, 80, 16);
				if(childInkFile.metaInfo.hasErrors) {
					EditorGUI.LabelField(iconRect, new GUIContent(InkBrowserIcons.errorIcon));
				} else if(childInkFile.metaInfo.hasWarnings) {
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

		void CreateErrorList () {
			errorList = new ReorderableList(inkFile.metaInfo.errors, typeof(string), false, false, false, false);
			errorList.elementHeight = 18;
			errorList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.errorIcon), new GUIContent("Errors"));
			};
			errorList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				InkMetaFile.InkFileLog log = ((List<InkMetaFile.InkFileLog>)errorList.list)[index];
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
			warningList = new ReorderableList(inkFile.metaInfo.warnings, typeof(string), false, false, false, false);
			warningList.elementHeight = 18;
			warningList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, new GUIContent(InkBrowserIcons.warningIcon), new GUIContent("Warnings"));
			};
			warningList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				InkMetaFile.InkFileLog log = ((List<InkMetaFile.InkFileLog>)warningList.list)[index];
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
			todosList = new ReorderableList(inkFile.metaInfo.todos, typeof(string), false, false, false, false);
			todosList.elementHeight = 18;
			todosList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "To do");
			};
			todosList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				InkMetaFile.InkFileLog log = ((List<InkMetaFile.InkFileLog>)todosList.list)[index];
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
			if(inkFile == null) {
				EditorGUILayout.HelpBox("Ink File is not in library.", MessageType.Warning);
				if(GUILayout.Button("Rebuild Library")) {
					InkLibrary.Rebuild();
					Rebuild();
				}
				return;
			}

			if(InkLibrary.GetCompilationStackItem(inkFile) != null) {
				EditorGUILayout.HelpBox("File is compiling...", MessageType.Info);
				return;
			}
			
			InkFile masterInkFile = inkFile.metaInfo.masterInkFileIncludingSelf;
			if(inkFile.metaInfo.isMaster) {
				DrawMasterFileHeader();
			} else {
				DrawSubFileHeader(masterInkFile);
			}

			DrawEditAndCompileDates(masterInkFile);
			if(masterInkFile.metaInfo.hasCompileErrors) {
				EditorGUILayout.HelpBox("Last compiled failed", MessageType.Error);
			} if(masterInkFile.metaInfo.hasErrors) {
				EditorGUILayout.HelpBox("Last compiled had errors", MessageType.Error);
			} else if(masterInkFile.metaInfo.hasWarnings) {
				EditorGUILayout.HelpBox("Last compile had warnings", MessageType.Warning);
			} else if(masterInkFile.jsonAsset == null) {
				EditorGUILayout.HelpBox("Ink file has not been compiled", MessageType.Warning);
			}
			if(inkFile.metaInfo.requiresCompile && GUILayout.Button("Compile")) {
				InkCompiler.CompileInk(masterInkFile);
			}
			
			DrawIncludedFiles();

			DrawCompileErrors();
			DrawErrors();
			DrawWarnings();
			DrawTODOList();
			DrawFileContents ();

			serializedObject.ApplyModifiedProperties();
		}

		void DrawMasterFileHeader () {
			EditorGUILayout.LabelField("Master File", EditorStyles.boldLabel);
			if(!InkSettings.Instance.compileAutomatically) {
				inkFile.compileAutomatically = EditorGUILayout.Toggle("Compile Automatially", inkFile.compileAutomatically);
				EditorApplication.RepaintProjectWindow();
			}
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("JSON Asset", inkFile.jsonAsset, typeof(TextAsset), false);
			EditorGUI.EndDisabledGroup();

			if(inkFile.jsonAsset != null && inkFile.metaInfo.errors.Count == 0 && GUILayout.Button("Play")) {
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
			if(masterInkFile.metaInfo.hasErrors) {
				GUILayout.Label(new GUIContent(InkBrowserIcons.errorIcon), GUILayout.Width(20));
			} else if(masterInkFile.metaInfo.hasWarnings) {
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

		void DrawEditAndCompileDates (InkFile masterInkFile) {
			string editAndCompileDateString = "";
			DateTime lastEditDate = inkFile.metaInfo.lastEditDate;
			editAndCompileDateString += "Last edit date "+lastEditDate.ToString();
			if(masterInkFile.jsonAsset != null) {
				DateTime lastCompileDate = masterInkFile.metaInfo.lastCompileDate;
				editAndCompileDateString += "\nLast compile date "+lastCompileDate.ToString();
				if(lastEditDate > lastCompileDate) {
					EditorGUILayout.HelpBox(editAndCompileDateString, MessageType.Warning);
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

		void DrawCompileErrors () {
			if(inkFile.metaInfo.compileErrors.Count == 0) 
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
			foreach(string compileError in inkFile.metaInfo.compileErrors) {
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