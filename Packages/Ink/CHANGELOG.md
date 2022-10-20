# CHANGELOG.md

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
- Resolves some compilation issues.

## Version 0.9.60:
- Moved InkLibrary and InkSettings from Assets into Library and ProjectSettings.
   - InkLibrary should no longer be tracked in source control.
   - Changes to InkSettings must be migrated manually.
   - The InkLibrary and InkSettings files in your project folder should be deleted.
- Added a divertable list of knots, stitches and other named content to the Ink Editor Window, replacing the Diverts subpanel.
