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
		private System.Exception exception;

		public override bool IsValid(string assetPath) {
			if(Path.GetExtension(assetPath) == ".ink") {
				return true;
			}
			return false;
		}

		public override void OnEnable () {
			InkCompiler.OnCompileInk += OnCompileInk;
			InkLibrary.Refresh();
			string assetPath = AssetDatabase.GetAssetPath(target);
			inkFile = InkLibrary.GetInkFileWithPath(assetPath);
			InkFile masterInkFile = inkFile;
			if(inkFile.master == null) {
				if(inkFile.includes != null) {
					CreateIncludeList();
				}
			} else {
				masterInkFile = inkFile.master;
			}
//			CreateTODOList();

			if (inkFile.jsonAsset != null) {
				// This can be slow. Disable if you find viewing an ink file in the inspector takes too long.
				InkEditorUtils.CheckStoryIsValid (masterInkFile.jsonAsset.text, out exception);
			}
		}

		void OnCompileInk (string inkAbsoluteFilePath, TextAsset compiledJSONTextAsset) {
			InkCompiler.OnCompileInk -= OnCompileInk;
			OnEnable();
		}

		void CreateIncludeList () {
			List<Object> includeTextAssets = new List<Object>();
			foreach(var include in inkFile.includes) {
				includeTextAssets.Add(include.inkFile);
			}
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
			GUI.enabled = true;

			InkFile masterInkFile = inkFile;
			if(inkFile.master == null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("JSON Asset", inkFile.jsonAsset, typeof(TextAsset), false);
				EditorGUI.EndDisabledGroup();

				if(GUILayout.Button("Play")) {
					InkPlayerWindow.LoadAndPlay(inkFile.jsonAsset);
				}
				if(includesFileList != null) {
					includesFileList.DoLayoutList();
				}
			} else {
				masterInkFile = inkFile.master;
				EditorGUILayout.HelpBox("This file is included by a master file.", MessageType.Info);
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Master Ink File", masterInkFile.inkFile, typeof(Object), false);
				EditorGUI.EndDisabledGroup();
			}

			DateTime lastEditDate = File.GetLastWriteTime(inkFile.absoluteFilePath);
			EditorGUILayout.LabelField("Last edit date "+lastEditDate.ToString());

			if(masterInkFile.jsonAsset == null) {
				EditorGUILayout.HelpBox("Ink file has not been compiled", MessageType.Info);
				if(GUILayout.Button("Compile")) {
					InkCompiler.CompileInk(masterInkFile);
				}
			} else {
				DateTime lastCompileDate = File.GetLastWriteTime(Path.Combine(Application.dataPath, AssetDatabase.GetAssetPath(masterInkFile.jsonAsset).Substring(7)));
				EditorGUILayout.LabelField("Last compile date "+lastCompileDate.ToString());

				if(lastEditDate > lastCompileDate && GUILayout.Button("Recompile")) {
					InkCompiler.CompileInk(masterInkFile);
				}

				if(exception != null) {
					EditorGUILayout.HelpBox("Story is invalid\n"+exception.ToString(), MessageType.Error);
				}
			}

			if(todosList != null) {
				todosList.DoLayoutList();
			}

			DrawFileContents ();

			serializedObject.ApplyModifiedProperties();
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