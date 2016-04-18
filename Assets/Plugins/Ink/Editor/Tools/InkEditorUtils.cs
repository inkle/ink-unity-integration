using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Ink.Runtime;

namespace Ink.UnityIntegration {
	public static class InkEditorUtils {
		public const string inkFileExtension = ".ink";

		[MenuItem("Ink/Create .Ink", false, -2)]
		public static void CreateFile() {
			string fileName = "New Ink File.ink";
			string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GetSelectedPathOrFallback(), fileName));
			System.IO.File.WriteAllText(filePath, "");
			AssetDatabase.ImportAsset(filePath);
			var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(filePath);
			EditorGUIUtility.PingObject(asset);
//			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(asset.GetInstanceID(), null, filePath, null, null);
		}

		private static string GetSelectedPathOrFallback() {
	         string path = "Assets";
	         foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)) {
	             path = AssetDatabase.GetAssetPath(obj);
	             if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
	                 path = Path.GetDirectoryName(path);
	                 break;
	             }
	         }
	         return path;
	     }


		[MenuItem("Ink/Help/About")]
		public static void OpenAbout() {
			Application.OpenURL("https://github.com/inkle/ink#ink");
		}

		[MenuItem("Ink/Help/API Documentation...")]
		public static void OpenWritingDocumentation() {
			Application.OpenURL("https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md");
		}

		public static TextAsset CreateStoryStateTextFile (string jsonStoryState, string defaultPath = "Assets/Ink", string defaultName = "storyState") {
			string name = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(defaultPath, defaultName+".json")).Substring(defaultPath.Length+1);
			string fullPathName = EditorUtility.SaveFilePanel("Save Story State", defaultPath, name, "json");
			if(fullPathName == "") 
				return null;
			using (StreamWriter outfile = new StreamWriter(fullPathName)) {
				outfile.Write(jsonStoryState);
			}
			AssetDatabase.ImportAsset(fullPathName.Substring(Application.dataPath.Length-6));
			TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPathName.Substring(Application.dataPath.Length-6));
			return textAsset;
		}

		public static bool StoryContainsVariables (Story story) {
			return story.variablesState.GetEnumerator().MoveNext();
		}

		public static bool CheckStoryIsValid (string storyJSON, out Exception exception) {
			try {
				new Story(storyJSON);
			} catch (Exception ex) {
				exception = ex;
				return false;
			}
			exception = null;
			return true;
		}

		public static bool CheckStoryIsValid (string storyJSON, out Story story) {
			try {
				story = new Story(storyJSON);
			} catch {
				story = null;
				return false;
			}
			return true;
		}

		public static bool CheckStoryIsValid (string storyJSON, out Exception exception, out Story story) {
			try {
				story = new Story(storyJSON);
			} catch (Exception ex) {
				exception = ex;
				story = null;
				return false;
			}
			exception = null;
			return true;
		}

		public static bool CheckStoryStateIsValid (string storyJSON, string storyStateJSON) {
			Story story;
			if(CheckStoryIsValid(storyJSON, out story)) {
				try {
					story.state.LoadJson(storyStateJSON);
				} catch {
					return false;
				}
			}
			return true;
		}
	}
}