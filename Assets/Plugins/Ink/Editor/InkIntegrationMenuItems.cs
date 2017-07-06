using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ink.UnityIntegration {
	public class InkIntegrationMenuItems {
		[MenuItem("Assets/Rebuild Ink Library", false, 60)]
		public static void RebuildLibrary() {
			InkLibrary.Rebuild();
		}

		[MenuItem("Assets/Recompile All Ink", false, 61)]
		public static void RecompileAll() {
			InkLibrary.Rebuild();
			List<InkFile> masterInkFiles = InkLibrary.GetMasterInkFiles ();
			foreach(InkFile masterInkFile in masterInkFiles)
				InkCompiler.CompileInk(masterInkFile);
		}
	}
}