using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using Ink.Runtime;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.Callbacks;
using Path = System.IO.Path;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public static class InkEditorUtils {
		class CreateInkAssetAction : EndNameEditAction {
			public override void Action(int instanceId, string pathName, string resourceFile) {
				var text = "";
				if(File.Exists(resourceFile)) {
					StreamReader streamReader = new StreamReader(resourceFile);
					text = streamReader.ReadToEnd();
					streamReader.Close();
				}
				var assetPath = CreateScriptAsset(pathName, text);
				var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
				ProjectWindowUtil.ShowCreatedAsset(asset);
			}
		}
		public const string inkFileExtension = ".ink";
		const string lastCompileTimeKey = "InkIntegrationLastCompileTime";

		private static Texture2D _inkLogoIcon;
		public static Texture2D inkLogoIcon {
			get {
				if(_inkLogoIcon == null) {
					_inkLogoIcon = Resources.Load<Texture2D>("InkLogoIcon");
				}
				return _inkLogoIcon;
			}
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
		public static void CreateNewInkFileAtSelectedPathWithTemplateAndStartNameEditing () {
			string fileName = "New Ink.ink";
			string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GetSelectedPathOrFallback(), fileName));
			CreateNewInkFileAtPathWithTemplateAndStartNameEditing(filePath, InkSettings.instance.templateFilePath);
		}
		
		public static void CreateNewInkFileAtPathWithTemplateAndStartNameEditing (string filePath, string templateFileLocation) {
			if(Path.GetExtension(filePath) != inkFileExtension) filePath += inkFileExtension;
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateInkAssetAction>(), filePath, InkBrowserIcons.inkFileIcon, templateFileLocation);
		}

		public static DefaultAsset CreateNewInkFileAtPath (string filePath, string text) {
			if(Path.GetExtension(filePath) != inkFileExtension) filePath += inkFileExtension;
			var assetPath = CreateScriptAsset(filePath, text);
			return AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetPath);
		}
		
		static string CreateScriptAsset(string pathName, string text) {
			string fullPath = Path.GetFullPath(pathName);
			fullPath = fullPath.Replace('\\', '/');
			var assetRelativePath = fullPath;
			if(fullPath.StartsWith(Application.dataPath)) {
				assetRelativePath = fullPath.Substring(Application.dataPath.Length-6); 
			}
			var directoryPath = Path.GetDirectoryName(fullPath);
			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			UTF8Encoding encoding = new UTF8Encoding(true, false);
			StreamWriter streamWriter = null;
			streamWriter = new StreamWriter(fullPath, false, encoding);
			streamWriter.Write(text);
			streamWriter.Close();
			AssetDatabase.ImportAsset(assetRelativePath);
			return assetRelativePath;
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

		[MenuItem("Help/Ink/Discord (Community + Support...")]
		public static void OpenDiscord() {
			Application.OpenURL("https://discord.gg/inkle");
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
			string name = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(defaultPath, defaultName+".json"));
			if(!string.IsNullOrEmpty(defaultPath)) name = name.Substring(defaultPath.Length+1);
			string fullPathName = EditorUtility.SaveFilePanel("Save Story State", defaultPath, name, "json");
			if(fullPathName == "") 
				return null;
			using (StreamWriter outfile = new StreamWriter(fullPathName)) {
				outfile.Write(jsonStoryState);
			}
			
			if(fullPathName.StartsWith(Application.dataPath)) {
				string relativePath = AbsoluteToUnityRelativePath(fullPathName);
				AssetDatabase.ImportAsset(relativePath);
				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
				return textAsset;
			}
			else return null;
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
			return SanitizePathString(firstPath+"/"+secondPath);
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



		/// <summary>
		/// Opens an ink file in the associated editor at the correct line number.
		/// TODO - If the editor is inky, this code should load the master file, but immediately show the correct child file at the correct line.
		/// </summary>
		public static void OpenInEditor (InkFile inkFile, InkCompilerLog log) {
			var targetFilePath = log.GetAbsoluteFilePath(inkFile);
			// EditorUtility.OpenWithDefaultApp(targetFilePath);
			AssetDatabase.OpenAsset(inkFile.inkAsset, log.lineNumber);
			// Unity.CodeEditor.CodeEditor.OSOpenFile();
#if UNITY_2019_1_OR_NEWER

			// This function replaces OpenFileAtLineExternal, but I guess it's totally internal and can't be accessed.
			// CodeEditorUtility.Editor.Current.OpenProject(targetFilePath, lineNumber);
			// #pragma warning disable
			// UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(targetFilePath, log.lineNumber);
			// #pragma warning restore
#else
			UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(targetFilePath, log.lineNumber);
#endif
		}
		/// <summary>
		/// Opens an ink file in the associated editor at the correct line number.
		/// TODO - If the editor is inky, this code should load the master file, but immediately show the correct child file at the correct line.
		/// </summary>
		public static void OpenInEditor (string masterFilePath, string subFilePath, int lineNumber) {
			if(!string.IsNullOrEmpty(subFilePath) && Path.GetFileName(masterFilePath) != subFilePath) {
				Debug.LogWarning("Tried to open an ink file ("+subFilePath+") at line "+lineNumber+" but the file is an include file. This is not currently implemented. The master ink file will be opened at line 0 instead.");
				lineNumber = 0;
			}
			#if UNITY_2019_1_OR_NEWER
			// This function replaces OpenFileAtLineExternal, but I guess it's totally internal and can't be accessed.
			// CodeEditorUtility.Editor.Current.OpenProject(masterFilePath, lineNumber);
			#pragma warning disable
			UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(masterFilePath, lineNumber);
			#pragma warning restore
			#else
			UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(masterFilePath, lineNumber);
			#endif
		}




		public static string FormatJson(string json) {
			const string INDENT_STRING = "	";

			int indentation = 0;
			int quoteCount = 0;
			var result = 
				from ch in json
				let quotes = ch == '"' ? quoteCount++ : quoteCount
				let lineBreak = ch == ',' && quotes % 2 == 0 ? ch + Environment.NewLine +  String.Concat(Enumerable.Repeat(INDENT_STRING, indentation)) : null
				let openChar = ch == '{' || ch == '[' ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, ++indentation)) : ch.ToString()
				let closeChar = ch == '}' || ch == ']' ? Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, --indentation)) + ch : ch.ToString()
				select lineBreak == null    
							? openChar.Length > 1 
								? openChar 
								: closeChar
							: lineBreak;

			return String.Concat(result);
		}


		// If this plugin is installed as a package, returns info about it.
		public static UnityEditor.PackageManager.PackageInfo GetPackageInfo() {
			var packageAssetPath = "Packages/com.inkle.ink-unity-integration";
			if (AssetDatabase.IsValidFolder(packageAssetPath)) return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageAssetPath);
			else return null;
		}
		
		// Gets the root directory of this plugin, enabling us to find assets within it.
		// Less efficent if not installed as a package because the location/folder name is not known.
		public static string FindAbsolutePluginDirectory() {
			var packageInfo = GetPackageInfo();
			if (packageInfo != null) {
				return packageInfo.resolvedPath;
			} else {
				// Find the InkLibs folder. We assume that it exists in the top level of the plugin folder. We use this folder because it has a fairly unique name and is essential for the plugin to function.
				string[] guids = AssetDatabase.FindAssets("t:DefaultAsset", new[] {"Assets"}).Where(g => AssetDatabase.GUIDToAssetPath(g).EndsWith("/InkLibs")).ToArray();
				if (guids.Length > 0) {
					var assetPathOfInkLibsFolder = AssetDatabase.GUIDToAssetPath(guids[0]);
					var rootPluginFolder = assetPathOfInkLibsFolder.Substring(0, assetPathOfInkLibsFolder.Length - "/InkLibs".Length);
					return Path.GetFullPath(Path.Combine(Application.dataPath, "..", rootPluginFolder));
				}
			}
			return null; // If no folder is found
		}
	}
}