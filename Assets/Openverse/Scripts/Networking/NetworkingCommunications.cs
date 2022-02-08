using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Openverse.NetCode
{
    public class NetworkingCommunications : MonoBehaviour
    {
        public enum ServerToClientId : ushort
        {
            spawnPlayer = 1,
            playerLocation,
            MetaContent
        }
        public enum ClientToServerId : ushort
        {
            playerName = 1
        }

    }
}