# ink-Unity integration

This Unity package allows you to integrate inkle's [ink narrative scripting language](http://www.inklestudios.com/ink) with Unity, and provides tools to help quickly test your stories in-editor.

Features:

 - **Running ink in game**: Allows usage of JSON-compiled ink files in Unity via the included `InkRuntime` source files (and Json.Net dependency in DLLs).

 - **Auto Compilation**: Instantly creates and updates a JSON story file when a `.ink` is updated.
 	
 - **Ink Player**: Provides a powerful player window for playing and debugging stories.
 	
 - **Inspector tools**: Provides an icon for ink files, and a custom inspector that provides information about a file.


## Getting started

* Download the [latest Unity package release](https://github.com/inkle/ink-unity-integration/releases), and add to your Unity project.
* Select one of the sample `.ink` stories included in the package. In Unity's Inspector window, you should see a *Play* button. Click it to open the **ink player** window, useful for playing (previewing) ink stories.

For more information on **ink**, see [the documentation in the main ink repo](https://github.com/inkle/ink). For convenience, the package also creates (**Help > Ink**) menu options.

## Using ink in game your game. 

The **ink player** is the core feature of this package; the minimal requirements to actually run a compiled JSON story file are files in the `InkRuntime` folder and `Newtonsoft.Json.dll` library.

## Ink Player

The Ink Player (**Ink > Player Window**) allows you to play stories in an editor window, and provides functionality to edit variables on the fly, save and load states, and divert.

**Playing a story**: You can play stories by clicking the "Play" button on the inspector of a master ink file, or by manually choosing a compiled ink story TextAsset in the Ink Player window and clicking "Play". You can then use the choice panel to advance the story, and the undo/redo buttons to rewind and test different paths.

**Saving and restoring story states**: You can save and restore the current state of the story using the save and load buttons in the Story State panel. States are stored as .json files.

**Diverting to a stitch**: To instantly move to a specific stitch in a story, you can manually enter the path of a stitch in the Divert panel. You need to use the full path of a stitch, including the knot that contains it. For example: "the_orient_express.in_first_class".

**Editing variables**: The variables panel allows you to view and edit all the story variables.

## Automatic compilation
	
Ink files must be compiled to JSON before they can be used in-game. 
	
This package provides tools to automate this process when a .ink file is edited. 

**Disabling auto-compilation**: This option exists via **Edit > Project Settings > Ink**

**Manual compilation**: If you have disabled auto-compilation, you can manually compile ink via the inspector of an ink file, using the functions in the InkCompiler class, or rebuild all the ink files using the **Assets > Recompile Ink** menu item (this can be a bit slow for larger projects)

## Inspector Tools

This package also replaces the icon for ink files to make them easier to spot, and populates the inspector for a selected ink file.

**The Inspector**: To replace the inspector for ink files, we've created a system that allows you to provide a custom inspector for any file. If this conflicts with existing behaviour in your project, you can delete the Ink Inspector folder altogether.

## Updating Ink manually

The ink git repo is updated far more frequently than the asset store package. 

If you're interested in keeping up-to-date with cutting edge features, you can download the [latest releases from the GitHub repo](https://github.com/inkle/ink/releases).


## Sublime Text Syntax Highlighting

The Sublime3Syntax zip file in the Extras folder provides syntax highlighting for ink files in Sublime Text 3. Installation details can be found in the readme inside the zip file.

## Customisation

The inklecate DLLs used to compile ink are quite large files. You may safely delete the DLLs not corresponding to your current OS, or both of them if not using the compiler.

## FAQ

* Is the Linux Unity Editor supported?

  *We haven't implemented it, although it should be easy enough by running inklecate.exe with mono. Take a look at `InkCompiler.cs` if you want to add it.*

* I'm getting this error:

        Internal compiler error. See the console log for more information. output was:
        Unhandled Exception: System.TypeLoadException: Could not load type 'Newtonsoft.Json.Linq.JContainer' from assembly 'Newtonsoft.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'.`
 	
 	You need to change your API compatibility level from .NET 2.0 subset to .NET 2.0.

* Other errors?
	
	*First off, recompile the ink library using **Assets > Recompile Ink**.

	If that doesn't work, [report an issue via github](https://github.com/inkle/ink/issues) or [ask us and the comminity for help via hipchat](https://www.hipchat.com/gkq2pSLqU)