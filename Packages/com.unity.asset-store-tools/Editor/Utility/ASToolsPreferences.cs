using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Utility {

    internal class ASToolsPreferences
    {
        private static ASToolsPreferences s_instance;
        public static ASToolsPreferences Instance => s_instance ?? (s_instance = new ASToolsPreferences());

        private ASToolsPreferences()
        {
            Load();
        }

        private void Load()
        {
            LegacyVersionCheck = PlayerPrefs.GetInt("AST_LegacyVersionCheck", 1) == 1;
            UploadVersionCheck = PlayerPrefs.GetInt("AST_UploadVersionCheck", 1) == 1;
            EnableSymlinkSupport = PlayerPrefs.GetInt("AST_EnableSymlinkSupport", 0) == 1;
            UseCustomExporting = PlayerPrefs.GetInt("AST_UseCustomExporting", 0) == 1;
        }

        public void Save()
        {
            PlayerPrefs.SetInt("AST_LegacyVersionCheck", LegacyVersionCheck ? 1 : 0);
            PlayerPrefs.SetInt("AST_UploadVersionCheck", UploadVersionCheck ? 1 : 0);
            PlayerPrefs.SetInt("AST_EnableSymlinkSupport", EnableSymlinkSupport ? 1 : 0);
            PlayerPrefs.SetInt("AST_UseCustomExporting", UseCustomExporting ? 1 : 0);
        }

        /// <summary>
        /// Check if legacy Asset Store Tools are in the Project
        /// </summary>
        public bool LegacyVersionCheck;

        /// <summary>
        /// Check if the package has been uploader from a correct Unity version at least once
        /// </summary>
        public bool UploadVersionCheck;

        /// <summary>
        /// Enables Junction symlink support
        /// </summary>
        public bool EnableSymlinkSupport;

        /// <summary>
        /// Enables custom exporting for Folder Upload workflow
        /// </summary>
        public bool UseCustomExporting;
    }
    
    internal class ASToolsPreferencesProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/Asset Store Tools";
        
        private class Styles
        {
            public static readonly GUIContent LegacyVersionCheckLabel = EditorGUIUtility.TrTextContent("Legacy ASTools Check", "Enable Legacy Asset Store Tools version checking.");
            public static readonly GUIContent UploadVersionCheckLabel = EditorGUIUtility.TrTextContent("Upload Version Check", "Check if the package has been uploader from a correct Unity version at least once.");
            public static readonly GUIContent EnableSymlinkSupportLabel = EditorGUIUtility.TrTextContent("Enable Symlink Support", "Enable Junction Symlink support. Note: folder selection validation will take longer.");
            public static readonly GUIContent UseCustomExportingLabel = EditorGUIUtility.TrTextContent("Use Custom Exporting (Experimental)", "Enable Custom Exporting for the Folder Upload workflow. Note: it's a little bit faster than regular, but might miss some Asset previews in the final product. Does not affect the functionality of the exported package.");
        }

        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        private ASToolsPreferencesProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) { }
        
        public override void OnGUI(string searchContext)
        {
            var preferences = ASToolsPreferences.Instance;
            EditorGUI.BeginChangeCheck();
            using (CreateSettingsWindowGUIScope())
            {
                preferences.LegacyVersionCheck = EditorGUILayout.Toggle(Styles.LegacyVersionCheckLabel, preferences.LegacyVersionCheck);
                preferences.UploadVersionCheck = EditorGUILayout.Toggle(Styles.UploadVersionCheckLabel, preferences.UploadVersionCheck);
                preferences.EnableSymlinkSupport = EditorGUILayout.Toggle(Styles.EnableSymlinkSupportLabel, preferences.EnableSymlinkSupport);
                preferences.UseCustomExporting = EditorGUILayout.Toggle(Styles.UseCustomExportingLabel, preferences.UseCustomExporting);

                if (preferences.UseCustomExporting)
                {
                    var cstExpWarning = "Custom exporter is an experimental feature. " + 
                    "It packs selected Assets without using the native Unity API and is observed to be slightly faster.\n\n" +
                    "Please note that Asset preview images used to showcase specific asset types (Textures, Materials, Prefabs) before importing the package " +
                    "might not be generated consistently at this time. This does not affect functionality of the package after it gets imported.";
                    
                    EditorGUILayout.HelpBox(cstExpWarning, MessageType.Warning, true);
                }
            }

            if (EditorGUI.EndChangeCheck())
                ASToolsPreferences.Instance.Save();
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateAssetStoreToolsSettingProvider()
        {
            var provider = new ASToolsPreferencesProvider(SettingsPath, SettingsScope.Project, GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
        
        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }
    }
}
