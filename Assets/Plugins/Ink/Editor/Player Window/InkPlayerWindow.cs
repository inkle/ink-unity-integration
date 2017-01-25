using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {

	/// <summary>
	/// Ink player window. Tests stories in an editor window.
	/// </summary>
	public class InkPlayerWindow : EditorWindow {
		private const string windowTitle = "Ink Player";

//		public StoryState storyState = new StoryState();
//		public class StoryState {
//		}
		public bool attached {get; private set;}
		
		private TextAsset storyJSONTextAsset;
		private string storyJSON;
		public Story story {get; private set;}
		private TextAsset _storyStateTextAsset;
		public TextAsset storyStateTextAsset {
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
		
		private UndoHistory<InkPlayerHistoryItem> storyStateHistory;
		private List<InkPlayerHistoryContentItem> storyHistory;
		
		private Exception errors;
		private bool storyStateValid = false;

		public PlayerOptions playerOptions = new PlayerOptions();
		public class PlayerOptions {
			public bool continueAutomatically = true;
			public bool chooseAutomatically = false;
		}

//		private WindowState windowState = new WindowState();
//		public class WindowState {
			public Vector2 scrollPosition;
			public bool showingContentPanel = true;
			public Vector2 contentScrollPosition;
			public bool showingChoicesPanel = true;
			public bool showingSaveLoadPanel;

			public bool showingFunctionsPanel;

			public Vector2 variablesScrollPosition;
//		}

		public bool showingDivertsPanel;
		private DivertPanelState divertPanelState = new DivertPanelState();
		public class DivertPanelState {
			public Vector2 divertsScrollPosition;
			public string divertCommand = String.Empty;
		}

		private FunctionPanelState functionPanelState = new FunctionPanelState();
		public class FunctionPanelState {
			public string functionName = String.Empty;
			public string testedFunctionName = null;
			public object functionReturnValue = null;
		}

		public bool showingVariablesPanel;
//		private VariablesPanelState variablesPanelState = new FunctionPanelState();
//		public class VariablesPanelState {
//		}

		public DisplayOptions displayOptions = new DisplayOptions();
		public class DisplayOptions {
			public bool displayTime = false;
			public bool displayChoicesInLog = false;
			public bool displayDebugNotesInLog = false;
		}


		[MenuItem("Window/Ink Player %#i", false, 2300)]
		public static InkPlayerWindow GetWindow () {
			return GetWindow<InkPlayerWindow>(windowTitle, true);
		}

		public static InkPlayerWindow GetWindow (bool focus) {
			return GetWindow<InkPlayerWindow>(windowTitle, focus);
		}
		
		void OnEnable () {
			storyStateHistory = new UndoHistory<InkPlayerHistoryItem>();
			storyHistory = new List<InkPlayerHistoryContentItem>();
			EditorApplication.update += Update;
		}

		private void Update () {
			if(story != null && playerOptions.chooseAutomatically) {
				if(story.currentChoices.Count > 0) {
					MakeRandomChoice();
				}
			}
		}

		// Requires testing - would allow users to attach a run-time story to this window for testing purposes.
		public static InkPlayerWindow Attach (Story story) {
			InkPlayerWindow window = GetWindow();
			window.Clear();
			window.playerOptions.continueAutomatically = false;
			window.playerOptions.chooseAutomatically = false;
			window.story = story;
			window.attached = true;
			return window;
		}

		public static void Detach () {
			InkPlayerWindow window = GetWindow();
			window.attached = false;
			window.story = null;
		}

		public static void LoadAndPlay (TextAsset storyJSONTextAsset) {
			InkPlayerWindow window = GetWindow();
			if(window.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					window.Stop();
					window.Play(storyJSONTextAsset);
				}
			} else {
				window.Play(storyJSONTextAsset);
			}
		}

		public static void LoadAndPlay (string storyJSON) {
			InkPlayerWindow window = GetWindow();
			if(window.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					window.Stop();
					window.Play(storyJSON);
				}
			} else {
				window.Play(storyJSON);
			}
		}

		void Play (TextAsset storyJSONTextAsset) {
			if(!InkEditorUtils.CheckStoryIsValid(storyJSONTextAsset.text, out errors))
				return;
			this.storyJSONTextAsset = storyJSONTextAsset;
			storyJSON = this.storyJSONTextAsset.text;
			PlayInternal();
		}

		void Play (string storyJSON) {
			if(!InkEditorUtils.CheckStoryIsValid(storyJSON, out errors))
				return;
			this.storyJSONTextAsset = null;
			this.storyJSON = storyJSON;
			PlayInternal();
		}

		void PlayInternal () {
			functionPanelState.testedFunctionName = null;
			InitStory();
			PingAutomator();
		}

		void PingAutomator () {
			if(story.canContinue && playerOptions.continueAutomatically) {
				TryContinue();
			} else if(story.currentChoices.Count > 0 && playerOptions.chooseAutomatically) { 
				MakeRandomChoice();
			}
		}

		void InitStory () {
			story = new Story(storyJSON);
			story.allowExternalFunctionFallbacks = true;
		}
		
		void Stop () {
			Clear ();
		}

		void Clear () {
			functionPanelState.testedFunctionName = null;
			storyStateHistory.Clear();
			storyHistory.Clear();
			story = null;
		}
		
		void Restart () {
			Stop();
			if(storyJSONTextAsset != null)
				Play(storyJSONTextAsset);
			else
				Play(storyJSON);
		}
		
		void ContinueStory () {
			story.Continue();
			AddContent(story.currentText.Trim());
		}

		void AddContent (string content) {
			storyHistory.Add(new InkPlayerHistoryContentItem(content, InkPlayerHistoryContentItem.ContentType.StoryContent));
			ScrollToBottom();
			AddToHistory();
		}
		
		void AddToHistory () {
			InkPlayerHistoryItem historyItem = new InkPlayerHistoryItem(story.state.ToJson(), new List<InkPlayerHistoryContentItem>(storyHistory));
			storyStateHistory.AddToUndoHistory(historyItem);
		}
		
		void Undo () {
			InkPlayerHistoryItem item = storyStateHistory.Undo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkPlayerHistoryContentItem>(item.storyHistory);
			ScrollToBottom();
		}
		
		void Redo () {
			InkPlayerHistoryItem item = storyStateHistory.Redo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkPlayerHistoryContentItem>(item.storyHistory);
			ScrollToBottom();
		}

		void SaveStoryState (string storyStateJSON) {
			storyHistory.Add(new InkPlayerHistoryContentItem("Saved state", InkPlayerHistoryContentItem.ContentType.DebugNote));
			storyStateTextAsset = InkEditorUtils.CreateStoryStateTextFile(storyStateJSON, System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(storyJSONTextAsset)), storyJSONTextAsset.name+"_SaveState");
		}

		void LoadStoryState (string storyStateJSON) {
			storyHistory.Clear();
			storyStateHistory.Clear();
			storyHistory.Add(new InkPlayerHistoryContentItem("Loaded state", InkPlayerHistoryContentItem.ContentType.DebugNote));
			story.state.LoadJson(storyStateJSON);
		}

		void ScrollToBottom () {
			contentScrollPosition.y = Mathf.Infinity;
		}

		void TryContinue () {
			if(!story.canContinue) 
				return;
			if(playerOptions.continueAutomatically) {
				while (story.canContinue) {
					ContinueStory();
				}
			} else {
				ContinueStory();
			}
		}
		
		void OnGUI () {
			this.Repaint();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			DisplayHeader();

			if(errors == null) {
				DisplayStoryControlGroup();
			} else {
				DisplayErrors();
			}

			if(story != null && story.state != null) {
				DrawStory();
				DrawChoices();
				DrawSaveLoad();
				DrawDiverts();
				DrawFunctions();
				DrawVariables();
			}

			EditorGUILayout.EndScrollView();
		}
		
		void DisplayHeader () {
			if(attached) {
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				GUILayout.Label("Attached");
				if (GUILayout.Button("Detach", EditorStyles.toolbarButton)) {
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
				if(storyJSONTextAsset != null && storyJSON != null && storyJSONTextAsset.text != storyJSON) {
					EditorGUILayout.HelpBox("Story JSON file has changed. Restart to play updated story.", MessageType.Warning);
				}
				EditorGUILayout.EndVertical();
			}
		}
		
		void DisplayErrors () {
			EditorGUILayout.HelpBox(errors.Message, MessageType.Error);
		}

		void DisplayStoryControlGroup () {
			EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);

			if(story == null) {
				EditorGUI.BeginDisabledGroup(storyJSONTextAsset == null);
				if(GUILayout.Button("Start", EditorStyles.toolbarButton)) {
					Play(storyJSONTextAsset);
				}
				EditorGUI.EndDisabledGroup();
			} else {
				if(GUILayout.Button("Stop", EditorStyles.toolbarButton)) {
					Stop();
				}
				if(GUILayout.Button("Restart", EditorStyles.toolbarButton)) {
					Restart();
				}
			}

			GUILayout.FlexibleSpace();

			if(story != null) {
				EditorGUI.BeginDisabledGroup(!storyStateHistory.canUndo);
				if(GUILayout.Button("Undo", EditorStyles.toolbarButton)) {
					Undo();
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!storyStateHistory.canRedo);
				if(GUILayout.Button("Redo", EditorStyles.toolbarButton)) {
					Redo();
				}
				EditorGUI.EndDisabledGroup();
			}

			GUILayout.FlexibleSpace();

			EditorGUI.BeginChangeCheck();
			playerOptions.continueAutomatically = GUILayout.Toggle(playerOptions.continueAutomatically, "Auto-Continue", EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck() && playerOptions.continueAutomatically) {
				PingAutomator();
			}

			playerOptions.chooseAutomatically = GUILayout.Toggle(playerOptions.chooseAutomatically, "Auto-Choice", EditorStyles.toolbarButton);

			GUILayout.EndHorizontal();
		}

		void DrawStory () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingContentPanel = EditorGUILayout.Foldout(showingContentPanel, "Content");
			EditorGUI.BeginChangeCheck();
			displayOptions.displayTime = GUILayout.Toggle(displayOptions.displayTime, "Time", EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
				ScrollToBottom();
			}

			EditorGUI.BeginChangeCheck();
			displayOptions.displayChoicesInLog = GUILayout.Toggle(displayOptions.displayChoicesInLog, "Choices", EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
				ScrollToBottom();
			}
			EditorGUI.BeginChangeCheck();
			displayOptions.displayDebugNotesInLog = GUILayout.Toggle(displayOptions.displayDebugNotesInLog, "Debug", EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
				ScrollToBottom();
			}
			EditorGUILayout.EndHorizontal();
			if(showingContentPanel)
				DisplayStoryBody ();
		}

		void DisplayStoryBody () {
			GUILayout.BeginVertical();
			
			contentScrollPosition = EditorGUILayout.BeginScrollView(contentScrollPosition);
			
			foreach(InkPlayerHistoryContentItem content in storyHistory) {
				if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryContent) {
					DisplayContent(content);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryChoice && displayOptions.displayChoicesInLog) {
					DisplayContent(content);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.DebugNote && displayOptions.displayDebugNotesInLog) {
					DisplayContent(content);
				}
			}
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

		void DisplayContent(InkPlayerHistoryContentItem content)  {
			string s = String.Empty;
			if(displayOptions.displayTime) 
				s = "("+content.time.ToLongTimeString()+") ";
			if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryChoice && displayOptions.displayChoicesInLog) {
				s += " > ";
			} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.DebugNote && displayOptions.displayDebugNotesInLog) {
				s += " // ";
			}
			s += content.content;
			DisplayLine(s);
			
		}
		void DisplayLine (string content) {
			float width = position.width-28;
			float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(content), width);
			EditorGUILayout.SelectableLabel(content, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.Width(width), GUILayout.Height(height));
		}

		void DrawChoices () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingChoicesPanel = EditorGUILayout.Foldout(showingChoicesPanel, "Choices");
			EditorGUILayout.EndHorizontal();
			if(showingChoicesPanel)
				DisplayChoices ();
		}

		void DisplayChoices () {
			GUILayout.BeginVertical();
			if(story.canContinue) {
				if(GUILayout.Button("Continue")) {
					ContinueStory();
				}
				if(GUILayout.Button("Continue Maximally")) {
					while(story.canContinue) {
						ContinueStory();
					}
				}
			} else if(story.currentChoices.Count > 0) {
				foreach(Choice choice in story.currentChoices) {
					if(GUILayout.Button(choice.text.Trim())) {
						MakeChoice(choice);
					}
				}
			} else {
				GUILayout.Label("Reached end of story");
			}

			GUILayout.EndVertical();
		}

		void MakeRandomChoice () {
			MakeChoice(story.currentChoices[UnityEngine.Random.Range(0, story.currentChoices.Count)]);
		}

		void MakeChoice (Choice choice) {
			storyHistory.Add(new InkPlayerHistoryContentItem(choice.text.Trim(), InkPlayerHistoryContentItem.ContentType.StoryChoice));
			story.ChooseChoiceIndex(choice.index);
			AddToHistory();
			TryContinue();
		}

		void DrawSaveLoad () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingSaveLoadPanel = EditorGUILayout.Foldout(showingSaveLoadPanel, "Story State");
			EditorGUILayout.EndHorizontal();
			if(showingSaveLoadPanel)
				DrawSaveLoadPanel ();
		}

		void DrawSaveLoadPanel () {
			GUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			string currentStateJSON = story.state.ToJson();
			EditorGUILayout.TextField("Current State JSON", currentStateJSON);
			EditorGUI.BeginDisabledGroup(GUIUtility.systemCopyBuffer == currentStateJSON);
//			if (GUILayout.Button("Copy To Clipboard")) {
//				GUIUtility.systemCopyBuffer = currentStateJSON;
//			}
			EditorGUI.EndDisabledGroup();
			if (GUILayout.Button("Save As...")) {
				SaveStoryState(currentStateJSON);
			}
			EditorGUILayout.EndHorizontal();

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
		}

		void DrawDiverts () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingDivertsPanel = EditorGUILayout.Foldout(showingDivertsPanel, "Diverts");
			EditorGUILayout.EndHorizontal();
			if(showingDivertsPanel)
				DrawDivertsPanel ();
		}

		void DrawDivertsPanel () {
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			divertPanelState.divertCommand = EditorGUILayout.TextField("Divert command", divertPanelState.divertCommand);
			EditorGUI.BeginDisabledGroup(divertPanelState.divertCommand == "");
			if (GUILayout.Button("Divert")) {
				storyHistory.Add(new InkPlayerHistoryContentItem("Diverted to '"+divertPanelState.divertCommand+"'", InkPlayerHistoryContentItem.ContentType.DebugNote));
				story.ChoosePathString(divertPanelState.divertCommand);
				PingAutomator();
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		void DrawFunctions () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingFunctionsPanel = EditorGUILayout.Foldout(showingFunctionsPanel, "Functions");
			EditorGUILayout.EndHorizontal();
			if(showingFunctionsPanel)
				DrawFunctionsPanel ();
		}

		void DrawFunctionsPanel () {
			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			functionPanelState.functionName = EditorGUILayout.TextField("Function", functionPanelState.functionName);
			if(EditorGUI.EndChangeCheck()) {
				functionPanelState.testedFunctionName = null;
				functionPanelState.functionReturnValue = null;
			}

			bool functionIsValid = functionPanelState.functionName != String.Empty && story.HasFunction(functionPanelState.functionName);
			EditorGUI.BeginDisabledGroup(!functionIsValid);
			if (GUILayout.Button("Execute")) {
				storyHistory.Add(new InkPlayerHistoryContentItem("Execute function '"+functionPanelState.functionName+"'", InkPlayerHistoryContentItem.ContentType.DebugNote));
				string outputContent = null;
				functionPanelState.functionReturnValue = story.EvaluateFunction(functionPanelState.functionName, out outputContent);
				if(outputContent != null)
					AddContent(outputContent);
				functionPanelState.testedFunctionName = functionPanelState.functionName;
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();

			if(functionIsValid && functionPanelState.functionName == functionPanelState.testedFunctionName) {
				if(functionPanelState.functionReturnValue == null) {
					EditorGUILayout.LabelField("Output (Null)");
				} else if(functionPanelState.functionReturnValue is string) {
					EditorGUILayout.TextField("Output (String)", (string)functionPanelState.functionReturnValue);
				} else if(functionPanelState.functionReturnValue is float) {
					EditorGUILayout.FloatField("Output (Float)", (float)functionPanelState.functionReturnValue);
				} else if(functionPanelState.functionReturnValue is int) {
					EditorGUILayout.IntField("Output (Int)", (int)functionPanelState.functionReturnValue);
				}
			}

			GUILayout.EndVertical();
		}

		void DrawVariables () {
			if(InkEditorUtils.StoryContainsVariables(story)) {
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				showingVariablesPanel = EditorGUILayout.Foldout(showingVariablesPanel, "Variables");
				EditorGUILayout.EndHorizontal();
				if(showingVariablesPanel)
					DrawVariablesPanel ();
			}
		}

		void DrawVariablesPanel () {
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxHeight(80));
			variablesScrollPosition = EditorGUILayout.BeginScrollView(variablesScrollPosition);

			string variableToChange = null;
			object newVariableValue = null;
			foreach(string variable in story.variablesState) {
				object variableValue = story.variablesState[variable];
				EditorGUI.BeginChangeCheck();
				if(variableValue is string) {
					variableValue = EditorGUILayout.TextField(variable, (string)variableValue);
				} else if(variableValue is float) {
					variableValue = EditorGUILayout.FloatField(variable, (float)variableValue);
				} else if(variableValue is int) {
					variableValue = EditorGUILayout.IntField(variable, (int)variableValue);
				}
				if(EditorGUI.EndChangeCheck()) {
					variableToChange = variable;
					newVariableValue = variableValue;
				}
			}
			if(variableToChange != null) {
				storyHistory.Add(new InkPlayerHistoryContentItem("Change '"+variableToChange+"' from '"+story.variablesState[variableToChange]+"' to '"+newVariableValue+"'", InkPlayerHistoryContentItem.ContentType.DebugNote));
				story.variablesState[variableToChange] = newVariableValue;
				variableToChange = null;
				newVariableValue = null;
			}
			
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
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
		public List<InkPlayerHistoryContentItem> storyHistory;
		
		public InkPlayerHistoryItem (string inkStateJSON, List<InkPlayerHistoryContentItem> storyHistory) {
			this.inkStateJSON = inkStateJSON;
			this.storyHistory = storyHistory;
		}
	}

	public class InkPlayerHistoryContentItem {
		public enum ContentType {
			StoryContent,
			StoryChoice,
			DebugNote
		}

		public string content;
		public ContentType contentType;
		public DateTime time;

		public InkPlayerHistoryContentItem (string text, ContentType contentType) {
			this.content = text;
			this.contentType = contentType;
			time = DateTime.Now;
		}
	}
}