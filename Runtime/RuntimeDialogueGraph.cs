using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueGraph.Runtime {
    [AddComponentMenu("Dialogue Graph/Dialogue Graph")]
    public class RuntimeDialogueGraph : MonoBehaviour {
        public DlogObject DlogObject;

        #region Inspector Data
        public string CurrentAssetGuid;
        public List<DlogObjectData> PersistentData = new List<DlogObjectData>();
        public StringIntSerializableDictionary PersistentDataIndices = new StringIntSerializableDictionary();
        public DlogObjectData CurrentData {
            get {
                if (string.IsNullOrEmpty(CurrentAssetGuid)) return null;
                if (!PersistentDataIndices.ContainsKey(CurrentAssetGuid)) {
                    PersistentDataIndices[CurrentAssetGuid] = PersistentData.Count;
                    PersistentData.Add(new DlogObjectData());
                }

                return PersistentData[PersistentDataIndices[CurrentAssetGuid]];
            }
        }
        public int CurrentIndex {
            get {
                if (string.IsNullOrEmpty(CurrentAssetGuid)) return -1;
                if (!PersistentDataIndices.ContainsKey(CurrentAssetGuid)) {
                    PersistentDataIndices[CurrentAssetGuid] = PersistentData.Count;
                    PersistentData.Add(new DlogObjectData());
                }

                return PersistentDataIndices[CurrentAssetGuid];
            }
        }
        public void ClearData() {
            PersistentDataIndices = new StringIntSerializableDictionary();
            PersistentData = new List<DlogObjectData>();
        }
        #endregion

        private bool conversationDone;
        private string currentNodeGuid;

        public void ResetConversation() {
            conversationDone = false;
            currentNodeGuid = DlogObject.StartNode;
        }

        public void EndConversation() {
            conversationDone = true;
            currentNodeGuid = null;
        }

        public bool IsConversationDone() {
            return conversationDone;
        }

        public ActorData GetCurrentActor() {
            if (currentNodeGuid == null) return null;

            var currentNode = DlogObject.NodeDictionary[currentNodeGuid];
            if (currentNode.Type != NodeType.DIALOG) return null;
            var currentNodeActorGuid = currentNode.ActorGuid;
            var actor = CurrentData.ActorData[CurrentData.ActorDataIndices[currentNodeActorGuid]];
            return actor;
        }

        public ActorData GetNextActor(int lineIndex)
        {
            if (currentNodeGuid != null)
            {
                var currentNode = DlogObject.NodeDictionary[currentNodeGuid];
                if (currentNode.Type == NodeType.DIALOG)
                {
                    var currentLine = currentNode.Lines[lineIndex];
                    if(currentLine.Next != null)
                    {
                        if(DlogObject.NodeDictionary.TryGetValue(currentLine.Next, out Node nextNode))
                        {
                            var nextActorGuid = nextNode.ActorGuid;
                            if (nextActorGuid != null)
                            {
                                return CurrentData.ActorData[CurrentData.ActorDataIndices[nextActorGuid]];
                            }
                        }
                    }
                }
            }

            return null;
        }

        public List<ConversationLine> GetCurrentLines()
        {
            if (currentNodeGuid == null) return null;
            var currentNode = DlogObject.NodeDictionary[currentNodeGuid];
            return currentNode.Lines;
        }

        public string ProgressDialog(int lineIndex) {
            var lines = GetCurrentLines();
            Progress(lines[lineIndex]);
            ExecuteTriggers(lines[lineIndex], lineIndex);
            return lines[lineIndex].Message;
        }

        private bool ExecuteChecks(ConversationLine line, int lineIndex) {
            bool currentCheck = true;
            foreach (CheckTree tree in line.CheckTrees) {
                currentCheck = EvaluateCheckTree(tree, lineIndex) && currentCheck;
            }

            return currentCheck;
        }

        private void ExecuteTriggers(ConversationLine line, int lineIndex) {
            foreach (var triggerGuid in line.Triggers) {
               CurrentData.TriggerData[CurrentData.TriggerDataIndices[triggerGuid]].Invoke(currentNodeGuid, lineIndex);
            }
        }

        private void Progress(ConversationLine line) {
            if (string.IsNullOrEmpty(line.Next)) {
                conversationDone = true;
                currentNodeGuid = null;
                return;
            }

            currentNodeGuid = line.Next;
        }

        private bool EvaluateCheckTree(CheckTree tree, int lineIndex) {
            if (tree.NodeKind == CheckTree.Kind.Property) {
                if (string.IsNullOrEmpty(tree.PropertyGuid)) return false;
                if (!CurrentData.CheckDataIndices.ContainsKey(tree.PropertyGuid)) return false;
                int index = CurrentData.CheckDataIndices[tree.PropertyGuid];
                if (index < 0 || index >= CurrentData.CheckData.Count) return false;
                return CurrentData.CheckData[index].Invoke(currentNodeGuid, lineIndex);
            }

            if (tree.NodeKind == CheckTree.Kind.Unary) {
                bool check = EvaluateCheckTree(tree.SubtreeA, lineIndex);
                return EvaluateUnaryOperation(tree.BooleanOperation, check);
            }

            if (tree.NodeKind == CheckTree.Kind.Binary) {
                bool checkA = EvaluateCheckTree(tree.SubtreeA, lineIndex);
                bool checkB = EvaluateCheckTree(tree.SubtreeB, lineIndex);
                return EvaluateBinaryOperation(tree.BooleanOperation, checkA, checkB);
            }

            // Unreachable
            throw new Exception("Unreachable");
        }

        private static bool EvaluateUnaryOperation(BooleanOperation operation, bool value) {
            switch (operation) {
                case BooleanOperation.NOT: return !value;
                default: throw new Exception("Unreachable");
            }
        }

        private static bool EvaluateBinaryOperation(BooleanOperation operation, bool valueA, bool valueB) {
            switch (operation) {
                case BooleanOperation.AND: return valueA && valueB;
                case BooleanOperation.OR: return valueA || valueB;
                case BooleanOperation.XOR: return valueA ^ valueB;
                case BooleanOperation.NAND: return !(valueA && valueB);
                case BooleanOperation.NOR: return !(valueA || valueB);
                case BooleanOperation.XNOR: return !(valueA ^ valueB);
                default: throw new Exception("Unreachable");
            }
        }
    }
}