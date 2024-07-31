# Dev Readme

This plugin is now designed to be imported as a UPM Package.
Our approach is to point [OpenUPM at a Git Repo](https://openupm.com/packages/com.inklestudios.ink-unity-integration/) with the assets in the Packages folder.
Demos are packaged up as separate .unitypackage files.

## To update create a new release
- Update CHANGELOG.md with a list of changes (these will then autopopulate)
- Increase the version number in InkLibrary.cs
- Open the Ink Publishing Tools wizard with the 'Publishing/Show Helper Window' menu item
- Click 'Prepare for publishing'. This will run a bunch of automated tasks and produce a .UnityPackage which we'll need later.
- You can click the 'Show Package' button to reveal the packages in Finder
- Commit any changes and push to Master, tagging with the version in the format (x.x.x)
- This causes an Action to trigger, which will create a UPM branch automatically. [Check it succeeded on OpenUPM](https://openupm.com/packages/com.inkle.ink-unity-integration/?subPage=pipelines)
- Click 'Draft GitHub Release'. This will take you to a page with most of the fields auto-populated. Drag the new package into the release and hit Publish.