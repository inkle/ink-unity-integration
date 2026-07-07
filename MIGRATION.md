# Upgrading Ink for Unity from 1.x to 2.0

2.0 replaces the old compile pipeline — separate `.ink` source and generated `.json`,
managed by `InkLibrary`/`InkCompiler` — with a Unity **ScriptedImporter**. Each `.ink` now
imports directly into an **`InkFile`** asset that holds the compiled story; there is no
separate `.json`.

Most projects only need steps 1–4. The **Scripting API** section at the end is only for code
that used `InkLibrary`, `InkCompiler`, or the old `InkFile` class.

## 1. Update Unity
The minimum supported version is now **Unity 2022.3 LTS**.

## 2. Update code that loads stories
1.x referenced the generated `.json` as a `TextAsset`:

```csharp
[SerializeField] TextAsset inkJson;
var story = new Story(inkJson.text);
```

2.0 references the `.ink`'s imported `InkFile`:

```csharp
using Ink.UnityIntegration;

[SerializeField] InkFile inkFile;
var story = new Story(inkFile.storyJson);
```

Change the field type wherever you referenced a compiled `.json` (a `TextAsset`) **or** the
`.ink` file itself (a `DefaultAsset`) to `InkFile`, and **reassign it** in your scenes and
prefabs — old references don't carry across a type change.

## 3. Delete the old `.json` files
The compiled JSON now lives inside each `InkFile`, so the committed `.json` files next to your
`.ink` files are unused. While any remain, a **Migrate Ink Project (1.x → 2.0)** button appears
in **Project Settings ▸ Ink** (and in the Ink Update window) — click it to find and delete them
(or delete them by hand). Stop committing them afterwards.

## 4. Master files
Unchanged and automatic: any `.ink` file not `INCLUDE`d by another is a master and compiles to
a runnable story; include-only files compile as part of their master and don't produce their own
errors. To also compile an include on its own (the old "Should also be Master File" option),
select it and tick **Compile As Master File** in its Import Settings.

⚠️ The old **Ink Settings ▸ "include files to compile as master files"** list is **not**
migrated automatically — re-tick **Compile As Master File** per file if you relied on it.

## Scripting API (advanced)
`InkLibrary` and `InkCompiler` are removed, and `InkFile` is now the runtime compiled asset
(not the old editor metadata class). `inkFile.errors` / `.warnings` / `.todos` /
`.unhandledCompileErrors` are unchanged; otherwise:

| 1.x | 2.0 |
|---|---|
| `new Story(jsonTextAsset.text)` | `new Story(inkFile.storyJson)` |
| `InkLibrary.GetInkFileWithFile(asset)` / `…WithPath(path)` / `…WithJSONFile(textAsset)` | `AssetDatabase.LoadAssetAtPath<InkFile>(path)` |
| `InkLibrary.instance.inkLibrary` (all ink files) | `AssetDatabase.FindAssets("t:InkFile")` |
| `InkLibrary.GetMasterInkFiles()` | the above, filtered by `inkFile.isMaster` |
| `InkLibrary.Rebuild()` / `RebuildInkFileConnections()` | not needed — the importer maintains this |
| `inkFile.includes` / `.includesInkFiles` | `InkIncludeGraph.GetDirectIncludes(path)` |
| `inkFile.masterInkFiles` | `InkIncludeGraph.GetIncludedBy(path)` |
| `inkFile.inkAsset` / `.jsonAsset` / `.filePath` | `AssetDatabase.GetAssetPath(inkFile)` |
| `inkFile.lastCompileDate` | `inkFile.compileDate` |
| `InkCompiler.CompileInk(...)` | automatic on import; force with `AssetDatabase.ImportAsset(path)` or `InkEditorUtils.ForceRecompileAllInkFilesSync()` |
| `InkCompiler.OnCompileInk` event | no direct equivalent — react in an `AssetPostprocessor.OnPostprocessAllAssets` to imported `.ink` paths |
| `InkEditorUtils.RebuildLibrary()` / `RecompileAll()` / `RecompileAllImmediately()` | `InkEditorUtils.ForceRecompileAllInkFilesAsync()` / `…Sync()` — scripting only, no menu |
| `InkEditorUtils.DrawStoryPropertyField(...)` | assign an `InkFile` field — its default drawer shows the compile state |
| `InkLibrary.inkVersionCurrent` / `.unityIntegrationVersionCurrent` | `InkEditorUtils.inkVersionCurrent` / `.unityIntegrationVersionCurrent` |
| **Assets ▸ Rebuild Ink Library** menu | Gone — ink recompiles automatically on import. Right-click ▸ **Reimport** if ever needed, or call `InkEditorUtils.ForceRecompileAllInkFiles*()` in scripts. |

Removed **Ink Settings** fields — every file now compiles on import, so these have no
replacement: `compileAllFilesAutomatically`, `filesToCompileAutomatically`,
`defaultJsonAssetPath`, `handleJSONFilesAutomatically`, `delayInPlayMode`, `compileTimeout`.

## Also note
- Errors, warnings and todos show on the `.ink`'s import inspector and in the console; a build
  is blocked if any master file has compile errors.
- Editing an include file reimports the master file(s) that include it, including nested includes.
