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

## VersionAdded in 0.7.0
- Update Ink to 0.7
- New icon for manually compiled
- Improved ink library editor
- Improved the ease of tethering your game's Story object to the Ink Player Window via an Editor GUI field and improvements to player window