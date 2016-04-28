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
		private ReorderableList todosList;
//		private System.Exception exception;
//		private bool checkedStoryForErrors;

		public override bool IsValid(string assetPath) {
			if(Path.GetExtension(assetPath) == ".ink") {
				return true;
			}
			return false;
		}

		public override void OnEnable () {
			InkLibrary.Refresh();
			string assetPath = AssetDatabase.GetAssetPath(target);
			inkFile = InkLibrary.GetInkFileWithPath(assetPath);
			if(inkFile == null) 
				return;
//			InkFile masterInkFile = inkFile;
//			if(inkFile.master != null) {
//				masterInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)inkFile.master);
//			}

			if(inkFile.includes != null) {
				CreateIncludeList();
			}
			CreateTODOList();

//			if (masterInkFile.jsonAsset != null) {
//				// This can be slow. Disable if you find viewing an ink file in the inspector takes too long.
//				GetStoryErrors();
//			}
			InkCompiler.OnCompileInk += OnCompileInk;
		}

		public override void OnDisable () {
			InkCompiler.OnCompileInk -= OnCompileInk;
		}
//		void GetStoryErrors () {
//			checkedStoryForErrors = true;
//			InkEditorUtils.CheckStoryIsValid (inkFile.jsonAsset.text, out exception);
//		}

		void OnCompileInk (string inkAbsoluteFilePath, TextAsset compiledJSONTextAsset) {
			InkCompiler.OnCompileInk -= OnCompileInk;
			OnEnable();
		}

		void CreateIncludeList () {
			List<Object> includeTextAssets = inkFile.includes;
//			foreach(Object include in inkFile.includes) {
//				InkFile inkFile = InkLibrary.GetInk();
//				includeTextAssets.Add(inkFile.includes);	
//			}
//			foreach(InkFile include in inkFile.includes) {
//				includeTextAssets.Add(include.inkFile);
//			}
			includesFileList = new ReorderableList(includeTextAssets, typeof(Object), false, false, false, false);
			includesFileList.elementHeight = 16;
			includesFileList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Included Files");
			};
			includesFileList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.ObjectField(rect, ((List<Object>)includesFileList.list)[index], typeof(Object), false);
				EditorGUI.EndDisabledGroup();
			};
		}

		void CreateTODOList () {
			List<string> todos = new List<string>();
			Regex re = new Regex(@"^TODO(.*)",RegexOptions.IgnoreCase | RegexOptions.Multiline);
			foreach (Match m in re.Matches(inkFile.fileContents)) {
				todos.Add(m.Value.Trim());
			}
			todosList = new ReorderableList(todos, typeof(string), false, false, false, false);
			todosList.elementHeight = 18;
			todosList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "To do");
			};
			todosList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
				Rect buttonRect = new Rect(labelRect.xMax, rect.y, 80, rect.height-2);
				string label = ((List<string>)todosList.list)[index];
				GUI.Label(labelRect, label);
				if(GUI.Button(buttonRect, "Open")) {
					var lineNumber = inkFile.fileContents.Take(inkFile.fileContents.IndexOf(label)).Count(c => c == '\n') + 1;
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(inkFile.filePath, lineNumber);
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

			if(inkFile.errors.Count > 0) {
				string errors = string.Join("\n", inkFile.errors.ToArray());
				EditorGUILayout.HelpBox(errors, MessageType.Error);
			} else if(inkFile.warnings.Count > 0) {
				string warnings = string.Join("\n", inkFile.warnings.ToArray());
				EditorGUILayout.HelpBox(warnings, MessageType.Warning);
			} else if(inkFile.todos.Count > 0) {
				string todos = string.Join("\n", inkFile.todos.ToArray());
				EditorGUILayout.HelpBox(todos, MessageType.Info);
			}

			DrawEditAndCompileDates(masterInkFile);
			if(!editedAfterLastCompile)
				DrawCompileButton(masterInkFile);
			DrawIncludedFiles();
			DrawTODOList();
			DrawFileContents ();

			serializedObject.ApplyModifiedProperties();
		}

		void DrawMasterFileHeader () {
			EditorGUILayout.LabelField("Master File", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("JSON Asset", inkFile.jsonAsset, typeof(TextAsset), false);
			EditorGUI.EndDisabledGroup();

			if(inkFile.jsonAsset != null && GUILayout.Button("Play")) {
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
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Master Ink File", masterInkFile.inkFile, typeof(Object), false);
			EditorGUI.EndDisabledGroup();
		}


		bool editedAfterLastCompile = false;
		void DrawEditAndCompileDates (InkFile masterInkFile) {
			editedAfterLastCompile = false;
			string editAndCompileDateString = "";
			DateTime lastEditDate = File.GetLastWriteTime(inkFile.absoluteFilePath);
			editAndCompileDateString += "Last edit date "+lastEditDate.ToString();
			if(masterInkFile.jsonAsset != null) {
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
			if(includesFileList != null) {
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