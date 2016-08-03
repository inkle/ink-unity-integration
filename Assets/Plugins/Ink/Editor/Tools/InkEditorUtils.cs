using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Ink.Runtime;
using UnityEditor.ProjectWindowCallback;

namespace Ink.UnityIntegration {

	class CreateInkAssetAction : EndNameEditAction {
		public override void Action(int instanceId, string pathName, string resourceFile) {
			UnityEngine.Object asset = CreateScriptAssetFromTemplate(pathName, resourceFile);
			ProjectWindowUtil.ShowCreatedAsset(asset);
		}
		
		internal static UnityEngine.Object CreateScriptAssetFromTemplate(string pathName, string templateFilePath) {
			string fullPath = Path.GetFullPath(pathName);
			string text = "";
			if(File.Exists(templateFilePath)) {
				StreamReader streamReader = new StreamReader(templateFilePath);
				text = streamReader.ReadToEnd();
				streamReader.Close();
			} else {
				Debug.LogWarning("Could not find .ink template file at expected path "+templateFilePath+". New file will be empty.");
			}
			UTF8Encoding encoding = new UTF8Encoding(true, false);
			bool append = false;
			StreamWriter streamWriter = new StreamWriter(fullPath, append, encoding);
			streamWriter.Write(text);
			streamWriter.Close();
			AssetDatabase.ImportAsset(pathName);
			return AssetDatabase.LoadAssetAtPath(pathName, typeof(DefaultAsset));
		}
	}

	public static class InkEditorUtils {
		public const string inkFileExtension = ".ink";
		private const string templateFileLocation = "Assets/Plugins/Ink/Template/Template.txt";

		[MenuItem("Assets/Create/Ink", false, 120)]
		public static void CreateNewInkFile () {
			string fileName = "New Ink.ink";
			string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GetSelectedPathOrFallback(), fileName));
			CreateNewInkFile(filePath);
		}

		public static void CreateNewInkFile (string filePath) {
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateInkAssetAction>(), filePath, InkBrowserIcons.inkFileIcon, templateFileLocation);
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

		

		[MenuItem("Help/Ink/About")]
		public static void OpenAbout() {
			Application.OpenURL("https://github.com/inkle/ink#ink");
		}

		[MenuItem("Help/Ink/API Documentation...")]
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

		public static string GetInklecateFilePath () {
			#if UNITY_EDITOR
			#if UNITY_EDITOR_WIN
			string inklecateName = "inklecate_win.exe";
			#endif
			// Unfortunately inklecate's implementation uses newer features of C# that aren't
			// available in the version of mono that ships with Unity, so we can't make use of
			// it. This means that we need to compile the mono runtime directly into it, inflating
			// the size of the executable quite dramatically :-( Hopefully we can improve that
			// when Unity ships with a newer version.
			#if UNITY_EDITOR_OSX
			string inklecateName = "inklecate_mac";
			#endif
			#endif

			string[] inklecateDirectories = Directory.GetFiles(Application.dataPath, inklecateName, SearchOption.AllDirectories);
			if(inklecateDirectories.Length == 0) {
				return null;
			} else {
				return Path.GetFullPath(inklecateDirectories[0]);
			}
		}
		
		// Returns a sanitized version of the supplied string by:
		//    - swapping MS Windows-style file separators with Unix/Mac style file separators.
		//
		// If null is provided, null is returned.
		public static string SanitizePathString(string path) {
			if (path == null) {
				return null;
			}
			return path.Replace('\\', '/');
		}
		
		// Combines two file paths and returns that path.  Unlike C#'s native Paths.Combine, regardless of operating 
		// system this method will always return a path which uses forward slashes ('/' characters) exclusively to ensure
		// equality checks on path strings return equalities as expected.
		public static string CombinePaths(string firstPath, string secondPath) {
			return SanitizePathString(Path.Combine(firstPath, secondPath));
		}
	}
}