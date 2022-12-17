using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	// Ink Compiler handles the compilation of Ink Files, storing and logging todos, warnings and errors.
	// It manages a queue of items, ensuring that each file is completed in sequence.
	
	// Ink compilation is handled automatically when ink files change (via InkPostProcessor), but you can also manually trigger compilation.
	// The simplest usage is InkCompiler.CompileInk(inkFile). 
	// You can also specify if the compilation should be immediate or async, and provide a callback for when the queue completes.
	// Logs are fired when compilation begins and completes.

	// To ensure that compilation always completes reliably, this class also manages delaying entering play mode and cancels builds kicked off while mid-compile.
	
	// InkCompiler is designed as a ScriptableObject and is saved to the Library folder so that the queue persists across compilation.
	// The way we currently handle threads isn't great. I'd love for a member of the community to find a better way to do it!
	#if UNITY_2020_1_OR_NEWER
    [FilePath("Library/InkCompiler.asset", FilePathAttribute.Location.ProjectFolder)]
	public class InkCompiler : ScriptableSingleton<InkCompiler> {
    #else
	public class InkCompiler : ScriptableObject {
    #endif
		#region Legacy ScriptableSingleton
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
		#endregion


		#region Public Facing
		// If any items are queued, compiling, or complete!
		// If any items are in the queue, one is guaranteed to be compiling (or should begin to or complete imminently).
		public static bool executingCompilationStack {
			get {
				return instance.compilationStack.Count > 0;
			}
		}

		// This runs once when the compilation stack completes.
		public delegate void OnCompleteInkCompliationStackEvent (InkFile[] inkFiles);
		public static event OnCompleteInkCompliationStackEvent OnCompileInk;

		
		[Serializable]
		public enum CompilationStackItemState {
			// Default state, item is about to be queued for compilation
			Queued,
			
			// Item is now owned by the thread pool and being compiled
			Compiling,
			
			// Compilation has finished, item to be processed for errors and result handled
			Complete,
		}

		
		public static void CompileInk (params InkFile[] inkFiles) {
            CompileInk(inkFiles, false, null);
        }
		public static void CompileInk (InkFile[] inkFiles, bool immediate, Action onComplete = null) {
			if(inkFiles == null || inkFiles.Length == 0) return;
			#if UNITY_2019_4_OR_NEWER
			if(!disallowedAutoRefresh) {
				disallowedAutoRefresh = true;
				try {
					AssetDatabase.DisallowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed DisallowAutoRefresh "+e);
				}
			}
			#endif
            
			InkLibrary.Validate();
            if(onComplete != null) onCompleteActions.Add(onComplete);
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var inkFile in inkFiles) filesCompiledLog.AppendLine().Append(inkFile.filePath);
			
			StringBuilder outputLog = new StringBuilder ();
			outputLog.Append ("Ink compilation started at ");
			outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
			outputLog.Append (filesCompiledLog.ToString());
			Debug.Log(outputLog);
			
			foreach(var inkFile in inkFiles) {
				CompileInkInternal (inkFile, immediate);
			}
		}


		public static void SetBuildBlocked () {
			buildBlocked = true;
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
            foreach(var includeFile in inkFile.includesInkFiles) {
                anyChange = instance.pendingCompilationStack.Remove(includeFile.filePath) || anyChange;
            }
			if(anyChange)
				instance.Save(true);
        }

		public static bool AnyOfStateInCompilationStack (CompilationStackItemState state) {
			foreach(var compilationStackItem in instance.compilationStack) {
				if(compilationStackItem.state == state) 
					return true;
			}
			return false;
		}
		public static int CountOfStateInCompilationStack (CompilationStackItemState state) {
			int count = 0;
			foreach(var compilationStackItem in instance.compilationStack) {
				if(compilationStackItem.state == state) 
					count++;
			}
			return count;
		}
		static CompilationStackItem GetCurrentlyCompilingFile () {
			foreach(var compilationStackItem in instance.compilationStack) {
				if(compilationStackItem.state == CompilationStackItemState.Compiling) {
					return compilationStackItem;
				}
			}
			return null;
		}

		public static bool IsInkFileOnCompilationStack (InkFile inkFile) {
			foreach(var compilationStackItem in instance.compilationStack) {
				if(compilationStackItem.inkFile == inkFile) 
					return true;
			}
			return false;
		}

		// Find all the master ink files in a list of assets given by path.
		public static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
                foreach(var masterInkFile in GetMasterFilesIncludingInkAssetPath(importedAssetPath)) {
					if (!masterInkFiles.Contains(masterInkFile) && InkSettings.instance.ShouldCompileInkFileAutomatically(masterInkFile)) {
						masterInkFiles.Add(masterInkFile);
					}
				}
            }
			return masterInkFiles;
			
			// An ink file might actually have several owners! Return them all.
			IEnumerable<InkFile> GetMasterFilesIncludingInkAssetPath (string importedAssetPath) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				// Trying to catch a rare (and not especially important) bug that seems to happen occasionally when opening a project
				// It's probably this - I've noticed it before in another context.
				Debug.Assert(InkSettings.instance != null, "No ink settings file. This is a bug. For now you should be able to fix this via Assets > Rebuild Ink Library");
				// I've caught it here before
				Debug.Assert(inkFile != null, "No internal InkFile reference at path "+importedAssetPath+". This is a bug. For now you can fix this via Assets > Rebuild Ink Library");
				Debug.Assert(inkFile != null);
				return inkFile.masterInkFilesIncludingSelf;
			}
		}
		#endregion


		#region Static Private Variables
		// If we just blocked a build because of an ink compile
		static bool buildBlocked = false;
		// If we just blocked entering play mode because of an ink compile
		static bool playModeBlocked = false;


		// Track if we've currently locked compilation of Unity C# Scripts
		static bool hasLockedUnityCompilation = false;
		
		// When compiling we call AssetDatabase.DisallowAutoRefresh. 
		// We NEED to remember to re-allow it or unity stops registering file changes!
		// The issue is that you need to pair calls perfectly, and you can't even use a try-catch to get around it.
		// So - we cache if we've disabled auto refresh here, since this persists across plays.
		static bool disallowedAutoRefresh {
			get => SessionState.GetBool("InkLibraryDisallowedAutoRefresh", false);
			set => SessionState.SetBool("InkLibraryDisallowedAutoRefresh", value);
		}

        // Actions that are passed into the CompileInk function, to run and then clear when we complete the compilation stack.
		// To recieve an event each time the stack completes, see OnCompileInk.
        static List<Action> onCompleteActions = new List<Action>();
		
		// Thread lock
		static bool compileThreadActive {
			get {
				lock(_compileThreadActiveLock) {
					return _compileThreadActive;
				}
			}
			set {
				lock(_compileThreadActiveLock) {
					_compileThreadActive = value;
				}
			}
		}
		static bool _compileThreadActive;
		static object _compileThreadActiveLock = new object();
		#endregion

		#if UNITY_2020_2_OR_NEWER
		// ID for the Unity Progress API, which shows progress of the compile in the bottom right of Unity.
		static int compileProgressID;
		#endif
		
		#region Serialized Private Variables
		// If InkSettings' delayInPlayMode option is true, dirty files are added here when they're changed in play mode
		// This ensures they're remembered when you exit play mode and can be compiled
		// TODO - It might be safer for this to track the DefaultAsset for the ink file, rather than the path?
		[SerializeField]
		List<string> pendingCompilationStack = new List<string>();

		// The state of files currently being compiled.
		[SerializeField]
		List<InkCompiler.CompilationStackItem> compilationStack = new List<InkCompiler.CompilationStackItem>();
		#endregion
		
		[System.Serializable]
		class CompilationStackItem {
			public CompilationStackItemState state = CompilationStackItemState.Queued;
			public bool immediate;
			public InkFile inkFile;
			public string compiledJson;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public List<InkCompilerLog> logOutput = new List<InkCompilerLog>();
			public List<string> unhandledErrorOutput = new List<string>();
			public DateTime startTime;
			public DateTime endTime;

			public float timeTaken {
				get {
					if(state == CompilationStackItemState.Complete) return (float)(endTime - startTime).TotalSeconds;
					else return (float)(DateTime.Now - startTime).TotalSeconds;
				}
			}

			public CompilationStackItem () {}
			
			// Sets errors, warnings and todos to the ink file, and logs them to the console.
			public void SetOutputLog () {
				inkFile.errors.Clear();
				inkFile.warnings.Clear();
				inkFile.todos.Clear();

				foreach(var childInkFile in inkFile.includesInkFiles) {
					childInkFile.unhandledCompileErrors.Clear();
					childInkFile.errors.Clear();
					childInkFile.warnings.Clear();
					childInkFile.todos.Clear();
				}

				foreach(var output in logOutput) {
					if(output.type == ErrorType.Error) {
						inkFile.errors.Add(output);
						Debug.LogError("Ink "+output.type+" for "+Path.GetFileName(inkFile.filePath)+": "+output.content + " (at "+output.relativeFilePath+":"+output.lineNumber+")", inkFile.inkAsset);
					} else if (output.type == ErrorType.Warning) {
						inkFile.warnings.Add(output);
						Debug.LogWarning("Ink "+output.type+" for "+Path.GetFileName(inkFile.filePath)+": "+output.content + " (at "+output.relativeFilePath+" "+output.lineNumber+")", inkFile.inkAsset);
					} else if (output.type == ErrorType.Author) {
						inkFile.todos.Add(output);
						if(InkSettings.instance.printInkLogsInConsoleOnCompile)
							Debug.Log("Ink Log for "+Path.GetFileName(inkFile.filePath)+": "+output.content + " (at "+output.relativeFilePath+" "+output.lineNumber+")", inkFile.inkAsset);
					}
				}
			}
		}


		#region Init, Update, Saving
        // Ensure we save the InkCompiler state when we save assets.
        class AssetSaver : UnityEditor.AssetModificationProcessor {
            static string[] OnWillSaveAssets(string[] paths) {
                InkCompiler.instance.Save(true);
                return paths;
            }
        }

		// This is called when Unity recompiles. 
		[InitializeOnLoadMethod]
		static void OnProjectLoadedInEditor() {
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
			if(disallowedAutoRefresh) {
				disallowedAutoRefresh = false;
				try {
					AssetDatabase.AllowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed AllowAutoRefresh "+e);
				}
			}
			#endif
		}
		
		// Update loop, using the EditorApplication.update callback.
		private static void Update () {
            #if UNITY_2020_1_OR_NEWER
            // If we're not compiling but have locked C# compilation then now is the time to reset
			if (!executingCompilationStack && hasLockedUnityCompilation) {
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

			// If we're working through the stack. This is true as soon as an item is added to the stack.
			if(executingCompilationStack) {
				// Check for timeouts, in case of an unhandled bug with this system/the ink compiler!
				for (int i = instance.compilationStack.Count - 1; i >= 0; i--) {
					var compilingFile = instance.compilationStack [i];
					if (compilingFile.state == CompilationStackItemState.Compiling) {
						if (compilingFile.timeTaken > InkSettings.instance.compileTimeout) {
							// TODO - Cancel the thread if it's still going. Not critical, since its kinda fine if it compiles a bit later, but it's not clear.
							compileThreadActive = false;
							RemoveCompilingFile(i);
							Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+".\nCompilation should never take more than a few seconds, but for large projects or slow computers you may want to increase the timeout time in the InkSettings file.\nIf this persists there may be another issue; or else check an ink file exists at this path and try Assets/Recompile Ink, else please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+string.Join("\n",compilingFile.unhandledErrorOutput.ToArray()));
							TryCompileNextFileInStack();
						}
					}
				}

				// When all files have compiled, run the complete function.
				if(!AnyOfStateInCompilationStack(CompilationStackItemState.Compiling)) {
					if(!AnyOfStateInCompilationStack(CompilationStackItemState.Queued)) {
						OnCompleteCompilationStack();
					} else {
						// We used to avoid calling this here in favour of calling it CompileInkThreaded but it seems that it doesn't run when called there, for some reason.
						// If someone can make this work please let me know!
						TryCompileNextFileInStack();
					}
				}
				// When the current item being compiled using threads has finished, we set compileThreadActive to false.
				// Here we then set the compiling file's state from Compiling to Complete.
				// This roundabout way of doing things ensures that all CompilationStackItem code runs entirely outside of the thread.  
				else if(!compileThreadActive) {
					for (int i = instance.compilationStack.Count - 1; i >= 0; i--) {
						var compilingFile = instance.compilationStack [i];
						if (compilingFile.state == CompilationStackItemState.Compiling) {
							CompleteCompilingFile(compilingFile);
							break;
						}
					}
				}
			}


			// We don't show a progress bar on Linux (there appeared to be a Unity bug on an earlier version), so skip this step there.
			#if !UNITY_EDITOR_LINUX
			UpdateProgressBar();
			#endif
		}
		#endregion
		

		#region Compilation

		// Move files from the pendingCompilationStack to the compilationStack
		static void CompilePendingFiles () {
			InkLibrary.CreateOrReadUpdatedInkFiles (instance.pendingCompilationStack);
			foreach (var pendingMasterFile in GetUniqueMasterInkFilesToCompile(instance.pendingCompilationStack))
				CompileInk(pendingMasterFile);
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
			if(IsInkFileOnCompilationStack(inkFile)) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inkFile.filePath);
				return;
			}

			string inputPath = InkEditorUtils.CombinePaths(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));
			Debug.Assert(inkFile.absoluteFilePath == inputPath);

			CompilationStackItem pendingFile = new CompilationStackItem
			{
				inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath),
				inkAbsoluteFilePath = inputPath,
				jsonAbsoluteFilePath = inkFile.absoluteJSONPath,
				state = CompilationStackItemState.Queued,
				immediate = immediate
			};
			
			if(instance.compilationStack.Count == 0) OnBeginCompilationStack();
			instance.compilationStack.Add(pendingFile);
			instance.Save(true);
			
			TryCompileNextFileInStack();
		}

		private static void TryCompileNextFileInStack () {
			if(!executingCompilationStack) return;
			if(AnyOfStateInCompilationStack(CompilationStackItemState.Compiling)) return;
			
			// Find the next file to compile!
			CompilationStackItem fileToCompile = null;
			foreach(var compilationItem in instance.compilationStack) {
				if(compilationItem.state == CompilationStackItemState.Queued) {
					fileToCompile = compilationItem;
					break;
				}
			}

			// If a file was found, compile it according to its settings.
			if(fileToCompile != null) {
				BeginCompilingFile(fileToCompile);
				if(fileToCompile.immediate) {
					CompileInkThreaded(fileToCompile);
					CompleteCompilingFile(fileToCompile);
				} else {
					if(EditorApplication.isCompiling) Debug.LogWarning("Was compiling scripts when ink compilation started! This seems to cause the thread to cancel and complete, but the work isn't done. It may cause a timeout.");
					compileThreadActive = true;
					ThreadPool.QueueUserWorkItem(CompileInkThreaded, fileToCompile);
					// CompleteCompilingFile is called in Update when compileThreadActive becomes false.
				}
			}
		}
	
		// Removes a file from the compilation stack.
        static void RemoveCompilingFile (int index) {
            instance.compilationStack.RemoveAt(index);
            instance.Save(true);
            // Progress bar prevents delayCall callback from firing in Linux Editor, locking the
            // compilation until it times out. Let's just not show progress bars in Linux Editor    
            #if !UNITY_EDITOR_LINUX
            if (instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
            #endif
        }

		// Marks a CompilationStackItem as Compiling
		private static void BeginCompilingFile(CompilationStackItem item) {
			if(item.state != CompilationStackItemState.Queued) {
				Debug.LogError("Cannot begin compiling ink file because it is "+item.state+"! This is most likely a threading bug. Please report this!");
				return;
			}
			item.state = CompilationStackItemState.Compiling;
			item.startTime = DateTime.Now;
			#if UNITY_2020_2_OR_NEWER
			Progress.SetStepLabel(compileProgressID, item.inkFile.filePath);
			Progress.Report(compileProgressID, instance.compilationStack.IndexOf(item), instance.compilationStack.Count);
			#endif
		}

		// Marks a CompilationStackItem as Complete
		private static void CompleteCompilingFile(CompilationStackItem item) {
			if(item.state != CompilationStackItemState.Compiling) {
				Debug.LogError("Cannot complete compiling ink file because it is "+item.state+"! This is most likely a threading bug. Please report this!");
				return;
			}
			item.state = CompilationStackItemState.Complete;
			item.endTime = DateTime.Now;
			if (item.timeTaken > InkSettings.instance.compileTimeout * 0.6f)
				Debug.LogWarning ("Compilation for "+Path.GetFileName(item.inkFile.filePath)+" took over 60% of the time required to timeout the compiler. Consider increasing the compile timeout on the InkSettings file.");
			
			// If there's nothing left to compile, we can mark the stack as done!
			if(!AnyOfStateInCompilationStack(CompilationStackItemState.Compiling) && !AnyOfStateInCompilationStack(CompilationStackItemState.Queued))
				OnCompleteCompilationStack();
		}

		// Runs a new instance of Ink.Compiler, performing the actual compilation process!
		// Can be run from a thread or syncronously.
		// Ink is not threadsafe, and so this function should never be run twice simultaneously.
		// For this reason, this function doesn't set CompilationStackItem.state.
		private static void CompileInkThreaded(object itemObj) {
			CompilationStackItem item = (CompilationStackItem) itemObj;
			if(item.state != CompilationStackItemState.Compiling) Debug.LogError("File passed to CompileInkThreaded is not in Compiling state.");

			var inputString = File.ReadAllText(item.inkAbsoluteFilePath);
			var compiler = new Compiler(inputString, new Compiler.Options
			{
				countAllVisits = true,
				fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(item.inkAbsoluteFilePath)),
				errorHandler = (string message, ErrorType type) => {
					InkCompilerLog log;
					if(InkCompilerLog.TryParse(message, out log)) {
						if(string.IsNullOrEmpty(log.relativeFilePath)) log.relativeFilePath = Path.GetFileName(item.inkAbsoluteFilePath);
						item.logOutput.Add(log);
					} else {
						Debug.LogWarning("Couldn't parse log "+message);
					}
				}
			});

			try {
				var compiledStory = compiler.Compile();
				if (compiledStory != null)
					item.compiledJson = compiledStory.ToJson();
			} catch (SystemException e) {
				item.unhandledErrorOutput.Add(string.Format(
					"Ink Compiler threw exception \nError: {0}\n---- Trace ----\n{1}\n--------\n", e.Message,
					e.StackTrace));
			}

			// This MUST be the very last thing to occur in this function!
			compileThreadActive = false;
		}

		// When the compilation stack first gains an item
		private static void OnBeginCompilationStack () {
			#if UNITY_2020_2_OR_NEWER
			compileProgressID = Progress.Start("Compiling Ink", null, Progress.Options.None, -1);
			#endif
		}

		// When all files in stack have been compiled. 
		// This is called via update because Process events run in another thread.
		// It's now also called via CompleteCompilingFile, which enables immediate mode compliation to run synchronously!
		private static void OnCompleteCompilationStack () {
			if(AnyOfStateInCompilationStack(CompilationStackItemState.Queued)) {
				Debug.LogError("OnCompleteCompilationStack was called, but a file is now in the queue!");
				return;
			}
			if(AnyOfStateInCompilationStack(CompilationStackItemState.Compiling)) {
				Debug.LogError("OnCompleteCompilationStack was called, but a file is now compiling!");
				return;
			}
            // Clone and clear the list. This is a surefire way to ensure the list is cleared in case of unhandled errors in this code.
            // A Try-catch would be better but I'm debugging blind as I write this and the nuclear option will definitely work!
            List<CompilationStackItem> compilationStack = new List<CompilationStackItem>(instance.compilationStack);
			instance.compilationStack.Clear();
			instance.Save(true);



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


			// Sets output info for each InkFile (todos, warnings and errors); produces and fires the post-compliation log
			bool errorsFound = false;
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var compilingFile in compilationStack) {
				if(compilingFile.inkFile.inkAsset != null) {
                    // Load and store a reference to the compiled file
                    compilingFile.inkFile.FindCompiledJSONAsset();
                    
                    filesCompiledLog.AppendLine().Append(compilingFile.inkFile.filePath);
                    filesCompiledLog.Append(string.Format(" ({0}s)", compilingFile.timeTaken));
                    if(compilingFile.unhandledErrorOutput.Count > 0) {
                        filesCompiledLog.Append(" (With unhandled error)");
                        StringBuilder errorLog = new StringBuilder ();
                        errorLog.Append ("Unhandled error(s) occurred compiling Ink file ");
                        errorLog.Append ("'");
                        errorLog.Append (compilingFile.inkFile.filePath);
                        errorLog.Append ("'");
                        errorLog.AppendLine ("! You may be able to resolve your issue by using Edit > Rebuild Ink Library.");
                        errorLog.AppendLine ("Please report following error(s) as a bug:");
                        foreach (var error in compilingFile.unhandledErrorOutput)
                            errorLog.AppendLine (error);
                        Debug.LogError(errorLog);
                        compilingFile.inkFile.unhandledCompileErrors = compilingFile.unhandledErrorOutput;
                        errorsFound = true;
                    } else {
                        compilingFile.SetOutputLog();
                        bool errorsInEntireStory = false;
                        bool warningsInEntireStory = false;
                        foreach(var inkFile in compilingFile.inkFile.includesInkFiles) {
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

			StringBuilder outputLog = new StringBuilder ();
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


			// Clean up locks and progress bars
			#if UNITY_2020_2_OR_NEWER
			Progress.Remove(compileProgressID);
			compileProgressID = -1;
			#endif
			
			#if !UNITY_EDITOR_LINUX
			EditorUtility.ClearProgressBar();
			#endif
			
			#if UNITY_2019_4_OR_NEWER
			if(disallowedAutoRefresh) {
				disallowedAutoRefresh = false;
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


			// Fires completion events
            foreach(var onCompleteAction in onCompleteActions) {
                if(onCompleteAction != null) onCompleteAction();
            }
            onCompleteActions.Clear();
			
			if (OnCompileInk != null) {
				InkFile[] inkFilesCompiled = new InkFile[compilationStack.Count];
				for (int i = 0; i < compilationStack.Count; i++) {
					inkFilesCompiled[i] = compilationStack[i].inkFile;
				}
				if(OnCompileInk != null) OnCompileInk (inkFilesCompiled);
			}


			// If we wanted to enter play mode but were blocked because of ink compliation, enter play mode now.
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
			
		}
		#endregion

		
		#region Progress Bar
		static void UpdateProgressBar () {
			if(instance.compilationStack.Count == 0) return;
			if(buildBlocked || playModeBlocked || EditorApplication.isPlaying) ShowProgressBar();
			else EditorUtility.ClearProgressBar();
		}

		public static void ShowProgressBar () {
			int numCompiling = CountOfStateInCompilationStack(CompilationStackItemState.Compiling);
			var compilingFile = GetCurrentlyCompilingFile();
			string message = "";
			if(compilingFile == null) message += "Compiling...";
			else message += "Compiling "+compilingFile.inkFile.inkAsset.name+" for "+compilingFile.timeTaken+"s";
			message += "\n.Ink File "+(instance.compilationStack.Count-numCompiling)+" of "+instance.compilationStack.Count+".";
			if(playModeBlocked) message += " Will enter play mode when complete.";
			EditorUtility.DisplayProgressBar("Compiling Ink...", message, GetEstimatedCompilationProgress());
		}

		public static float GetEstimatedCompilationProgress () {
			if(!executingCompilationStack) return 1;
			float progress = 0;
			foreach (var compilingFile in instance.compilationStack) {
				if (compilingFile.state == CompilationStackItemState.Compiling)
					progress += compilingFile.timeTaken / InkSettings.instance.compileTimeout;
				if (compilingFile.state == CompilationStackItemState.Complete)
					progress += 1;
			}
			progress /= instance.compilationStack.Count;
			return progress;
		}
		#endregion


		#region Prevent entering Play Mode while mid-compile
		#if UNITY_2017_1_OR_NEWER
		static void OnPlayModeChange (PlayModeStateChange mode) {
			if(mode == PlayModeStateChange.EnteredEditMode && instance.pendingCompilationStack.Count > 0)
				CompilePendingFiles();
			if(mode == PlayModeStateChange.ExitingEditMode && executingCompilationStack)
				BlockPlayMode();
			if(mode == PlayModeStateChange.EnteredPlayMode && executingCompilationStack)
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

		static void BlockPlayMode () {
			EditorApplication.isPlaying = false;
			var percentage = String.Format("{0:P0}.", GetEstimatedCompilationProgress());
			Debug.LogWarning("Delayed entering play mode because Ink is still compiling ("+percentage+"). Will enter play mode on completion.");
			playModeBlocked = true;
		}

		static void EnteredPlayModeWhenCompiling () {
			Debug.LogError("Entered Play Mode while Ink was still compiling! Your story will not be up to date. Recommend exiting and re-entering play mode.\nWe normally delay entering play mode when compiling, so you've found an edge case!");
		}
		#endregion
	}
}