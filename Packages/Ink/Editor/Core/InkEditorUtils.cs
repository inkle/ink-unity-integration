﻿#pragma warning disable IDE1006

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Ink.Runtime;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.Callbacks;
using Path = System.IO.Path;

namespace Ink.UnityIntegration {
	class CreateInkAssetAction : EndNameEditAction {
		public override void Action(int instanceId, string pathName, string resourceFile) {
			var text = "";
			if(File.Exists(resourceFile)) {
#pragma warning disable IDE0090 // Use 'new(...)'
				StreamReader streamReader = new StreamReader(resourceFile);
#pragma warning restore IDE0090 // Use 'new(...)'
				text = streamReader.ReadToEnd();
				streamReader.Close();
			}
			UnityEngine.Object asset = CreateScriptAsset(pathName, text);
			ProjectWindowUtil.ShowCreatedAsset(asset);
		}
		
		internal static UnityEngine.Object CreateScriptAsset(string pathName, string text) {
			string fullPath = Path.GetFullPath(pathName);
#pragma warning disable IDE0090 // Use 'new(...)'
			UTF8Encoding encoding = new UTF8Encoding(true, false);
#pragma warning restore IDE0090 // Use 'new(...)'
			bool append = false;
#pragma warning disable IDE0090 // Use 'new(...)'
			StreamWriter streamWriter = new StreamWriter(fullPath, append, encoding);
#pragma warning restore IDE0090 // Use 'new(...)'
			streamWriter.Write(text);
			streamWriter.Close();
			AssetDatabase.ImportAsset(pathName);
			return AssetDatabase.LoadAssetAtPath(pathName, typeof(DefaultAsset));
		}
	}
    
	[InitializeOnLoad]
	public static class InkEditorUtils {
		public const string inkFileExtension = ".ink";
		const string lastCompileTimeKey = "InkIntegrationLastCompileTime";

		
		// When compiling we call AssetDatabase.DisallowAutoRefresh. 
		// We NEED to remember to re-allow it or unity stops registering file changes!
		// The issue is that you need to pair calls perfectly, and you can't even use a try-catch to get around it.
		// So - we cache if we've disabled auto refresh here, since this persists across plays.
		// This does have one issue - this setting is saved even when unity re-opens, but the internal asset refresh state isn't.
		// We need this to reset on launching the editor.
		// We currently fix this by setting it false on InkEditorUtils.OnOpenUnityEditor
		// A potentially better approach is to use playerprefs for this, since it's really nothing to do with the library.
		public static bool disallowedAutoRefresh {
			get {
				if(EditorPrefs.HasKey("InkLibraryDisallowedAutoRefresh")) 
					return EditorPrefs.GetBool("InkLibraryDisallowedAutoRefresh");
				return false;
			} set {
				EditorPrefs.SetBool("InkLibraryDisallowedAutoRefresh", value);
			}
		}

		// This should run before any of the other ink integration scripts.
		static InkEditorUtils () {
			EnsureFirstLaunchHandled();
			EditorApplication.wantsToQuit += WantsToQuit;
		}

		// Save the current EditorApplication.timeSinceStartup so OnOpenUnityEditor is sure to run next time the editor opens. 
		static bool WantsToQuit () {
			LoadAndSaveLastCompileTime();
			return true;
		}
		public static bool isFirstCompile;
		static void EnsureFirstLaunchHandled () {
			float lastCompileTime = LoadAndSaveLastCompileTime();
			isFirstCompile = EditorApplication.timeSinceStartup < lastCompileTime;
			if(isFirstCompile)
				OnOpenUnityEditor();
		}

		static float LoadAndSaveLastCompileTime () {
			float lastCompileTime = 0;
			if(EditorPrefs.HasKey(lastCompileTimeKey))
				lastCompileTime = EditorPrefs.GetFloat(lastCompileTimeKey);
			EditorPrefs.SetFloat(lastCompileTimeKey, (float)EditorApplication.timeSinceStartup);
			return lastCompileTime;
		}

		static void OnOpenUnityEditor () {
			disallowedAutoRefresh = false;
		}

		[MenuItem("Assets/Rebuild Ink Library", false, 200)]
		public static void RebuildLibrary() {
			InkLibrary.Rebuild();
		}

		[MenuItem("Assets/Recompile Ink", false, 201)]
		public static void RecompileAll() {
			var filesToRecompile = InkLibrary.FilesCompiledByRecompileAll().ToArray();
			string logString = filesToRecompile.Any() ? 
				"Recompile All will compile "+string.Join(", ", filesToRecompile.Select(x => Path.GetFileName(x.filePath)).ToArray()) :
				"No valid ink found. Note that only files with 'Compile Automatic' checked are compiled if not set to compile all files automatically in InkSettings file.";
			Debug.Log(logString);
			InkCompiler.CompileInk(filesToRecompile);
		}

