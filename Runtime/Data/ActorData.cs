using System;
using UnityEngine;
using LocalizationExtension;

namespace DialogueGraph.Runtime {
    [Serializable]
    public class ActorData {
        // public LocalizationReference Name;
        public ScriptableObject CustomData;
        public Property Property;

        public ActorData(ScriptableObject customData, Property property) {
            // Name = name;
            CustomData = customData;
            Property = property;
        }
    }
}