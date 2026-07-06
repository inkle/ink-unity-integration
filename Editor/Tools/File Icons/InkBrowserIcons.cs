using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Loads the ink file icons and overlays state badges (error / warning / todo / include) on .ink files
	/// in the Project window. The base ink icon itself is set on the imported InkFile asset by InkImporter,
	/// so it shows everywhere; this overlay only adds the badges on top.
	/// </summary>
	[InitializeOnLoad]
	public class InkBrowserIcons {
		// macOS editors use the @2x (retina) variant of the file icon.
		private static bool isRetina => Application.platform == RuntimePlatform.OSXEditor;
	    private const float largeIconSize = 64f;

		private static Texture2D _inkFileIcon;
		public static Texture2D inkFileIcon {
			get {
				if(_inkFileIcon == null) {
					if(isRetina) {
						_inkFileIcon = Resources.Load<Texture2D>("InkFileIcon-retina");
					} else {
						_inkFileIcon = Resources.Load<Texture2D>("InkFileIcon");
					}
				}
				return _inkFileIcon;
			}
		}
		private static Texture2D _inkFileIconLarge;
		public static Texture2D inkFileIconLarge {
			get {
				if(_inkFileIconLarge == null) {
					_inkFileIconLarge = Resources.Load<Texture2D>("InkFileIcon-large");
				}
				return _inkFileIconLarge;
			}
		}
		private static Texture2D _errorIcon;
		public static Texture2D errorIcon {
			get {
				if(_errorIcon == null) {
					_errorIcon = Resources.Load<Texture2D>("InkErrorIcon");
				}
				return _errorIcon;
			}
		}
		private static Texture2D _warningIcon;
		public static Texture2D warningIcon {
			get {
				if(_warningIcon == null) {
					_warningIcon = Resources.Load<Texture2D>("InkWarningIcon");
				}
				return _warningIcon;
			}
		}
		private static Texture2D _todoIcon;
		public static Texture2D todoIcon {
			get {
				if(_todoIcon == null) {
					_todoIcon = Resources.Load<Texture2D>("InkTodoIcon");
				}
				return _todoIcon;
			}
		}
		private static Texture2D _manualIcon;
		public static Texture2D manualIcon {
			get {
				if(_manualIcon == null) {
					_manualIcon = Resources.Load<Texture2D>("InkCompileManualIcon");
				}
				return _manualIcon;
			}
		}
		private static Texture2D _childIcon;
		public static Texture2D childIcon {
			get {
				if(_childIcon == null) {
					_childIcon = Resources.Load<Texture2D>("InkChildIcon");
				}
				return _childIcon;
			}
		}
		private static Texture2D _childIconLarge;
		public static Texture2D childIconLarge {
			get {
				if(_childIconLarge == null) {
					_childIconLarge = Resources.Load<Texture2D>("InkChildIcon-Large");
				}
				return _childIconLarge;
			}
		}
		private static Texture2D _unknownFileIcon;
		public static Texture2D unknownFileIcon {
			get {
				if(_unknownFileIcon == null) {
					_unknownFileIcon = Resources.Load<Texture2D>("InkUnknownFileIcon");
				}
				return _unknownFileIcon;
			}
		}

		static InkBrowserIcons () {
			EditorApplication.projectWindowItemOnGUI += OnDrawProjectWindowItem;
		}

		static void OnDrawProjectWindowItem (string guid, Rect rect) {
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (!InkEditorUtils.IsInkFile(path)) return;
			// Read state straight off the imported InkFile asset (errors/warnings/todos and master/include).
			var inkFile = AssetDatabase.LoadAssetAtPath<InkFile>(path);
			if (inkFile == null) return;
			DrawInkFile(inkFile, rect);
		}

		static void DrawInkFile (InkFile inkFile, Rect rect) {
			bool isSmall = rect.width > rect.height;
			if (isSmall) rect.width = rect.height;
			else rect.height = rect.width;
			if (rect.width >= largeIconSize) DrawLarge(inkFile, rect);
			else DrawSmall(inkFile, rect);
		}

		// The base ink icon is set on the asset itself (InkImporter.AddObjectToAsset), so it shows in
		// every view. This overlay only adds the state badges on top, in the Project window.
		static void DrawLarge (InkFile inkFile, Rect rect) {
			var offset = (rect.width - largeIconSize) * 0.5f;
			rect = new Rect(rect.x + offset, rect.y + offset, largeIconSize, largeIconSize);
			DrawStatusBadge(inkFile, new Rect(rect.center, rect.size * 0.5f));
			if (!inkFile.isMaster && childIconLarge != null)
				GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height * 0.5f), childIconLarge);
		}

		static void DrawSmall (InkFile inkFile, Rect rect) {
			DrawStatusBadge(inkFile, new Rect(rect.center, rect.size * 0.5f));
			if (!inkFile.isMaster && childIcon != null)
				GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height * 0.5f), childIcon);
		}

		static void DrawStatusBadge (InkFile inkFile, Rect rect) {
			if (inkFile.hasErrors && errorIcon != null) GUI.DrawTexture(rect, errorIcon);
			else if (inkFile.hasWarnings && warningIcon != null) GUI.DrawTexture(rect, warningIcon);
			else if (inkFile.hasTodos && todoIcon != null) GUI.DrawTexture(rect, todoIcon);
		}

	}
}