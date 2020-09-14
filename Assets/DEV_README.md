# Dev Readme

This plugin is now designed to be imported as a UPM Package.
Our approach is to point [OpenUPM at a Git Repo](https://openupm.com/packages/com.inklestudios.ink-unity-integration/) with the assets in the Packages folder.
Demos are packaged up as separate .unitypackage files.

## To update the UPM
- (optional) Increase the version number in InkLibrary.cs
- Commit any changes and push to Master.
- Draft a new release. (You may also want to add a .unitypackage to the release)

### Exporting a .unitypackage
- Run Publishing > Create .unitypackage
This moves all the files from Packages into Assets, creates a package (also including the Demos folder), and then moves the files back to Packages.