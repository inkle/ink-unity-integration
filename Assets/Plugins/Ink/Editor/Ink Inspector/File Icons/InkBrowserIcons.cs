using System.IO;
using UnityEditor;
using UnityEngine;

/* 
* This script allows you to set custom icons for folders in project browser.
* Recommended icon sizes - small: 16x16 px, large: 64x64 px;
*/

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public class InkBrowserIcons {
	    private const float largeIconSize = 64f;

		private static Texture2D inkFileIcon;

	    static InkBrowserIcons() {
			float unityVersion = float.Parse(Application.unityVersion.Substring (0, 3));
			if(Application.platform == RuntimePlatform.OSXEditor && unityVersion > 5.4f) {
				inkFileIcon = Resources.Load<Texture2D>("InkFileIcon-retina");
	    	} else {
				inkFileIcon = Resources.Load<Texture2D>("InkFileIcon");
	    	}
			if(inkFileIcon != null)
				EditorApplication.projectWindowItemOnGUI += OnDrawProjectWindowItem;
	    }

	    static void OnDrawProjectWindowItem(string guid, Rect rect) {
	        var path = AssetDatabase.GUIDToAssetPath(guid);

			if (Path.GetExtension(path) == InkEditorUtils.inkFileExtension) {
				var isSmall = rect.width > rect.height;
				if (isSmall) {
					rect.width = rect.height;
				} else {
					rect.height = rect.width;
				}

				if (rect.width > largeIconSize) {
					var offset = (rect.width - largeIconSize) * 0.5f;
					var position = new Rect(rect.x + offset, rect.y + offset, largeIconSize, largeIconSize);
					GUI.DrawTexture(position, inkFileIcon);
				}
				else {
					GUI.DrawTexture(rect, inkFileIcon);
				}
			}
	    }
	}
}