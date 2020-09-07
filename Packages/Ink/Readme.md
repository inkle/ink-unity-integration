# ink-Unity integration

This Unity package allows you to integrate inkle's [ink narrative scripting language](http://www.inklestudios.com/ink) with Unity, and provides tools to help quickly test your stories in-editor.

Features:

 - **Running ink in game**: Allows usage of JSON-compiled ink files in Unity via the included `ink-engine.dll`.

 - **Auto compilation**: Instantly creates and updates a JSON story file when a `.ink` script is updated. Errors in your `.ink` script are displayed as errors in Unity's own console.
 	
 - **Ink player**: Provides a powerful player window for playing and debugging stories.
 	
 - **Inspector tools**: Provides an icon for ink files, and a custom inspector that provides information about a file.


## Getting started

* Download the [latest Unity package release](https://github.com/inkle/ink-unity-integration/releases), and add to your Unity project.
* Select one of the sample `.ink` stories included in the package. In Unity's Inspector window, you should see a *Play* button. Click it to open the **ink player** window, useful for playing (previewing) ink stories.

For more information on **ink**, see [the documentation in the main ink repo](https://github.com/inkle/ink). For convenience, the package also creates an (**Ink > Help**) menu option.

For assistance with writing or code, [Inkle's Discord forum](https://discord.gg/tD8Am2K) is full of lovely people who can help you out!

To keep up to date with the latest news about ink [sign up for the mailing list](http://www.inklestudios.com/ink#signup).

## Customisation

This package has been designed to be modular. You can delete many of the folders to remove functionality without causing errors.

You should never delete the DLL folder (although you may delete the windows/mac dlls if you are sure no team members are on a certain platform)

## Using ink in game your game. 

The **ink player** is the core feature of this package; the minimal requirements to actually run a compiled JSON story file is the source code in the InkRuntime folder. 

Alternatively, the `ink-engine-runtime.dll` file can be used instead of the source code in the InkRuntime folder. It may otherwise be safely deleted.

## Ink Player

The Ink Player (**Ink > Player Window**) allows you to play stories in an editor window, and provides functionality to edit variables on the fly, test functions, profile, save and load states, and divert.

To play a story, click the "play" button shown on the inspector of a compiled ink file, or drag a compiled ink story TextAsset into the window.

**Editor Attaching**: Attach the InkStory instance used by your game to the Ink Player window allows you to view and edit your story as it runs in game. 

See BasicInkExampleEditor.cs in the Examples folder for an example of how to:
* Show an attach/detach button on an inspector
* Automatically attach on entering play mode


## Automatic compilation
	
Ink files must be compiled to JSON before they can be used in-game. 
	
This package provides tools to automate this process when a .ink file is edited. 

**Disabling auto-compilation**: You might want to have manual control over ink compilation. If this is the case, you can disable "Compile ink automatically" in the InkSettings file or delete the InkPostProcessor class.

**Manual compilation**: If you have disabled auto-compilation, you can manually compile ink using the **Assets > Recompile Ink** menu item, via the inspector of an ink file, or using the functions in the InkCompiler class which execute the files in the InkCompiler folder.
Currently compilation is Editor-only, although it's a very solvable problem if you need the functionality.

**Play mode delay**: By default, ink does not compile while in play mode. This can be disabled in the InkSettings file.

## Inspector Tools

This package also replaces the icon for ink files to make them easier to spot, and populates the inspector for a selected ink file.

**The Inspector**: To replace the inspector for ink files, we've created a system that allows you to provide a custom inspector for any file. If this conflicts with existing behaviour in your project, you can delete the Ink Inspector folder altogether.

## Updating Ink manually

The ink git repo is updated far more frequently than this asset store package. 

If you're interested in keeping up-to-date with cutting edge features, you can download the [latest releases from the GitHub repo](https://github.com/inkle/ink/releases).

## Plugins

There's unofficial support for PlayMaker at this link. 
https://github.com/inkle/ink-unity-integration/issues/22
We'd love to see this supported more if you'd like to assist the effort!

## FAQ

* Is the Linux Unity Editor supported?

  *Yes!*

# License

**ink** and this package is released under the MIT license. Although we don't require attribution, we'd love to know if you decide to use **ink** a project! Let us know on [Twitter](http://www.twitter.com/inkleStudios) or [by email](mailto:info@inklestudios.com).

[Newtonsoft's Json.NET](http://www.newtonsoft.com/json) is included, and also has the MIT License.

### The MIT License (MIT)
Copyright (c) 2016 inkle Ltd.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
