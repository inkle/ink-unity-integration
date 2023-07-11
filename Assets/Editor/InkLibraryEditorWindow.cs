using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	public class InkLibraryEditorWindow : EditorWindow {

		#pragma warning disable
		Editor inkLibraryEditor;

		[MenuItem("Window/Ink Library Editor Window")]
		static void Init() {
			var window = (InkLibraryEditorWindow)GetWindow(typeof(InkLibraryEditorWindow));
			window.titleContent = new GUIContent("Ink Library Window");
			window.Show();
		}

		public void OnEnable() {
			inkLibraryEditor = Editor.CreateEditor(InkLibrary.instance);
		}

		void OnInspectorUpdate() {
			Repaint();
		}

		public void OnGUI() {
			inkLibraryEditor.OnInspectorGUI();
	    }
	}
}