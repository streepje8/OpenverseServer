namespace Openverse.Core
{
    using Openverse.Input;
    using Openverse.NetCode;
    using RiptideNetworking;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class VirtualPlayer : MonoBehaviour
    {
        public struct Tuple<T1, T2>
        {
            public readonly T1 Item1;
            public readonly T2 Item2;
            public Tuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
        }

        public PlayerConnection connection;
        public GameObject head;
        public GameObject handLeft;
        public GameObject handRight;

        private Dictionary<Tuple<Type, string>, Action<InputValue>> lookupTable = new Dictionary<Tuple<Type, string>, Action<InputValue>>();
        private Dictionary<Type, short> typeToId = new Dictionary<Type, short>() {
        {typeof(bool), 0},
        {typeof(int), 1},
        {typeof(float), 2},
        {typeof(Vector2), 3},
        {typeof(Quaternion), 4}
    };
        private Dictionary<Tuple<Type, string>, Message> packetQueue = new Dictionary<Tuple<Type, string>, Message>();

        public void AddInputListner<T>(string name, Action<InputValue> callback)
        {
            lookupTable.Add(new Tuple<Type, string>(typeof(T), name), callback);
        }

        public void RequestInput<T>(string name)
        {
            if (typeToId.ContainsKey(typeof(T)))
            {
                Message request = Message.Create(MessageSendMode.unreliable, ServerToClientId.RequestInput);
                request.Add(typeToId[typeof(T)]);
                request.Add(name);
                Tuple<Type, string> packetKey = new Tuple<Type, string>(typeof(T), name);
                if (!packetQueue.ContainsKey(packetKey))
                { //prevent duplicate packets per tick
                    packetQueue.Add(packetKey, request);
                }
            }
        }

        private void FixedUpdate()
        {
            foreach (KeyValuePair<Tuple<Type, string>, Message> kvp in packetQueue)
            {
                Metaserver.Instance.server.Send(kvp.Value, connection.Id);
            }
            packetQueue = new Dictionary<Tuple<Type, string>, Message>();
        }

        internal void InputRecieved(Message packet)
        {
            switch (packet.GetShort())
            {
                case 0:
                    Tuple<Type, string> key = new Tuple<Type, string>(typeof(bool), packet.GetString());
                    if (lookupTable.ContainsKey(key))
                    {
                        lookupTable[key].Invoke(new InputValue(packet.GetBool()));
                    }
                    break;
                case 1:
                    key = new Tuple<Type, string>(typeof(int), packet.GetString());
                    if (lookupTable.ContainsKey(key))
                    {
                        lookupTable[key].Invoke(new InputValue(packet.GetInt()));
                    }
                    break;
                case 2:
                    key = new Tuple<Type, string>(typeof(float), packet.GetString());
                    if (lookupTable.ContainsKey(key))
                    {
                        lookupTable[key].Invoke(new InputValue(packet.GetFloat()));
                    }
                    break;
                case 3:
                    key = new Tuple<Type, string>(typeof(Vector2), packet.GetString());
                    if (lookupTable.ContainsKey(key))
                    {
                        lookupTable[key].Invoke(new InputValue(packet.GetVector2()));
                    }
                    break;
                case 4:
                    key = new Tuple<Type, string>(typeof(Quaternion), packet.GetString());
                    if (lookupTable.ContainsKey(key))
                    {
                        lookupTable[key].Invoke(new InputValue(packet.GetQuaternion()));
                    }
                    break;
            }

        }
    }
}

namespace Openverse.Input
{
    using System;
    public struct InputValue
    {
        public object value;
        public InputValue(object value)
        {
            this.value = value;
        }

        public T Get<T>()
        {
            if (typeof(T) == value.GetType())
                return (T)value;
            throw new Exception("InputValue is not of type: " + typeof(T).Name);
        }
    }
}