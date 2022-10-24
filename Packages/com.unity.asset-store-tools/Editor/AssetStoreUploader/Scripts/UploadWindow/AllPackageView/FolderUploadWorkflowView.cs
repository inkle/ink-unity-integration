using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AssetStoreTools.Utility;
using AssetStoreTools.Utility.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader
{
    internal class FolderUploadWorkflowView : UploadWorkflowView
    {
        public const string WorkflowName = "FolderWorkflow";
        public const string WorkflowDisplayName = "From Assets Folder";

        public override string Name => WorkflowName;
        public override string DisplayName => WorkflowDisplayName;

        private Toggle _dependenciesToggle;
        private bool _isCompleteProject;
        private string _category;

        private ValidationElement _validationElement;
        private VisualElement _specialFoldersElement;

        // Special folders that would not work if not placed directly in the 'Assets' folder
        private readonly string[] _extraAssetFolderNames =
        {
            "Editor Default Resources", "Gizmos", "Plugins",
            "StreamingAssets", "Standard Assets", "WebGLTemplates",
            "ExternalDependencyManager"
        };

        private FolderUploadWorkflowView(string category, bool isCompleteProject, Action serializeSelection) : base(serializeSelection)
        {
            _isCompleteProject = isCompleteProject;
            _category = category;
            
            SetupWorkflow();
        }

        public static FolderUploadWorkflowView Create(string category, bool isCompleteProject, Action serializeAction)
        {
            return new FolderUploadWorkflowView(category, isCompleteProject, serializeAction);
        }

        public void SetCompleteProject(bool isCompleteProject)
        {
            _isCompleteProject = isCompleteProject;
        }

        public bool GetIncludeDependencies()
        {
            return _dependenciesToggle.value;
        }

        protected sealed override void SetupWorkflow()
        {
            // Path selection
            VisualElement folderPathSelectionRow = new VisualElement();
            folderPathSelectionRow.AddToClassList("selection-box-row");

            VisualElement labelHelpRow = new VisualElement();
            labelHelpRow.AddToClassList("label-help-row");

            Label folderPathLabel = new Label { text = "Folder path" };
            Image folderPathLabelTooltip = new Image
            {
                tooltip = "Select the main folder of your package" +
                "\n\nAll files and folders of your package should preferably be contained within a single root folder that is named after your package" +
                "\n\nExample: 'Assets/[MyPackageName]'" +
                "\n\nNote: If your content makes use of special folders that are required to be placed in the root Assets folder (e.g. 'StreamingAssets')," +
                " you will be able to include them after selecting the main folder"
            };

            labelHelpRow.Add(folderPathLabel);
            labelHelpRow.Add(folderPathLabelTooltip);

            PathSelectionField = new TextField();
            PathSelectionField.AddToClassList("path-selection-field");
            PathSelectionField.isReadOnly = true;

            Button browsePathButton = new Button(BrowsePath) { name = "BrowsePathButton", text = "Browse" };
            browsePathButton.AddToClassList("browse-button");

            folderPathSelectionRow.Add(labelHelpRow);
            folderPathSelectionRow.Add(PathSelectionField);
            folderPathSelectionRow.Add(browsePathButton);

            Add(folderPathSelectionRow);

            // Dependencies selection
            VisualElement dependenciesSelectionRow = new VisualElement();
            dependenciesSelectionRow.AddToClassList("selection-box-row");

            VisualElement dependenciesLabelHelpRow = new VisualElement();
            dependenciesLabelHelpRow.AddToClassList("label-help-row");

            Label dependenciesLabel = new Label { text = "Dependencies" };
            Image dependenciesLabelTooltip = new Image
            {
                tooltip = "Tick this checkbox if your package content has dependencies on Unity packages from the Package Manager"
            };

            _dependenciesToggle = new Toggle { name = "DependenciesToggle", text = "Include Package Manifest" };
            _dependenciesToggle.AddToClassList("dependencies-toggle");
            _dependenciesToggle.RegisterValueChangedCallback((_) => _serializeSelection?.Invoke());

            dependenciesLabelHelpRow.Add(dependenciesLabel);
            dependenciesLabelHelpRow.Add(dependenciesLabelTooltip);

            dependenciesSelectionRow.Add(dependenciesLabelHelpRow);
            dependenciesSelectionRow.Add(_dependenciesToggle);

            Add(dependenciesSelectionRow);

            _validationElement = new ValidationElement();
            Add(_validationElement);
            
            _validationElement.SetCategory(_category);
        }

        public override JsonValue SerializeWorkflow()
        {
            var workflowDict = base.SerializeWorkflow();
            workflowDict["dependencies"] = GetIncludeDependencies();

            return workflowDict;
        }

        public override void LoadSerializedWorkflow(JsonValue json, string lastUploadedPath, string lastUploadedGuid)
        {
            if(!DeserializeMainExportPath(json, out string mainExportPath) || !Directory.Exists(mainExportPath))
            {
                ASDebug.Log("Unable to restore Folder upload workflow paths from the local cache");
                LoadSerializedWorkflowFallback(lastUploadedPath, lastUploadedGuid);
                return;
            }

            DeserializeExtraExportPaths(json, out List<string> extraExportPaths);

            var dependenciesToggle = json["dependencies"].AsBool();

            ASDebug.Log($"Restoring serialized Folder workflow values from local cache");
            HandleFolderUploadPathSelection(mainExportPath, extraExportPaths, false);
            _dependenciesToggle.SetValueWithoutNotify(dependenciesToggle);
        }

        public override void LoadSerializedWorkflowFallback(string lastUploadedPath, string lastUploadedGuid)
        {
            var mainExportPath = AssetDatabase.GUIDToAssetPath(lastUploadedGuid);
            if (string.IsNullOrEmpty(mainExportPath))
                mainExportPath = lastUploadedPath;

            if ((!mainExportPath.StartsWith("Assets/") && mainExportPath != "Assets") || !Directory.Exists(mainExportPath))
            {
                ASDebug.Log("Unable to restore Folder workflow paths from previous upload values");
                return;
            }

            ASDebug.Log($"Restoring serialized Folder workflow values from previous upload values");
            HandleFolderUploadPathSelection(mainExportPath, null, false);
        }

        #region Folder Upload

        protected override void BrowsePath()
        {
            // Path retrieval
            var absoluteExportPath = string.Empty;
            var relativeExportPath = string.Empty;
            var rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

            bool includeAllAssets = false;

            if (_isCompleteProject)
            {
                includeAllAssets = EditorUtility.DisplayDialog("Notice",
                    "Your package draft is set to a category that is treated" +
                    " as a complete project. Project settings will be included automatically. Would you like everything in the " +
                    "'Assets' folder to be included?\n\nYou will still be able to change the selected assets before uploading",
                    "Yes, include all folders and assets",
                    "No, I'll select what to include manually");
                if (includeAllAssets)
                    absoluteExportPath = Application.dataPath;
            }

            if (!includeAllAssets)
            {
                absoluteExportPath =
                    EditorUtility.OpenFolderPanel("Select folder to compress into a package", "Assets/", "");
                if (string.IsNullOrEmpty(absoluteExportPath))
                    return;
            }
            
            if (absoluteExportPath.StartsWith(rootProjectPath))
            {
                relativeExportPath = absoluteExportPath.Substring(rootProjectPath.Length);
            }
            else
            {
                if (ASToolsPreferences.Instance.EnableSymlinkSupport)
                    SymlinkUtil.FindSymlinkFolderRelative(absoluteExportPath, out relativeExportPath);
            }

            if (!relativeExportPath.StartsWith("Assets/") && !(relativeExportPath == "Assets" && _isCompleteProject))
            {
                if (relativeExportPath.StartsWith("Assets") && !_isCompleteProject)
                    EditorUtility.DisplayDialog("Invalid selection",
                        "'Assets' folder is only available for packages tagged as a 'Complete Project'.", "OK");
                else
                    EditorUtility.DisplayDialog("Invalid selection", "Selected folder path must be within the project.",
                        "OK");
                return;
            }

            HandleFolderUploadPathSelection(relativeExportPath, null, true);
        }

        private void HandleFolderUploadPathSelection(string relativeExportPath, List<string> serializedToggles, bool serializeValues)
        {
            PathSelectionField.value = relativeExportPath + "/";

            MainExportPath = relativeExportPath;
            ExtraExportPaths = new List<string>();

            LocalPackageGuid = AssetDatabase.AssetPathToGUID(MainExportPath);
            LocalPackagePath = MainExportPath;
            LocalProjectPath = MainExportPath;

            _validationElement.SetLocalPath(MainExportPath);

            if (_specialFoldersElement != null)
            {
                _specialFoldersElement.Clear();
                Remove(_specialFoldersElement);

                _specialFoldersElement = null;
            }

            // Prompt additional path selection (e.g. StreamingAssets, WebGLTemplates, etc.)
            List<string> specialFoldersFound = new List<string>();

            foreach (var extraAssetFolderName in _extraAssetFolderNames)
            {
                var fullExtraPath = "Assets/" + extraAssetFolderName;

                if (!Directory.Exists(fullExtraPath))
                    continue;

                if (MainExportPath.ToLower().StartsWith(fullExtraPath.ToLower()))
                    continue;

                // Don't include nested paths
                if (!fullExtraPath.ToLower().StartsWith(MainExportPath.ToLower()))
                    specialFoldersFound.Add(fullExtraPath);
            }

            if (specialFoldersFound.Count != 0)
                PopulateExtraPathsBox(specialFoldersFound, serializedToggles);

            // Only serialize current selection when no serialized toggles were passed
            if (serializeValues)
                _serializeSelection?.Invoke();
        }

        private void PopulateExtraPathsBox(List<string> specialFoldersFound, List<string> checkedToggles)
        {
            // Dependencies selection
            _specialFoldersElement = new VisualElement();
            _specialFoldersElement.AddToClassList("selection-box-row");

            VisualElement specialFoldersHelpRow = new VisualElement();
            specialFoldersHelpRow.AddToClassList("label-help-row");

            Label specialFoldersLabel = new Label { text = "Special folders" };
            Image specialFoldersLabelTooltip = new Image
            {
                tooltip =
                    "If your package content relies on Special Folders (e.g. StreamingAssets), please select which of these folders should be included in the package"
            };

            VisualElement specialFolderTogglesBox = new VisualElement { name = "SpecialFolderToggles" };
            specialFolderTogglesBox.AddToClassList("special-folders-toggles-box");

            specialFoldersHelpRow.Add(specialFoldersLabel);
            specialFoldersHelpRow.Add(specialFoldersLabelTooltip);

            _specialFoldersElement.Add(specialFoldersHelpRow);
            _specialFoldersElement.Add(specialFolderTogglesBox);

            EventCallback<ChangeEvent<bool>, string> toggleChangeCallback = OnSpecialFolderPathToggledAsset;

            foreach (var path in specialFoldersFound)
            {
                var toggle = new Toggle { value = false, text = path };
                toggle.AddToClassList("special-folder-toggle");
                if (checkedToggles != null && checkedToggles.Contains(toggle.text))
                {
                    toggle.SetValueWithoutNotify(true);
                    ExtraExportPaths.Add(toggle.text);
                }

                toggle.RegisterCallback(toggleChangeCallback, toggle.text);
                specialFolderTogglesBox.Add(toggle);
            }

            Add(_specialFoldersElement);
        }

        private void OnSpecialFolderPathToggledAsset(ChangeEvent<bool> evt, string folderPath)
        {
            switch (evt.newValue)
            {
                case true when !ExtraExportPaths.Contains(folderPath):
                    ExtraExportPaths.Add(folderPath);
                    break;
                case false when ExtraExportPaths.Contains(folderPath):
                    ExtraExportPaths.Remove(folderPath);
                    break;
            }

            _serializeSelection?.Invoke();
        }

        public override async Task<PackageExporter.ExportResult> ExportPackage(bool isCompleteProject)
        {
            var paths = GetAllExportPaths();
            var includeDependencies = GetIncludeDependencies();
            var outputPath = $"{FileUtil.GetUniqueTempPathInProject()}-{Name}.unitypackage";

            return await PackageExporter.ExportPackage(paths, outputPath, includeDependencies, isCompleteProject, ASToolsPreferences.Instance.UseCustomExporting);
        }

        #endregion
    }
}