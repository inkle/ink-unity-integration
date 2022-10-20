using System.Collections.Generic;
using System.IO;
using AssetStoreTools.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using AssetStoreTools.Utility.Json;

namespace AssetStoreTools.Validator
{
    internal class PathBoxElement : VisualElement
    {
        private const string PackagesLockPath = "Packages/packages-lock.json";

        private TextField _folderPathField;
        private Button _browseButton;
        
        public PathBoxElement()
        {
            ConstructPathBox();
        }

        public string GetPathBoxValue()
        {
            return _folderPathField.value;
        }

        public void SetPathBoxValue(string path)
        {
            _folderPathField.value = path;
            TestActions.Instance.SetMainPath(path);
        }

        private void ConstructPathBox()
        {
            AddToClassList("path-box");
            
            var pathSelectionBox = new VisualElement();
            pathSelectionBox.AddToClassList("selection-box-row");
            
            VisualElement labelHelpRow = new VisualElement();
            labelHelpRow.AddToClassList("label-help-row");
            
            Label pathLabel = new Label { text = "Folder path" };
            Image pathLabelTooltip = new Image
            {
                tooltip = "Select the main folder of your package" +
                          "\n\nAll files and folders of your package should preferably be contained within a single root folder that is named after your package" +
                          "\n\nExample: 'Assets/[MyPackageName]' or 'Packages/[MyPackageName]'"
            };
            
            labelHelpRow.Add(pathLabel);
            labelHelpRow.Add(pathLabelTooltip);
            
            _folderPathField = new TextField
            {
                label = "",
                isReadOnly = true
            };
            _folderPathField.AddToClassList("path-input-field");
            
            _browseButton = new Button (Browse) {text = "Browse"};
            _browseButton.AddToClassList("browse-button");

            pathSelectionBox.Add(labelHelpRow);
            pathSelectionBox.Add(_folderPathField);
            pathSelectionBox.Add(_browseButton);

            Add(pathSelectionBox);
        }

        private void Browse()
        {
            string result = EditorUtility.OpenFolderPanel("Select a directory", "Assets", "");

            if (result == string.Empty)
                return;

            if (ValidateFolderPath(ref result))
                _folderPathField.value = result;
            else
                return;

            SetPathBoxValue(result);
        }
        
        private bool ValidateFolderPath(ref string resultPath)
        {
            var folderPath = resultPath;
            var pathWithinProject = Application.dataPath.Replace("Assets", "");

            // Selected path is within the project
            if (folderPath.Contains(pathWithinProject))
            {
                var localPath = folderPath.Replace(pathWithinProject, "");

                if (localPath.StartsWith("Packages"))
                {
                    if (IsValidLocalPackage(localPath, out var adbPath))
                    {
                        resultPath = adbPath;
                        return true;
                    }
                }

                if (localPath.StartsWith("Assets"))
                {
                    resultPath = localPath;
                    return true;
                }

                DisplayMessage("Folder not found", "Selection must be within Assets folder or a local package.");
                return false;
            }
            
            bool validLocalPackage = IsValidLocalPackage(folderPath, out var relativePackagePath);

            bool isSymlinkedPath = false;
            string relativeSymlinkPath = string.Empty;
            
            if (ASToolsPreferences.Instance.EnableSymlinkSupport)
                isSymlinkedPath = SymlinkUtil.FindSymlinkFolderRelative(folderPath, out relativeSymlinkPath);
            
            // Selected path is not within the project, but could be a local package or symlinked folder
            if (!validLocalPackage && !isSymlinkedPath)
            {
                DisplayMessage("Folder not found", "Selection must be within Assets folder or a local package.");
                return false;
            }

            resultPath = validLocalPackage ? relativePackagePath : relativeSymlinkPath;
            return true;
        }
        
        private bool IsValidLocalPackage(string packageFolderPath, out string assetDatabasePackagePath)
        {
            assetDatabasePackagePath = string.Empty;
            
            string packageManifestPath = $"{packageFolderPath}/package.json";

            if (!File.Exists(packageManifestPath))
                return false;
            try
            {
                var localPackages = GetAllLocalPackages();

                if (localPackages == null || localPackages.Count == 0)
                    return false;

                foreach (var package in localPackages)
                {
                    var localPackagePath = package.Get("path_absolute").AsString();
                    
                    if (localPackagePath != packageFolderPath) 
                        continue;
                    
                    assetDatabasePackagePath = package.Get("path_assetdb").AsString();
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private List<JsonValue> GetAllLocalPackages()
        {
            try
            {
                string packageLockJsonString = File.ReadAllText(PackagesLockPath);
                JSONParser parser = new JSONParser(packageLockJsonString);
                var packageLockJson = parser.Parse();

                var packages = packageLockJson.Get("dependencies").AsDict();
                var localPackages = new List<JsonValue>();

                foreach (var kvp in packages)
                {
                    var packageSource = kvp.Value.Get("source").AsString();
                    
                    if (!packageSource.Equals("embedded") && !packageSource.Equals("local")) 
                        continue;
                    
                    var packagePath = kvp.Value.Get("version").AsString().Substring("file:".Length);
                        
                    if (packageSource.Equals("embedded"))
                        packagePath = $"Packages/{packagePath}";
                    else if (packageSource.Equals("local") && packagePath.StartsWith("../"))
                        packagePath = packagePath.Substring("../".Length);
                        
                    JsonValue localPackage = new JsonValue
                    {
                        ["name"] = JsonValue.NewString(kvp.Key),
                        ["source"] = JsonValue.NewString(kvp.Value.Get("source")),
                        ["path_absolute"] = JsonValue.NewString(packagePath),
                        ["path_assetdb"] = JsonValue.NewString($"Packages/{kvp.Key}")
                    };
                        
                    localPackages.Add(localPackage);
                }

                return localPackages;
            }
            catch
            {
                return null;
            }
        }

        private void DisplayMessage(string title, string message)
        {
            if (EditorUtility.DisplayDialog(title, message, "Okay", "Cancel"))
                Browse();
        }
    }
}