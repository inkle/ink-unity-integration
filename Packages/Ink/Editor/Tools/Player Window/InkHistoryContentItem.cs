using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;

namespace Ink.UnityIntegration.Debugging {
    [System.Serializable]
    public class InkHistoryContentItem {
        public enum ContentType {
            PresentedContent,
            ChooseChoice,
            PresentedChoice,
            EvaluateFunction,
            CompleteEvaluateFunction,
            ChoosePathString,
            Warning,
            Error,
            DebugNote
        }

        public string content;
        public List<string> tags;
        public ContentType contentType;
        // Creating a datetime from a long is slightly expensive (it can happen many times in a frame). To fix this we cache the result once converted. 
        [SerializeField] JsonDateTime _serializableTime;
        [NonSerialized] bool hasDeserializedTime;
        [NonSerialized] DateTime _time;
        public DateTime time {
            get {
                if (!hasDeserializedTime) {
                    _time = _serializableTime;
                    hasDeserializedTime = true;
                }
                return _time;
            } private set {
                _time = value;
                _serializableTime = value;
            }
        }

        InkHistoryContentItem (string text, ContentType contentType) {
            this.content = text;
            this.contentType = contentType;
            this.time = DateTime.Now;
        }
        InkHistoryContentItem (string text, List<string> tags, ContentType contentType) {
            this.content = text;
            this.tags = tags;
            this.contentType = contentType;
            this.time = DateTime.Now;
        }

        public static InkHistoryContentItem CreateForContent (string choiceText, List<string> tags) {
            return new InkHistoryContentItem(choiceText, tags, InkHistoryContentItem.ContentType.PresentedContent);
        }
        public static InkHistoryContentItem CreateForPresentChoice (Choice choice) {
            return new InkHistoryContentItem(choice.text.Trim(), choice.tags, InkHistoryContentItem.ContentType.PresentedChoice);
        }
        public static InkHistoryContentItem CreateForMakeChoice (Choice choice) {
            return new InkHistoryContentItem(choice.text.Trim(), choice.tags, InkHistoryContentItem.ContentType.ChooseChoice);
        }
        public static InkHistoryContentItem CreateForEvaluateFunction (string functionInfoText) {
            return new InkHistoryContentItem(functionInfoText, InkHistoryContentItem.ContentType.EvaluateFunction);
        }
        public static InkHistoryContentItem CreateForCompleteEvaluateFunction (string functionInfoText) {
            return new InkHistoryContentItem(functionInfoText, InkHistoryContentItem.ContentType.CompleteEvaluateFunction);
        }
        public static InkHistoryContentItem CreateForChoosePathString (string choosePathStringText) {
            return new InkHistoryContentItem(choosePathStringText, InkHistoryContentItem.ContentType.ChoosePathString);
        }
        public static InkHistoryContentItem CreateForWarning (string warningText) {
            return new InkHistoryContentItem(warningText, InkHistoryContentItem.ContentType.Warning);
        }
        public static InkHistoryContentItem CreateForError (string errorText) {
            return new InkHistoryContentItem(errorText, InkHistoryContentItem.ContentType.Error);
        }
        public static InkHistoryContentItem CreateForDebugNote (string noteText) {
            return new InkHistoryContentItem(noteText, InkHistoryContentItem.ContentType.DebugNote);
        }

        struct JsonDateTime {
            public long value;
            public static implicit operator DateTime(JsonDateTime jdt) {
                return DateTime.FromFileTime(jdt.value);
            }
            public static implicit operator JsonDateTime(DateTime dt) {
                JsonDateTime jdt = new JsonDateTime();
                jdt.value = dt.ToFileTime();
                return jdt;
            }
        }
    }
}