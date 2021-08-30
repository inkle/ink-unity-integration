﻿#pragma warning disable IDE1006

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditorInternal;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	#if UNITY_2020_1_OR_NEWER
    [FilePath("Library/InkCompiler.asset", FilePathAttribute.Location.ProjectFolder)]
	public class InkCompiler : ScriptableSingleton<InkCompiler> {
    #else
	public class InkCompiler : ScriptableObject {
    #endif
    
        #if !UNITY_2020_1_OR_NEWER
		public static bool created {
			get {
				return (_instance != (UnityEngine.Object) null);
			}
		}
		private static InkCompiler _instance;
		public static InkCompiler instance {
			get {
				if(!created)
                	LoadOrCreateInstance();
				return _instance;
			} private set {
				if(_instance == value) return;
				_instance = value;
            }
		}
        
		static string absoluteSavePath {
			get {
				return System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Library","InkCompiler.asset"));
			}
		}
		public static void LoadOrCreateInstance () {
			InternalEditorUtility.LoadSerializedFileAndForget(absoluteSavePath);
			if(created) {
				if(InkEditorUtils.isFirstCompile) {
					ClearCompilationStacks();
				}
			} else {
				instance = ScriptableObject.CreateInstance<InkCompiler>();
				instance.hideFlags = HideFlags.HideAndDontSave;
			}
		}
		public void Save (bool saveAsText) {
			InternalEditorUtility.SaveToSerializedFileAndForget((UnityEngine.Object[]) new InkCompiler[1] {this}, absoluteSavePath, saveAsText);
		}

		protected InkCompiler () {
			if (created)
				Debug.LogError((object) "ScriptableSingleton already exists. Did you query the singleton in a constructor?");
			else {
				instance = this;
			}
		}
        #endif
		
        
        public class AssetSaver : UnityEditor.AssetModificationProcessor {
#pragma warning disable IDE0051 // Remove unused private members
            static string[] OnWillSaveAssets(string[] paths) {
#pragma warning restore IDE0051 // Remove unused private members
                InkCompiler.instance.Save(true);
                return paths;
            }
        }
		public static void SaveStatic (bool saveAsText) {
			#if !UNITY_2020_1_OR_NEWER
            if(!created) return;
			#endif
			instance.Save(saveAsText);
		}

		public static bool compiling {
			get {
				return instance.compilationStack.Count > 0;
			}
		}
		public static bool buildBlocked = false;
		static bool playModeBlocked = false;

		public delegate void OnCompileInkEvent (InkFile inkFile);
		public static event OnCompileInkEvent OnCompileInk;

		// Track if we've currently locked compilation of Unity C# Scripts
		public static bool hasLockedUnityCompilation = false;
        
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0090 // Use 'new(...)'
        private static List<Action> onCompleteActions = new List<Action>();
#pragma warning restore IDE0090 // Use 'new(...)'
#pragma warning restore IDE0044 // Add readonly modifier

		
		// If InkSettings' delayInPlayMode option is true, dirty files are added here when they're changed in play mode
		// This ensures they're remembered when you exit play mode and can be compiled
#pragma warning disable IDE0090 // Use 'new(...)'
		public List<string> pendingCompilationStack = new List<string>();
#pragma warning restore IDE0090 // Use 'new(...)'
		// The state of files currently being compiled.
#pragma warning disable IDE0090 // Use 'new(...)'
		public List<InkCompiler.CompilationStackItem> compilationStack = new List<InkCompiler.CompilationStackItem>();
#pragma warning restore IDE0090 // Use 'new(...)'


		[Serializable]
		public class CompilationStackItem {
			public enum State {
				// Default state, item is about to be queued for compilation
				Queued,
				
				// Item is now owned by the thread pool and being compiled
				Compiling,
				
				// Compilation has finished, item to be processed for errors and result handled
				Complete,
			}

			public State state = State.Queued;
			public bool immediate;
			public InkFile inkFile;
			public string compiledJson;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
#pragma warning disable IDE0090 // Use 'new(...)'
			public List<InkCompilerLog> logOutput = new List<InkCompilerLog>();
#pragma warning restore IDE0090 // Use 'new(...)'
#pragma warning disable IDE0090 // Use 'new(...)'
			public List<string> unhandledErrorOutput = new List<string>();
#pragma warning restore IDE0090 // Use 'new(...)'
			public DateTime startTime;
			public DateTime endTime;

			public float timeTaken {
				get {
					if(state == State.Complete) return (float)(endTime - startTime).TotalSeconds;
					else return (float)(DateTime.Now - startTime).TotalSeconds;
				}
			}

			public CompilationStackItem () {}
		}

		// This always runs after the InkEditorUtils constructor
		[InitializeOnLoadMethod]
#pragma warning disable IDE0051 // Remove unused private members
		static void OnProjectLoadedInEditor() {
#pragma warning restore IDE0051 // Remove unused private members
			#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlayModeChange;
			#else
			EditorApplication.playmodeStateChanged += LegacyOnPlayModeChange;
			#endif
			EditorApplication.update += Update;
            // I really don't know if this can fire, since it assumes that it compiled so can't have been locked. But safety first!
            EditorApplication.UnlockReloadAssemblies();
			#if UNITY_2019_4_OR_NEWER
			// This one, on the other hand, seems to actually occur sometimes - presumably because c# compiles at the same time as the ink.
			if(InkEditorUtils.disallowedAutoRefresh) {
				InkEditorUtils.disallowedAutoRefresh = false;
				try {
					AssetDatabase.AllowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed AllowAutoRefresh "+e);
				}
			}
			#endif
		}
		
		private static void Update () {
            #if UNITY_2020_1_OR_NEWER
            // If we're not compiling but have locked C# compilation then now is the time to reset
			if (!compiling && hasLockedUnityCompilation) {
				hasLockedUnityCompilation = false;
				EditorApplication.UnlockReloadAssemblies();
			}
			#else
            // If we're not compiling but have locked C# compilation then now is the time to reset
			if ((!InkLibrary.created || !compiling) && hasLockedUnityCompilation) {
				hasLockedUnityCompilation = false;
				EditorApplication.UnlockReloadAssemblies();
			}
			if(!InkLibrary.created) 
				return;
            #endif

			if(compiling) {
				// Check for timeouts, in case of an unhandled bug with this system/the ink compiler!
				for (int i = instance.compilationStack.Count - 1; i >= 0; i--) {
					var compilingFile = instance.compilationStack [i];
					if (compilingFile.state == CompilationStackItem.State.Compiling) {
						if (compilingFile.timeTaken > InkSettings.instance.compileTimeout) {
							// TODO - Cancel the thread if it's still going. Not critical, since its kinda fine if it compiles a bit later, but it's not clear.
							RemoveCompilingFile(i);
							Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+".\nCompilation should never take more than a few seconds, but for large projects or slow computers you may want to increase the timeout time in the InkSettings file.\nIf this persists there may be another issue; or else check an ink file exists at this path and try Assets/Recompile Ink, else please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+string.Join("\n",compilingFile.unhandledErrorOutput.ToArray()));
							TryCompileNextFileInStack();
						}
					}
				}

				// When all files have compiled, run the complete function.
				if(NumFilesInCompilingStackInState(CompilationStackItem.State.Compiling) == 0) {
					if(NumFilesInCompilingStackInState(CompilationStackItem.State.Queued) == 0) {
						DelayedComplete();
					} else {
						// We used to avoid calling this here in favour of calling it CompileInkThreaded but it seems that it doesn't run when called there, for some reason.
						// If someone can make this work please let me know!
						TryCompileNextFileInStack();
					}
				}
			}


			// If we're not showing a progress bar in Linux this whole step is superfluous
			#if !UNITY_EDITOR_LINUX
			UpdateProgressBar();
			#endif
		}

        static void RemoveCompilingFile (int index) {
            instance.compilationStack.RemoveAt(index);
            instance.Save(true);
            // Progress bar prevents delayCall callback from firing in Linux Editor, locking the
            // compilation until it times out. Let's just not show progress bars in Linux Editor    
            #if !UNITY_EDITOR_LINUX
            if (instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
            #endif
        }

		static void UpdateProgressBar () {
			if(instance.compilationStack.Count == 0) return;
			int numCompiling = NumFilesInCompilingStackInState(CompilationStackItem.State.Compiling);
			string message = "Compiling .Ink File "+(instance.compilationStack.Count-numCompiling)+" of "+instance.compilationStack.Count+".";
			if(playModeBlocked) message += " Will enter play mode when complete.";
			if(buildBlocked || playModeBlocked || EditorApplication.isPlaying) EditorUtility.DisplayProgressBar("Compiling Ink...", message, GetEstimatedCompilationProgress());
			else EditorUtility.ClearProgressBar();
		}

		public static float GetEstimatedCompilationProgress () {
			if(!compiling) return 1;
			float progress = 0;
			foreach (var compilingFile in instance.compilationStack) {
				if (compilingFile.state == CompilationStackItem.State.Compiling)
					progress += compilingFile.timeTaken / InkSettings.instance.compileTimeout;
				if (compilingFile.state == CompilationStackItem.State.Complete)
					progress += 1;
			}
			progress /= instance.compilationStack.Count;
			return progress;
		}

		#if UNITY_2017_1_OR_NEWER
		static void OnPlayModeChange (PlayModeStateChange mode) {
			if(mode == PlayModeStateChange.EnteredEditMode && instance.pendingCompilationStack.Count > 0)
				CompilePendingFiles();
			if(mode == PlayModeStateChange.ExitingEditMode && compiling)
				BlockPlayMode();
			if(mode == PlayModeStateChange.EnteredPlayMode && compiling)
				EnteredPlayModeWhenCompiling();
		}
		#else
		static void LegacyOnPlayModeChange () {
			if(!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && instance.pendingCompilationStack.Count > 0) 
				CompilePendingFiles();
			if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying && compiling)
				BlockPlayMode();
			if(EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && compiling)
				EnteredPlayModeWhenCompiling();
		}
		#endif

		static void CompilePendingFiles () {
			InkLibrary.CreateOrReadUpdatedInkFiles (instance.pendingCompilationStack);
			foreach (var pendingMasterFile in GetUniqueMasterInkFilesToCompile(instance.pendingCompilationStack))
				InkCompiler.CompileInk(pendingMasterFile);
		}

		static void BlockPlayMode () {
			EditorApplication.isPlaying = false;
			var percentage = String.Format("{0:P0}.", GetEstimatedCompilationProgress());
			Debug.LogWarning("Delayed entering play mode because Ink is still compiling ("+percentage+"). Will enter play mode on completion.");
			playModeBlocked = true;
		}

		static void EnteredPlayModeWhenCompiling () {
			Debug.LogError("Entered Play Mode while Ink was still compiling! Your story will not be up to date. Recommend exiting and re-entering play mode.\nWe normally delay entering play mode when compiling, so you've found an edge case!");
		}

		public static void CompileInk (params InkFile[] inkFiles) {
            CompileInk(inkFiles, false, null);
        }
		public static void CompileInk (InkFile[] inkFiles, bool immediate, Action onComplete) {
			#if UNITY_2019_4_OR_NEWER
			if(!InkEditorUtils.disallowedAutoRefresh) {
				InkEditorUtils.disallowedAutoRefresh = true;
				try {
					AssetDatabase.DisallowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed DisallowAutoRefresh "+e);
				}
			}
			#endif
            
			InkLibrary.Validate();
            if(onComplete != null) onCompleteActions.Add(onComplete);
#pragma warning disable IDE0090 // Use 'new(...)'
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
#pragma warning restore IDE0090 // Use 'new(...)'
			foreach (var inkFile in inkFiles) filesCompiledLog.AppendLine().Append(inkFile.filePath);
			
#pragma warning disable IDE0090 // Use 'new(...)'
			StringBuilder outputLog = new StringBuilder ();
#pragma warning restore IDE0090 // Use 'new(...)'
			outputLog.Append ("Ink compilation started at ");
			outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
			outputLog.Append (filesCompiledLog.ToString());
			Debug.Log(outputLog);
			
			foreach(var inkFile in inkFiles) {
				CompileInkInternal (inkFile, immediate);
			}
		}

		/// <summary>
		/// Starts a System.Process that compiles a master ink file, creating a playable JSON file that can be parsed by the Ink.Story class
		/// </summary>
		/// <param name="inkFile">Ink file.</param>
		private static void CompileInkInternal (InkFile inkFile, bool immediate) {
			if(inkFile == null) {
				Debug.LogError("Tried to compile ink file but input was null.");
				return;
			}
			if(!inkFile.isMaster)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");

			// If we've not yet locked C# compilation do so now
			if (!hasLockedUnityCompilation)
			{
				hasLockedUnityCompilation = true;
				EditorApplication.LockReloadAssemblies();
			}
			
            RemoveFromPendingCompilationStack(inkFile);
			if(GetCompilationStackItem(inkFile) != null) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inkFile.filePath);
				return;
			}

			string inputPath = InkEditorUtils.CombinePaths(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));
			Debug.Assert(inkFile.absoluteFilePath == inputPath);

