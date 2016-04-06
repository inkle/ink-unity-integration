This package allows you to integrate the ink narrative scripting language with Unity, and provides tools to help quickly test your stories in-editor.

 - Use Ink in your game
 	Allows usage of JSON-compiled ink files in Unity via the ink-engine dll.
 - Auto Compilation
 	Instantly creates and updates a JSON story file when a .ink is updated.
 - Ink Player
 	Provides a powerful player window for playing and debugging stories.
 - Inspector tools
 	Provides an icon for ink files, and a custom inspector that provides information about a file.


Getting started
 - For beginners to Ink, we recommend first testing out the example stories provided in the Ink Player Window.


Using ink in game your game. 
	The ink player is the core feature of this package; a minimal version might only contain the ink-engine.dll and Newtonsoft.Json.dll libraries.
	Documentation for using ink can be found at the link below, or via the Ink > Help menu item.
	https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md


Ink Player
	The Ink Player (Ink > Player Window) allows you to play stories in an editor window, and provides functionality to edit variables on the fly, save and load states, and divert.

	To play a story, drag in a compiled ink story TextAsset


Ink > JSON Compilation
	Ink files must be compiled to JSON before they can be used in-game. 
	This package provides tools to automate this process when a .ink file is edited. 

	Disabling auto-compilation
		You might want to have manual control over ink compilation. If this is the case, you can safely delete the InkPostProcessor class.

	Manual compilation
		If you have disabled auto-compilation, you can manually compile ink using the Ink > Compile All menu item, via the inspector of an ink file, or using the functions in the InkCompiler class.


Inspector Tools
	This package also replaces the icon for ink files to make them easier to spot, and populates the inspector for a selected ink file.

	The Inspector
		To replace the inspector for ink files, we've created a system that allows you to provide a custom inspector for any file. If this conflicts with existing behaviour in your project, you can delete the Ink Inspector folder altogether.


Updating Ink manually
	The ink git repo is updated far more frequently than this asset store package. 
	If you're interested in keeping up-to-date with cutting edge features, you can follow the git repo here.
	If you want to update ink in your game XXX



FAQ
	Is the Linux Unity Editor supported?
		Not officially, although it should be possible by running inklecate via Mono and adding an ifdef in XXX; you savvy techies are more than welcome to try.

	I'm getting this error - "Internal compiler error. See the console log for more information. output was:
 Unhandled Exception: System.TypeLoadException: Could not load type 'Newtonsoft.Json.Linq.JContainer' from assembly 'Newtonsoft.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'."
 		You need to change your API compatibility level from .NET 2.0 subset to .NET 2.0.



The package has been designed to be modular. You can delete many of the folders to remove functionality without causing errors.
You should never delete the DLL folder (although you may delete the windows/mac dlls if you are sure no team members are on a certain platform)
