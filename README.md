# ink-Unity integration

This Unity package allows you to integrate inkle's [ink narrative scripting language](http://www.inklestudios.com/ink) with Unity and provides tools to **compile**, **play** and **debug** your stories.

# Overview

 - **Using ink in your game**: Allows running and controlling ink files in Unity via the [C# runtime API](https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md).
  
 - **ink player**: Provides a powerful [Ink Player Window](https://github.com/inkle/ink-unity-integration/blob/master/Documentation/InkPlayerWindow.md) for playing and debugging stories.
 
 - **Automatic compilation**: `.ink` files are imported by Unity as an `InkFile` asset and compiled automatically.
  
 - **Inspector tools**: Provides an icon for ink files, and a custom inspector that provides information about a file.

# Getting started

## :inbox_tray: Installation
The recommended way to install ink is as a UPM package via Unity's Package Manager. A few other options are listed below.

### :star:Via the Package Manager:star:
* Open the Package Manager (**Window > Package Manager**), click the **+** button and choose **Install package from git URL…**
* Enter `https://github.com/inkle/ink-unity-integration.git#upm`
* It installs at Packages > Ink Unity Integration. Demos can be imported from Packages > Ink Unity Integration > Demos.

### Via OpenUPM
If you'd like the Package Manager to notify you when a new version is available, install through [OpenUPM](https://openupm.com/packages/com.inkle.ink-unity-integration/) instead. It distributes the same package through a scoped registry — follow the instructions there.

### As a .UnityPackage
Best if you want to edit the source, or don't have git installed. This imports the source into your Assets folder.
* [Download the latest .UnityPackage](https://github.com/inkle/ink-unity-integration/releases).
* Open the downloaded file to import it into your Unity project.

### From GitHub
Clone or fork the [GitHub repo](https://github.com/inkle/ink-unity-integration) if you want to work from or contribute to the source. To just install the latest version, use the git URL as described in [Via the Package Manager](#via-the-package-manager) above; to embed an editable copy, move the `Packages/com.inkle.ink-unity-integration` folder into your own project's `Packages` folder.

### Via the Asset Store
For convenience a .UnityPackage is hosted at the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/ink-unity-integration-60055). **This version is updated rarely, and so is not recommended.**



## :video_game: Demos
This project includes a demo scene, providing a simple example of how to control an ink story with C# code using Unity UI.

(If you imported this package as a UPM, then you must first import the demos from Packages > Ink Unity Integration > Demos)

To run a demo, double-click the scene file at the root of the demo folder to open it, and press the Play button at the top of the screen to start it.

## :page_facing_up: C# API
The C# API provides all you need to control ink stories in code; advancing your story, making choices, diverting to knots, saving and loading, and much more.

[It is documented in the main ink repo](https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md#getting-started-with-the-runtime-api).

For convenience, the package also creates an (**Help > Ink > API Documentation**) menu option.

## :pencil2: Writing ink
For more information on writing with **ink**, see [the documentation in the main ink repo](https://github.com/inkle/ink). 

For convenience, the package also creates an (**Help > Ink > Writing Tutorial**) menu option.


## :question: Further Help
For assistance with writing or code, [Inkle's Discord forum](https://discord.gg/tD8Am2K) is full of lovely people who can help you out!

To keep up to date with the latest news about ink [sign up for the mailing list](http://www.inklestudios.com/ink#signup).


# Features

## Compilation
  
`.ink` files are compiled to a runnable story automatically by a Unity **ScriptedImporter**, whenever you create or edit one (or any file it `INCLUDE`s). There's no separate compile step and no `.json` file to manage — the compiled story is stored in the imported **`InkFile`** asset. Read it with `inkFile.storyJson` and pass it to `new Ink.Runtime.Story(...)`.

Master files (any `.ink` not `INCLUDE`d by another file) compile to a story; include files compile as part of their master. To also compile an include on its own, tick **Compile As Master File** in its import settings.

**Compiling at runtime**: the ink compiler ships in builds (part of the `Ink-Libraries` assembly), so you can compile `.ink` in-game if you need to. If you don't, see [WebGL best practices](#WebGLBestPractices) to trim it.


## <a name="InkPlayerWindow"></a>Ink Player Window

The Ink Player Window (**Window > Ink Player**) allows you to play stories in an editor window, and provides functionality to edit variables on the fly, test functions, profile performance, save and load states, and divert.

To play a story, click **Play in Ink Player** on an ink file's inspector, or assign an `InkFile` in the Ink Player window.

**Editor Attaching**: Attaching the InkStory instance used by your game to the Ink Player window allows you to view and edit your story as it runs in game. 

See BasicInkExampleEditor.cs in the Basic Demo for an example of how to:
* Show an attach/detach button on an inspector
* Automatically attach on entering play mode

[More information on using and extending Ink Player Window](https://github.com/inkle/ink-unity-integration/blob/master/Documentation/InkPlayerWindow.md)


## Inspector

Selecting an ink file shows any compile errors, warnings and TODOs, and maps out its include hierarchy: whether it's a master or an include file, and the files it includes and is included by.


# <a name="WebGLBestPractices"></a>WebGL best practices

WebGL builds should be as small as possible. The ink compiler is bundled with the runtime in the `Ink-Libraries` assembly, so it ships in builds even though most games only use it at edit time. If build size matters and you never compile ink at runtime, you can split the compiler (`InkLibs/InkCompiler`) into its own editor-only assembly definition so it's stripped from builds — you'll also need the editor assembly to reference that new assembly. The runtime (`InkLibs/InkRuntime`) itself never needs the compiler.


# FAQ

* Is the Linux Unity Editor supported?

  *Yes!*

* What versions of Unity are supported?

  Ink for Unity 2.0 supports **Unity 2022.3 LTS and above**. (The 1.x line supports 2020 LTS and above.)

# Support us! :heart:

Ink is free, forever; but we'd really appreciate your support!
If you're able to give back, generous donations at our [Patreon](https://www.patreon.com/inkle) mean the world to us. 

# Discord:

Looking for help or want to meet likeminded writers/developers? Come say hello on our [Discord](https://discord.gg/inkle) server! 

# License

**ink** and this package is released under the MIT license. Although we don't require attribution, we'd love to know if you decide to use **ink** a project! Let us know on [Twitter](http://www.twitter.com/inkleStudios) or [by email](mailto:info@inklestudios.com).
View the full licence [Here](https://github.com/inkle/ink-unity-integration/blob/master/LICENCE.md)
