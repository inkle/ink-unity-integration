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
#if UNITY_6000_4_OR_NEWER
		class CreateInkAssetAction : AssetCreationEndAction {
			public override void Action(EntityId entityId, string pathName, string resourceFile) {
#else
		class CreateInkAssetAction : EndNameEditAction {
			public override void Action(int instanceId, string pathName, string resourceFile) {
#endif
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

		// Version constants (previously on InkLibrary, which has been retired in favour of the InkImporter).
		public static readonly System.Version inkVersionCurrent = new System.Version(1,2,1);
		public static readonly System.Version unityIntegrationVersionCurrent = new System.Version(2,0,0);

		private static Texture2D _inkLogoIcon;

		static InkEditorUtils() {
			var isFirstLaunch = SessionState.GetBool("InkIsFirstUnityLaunch", true);
			if (isFirstLaunch) {
				SessionState.SetBool("InkIsFirstUnityLaunch", false);
				if(InkSettings.instance.automaticallyAddDefineSymbols)
					InkDefineSymbols.AddGlobalDefine();
			}
		}

		public static Texture2D inkLogoIcon {
			get {
				if(_inkLogoIcon == null) {
					_inkLogoIcon = Resources.Load<Texture2D>("InkLogoIcon");
				}
				return _inkLogoIcon;
			}
		}


		/// <summary>
		/// Reimports every ink file, recompiling all master files via the InkImporter. A scripting utility
		/// for the rare case you need to force a full recompile (e.g. a CI lint step) — ink recompiles
		/// automatically on import, so this isn't exposed as a menu item.
		/// </summary>
		public static void ForceRecompileAllInkFilesAsync() {
			ReimportAllInkFiles(ImportAssetOptions.Default);
		}

		/// <summary>As <see cref="ForceRecompileAllInkFilesAsync"/>, but blocks until all imports finish.
		/// For build scripts; no menu item, since a synchronous reimport freezes the editor.</summary>
		public static void ForceRecompileAllInkFilesSync() {
			ReimportAllInkFiles(ImportAssetOptions.ForceSynchronousImport);
		}

		static void ReimportAllInkFiles(ImportAssetOptions options) {
			var guids = AssetDatabase.FindAssets("glob:\"*.ink\"");
			AssetDatabase.StartAssetEditing();
			try {
				foreach (var guid in guids) {
					var path = AssetDatabase.GUIDToAssetPath(guid);
					AssetDatabase.ImportAsset(path, options | ImportAssetOptions.ForceUpdate);
				}
			} finally {
				AssetDatabase.StopAssetEditing();
			}
		}



		[MenuItem("Assets/Create/Ink", false, 120)]
		public static void CreateNewInkFileAtSelectedPathWithTemplateAndStartNameEditing () {
			string fileName = "New Ink.ink";
			string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GetSelectedPathOrFallback(), fileName));
			CreateNewInkFileAtPathWithTemplateAndStartNameEditing(filePath, InkSettings.instance.templateFilePath);
		}
		
		public static void CreateNewInkFileAtPathWithTemplateAndStartNameEditing (string filePath, string templateFileLocation) {
			if(Path.GetExtension(filePath) != inkFileExtension) filePath += inkFileExtension;
#if UNITY_6000_4_OR_NEWER
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None, ScriptableObject.CreateInstance<CreateInkAssetAction>(), filePath, InkBrowserIcons.inkFileIcon, templateFileLocation);
#else
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateInkAssetAction>(), filePath, InkBrowserIcons.inkFileIcon, templateFileLocation);
#endif
		}

		/// <summary>Creates a new .ink file at the given path with the given contents and returns the imported asset.</summary>
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

		/// <summary>Returns true if the JSON parses as a valid ink story; otherwise false, with the parse exception.</summary>
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
		
		/// <summary>Normalises a path to use forward slashes (returns null for null input).</summary>
		public static string SanitizePathString(string path) {
			if (path == null) {
				return null;
			}
			return path.Replace('\\', '/');
		}
		
		/// <summary>Combines two paths, always using forward slashes (unlike Path.Combine) so path comparisons are consistent.</summary>
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

		/// <summary>Returns true if the given path is a .ink file.</summary>
		public static bool IsInkFile(string path) {
			if (string.IsNullOrEmpty(path)) return false;
			return Path.GetExtension(path) == inkFileExtension;
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
			// This function replaces OpenFileAtLineExternal, but I guess it's totally internal and can't be accessed.
			// CodeEditorUtility.Editor.Current.OpenProject(masterFilePath, lineNumber);
			#pragma warning disable
			UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(masterFilePath, lineNumber);
			#pragma warning restore
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
