## Version 2.0.0 (7th July 2026):
Ink files are now compiled by a Unity ScriptedImporter, replacing the old compile queue and its per-file `.json` output.

- ⚠️ Breaking: `.ink` files import as an `InkFile` asset — no `.json` is generated. Reference `InkFile` and use `new Story(inkFile.storyJson)`.
- ⚠️ Breaking: minimum Unity version is now 2022.3 LTS.
- Editing an include (even nested) now reliably reimports its master file(s).
- Errors, warnings and TODOs show on the import inspector and in the console.
- Ink file icons show state badges (error, warning, TODO, include) consistently in the Project window, inspector header and object fields.
- Builds now fail if any ink file has compile errors.
- Ink Player: load a story by assigning an `InkFile` (not a JSON `TextAsset`); tethered stories let you edit variables by default; the history-visibility filter and the auto-play controls are now compact popups.
- Ink Player: auto-play can use a fixed random seed to replay the exact same route — seeds both `RANDOM()`/shuffles and Auto-Choice.
- Removed `InkLibrary`, the `InkCompiler` queue and the auto-compiler; "Rebuild Ink Library" is now "Recompile All Ink Files".
- Editor inspectors and windows rebuilt with UI Toolkit.

### Migrating from 1.x — see MIGRATION.md
1. Update to Unity 2022.3+.
2. Reference the `.ink`'s `InkFile` instead of the `.json`, and use `new Story(inkFile.storyJson)`.
3. Delete the old `.json` files with the **Migrate Ink Project from 1.x** button in Project Settings ▸ Ink (also offered in the Ink Update window).

## Version 1.3.0 (5th July 2026):
- Updated Ink to 1.2.1
- ⚠️ The minimum supported Unity version is now 2022.3 LTS
- Updated for Unity 6: resolves obsolete API warnings (script define symbols now use NamedBuildTarget) and the Unity 6 serialization analyzer warnings
- Removed legacy code paths for Unity versions older than 2022.3, modernising the editor code (e.g. using TypeCache for inspector lookup)
- The demo now works with both the legacy Input Manager and the new Input System

## Version 1.2.1 (31st July 2024):
- Fixes broken demo script

## Version 1.2.0 (12th July 2024):
- 🎉 Updated Ink to 1.2.0! See whats new!
- Some significant editor performance improvements
- #173 Add support for automatically adding #INK_RUNTIME and #INK_EDITOR defines. Go to Project Settings -> Ink Settings to toggle it!

## Version 1.1.8 (11th July 2023):
- Update the demo scene to Unity 2020.3.25f1 to improve compatibility with more recent versions
- Fixes a missing GUIStyle in the Ink Player Window in recent versions of Unity
- Optimise the Ink Player Window for large projects
- Automatically populate the changelog on the startup window

## Version 1.1.7 (20th Feb 2023):
- Rework of the plugin's INCLUDE hierarchy system, allowing for previously unhandled valid setups
- Changes the OpenInEditor function to use AssetDatabase.OpenAsset, which correctly uses the OS file editor
- Prevents the Ink Player Window from showing itself when scripts are recompiled

## Version 1.1.5 (2nd December 2022):
- Adds InkSettings.suppressStartupWindow, which can be used to prevent this window from appearing (requested for some CI/CD pipelines)
- Adds links to Discord for community support in help menu, startup window and setting menu
- Fixes an issue where InkSettings ScriptableObjects wouldn't be unloaded
- Updates build documentation for this plugin

## Version 1.1.1 (20th October 2022):
- Updates ink to 1.1.1.
- The InkCompiler.OnCompileInk event now fires once when the compilation stack completes and returns an array of compiled files
- Fixes some async threading issues when compiling
- Adds JSON formatting for save states copied or saved via the Ink Player Window
- Use the Unity Progress API to show compilation. Useful for large ink projects!
- Included files now show their own included files in the Inspector
- Various optimisations

## Version 1.0.2:
- Fix a very rare but quite nasty compilation bug

## Version 🎉1.0.0🎉:
- Update ink to 1.0.0
- Ink Editor Window: Allow resizing (some) panels
- Ink Editor Window: Named content panel 
- Ink Editor Window: Improved performance for large stories
- Allow compiling include files that don't have the .ink file extension
- Remove ability to use a custom inklecate (legacy compiler)
- Fixes settings menu on 2020+
- Improved migration from earlier versions
- Moved persistent compilation tracking code from InkLibrary into InkCompiler
- Use Unity's new ScriptableSingleton for InkLibrary, InkSettings and InkCompiler on 2020+

## Version 0.9.71:
- Resolves some compilation issues

## Version 0.9.60:
- Moved InkLibrary and InkSettings from Assets into Library and ProjectSettings
   - InkLibrary should no longer be tracked in source control
   - Changes to InkSettings must be migrated manually
   - The InkLibrary and InkSettings files in your project folder should be deleted
- Added a divertable list of knots, stitches and other named content to the Ink Editor Window, replacing the Diverts subpanel

## Version 0.9.4
Bug fixes

## Version 0.9.24
- Updates ink to latest
- Various improvements to the ink player window
- Performance improvements and bug fixes for projects with multiple ink files.
- Easier workflow for manual compilation in-game
- Minor package updates and fixes

## Version 0.9.2
- Updates ink to 0.9.2
- Better tethering for Ink Window
- Minor package updates and fixes

## Version 0.9.1
- Updates ink to 0.9.1
- Minor package updates and fixes

## Version 0.8.3
- Updates ink to 0.8.3
- Minor package updates and fixes

## Version 0.8.2
- Unity 2018 compatibility

## Version 0.8.1
- Updates ink to 0.8.1
- Fixes some rare compilation issues
- Tooltips and other minor features

## Version 0.7.6
- Compatability for Unity 2017.X

## Version 0.7.5
- Updates ink to version 0.7.5
- Option to delay compilation when ink changes are detected in play mode (enabled by default)
- Adds ability to run functions and profile from player window

## Version 0.7.4
- Update Ink to 0.7.4
- Add tooltips to player window
- Improve performance
- Fix compilation issues on larger projects
- Stability fixes
- Don't show compiler shell in windows

## Version 0.7.1
- Update to Unity 5.6
- Reduced the amount of data saved in InkLibrary by storing metadata in EditorPrefs
- Split some parts of InkLibrary into InkSettings ScriptableObject

## Version 0.7.0
- Update Ink to 0.7
- New icon for manually compiled
- Improved ink library editor
- Improved the ease of tethering your game's Story object to the Ink Player Window via an Editor GUI field and improvements to player window