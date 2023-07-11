using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Ink.Runtime;
using Ink.UnityIntegration.Debugging;

namespace Ink.UnityIntegration {

	/// <summary>
	/// Ink player window. Tests stories in an editor window.
	/// Stories may be attached at runtime. InkPlayerWindow.DrawStoryPropertyField may be used for this.
	/// </summary>
	public class InkPlayerWindow : EditorWindow {
        #region User Facing

		public static bool visible {get; private set;}
		public static bool isOpen {get; private set;}
		public static bool attached {get; private set;}

		
		// Fires when story is set. Handy if you want to perform actions on the story as soon as it's tethered.
        public static Action<Story> OnDidSetStory;

		// Overrides the action triggered by buttons that display story.currentChoices.
        public static Action<int> OnChooseChoiceIndexOverride;


		// Overrides the "Divert" button for named content. Allows for custom ChoosePathString functionality.
        public static Action<string> OnDivertOverride;
		// Overrides the "Divert" button label. Can be handy for making it clear that Divert has been overridden in different contexts.
        public static Func<string, GUIContent> OnDivertLabelOverride;


        // Allows injecting right click context options into the story content view.
        public delegate void ContextMenuDelegate(GenericMenu contextMenu, InkHistoryContentItem content);
        public static List<ContextMenuDelegate> contextMenuDelegates = new List<ContextMenuDelegate>();

		
		
		public static InkPlayerParams playerParams = InkPlayerParams.Standard;
		public static PlayerOptions playerOptions = new PlayerOptions();

		

		// Create or get the window. If creating, dock it on the same panel as the inspector.
		[MenuItem("Window/Ink Player %#i", false, 2300)]
		public static InkPlayerWindow GetWindow () {
			return GetWindow(true);
		}

		public static InkPlayerWindow GetWindow (bool focus) {
			Type windowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			return GetWindow<InkPlayerWindow>(windowTitle, focus, windowType);
		}
		


		

		// Entry point for loading and playing a story.
		public static void LoadAndPlay (TextAsset storyJSONTextAsset, bool focusWindow = true) {
			GetWindow(focusWindow);
			if(InkPlayerWindow.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					InkPlayerWindow.Stop();
					InkPlayerWindow.Play(storyJSONTextAsset);
				}
			} else {
				InkPlayerWindow.Play(storyJSONTextAsset);
			}
		}

		public static void LoadAndPlay (string storyJSON, bool focusWindow = true) {
			GetWindow(focusWindow);
			if(InkPlayerWindow.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					InkPlayerWindow.Stop();
					InkPlayerWindow.Play(storyJSON);
				}
			} else {
				InkPlayerWindow.Play(storyJSON);
			}
		}
	
		// Handy utility for the common case of not wanting to show the ink player window when game view is maximised
		public static bool GetGameWindowIsMaximised () {
			var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
			var gameWindow = EditorWindow.GetWindow(gameViewType);
            return gameWindow == null ? false : gameWindow.maximized;
		}
		

		// Loads an existing story to the player window. Handy for debugging stories running in games in editor.
		public static void Attach (Story story) {
			Attach(story, InkPlayerWindow.InkPlayerParams.DisableInteraction);
		}
		public static void Attach (Story story, InkPlayerParams inkPlayerParams) {
			Clear();
			playerOptions.continueAutomatically = false;
			playerOptions.chooseAutomatically = false;
			playerParams = inkPlayerParams;
			attached = true;
			attachedWhileInPlayMode = EditorApplication.isPlaying;
			InkPlayerWindow.story = story;
            
			// Clear the last loaded story setup on attaching stories. We don't NEED to do this but it's never really helpful and often seems like a bug.
			InkPlayerWindowState.Instance.lastStoryJSONAssetPath = null;
			InkPlayerWindowState.Instance.lastStoryWasPlaying = false;

            // This allows reconstructing the story so it can be used after recompile. However, it can be expensive to run so there's a good argument for not running it on play!
            // var lastTime = Time.realtimeSinceStartup;
            // storyJSON = InkPlayerWindow.story.ToJson();
            // File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AttachedStory.json"), storyJSON);
            // Debug.Log("Wrote to "+System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AttachedStory.json")+" in "+(Time.realtimeSinceStartup-lastTime));
		}
		
		// Detaches the attached story
		public static void Detach () {
			DetachInstance();
		}


        /// <summary>
		/// Draws a property field for a story using GUILayout, allowing you to attach stories to the player window for debugging.
		/// An example of how this is used is found in the Demo scene.
		/// </summary>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Story story, ref bool expanded, GUIContent label) {
			DrawStoryPropertyField(story, InkPlayerParams.DisableInteraction, ref expanded, label);
		}
		public static void DrawStoryPropertyField (Story story, InkPlayerParams playerParams, ref bool expanded, GUIContent label, bool interactable = false) {
			EditorGUILayout.BeginHorizontal();
			expanded = EditorGUILayout.Foldout(expanded, label, true);
			// var lastRect = GUILayoutUtility.GetLastRect();
			// GUILayout.BeginArea(new Rect(lastRect.x+200,lastRect.y,lastRect.width-200,lastRect.height));
			// Debug.Log(lastRect);
			// GUI.Box(lastRect,"");
			// GUILayout.BeginArea(lastRect);
			if(EditorApplication.isPlaying) {
				if(story != null) {
					// InkPlayerWindow window = InkPlayerWindow.GetWindow(false);
					if(InkPlayerWindow.attached && InkPlayerWindow.story == story) {
						if(GUILayout.Button("Detach", GUILayout.Width(80))) {
							InkPlayerWindow.Detach();
						}
					} else {
						if(GUILayout.Button("Attach", GUILayout.Width(80))) {
							InkPlayerWindow.Attach(story, playerParams);
						}
					}
					// EditorGUI.BeginDisabledGroup(visible);
					if(GUILayout.Button(InkPlayerWindow.isOpen ? "Show Player Window" : "Open Player Window", GUILayout.Width(140))) {
						InkPlayerWindow.GetWindow();
					}
				} else {
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button("Story cannot be null to attach to editor");
					EditorGUI.EndDisabledGroup();
				}
			} else {
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button("Enter play mode to attach to editor", GUILayout.Width(220));
				EditorGUI.EndDisabledGroup();
			}
			// GUILayout.EndArea();
			EditorGUILayout.EndHorizontal();
			if(expanded) {

				if(story != null) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(!interactable);
					// Draw can continue
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Toggle("Can Continue", story.canContinue);
					if(GUILayout.Button("Continue")) {
						story.Continue();
					}
					EditorGUILayout.EndHorizontal();

					// Draw current text
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Current Text");
					EditorGUILayout.TextArea(story.currentText, wordWrappedTextArea);
					EditorGUILayout.EndHorizontal();

					// Draw current choices
					foreach(var choice in story.currentChoices) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Choice "+choice.index, choice.text);
						if(GUILayout.Button(new GUIContent("Choose"))) {
							if(OnChooseChoiceIndexOverride != null) OnChooseChoiceIndexOverride(choice.index);
							else story.ChooseChoiceIndex(choice.index);
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}
			}
		}

		

		/// <summary>
		/// Draws a property field for a story using GUI, allowing you to attach stories to the player window for debugging.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Rect position, Story story, GUIContent label) {
			position = EditorGUI.PrefixLabel(position, label);
			InkPlayerWindow.GetWindow(false);
			if(EditorApplication.isPlaying && story != null/* && story.state != null*/) {
				if(InkPlayerWindow.attached && InkPlayerWindow.story == story) {
					if(GUI.Button(position, "Detach")) {
						InkPlayerWindow.Detach();
					}
				} else {
					if(GUI.Button(position, "Attach")) {
						InkPlayerWindow.Attach(story);
					}
				}
			} else {
				EditorGUI.BeginDisabledGroup(true);
				GUI.Button(position, "Enter play mode to attach to editor");
				EditorGUI.EndDisabledGroup();
			}
		}

        



		// Allows you to change what the ink player does/is allowed to do when a story is loaded.
		// Especially handy for attached stories, where performance is critical or play controls might interfere with the game
		// TODO: Show these params somewhere slightly out of the way (debug mode for the window?) so you can fiddle with them mid-game if you <i>really</i> need to
		public struct InkPlayerParams {
			public bool disablePlayControls;
			public bool disableUndoHistory;
			public bool disableChoices;
			public bool disableStateLoading;
			public bool disableSettingVariables;
			public bool profileOnStart;
            
			public static InkPlayerParams Standard {
				get {
					return new InkPlayerParams();
				}
			} 
			public static InkPlayerParams DisableInteraction {
				get {
					var inkPlayerParams = new InkPlayerParams();
					inkPlayerParams.disablePlayControls = true;
					inkPlayerParams.disableUndoHistory = true;
					inkPlayerParams.disableChoices = true;
					inkPlayerParams.disableStateLoading = true;
					inkPlayerParams.disableSettingVariables = true;
					inkPlayerParams.profileOnStart = false;
					return inkPlayerParams;
				}
			} 
		}

		
        // Allows telling the story to play automatically.
		[System.Serializable]
		public class PlayerOptions {
			public bool continueAutomatically = true;
			public bool chooseAutomatically = false;
			public float continueAutomaticallyTimeInterval = 0.1f;
			public float chooseAutomaticallyTimeInterval = 0.1f;
		}

        #endregion




		
		static string storyJSON;
		static DateTime currentStoryJSONLastEditDateTime;

		private static Story _story;
		public static Story story {
			get {
				return _story;
			} private set {
				if(_story != null) {
					OnUnsetStory();
				}
				_story = value;
				if(_story != null) {
					OnSetStory();
				}
			}
		}


		private static TextAsset _storyStateTextAsset;
		public static TextAsset storyStateTextAsset {
			get {
				return _storyStateTextAsset;
			} set {
				if(_storyStateTextAsset == value) 
					return;
				_storyStateTextAsset = value;
				if(_storyStateTextAsset != null)
					storyStateValid = InkEditorUtils.CheckStoryStateIsValid(storyJSONTextAsset.text, storyStateTextAsset.text);
			}
		}





		[System.Serializable]
		public class InkPlayerWindowState {
			static string settingsEditorPrefsKey = typeof(InkPlayerWindowState).Name +" Settings";
			public static event Action OnCreateOrLoad;
			static InkPlayerWindowState _Instance;
			public static InkPlayerWindowState Instance {
				get {
					if(_Instance == null) LoadOrCreateAndSave();
					return _Instance;
				}
			}

			static InkPlayerWindowState LoadOrCreateAndSave () {
				Load();
				if(_Instance == null) CreateAndSave();
				return _Instance;
			}

			public static void CreateAndSave () {
				_Instance = new InkPlayerWindowState();
				Save(_Instance);
				if(OnCreateOrLoad != null) OnCreateOrLoad();
			}
			
			public static void Save () {
				Save(_Instance);
			}

			public static void Save (InkPlayerWindowState settings) {
				string data = JsonUtility.ToJson(settings);
				EditorPrefs.SetString(settingsEditorPrefsKey, data);
			}

			static void Load () {
				if(!EditorPrefs.HasKey(settingsEditorPrefsKey)) return;
				string data = EditorPrefs.GetString(settingsEditorPrefsKey);
				try {
					_Instance = JsonUtility.FromJson<InkPlayerWindowState>(data);
					if(_Instance != null) if(OnCreateOrLoad != null) OnCreateOrLoad();
				} catch {
					Debug.LogError("Save Data was corrupt and could not be parsed. New data created. Old data was:\n"+data);
					CreateAndSave();
				}
			}

			public string lastStoryJSONAssetPath;
			public bool lastStoryWasPlaying;
			public TextAsset TryGetLastStoryJSONAsset () {
				if(lastStoryJSONAssetPath == null) return null;
				var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(lastStoryJSONAssetPath);
				if(asset == null) {
					lastStoryJSONAssetPath = null;
					Save();
				}
				return asset;
			}

			public StoryPanelState storyPanelState = new StoryPanelState() {showing=true};
			public BaseStoryPanelState choicePanelState = new BaseStoryPanelState() {showing=true};
			public BaseStoryPanelState storyStatePanelState = new BaseStoryPanelState();
			public BaseStoryPanelState profilerPanelState = new BaseStoryPanelState();
			public BaseStoryPanelState saveLoadPanelState = new BaseStoryPanelState();
			// public DivertPanelState divertPanelState = new DivertPanelState();
			public NamedContentPanelState namedContentPanelState = new NamedContentPanelState();
			public FunctionPanelState functionPanelState = new FunctionPanelState();
			// public FunctionPanelState.FunctionParams functionParams = new FunctionPanelState.FunctionParams();
			public VariablesPanelState variablesPanelState = new VariablesPanelState();
			public ObservedVariablesPanelState observedVariablesPanelState = new ObservedVariablesPanelState();
		}

		
		private const string windowTitle = "Ink Player";

		static bool attachedWhileInPlayMode {get; set;}

		static TextAsset _storyJSONTextAsset;
		static TextAsset storyJSONTextAsset {
			get {
				return _storyJSONTextAsset;
			} set {
				if(_storyJSONTextAsset == value) return;
				_storyJSONTextAsset = value;
				if (_storyJSONTextAsset != null) {
					InkPlayerWindowState.Instance.lastStoryJSONAssetPath = AssetDatabase.GetAssetPath(storyJSONTextAsset);
					string fullJSONFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(InkPlayerWindowState.Instance.lastStoryJSONAssetPath);
					currentStoryJSONLastEditDateTime = File.GetLastWriteTime(fullJSONFilePath);
				} else {
					InkPlayerWindowState.Instance.lastStoryJSONAssetPath = null;
				}
				InkPlayerWindowState.Save();
			}
		}
		




		// This tracks the story state each time the user advances the story so that we can undo/redo
		static UndoHistory<InkPlayerHistoryItem> storyStateHistory = new UndoHistory<InkPlayerHistoryItem>();
		// This tracks the story output so we can view it in the content panel
		static List<InkHistoryContentItem> storyHistory = new List<InkHistoryContentItem>();

		
		static Exception playStoryException;
		static bool storyStateValid = false;


