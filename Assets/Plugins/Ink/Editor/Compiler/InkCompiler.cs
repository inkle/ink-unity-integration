using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public static class InkCompiler {
		public static bool compiling {
			get {
				return inkJSONAssetsToLoad.Count > 0;
			}
		}
		public static List<KeyValuePair<string, string>> inkJSONAssetsToLoad = new List<KeyValuePair<string, string>>();

		public delegate void OnCompileInkEvent (string inkAbsoluteFilePath, TextAsset compiledJSONTextAsset);
		public static event OnCompileInkEvent OnCompileInk;

		static InkCompiler () {
			EditorApplication.playmodeStateChanged += OnPlayModeChange;
		}

		private static void OnPlayModeChange () {
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				if(compiling)
					Debug.LogWarning("Entered Play Mode while Ink was still compiling. Recommend exiting and re-entering play mode.");
			}
		}

		[MenuItem("Ink/Recompile Ink")]
		public static void RecompileAll() {
			InkLibrary.Refresh();
			List<InkFile> masterInkFiles = InkLibrary.GetMasterInkFiles ();
			foreach(InkFile masterInkFile in masterInkFiles) {
				CompileInk(masterInkFile);
			}
		}

		public static void CompileInk (InkFile inkFile) {
			if(inkFile.master != null)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");
			CompileInk(inkFile.absoluteFilePath);
		}

		public static void CompileInk (string absoluteFilePath) {
			string inklecatePath = GetInklecateFilePath();
			if(inklecatePath == null) {
				UnityEngine.Debug.LogWarning("Inklecate (the ink compiler) not found in assets. This will prevent automatic building of JSON TextAsset files from ink story files.");
				return;
			}

			string absoluteFolderPath = Path.GetDirectoryName(absoluteFilePath);
			string fileName = Path.GetFileName(absoluteFilePath);
			string inputPath = Path.Combine(absoluteFolderPath, fileName);
			string outputPath = Path.Combine(absoluteFolderPath, Path.GetFileNameWithoutExtension(fileName))+".json";
			string inkArguments = "-c -o "+"\""+outputPath +"\" \""+inputPath+"\"";

			Process process = new Process();
			process.StartInfo.WorkingDirectory = absoluteFolderPath;
			process.StartInfo.FileName = inklecatePath;
			process.StartInfo.Arguments = inkArguments;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"] = inputPath;
			process.StartInfo.EnvironmentVariables["jsonAbsoluteFilePath"] = outputPath;
			process.Exited += OnCompileProcessComplete;
			process.Start();
		}

		static void OnCompileProcessComplete(object sender, System.EventArgs e) {
			Process process = (Process)sender;
			string absoluteFolderPath = process.StartInfo.WorkingDirectory;

			if( _errorRegex == null ) {
				_errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");
			}

			// This event runs in another thread, preventing us from calling AssetDatabase.Refresh directly.
			// Instead, this flag is picked up by the update loop.
			string allOutput = process.StandardOutput.ReadToEnd();
			string[] splitOutput = allOutput.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
			bool foundError = false;
			foreach(string output in splitOutput) {

				var match = _errorRegex.Match(output);
				if (match.Success) {

					bool isHardError = false;
					string errorType = null;
					string filename = null;
					int lineNo = -1;
					string message = null;

					var errorTypeCapture = match.Groups["errorType"];
					if( errorTypeCapture != null ) {
						errorType = errorTypeCapture.Value;
						isHardError = errorType == "ERROR";
					}

					var filenameCapture = match.Groups["filename"];
					if (filenameCapture != null)
						filename = filenameCapture.Value;

					var lineNoCapture = match.Groups["lineNo"];
					if (lineNoCapture != null)
						lineNo = int.Parse (lineNoCapture.Value);

					var messageCapture = match.Groups["message"];
					if (messageCapture != null)
						message = messageCapture.Value;
					
					string pathAndLineNumberString = "\n"+Path.Combine(absoluteFolderPath, filename)+"("+lineNo+")";

					if(isHardError) {
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString);
						foundError = true;
					} else {
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString);
					}
				}
			}

			if(!foundError) {
				inkJSONAssetsToLoad.Add(new KeyValuePair<string, string>(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"], process.StartInfo.EnvironmentVariables["jsonAbsoluteFilePath"]));
				if(inkJSONAssetsToLoad.Count == 1)
					EditorApplication.delayCall += Delay;
			}
		}

		private static void Delay () {
			foreach(var s in inkJSONAssetsToLoad) {
				string localJSONAssetPath = s.Value.Substring(Application.dataPath.Length-6);
				AssetDatabase.ImportAsset(localJSONAssetPath);
				TextAsset jsonTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(localJSONAssetPath);
				if(OnCompileInk != null) {
					OnCompileInk(s.Key, jsonTextAsset);
				}
			}
			inkJSONAssetsToLoad.Clear();
		}

		private static string GetInklecateFilePath () {
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

//			string defaultInklecateFilePath = Path.Combine(Application.dataPath, "Plugins/Ink/Editor/Compiler/"+inklecateName);
//			if(File.Exists(defaultInklecateFilePath))
//				return defaultInklecateFilePath;
			string[] inklecateDirectories = Directory.GetFiles(Application.dataPath, inklecateName, SearchOption.AllDirectories);
			if(inklecateDirectories.Length == 0) {
				return null;
			} else {
				return inklecateDirectories[0];
			}
		}

		static Regex _errorRegex;
	}
}