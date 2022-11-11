using UnityEngine;

namespace Openverse.ScriptableObjects { 
    public class OpenverseSettings : ScriptableObject
    {
        public GameObject playerPrefab;
        public GameObject connectionPrefab;
        public GameObject webServerPrefab;
        public string serverName;
        public ushort serverPort;
        public ushort webServerPort;
        public ushort playerLimit;
        public string iconURL = "https://wezzel.nl/openverse/assets/img/openverselogo.png";
        public string serverDescription = "New Openverse Server";
    }
}