//		WindowState windowState = new WindowState();
//		public class WindowState {
		static Vector2 _scrollPosition;
		static Vector2 scrollPosition {
			get {
				return _scrollPosition;
			} set {
				if(_scrollPosition == value) return;
				_scrollPosition = value;
			}
		}
//		}

		
		
		[System.Serializable]
		public class BaseStoryPanelState {
			public bool showing;
			[SerializeField]
			Vector2 _scrollPosition;
			public Vector2 scrollPosition {
				get {
					return _scrollPosition;
				} set {
					if(_scrollPosition == value) return;
					_scrollPosition = value;
					// Deselect lines on scrolling because otherwise unity does weird things with the selection
					// Disabled because it was deselecting the search field as you typed
					// GUI.FocusControl(null);
				}
			}
            
			public float y;
			public float height = 200;
		}

		[System.Serializable]
		public class StoryPanelState : BaseStoryPanelState {
			public DisplayOptions displayOptions = new DisplayOptions();
			public string searchString = string.Empty;
			
			public const float minScrollRectHeight = 30;
			public const float maxScrollRectHeight = 480;
                
            public enum AutoScrollMode {
                WhenAtBottom,
                Always,
                Off
            }
            // Controls when story view should scroll to the bottom when the story view changes.
            public AutoScrollMode autoScrollMode = AutoScrollMode.WhenAtBottom;
		}


		[System.Serializable]
		public class NamedContentPanelState : BaseStoryPanelState {
			public string searchString = string.Empty;
		}

		// [System.Serializable]
		// public class DivertPanelState : BaseStoryPanelState {
		// 	public string divertCommand = String.Empty;
		// }

		static ReorderableList functionInputList;
		[System.Serializable]
		public class FunctionPanelState : BaseStoryPanelState {
			[System.Serializable]
			public class FunctionParams {
				[System.Serializable]
				public class FunctionInput {
					public enum FunctionInputType {
						Float,
						Int,
						String,
						Bool,
						InkVariable,
						InkListVariable
					}
					public FunctionInputType type;
					public float floatValue;
					public int intValue;
					public string stringValue;
					public bool boolValue;
					public string inkVariablePath;
					public object inkVariableValue;
					public InkList inkListVariableValue;
					public string inkListVariablePath;

					public void RefreshInkVariableValue (Story story) {
						if(!string.IsNullOrWhiteSpace(inkVariablePath)) inkVariableValue = story.variablesState[inkVariablePath];
						else inkVariableValue = null;
					}
					public void RefreshInkListVariableValue (Story story) {
						inkListVariableValue = null;
						try {
							if(!string.IsNullOrWhiteSpace(inkListVariablePath)) 
								inkListVariableValue = Ink.Runtime.InkList.FromString(inkListVariablePath, story);
						} catch {}
					}

					public override int GetHashCode () {	
						switch(type) {
						case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Float:
							return floatValue.GetHashCode();
						case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Int:
							return intValue.GetHashCode();
						case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.String:
							return stringValue != null ? stringValue.GetHashCode() : 0;
						case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Bool:
							return boolValue.GetHashCode();
						case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable:
							return inkVariablePath != null ? inkVariablePath.GetHashCode() : 0;
						case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable:
							return inkListVariablePath != null ? inkListVariablePath.GetHashCode() : 0;
						default:
							Debug.LogWarning("No hash code for type: "+type);
							return 1;
						}
					}
				}
				public string functionName = String.Empty;
				public List<FunctionInput> inputs = new List<FunctionInput>();

				public int GetInputHashCode () {	
					int hash = 19;
					if(!string.IsNullOrEmpty(functionName)) hash = hash * 31 + functionName.GetHashCode();
					foreach(var item in inputs) hash = (hash * 31) + item.GetHashCode();
					return hash;
				}
			}
			public FunctionParams functionParams = new FunctionParams();
			public int testedFunctionHash;
			public DateTime testedFunctionTime;
			public object functionReturnValue = null;
		}

		[System.Serializable]
		public class VariablesPanelState : BaseStoryPanelState {
			public string searchString = string.Empty;
			public List<string> expandedVariables = new List<string>();
		}
		public class ObservedVariablesPanelState : BaseStoryPanelState {
			// The cache is used to restore observed variables when the user exits play mode.
			public List<string> restorableObservedVariableNames = new List<string>();
			public Dictionary<string, ObservedVariable> observedVariables = new Dictionary<string, ObservedVariable>();
		}

		[System.Serializable]
		public class DisplayOptions {
			[Flags]
			public enum VisibilityOptions {
				Warnings = 1 << 0,
				Errors = 1 << 1,
				Content = 1 << 2,
				PresentedChoices = 1 << 3,
				SelectedChoice = 1 << 4,
				Function = 1 << 5,
				ChoosePathString = 1 << 6,
				DebugNotes = 1 << 7,
				TimeStamp = 1 << 8,
				EmptyEntries = 1 << 9,
				Tags = 1 << 10,
			}
			public VisibilityOptions visibilityOptions = VisibilityOptions.Warnings | VisibilityOptions.Errors | VisibilityOptions.Content;

			public bool displayWarningsInConsole = true;
			public bool displayErrorsInConsole = true;
		}

		static GUIStyle searchTextFieldStyle;
		static GUIStyle searchCancelButtonStyle;

		internal static DateTime dateTimeNow;


		static float lastOnGUITime = -1f;
		static float lastUpdateTime = -1f;

        

        // Lots of bits to do with the story view scroll rect
		public enum AutoScrollSmoothingMode {
			NONE,
			Snap,
			Smooth
		}
        
        static float storyScrollRectBottom;
        static bool storyScrollSnappedToBottom;
		static AutoScrollSmoothingMode storyScrollMarkedForBottom;
		static AutoScrollSmoothingMode storyScrollMarkedForSelectedLine;
		static InkHistoryContentItem selectedLine;

        static bool mainScrollViewActive;
		
		static bool doingAutoscroll;
		static float autoscrollTarget;
		static float autoscrollVelocity;
		static float autoscrollSmoothTime = 0.225f;



		static float timeUntilNextAutomaticChoice = 0;
		static float timeUntilNextAutomaticContinue = 0;



        // TODO - find a way to restore tethered stories after recompile. This is tricky because we don't have a reference to the json, and stories aren't serialized.
        // We should probably save the story to this path - but watch out for giant stories.
        // var jsonStr = story.ToJson ();
        // https://docs.microsoft.com/en-us/dotnet/api/system.io.path.gettemppath
        // Directory.temporaryFolder
		void OnEnable () {
			if(isOpen) return;
			isOpen = true;
			
			InkPlayerWindowState.OnCreateOrLoad += () => {
				// InkPlayerWindowState.Instance.functionPanelState.functionParams = InkPlayerWindowState.Instance.functionParams;
				BuildFunctionInputList();
			};
			
			BuildFunctionInputList();
			
			EditorApplication.update += Update;

			if(story == null && !EditorApplication.isPlayingOrWillChangePlaymode) {
				var lastLoadedStory = InkPlayerWindowState.Instance.TryGetLastStoryJSONAsset();
				if(lastLoadedStory != null) {
					if(InkPlayerWindowState.Instance.lastStoryWasPlaying) {
						LoadAndPlay(lastLoadedStory, false);
					} else {
						TryPrepareInternal(lastLoadedStory);
					}
				}
			}
		}

		void OnDisable () {
			EditorApplication.update -= Update;
		}

		private void OnBecameVisible() {
        	visible = true;
			if(doingAutoscroll) {
				InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, autoscrollTarget);
				doingAutoscroll = false;
			}
		}
		
		void OnBecameInvisible () {
			visible = false;
		}
			
		void OnDestroy () {
			isOpen = false;
			Clear();
		}

		private static void Update () {
			var time = Time.realtimeSinceStartup;
			var deltaTime = 0f;
			if(lastUpdateTime != -1)
				deltaTime = time - lastUpdateTime;
			lastUpdateTime = time;
			
            if(story != null) {
				timeUntilNextAutomaticChoice -= deltaTime;
				if(story.currentChoices.Count > 0 && playerOptions.chooseAutomatically) {
					if(timeUntilNextAutomaticChoice <= 0) {
						MakeRandomChoice();
						timeUntilNextAutomaticChoice = playerOptions.chooseAutomaticallyTimeInterval;
					}
				}
				if(story.canContinue && playerOptions.continueAutomatically) {
					timeUntilNextAutomaticContinue -= deltaTime;
					if(timeUntilNextAutomaticContinue <= 0) {
						if(playerOptions.continueAutomaticallyTimeInterval == 0) {
							story.ContinueMaximally();
						} else {
							ContinueStory();
						}
						timeUntilNextAutomaticContinue = playerOptions.continueAutomaticallyTimeInterval;
					}
				}
			}
		}




		

		static void OnDidContinue () {
            AddStoryContent(story.currentText.Trim(), story.currentTags);
			if(story.currentChoices != null) {
				foreach(var choice in story.currentChoices) {
					AddToHistory(InkHistoryContentItem.CreateForPresentChoice(choice));
				}
			}
			AddWarningsAndErrorsToHistory();
		}
		static void OnMakeChoice (Choice choice) {
            AddToHistory(InkHistoryContentItem.CreateForMakeChoice(choice));		
			AddWarningsAndErrorsToHistory();
		}
		static void OnEvaluateFunction (string functionName, object[] arguments) {
            StringBuilder sb = new StringBuilder(functionName);
			if(arguments != null && arguments.Length > 0) {
				sb.Append(" with args: ");
				for (int i = 0; i < arguments.Length; i++) {
					if(arguments[i] == null) sb.Append("NULL");
					else {
						sb.Append(arguments[i]);
						sb.Append(" (");
						sb.Append(arguments[i].GetType().Name);
						sb.Append(")");
					}
					if(i < arguments.Length-1) sb.Append(", ");
				}
			}
			AddToHistory(InkHistoryContentItem.CreateForEvaluateFunction(sb.ToString().Trim()));		
			AddWarningsAndErrorsToHistory();
		}
		static void OnCompleteEvaluateFunction (string functionName, object[] arguments, string textOutput, object result) {
			StringBuilder sb = new StringBuilder(functionName);
			if(arguments != null && arguments.Length > 0) {
				sb.Append(" with args: ");
				for (int i = 0; i < arguments.Length; i++) {
					if(arguments[i] == null) sb.Append("NULL");
					else {
						sb.Append(arguments[i]);
						sb.Append(" (");
						sb.Append(arguments[i].GetType().Name);
						sb.Append(")");
					}
					if(i < arguments.Length-1) sb.Append(", ");
				}
				bool hasTextOutput = textOutput != null && textOutput != string.Empty;
				if(hasTextOutput) sb.Append(" text output is: "+textOutput);
				if(result != null) sb.Append(" result is: "+result);
				if(!hasTextOutput && result == null) sb.Append("has no output");
			}
			AddToHistory(InkHistoryContentItem.CreateForCompleteEvaluateFunction(sb.ToString().Trim()));		
			AddWarningsAndErrorsToHistory();
		}
		static void OnChoosePathString (string pathString, object[] arguments) {
            StringBuilder sb = new StringBuilder("ChoosePathString: ");
			sb.Append(pathString);
			if(arguments != null) {
				sb.Append(" with args: ");
				for (int i = 0; i < arguments.Length; i++) {
					if(arguments[i] == null) sb.Append("NULL");
					else {
						sb.Append(arguments[i]);
						sb.Append(" (");
						sb.Append(arguments[i].GetType().Name);
						sb.Append(")");
					}
					if(i < arguments.Length-1) sb.Append(", ");
				}
			}
			AddToHistory(InkHistoryContentItem.CreateForChoosePathString(sb.ToString().Trim()));
			AddWarningsAndErrorsToHistory();
		}
        static void OnLoadState () {
            OnDidContinue();
        }

		static void AddWarningsAndErrorsToHistory () {
			if(story.hasWarning) {
				foreach(var warning in story.currentWarnings) {
					AddToHistory(InkHistoryContentItem.CreateForWarning(warning.Trim()));
					if(InkPlayerWindowState.Instance.storyPanelState.displayOptions.displayWarningsInConsole) {
						Debug.LogWarning("Ink Warning: "+warning.Trim());
					}
				}
			}
			if(story.hasError) {
				foreach(var error in story.currentErrors) {
					AddToHistory(InkHistoryContentItem.CreateForError(error.Trim()));
					if(InkPlayerWindowState.Instance.storyPanelState.displayOptions.displayErrorsInConsole) {
						Debug.LogError("Ink Error: "+error.Trim());
					}
				}
			}
		}





		static void DetachInstance () {
			attached = false;
			story = null;
		}

		
		static void Play (TextAsset storyJSONTextAsset) {
			Play(storyJSONTextAsset, InkPlayerParams.Standard);
		}
		static void Play (TextAsset storyJSONTextAsset, InkPlayerParams inkPlayerParams) {
			if(TryPrepareInternal(storyJSONTextAsset)) {
				InkPlayerWindow.playerParams = inkPlayerParams;
				PlayInternal();
			}
		}
		static void Play (string storyJSON) {
			Play(storyJSON, InkPlayerParams.Standard);
		}
		static void Play (string storyJSON, InkPlayerParams inkPlayerParams) {
			if(TryPrepareInternal(storyJSON)) {
				InkPlayerWindow.playerParams = inkPlayerParams;
				PlayInternal();
			}
		}


		static void PlayInternal () {
			story = new Story(storyJSON);
		}

		// Loads the story, ready to be played
		static bool TryPrepareInternal (TextAsset newStoryJSONTextAsset) {
			// This forces a refresh
			storyJSONTextAsset = null;
			storyJSONTextAsset = newStoryJSONTextAsset;
			if(storyJSONTextAsset == null || !InkEditorUtils.CheckStoryIsValid(storyJSONTextAsset.text, out playStoryException))
				return false;
			storyJSON = storyJSONTextAsset.text;
			return true;
		}
		static bool TryPrepareInternal (string newStoryJSON) {
			if(!InkEditorUtils.CheckStoryIsValid(storyJSON, out playStoryException))
				return false;
			InkPlayerWindow.storyJSONTextAsset = null;
			InkPlayerWindow.storyJSON = newStoryJSON;
			return true;
		}

		static void OnUnsetStory () {
			// Unsubscribe from all story events we subscribed to
			_story.onDidContinue -= OnDidContinue;
			_story.onMakeChoice -= OnMakeChoice;
			_story.onEvaluateFunction -= OnEvaluateFunction;
			_story.onCompleteEvaluateFunction -= OnCompleteEvaluateFunction;
			_story.onChoosePathString -= OnChoosePathString;
			_story.state.onDidLoadState -= OnLoadState;
			
			// Clear any exceptions related to the story we were storing
			playStoryException = null;
			
			// Clear the history 
			ClearStoryHistory();
			
			// Unobserve all observed variables.
			foreach(var observedVariableName in InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames)
				UnobserveVariable(observedVariableName, false);
			InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
			
			InkPlayerWindowState.Instance.lastStoryWasPlaying = false;

			InkPlayerWindowState.Save();
		}


		static void OnSetStory () {
			// Allow function fallbacks so we can subscribe to them and avoid throwing errors.
			_story.allowExternalFunctionFallbacks = true;
			
			// Subscribe to all story events we'll use
			_story.onDidContinue += OnDidContinue;
			_story.onMakeChoice += OnMakeChoice;
			_story.onEvaluateFunction += OnEvaluateFunction;
			_story.onCompleteEvaluateFunction += OnCompleteEvaluateFunction;
			_story.onChoosePathString += OnChoosePathString;
			_story.state.onDidLoadState += OnLoadState;

            if(OnDidSetStory != null) OnDidSetStory(story);
			
			// Recalculate function ink variables
			foreach(var input in InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs) {
				if(input.type == FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable) {
					input.RefreshInkVariableValue(story);
				} else if(input.type == FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable) {
					input.RefreshInkListVariableValue(story);
				}
			}
			
			// Reobserve variables
			var variablesToObserve = new List<string>(InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames);
			InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
			InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Clear();
			foreach(var observedVariableName in variablesToObserve) {
				if(_story.variablesState.Contains(observedVariableName)) {
					var observedVariable = ObserveVariable(observedVariableName, true);
					observedVariable.AddValueState(_story.variablesState[observedVariableName]);
				}
			}

            RefreshVisibleHistory();
            RefreshVisibleVariables();

			if(!attached) 
				InkPlayerWindowState.Instance.lastStoryWasPlaying = true;
			InkPlayerWindowState.Save();
			
			if(playerParams.profileOnStart) isProfiling = true;

			PingAutomator();
		}


		static void PingAutomator () {
			if(playerParams.disablePlayControls) return;
			if(story == null) return;
            if(story.canContinue && playerOptions.continueAutomatically) {
				TryContinue();
			} else if(story.currentChoices.Count > 0 && playerOptions.chooseAutomatically) { 
				MakeRandomChoice();
			}
		}
		
		
		static void Stop () {
			Clear ();
		}
		
		// This function's role isn't clear. It's used both when restarting a story and when clearing it entirely. We should probably have two separate functions.
		static void Clear () {
			// InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Clear();
			
			// Clear the variable panel search
			RefreshVisibleVariables();
			
			story = null;
		}
		
		static void Restart () {
			Stop();
			if(storyJSONTextAsset != null)
				Play(storyJSONTextAsset);
			else if(storyJSON != null)
				Play(storyJSON);
			else
				Debug.LogError("Can't restart because no text asset or cached JSON exists");
		}

		static bool CanRestart() {
			return storyJSONTextAsset != null || storyJSON != null;
		}
		
		static void ContinueStory () {
			story.Continue();
		}

		static void AddStoryContent (string content, List<string> tags) {
			AddToHistory(InkHistoryContentItem.CreateForContent(content, tags));
			if(!playerParams.disableUndoHistory) AddToStateHistory();
		}
		
		static void AddToHistory (InkHistoryContentItem content) {
			storyHistory.Add(content);
            RefreshVisibleHistory();
            if(GetShouldAutoScrollOnStoryChange())
			    ScrollToBottom();
		}

		static void AddToStateHistory () {
			InkPlayerHistoryItem historyItem = new InkPlayerHistoryItem(story.state.ToJson(), new List<InkHistoryContentItem>(storyHistory));
			storyStateHistory.AddToUndoHistory(historyItem);
		}
		
		static void ClearStoryHistory () {
			storyHistory.Clear();
			storyStateHistory.Clear();
			RefreshVisibleHistory();
			ScrollToBottom();
		}

		static void CopyStoryHistoryToClipboard () {
			StringBuilder sb = new StringBuilder("Story Log\n");
			foreach(InkHistoryContentItem content in storyHistory) {
				sb.AppendLine();
				sb.Append(content.time.ToShortDateString());
				sb.Append(" ");
				sb.Append(content.time.ToLongTimeString());
				sb.Append(" (");
				sb.Append(content.contentType.ToString());
				sb.Append(") ");
				sb.Append(content.content);
			}
			GUIUtility.systemCopyBuffer = sb.ToString();
		}
		static void Undo () {
			InkPlayerHistoryItem item = storyStateHistory.Undo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkHistoryContentItem>(item.storyHistory);
            RefreshVisibleHistory();
            if(GetShouldAutoScrollOnStoryChange())
                ScrollToBottom();
		}
		
		static void Redo () {
			InkPlayerHistoryItem item = storyStateHistory.Redo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkHistoryContentItem>(item.storyHistory);
			RefreshVisibleHistory();
            if(GetShouldAutoScrollOnStoryChange())
                ScrollToBottom();
		}

		static void SaveStoryState (string storyStateJSON) {
			AddToHistory(InkHistoryContentItem.CreateForDebugNote("Saved state"));

			// Text asset can be null if we attached to an existing story rather than loading our own
			string dirPath = string.Empty;
			string storyName = "story";
			if( storyJSONTextAsset != null ) {
				dirPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(storyJSONTextAsset));
				storyName = storyJSONTextAsset.name;
			}

			var newStateAsset = InkEditorUtils.CreateStoryStateTextFile(storyStateJSON, dirPath, storyName+"_SaveState");
			if(!playerParams.disableStateLoading)
				storyStateTextAsset = newStateAsset;
		}

		static void LoadStoryState (string storyStateJSON) {
			storyHistory.Clear();
			storyStateHistory.Clear();
			AddToHistory(InkHistoryContentItem.CreateForDebugNote("Loaded state"));
			story.state.LoadJson(storyStateJSON);
		}

        static bool GetShouldAutoScrollOnStoryChange () {
            if(InkPlayerWindowState.Instance.storyPanelState.autoScrollMode == StoryPanelState.AutoScrollMode.Off) return false;
            if(InkPlayerWindowState.Instance.storyPanelState.autoScrollMode == StoryPanelState.AutoScrollMode.Always) return true;
            if(InkPlayerWindowState.Instance.storyPanelState.autoScrollMode == StoryPanelState.AutoScrollMode.WhenAtBottom && storyScrollSnappedToBottom) return true;
            return false;
        }

		static void ScrollToBottom (bool instant = false) {
			storyScrollMarkedForBottom = instant ? AutoScrollSmoothingMode.Snap : AutoScrollSmoothingMode.Smooth;
		}

		static void ScrollToSelectedLine (bool instant = false) {
			storyScrollMarkedForSelectedLine = instant ? AutoScrollSmoothingMode.Snap : AutoScrollSmoothingMode.Smooth;
		}

		static void TryContinue () {
			if(!story.canContinue) 
				return;
			// if(playerOptions.continueAutomatically) {
			// 	while (story.canContinue) {
			// 		ContinueStory();
			// 	}
			// } else {
				ContinueStory();
			// }
		}
        void OnGUI () {
			HandleDragAndDrop();
			if(searchTextFieldStyle == null) searchTextFieldStyle = GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.FindStyle("ToolbarSeachTextField");
			if(searchCancelButtonStyle == null) searchCancelButtonStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSeachCancelButton");

			dateTimeNow = DateTime.Now;
			var time = Time.realtimeSinceStartup;
			var deltaTime = 0f;
			if(lastOnGUITime != -1)
				deltaTime = time - lastOnGUITime;
			lastOnGUITime = time;
			
			if(doingAutoscroll) {
				var newY = Mathf.SmoothDamp(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y, autoscrollTarget, ref autoscrollVelocity, autoscrollSmoothTime, Mathf.Infinity, deltaTime);
				InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, newY);
				if(Mathf.Abs(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y - autoscrollTarget) < 0.1f) {
                    doingAutoscroll = false;
                }
			}

			this.Repaint();
			
			var lw = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 200;

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			if(story == null && attached) 
				DetachInstance();

			DisplayHeader();

			if(playStoryException == null) {
				DisplayStoryControlGroup();
			} else {
				DisplayErrors();
			}

			if(story != null && story.state != null) {
				DrawStoryHistory();
				DrawChoices();
				DrawStoryState();
				DrawProfilerData();
				DrawSaveLoad();
				DrawNamedContent();
				// DrawDiverts();
				DrawFunctions();
				DrawVariables();
				InkPlayerWindowState.Save();
			} else {
				// EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
				GUILayout.Space(40);
				EditorGUILayout.LabelField("Use this window to play and debug ink stories", EditorStyles.boldLabel);
				GUILayout.Space(10);
				EditorGUILayout.LabelField("You can tether your story as it runs in Play Mode to this window, allowing you to view and edit variables and divert in real time!", EditorStyles.wordWrappedLabel);
				GUILayout.Space(20);
				if(GUILayout.Button("View Documentation")) {
					Application.OpenURL("https://github.com/inkle/ink-unity-integration/blob/master/Documentation/InkPlayerWindow.md");
				}
				// EditorGUILayout.EndVertical();
			}

            var lastRectMaxY = GUILayoutUtility.GetLastRect().yMax;
            EditorGUILayout.EndScrollView();
            if (Event.current.type == EventType.Repaint) {
                var scrollRectMaxY = GUILayoutUtility.GetLastRect().yMax;
                mainScrollViewActive = lastRectMaxY >= (scrollRectMaxY-3);
            }

			EditorGUIUtility.labelWidth = lw;
		}
		
		void DisplayHeader () {
			if(attached) {
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				var headerTitle = new System.Text.StringBuilder("Attached");
				if(attachedWhileInPlayMode != EditorApplication.isPlaying) {
                    playerParams = InkPlayerParams.Standard;
					if(attachedWhileInPlayMode) headerTitle.Append(" (Ex-play-mode story)");
				}
				GUILayout.Label(new GUIContent(headerTitle.ToString(), attachedStoryIcon, "This story reference has been attached from elsewhere"));
				if (GUILayout.Button(new GUIContent("Detach", "Detach from the loaded external story"), EditorStyles.toolbarButton)) {
					Detach();
				}
				EditorGUILayout.EndHorizontal();
			} else {
				EditorGUILayout.BeginVertical();
				EditorGUI.BeginChangeCheck();
				storyJSONTextAsset = EditorGUILayout.ObjectField("Story JSON", storyJSONTextAsset, typeof(TextAsset), false) as TextAsset;
				if(EditorGUI.EndChangeCheck()) {
					if(storyJSONTextAsset == null) {
						story = null;
					} else {
						Stop();
						Play(storyJSONTextAsset);
					}
				}
				if(CanRestart()) {
					string fullJSONFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(AssetDatabase.GetAssetPath(storyJSONTextAsset));
					var updatedStoryJSONLastEditDateTime = File.GetLastWriteTime(fullJSONFilePath);
					if (currentStoryJSONLastEditDateTime != updatedStoryJSONLastEditDateTime ) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox ("Story JSON file has changed. Reload or restart to play updated story.", MessageType.Warning);
						EditorGUILayout.BeginVertical();
						if(GUILayout.Button(new GUIContent("Reload", restartIcon, "Reload and restart the current story, which has been updated."))) {
							var storyStateJSON = story.state.ToJson();
							Play(storyJSONTextAsset, InkPlayerWindow.playerParams);
							story.state.LoadJson(storyStateJSON);
						}
						if(GUILayout.Button(new GUIContent("Restart", restartIcon, "Restart the current story"))) {
							Restart();
						}
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.EndVertical();
			}
		}
		
		void DisplayErrors () {
			EditorGUILayout.HelpBox(playStoryException.Message, MessageType.Error);
		}

		void DisplayStoryControlGroup () {
			EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);

			if(story == null) {
				EditorGUI.BeginDisabledGroup(storyJSONTextAsset == null);
				if(GUILayout.Button(new GUIContent("Start", playIcon, "Run the story"), EditorStyles.toolbarButton)) {
					var playerParams = InkPlayerParams.Standard;
					playerParams.profileOnStart = InkPlayerWindow.playerParams.profileOnStart;
					Play(storyJSONTextAsset, playerParams);
				}
				EditorGUI.EndDisabledGroup();
			} else {
				EditorGUI.BeginDisabledGroup(playerParams.disablePlayControls);
				if(GUILayout.Button(new GUIContent("Stop", stopIcon, "Stop the story"), EditorStyles.toolbarButton)) {
					Stop();
				}
				EditorGUI.BeginDisabledGroup(!CanRestart());
				if(GUILayout.Button(new GUIContent("Restart", restartIcon, "Restarts the story"), EditorStyles.toolbarButton)) {
					Restart();
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}

			GUILayout.FlexibleSpace();

			// Profiler button
			if( story != null ) {
				var profileButtonTitle = new GUIContent(
					isProfiling ? "Stop Profiling" : "Profile", 
					profilerIcon,
					(isProfiling ? "Stop" : "Start") + " Profiling");
				isProfiling = GUILayout.Toggle(isProfiling, profileButtonTitle, EditorStyles.toolbarButton);

			} else {
				var profileButtonTitle = new GUIContent("Profile On Start", profilerIcon, "Immediately start the profiler when the story starts.");
				playerParams.profileOnStart = GUILayout.Toggle(playerParams.profileOnStart, profileButtonTitle, EditorStyles.toolbarButton);
			}
			GUILayout.FlexibleSpace();
				
			// Undo/Redo
			if(story != null) {
				EditorGUI.BeginDisabledGroup(playerParams.disableUndoHistory || !storyStateHistory.canUndo);
				if(GUILayout.Button(new GUIContent("Undo", undoIcon, "Undo the last continue or choice"), EditorStyles.toolbarButton, GUILayout.Width(46))) {
					Undo();
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(playerParams.disableUndoHistory || !storyStateHistory.canRedo);
				if(GUILayout.Button(new GUIContent("Redo", redoIcon, "Redo the last continue or choice"), EditorStyles.toolbarButton, GUILayout.Width(46))) {
					Redo();
				}
				EditorGUI.EndDisabledGroup();
			}

			GUILayout.FlexibleSpace();

			EditorGUI.BeginDisabledGroup(playerParams.disablePlayControls);
			EditorGUI.BeginChangeCheck();
			var newContinueAutomatically = GUILayout.Toggle(playerOptions.continueAutomatically && !playerParams.disablePlayControls, new GUIContent("Auto-Continue", "Continues content automatically"), EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
				if(!playerParams.disablePlayControls) playerOptions.continueAutomatically = newContinueAutomatically;
				PingAutomator();
			}

			EditorGUI.BeginChangeCheck();
			var newChooseAutomatically = GUILayout.Toggle(playerOptions.chooseAutomatically && !playerParams.disablePlayControls, new GUIContent("Auto-Choice", "Makes choices automatically"), EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
				if(!playerParams.disablePlayControls) playerOptions.chooseAutomatically = newChooseAutomatically;
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.LabelField(new GUIContent(timeIntervalIcon, "Time between automatic choices"), GUILayout.Width(16));
			playerOptions.continueAutomaticallyTimeInterval = playerOptions.chooseAutomaticallyTimeInterval = GUILayout.HorizontalSlider(playerOptions.continueAutomaticallyTimeInterval, 1f, 0f, GUILayout.Width(80));
			GUILayout.EndHorizontal();
		}
			



		
		#region Story
		void DrawStoryHistory () {
			DisplayStoryHeader();
			if(InkPlayerWindowState.Instance.storyPanelState.showing)
				DisplayStoryBody ();
		}

		void DisplayStoryHeader () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			
			InkPlayerWindowState.Instance.storyPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.storyPanelState.showing, "Content", true);
			
			if(GUILayout.Button(new GUIContent("Clear", clearIcon, "Clears the output"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) {
				ClearStoryHistory();
			}
			GUILayout.Space(12);
			if(GUILayout.Button(new GUIContent("Copy", /*copyToClipboardIcon, */"Copy the output to clipboard"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) {
				CopyStoryHistoryToClipboard();
			}

			GUILayout.Space(12);
            {
                var lw = EditorGUIUtility.labelWidth;
                var autoScrollGUIContent = EditorGUIUtility.IconContent("ScrollRect Icon");
                autoScrollGUIContent.tooltip = "Autoscroll Mode";
                EditorGUIUtility.labelWidth = 20;
                InkPlayerWindowState.Instance.storyPanelState.autoScrollMode = (StoryPanelState.AutoScrollMode)EditorGUILayout.EnumPopup(autoScrollGUIContent, InkPlayerWindowState.Instance.storyPanelState.autoScrollMode, EditorStyles.toolbarPopup, GUILayout.Width(130));
                EditorGUIUtility.labelWidth = lw;
            }
            
			
			GUILayout.Space(12);
			EditorGUI.BeginChangeCheck();
            DrawVisibilityOptions();
			if(EditorGUI.EndChangeCheck()) {
                RefreshVisibleHistory();
                if(GetShouldAutoScrollOnStoryChange())
				    ScrollToBottom();
			}
            void DrawVisibilityOptions () {
                var lw = EditorGUIUtility.labelWidth;
                var visibilityOptionsGUIContent = EditorGUIUtility.IconContent("d_ViewToolOrbit");
                visibilityOptionsGUIContent.tooltip = "Visiblity Options";
                EditorGUIUtility.labelWidth = 20;
                Enum newVisibilityOptions = EditorGUILayout.EnumFlagsField(visibilityOptionsGUIContent, InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions, EditorStyles.toolbarDropDown, GUILayout.Width(80));
                EditorGUIUtility.labelWidth = lw;
                InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions = (DisplayOptions.VisibilityOptions)(int)Convert.ChangeType(newVisibilityOptions, typeof(DisplayOptions.VisibilityOptions));

                // TODO: tooltips for options. I'd REALLY like for it not to show "Mixed ..." in the box mais c'est la vie
                // TODO: Add a "default" option in the dropdown
                // See:
                // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorGUI.cs#L3561
                // But a lot of the code is internal.

                // var enumValue = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
                // var style = EditorStyles.toolbarDropDown;
                // var position = EditorGUILayout.GetControlRect(true, 18, style, GUILayout.Width(80));
                
                // var enumType = typeof(DisplayOptions.VisibilityOptions);
                // var includeObsolete = false;
                // var label = GUIContent.none;
                // var displayNames = Enum.GetNames(typeof(DisplayOptions.VisibilityOptions));
                // // var flagValues = new int[];

                // var id = GUIUtility.GetControlID(0, FocusType.Keyboard, position);
                // position = EditorGUI.PrefixLabel(position, id, label);

                // InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions = (DisplayOptions.VisibilityOptions)EditorGUI.MaskField(position, (int)enumValue, displayNames, style);
            }

			bool changed = DrawSearchBar(ref InkPlayerWindowState.Instance.storyPanelState.searchString);
			if(changed) {
			    RefreshVisibleHistory();
				if(selectedLine != null) ScrollToSelectedLine(true);
				else ScrollToBottom();
			}

			EditorGUILayout.EndHorizontal();
		}

		static bool ShouldShowContentWithSearchString (string contentString, string searchString) {
			if(StringContains(contentString, searchString, StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		static bool ShouldShowContent (InkHistoryContentItem content, DisplayOptions.VisibilityOptions visibilityOpts) {
			switch(content.contentType) {
				case InkHistoryContentItem.ContentType.PresentedContent: {
					if((visibilityOpts & DisplayOptions.VisibilityOptions.Content) != 0) {
						if(content.content.Length == 0 && (visibilityOpts & DisplayOptions.VisibilityOptions.EmptyEntries) == 0) return false;
						else return true;
					} else return false;
				}
				case InkHistoryContentItem.ContentType.ChooseChoice:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.SelectedChoice) != 0;
				case InkHistoryContentItem.ContentType.PresentedChoice:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.PresentedChoices) != 0;
				case InkHistoryContentItem.ContentType.DebugNote:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.DebugNotes) != 0;
				case InkHistoryContentItem.ContentType.Warning:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.Warnings) != 0;
				case InkHistoryContentItem.ContentType.Error:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.Errors) != 0;
				case InkHistoryContentItem.ContentType.ChoosePathString:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.ChoosePathString) != 0;
				case InkHistoryContentItem.ContentType.EvaluateFunction:
					return (visibilityOpts & DisplayOptions.VisibilityOptions.Function) != 0;
				default: break;
			}
			return false;
		}
		
		static List<InkHistoryContentItem> visibleHistory = new List<InkHistoryContentItem>();
		static void RefreshVisibleHistory () {
			visibleHistory.Clear();
			bool doingSearch = !string.IsNullOrWhiteSpace(InkPlayerWindowState.Instance.storyPanelState.searchString);
			var visibilityOpts = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
			var count = storyHistory.Count;
			for(int i = 0; i < count; i++) {
				var content = storyHistory[i];
				if(doingSearch && !ShouldShowContentWithSearchString(content.content, InkPlayerWindowState.Instance.storyPanelState.searchString)) continue;
				if(!ShouldShowContent(content, visibilityOpts)) continue;
				visibleHistory.Add(content);
			}
		}

        void DisplayStoryBody () {	
			float contentMarginXY = 4;
			float contentSpacing = 8;
			
			var timestampWidth = 58;
			var contentTypeWidth = 26;
			var tagsWidth = 160;

			var visibilityOptions = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
			bool showTimestamp = (visibilityOptions & DisplayOptions.VisibilityOptions.TimeStamp) != 0; 
			bool showTags = (visibilityOptions & DisplayOptions.VisibilityOptions.Tags) != 0; 

			var lastRect = GUILayoutUtility.GetLastRect();
			var containerWidth = position.width - GUI.skin.verticalScrollbar.fixedWidth;
            if(mainScrollViewActive) containerWidth -= GUI.skin.verticalScrollbar.fixedWidth;
			
			var lineWidth = containerWidth - contentMarginXY * 2;
			
			var contentWidth = lineWidth;
			if(showTimestamp) {
				contentWidth -= timestampWidth;
				contentWidth -= contentSpacing;
			}
			contentWidth -= contentTypeWidth;
			contentWidth -= contentSpacing;
			if(showTags) {
				contentWidth -= tagsWidth;
				contentWidth -= contentSpacing;
			}
			float totalHeight = 0;
			float[] heights = new float[storyHistory.Count];
			int selectedLineIndex = -1;
			float selectedLineY = -1;

			for(int i = 0; i < visibleHistory.Count; i++) {
				var content = visibleHistory[i];
				heights[i] = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(content.content), contentWidth);
				if(showTags) {
					var tagsHeight = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(GetTagsString(content.tags)), tagsWidth);
					heights[i] = Mathf.Max(heights[i], tagsHeight);
				}
				heights[i] += contentMarginXY * 2;
				if(content == selectedLine) {
					selectedLineIndex = i;
					selectedLineY = totalHeight;
				}
				totalHeight += heights[i];
			}

            void OnRefreshSelectedLine () {
                float _totalHeight = 0;
                for(int i = 0; i < visibleHistory.Count; i++) {
                    var content = visibleHistory[i];
                    if(content == selectedLine) {
                        selectedLineIndex = i;
                        selectedLineY = _totalHeight;
                    }
                    _totalHeight += heights[i];
                }
            }

			if(Event.current.type == EventType.Repaint) {
				InkPlayerWindowState.Instance.storyPanelState.y = lastRect.yMax;
			}

			var viewportRect = new Rect(0, lastRect.yMax, position.width, InkPlayerWindowState.Instance.storyPanelState.height);
            if(mainScrollViewActive) viewportRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
			var containerRect = new Rect(0,0,containerWidth, totalHeight);


            var showScrollToBottomButton = (totalHeight - viewportRect.height) - (doingAutoscroll ? autoscrollTarget : InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y) > 100;
            var scrollToBottomButtonRect = new Rect(viewportRect.center.x - 80, viewportRect.yMax-40, 160, 32);
            
            if(showScrollToBottomButton) {
                EditorGUIUtility.AddCursorRect(scrollToBottomButtonRect, MouseCursor.Link);
                if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && scrollToBottomButtonRect.Contains(Event.current.mousePosition)) {
                    GUI.FocusControl(null);
                    ScrollToBottom();
                    Event.current.Use();
                }
            }

            storyScrollRectBottom = totalHeight - viewportRect.height;

			
			var newScrollPos = GUI.BeginScrollView(viewportRect, InkPlayerWindowState.Instance.storyPanelState.scrollPosition, containerRect, false, true);
			if(newScrollPos != InkPlayerWindowState.Instance.storyPanelState.scrollPosition) {
				doingAutoscroll = false;
				InkPlayerWindowState.Instance.storyPanelState.scrollPosition = newScrollPos;
                storyScrollSnappedToBottom = storyScrollRectBottom - InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y < 0.1f;
			}

			var y = 0f;
			var panelTop = InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y;
			var panelBottom = InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y + viewportRect.height;
			// int numShown = 0;
			// var log = "";

			// This appears to be necessary, else the selected text moves around when scrolling!
			// if(doingAutoscroll) {
			// 	GUI.FocusControl(null);
			// }
            


			for(int i = 0; i < visibleHistory.Count; i++) {
				var endY = y + heights[i];
				if(panelTop <= endY && panelBottom >= y) {
					// if(numShown == 0) {
					//     log += "Total space "+totalHeight+" Scroll "+InkPlayerWindowState.Instance.storyPanelState.storyScrollPosition.y+" Space "+y+", showing: ";
					// }
					var content = visibleHistory[i];
					var lineContainerRect = new Rect(0, y, containerWidth, heights[i]);
					var lineRect = new Rect(lineContainerRect.x + contentMarginXY, lineContainerRect.y + contentMarginXY, lineContainerRect.width - contentMarginXY * 2, lineContainerRect.height - contentMarginXY * 2);
					
					GUIStyle lineStyle = null;
					if(selectedLine == content) lineStyle = historyItemBGStyleSelected.guiStyle;
					else lineStyle = i % 2 == 0 ? historyItemBGStyleDark.guiStyle : historyItemBGStyleLight.guiStyle;
					
					GUI.Box(lineContainerRect, GUIContent.none, lineStyle);
                    EditorGUIUtility.AddCursorRect(lineContainerRect, MouseCursor.ArrowPlus);
					if(Event.current.type == EventType.MouseDown && lineContainerRect.Contains(Event.current.mousePosition)) {
						if(Event.current.button == 0) {
							selectedLine = content;
                            OnRefreshSelectedLine();
                            // To avoid disruption, only scroll when the line is close to the edge of the panel
                            var targetY = GetTargetScrollPositionToCenterStoryLine(i, false);
                            if(Mathf.Abs(targetY-InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y) > viewportRect.height * 0.225f) {
                                ScrollToSelectedLine();
                            }
                            Event.current.Use();
						}
					}
					string textToDisplay = String.Empty;
					
					var x = lineRect.x;
					if(showTimestamp) {
						var timeRect = new Rect(x, lineRect.y, timestampWidth, lineRect.height);
						DisplayLineTime(timeRect, content);
						x += timestampWidth;
						x += contentSpacing;
					}
					var iconRect = new Rect(x, lineRect.y, contentTypeWidth, lineRect.height);
					DisplayLineIcon(iconRect, content);
					x += contentTypeWidth;
					x += contentSpacing;
					
					var contentRect = new Rect(x, lineRect.y, contentWidth, lineRect.height);
					DisplayLine(contentRect, content);
					x += contentWidth;
					x += contentSpacing;
					if(showTags) {
						var tagsRect = new Rect(x, lineRect.y, tagsWidth, lineRect.height);
						DisplayTags(tagsRect, content);
						x += tagsWidth;
						x += contentSpacing;
					}

					if(Event.current.type == EventType.MouseDown && lineContainerRect.Contains(Event.current.mousePosition)) {
                        if(Event.current.button == 1) {
							if(GUI.GetNameOfFocusedControl() != content.GetHashCode().ToString()) {
								GUI.FocusControl(null);
								var contextMenu = new GenericMenu();
								contextMenu.AddItem(new GUIContent("Copy"), false, () => {
									GUIUtility.systemCopyBuffer = content.content;
								});
                                foreach(var contextMenuDelegate in contextMenuDelegates) {
                                    contextMenuDelegate(contextMenu, content);
                                }
								contextMenu.ShowAsContext();
								Event.current.Use();
							}
						}
					}

					// log += i+", ";
				}
				y = endY;
			}

			var lineX = contentMarginXY;
			if(showTimestamp) {
				lineX += timestampWidth;
				lineX += contentSpacing * 0.5f;
				GUI.Box(new Rect(lineX, 0, 1, containerRect.height), "", dividerLineStyle.guiStyle);
				lineX += contentSpacing * 0.5f;
			}

			lineX += contentTypeWidth;
			lineX += contentSpacing * 0.5f;
			GUI.Box(new Rect(lineX, 0, 1, containerRect.height), "", dividerLineStyle.guiStyle);
			lineX += contentSpacing * 0.5f;
			
			if(showTags) {
				lineX += contentWidth;
				lineX += contentSpacing * 0.5f;
				GUI.Box(new Rect(lineX, 0, 1, containerRect.height), "", dividerLineStyle.guiStyle);
				lineX += contentSpacing * 0.5f;
			}

			GUI.EndScrollView();

            if(showScrollToBottomButton) {
                GUI.Box(scrollToBottomButtonRect, "Scroll to bottom", GUI.skin.button);
            }

			GUILayout.Space(viewportRect.height);



			if(Event.current.type == EventType.Layout) {
				if(storyScrollMarkedForBottom != AutoScrollSmoothingMode.NONE) {
					var targetPosition = totalHeight - viewportRect.height;
					if(storyScrollMarkedForBottom == AutoScrollSmoothingMode.Smooth) {
						doingAutoscroll = true;
						autoscrollTarget = targetPosition;
					} else if(storyScrollMarkedForBottom == AutoScrollSmoothingMode.Snap) {
						doingAutoscroll = false;
						InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, targetPosition);
					}
					autoscrollVelocity = 0;
                    storyScrollSnappedToBottom = true;
					storyScrollMarkedForBottom = AutoScrollSmoothingMode.NONE;
				}
				if(storyScrollMarkedForSelectedLine != AutoScrollSmoothingMode.NONE && selectedLineIndex != -1) {
					var targetPosition = GetTargetScrollPositionToCenterStoryLine(selectedLineIndex);
					if(storyScrollMarkedForSelectedLine == AutoScrollSmoothingMode.Smooth) {
						doingAutoscroll = true;
						autoscrollTarget = targetPosition;
					} else if(storyScrollMarkedForSelectedLine == AutoScrollSmoothingMode.Snap) {
						doingAutoscroll = false;
						InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, targetPosition);
					}
					autoscrollVelocity = 0;
                    storyScrollSnappedToBottom = (storyScrollRectBottom+viewportRect.height * 0.5f) - targetPosition < 0.1f;
					storyScrollMarkedForSelectedLine = AutoScrollSmoothingMode.NONE;
				}
			}

			float GetTargetScrollPositionToCenterStoryLine (int lineIndex, bool clamped = true) {
				var targetY = (selectedLineY + heights[lineIndex] * 0.5f) - viewportRect.height * 0.5f;
				if(clamped) targetY = Mathf.Clamp(targetY, 0, totalHeight - viewportRect.height);
				return targetY;
			}
		}

		static ColoredBackgroundGUIStyle _historyItemBGStyleDark;
		static ColoredBackgroundGUIStyle historyItemBGStyleDark {
			get {
				if(_historyItemBGStyleDark == null) _historyItemBGStyleDark = new ColoredBackgroundGUIStyle(new Color(0.8470589f,0.8470589f,0.8470589f,1), new Color(0.21f,0.21f,0.21f,1f), new Color(0.92f,0.92f,0.92f,1), new Color(0.3f,0.3f,0.3f,1f));
				return _historyItemBGStyleDark;
			}
		}

		static ColoredBackgroundGUIStyle _historyItemBGStyleLight;
		static ColoredBackgroundGUIStyle historyItemBGStyleLight {
			get {
				if(_historyItemBGStyleLight == null) _historyItemBGStyleLight = new ColoredBackgroundGUIStyle(new Color(0.8745099f,0.8745099f,0.8745099f,1f), new Color(0.23f,0.23f,0.23f,1f), new Color(0.92f,0.92f,0.92f,1), new Color(0.3f,0.3f,0.3f,1f));
				return _historyItemBGStyleLight;
			}
		}

		static ColoredBackgroundGUIStyle _historyItemBGStyleSelected;
		static ColoredBackgroundGUIStyle historyItemBGStyleSelected {
			get {
				if(_historyItemBGStyleSelected == null) _historyItemBGStyleSelected = new ColoredBackgroundGUIStyle(new Color(0.3920879f,0.6161963f,0.9339623f,1f), new Color(0.243137255f,0.37254902f,0.588235294f,1f));
				return _historyItemBGStyleSelected;
			}
		}

		static ColoredBackgroundGUIStyle _dividerLineStyle;
		static ColoredBackgroundGUIStyle dividerLineStyle {
			get {
				if(_dividerLineStyle == null) _dividerLineStyle = new ColoredBackgroundGUIStyle(new Color(0.85f,0.85f,0.85f,1f), new Color(0.25f,0.25f,0.25f,1f));
				return _dividerLineStyle;
			}
		}

		void DisplayLineTime (Rect rect, InkHistoryContentItem content) {
			EditorGUI.LabelField(rect, new GUIContent(content.time.ToLongTimeString()));
		}

		void DisplayLineIcon (Rect rect, InkHistoryContentItem content) {
			var visibilityOptions = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
			if(content.contentType == InkHistoryContentItem.ContentType.ChooseChoice && (visibilityOptions & DisplayOptions.VisibilityOptions.SelectedChoice) != 0) {
				var icon = new GUIContent("*", "Selected Choice");
				EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.PresentedChoice && (visibilityOptions & DisplayOptions.VisibilityOptions.PresentedChoices) != 0) {
				var icon = new GUIContent("*?", "Presented Choice");
				EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.EvaluateFunction && (visibilityOptions & DisplayOptions.VisibilityOptions.Function) != 0) {
				var icon = new GUIContent("f(x)", "Took Function");
				EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.ChoosePathString && (visibilityOptions & DisplayOptions.VisibilityOptions.ChoosePathString) != 0) {
				var icon = new GUIContent("->", "Took Path");
				EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.DebugNote && (visibilityOptions & DisplayOptions.VisibilityOptions.DebugNotes) != 0) {
				var icon = new GUIContent("//", "Debug Note");
				EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.Warning && (visibilityOptions & DisplayOptions.VisibilityOptions.Warnings) != 0) {
				var icon = EditorGUIUtility.IconContent("console.warnicon.sml");
				icon.tooltip = "Warning";
				EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.Error && (visibilityOptions & DisplayOptions.VisibilityOptions.Errors) != 0) {
				var icon = EditorGUIUtility.IconContent("console.erroricon.sml");
				icon.tooltip = "Error";
				EditorGUI.LabelField(rect, icon);
			} else {
				// var icon = EditorGUIUtility.IconContent("console.infoicon.sml");
				// EditorGUI.LabelField(rect, icon);
			}
		}

		void DisplayLine (Rect rect, InkHistoryContentItem content) {
			if (content.content.Length == 0) return;
			float timeSinceLastWrite = (float)(dateTimeNow - content.time).TotalSeconds;
			var revealTime = 0.8f;
			var l = Mathf.InverseLerp(revealTime, 0, timeSinceLastWrite);
			var newColor = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0);
			var color = Color.Lerp(GUI.color, newColor, l);
			var oldGUIColor = GUI.color; 
			GUI.color = color;
			GUI.SetNextControlName(content.GetHashCode().ToString());
			EditorGUI.SelectableLabel(rect, content.content, EditorStyles.wordWrappedLabel);
			GUI.color = oldGUIColor;
		}

		void DisplayTags (Rect rect, InkHistoryContentItem content) {
			if (content.tags == null || content.tags.Count == 0) return;
			float timeSinceLastWrite = (float)(dateTimeNow - content.time).TotalSeconds;
			var revealTime = 0.8f;
			var l = Mathf.InverseLerp(revealTime, 0, timeSinceLastWrite);
			var newColor = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0);
			var color = Color.Lerp(GUI.color, newColor, l);
			var oldGUIColor = GUI.color; 
			GUI.color = color;
			GUI.SetNextControlName(content.GetHashCode().ToString());
			EditorGUI.SelectableLabel(rect, GetTagsString(content.tags), EditorStyles.wordWrappedLabel);
			GUI.color = oldGUIColor;
		}

		string GetTagsString (List<string> tags) {
			return (tags == null || tags.Count == 0) ? string.Empty : string.Join("\n", tags);
		}
		#endregion






		#region Choices
		void DrawChoices () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.choicePanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.choicePanelState.showing, "Choices", true);
			EditorGUILayout.EndHorizontal();
			if(InkPlayerWindowState.Instance.choicePanelState.showing)
				DisplayChoices ();
		}

		void DisplayChoices () {
			EditorGUI.BeginDisabledGroup(playerParams.disableChoices);
			GUILayout.BeginVertical();
			if(story.canContinue) {
				EditorGUI.BeginDisabledGroup(playerOptions.continueAutomatically);
				if(GUILayout.Button(new GUIContent("Continue", "Continues once"))) {
					ContinueStory();
				}
				if(GUILayout.Button(new GUIContent("Continue Maximally", "Continues until the next choice"))) {
					while(story.canContinue) {
						ContinueStory();
					}
				}
				EditorGUI.EndDisabledGroup();
			} else if(story.currentChoices.Count > 0) {
				EditorGUI.BeginDisabledGroup(playerOptions.chooseAutomatically);
				foreach(Choice choice in story.currentChoices) {
					GUILayout.BeginHorizontal();
					StringBuilder sb = new StringBuilder();
					sb.Append("Index: ");
					sb.AppendLine(choice.index.ToString());
					sb.Append("Tags: ");
					if (choice.tags == null) {
						sb.Append("NONE");
					} else {
						for (var index = 0; index < choice.tags.Count; index++) {
							var tag = choice.tags[index];
							sb.Append(tag);
							if (index < choice.tags.Count - 1) sb.Append(", ");
							else sb.AppendLine();
						}
					}
					sb.Append("SourcePath: ");
					sb.Append(choice.sourcePath.Trim());
					if(GUILayout.Button(new GUIContent(choice.text.Trim(), sb.ToString()))) {
						MakeChoice(choice);
					}
					GUILayout.EndHorizontal();
				}
				EditorGUI.EndDisabledGroup();
			} else {
				GUILayout.Label("Reached end of story");
			}

			GUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		static void MakeRandomChoice () {
			MakeChoice(story.currentChoices[UnityEngine.Random.Range(0, story.currentChoices.Count)]);
		}

		static void MakeChoice (Choice choice) {
			if(OnChooseChoiceIndexOverride != null) OnChooseChoiceIndexOverride(choice.index);
			else story.ChooseChoiceIndex(choice.index);
			if(!playerParams.disableUndoHistory) AddToStateHistory();
			TryContinue();
		}
		#endregion
		




		#region StoryState
		static void DrawStoryState () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.storyStatePanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.storyStatePanelState.showing, "Story State", true);
			EditorGUILayout.EndHorizontal();
			if(InkPlayerWindowState.Instance.storyStatePanelState.showing)
				DrawStoryStatePanel ();
		}

		static void DrawStoryStatePanel () {
			GUILayout.BeginVertical();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Story Seed");
			EditorGUILayout.LabelField(story.state.storySeed.ToString());
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Current Turn Index");
			EditorGUILayout.LabelField(story.state.currentTurnIndex.ToString());
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Current Path String");
			if(story.canContinue) EditorGUILayout.LabelField(story.state.currentPathString);
			else EditorGUILayout.LabelField("(Always blank when story.canContinue is false)");
			EditorGUILayout.EndHorizontal();

			GUILayout.EndVertical();
		}
		#endregion



		#region SaveLoad
		static void DrawSaveLoad () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.saveLoadPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.saveLoadPanelState.showing, "Save/Load", true);
			EditorGUILayout.EndHorizontal();
			if(InkPlayerWindowState.Instance.saveLoadPanelState.showing)
				DrawSaveLoadPanel ();
		}

		static void DrawSaveLoadPanel () {
			GUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Copy To Clipboard")) {
				GUIUtility.systemCopyBuffer = InkEditorUtils.FormatJson(story.state.ToJson());
			}
			if (GUILayout.Button("Save As...")) {
				SaveStoryState(InkEditorUtils.FormatJson(story.state.ToJson()));
			}
			EditorGUILayout.EndHorizontal();

			EditorGUI.BeginDisabledGroup(playerParams.disableStateLoading);
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			storyStateTextAsset = EditorGUILayout.ObjectField("Load Story State JSON File", storyStateTextAsset, typeof(TextAsset), false) as TextAsset;
			EditorGUI.BeginDisabledGroup(storyStateTextAsset == null);
			if (GUILayout.Button("Load")) {
				LoadStoryState(storyStateTextAsset.text);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			if(storyStateTextAsset != null && !storyStateValid) {
				EditorGUILayout.HelpBox("Loaded story state file is not valid.", MessageType.Error);
			}
			GUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}
		#endregion




		

		#region Diverts
		void DrawNamedContent () {
			DrawNamedContentHeader();
			if(InkPlayerWindowState.Instance.namedContentPanelState.showing)
				DrawNamedContentPanel ();
		}

		void DrawNamedContentHeader () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.namedContentPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.namedContentPanelState.showing, "Named Content", true);
			
			EditorGUI.BeginDisabledGroup(!InkPlayerWindowState.Instance.namedContentPanelState.showing);
			bool changed = DrawSearchBar(ref InkPlayerWindowState.Instance.namedContentPanelState.searchString);
			if(changed) InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition = Vector2.zero;
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();
		}

        void DrawNamedContentPanel () {	
			float contentMarginX = 4;
			float contentMarginY = 0;
			float indentChangeVerticalSpacing = 6;
			
			var lastRect = GUILayoutUtility.GetLastRect();
			var containerWidth = position.width - GUI.skin.verticalScrollbar.fixedWidth;
            if(mainScrollViewActive) containerWidth -= GUI.skin.verticalScrollbar.fixedWidth;
			
			float totalHeight = 0;

			List<Rect> rects = new List<Rect>();
			List<string> paths = new List<string>();
            {
                AddContainer(string.Empty, story.mainContentContainer);
                void AddContainer (string currentPath, Container container, int indent = 0) {
                    if(container == null || container.namedOnlyContent == null) return;
                    
                    var lastTotalHeight = totalHeight;
                    indent++;
                    foreach(var contentKVP in container.namedOnlyContent) {
                        var newPath = currentPath.Length == 0 ? contentKVP.Key : currentPath+"."+contentKVP.Key;
                        AddContent(newPath, contentKVP, indent);
                    }
                    indent--;
                    if(lastTotalHeight != totalHeight) totalHeight += indentChangeVerticalSpacing;
                }
                void AddContent (string currentPath, KeyValuePair<string, Runtime.Object> contentKVP, int indent = 0) {
                    if(SearchStringMatch(currentPath, InkPlayerWindowState.Instance.namedContentPanelState.searchString)) {
                        var itemHeight = EditorGUIUtility.singleLineHeight;
                        itemHeight += contentMarginY * 2;

                        rects.Add(new Rect(indent*8, totalHeight, containerWidth-indent*8, itemHeight));
                        totalHeight += itemHeight;
                        paths.Add(currentPath);
                    }
                    
                    var namedContainer = contentKVP.Value as Container;
                    AddContainer(currentPath, namedContainer, indent);
                }
            }
            totalHeight -= indentChangeVerticalSpacing;

			if(Event.current.type == EventType.Repaint) {
				InkPlayerWindowState.Instance.namedContentPanelState.y = lastRect.yMax;
			}
			var viewportRect = new Rect(0, lastRect.yMax, position.width, InkPlayerWindowState.Instance.namedContentPanelState.height);
            if(mainScrollViewActive) viewportRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
			var containerRect = new Rect(0,0,containerWidth, totalHeight);
			
			var newScrollPos = GUI.BeginScrollView(viewportRect, InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition, containerRect, false, true);
			if(newScrollPos != InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition) {
				doingAutoscroll = false;
				InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition = newScrollPos;
			}

			var panelTop = InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition.y;
			var panelBottom = InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition.y + viewportRect.height;
            
			for(int i = 0; i < paths.Count; i++) {
				if(panelTop <= rects[i].yMax && panelBottom >= rects[i].yMin) {
					var content = paths[i];
					var lineContainerRect = rects[i];
					var lineRect = new Rect(lineContainerRect.x + contentMarginX, lineContainerRect.y + contentMarginY, lineContainerRect.width - contentMarginX * 2, lineContainerRect.height - contentMarginY * 2);		
					DrawNamedContentItem(lineRect, content);
				}
			}

			GUI.EndScrollView();
			GUILayout.Space(viewportRect.height);
		}

		static GUIContent defaultDivertLabel = new GUIContent("Divert");
		void DrawNamedContentItem (Rect rect, string currentPath) {
			EditorGUI.LabelField(rect, new GUIContent(currentPath, "Path"));
			EditorGUI.LabelField(new Rect(rect.xMax-200, rect.y, 32, rect.height), new GUIContent(story.state.VisitCountAtPathString(currentPath).ToString(), "Read count"));
			if(GUI.Button(new Rect(rect.xMax-168, rect.y, 80, rect.height), OnDivertLabelOverride == null ? defaultDivertLabel : OnDivertLabelOverride(currentPath))) {
                if(OnDivertOverride != null) {
					OnDivertOverride(currentPath);
				} else {
					// This is a bit horrible tbh. Not all the paths we show are valid for diverting, but we don't really have a way of testing it.
					// Additionally, doing this can brick the story, so it's important we prevent it.
					// We test by creating an entirely new story and running it, and checking for errors in the flow.
					// The result is that this function is really slow :(
					var hadError = false;
					try {
						// We might optimise this by caching story.ToJson() - we could use this in other places too.
						var tmpStory = new Story(story.ToJson());
						tmpStory.allowExternalFunctionFallbacks = true;
						var state = story.state.ToJson();
						tmpStory.state.LoadJson(state);
						tmpStory.ChoosePathString(currentPath);
						tmpStory.ContinueMaximally();
					} catch (Exception e) {
						Debug.LogWarning("Could not divert to "+currentPath+"! Only Knots and Stitches can be diverted to. Is this a function? Alternatively, the path might lead to an error, which we prevent from occuring in this tool to safeguard the state.\n"+e.ToString());
						hadError = true;
					} finally {
						if(!hadError) {
							story.ChoosePathString(currentPath);
							AddToHistory(InkHistoryContentItem.CreateForDebugNote("Diverted to '"+currentPath+"'"));
						}
					}
				}
            }

            if(Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) {
                var contextMenu = new GenericMenu();
                contextMenu.AddItem(new GUIContent("Copy Path"), false, () => {
                    GUIUtility.systemCopyBuffer = currentPath;
                });
                contextMenu.ShowAsContext();
                Event.current.Use();
            }
		}
		#endregion


		#region Functions
		void DrawFunctions () {
			DrawFunctionsHeader();
			if(InkPlayerWindowState.Instance.functionPanelState.showing)
				DrawFunctionsPanel ();
		}

		void DrawFunctionsHeader () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.functionPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.functionPanelState.showing, "Functions", true);
			EditorGUILayout.EndHorizontal();
		}

		void DrawFunctionsPanel () {
			GUILayout.BeginVertical();

			DrawFunctionInput();
			DrawFunctionOutput();

			GUILayout.EndVertical();
		}

		void DrawFunctionInput () {
			// TODO - Autocomplete function names using this, which I should add to Story.cs ( ask joe first! )	
			// public IEnumerable<string> allFunctionNames {
			//     get {
			//         return mainContentContainer.namedContent.Keys;
			//     }
			// }

			var functionParams = InkPlayerWindowState.Instance.functionPanelState.functionParams;

			GUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginChangeCheck();
			functionParams.functionName = EditorGUILayout.TextField("Function Name", functionParams.functionName);
			if(EditorGUI.EndChangeCheck()) {
				InkPlayerWindowState.Instance.functionPanelState.testedFunctionHash = 0;
				InkPlayerWindowState.Instance.functionPanelState.testedFunctionTime = default(DateTime);
				InkPlayerWindowState.Instance.functionPanelState.functionReturnValue = null;
			}
			functionInputList.DoLayoutList();
			bool functionIsValid = functionParams.functionName != String.Empty && story.HasFunction(functionParams.functionName);
			EditorGUI.BeginDisabledGroup(!functionIsValid);
			
			Story.ExternalFunction externalFunction;
			var isExternalFunction = story.TryGetExternalFunction(functionParams.functionName, out externalFunction);
			if (GUILayout.Button(new GUIContent(isExternalFunction ? "Execute (External)" : "Execute", "Runs the function"))) {
				AddToHistory(InkHistoryContentItem.CreateForDebugNote("Execute function '"+functionParams.functionName+"'"));
				string outputContent = null;
				object[] allInput = new object[functionParams.inputs.Count];
				for (int i = 0; i < functionParams.inputs.Count; i++) {
					var input = functionParams.inputs[i];
					object obj = null;
					switch(input.type) {
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Float:
						obj = input.floatValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Int:
						obj = input.intValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.String:
						obj = input.stringValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Bool:
						obj = input.boolValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable:
						obj = input.inkVariableValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable:
						obj = input.inkListVariableValue;
						break;
					}
					allInput[i] = obj;
				}
				if(isExternalFunction) {
					OnEvaluateFunction(functionParams.functionName, allInput);
					InkPlayerWindowState.Instance.functionPanelState.functionReturnValue = externalFunction (allInput);
					OnCompleteEvaluateFunction(functionParams.functionName, allInput, null, InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else {
					InkPlayerWindowState.Instance.functionPanelState.functionReturnValue = story.EvaluateFunction(functionParams.functionName, out outputContent, allInput);
				}
				if(outputContent != null)
					AddStoryContent(outputContent, null);

				InkPlayerWindowState.Instance.functionPanelState.testedFunctionHash = functionParams.GetInputHashCode();
				InkPlayerWindowState.Instance.functionPanelState.testedFunctionTime = dateTimeNow;
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndVertical();
		}

		void DrawFunctionOutput () {
			float timeSinceLastWrite = (float)(dateTimeNow - InkPlayerWindowState.Instance.functionPanelState.testedFunctionTime).TotalSeconds;
			var revealTime = 0.8f;
			var l = Mathf.InverseLerp(revealTime, 0, timeSinceLastWrite);
			var newColor = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0);
			var color = Color.Lerp(GUI.color, newColor, l);
			var oldGUIColor = GUI.color; 
			GUI.color = color;

			var functionParams = InkPlayerWindowState.Instance.functionPanelState.functionParams;

			bool functionIsValid = functionParams.functionName != String.Empty && story.HasFunction(functionParams.functionName);
			if(functionIsValid && functionParams.GetInputHashCode() == InkPlayerWindowState.Instance.functionPanelState.testedFunctionHash) {
				GUILayout.BeginVertical(GUI.skin.box);
				if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue == null) {
					EditorGUILayout.LabelField("Output (Null)");
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is string) {
					EditorGUILayout.TextField("Output (String)", (string)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is float) {
					EditorGUILayout.FloatField("Output (Float)", (float)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is int) {
					EditorGUILayout.IntField("Output (Int)", (int)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is bool) {
					EditorGUILayout.Toggle("Output (Bool)", (bool)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is InkList) {
					EditorGUILayoutInkListField(new GUIContent("Output (InkList)"), (InkList)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else {
					EditorGUILayout.LabelField("Function returned unexpected type "+InkPlayerWindowState.Instance.functionPanelState.functionReturnValue.GetType().Name+".");
				}
				GUILayout.EndVertical();
			}
			GUI.color = oldGUIColor;
		}

		void BuildFunctionInputList () {
			var functionParams = InkPlayerWindowState.Instance.functionPanelState.functionParams;

			functionInputList = new ReorderableList(functionParams.inputs, typeof(FunctionPanelState.FunctionParams.FunctionInput), true, true, true, true);
			functionInputList.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Inputs");
			};
			functionInputList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
			functionInputList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				var input = functionParams.inputs[index];
				Rect typeRect = new Rect(rect.x, rect.y, 80, EditorGUIUtility.singleLineHeight);
				input.type = (FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType)EditorGUI.EnumPopup(typeRect, input.type);
				Rect inputRect = new Rect(rect.x + 90, rect.y, rect.width - 90, EditorGUIUtility.singleLineHeight);
				switch(input.type) {
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Float:
					input.floatValue = EditorGUI.FloatField(inputRect, input.floatValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Int:
					input.intValue = EditorGUI.IntField(inputRect, input.intValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.String:
					input.stringValue = EditorGUI.TextField(inputRect, input.stringValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Bool:
					input.boolValue = EditorGUI.Toggle(inputRect, input.boolValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable:
					{
						var halfInput = new Rect(inputRect.x, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 5, inputRect.height);
						var halfInput2 = new Rect(inputRect.x + Mathf.RoundToInt(inputRect.width * 0.5f) + 5, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 10, inputRect.height);
						EditorGUI.BeginChangeCheck();
						input.inkVariablePath = EditorGUI.TextField(halfInput, input.inkVariablePath);
						if(EditorGUI.EndChangeCheck()) input.RefreshInkVariableValue(story);
						
						EditorGUI.BeginDisabledGroup(true);
						DrawVariable(halfInput2, GUIContent.none, input.inkVariableValue);
						EditorGUI.EndDisabledGroup();
					}
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable:
					{
						var halfInput = new Rect(inputRect.x, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 5, inputRect.height);
						var halfInput2 = new Rect(inputRect.x + Mathf.RoundToInt(inputRect.width * 0.5f) + 5, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 10, inputRect.height);
						EditorGUI.BeginChangeCheck();
						input.inkListVariablePath = EditorGUI.TextField(halfInput, input.inkListVariablePath);
						if(EditorGUI.EndChangeCheck()) input.RefreshInkListVariableValue(story);
						EditorGUI.BeginDisabledGroup(true);
						DrawVariable(halfInput2, GUIContent.none, input.inkListVariableValue);
						EditorGUI.EndDisabledGroup();
					}
					break;
				}
			};
		}
		#endregion



		
		#region Variables
		void DrawVariables () {
            EditorGUILayout.BeginVertical();
			if(InkEditorUtils.StoryContainsVariables(story)) {
				DrawVariablesHeader();
				if(InkPlayerWindowState.Instance.variablesPanelState.showing)
					DrawVariablesPanel ();

				if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count > 0) {
					DrawObservedVariablesHeader();
					if(InkPlayerWindowState.Instance.observedVariablesPanelState.showing)
						DrawObservedVariablesPanel ();
				}
			}
            EditorGUILayout.EndVertical();
		}

		void DrawVariablesHeader () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.variablesPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.variablesPanelState.showing, "Variables", true);

			EditorGUI.BeginDisabledGroup(!InkPlayerWindowState.Instance.variablesPanelState.showing);
			bool changed = DrawSearchBar(ref InkPlayerWindowState.Instance.variablesPanelState.searchString);
			if(changed) {
                RefreshVisibleVariables();
                InkPlayerWindowState.Instance.variablesPanelState.scrollPosition = Vector2.zero;
            }
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();
		}

		void DrawVariablesPanel () {
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			InkPlayerWindowState.Instance.variablesPanelState.scrollPosition = EditorGUILayout.BeginScrollView(InkPlayerWindowState.Instance.variablesPanelState.scrollPosition);
			string variableToChange = null;
			object newVariableValue = null;
			foreach(string variable in visibleVariables) {
				DrawObservableVariable(variable, ref variableToChange, ref newVariableValue);
			}
			if(variableToChange != null) {
				AddToHistory(InkHistoryContentItem.CreateForDebugNote("Change '"+variableToChange+"' from '"+story.variablesState[variableToChange]+"' to '"+newVariableValue+"'"));
				story.variablesState[variableToChange] = newVariableValue;
				variableToChange = null;
				newVariableValue = null;
			}
			
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

        static List<string> visibleVariables = new List<string>();
		static void RefreshVisibleVariables () {
			visibleVariables.Clear();
            if(story == null) return;
			bool doingSearch = !string.IsNullOrWhiteSpace(InkPlayerWindowState.Instance.variablesPanelState.searchString);
			foreach(string variable in story.variablesState) {
				if(doingSearch && !ShouldShowContentWithSearchString(variable, InkPlayerWindowState.Instance.variablesPanelState.searchString)) continue;
				visibleVariables.Add(variable);
			}
		}

        // TODO - only draw those that are visible in the scroll rect, as we do for content. Important for performance on larger projects.
        void DrawObservableVariable (string variable, ref string variableToChange, ref object newVariableValue) {
            if(!SearchStringMatch(variable, InkPlayerWindowState.Instance.variablesPanelState.searchString)) 
                return;
            EditorGUILayout.BeginHorizontal();
            object variableValue = story.variablesState[variable];
            if(DrawVariableLayout(new GUIContent(variable, variableValue.GetType().Name), variable, ref variableValue, "observable")) {
                variableToChange = variable;
                newVariableValue = variableValue;
            }

            if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.ContainsKey(variable)) {
                if(GUILayout.Button(new GUIContent(unobserveIcon, "Un-observe this variable"), GUILayout.Width(24))) {
                    UnobserveVariable(variable, true);
                }
            } else {
                if(GUILayout.Button(new GUIContent(observeIcon, "Click to observe this variable, tracking changes"), GUILayout.Width(24))) {
                    var observedVariable = ObserveVariable(variable, true);
                    observedVariable.AddValueState(variableValue);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

		static ObservedVariable ObserveVariable (string variableName, bool alsoAddToCache) {
			if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.ContainsKey(variableName)) return InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables[variableName];
			var observedVariable = new ObservedVariable(variableName);
			observedVariable.variableObserver = (_variableName, newValue) => {
				observedVariable.AddValueState(newValue);
			};
			story.ObserveVariable(variableName, observedVariable.variableObserver);
			InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Add(variableName, observedVariable);
			if(alsoAddToCache) {
				InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Add(variableName);
				if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count != InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Count) {
					Debug.LogError(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count +" "+ InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Count);
					InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Clear();
					InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
				}
			}
			return observedVariable;
		}
		
		// The cache is used to restore observed variables when the user exits play mode.
		static void UnobserveVariable (string variableName, bool alsoRemoveFromCache) {
			if(!InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.ContainsKey(variableName)) return;
			
			var observedVariable = InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables[variableName];
			story.RemoveVariableObserver(observedVariable.variableObserver, variableName);
			InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Remove(variableName);
			if(alsoRemoveFromCache) {
				InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Remove(variableName);
				if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count != InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Count) {
					Debug.LogError(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count +" "+ InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Count);
					InkPlayerWindowState.Instance.observedVariablesPanelState.restorableObservedVariableNames.Clear();
					InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
				}
			}
		}

		bool DrawVariableLayout (GUIContent guiContent, string variableName, ref object variableValue, string expandedIDModifier) {
			var lastVariableValue = variableValue;
            var anythingChanged = false;
            EditorGUILayout.BeginHorizontal();
			if(variableValue is string) {
				EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.DelayedTextField(guiContent, (string)variableValue);
                anythingChanged = (string)lastVariableValue != (string)variableValue;
				EditorGUI.EndDisabledGroup();
			} else if(variableValue is float) {
				EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.FloatField(guiContent, (float)variableValue);
                anythingChanged = (float)lastVariableValue != (float)variableValue;
				EditorGUI.EndDisabledGroup();
			} else if(variableValue is int) {
				EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.IntField(guiContent, (int)variableValue);
                anythingChanged = (int)lastVariableValue != (int)variableValue;
				EditorGUI.EndDisabledGroup();
			} else if(variableValue is bool) {
				EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.Toggle(guiContent, (bool)variableValue);
                anythingChanged = (bool)lastVariableValue != (bool)variableValue;
				EditorGUI.EndDisabledGroup();
			} else if(variableValue is InkList) {
				anythingChanged = EditorGUILayoutInkListField(guiContent, (InkList)variableValue, variableName+expandedIDModifier);
			} else if(variableValue is Ink.Runtime.Path) {
				var c = new GUIContent(((Ink.Runtime.Path)variableValue).ToString()+" (Ink.Runtime.Path)");
				EditorGUILayout.LabelField(guiContent, c);
			} else if(variableValue == null) {
				EditorGUILayout.LabelField(guiContent, new GUIContent("InkPlayerError: Variable is null"));
			} else {
				EditorGUILayout.LabelField(guiContent, new GUIContent("InkPlayerError: Variable is of unexpected type "+variableValue.GetType().Name+"."));
			}
			EditorGUILayout.LabelField(variableValue.GetType().Name, EditorStyles.miniLabel, GUILayout.Width(80));
			EditorGUILayout.EndHorizontal();
			return anythingChanged;
		}

		object DrawVariable (Rect rect, GUIContent variable, object variableValue) {
			if(variableValue is string) {
				variableValue = EditorGUI.TextField(rect, variable, (string)variableValue);
			} else if(variableValue is float) {
				variableValue = EditorGUI.FloatField(rect, variable, (float)variableValue);
			} else if(variableValue is int) {
				variableValue = EditorGUI.IntField(rect, variable, (int)variableValue);
			} else if(variableValue is InkList) {
				var c = new GUIContent(variable);
				var inkList = (InkList)variableValue;
				c.text += " (InkList)";
				if(inkList.Any()) {
					bool first = true;
					foreach(var item in inkList) {
						if(!first) c.text += ", ";
						c.text += item.ToString();
						first = false;
					}
				} else {
					c.text += " Empty";
				}
				EditorGUI.LabelField(rect, c);
			} else if(variableValue is Ink.Runtime.Path) {
				var c = new GUIContent(((Ink.Runtime.Path)variableValue).ToString()+" (Ink.Runtime.Path)");
				EditorGUI.LabelField(rect, c);
			} else if(variableValue == null) {
				EditorGUI.LabelField(rect, variable, new GUIContent("InkPlayerError: Variable is null"));
			} else {
				EditorGUI.LabelField(rect, variable, new GUIContent("InkPlayerError: Variable is of unexpected type "+variableValue.GetType().Name+"."));
			}
			return variableValue;
		}

		void DrawObservedVariablesHeader () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.observedVariablesPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.observedVariablesPanelState.showing, "Observed Variables", true);
			EditorGUILayout.EndHorizontal();
		}

		void DrawObservedVariablesPanel () {
			List<string> allToRemove = new List<string>();
			foreach(var observedVariable in InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables) {
				bool removeVariable = DrawObservedVariable(observedVariable.Value);
				if(removeVariable)
					allToRemove.Add(observedVariable.Key);
			}
			foreach(var toRemove in allToRemove) {
				UnobserveVariable(toRemove, true);
			}
		}

		bool DrawObservedVariable (ObservedVariable observedVariable) {
			GUILayout.BeginHorizontal();
			observedVariable.expanded = EditorGUILayout.Foldout(observedVariable.expanded, observedVariable.variable, true);
			if(GUILayout.Button(new GUIContent(unobserveIcon, "Un-observe this variable"), GUILayout.Width(24))) {
				return true;
			}
			GUILayout.EndHorizontal();

			if(observedVariable.expanded) {
				GUILayout.BeginVertical(GUILayout.ExpandHeight(false));
				observedVariable.scrollPosition = EditorGUILayout.BeginScrollView(observedVariable.scrollPosition, GUI.skin.box);
				
				foreach(var value in observedVariable.values) {
					DrawVariableLayout(new GUIContent(value.dateTime.ToLongTimeString()), observedVariable.variable, ref value.state, "observed"+value.GetHashCode().ToString());
				}
				
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}

			return false;
		}
		#endregion





		#region Profiler
		static bool isProfiling {
			get {
				return _currentStoryProfiler != null;
			}
			set {
				var shouldBeProfiling = value;
				if( shouldBeProfiling != isProfiling ) {
					if( _currentStoryProfiler == null ) {
						_currentStoryProfiler = story.StartProfiling();
					} else {
						story.EndProfiling();
						_profilerResultRootNode = _currentStoryProfiler.rootNode;

						Debug.Log(_currentStoryProfiler.StepLengthReport());

						_previousStoryProfiler = _currentStoryProfiler;
						_currentStoryProfiler = null;
					}
				}
			}
		}
		static ProfileNode _profilerResultRootNode;
		static Ink.Runtime.Profiler _currentStoryProfiler;
		static Ink.Runtime.Profiler _previousStoryProfiler;
		
		void DrawProfilerData() {

			// Don't show profiler data at all if you've never clicked Profile button
			if( _profilerResultRootNode == null && !isProfiling ) return;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.profilerPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.profilerPanelState.showing, "Profiler data", true);
			GUILayout.FlexibleSpace();
			if( _previousStoryProfiler != null && GUILayout.Button("Save mega log", EditorStyles.toolbarButton) ) {

				var path = EditorUtility.SaveFilePanel(
					"Save mega log",
					"",
					"megalog.txt",
					"txt");
				if( path != null && path.Length > 0 ) 
					File.WriteAllText(path, _previousStoryProfiler.Megalog());

			}
			EditorGUILayout.EndHorizontal();

			if(InkPlayerWindowState.Instance.profilerPanelState.showing) {
				if(isProfiling) {
					EditorGUILayout.LabelField("Click 'Stop Profiling' to show profiling results.", EditorStyles.centeredGreyMiniLabel);
				} else {
					DisplayProfileDataNode(_profilerResultRootNode);
				}
			}
		}

		void DisplayProfileDataNode(ProfileNode node) {
			var key = node.key;
			if( key == null ) {
				if( node == _profilerResultRootNode )
					key = "TOTAL";
				else
					key = "?";
			}

			var nodeText = key + ": " + node.ownReport;

			if( node.hasChildren ) {
				node.openInUI = EditorGUILayout.Foldout(node.openInUI, nodeText, true);

				if( node.openInUI ) {
					EditorGUI.indentLevel++;

					foreach(var childNode in node.descendingOrderedNodes)
						DisplayProfileDataNode(childNode.Value);

					EditorGUI.indentLevel--;
				}
			} else {
				EditorGUILayout.LabelField(nodeText);
			}
		}
		#endregion
		




		BaseStoryPanelState resizingPanel;
		// float height = 50;
		Rect GetResizeArea (float x, float width, float centerY) {
			float height = 10;
			return new Rect(x, centerY-Mathf.RoundToInt(height * 0.5f), width, height);
		}
		void HandleDragAndDrop () {
			// Rect area1 = GUILayoutUtility.GetRect (0.0f, height, GUILayout.ExpandWidth (true));
			// Rect area2 = GUILayoutUtility.GetRect (0.0f, 50.0f, GUILayout.ExpandWidth (true));
        	// GUI.Box (area1, "Add Trigger");
        	// GUI.Box (area2, "Add Trigger");

			// if (Event.current.type == EventType.DragUpdated) {
			// 	DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			// 	Event.current.Use();
			// } else if (Event.current.type == EventType.DragPerform) {
			// 	// To consume drag data.
				
			// 	DragAndDrop.AcceptDrag();
			// 	foreach (var obj in DragAndDrop.objectReferences) {
			// 		if(obj is TextAsset && System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(obj)) == ".json") {
			// 			Play(obj as TextAsset);
			// 		}
			// 	}
			// }
            
            if (resizingPanel != null && Event.current.type == EventType.MouseUp) {
                resizingPanel = null;
                Event.current.Use();
            }
            HandlePanelResize(InkPlayerWindowState.Instance.storyPanelState, StoryPanelState.minScrollRectHeight, StoryPanelState.maxScrollRectHeight);
            HandlePanelResize(InkPlayerWindowState.Instance.namedContentPanelState, StoryPanelState.minScrollRectHeight, StoryPanelState.maxScrollRectHeight);
		}
		void HandlePanelResize (BaseStoryPanelState panel, float minHeight, float maxHeight) {
			var resizeArea = GetResizeArea(0, position.width, panel.y+panel.height);
			EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeVertical);
			
			if (Event.current.type == EventType.MouseDown) {
				if(resizeArea.Contains(Event.current.mousePosition)) {
					resizingPanel = panel;
					Event.current.Use();
				}
			}
			if (resizingPanel == panel) {
				if(Event.current.type == EventType.MouseDrag) {
					var targetHeight = panel.height + Event.current.delta.y;
					panel.height = Mathf.Clamp(targetHeight, minHeight, maxHeight);
					Event.current.Use();
				}
			}
        }
		
		#region Utils
		static bool StringContains(string str, string toCheck, StringComparison comp) {
			if(toCheck.Length == 0) return false;
			return str.IndexOf(toCheck, comp) >= 0;
		}

		static bool DrawSearchBar (ref string searchString) {
			var lastString = searchString;
			searchString = GUILayout.TextField(searchString, searchTextFieldStyle);
			if (GUILayout.Button("", searchCancelButtonStyle)) {
				searchString = string.Empty;
			}
			return lastString != searchString;
		}

        static bool SearchStringMatch (string content, string searchString) {
            return string.IsNullOrWhiteSpace(searchString) || StringContains(content, searchString, StringComparison.OrdinalIgnoreCase);
        }


		static bool EditorGUILayoutInkListField (GUIContent guiContent, InkList inkList, string expandedVariableKey = null) {
			var anythingChanged = false;
            // if(inkList.Any()) {
				var show = expandedVariableKey == null ? true : InkPlayerWindowState.Instance.variablesPanelState.expandedVariables.Contains(expandedVariableKey);
				var c = new GUIContent(guiContent);
				c.text += " (InkList with "+inkList.Count+" entries)";
				EditorGUILayout.BeginVertical();
				
				EditorGUI.BeginChangeCheck();
				show = EditorGUILayout.Foldout(show, c, true);
				if(EditorGUI.EndChangeCheck() && expandedVariableKey != null) {
					if(show) InkPlayerWindowState.Instance.variablesPanelState.expandedVariables.Add(expandedVariableKey);
					else InkPlayerWindowState.Instance.variablesPanelState.expandedVariables.Remove(expandedVariableKey);
				}
				
				if(show) {
					EditorGUI.indentLevel++;
					var isOrigin = inkList.origins == null;
                    var list = isOrigin ? inkList : inkList.all;
                    List<KeyValuePair<InkListItem, int>> toAdd = null;
                    List<InkListItem> toRemove = null;
                    foreach(var item in list) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField(new GUIContent(item.Key.fullName));
						// Disabled until I can be bothered to integrate this into the change detection system
						var contains = inkList.Contains(item);
						var newContains = EditorGUILayout.Toggle(contains, GUILayout.Width(100));
                        if(contains != newContains) {
                            if(newContains) {
                                if(toAdd == null) toAdd = new List<KeyValuePair<InkListItem, int>>();
                                toAdd.Add(item);
                            } else {
                                if(toRemove == null) toRemove = new List<InkListItem>();
                                toRemove.Add(item.Key);
                            }
                            anythingChanged = true;
                        }
						EditorGUI.BeginDisabledGroup(true);
						EditorGUILayout.IntField(item.Value, GUILayout.Width(100));
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.EndHorizontal();
					}
                    if(toAdd != null)
                        foreach(var item in toAdd)
                            inkList.Add(item.Key, item.Value);
                    if(toRemove != null)
                        foreach(var item in toRemove)
                            inkList.Remove(item);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndVertical();
			// } else {
			// 	var c = new GUIContent(guiContent);
			// 	c.text += " (InkList)";
			// 	EditorGUILayout.PrefixLabel(c);
			// 	EditorGUILayout.LabelField("Empty");
			// }
            return anythingChanged;
		}

		// static void EditorGUILayoutInkListField (string text, InkList inkList) {
		//     EditorGUILayoutInkListField(new GUIContent(text), inkList);
		// }

		// static void EditorGUILayoutInkListField (GUIContent content, InkList inkList) {
		//     EditorGUILayout.BeginVertical(GUI.skin.box);
		//     EditorGUILayout.LabelField("InkList with "+inkList.Count+" values:", EditorStyles.boldLabel);
		//     foreach(var item in inkList) {
		//         EditorGUILayout.LabelField(item.Key.ToString()+" ("+item.Value.ToString()+")");
		//     }
		//     EditorGUILayout.EndVertical();
		// }
		static void EditorGUIInkListField (Rect rect, GUIContent content, InkList inkList, string variableName) {
			EditorGUI.PrefixLabel(rect, content);
			if(inkList.Any()) {
				if(GUILayout.Button("Log Contents")) {
					string log = "Log for InkList "+variableName+":";
					foreach(var item in inkList)
						log += item.ToString() + " / ";
					Debug.Log(log);
				}
			} else {
				EditorGUI.LabelField(rect, "Empty");
			}
		}
		#endregion


		

		static GUIStyle _wordWrappedTextArea;
		static GUIStyle wordWrappedTextArea {
			get {
				if(_wordWrappedTextArea == null) {
					_wordWrappedTextArea = new GUIStyle(EditorStyles.textArea);
					_wordWrappedTextArea.wordWrap = true;
				}
				return _wordWrappedTextArea;
			}
		}

		static Texture _profilerIcon;
		static Texture profilerIcon {
			get {
				if(_profilerIcon == null) {
					_profilerIcon = EditorGUIUtility.IconContent("UnityEditor.ProfilerWindow").image;
					// Profiler.CPU
					// Record Off
					// Record On
				}
				return _profilerIcon;
			}
		}
		
		static Texture _playIcon;
		static Texture playIcon {
			get {
				if(_playIcon == null) {
					_playIcon = EditorGUIUtility.IconContent("PlayButton").image;
				}
				return _playIcon;
			}
		}
		static Texture _stopIcon;
		static Texture stopIcon {
			get {
				if(_stopIcon == null) {
					_stopIcon = EditorGUIUtility.IconContent("PreMatQuad").image;
				}
				return _stopIcon;
			}
		}
		static Texture _restartIcon;
		static Texture restartIcon {
			get {
				if(_restartIcon == null) {
					_restartIcon = EditorGUIUtility.IconContent("Refresh").image;
				}
				return _restartIcon;
			}
		}
		static Texture _attachedStoryIcon;
		static Texture attachedStoryIcon {
			get {
				if(_attachedStoryIcon == null) {
					_attachedStoryIcon = EditorGUIUtility.IconContent("UnityEditor.FindDependencies").image;
				}
				return _attachedStoryIcon;
			}
		}
		static Texture _saveIcon;
		static Texture saveIcon {
			get {
				if(_saveIcon == null) {
					_saveIcon = EditorGUIUtility.IconContent("SaveAs").image;
				}
				return _saveIcon;
			}
		}
		static Texture _clearIcon;
		static Texture clearIcon {
			get {
				if(_clearIcon == null) {
					_clearIcon = EditorGUIUtility.IconContent("d_Grid.EraserTool").image;
					// d_back
				}
				return _clearIcon;
			}
		}
		static Texture _copyToClipboardIcon;
		static Texture copyToClipboardIcon {
			get {
				if(_copyToClipboardIcon == null) {
					_copyToClipboardIcon = EditorGUIUtility.IconContent("Clipboard").image;
				}
				return _copyToClipboardIcon;
			}
		}
		static Texture _undoIcon;
		static Texture undoIcon {
			get {
				if(_undoIcon == null) {
					_undoIcon = EditorGUIUtility.IconContent("d_back").image;
					// d_back
					// Animation.PrevKey
				}
				return _undoIcon;
			}
		}
		static Texture _redoIcon;
		static Texture redoIcon {
			get {
				if(_redoIcon == null) {
					_redoIcon = EditorGUIUtility.IconContent("d_forward").image;
					// d_forward
					// Animation.NextKey
				}
				return _redoIcon;
			}
		}
		static Texture _warningIcon;
		static Texture warningIcon {
			get {
				if(_warningIcon == null) {
					_warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;
				}
				return _warningIcon;
			}
		}
		static Texture _errorIcon;
		static Texture errorIcon {
			get {
				if(_errorIcon == null) {
					_errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;
				}
				return _errorIcon;
			}
		}
		static Texture _functionIcon;
		static Texture functionIcon {
			get {
				if(_functionIcon == null) {
					_functionIcon = EditorGUIUtility.IconContent("d_StyleSheet Icon").image;
				}
				return _functionIcon;
			}
		}
		static Texture _timeIntervalIcon;
		static Texture timeIntervalIcon {
			get {
				if(_timeIntervalIcon == null) {
					_timeIntervalIcon = EditorGUIUtility.IconContent("UnityEditor.AnimationWindow").image;
				}
				return _timeIntervalIcon;
			}
		}
		static Texture _observeIcon;
		static Texture observeIcon {
			get {
				if(_observeIcon == null) {
					_observeIcon = EditorGUIUtility.IconContent("d_animationvisibilitytoggleon").image;
				}
				return _observeIcon;
			}
		}
		static Texture _unobserveIcon;
		static Texture unobserveIcon {
			get {
				if(_unobserveIcon == null) {
					_unobserveIcon = EditorGUIUtility.IconContent("d_animationvisibilitytoggleoff").image;
				}
				return _unobserveIcon;
			}
		}
	}

	// Keeps a history of state changes for an ink variable. Handy for debugging.
	public class ObservedVariable {
		public string variable;
		public Story.VariableObserver variableObserver;
		public List<ObservedVariableState> values = new List<ObservedVariableState>();
		public bool expanded = true;
		public Vector2 scrollPosition = Vector2.zero;

		public class ObservedVariableState {
			public object state;
			public DateTime dateTime;
			public ObservedVariableState (object state) {
				// Make sure to clone any object ref types! (just InkList at time of writing)
				if(state is InkList) state = new InkList((InkList)state);
				this.state = state;
				dateTime = InkPlayerWindow.dateTimeNow;
			}
		}

		public ObservedVariable (string variable) {
			this.variable = variable;
		}
		public void AddValueState (object value) {
			values.Add(new ObservedVariableState(value));
		}
	}
    

	public class ColoredBackgroundGUIStyle {
		public GUIStyle guiStyle;
		public ColoredBackgroundGUIStyle (Color color) : this (color, color) {}
		public ColoredBackgroundGUIStyle (Color colorFree, Color colorPro) {
			guiStyle = new GUIStyle();

			var texture = new Texture2D( 1, 1 );
			texture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? colorPro : colorFree);
			texture.Apply();
			guiStyle.normal.background = texture;
		}
		public ColoredBackgroundGUIStyle (Color colorFree, Color colorPro, Color hoverColorFree, Color hoverColorPro) {
			guiStyle = new GUIStyle();

			var texture = new Texture2D( 1, 1 );
			texture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? colorPro : colorFree);
			texture.Apply();
			guiStyle.normal.background = texture;

			var hoverTexture = new Texture2D( 1, 1 );
			hoverTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? hoverColorPro : hoverColorFree);
			hoverTexture.Apply();
			guiStyle.hover.background = hoverTexture;
		}
	}


	[System.Serializable]
	public class UndoHistory<T> where T : class {
		
		private int _undoHistoryIndex;
		public int undoHistoryIndex {
			get {
				return _undoHistoryIndex;
			} set {
				_undoHistoryIndex = Mathf.Clamp(value, 0, undoHistory.Count-1);
				if(OnChangeHistoryIndex != null) OnChangeHistoryIndex(undoHistory[undoHistoryIndex]);
			}
		}
		
		public List<T> undoHistory;
		public int maxHistoryItems = 100;
		
		public bool canUndo {
			get {
				return undoHistory.Count > 0 && undoHistoryIndex > 0;
			}
		}
		
		public bool canRedo {
			get {
				return undoHistory.Count > 0 && undoHistoryIndex < undoHistory.Count - 1;
			}
		}
		
		public delegate void OnUndoEvent(T historyItem);
		public event OnUndoEvent OnUndo;
		
		public delegate void OnRedoEvent(T historyItem);
		public event OnRedoEvent OnRedo;
		
		public delegate void OnChangeHistoryIndexEvent(T historyItem);
		public event OnChangeHistoryIndexEvent OnChangeHistoryIndex;
		
		public delegate void OnChangeUndoHistoryEvent();
		public event OnChangeUndoHistoryEvent OnChangeUndoHistory;
		
		public UndoHistory () {
			undoHistory = new List<T>();
			_undoHistoryIndex = -1;
		}
		
		public UndoHistory (int maxHistoryItems) : this () {
			this.maxHistoryItems = Mathf.Clamp(maxHistoryItems, 1, int.MaxValue);
		}
		
		public virtual void AddToUndoHistory (T state) {
			if(undoHistory.Count > 0 && undoHistory.Count - (undoHistoryIndex + 1) > 0) {
				undoHistory.RemoveRange(undoHistoryIndex + 1, undoHistory.Count - (undoHistoryIndex + 1));
			}
			
			if(undoHistory.Count >= maxHistoryItems) {
				undoHistory.RemoveAt (0);
				_undoHistoryIndex--;
			}
			
			undoHistory.Add (state);
			_undoHistoryIndex++;
			
			if(OnChangeUndoHistory != null) OnChangeUndoHistory();
		}
		
		public virtual void Clear () {
			undoHistory.Clear();
			_undoHistoryIndex = -1;
			if(OnChangeUndoHistory != null) OnChangeUndoHistory();
		}
		
		public virtual T Undo () {
			if(!canUndo) {
				if(undoHistory.Count > 0)
					return default(T);
			} else {
				undoHistoryIndex--;
				if(OnUndo != null) OnUndo(undoHistory[undoHistoryIndex]);
			}
			return undoHistory[undoHistoryIndex];
		}
		
		public virtual T Redo () {
			if(!canRedo) {
				if(undoHistory.Count > 0)
					return default(T);
			} else {
				undoHistoryIndex++;
				if(OnRedo != null) OnRedo(undoHistory[undoHistoryIndex]);
			}
			return undoHistory[undoHistoryIndex];
		}
		
		protected virtual void ApplyHistoryItem (T historyItem) {}
	}

	public class InkPlayerHistoryItem {
		public string inkStateJSON;
		public List<InkHistoryContentItem> storyHistory;
		
		public InkPlayerHistoryItem (string inkStateJSON, List<InkHistoryContentItem> storyHistory) {
			this.inkStateJSON = inkStateJSON;
			this.storyHistory = storyHistory;
		}
	}
}