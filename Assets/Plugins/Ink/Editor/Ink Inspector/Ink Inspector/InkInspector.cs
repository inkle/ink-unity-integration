using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEditorInternal;

using Object = UnityEngine.Object;

namespace Ink.UnityIntegration {
	public class InkInspector : ObjectInspector {

		private InkFile inkFile;
		private ReorderableList includesFileList;
		private Story story;
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
			} else {
				masterInkFile = inkFile.master;
			}

			// This can be slow. Disable if you find viewing an ink file in the inspector takes too long.
			InkEditorUtils.CheckStoryIsValid(masterInkFile.jsonAsset.text, out exception, out story);
		}

		void OnCompileInk (string inkAbsoluteFilePath, TextAsset compiledJSONTextAsset) {
			InkCompiler.OnCompileInk -= OnCompileInk;
			OnEnable();
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

			serializedObject.ApplyModifiedProperties();

			string trimmedStory = inkFile.fileContents.Substring(0, Mathf.Min(inkFile.fileContents.Length, 5000));
			float width = EditorGUIUtility.currentViewWidth-44;
			float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(trimmedStory), width);
			EditorGUILayout.SelectableLabel(trimmedStory, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.Width(width), GUILayout.Height(height));
		}
	}
}