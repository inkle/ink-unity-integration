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

		public static Texture2D inkFileIcon;
		public static Texture2D errorIcon;
		public static Texture2D warningIcon;
		public static Texture2D childIcon;
		public static Texture2D unknownFileIcon;

	    static InkBrowserIcons() {
			LoadIcons();
			EditorApplication.projectWindowItemOnGUI += OnDrawProjectWindowItem;
	    }

	    static void LoadIcons () {
			float unityVersion = float.Parse(Application.unityVersion.Substring (0, 3));
			if(Application.platform == RuntimePlatform.OSXEditor && unityVersion >= 5.4f) {
				inkFileIcon = Resources.Load<Texture2D>("InkFileIcon-retina");
	    	} else {
				inkFileIcon = Resources.Load<Texture2D>("InkFileIcon");
	    	}
			errorIcon = Resources.Load<Texture2D>("InkErrorIcon");
			warningIcon = Resources.Load<Texture2D>("InkWarningIcon");
			childIcon = Resources.Load<Texture2D>("InkChildIcon");
			unknownFileIcon = Resources.Load<Texture2D>("InkUnknownFileIcon");
	    }

	    static void OnDrawProjectWindowItem(string guid, Rect rect) {
	    	if(!InkLibrary.created)
	    		return;
	        var path = AssetDatabase.GUIDToAssetPath(guid);

			if (Path.GetExtension(path) == InkEditorUtils.inkFileExtension) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(path);

				var isSmall = rect.width > rect.height;
				if (isSmall) {
					rect.width = rect.height;
				} else {
					rect.height = rect.width;
				}

				if (rect.width > largeIconSize) {
					var offset = (rect.width - largeIconSize) * 0.5f;
					var position = new Rect(rect.x + offset, rect.y + offset, largeIconSize, largeIconSize);
					if(inkFileIcon != null)
						GUI.DrawTexture(position, inkFileIcon);
				}
				else {
					if(inkFileIcon != null)
						GUI.DrawTexture(rect, inkFileIcon);

					if(inkFile == null) {
						if(unknownFileIcon != null) {
							GUI.DrawTexture(new Rect(rect.x, rect.y, unknownFileIcon.width, unknownFileIcon.height), unknownFileIcon);
						}
					} else {
						Rect miniRect = new Rect(rect.center, rect.size * 0.5f);
						if(inkFile.hasErrors && errorIcon != null) {
							GUI.DrawTexture(miniRect, errorIcon);
						} else if(inkFile.hasWarnings && warningIcon != null) {
							GUI.DrawTexture(miniRect, warningIcon);
						}
						if(!inkFile.isMaster && childIcon != null) {
							GUI.DrawTexture(new Rect(rect.x, rect.y, childIcon.width, childIcon.height), childIcon);
						}
					}
				}
			}
	    }
	}
}