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
		private static bool isRetina {
			get {
				float unityVersion = float.Parse(Application.unityVersion.Substring (0, 3));
				return Application.platform == RuntimePlatform.OSXEditor && unityVersion >= 5.4f;
			}
		}
	    private const float largeIconSize = 64f;

		private static Texture2D _inkFileIcon;
		public static Texture2D inkFileIcon {
			get {
				if(_inkFileIcon == null) {
					if(isRetina) {
						_inkFileIcon = (Texture2D)EditorGUIUtility.Load("InkFileIcon-retina.psd");
					} else {
						_inkFileIcon = (Texture2D)EditorGUIUtility.Load("InkFileIcon.psd");
					}
				}
				return _inkFileIcon;
			}
		}
		private static Texture2D _inkFileIconLarge;
		public static Texture2D inkFileIconLarge {
			get {
				if(_inkFileIconLarge == null) {
					_inkFileIconLarge = (Texture2D)EditorGUIUtility.Load("InkFileIcon-large.psd");
				}
				return _inkFileIconLarge;
			}
		}
		private static Texture2D _errorIcon;
		public static Texture2D errorIcon {
			get {
				if(_errorIcon == null) {
					_errorIcon = (Texture2D)EditorGUIUtility.Load("InkErrorIcon.psd");
				}
				return _errorIcon;
			}
		}
		private static Texture2D _warningIcon;
		public static Texture2D warningIcon {
			get {
				if(_warningIcon == null) {
					_warningIcon = (Texture2D)EditorGUIUtility.Load("InkWarningIcon.psd");
				}
				return _warningIcon;
			}
		}
		private static Texture2D _todoIcon;
		public static Texture2D todoIcon {
			get {
				if(_todoIcon == null) {
					_todoIcon = (Texture2D)EditorGUIUtility.Load("InkTodoIcon.psd");
				}
				return _todoIcon;
			}
		}
		private static Texture2D _manualIcon;
		public static Texture2D manualIcon {
			get {
				if(_manualIcon == null) {
					_manualIcon = (Texture2D)EditorGUIUtility.Load("InkCompileManualIcon.psd");
				}
				return _manualIcon;
			}
		}
		private static Texture2D _childIcon;
		public static Texture2D childIcon {
			get {
				if(_childIcon == null) {
					_childIcon = (Texture2D)EditorGUIUtility.Load("InkChildIcon.psd");
				}
				return _childIcon;
			}
		}
		private static Texture2D _childIconLarge;
		public static Texture2D childIconLarge {
			get {
				if(_childIconLarge == null) {
					_childIconLarge = (Texture2D)EditorGUIUtility.Load("InkChildIcon-Large.psd");
				}
				return _childIconLarge;
			}
		}
		private static Texture2D _unknownFileIcon;
		public static Texture2D unknownFileIcon {
			get {
				if(_unknownFileIcon == null) {
					_unknownFileIcon = (Texture2D)EditorGUIUtility.Load("InkUnknownFileIcon.psd");
				}
				return _unknownFileIcon;
			}
		}

	    static InkBrowserIcons() {
			EditorApplication.projectWindowItemOnGUI += OnDrawProjectWindowItem;
	    }

	    static void OnDrawProjectWindowItem(string guid, Rect rect) {
	        string path = AssetDatabase.GUIDToAssetPath(guid);
			if (Path.GetExtension(path) == InkEditorUtils.inkFileExtension && InkLibrary.created) {
				DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
				DrawInkFile(InkLibrary.GetInkFileWithFile(asset), rect);
			}
	    }

		static void DrawInkFile (InkFile inkFile, Rect rect) {
			bool isSmall = rect.width > rect.height;
			if (isSmall) {
				rect.width = rect.height;
			} else {
				rect.height = rect.width;
			}
			if (rect.width >= largeIconSize) {
				DrawLarge(inkFile, rect);
			} else {
				DrawSmall(inkFile, rect);
			}
	    }

		static void DrawLarge (InkFile inkFile, Rect rect) {
			var offset = (rect.width - largeIconSize) * 0.5f;
			rect = new Rect(rect.x + offset, rect.y + offset, largeIconSize, largeIconSize);
			if(inkFileIconLarge != null)
				GUI.DrawTexture(rect, inkFileIconLarge);

			Rect miniRect = new Rect(rect.center, rect.size * 0.5f);
			if(inkFile == null) {
				if(unknownFileIcon != null) {
					GUI.DrawTexture(miniRect, unknownFileIcon);
				}
			} else {
				if(inkFile.metaInfo.hasErrors && errorIcon != null) {
					GUI.DrawTexture(miniRect, errorIcon);
				} else if(inkFile.metaInfo.hasWarnings && warningIcon != null) {
					GUI.DrawTexture(miniRect, warningIcon);
				} else if(inkFile.metaInfo.hasTodos && todoIcon != null) {
					GUI.DrawTexture(miniRect, todoIcon);
				}
				if(!inkFile.metaInfo.isMaster && childIcon != null) {
					GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height * 0.5f), childIconLarge);
				}
			}
		}

		static void DrawSmall (InkFile inkFile, Rect rect) {
			if(inkFileIcon != null)
				GUI.DrawTexture(rect, inkFileIcon);

			if(inkFile == null) {
				if(unknownFileIcon != null) {
					GUI.DrawTexture(new Rect(rect.x, rect.y, unknownFileIcon.width, unknownFileIcon.height), unknownFileIcon);
				}
			} else {
				if(!InkSettings.Instance.compileAutomatically && !inkFile.metaInfo.masterInkFileIncludingSelf.compileAutomatically)
					GUI.DrawTexture(new Rect(rect.x, rect.y + rect.size.y * 0.5f, rect.size.x * 0.5f, rect.size.y * 0.5f), manualIcon);

				Rect miniRect = new Rect(rect.center, rect.size * 0.5f);
				if(inkFile.metaInfo.hasErrors && errorIcon != null) {
					GUI.DrawTexture(miniRect, errorIcon);
				} else if(inkFile.metaInfo.hasWarnings && warningIcon != null) {
					GUI.DrawTexture(miniRect, warningIcon);
				} else if(inkFile.metaInfo.hasTodos && todoIcon != null) {
					GUI.DrawTexture(miniRect, todoIcon);
				}
				if(!inkFile.metaInfo.isMaster && childIcon != null) {
					GUI.DrawTexture(new Rect(rect.x, rect.y, childIcon.width, childIcon.height), childIcon);
				}
			}
	    }
	}
}