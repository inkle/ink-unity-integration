using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Loads the ink file icons and composites the base ink icon with its state badges
	/// (error / warning / todo, plus a child badge for include files) into a single texture.
	///
	/// InkImporter bakes that composite into the imported InkFile asset's thumbnail at import time, so the
	/// icon (badges included) shows everywhere the asset icon is used: Project window, inspector header and
	/// object fields. There is no live Project-window overlay — the badges are part of the asset's icon.
	/// State only changes on reimport (compile happens on import; include-graph changes trigger a reimport),
	/// so a baked icon is always up to date.
	/// </summary>
	// [InitializeOnLoad] only to register the domain-reload cleanup below — no per-frame or startup work.
	[InitializeOnLoad]
	public static class InkBrowserIcons {
		static InkBrowserIcons () {
			// The composited icons are HideAndDontSave, so they outlive a domain reload, but the static caches
			// that reference them don't. Destroy them before each reload so they don't accumulate over a
			// session of recompiles.
			AssemblyReloadEvents.beforeAssemblyReload += DisposeCachedIcons;
		}

		static void DisposeCachedIcons () {
			foreach (var entry in _instanceIcons.Values)
				if (entry.icon != null) Object.DestroyImmediate(entry.icon);
			_instanceIcons.Clear();
			foreach (var icon in _thumbnailCache.Values)
				if (icon != null) Object.DestroyImmediate(icon);
			_thumbnailCache.Clear();
		}

		// macOS editors use the @2x (retina) variant of the file icon.
		private static bool isRetina => Application.platform == RuntimePlatform.OSXEditor;

		// The base used when compositing the asset icon: the full-size InkFileIcon art (not the retina/small
		// variants), so Unity's downscaling to 16px stays crisp.
		private static Texture2D _inkFileIconBase;
		public static Texture2D inkFileIconBase {
			get {
				if(_inkFileIconBase == null) _inkFileIconBase = Resources.Load<Texture2D>("InkFileIcon");
				return _inkFileIconBase;
			}
		}

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

		// Encodes the four badge-relevant flags into a small int, so icons can be keyed/compared by state.
		static int StateKey (bool isMaster, bool hasErrors, bool hasWarnings, bool hasTodos) =>
			(isMaster ? 1 : 0) | (hasErrors ? 2 : 0) | (hasWarnings ? 4 : 0) | (hasTodos ? 8 : 0);

		/// <summary>
		/// Builds the icon baked into an InkFile asset's thumbnail by InkImporter (base ink icon + state
		/// badges). Cached by state — the same instance is safe to reuse here because Unity copies the
		/// thumbnail's pixels into the asset rather than retaining the texture. Returns the plain base icon
		/// if compositing isn't possible (base texture not readable).
		/// </summary>
		static readonly Dictionary<int, Texture2D> _thumbnailCache = new Dictionary<int, Texture2D>();

		public static Texture2D BuildInkFileThumbnail (bool isMaster, bool hasErrors, bool hasWarnings, bool hasTodos) {
			var key = StateKey(isMaster, hasErrors, hasWarnings, hasTodos);
			if (_thumbnailCache.TryGetValue(key, out var cached) && cached != null) return cached;
			var result = Composite(isMaster, hasErrors, hasWarnings, hasTodos);
			if (result == null) return inkFileIconBase != null ? inkFileIconBase : inkFileIcon;
			_thumbnailCache[key] = result;
			return result;
		}

		/// <summary>
		/// Gives a specific InkFile instance a per-object icon (base + state badges) via SetIconForObject.
		///
		/// The baked asset thumbnail (see InkImporter) drives the Project window, but the inspector header and
		/// object fields resolve their icon through AssetPreview.GetMiniThumbnail, which uses the per-object
		/// icon — not the asset thumbnail. Setting a per-object icon is what makes badges show in those
		/// surfaces too. Call this only when an InkFile is actually being shown (an inspector or a reference
		/// field), so there is no project-wide scan.
		///
		/// Each file gets its OWN texture instance (keyed by asset path): handing one shared texture to
		/// SetIconForObject for several objects makes Unity show the wrong object's icon. The instance is
		/// reused and only rebuilt when that file's state changes, so repeated opens don't leak textures.
		/// </summary>
		static readonly Dictionary<string, (int state, Texture2D icon)> _instanceIcons = new Dictionary<string, (int, Texture2D)>();

		public static void ApplyInstanceIcon (InkFile inkFile) {
			if (inkFile == null) return;
			var path = AssetDatabase.GetAssetPath(inkFile);
			var state = StateKey(inkFile.isMaster, inkFile.hasErrors, inkFile.hasWarnings, inkFile.hasTodos);

			if (_instanceIcons.TryGetValue(path, out var existing) && existing.icon != null) {
				if (existing.state == state) { EditorGUIUtility.SetIconForObject(inkFile, existing.icon); return; }
				Object.DestroyImmediate(existing.icon); // state changed — replace our own composite, don't leak
			}

			var icon = Composite(inkFile.isMaster, inkFile.hasErrors, inkFile.hasWarnings, inkFile.hasTodos);
			if (icon == null) {
				// Can't composite (base not readable) — fall back to the plain base icon, which we don't own.
				_instanceIcons.Remove(path);
				var fallback = inkFileIconBase != null ? inkFileIconBase : inkFileIcon;
				if (fallback != null) EditorGUIUtility.SetIconForObject(inkFile, fallback);
				return;
			}
			_instanceIcons[path] = (state, icon);
			EditorGUIUtility.SetIconForObject(inkFile, icon);
		}

		// Builds a fresh composited icon (base ink icon + child/status badges). Composites on the CPU
		// (GetPixels/SetPixels into an in-memory array — far faster than per-pixel GetPixel/SetPixel native
		// calls) so it's safe on background import workers, which may have no GPU. Returns null if the base
		// icon can't be read (Read/Write disabled); callers fall back to the plain base icon.
		static Texture2D Composite (bool isMaster, bool hasErrors, bool hasWarnings, bool hasTodos) {
			var baseIcon = inkFileIconBase != null ? inkFileIconBase : inkFileIcon;
			if (baseIcon == null || !baseIcon.isReadable) return null;

			int w = baseIcon.width, h = baseIcon.height;
			var pixels = baseIcon.GetPixels();

			// Include files get a child badge in the top-left.
			if (!isMaster) BlendQuadrant(pixels, w, h, childIconLarge != null ? childIconLarge : childIcon, Quadrant.TopLeft);

			// A single status badge in the bottom-right: errors take priority over warnings over todos.
			var status = hasErrors ? errorIcon : hasWarnings ? warningIcon : hasTodos ? todoIcon : null;
			if (status != null) BlendQuadrant(pixels, w, h, status, Quadrant.BottomRight);

			var result = new Texture2D(w, h, TextureFormat.RGBA32, false) {
				hideFlags = HideFlags.HideAndDontSave,
				name = "InkFileIcon (composited)"
			};
			result.SetPixels(pixels);
			result.Apply();
			return result;
		}

		enum Quadrant { TopLeft, BottomRight }

		// Alpha-blends a badge into one quadrant of the target pixel array (row-major, bottom-left origin,
		// width w / height h), nearest-neighbour scaling the badge to the quadrant so mismatched sizes still
		// line up. "Top-left" visually maps to the upper rows (higher y).
		static void BlendQuadrant (Color[] dst, int w, int h, Texture2D badge, Quadrant q) {
			if (badge == null || !badge.isReadable) return;
			int qw = w / 2, qh = h / 2;
			int ox = q == Quadrant.BottomRight ? w - qw : 0;
			int oy = q == Quadrant.TopLeft ? h - qh : 0;
			var badgePixels = badge.GetPixels();
			int bw = badge.width, bh = badge.height;
			for (int y = 0; y < qh; y++) {
				int sy = Mathf.Clamp(y * bh / qh, 0, bh - 1);
				for (int x = 0; x < qw; x++) {
					int sx = Mathf.Clamp(x * bw / qw, 0, bw - 1);
					var s = badgePixels[sy * bw + sx];
					if (s.a <= 0f) continue;
					int di = (oy + y) * w + (ox + x);
					var d = dst[di];
					var blended = Color.Lerp(d, s, s.a);
					blended.a = Mathf.Max(d.a, s.a);
					dst[di] = blended;
				}
			}
		}
	}
}