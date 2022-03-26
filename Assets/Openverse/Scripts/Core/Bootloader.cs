using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Bootloader : Singleton<Bootloader>
{
    public Harmony harmony;
    private Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
    private Dictionary<string, NetworkedObject> networkedObjects = new Dictionary<string, NetworkedObject>();

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;
        harmony = new Harmony("com.streep.openversepatch");
        harmony.PatchAll();
    }

    public void log(string v)
    {
        Debug.Log(v);
    }

    public string GetNetworkedObjectID(NetworkedObject networkedObject)
    {
        string result = Guid.NewGuid().ToString();
        if(!networkedObjects.ContainsValue(networkedObject))
        {
            networkedObjects.Add(result, networkedObject);
        } else
        {
            foreach(KeyValuePair<string, NetworkedObject> kvp in networkedObjects)
            {
                if(kvp.Value == networkedObject)
                {
                    result = kvp.Key;
                }
            }
        }
        return result;
    }

    public NetworkedObject GetNetworkedObject(string guid)
    {
        if(networkedObjects.ContainsKey(guid))
        {
            return networkedObjects[guid];
        } else
        {
            return null;
        }
    }

    public string GetPropertyID(PropertyInfo Property)
    {
        string result = Guid.NewGuid().ToString();
        if (!properties.ContainsValue(Property))
        {
            properties.Add(result, Property);
        }
        else
        {
            foreach (KeyValuePair<string, PropertyInfo> kvp in properties)
            {
                if (kvp.Value == Property)
                {
                    result = kvp.Key;
                }
            }
        }
        return result;
    }

    public PropertyInfo GetProperty(string guid)
    {
        if (properties.ContainsKey(guid))
        {
            return properties[guid];
        }
        else
        {
            return null;
        }
    }
}