#pragma warning disable IDE0090 // Use 'new(...)'
			CompilationStackItem pendingFile = new CompilationStackItem
#pragma warning restore IDE0090 // Use 'new(...)'
			{
				inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath),
				inkAbsoluteFilePath = inputPath,
				jsonAbsoluteFilePath = inkFile.absoluteJSONPath,
				state = CompilationStackItem.State.Queued,
				immediate = immediate
			};

			AddToCompilationStack(pendingFile);

			TryCompileNextFileInStack();
		}




		private static void TryCompileNextFileInStack () {
			if(!compiling) return;
			InkCompiler.CompilationStackItem fileToCompile = null;
			foreach(var x in instance.compilationStack) {
				if(x.state == CompilationStackItem.State.Compiling) return;
				if(x.state == CompilationStackItem.State.Queued) {
					fileToCompile = x;
					break;
				}
			}
			if(fileToCompile != null) {
				if(fileToCompile.immediate) {
					CompileInkThreaded(fileToCompile);
				} else {
					if(EditorApplication.isCompiling) Debug.LogWarning("Was compiling scripts when ink compilation started! This seems to cause the thread to cancel and complete, but the work isn't done. It may cause a timeout.");
					ThreadPool.QueueUserWorkItem(CompileInkThreaded, fileToCompile);
				}
			} else {
			}
		}

		private static void BeginCompilingFile(CompilationStackItem item) {
			if(item.state != CompilationStackItem.State.Queued) return;
			item.state = CompilationStackItem.State.Compiling;
			item.startTime = DateTime.Now;
		}
		private static void CompleteCompilingFile(CompilationStackItem item) {
			if(item.state != CompilationStackItem.State.Compiling) return;
			item.state = CompilationStackItem.State.Complete;
			item.endTime = DateTime.Now;
			if (item.timeTaken > InkSettings.instance.compileTimeout * 0.6f)
				Debug.LogWarning ("Compilation for "+Path.GetFileName(item.inkFile.filePath)+" took over 60% of the time required to timeout the compiler. Consider increasing the compile timeout on the InkSettings file.");
		}

		private static void CompileInkThreaded(object itemObj) {
			CompilationStackItem item = (CompilationStackItem) itemObj;
			if(item.state == CompilationStackItem.State.Compiling) {
				Debug.LogWarning("CompileInkThreaded was called on a file that is already compiling! This is most likely a threading bug. Please report this!");
				return;
			}
			BeginCompilingFile(item);

			var inputString = File.ReadAllText(item.inkAbsoluteFilePath);
			var compiler = new Compiler(inputString, new Compiler.Options
			{
				countAllVisits = true,
				fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(item.inkAbsoluteFilePath)),
				errorHandler = (string message, ErrorType type) => {
#pragma warning disable IDE0018 // Inline variable declaration
					InkCompilerLog log;
#pragma warning restore IDE0018 // Inline variable declaration
					if(InkCompilerLog.TryParse(message, out log)) {
						if(string.IsNullOrEmpty(log.fileName)) log.fileName = Path.GetFileName(item.inkAbsoluteFilePath);
						item.logOutput.Add(log);
					} else {
						Debug.LogWarning("Couldn't parse log "+message);
					}
				}
			});

			try
			{
				var compiledStory = compiler.Compile();
				if (compiledStory != null)
					item.compiledJson = compiledStory.ToJson();
			}
			catch (SystemException e)
			{
				item.unhandledErrorOutput.Add(string.Format(
					"Ink Compiler threw exception \nError: {0}\n---- Trace ----\n{1}\n--------\n", e.Message,
					e.StackTrace));
			}

			CompleteCompilingFile(item);

			// This doesn't seem to execute when called in a thread, and I apparently don't have a bloody clue how threads work.
			// If someone can make this work, I'd rather that TryCompileNextFileInStack ran directly after CompileInkThreaded finishes.
			// I couldn't make it work, so I've put TryCompileNextFileInStack in Update instead. Bleh!
			// TryCompileNextFileInStack();
		}

		// When all files in stack have been compiled. This is called via update because Process events run in another thread.
		private static void DelayedComplete () {
			if(NumFilesInCompilingStackInState(CompilationStackItem.State.Compiling) > 0) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
            // Clone and clear the list. This is a surefire way to ensure the list is cleared in case of unhandled errors in this code.
            // A Try-catch would be better but I'm debugging blind as I write this and the nuclear option will definitely work!
#pragma warning disable IDE0090 // Use 'new(...)'
            List<CompilationStackItem> compilationStack = new List<CompilationStackItem>(instance.compilationStack);
#pragma warning restore IDE0090 // Use 'new(...)'
			ClearCompilationStack();

			bool errorsFound = false;
#pragma warning disable IDE0090 // Use 'new(...)'
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
#pragma warning restore IDE0090 // Use 'new(...)'

            // Create and import compiled files
			AssetDatabase.StartAssetEditing();
			foreach (var compilingFile in compilationStack) {
				// Complete status is also set when an error occured, in these cases 'compiledJson' will be null so there's no import to process
				if (compilingFile.compiledJson == null) continue;
				
				// Write new compiled data to the file system
				File.WriteAllText(compilingFile.jsonAbsoluteFilePath, compilingFile.compiledJson, Encoding.UTF8);
                if(compilingFile.inkFile.inkAsset != null) AssetDatabase.ImportAsset(compilingFile.inkFile.jsonPath);
			}
			AssetDatabase.StopAssetEditing();

			foreach (var compilingFile in compilationStack) {
				if(compilingFile.inkFile.inkAsset != null) {
                    // Load and store a reference to the compiled file
                    compilingFile.inkFile.FindCompiledJSONAsset();
                    
                    filesCompiledLog.AppendLine().Append(compilingFile.inkFile.filePath);
                    filesCompiledLog.Append(string.Format(" ({0}s)", compilingFile.timeTaken));
                    if(compilingFile.unhandledErrorOutput.Count > 0) {
                        filesCompiledLog.Append(" (With unhandled error)");
#pragma warning disable IDE0090 // Use 'new(...)'
                        StringBuilder errorLog = new StringBuilder ();
#pragma warning restore IDE0090 // Use 'new(...)'
                        errorLog.Append ("Unhandled error(s) occurred compiling Ink file ");
                        errorLog.Append ("'");
                        errorLog.Append (compilingFile.inkFile.filePath);
                        errorLog.Append ("'");
                        errorLog.AppendLine ("! Please report following error(s) as a bug:");
                        foreach (var error in compilingFile.unhandledErrorOutput)
                            errorLog.AppendLine (error);
                        Debug.LogError(errorLog);
                        compilingFile.inkFile.unhandledCompileErrors = compilingFile.unhandledErrorOutput;
                        errorsFound = true;
                    } else {
                        SetOutputLog(compilingFile);
                        bool errorsInEntireStory = false;
                        bool warningsInEntireStory = false;
                        foreach(var inkFile in compilingFile.inkFile.inkFilesInIncludeHierarchy) {
                            if(inkFile.hasErrors) {
                                errorsInEntireStory = true;
                            }
                            if(inkFile.hasWarnings) {
                                warningsInEntireStory = true;
                            }
                        }
                        if(errorsInEntireStory) {
                            filesCompiledLog.Append(" (With error)");
                            errorsFound = true;
                        }
                        if(warningsInEntireStory) {
                            filesCompiledLog.Append(" (With warning)");
                        }
                    }
                } else {
                    filesCompiledLog.AppendLine().Append(compilingFile.inkAbsoluteFilePath);
                    filesCompiledLog.Append(" (With post-compile missing file error)");
                    Debug.LogError("Ink file at "+compilingFile.inkAbsoluteFilePath+" was not found after compilation");
                }
			}
			

			foreach (var compilingFile in compilationStack) {
				if (OnCompileInk != null) {
#pragma warning disable IDE1005 // Delegate invocation can be simplified.
					OnCompileInk (compilingFile.inkFile);
#pragma warning restore IDE1005 // Delegate invocation can be simplified.
				}
			}

#pragma warning disable IDE0090 // Use 'new(...)'
			StringBuilder outputLog = new StringBuilder ();
#pragma warning restore IDE0090 // Use 'new(...)'
			if(errorsFound) {
				outputLog.Append ("Ink compilation completed with errors at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.LogError(outputLog);
			} else {
				outputLog.Append ("Ink compilation completed at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.Log(outputLog);
			}

			
			#if !UNITY_EDITOR_LINUX
			EditorUtility.ClearProgressBar();
			#endif
			
			#if UNITY_2019_4_OR_NEWER
			if(InkEditorUtils.disallowedAutoRefresh) {
				InkEditorUtils.disallowedAutoRefresh = false;
				try {
					AssetDatabase.AllowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed AllowAutoRefresh "+e);
				}
			}
			#endif

            // This is now allowed, if compiled manually. I've left this code commented out because at some point we might want to track what caused a file to compile. 
            // if(EditorApplication.isPlayingOrWillChangePlaymode && InkSettings.instance.delayInPlayMode) {
			// 	Debug.LogError("Ink just finished recompiling while in play mode. This should never happen when InkSettings.instance.delayInPlayMode is true!");
			// }
            
            buildBlocked = false;

			if(playModeBlocked) {
                playModeBlocked = false;
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

            foreach(var onCompleteAction in onCompleteActions) {
#pragma warning disable IDE1005 // Delegate invocation can be simplified.
                if(onCompleteAction != null) onCompleteAction();
#pragma warning restore IDE1005 // Delegate invocation can be simplified.
            }
            onCompleteActions.Clear();
		}
		
		
		private static void SetOutputLog (CompilationStackItem pendingFile) {
			pendingFile.inkFile.errors.Clear();
			pendingFile.inkFile.warnings.Clear();
			pendingFile.inkFile.todos.Clear();

			foreach(var childInkFile in pendingFile.inkFile.inkFilesInIncludeHierarchy) {
				childInkFile.unhandledCompileErrors.Clear();
				childInkFile.errors.Clear();
				childInkFile.warnings.Clear();
				childInkFile.todos.Clear();
			}

			foreach(var output in pendingFile.logOutput) {
				if(output.type == ErrorType.Error) {
					pendingFile.inkFile.errors.Add(output);
					Debug.LogError("Ink "+output.type+": "+output.content + " (at "+output.fileName+":"+output.lineNumber+")", pendingFile.inkFile.inkAsset);
				} else if (output.type == ErrorType.Warning) {
					pendingFile.inkFile.warnings.Add(output);
					Debug.LogWarning("Ink "+output.type+": "+output.content + " (at "+output.fileName+" "+output.lineNumber+")", pendingFile.inkFile.inkAsset);
				} else if (output.type == ErrorType.Author) {
					pendingFile.inkFile.todos.Add(output);
					if(InkSettings.instance.printInkLogsInConsoleOnCompile)
						Debug.Log("Ink Log: "+output.content + " (at "+output.fileName+" "+output.lineNumber+")", pendingFile.inkFile.inkAsset);
				}
			}
		}



		public static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
#pragma warning disable IDE0090 // Use 'new(...)'
			List<InkFile> masterInkFiles = new List<InkFile>();
#pragma warning restore IDE0090 // Use 'new(...)'
			foreach (var importedAssetPath in importedInkAssets) {
                foreach(var masterInkFile in GetMasterFilesIncludingInkAssetPath(importedAssetPath)) {
					if (!masterInkFiles.Contains(masterInkFile) && (InkSettings.instance.compileAutomatically || masterInkFile.compileAutomatically)) {
						masterInkFiles.Add(masterInkFile);
					}
				}
            }
			return masterInkFiles;
		}

		// An ink file might actually have several owners! This should be reflected here.
        public static IEnumerable<InkFile> GetMasterFilesIncludingInkAssetPath (string importedAssetPath) {
            InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
            // Trying to catch a rare (and not especially important) bug that seems to happen occasionally when opening a project
            // It's probably this - I've noticed it before in another context.
            Debug.Assert(InkSettings.instance != null, "No ink settings file. This is a bug. For now you should be able to fix this via Assets > Rebuild Ink Library");
            // I've caught it here before
            Debug.Assert(inkFile != null, "No internal InkFile reference at path "+importedAssetPath+". This is a bug. For now you can fix this via Assets > Rebuild Ink Library");
            Debug.Assert(inkFile != null);
            return inkFile.masterInkFilesIncludingSelf;
        }






		public static void AddToCompilationStack (InkCompiler.CompilationStackItem compilationStackItem) {
			if(!instance.compilationStack.Contains(compilationStackItem)) {
				instance.compilationStack.Add(compilationStackItem);
				instance.Save(true);
			}
		}

        public static void ClearCompilationStack () {
			if(instance.compilationStack.Count != 0) {
				instance.compilationStack.Clear();
				instance.Save(true);
			}
        }


        public static void AddToPendingCompilationStack (string filePath) {
			if(!instance.pendingCompilationStack.Contains(filePath)) {
				instance.pendingCompilationStack.Add(filePath);
				instance.Save(true);
			}
		}

        public static void RemoveFromPendingCompilationStack (InkFile inkFile) {
            bool anyChange = false;
			anyChange = instance.pendingCompilationStack.Remove(inkFile.filePath) || anyChange;
            foreach(var includeFile in inkFile.inkFilesInIncludeHierarchy) {
                anyChange = instance.pendingCompilationStack.Remove(includeFile.filePath) || anyChange;
            }
			if(anyChange)
				instance.Save(true);
        }
        public static void ClearCompilationStacks () {
            instance.compilationStack.Clear();
            instance.pendingCompilationStack.Clear();
			instance.Save(true);
        }

		public static int NumFilesInCompilingStackInState (InkCompiler.CompilationStackItem.State state) {
			int count = 0;
			foreach(var x in instance.compilationStack) {
				if(x.state == state) 
					count++;
			}
			return count;
		}
		public static List<InkCompiler.CompilationStackItem> FilesInCompilingStackInState (InkCompiler.CompilationStackItem.State state) {
#pragma warning disable IDE0090 // Use 'new(...)'
			List<InkCompiler.CompilationStackItem> items = new List<InkCompiler.CompilationStackItem>();
#pragma warning restore IDE0090 // Use 'new(...)'
			foreach(var x in instance.compilationStack) {
				if(x.state == state) 
					items.Add(x);
			}
			return items;
		}

		public static InkCompiler.CompilationStackItem GetCompilationStackItem (InkFile inkFile) {
			foreach(var x in instance.compilationStack) {
				if(x.inkFile == inkFile) 
					return x;
			}
			return null;
		}
	}
}
