using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public static class InkCompiler {
		
		public static bool compiling {
			get {
				return InkLibrary.Instance.compilationStack.Count > 0;
			}
		}
		static bool playModeBlocked = false;

		public delegate void OnCompileInkEvent (InkFile inkFile);
		public static event OnCompileInkEvent OnCompileInk;

		[Serializable]
		public class CompilationStackItem {
			public enum State {
				Idle,
				Compiling,
				Importing
			}

			public Process process;
			public State state = State.Idle;
			public InkFile inkFile;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public List<string> output = new List<string>();
			public List<string> errorOutput = new List<string>();
			public DateTime startTime;
			public float timeTaken;

			public CompilationStackItem () {
				startTime = DateTime.Now;
			}
		}

		static InkCompiler () {
			#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlayModeChange;
			#else
			EditorApplication.playmodeStateChanged += LegacyOnPlayModeChange;
			#endif
			EditorApplication.update += Update;
		}

		private static void Update () {
			if(!InkLibrary.created) 
				return;

			// When all files have compiled, run the complete function.
			if(compiling && InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count == 0) {
				DelayedComplete();
			}

			for (int i = InkLibrary.Instance.compilationStack.Count - 1; i >= 0; i--) {
				var compilingFile = InkLibrary.Instance.compilationStack [i];
				if (compilingFile.state == CompilationStackItem.State.Compiling) {
					compilingFile.timeTaken = (float)((DateTime.Now - compilingFile.startTime).TotalSeconds);
					if (compilingFile.timeTaken > InkSettings.Instance.compileTimeout) {
						if (compilingFile.process != null) {	
							compilingFile.process.Exited -= OnCompileProcessComplete;
							compilingFile.process.Kill ();
						}
						RemoveCompilingFile(i);
						Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+".\nCompilation should never take more than a few seconds, but for large projects or slow computers you may want to increase the timeout time in the InkSettings file.\nIf this persists there may be another issue; or else check an ink file exists at this path and try Assets/Recompile Ink, else please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+string.Join("\n",compilingFile.errorOutput.ToArray()));
					}
				} else if (compilingFile.state == CompilationStackItem.State.Importing) {
					// This covers a rare bug that I've not pinned down. It seems to happen when importing new assets.
					// DOES THIS STILL OCCUR? I FIXED A BUG.
					var timeTaken = (float)((DateTime.Now - compilingFile.startTime).TotalSeconds);
					if (timeTaken > InkSettings.Instance.compileTimeout + 2) {
						if (compilingFile.process != null && !compilingFile.process.HasExited) {
							compilingFile.process.Exited -= OnCompileProcessComplete;
							compilingFile.process.Kill ();
						}
						// Can remove this if it never fires
						Debug.Assert(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count != 0);
						RemoveCompilingFile(i);
						Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+" while the file was importing.\nPlease report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+compilingFile.errorOutput);
					}
				}
			}

			// If we're not showing a progress bar in Linux this whole step is superfluous
			#if !UNITY_EDITOR_LINUX
			UpdateProgressBar();
			#endif
		}

		static void RemoveCompilingFile (int index) {
			InkLibrary.Instance.compilationStack.RemoveAt(index);
			InkLibrary.Save();
			// Progress bar prevents delayCall callback from firing in Linux Editor, locking the
			// compilation until it times out. Let's just not show progress bars in Linux Editor	
			#if !UNITY_EDITOR_LINUX
			if (InkLibrary.Instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
			#endif
		}

		static void UpdateProgressBar () {
			if(InkLibrary.Instance.compilationStack.Count == 0) return;
			int numCompiling = InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count;
			string message = "Compiling .Ink File "+(InkLibrary.Instance.compilationStack.Count-numCompiling)+" of "+InkLibrary.Instance.compilationStack.Count+".";
			if(playModeBlocked) message += " Will enter play mode when complete.";
			if(playModeBlocked) EditorUtility.DisplayProgressBar("Compiling Ink...", message, GetEstimatedCompilationProgress());
			else EditorUtility.ClearProgressBar();
		}

		public static float GetEstimatedCompilationProgress () {
			if(!compiling) return 1;
			float progress = 0;
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				if (compilingFile.state == CompilationStackItem.State.Compiling)
					progress += compilingFile.timeTaken / InkSettings.Instance.compileTimeout;
				if (compilingFile.state == CompilationStackItem.State.Importing)
					progress += 1;
			}
			progress /= InkLibrary.Instance.compilationStack.Count;
			return progress;
		}

		#if UNITY_2017_1_OR_NEWER
		static void OnPlayModeChange (PlayModeStateChange mode) {
			if(mode == PlayModeStateChange.EnteredEditMode && InkLibrary.Instance.pendingCompilationStack.Count > 0)
				CompilePendingFiles();
			if(mode == PlayModeStateChange.ExitingEditMode && compiling)
				BlockPlayMode();
			if(mode == PlayModeStateChange.EnteredPlayMode && compiling)
				EnteredPlayModeWhenCompiling();
		}
		
		#else
		
		static void LegacyOnPlayModeChange () {
			if(!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && InkLibrary.Instance.pendingCompilationStack.Count > 0) 
				CompilePendingFiles();
			if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying && compiling)
				BlockPlayMode();
			if(EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && compiling)
				EnteredPlayModeWhenCompiling();
		}
		#endif

		static void CompilePendingFiles () {
			InkLibrary.CreateOrReadUpdatedInkFiles (InkLibrary.Instance.pendingCompilationStack);
			foreach (var pendingFile in GetUniqueMasterInkFilesToCompile(InkLibrary.Instance.pendingCompilationStack))
				InkCompiler.CompileInk(pendingFile);
			// Files are removed when they're compiled, but we clear the list now just in case.
			InkLibrary.Instance.pendingCompilationStack.Clear();
		}

		static void BlockPlayMode () {
			EditorApplication.isPlaying = false;
			var percentage = String.Format("{0:P0}.", GetEstimatedCompilationProgress());
			Debug.LogWarning("Delayed entering play mode because Ink is still compiling ("+percentage+"). Will enter play mode on completion.");
			playModeBlocked = true;
		}

		static void EnteredPlayModeWhenCompiling () {
			Debug.LogError("Entered Play Mode while Ink was still compiling. Story will not be up to date. This should never happen. Recommend exiting and re-entering play mode.");
		}

		public static void CompileInk (params InkFile[] inkFiles) {
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var inkFile in inkFiles) filesCompiledLog.AppendLine().Append(inkFile.filePath);
			
			StringBuilder outputLog = new StringBuilder ();
			outputLog.Append ("Ink compilation started at ");
			outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
			outputLog.Append (filesCompiledLog.ToString());
			Debug.Log(outputLog);
			
			foreach(var inkFile in inkFiles) {
				CompileInkInternal (inkFile);
				InkLibrary.Instance.pendingCompilationStack.Remove(inkFile.filePath);
			}
		}

		/// <summary>
		/// Starts a System.Process that compiles a master ink file, creating a playable JSON file that can be parsed by the Ink.Story class
		/// </summary>
		/// <param name="inkFile">Ink file.</param>
		internal static void CompileInkInternal (InkFile inkFile) {
			if(inkFile == null) {
				Debug.LogError("Tried to compile ink file, but input was null. Check Ink Library for empty input lines");
				return;
			}
			if(!inkFile.metaInfo.isMaster)
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
			if(Application.platform == RuntimePlatform.OSXEditor) {
				SetInklecateFilePermissions(inklecatePath);
			}
			if(inklecatePath.Contains("'")){
				Debug.LogError("Due to a Unity bug, Inklecate path cannot contain an apostrophe. Ink will not compile until this is resolved. Path is '"+inklecatePath+"'");
				return;
			}
			// This hasn't been affecting us lately. Left it in so we can easily restore it in case of future bugs.
			/* else if(inklecatePath.Contains(" ")){
				Debug.LogWarning("Inklecate path should not contain a space. This might lead to compilation failing. Path is '"+inklecatePath+"'. If you don't see any compilation errors, you can ignore this warning.");
			}*/
			string inputPath = InkEditorUtils.CombinePaths(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));
			Debug.Assert(inkFile.absoluteFilePath == inputPath);
			string outputPath = inkFile.absoluteJSONPath;
			string inkArguments = InkSettings.Instance.customInklecateOptions.additionalCompilerOptions + " -c -o " + "\"" + outputPath + "\" \"" + inputPath + "\"";

			CompilationStackItem pendingFile = new CompilationStackItem();
			pendingFile.inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath);
			pendingFile.inkAbsoluteFilePath = inputPath;
			pendingFile.jsonAbsoluteFilePath = outputPath;
			pendingFile.state = CompilationStackItem.State.Compiling;
			InkLibrary.Instance.compilationStack.Add(pendingFile);
			InkLibrary.Save();

			Process process = new Process();
			if( InkSettings.Instance.customInklecateOptions.runInklecateWithMono && Application.platform != RuntimePlatform.WindowsEditor ) {
				foreach (var path in InkSettings.Instance.customInklecateOptions.monoPaths) {
					if (File.Exists(path)) {
						process.StartInfo.FileName = path;
					}
				}
				if (process.StartInfo.FileName == null) {
					Debug.LogError("Mono was not found on machine, please edit the mono paths in settings to include a valid one for your machine.");
					return;
				}
				process.StartInfo.Arguments = inklecatePath + " " + inkArguments;
			} else {
				process.StartInfo.FileName = inklecatePath;
				process.StartInfo.Arguments = inkArguments;
			}

			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += OnProcessOutput;
			// For some reason having this line enabled spams the output and error streams with null and "???" (only on OSX?)
			// Rather than removing unhandled error detection I thought it'd be best to just catch those messages and ignore them instead.
			process.ErrorDataReceived += OnProcessError;
			process.Exited += OnCompileProcessComplete;
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			pendingFile.process = process;
			// If you'd like to run this command outside of unity, you could instead run process.StartInfo.Arguments in the command line.
		}

		static void OnProcessOutput (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			ProcessOutput(process, e.Data);
		}

		static void OnProcessError (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			ProcessError(process, e.Data);
		}

		static void OnCompileProcessComplete(object sender, System.EventArgs e) {
			Process process = (Process)sender;
			CompilationStackItem pendingFile = InkLibrary.GetCompilationStackItem(process);
			pendingFile.state = CompilationStackItem.State.Importing;
		}

		private static void ProcessOutput (Process process, string message) {
			if (message == null || message.Length == 0 || message == "???")
				return;
			CompilationStackItem compilingFile = InkLibrary.GetCompilationStackItem(process);
			compilingFile.output.Add(message);
		}

		private static void ProcessError (Process process, string message) {
			message = message.Trim(new char[]{'\uFEFF','\u200B'});
			if (InkEditorUtils.IsNullOrWhiteSpace(message) || message == "???")
				return;
			Debug.Log(message[0]);
			Debug.Log(char.IsWhiteSpace(message[0]));
			Debug.Log((int)(message[0]));
			CompilationStackItem compilingFile = InkLibrary.GetCompilationStackItem(process);
			compilingFile.errorOutput.Add(message);
		}

		// When all files in stack have been compiled. This is called via update because Process events run in another thread.
		private static void DelayedComplete () {
			if(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count > 0) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
			float longestTimeTaken = 0;
			bool errorsFound = false;
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				longestTimeTaken = Mathf.Max (compilingFile.timeTaken);
				filesCompiledLog.AppendLine().Append(compilingFile.inkFile.filePath);
				if(compilingFile.errorOutput.Count > 0) {
					filesCompiledLog.Append(" (With unhandled error)");
					StringBuilder errorLog = new StringBuilder ();
					errorLog.Append ("Unhandled error(s) occurred compiling Ink file ");
					errorLog.Append ("'");
					errorLog.Append (compilingFile.inkFile.filePath);
					errorLog.Append ("'");
					errorLog.AppendLine ("! Please report following error(s) as a bug:");
					foreach (var error in compilingFile.errorOutput)
						errorLog.AppendLine (error);
					Debug.LogError(errorLog);
					compilingFile.inkFile.metaInfo.compileErrors = compilingFile.errorOutput;
					errorsFound = true;
				} else {
					SetOutputLog(compilingFile);
					bool errorsInEntireStory = false;
					bool warningsInEntireStory = false;
					foreach(var inkFile in compilingFile.inkFile.metaInfo.inkFilesInIncludeHierarchy) {
						if(inkFile.metaInfo.hasErrors) {
							errorsInEntireStory = true;
						}
						if(inkFile.metaInfo.hasWarnings) {
							warningsInEntireStory = true;
						}
					}
					if(errorsInEntireStory) {
						filesCompiledLog.Append(" (With error)");
						errorsFound = true;
					} else {
						string localJSONAssetPath = InkEditorUtils.AbsoluteToUnityRelativePath(compilingFile.jsonAbsoluteFilePath);
						AssetDatabase.ImportAsset (localJSONAssetPath);
						compilingFile.inkFile.jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (localJSONAssetPath);
					}
					if(warningsInEntireStory) {
						filesCompiledLog.Append(" (With warning)");
					}
				}
			}

			if (longestTimeTaken > InkSettings.Instance.compileTimeout * 0.6f)
				Debug.LogWarning ("Compilation took over 60% of the time required to timeout the compiler. Consider increasing the compile timeout on the InkSettings file.");

			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				if (OnCompileInk != null) {
					OnCompileInk (compilingFile.inkFile);
				}
			}

			StringBuilder outputLog = new StringBuilder ();
			if(errorsFound) {
				outputLog.Append ("Ink compilation completed with errors at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.LogWarning(outputLog);
			} else {
				outputLog.Append ("Ink compilation completed at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.Log(outputLog);
			}

			InkLibrary.Instance.compilationStack.Clear();
			InkLibrary.Save();
			InkMetaLibrary.Save();

			#if !UNITY_EDITOR_LINUX
			EditorUtility.ClearProgressBar();
			#endif
			if(EditorApplication.isPlayingOrWillChangePlaymode && InkSettings.Instance.delayInPlayMode) {
				Debug.LogError("Ink just finished recompiling while in play mode. This should never happen when InkSettings.Instance.delayInPlayMode is true!");
			}

			if(playModeBlocked) {
				if(!errorsFound) {
					// Delaying gives the editor a frame to clear the progress bar.
					EditorApplication.delayCall += () => {
						Debug.Log("Compilation completed, entering play mode.");
						EditorApplication.isPlaying = true;
					};
				} else {
					Debug.LogWarning("Play mode not entered after ink compilation because ink had errors.");
				}
			}
		}

		private static void SetOutputLog (CompilationStackItem pendingFile) {
			pendingFile.inkFile.metaInfo.errors.Clear();
			pendingFile.inkFile.metaInfo.warnings.Clear();
			pendingFile.inkFile.metaInfo.todos.Clear();

			foreach(var childInkFile in pendingFile.inkFile.metaInfo.inkFilesInIncludeHierarchy) {
				childInkFile.metaInfo.compileErrors.Clear();
				childInkFile.metaInfo.errors.Clear();
				childInkFile.metaInfo.warnings.Clear();
				childInkFile.metaInfo.todos.Clear();
			}

			foreach(string output in pendingFile.output) {
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
					
					
					string logFilePath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(pendingFile.inkFile.filePath), filename);
					InkFile inkFile = InkLibrary.GetInkFileWithPath(logFilePath);
					if(inkFile == null)
						inkFile = pendingFile.inkFile;
					
					string pathAndLineNumberString = "\n"+inkFile.filePath+":"+lineNo;
					if(errorType == "ERROR") {
						inkFile.metaInfo.errors.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString, inkFile.inkAsset);
					} else if (errorType == "WARNING") {
						inkFile.metaInfo.warnings.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString, inkFile.inkAsset);
					} else if (errorType == "TODO") {
						inkFile.metaInfo.todos.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.Log("INK "+errorType+": "+message + pathAndLineNumberString, inkFile.inkAsset);
					}
				}
			}
		}

		private static Regex _errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");

		// The asset store version of this plugin removes execute permissions. We can't run unless they're restored.
		private static void SetInklecateFilePermissions (string inklecatePath) {
			Process process = new Process();
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(inklecatePath);
			process.StartInfo.FileName = "chmod";
			process.StartInfo.Arguments = "+x "+ Path.GetFileName(inklecatePath);
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.Start();
			process.WaitForExit();
		}

		public static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				// Trying to catch a rare (and not especially important) bug that seems to happen occasionally when opening a project
				// It's probably this - I've noticed it before in another context.
				Debug.Assert(InkSettings.Instance != null, "No ink settings file. This is a bug. For now you should be able to fix this via Assets > Rebuild Ink Library");
				// I've caught it here before
				Debug.Assert(inkFile != null, "No internal InkFile reference at path "+importedAssetPath+". This is a bug. For now you can fix this via Assets > Rebuild Ink Library");
				Debug.Assert(inkFile.metaInfo != null);
				Debug.Assert(inkFile.metaInfo.masterInkFileIncludingSelf != null);
				if (!masterInkFiles.Contains(inkFile.metaInfo.masterInkFileIncludingSelf) && (InkSettings.Instance.compileAutomatically || inkFile.metaInfo.masterInkFileIncludingSelf.compileAutomatically)) {
					masterInkFiles.Add(inkFile.metaInfo.masterInkFileIncludingSelf);
				}
			}
			return masterInkFiles;
		}
	}
}
