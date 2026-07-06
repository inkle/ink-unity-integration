using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
    /// <summary>
    /// Inspector for the InkImporter import settings. Master files (not INCLUDEd by another file) are
    /// detected automatically and compiled. For include files, exposes the optional "Compile As Master
    /// File" override — the equivalent of the old "Should also be Master File" tickbox.
    /// </summary>
    [CustomEditor(typeof(InkImporter))]
    public class InkImporterEditor : ScriptedImporterEditor {
        public override VisualElement CreateInspectorGUI () {
            var root = new VisualElement();
            var importer = (InkImporter)target;

            // Give the imported asset its badged per-object icon so the header shows the same icon as the
            // Project window (which uses the baked thumbnail). Only runs for the inspected asset — no scan.
            InkBrowserIcons.ApplyInstanceIcon(assetTarget as InkFile);

            if (importer.IsMasterFile) {
                var label = new Label("Master file (detected automatically).");
                label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Italic;
                label.style.marginTop = 2;
                label.style.marginBottom = 4;
                root.Add(label);
            } else {
                var field = new PropertyField(serializedObject.FindProperty("compileAsMasterFileOverride"), "Compile As Master File") {
                    tooltip = "Also compile this file as a master, even though it's included by another file. " +
                              "(The equivalent of the old \"Should also be Master File\" option.)"
                };
                root.Add(field);
            }

            // ScriptedImporterEditor's Apply/Revert buttons are IMGUI-only; host them in an IMGUIContainer.
            root.Add(new IMGUIContainer(ApplyRevertGUI));
            return root;
        }
    }
}
