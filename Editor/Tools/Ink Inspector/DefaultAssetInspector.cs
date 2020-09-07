using UnityEngine;
using UnityEditor;

namespace Ink.UnityIntegration {
	public abstract class DefaultAssetInspector {
		// Reference to the actual editor we draw to
		public Editor editor;
		// Shortcut to the target object
		public Object target;
		// Shortcut to the serializedObject
		public SerializedObject serializedObject;

		public abstract bool IsValid(string assetPath);
		public virtual void OnEnable () {}
		public virtual void OnDisable () {}
		public virtual void OnHeaderGUI () {}
		public virtual void OnInspectorGUI() {}
	}
}