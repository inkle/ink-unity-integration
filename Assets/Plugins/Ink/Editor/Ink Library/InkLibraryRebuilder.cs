/*
using UnityEditor;
using Debug = UnityEngine.Debug;
*/

/// <summary>
/// Rebuilds the ink meta library whenever scripts compile, as I was finding that you can't rely on the meta library to serialize and deserialize object references reliably.
/// </summary>
namespace Ink.UnityIntegration {


	/*
	[InitializeOnLoad]
	public static class InkLibraryRebuilder {
		static InkLibraryRebuilder () {
			EditorApplication.update += AfterCompile;
    	}

    	static void AfterCompile () {
    		Debug.Log("LAUNCH");
			EditorApplication.update -= AfterCompile;
			InkMetaLibrary.Instance.metaLibrary.Clear();
			InkMetaLibrary.RebuildInkFileConnections();
//			InkMetaLibrary.Rebuild();
    	}
	

		[MenuItem("Assets/Clear", false, 120)]
		public static void CreateNewInkFile () {
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.metaInfo = new InkMetaFile(inkFile);
			}
		}
		[MenuItem("Assets/Rebuild", false, 120)]
		public static void Rebuild () {
			InkMetaLibrary.RebuildInkFileConnections();
		}
	}
	*/
}