namespace Openverse.NetCode
{
    using RiptideNetworking;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class ClientMoveable : MonoBehaviour
    {
        public MessageSendMode mode = MessageSendMode.unreliable;
        public string myID = Guid.NewGuid().ToString();
        public bool autoSync = false;

        public static Dictionary<string, ClientMoveable> ClientMoveables = new Dictionary<string, ClientMoveable>();

        [HideInInspector] public Vector3 lastPOS;
        [HideInInspector] public Quaternion lastRot;
        [HideInInspector] public Vector3 lastScale;

        private void Awake()
        {
            ClientMoveables.Add(myID, this);
            lastPOS = transform.position;
            lastRot = transform.rotation;
            lastScale = transform.localScale;
        }
    }
}