        public static void RecompileAllImmediately() {
            var filesToRecompile = InkLibrary.FilesCompiledByRecompileAll().ToArray();
            string logString = filesToRecompile.Any() ? 
                                   "Recompile All Immediately will compile "+string.Join(", ", filesToRecompile.Select(x => Path.GetFileName(x.filePath)).ToArray()) :
                                   "No valid ink found. Note that only files with 'Compile Automatic' checked are compiled if not set to compile all files automatically in InkSettings file.";
            Debug.Log(logString);
            InkCompiler.CompileInk(filesToRecompile, true, null);
        }


		[MenuItem("Assets/Create/Ink", false, 120)]
		public static void CreateNewInkFile () {
			string fileName = "New Ink.ink";
			string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GetSelectedPathOrFallback(), fileName));
			CreateNewInkFile(filePath, InkSettings.instance.templateFilePath);
		}

		public static void CreateNewInkFile (string filePath, string templateFileLocation) {
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

		[MenuItem("Help/Ink/Writing Tutorial...")]
		public static void OpenWritingDocumentation() {
			Application.OpenURL("https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md");
		}
		
        [MenuItem("Help/Ink/API Documentation...")]
		public static void OpenAPIDocumentation() {
			Application.OpenURL("https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md");
		}

		[MenuItem("Help/Ink/Donate...")]
		public static void Donate() {
			Application.OpenURL("https://www.patreon.com/inkle");
		}

		[PostProcessBuildAttribute(-1)]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
			if(!Debug.isDebugBuild) {
				var color = EditorGUIUtility.isProSkin ? "#3498db" : "blue";
				Debug.Log("<color="+color+">Thanks for using ink, and best of luck with your release!\nIf you're doing well, please help fund the project via Patreon https://www.patreon.com/inkle</color>");
			}
		}

		public static TextAsset CreateStoryStateTextFile (string jsonStoryState, string defaultPath = "Assets/Ink", string defaultName = "storyState") {
			string name = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(defaultPath, defaultName+".json")).Substring(defaultPath.Length+1);
			string fullPathName = EditorUtility.SaveFilePanel("Save Story State", defaultPath, name, "json");
			if(fullPathName == "") 
				return null;
#pragma warning disable IDE0090 // Use 'new(...)'
			using (StreamWriter outfile = new StreamWriter(fullPathName)) {
#pragma warning restore IDE0090 // Use 'new(...)'
				outfile.Write(jsonStoryState);
			}
			string relativePath = AbsoluteToUnityRelativePath(fullPathName);
			AssetDatabase.ImportAsset(relativePath);
			TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
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
		
		// Returns a sanitized version of the supplied string by:
		//    - swapping MS Windows-style file separators with Unix/Mac style file separators.
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
            Debug.Assert(firstPath != null);
            Debug.Assert(secondPath != null);
			return SanitizePathString(Path.Combine(firstPath, secondPath));
		}

		public static string AbsoluteToUnityRelativePath(string fullPath) {
			return SanitizePathString(fullPath.Substring(Application.dataPath.Length-6));
		}

		public static string UnityRelativeToAbsolutePath(string filePath) {
			return InkEditorUtils.CombinePaths(Application.dataPath, filePath.Substring(7));
		}

		/// <summary>
		/// Draws a property field for a story using GUILayout, allowing you to attach stories to the player window for debugging.
		/// </summary>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Story story, GUIContent label) {
			Debug.LogWarning("DrawStoryPropertyField has been moved from InkEditorUtils to InkPlayerWindow");
		}

		/// <summary>
		/// Draws a property field for a story using GUI, allowing you to attach stories to the player window for debugging.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Rect position, Story story, GUIContent label) {
			Debug.LogWarning("DrawStoryPropertyField has been moved from InkEditorUtils to InkPlayerWindow");
		}
		
		/// <summary>
		/// Checks to see if the given path is an ink file or not, regardless of extension.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns>True if it's an ink file, otherwise false.</returns>
		public static bool IsInkFile(string path) {
			string extension = Path.GetExtension(path);
			if (extension == InkEditorUtils.inkFileExtension) {
				return true;
			}

			return String.IsNullOrEmpty(extension) && InkLibrary.instance.inkLibrary.Exists(f => f.filePath == path);
		}
	}
}