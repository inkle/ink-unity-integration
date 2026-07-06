using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Default drawer for InkFile reference fields. Draws the object field and, when the assigned InkFile
	/// has no compiled story (an include file, or one that failed to compile), shows a warning — so it's
	/// obvious when a slot has a file that can't be turned into a Story at runtime.
	/// </summary>
	[CustomPropertyDrawer(typeof(InkFile))]
	public class InkFilePropertyDrawer : PropertyDrawer {
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
				bool show = inkFile != null && !inkFile.isCompiled;
				warning.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
				if (show)
					warning.text = inkFile.isMaster
						? "This ink file hasn't compiled to a story (it may have errors)."
						: "This is an INCLUDE file with no story of its own. Assign a master ink file instead.";
			}

			Refresh(property.objectReferenceValue as InkFile);
			objectField.RegisterValueChangedCallback(evt => Refresh(evt.newValue as InkFile));
			return root;
		}
	}
}
