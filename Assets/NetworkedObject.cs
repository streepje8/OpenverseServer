using Openverse.Core;
using Openverse.NetCode;
using RiptideNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Openverse.NetCode.NetworkingCommunications;

public class NetworkedObject : MonoBehaviour
{
    public Guid myID;
    public static Dictionary<Guid, NetworkedObject> NetworkedObjects = new Dictionary<Guid, NetworkedObject>();

    public HashSet<Type> networkedPropertyTypes = new HashSet<Type>
    {
        typeof(string),
        typeof(float),
        typeof(int),
        typeof(bool),
        typeof(Vector3),
        typeof(Vector2),
        typeof(Quaternion)
    };

    private void Awake()
    {
        NetworkedObjects.Add(myID,this);
    }

    public void SendtoPlayer(PlayerConnection p)
    {
        myID = Guid.NewGuid();
        Message createMessage = Message.Create(MessageSendMode.reliable, ServerToClientId.spawnObject);
        createMessage.Add(myID.ToString());
        createMessage.Add(transform.position);
        createMessage.Add(transform.rotation);
        createMessage.Add(gameObject.name);
        Component[] myComponents = gameObject.GetComponents<Component>();
        createMessage.Add(myComponents.Length);
        for(int i = 0; i < myComponents.Length; i++)
        {
            if(AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()))
            {
                createMessage.Add(AllowedComponents.allowedTypesList.IndexOf(myComponents[i].GetType()));
                foreach (var prop in myComponents[i].GetType().GetProperties())
                {
                    if (networkedPropertyTypes.Contains(prop.GetType()))
                    {
                        createMessage.Add(true);
                        createMessage.Add(prop.Name);
                        //Couldn't make this a switch but would have loved to do so
                        if (prop.GetType() == typeof(string))
                        {
                            createMessage.Add((ushort)0);
                            createMessage.Add((string)prop.GetValue(myComponents[i], null));
                        }
                        if (prop.GetType() == typeof(float))
                        {
                            createMessage.Add((ushort)1);
                            createMessage.Add((float)prop.GetValue(myComponents[i], null));
                        }
                        if (prop.GetType() == typeof(int))
                        {
                            createMessage.Add((ushort)2);
                            createMessage.Add((int)prop.GetValue(myComponents[i], null));
                        }
                        if (prop.GetType() == typeof(bool))
                        {
                            createMessage.Add((ushort)3);
                            createMessage.Add((bool)prop.GetValue(myComponents[i], null));
                        }
                        if (prop.GetType() == typeof(Vector2))
                        {
                            createMessage.Add((ushort)4);
                            createMessage.Add((Vector2)prop.GetValue(myComponents[i], null));
                        }
                        if (prop.GetType() == typeof(Vector3))
                        {
                            createMessage.Add((ushort)5);
                            createMessage.Add((Vector3)prop.GetValue(myComponents[i], null));
                        }
                        if (prop.GetType() == typeof(Quaternion))
                        {
                            createMessage.Add((ushort)6);
                            createMessage.Add((Quaternion)prop.GetValue(myComponents[i], null));
                        }
                    }
                }
                createMessage.Add(false);
            } else
            {
                if(myComponents[i]?.GetType() != typeof(NetworkedObject))
                {
                    Debug.LogWarning("The component of type " + myComponents[i].name + " will not be networked to the client!");
                }
            }
        }
        Metaserver.Instance.server.Send(createMessage, p.Id);
    }
}
