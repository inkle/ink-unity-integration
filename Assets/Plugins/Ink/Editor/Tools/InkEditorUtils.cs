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

		[MenuItem("Assets/Create/Ink")]
		public static void CreateFile() {
	//		var icon = AssetDatabase.LoadAssetAtPath("Assets/Plugins/Ink/Resources/InkIcon.png", typeof(Texture2D)) as Texture2D;
	//		ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<Ink>(), "NewFSharpFile.fs", icon, "");
			//....
			//IO stuff to create the file.
	//		ProjectWindowUtil.ShowCreatedAsset(file);
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
			string name = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(defaultPath, defaultName+".txt")).Substring(defaultPath.Length+1);
			string fullPathName = EditorUtility.SaveFilePanel("Save Story State", defaultPath, name, "txt");
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