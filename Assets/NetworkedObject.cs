using Openverse.Core;
using Openverse.NetCode;
using RiptideNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Openverse.NetCode.NetworkingCommunications;

public class NetworkedObject : MonoBehaviour
{
    public Guid myID;
    public static Dictionary<Guid, NetworkedObject> NetworkedObjects = new Dictionary<Guid, NetworkedObject>();

    public List<WatchableProperty> watchableProperties = new List<WatchableProperty>();

    public HashSet<Type> networkedPropertyTypes = new HashSet<Type>
    {
        typeof(string),
        typeof(float),
        typeof(int),
        typeof(bool),
        typeof(Vector3),
        typeof(Vector2),
        typeof(Quaternion),
        typeof(Mesh),
        typeof(Material)
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
        createMessage.Add(transform.localScale);
        createMessage.Add(gameObject.name);
        Component[] myComponents = gameObject.GetComponents<Component>();
        int nonNetworkedComps = 0;
        for (int i = 0; i < myComponents.Length; i++)
        {
            if (!AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()) || myComponents[i].GetType() == typeof(Transform))
            {
                nonNetworkedComps++;
            }
        }
        createMessage.Add(myComponents.Length - nonNetworkedComps);
        for(int i = 0; i < myComponents.Length; i++)
        {
            if(AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()) && myComponents[i].GetType() != typeof(Transform))
            {
                createMessage.Add(AllowedComponents.allowedTypesList.IndexOf(myComponents[i].GetType()));
                foreach (var prop in myComponents[i].GetType().GetProperties())
                {
                    if (networkedPropertyTypes.Contains(prop.PropertyType) && prop.CanWrite)
                    {
                        bool success = false;
                        //Couldn't make this a switch but would have loved to do so
                        if (prop.PropertyType == typeof(string))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)0);
                            createMessage.Add((string)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(float))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)1);
                            createMessage.Add((float)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(int))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)2);
                            createMessage.Add((int)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(bool))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)3);
                            createMessage.Add((bool)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Vector2))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)4);
                            createMessage.Add((Vector2)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Vector3))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)5);
                            createMessage.Add((Vector3)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Quaternion))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)6);
                            createMessage.Add((Quaternion)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if(!success)
                        {
                            object value = prop.GetValue(myComponents[i], null);
                            string name = "null";
                            try
                            {
                                name = ((UnityEngine.Object)value).name.Replace(" (Instance)","");
                                bool foundInBundle = false;
                                foreach (UnityEngine.Object obj in Metaserver.Instance.allAssets)
                                {
                                    if (obj.name == name)
                                    {
                                        foundInBundle = true;
                                    }
                                }
                                if(foundInBundle)
                                {
                                    createMessage.Add(true);
                                    createMessage.Add(prop.Name);
                                    createMessage.Add((ushort)7);
                                    createMessage.Add(name);
                                    AddWatchableProperty(myComponents[i], prop);
                                }
                            } catch { }
                        } else
                        {
                            AddWatchableProperty(myComponents[i], prop);
                        }
                    }
                }
                createMessage.Add(false);
            } else
            {
                if(myComponents[i]?.GetType() != typeof(NetworkedObject) && myComponents[i].GetType() != typeof(Transform))
                {
                    Debug.LogWarning("The component of type " + myComponents[i].GetType().Name + " will not be networked to the client!");
                }
            }
        }
        Metaserver.Instance.server.Send(createMessage, p.Id);
    }

    private void AddWatchableProperty(Component comp,PropertyInfo prop)
    {
        watchableProperties.Add(new WatchableProperty(comp.GetType(), prop));
    }
}

public class WatchableProperty
{
    public PropertyInfo property;
    public Type component;
    public bool isWatched;

    public WatchableProperty(Type comp, PropertyInfo prop)
    {
        property = prop;
        component = comp;
    }
}
