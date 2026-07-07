using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Default drawer for InkFile reference fields. Draws the object field and, when the assigned InkFile
	/// has no compiled story (an include file, or one that failed to compile), shows a warning — so it's
	/// obvious when a slot has a file that can't be turned into a Story at runtime. Implements both the
	/// UI Toolkit and IMGUI paths, so it renders in default inspectors and custom IMGUI editors alike.
	/// </summary>
	[CustomPropertyDrawer(typeof(InkFile))]
	public class InkFilePropertyDrawer : PropertyDrawer {
		// The warning to show for the assigned file, or null if there's nothing to warn about (an empty
		// slot, or a compiled master file).
		static string GetWarning (InkFile inkFile) {
			if (inkFile == null || inkFile.isCompiled) return null;
			return inkFile.isMaster
				? "This ink file hasn't compiled to a story (it may have errors)."
				: "This is an INCLUDE file with no story of its own. Assign a master ink file instead.";
		}

		public override VisualElement CreatePropertyGUI (SerializedProperty property) {
			var root = new VisualElement();

			// An ObjectField (not a PropertyField) so we don't recursively invoke this drawer.
			var objectField = new ObjectField(property.displayName) {
				objectType = typeof(InkFile),
				allowSceneObjects = false,
			};
			objectField.BindProperty(property);
			root.Add(objectField);

			var warning = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
			root.Add(warning);

			void Refresh (InkFile inkFile) {
				// Give the referenced file its badged per-object icon, so the object field shows the same
				// icon (with error/warning/include badges) as the Project window.
				InkBrowserIcons.ApplyInstanceIcon(inkFile);

				var message = GetWarning(inkFile);
				warning.style.display = message != null ? DisplayStyle.Flex : DisplayStyle.None;
				if (message != null) warning.text = message;
			}

			Refresh(property.objectReferenceValue as InkFile);
			objectField.RegisterValueChangedCallback(evt => Refresh(evt.newValue as InkFile));
			return root;
		}

		// IMGUI fallback, so the drawer also renders in custom IMGUI editors. Without it, Unity shows
		// "No GUI Implemented" there (e.g. the demo's BasicInkExampleEditor).
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var inkFile = property.objectReferenceValue as InkFile;
			var message = GetWarning(inkFile);

			// An ObjectField (not a PropertyField) so we don't recursively invoke this drawer.
			var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.BeginProperty(fieldRect, label, property);
			var newValue = EditorGUI.ObjectField(fieldRect, label, property.objectReferenceValue, typeof(InkFile), false);
			if (newValue != property.objectReferenceValue) property.objectReferenceValue = newValue;
			EditorGUI.EndProperty();

			if (inkFile != null) InkBrowserIcons.ApplyInstanceIcon(inkFile);
			if (message != null) {
				var boxRect = new Rect(position.x, fieldRect.yMax + 2, position.width, position.height - fieldRect.height - 2);
				EditorGUI.HelpBox(boxRect, message, MessageType.Warning);
			}
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
			var line = EditorGUIUtility.singleLineHeight;
			if (GetWarning(property.objectReferenceValue as InkFile) == null) return line;
			return line + 2 + line * 2; // object field + gap + ~2-line help box
		}
	}
}
