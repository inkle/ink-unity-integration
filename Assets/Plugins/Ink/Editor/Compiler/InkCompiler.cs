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
		private const float timeout = 10;

		public static bool compiling {
			get {
				return InkLibrary.Instance.compilationStack.Count > 0;
			}
		}

		public delegate void OnCompileInkEvent (InkFile inkFile);
		public static event OnCompileInkEvent OnCompileInk;

		[Serializable]
		public class CompilationStackItem {
			public enum State {
				Idle,
				Compiling,
				Importing
			}

			public State state = State.Idle;
			public InkFile inkFile;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public string output;
			public float startTime;

			public CompilationStackItem () {
				startTime = (float)EditorApplication.timeSinceStartup;
			}
		}

		static InkCompiler () {
			EditorApplication.playmodeStateChanged += OnPlayModeChange;
			EditorApplication.update += Update;
		}

		private static void Update () {
			if(!InkLibrary.created) 
				return;

			for (int i = InkLibrary.Instance.compilationStack.Count - 1; i >= 0; i--) {
				var compilingFile = InkLibrary.Instance.compilationStack [i];
				if (EditorApplication.timeSinceStartup-compilingFile.startTime > timeout) {
					Debug.LogError("Ink Compiler timed out for "+InkLibrary.Instance.compilationStack[i]);
					InkLibrary.Instance.compilationStack.RemoveAt(i);
					EditorUtility.ClearProgressBar();
				}
			}
			if(InkLibrary.Instance.compilationStack.Count > 0) {
				int numCompiling = InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count;
				string message = "Compiling .Ink File "+(InkLibrary.Instance.compilationStack.Count-numCompiling)+" of "+InkLibrary.Instance.compilationStack.Count;
				EditorUtility.DisplayProgressBar("Compiling Ink...", message, (InkLibrary.Instance.compilationStack.Count-numCompiling)/InkLibrary.Instance.compilationStack.Count);
			}
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
				Debug.LogError("Tried to compile ink file "+inkFile.filePath+", but input was null.");
				return;
			} else if(inkFile.circularIncludeReferences.Count > 0) {
				Debug.LogError("Tried to compile ink file "+inkFile.filePath+", but file is part of a circular include reference. This must be resolved before compiling.");
				return;
			}
			if(!inkFile.isMaster)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");
			if(InkLibrary.GetCompilationStackItem(inkFile) != null) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inkFile.filePath);
				return;
			}

			string inklecatePath = InkEditorUtils.GetInklecateFilePath();
			if(inklecatePath == null) {
				UnityEngine.Debug.LogWarning("Inklecate (the ink compiler) not found in assets. This will prevent automatic building of JSON TextAsset files from ink story files.");
				return;
			}

			string inputPath = Path.Combine(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));

			string outputPath = Path.Combine(inkFile.absoluteFolderPath, Path.GetFileNameWithoutExtension(Path.GetFileName(inkFile.filePath)))+".json";
			string inkArguments = "-c -o "+"\""+outputPath +"\" \""+inputPath+"\"";

			CompilationStackItem pendingFile = new CompilationStackItem();
			pendingFile.inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath);
			pendingFile.inkAbsoluteFilePath = inputPath;
			pendingFile.jsonAbsoluteFilePath = outputPath;
			pendingFile.state = CompilationStackItem.State.Compiling;
			InkLibrary.Instance.compilationStack.Add(pendingFile);

			Process process = new Process();
			process.StartInfo.WorkingDirectory = inkFile.absoluteFolderPath;
			process.StartInfo.FileName = inklecatePath;
			process.StartInfo.Arguments = inkArguments;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"] = inputPath;
			process.Exited += OnCompileProcessComplete;
			process.ErrorDataReceived += OnProcessError;
			process.Start();

		}

		static void OnProcessError (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			Debug.LogError("Fatal Error compiling Ink! Ink failed to process. Please report this as a bug.");
			InkFile inkFile = InkLibrary.GetInkFileWithAbsolutePath(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			Debug.LogError(inkFile);
			CompilationStackItem compilingFile = InkLibrary.GetCompilationStackItem(inkFile);
			InkLibrary.Instance.compilationStack.Remove(compilingFile);
		}

		static void OnCompileProcessComplete(object sender, System.EventArgs e) {
			Process process = (Process)sender;

			CompilationStackItem pendingFile = InkLibrary.GetCompilationStackItem(process.StartInfo.EnvironmentVariables["inkAbsoluteFilePath"]);
			pendingFile.state = CompilationStackItem.State.Importing;
			pendingFile.output = process.StandardOutput.ReadToEnd();

			if(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count == 0) {
				// This event runs in another thread, preventing us from calling some UnityEditor functions directly. Instead, we delay till the next inspector update.
				EditorApplication.delayCall += Delay;
			}
		}

		private static void Delay () {
			if(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count > 0) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				SetOutputLog(compilingFile);
				if(!compilingFile.inkFile.hasErrors) {
					string localJSONAssetPath = compilingFile.jsonAbsoluteFilePath.Substring (Application.dataPath.Length - 6);
					AssetDatabase.ImportAsset (localJSONAssetPath);
					compilingFile.inkFile.jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (localJSONAssetPath);
				}
			}
//			InkLibrary.Refresh();
			InkLibrary.Instance.compilationStack.Clear();

			EditorUtility.ClearProgressBar();
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				Debug.LogWarning("Ink just finished recompiling while in play mode. Your runtime story may not be up to date.");
			}
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				if (OnCompileInk != null) {
					OnCompileInk (compilingFile.inkFile);
				}
			}
		}

		private static void SetOutputLog (CompilationStackItem pendingFile) {
			pendingFile.inkFile.errors.Clear();
			pendingFile.inkFile.warnings.Clear();
			pendingFile.inkFile.todos.Clear();
			foreach(var child in pendingFile.inkFile.includes) {
				InkFile childInkFile = InkLibrary.GetInkFileWithFile((DefaultAsset)child);
				childInkFile.errors.Clear();
				childInkFile.warnings.Clear();
				childInkFile.todos.Clear();
			}

			string[] splitOutput = pendingFile.output.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
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
					

					string logFilePath = Path.Combine(Path.GetDirectoryName(pendingFile.inkFile.filePath), filename);
					InkFile inkFile = InkLibrary.GetInkFileWithPath(logFilePath);
					if(inkFile == null)
						inkFile = pendingFile.inkFile;

					string pathAndLineNumberString = "\n"+inkFile.filePath+"("+lineNo+")";
					if(errorType == "ERROR") {
						inkFile.errors.Add(new InkFile.InkFileLog(message, lineNo));
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString);
					} else if (errorType == "WARNING") {
						inkFile.warnings.Add(new InkFile.InkFileLog(message, lineNo));
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString);
					} else if (errorType == "TODO") {
						inkFile.todos.Add(new InkFile.InkFileLog(message, lineNo));
						Debug.Log("INK "+errorType+": "+message + pathAndLineNumberString);
					}
				}
			}
		}

		private static Regex _errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");
	}
}