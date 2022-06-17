using UnityEngine;

namespace Openverse.ScriptableObjects { 
    public class OpenverseSettings : ScriptableObject
    {
        public GameObject playerPrefab;
        public GameObject connectionPrefab;
        public string serverName;
        public ushort ServerPort;
        public ushort playerLimit;
    }
}
