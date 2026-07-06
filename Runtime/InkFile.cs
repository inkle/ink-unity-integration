using System.Collections.Generic;
using UnityEngine;

namespace Ink.UnityIntegration {
    /// <summary>
    /// The imported representation of a .ink file. This is a runtime ScriptableObject
    /// (the main asset produced by InkImporter) holding the compiled story JSON plus any
    /// compiler logs. Reference this from game code and pass storyJson to a new Ink.Runtime.Story.
    /// </summary>
    public class InkFile : ScriptableObject {
        [SerializeField] string _storyJson;
        /// <summary>The compiled ink story as JSON. Pass this to new Ink.Runtime.Story(inkFile.storyJson).</summary>
        public string storyJson => _storyJson;

        /// <summary>True if this file has a compiled story (a master with no fatal errors). Include files
        /// and files that failed to compile return false — check this before creating a Story.</summary>
        public bool isCompiled => !string.IsNullOrEmpty(_storyJson);

        [SerializeField] bool _isMaster;
        /// <summary>True if this file is a master (compiled on its own) rather than only INCLUDEd by others.</summary>
        public bool isMaster => _isMaster;

        [SerializeField] long _compileDateTicks;
        /// <summary>When this file was last compiled (set at import), or null if it hasn't been compiled.</summary>
        public System.DateTime? compileDate => _compileDateTicks > 0 ? new System.DateTime(_compileDateTicks) : (System.DateTime?)null;

        // Compiler diagnostics captured during import.
        public List<InkCompilerLog> errors = new List<InkCompilerLog>();
        public List<InkCompilerLog> warnings = new List<InkCompilerLog>();
        public List<InkCompilerLog> todos = new List<InkCompilerLog>();
        // Fatal, unexpected compiler failures (exceptions) — i.e. likely compiler bugs, not ink script errors.
        public List<string> unhandledCompileErrors = new List<string>();

        public bool hasErrors => errors.Count > 0;
        public bool hasWarnings => warnings.Count > 0;
        public bool hasTodos => todos.Count > 0;
        public bool hasUnhandledCompileErrors => unhandledCompileErrors.Count > 0;

        /// <summary>
        /// Populated by InkImporter during import. Not intended to be called elsewhere.
        /// </summary>
        public void SetStoryJson (string storyJson) {
            _storyJson = storyJson;
        }

        /// <summary>Populated by InkImporter during import. Not intended to be called elsewhere.</summary>
        public void SetIsMaster (bool isMaster) {
            _isMaster = isMaster;
        }

        /// <summary>Populated by InkImporter during import. Not intended to be called elsewhere.</summary>
        public void SetCompileDate (System.DateTime date) {
            _compileDateTicks = date.Ticks;
        }

        public override string ToString () => $"[InkFile: name={name}]";
    }
}
