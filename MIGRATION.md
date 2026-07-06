# Migrating to Ink for Unity 2.0

Version 2.0 replaces the old compilation system (separate `.ink` source and generated
`.json` files, managed by `InkLibrary`/`InkCompiler`) with a Unity **ScriptedImporter**.
Each `.ink` file now imports directly into an **`InkFile`** asset that holds the compiled
story. There is no longer a separate `.json` file.

This is a breaking change. Here's how to upgrade an existing project.

## 1. Update Unity
The minimum supported version is now **Unity 2022.3 LTS**. Open your project in 2022.3+.

## 2. Master files (automatic — no action needed)
As in 1.x, master files are detected automatically: any `.ink` file that isn't `INCLUDE`d by
another file is a master and is compiled into a runnable story. Files that are only `INCLUDE`d
are compiled as part of their master and no longer produce their own errors.

If you need to compile an include file on its own as well (the old "Should also be Master File"
option), select it and tick **Compile As Master File** in its Import Settings.

## 3. Update your code and references
Game code previously referenced the generated `.json` as a `TextAsset`:

```csharp
[SerializeField] TextAsset inkJsonAsset;
var story = new Story(inkJsonAsset.text);
```

Now reference the `.ink`'s imported `InkFile` directly:

```csharp
using Ink.UnityIntegration;

[SerializeField] InkFile inkFileAsset;
var story = new Story(inkFileAsset.storyJson);
```

Reassign the field in any scenes/prefabs (the old `TextAsset` reference won't carry over to
the new field type).

## 4. Delete the old generated `.json` files
The compiled JSON now lives inside the `InkFile` import artifact, so the committed `.json`
files next to your `.ink` files are no longer used. While any remain, a **Migrate Ink Project
from 1.x** button appears in **Project Settings ▸ Ink** (and in the Ink Update window) — click it
to find and delete them (or delete them by hand). Stop committing them afterwards.

## Notes
- Compiler errors/warnings/todos now show on the `.ink` file's import inspector and in the
  console. A build is blocked if any master file has compile errors.
- Editing an include file automatically reimports the master file(s) that include it,
  including nested includes.
- The "Rebuild Ink Library" menu is gone. Use **Assets ▸ Recompile All Ink Files (Async/Sync)**
  if you ever need to force a full reimport (the Sync variant is suitable for build scripts).
