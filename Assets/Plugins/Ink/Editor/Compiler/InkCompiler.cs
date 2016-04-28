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
		public static List<string> filesCompiling = new List<string>();
		public static List<KeyValuePair<string, string>> inkJSONAssetsToLoad = new List<KeyValuePair<string, string>>();

		public delegate void OnCompileInkEvent (string inkAbsoluteFilePath, TextAsset compiledJSONTextAsset);
		public static event OnCompileInkEvent OnCompileInk;

		static InkCompiler () {
			filesCompiling.Clear();
			inkJSONAssetsToLoad.Clear();
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
			if(filesCompiling.Contains(inputPath)) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inputPath);
				return;
			}
			string outputPath = Path.Combine(absoluteFolderPath, Path.GetFileNameWithoutExtension(fileName))+".json";
			string inkArguments = "-c -o "+"\""+outputPath +"\" \""+inputPath+"\"";

			filesCompiling.Add(inputPath);
			EditorUtility.DisplayProgressBar("Compiling Ink...", "Compiling '"+fileName+"'", 0);
//			Debug.Log("COMPILE - "+absoluteFilePath+" "+filesCompiling.Count);

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
			process.ErrorDataReceived += OnProcessError;
			process.Start();
		}

		static void OnProcessError (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			Debug.Log("HERERERERE");
			Debug.LogError("Fatal Error compiling Ink! Please report this as a bug.");
			filesCompiling.Remove(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
		}

		static void OnCompileProcessComplete(object sender, System.EventArgs e) {
			Process process = (Process)sender;
			string absoluteFolderPath = process.StartInfo.WorkingDirectory;
			
			if( _errorRegex == null ) {
				_errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");
			}

			InkFile inkFile = InkLibrary.GetInkFileWithAbsolutePath(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			inkFile.errors.Clear();
			inkFile.warnings.Clear();
			inkFile.todos.Clear();

			string allOutput = process.StandardOutput.ReadToEnd();
			string[] splitOutput = allOutput.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
			bool foundError = false;
			foreach(string output in splitOutput) {
				var match = _errorRegex.Match(output);
				if (match.Success) {
					string errorType = null;
					string filename = null;
					int lineNo = -1;
					string message = null;

					var errorTypeCapture = match.Groups["errorType"];
					if( errorTypeCapture != null ) {
						errorType = errorTypeCapture.Value;
					}

					var filenameCapture = match.Groups["filename"];
					if (filenameCapture != null)
						filename = filenameCapture.Value;

					var lineNoCapture = match.Groups["lineNo"];
					if (lineNoCapture != null)
						lineNo = int.Parse (lineNoCapture.Value);

					var messageCapture = match.Groups["message"];
					if (messageCapture != null)
						message = messageCapture.Value.Trim();
					
					string pathAndLineNumberString = "\n"+Path.Combine(absoluteFolderPath, filename)+"("+lineNo+")";

					if(errorType == "ERROR") {
						inkFile.errors.Add(message);
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString);
						foundError = true;
					} else if (errorType == "WARNING") {
						inkFile.warnings.Add(message);
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString);
					} else if (errorType == "TODO") {
						inkFile.todos.Add(message);
						Debug.Log("INK "+errorType+": "+message + pathAndLineNumberString);
					}
				}
			}

//			Debug.Log("COMPLETE - "+process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]+" "+filesCompiling.Contains(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"])+" "+filesCompiling.Count);
			var removed = filesCompiling.Remove(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			if(!removed) {
				foreach(var x in filesCompiling)
					Debug.Log(x);
				filesCompiling.Clear();
			}

			if(!foundError) {
				inkJSONAssetsToLoad.Add(new KeyValuePair<string, string>(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"], process.StartInfo.EnvironmentVariables["jsonAbsoluteFilePath"]));
			}
			
			if(filesCompiling.Count == 0) {
				// This event runs in another thread, preventing us from calling some UnityEditor functions directly. Instead, we delay till the next inspector update.
				EditorApplication.delayCall += Delay;
			}
		}

		private static void Delay () {
//			Debug.Log("DELAY! "+filesCompiling.Count);
			if(filesCompiling.Count > 0)
				return;
			for (int i = 0; i < inkJSONAssetsToLoad.Count; i++) {
				string localJSONAssetPath = inkJSONAssetsToLoad [i].Value.Substring (Application.dataPath.Length - 6);
				AssetDatabase.ImportAsset (localJSONAssetPath);
				TextAsset jsonTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (localJSONAssetPath);
				if (OnCompileInk != null) {
					OnCompileInk (inkJSONAssetsToLoad [i].Key, jsonTextAsset);
				}
			}
			inkJSONAssetsToLoad.Clear();
			EditorUtility.ClearProgressBar();
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				Debug.LogWarning("Ink just finished recompiling while in play mode. Your runtime story may not be up to date.");
			}
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

//			string defaultInklecateFilePath = Path.Combine(Application.dataPath, "Plugins/Ink/DLL/"+inklecateName);
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