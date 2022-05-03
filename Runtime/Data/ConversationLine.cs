using System;
using System.Collections.Generic;
using LocalizationExtension;

namespace DialogueGraph.Runtime {
    [Serializable]
    public class ConversationLine {
        public LocalizationReference Message;
        public string Next;
        public string TriggerPort;
        public string CheckPort;
        public List<string> Triggers;
        public List<string> Checks;
        public List<CheckTree> CheckTrees;
    }
}