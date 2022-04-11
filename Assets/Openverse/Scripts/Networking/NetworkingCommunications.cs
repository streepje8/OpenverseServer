using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Openverse.NetCode
{
    public class NetworkingCommunications : MonoBehaviour
    {
        public enum ServerToClientId : ushort
        {
            downloadWorld = 1,
            openWorld,
            spawnPlayer,
            playerLocation,
            spawnObject,
            updateObject,
            updateVariable,
            transformObject,
            addComponent,
            removeComponent,
            moveClientMoveable
        }
        public enum ClientToServerId : ushort
        {
            playerName = 1,
            vrPositions,
            playerReady,
            moveClientMoveable
        }

    }
}