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
				return InkLibrary.Instance.compilingFiles.Count > 0;
			}
		}
//		public static List<string> filesCompiling = new List<string>();
//		public static List<KeyValuePair<string, string>> inkJSONAssetsToLoad = new List<KeyValuePair<string, string>>();

		public delegate void OnCompileInkEvent (InkFile inkFile);
		public static event OnCompileInkEvent OnCompileInk;

		public class PendingInkFileProperties {
			public State state = State.Idle;
			public InkFile inkFile;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public string output;
			public enum State {
				Idle,
				Compiling,
				Importing
			}
		}

		static InkCompiler () {
//			filesCompiling.Clear();
//			inkJSONAssetsToLoad.Clear();
			EditorApplication.playmodeStateChanged += OnPlayModeChange;
		}

		private static void OnPlayModeChange () {
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				if(compiling)
					Debug.LogWarning("Entered Play Mode while Ink was still compiling. Recommend exiting and re-entering play mode.");
			}
		}

		[MenuItem("Assets/Recompile Ink", false, 60)]
		public static void RecompileAll() {
			InkLibrary.Refresh();
			List<InkFile> masterInkFiles = InkLibrary.GetMasterInkFiles ();
			foreach(InkFile masterInkFile in masterInkFiles) {
				CompileInk(masterInkFile);
			}
		}

		public static void CompileInk (InkFile inkFile) {
			if(inkFile == null) {
				Debug.LogError("Tried to compile ink file, but input was null.");
				return;
			}
			if(inkFile.master != null)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");
			if(InkLibrary.Instance.compilingFiles.ContainsKey(inkFile)) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inkFile.filePath);
				return;
			}
//			absoluteFilePath = Path.GetFullPath(absoluteFilePath);
			string inklecatePath = GetInklecateFilePath();
			if(inklecatePath == null) {
				UnityEngine.Debug.LogWarning("Inklecate (the ink compiler) not found in assets. This will prevent automatic building of JSON TextAsset files from ink story files.");
				return;
			}

			string inputPath = Path.Combine(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));

			string outputPath = Path.Combine(inkFile.absoluteFolderPath, Path.GetFileNameWithoutExtension(Path.GetFileName(inkFile.filePath)))+".json";
			string inkArguments = "-c -o "+"\""+outputPath +"\" \""+inputPath+"\"";

//			filesCompiling.Add(inputPath);
			PendingInkFileProperties pendingFile = new PendingInkFileProperties();
			pendingFile.inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath);
			pendingFile.inkAbsoluteFilePath = inputPath;
			pendingFile.jsonAbsoluteFilePath = outputPath;
			pendingFile.state = PendingInkFileProperties.State.Compiling;
			InkLibrary.Instance.compilingFiles.Add(pendingFile.inkFile, pendingFile);

			EditorUtility.DisplayProgressBar("Compiling Ink...", "Compiling '"+Path.GetFileName(inkFile.filePath)+"'", 0);
//			Debug.Log("COMPILE - "+absoluteFilePath+" "+filesCompiling.Count);

			Process process = new Process();
			process.StartInfo.WorkingDirectory = inkFile.absoluteFolderPath;
			process.StartInfo.FileName = inklecatePath;
			process.StartInfo.Arguments = inkArguments;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"] = inputPath;
