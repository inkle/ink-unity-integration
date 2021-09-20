using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ink.UnityIntegration {
	public class InkCompilerEditorWindow : EditorWindow {

		#pragma warning disable
		Editor InkCompilerEditor;

		[MenuItem("Window/Ink Compiler Editor Window")]
		static void Init() {
			var window = (InkCompilerEditorWindow)EditorWindow.GetWindow(typeof(InkCompilerEditorWindow));
			window.titleContent = new GUIContent("Ink Compiler Window");
			window.Show();
		}

		public void OnEnable() {
			InkCompilerEditor = Editor.CreateEditor(InkCompiler.instance);
		}

		void OnInspectorUpdate() {
			Repaint();
		}

		public void OnGUI() {
			InkCompilerEditor.OnInspectorGUI();
	    }
	}
}