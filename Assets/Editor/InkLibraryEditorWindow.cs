using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ink.UnityIntegration {
	public class InkLibraryEditorWindow : EditorWindow {

		#pragma warning disable
		Editor inkLibraryEditor;

		[MenuItem("Window/Ink Library Editor Window")]
		static void Init() {
			var window = (InkLibraryEditorWindow)EditorWindow.GetWindow(typeof(InkLibraryEditorWindow));
			window.titleContent = new GUIContent("Ink Library Window");
			window.Show();
		}

		public void OnEnable() {
			inkLibraryEditor = Editor.CreateEditor(InkLibrary.Instance);
		}

		void OnInspectorUpdate() {
			Repaint();
		}

		public void OnGUI() {
			inkLibraryEditor.OnInspectorGUI();
	    }
	}
}