//			process.StartInfo.EnvironmentVariables["jsonAbsoluteFilePath"] = outputPath;
			process.Exited += OnCompileProcessComplete;
			process.ErrorDataReceived += OnProcessError;
			process.Start();
		}
		static bool AnyFilesInStackCompiling () {
			foreach(var x in InkLibrary.Instance.compilingFiles) {
				if(x.Value.state == PendingInkFileProperties.State.Compiling) 
					return true;
			}
			return false;
		}
		static InkFile GetInkFileFromOutsideMainThreadFromAbsoluteFilePath (string inkAbsoluteFilePath) {
			foreach(var x in InkLibrary.Instance.compilingFiles) {
				if(x.Value.inkAbsoluteFilePath == inkAbsoluteFilePath) 
					return x.Value.inkFile;
			}
			Debug.LogError("Fatal Error compiling Ink! No file found! Please report this as a bug. "+inkAbsoluteFilePath);
			return null;

		}
		static void OnProcessError (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			Debug.LogError("Fatal Error compiling Ink! Ink failed to process. Please report this as a bug.");
			InkFile inkFile = InkLibrary.GetInkFileWithAbsolutePath(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			Debug.LogError(inkFile);
			InkLibrary.Instance.compilingFiles.Remove(inkFile);
		}

		static void OnCompileProcessComplete(object sender, System.EventArgs e) {
			Process process = (Process)sender;
			string absoluteFolderPath = process.StartInfo.WorkingDirectory;

//			InkFile inkFile = InkLibrary.GetInkFileWithPath(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			InkFile inkFile = GetInkFileFromOutsideMainThreadFromAbsoluteFilePath(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			PendingInkFileProperties pendingFile = InkLibrary.Instance.compilingFiles[inkFile];
			pendingFile.state = PendingInkFileProperties.State.Importing;
			pendingFile.output = process.StandardOutput.ReadToEnd();

//			InkFile inkFile = InkLibrary.GetInkFileWithAbsolutePath(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			

//			Debug.Log("COMPLETE - "+process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]+" "+filesCompiling.Contains(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"])+" "+filesCompiling.Count);
//			var removed = filesCompiling.Remove(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
//			if(!removed) {
//				foreach(var x in filesCompiling)
//					Debug.Log(x);
//				filesCompiling.Clear();
//			}

//			if(!foundError) {
//				inkJSONAssetsToLoad.Add(new KeyValuePair<string, string>(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"], process.StartInfo.EnvironmentVariables["jsonAbsoluteFilePath"]));
//			}
			
			if(!AnyFilesInStackCompiling()) {
				// This event runs in another thread, preventing us from calling some UnityEditor functions directly. Instead, we delay till the next inspector update.
				EditorApplication.delayCall += Delay;
			}
		}

		private static void Delay () {
			if(AnyFilesInStackCompiling()) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
			foreach (var compilingFile in InkLibrary.Instance.compilingFiles) {
				PostCompile(compilingFile.Value);
				if(!compilingFile.Value.inkFile.hasErrors) {
					string localJSONAssetPath = compilingFile.Value.jsonAbsoluteFilePath.Substring (Application.dataPath.Length - 6);
					AssetDatabase.ImportAsset (localJSONAssetPath);
					//				TextAsset jsonTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (localJSONAssetPath);
				}
			}
			InkLibrary.Refresh();
			foreach (var compilingFile in InkLibrary.Instance.compilingFiles) {
				if(!compilingFile.Value.inkFile.hasErrors) {
					if (OnCompileInk != null) {
						OnCompileInk (compilingFile.Value.inkFile);
					}
				}
			}
			InkLibrary.Instance.compilingFiles.Clear();

//			for (int i = 0; i < inkJSONAssetsToLoad.Count; i++) {
//				string localJSONAssetPath = inkJSONAssetsToLoad [i].Value.Substring (Application.dataPath.Length - 6);
//				AssetDatabase.ImportAsset (localJSONAssetPath);
//				TextAsset jsonTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (localJSONAssetPath);
//				// Failed to get this working.
////				string localInkAssetPath = inkJSONAssetsToLoad [i].Key.Substring (Application.dataPath.Length - 6);
////				DefaultAsset inkAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset> (localInkAssetPath);
////				AssetDatabase.AddObjectToAsset(jsonTextAsset, inkAsset);
//				InkLibrary.Refresh();
//				if (OnCompileInk != null) {
//					OnCompileInk (inkJSONAssetsToLoad [i].Key, jsonTextAsset);
//				}
//			}
//			inkJSONAssetsToLoad.Clear();
			EditorUtility.ClearProgressBar();
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				Debug.LogWarning("Ink just finished recompiling while in play mode. Your runtime story may not be up to date.");
			}
		}

		private static void PostCompile (PendingInkFileProperties pendingFile) {
			pendingFile.inkFile.errors.Clear();
			pendingFile.inkFile.warnings.Clear();
			pendingFile.inkFile.todos.Clear();

			string[] splitOutput = pendingFile.output.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
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
					
					string pathAndLineNumberString = "\n"+pendingFile.inkFile.filePath+"("+lineNo+")";

					if(errorType == "ERROR") {

						pendingFile.inkFile.errors.Add(new InkFile.InkFileLog(pendingFile.inkFile.inkAsset, message, lineNo));
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString);
						foundError = true;
					} else if (errorType == "WARNING") {
						pendingFile.inkFile.warnings.Add(new InkFile.InkFileLog(pendingFile.inkFile.inkAsset, message, lineNo));
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString);
					} else if (errorType == "TODO") {
						pendingFile.inkFile.todos.Add(new InkFile.InkFileLog(pendingFile.inkFile.inkAsset, message, lineNo));
						Debug.Log("INK "+errorType+": "+message + pathAndLineNumberString);
					}
				}
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
				return Path.GetFullPath(inklecateDirectories[0]);
			}
		}

		static Regex _errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");
	}
}