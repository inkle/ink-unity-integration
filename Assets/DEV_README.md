# Dev Readme

This plugin is now designed to be imported as a UPM Package.
Our approach is to point [OpenUPM at a Git Repo](https://openupm.com/packages/com.inklestudios.ink-unity-integration/) with the assets in the Packages folder.
Demos are packaged up as separate .unitypackage files.

## To update create a new release
- Increase the version number in InkLibrary.cs
- Run 'Publishing > Prepare for publishing'
- Commit any changes and push to Master, tagging with the version in the format (x.x.x)
- This causes an Action to trigger, which will create a UPM branch automatically. [Check it succeeded on OpenUPM](https://openupm.com/packages/com.inkle.ink-unity-integration/?subPage=pipelines)
- Draft a new release on GitHub, attaching the .unitypackage that the publish menu item will have output to the repo root.
- The Create .unitypackage step of the publish menu item moves all the files from Packages into Assets, creates a package (also including the Demos folder), and then moves the files back to Packages.