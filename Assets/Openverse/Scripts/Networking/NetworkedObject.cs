using HarmonyLib;
using Openverse.Core;
using Openverse.NetCode;
using RiptideNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
                                    PatchProperty(myComponents[i], prop);
                                }
                            } catch { }
                        } else
                        {
                            PatchProperty(myComponents[i], prop);
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

    private void PatchProperty(Component comp,PropertyInfo prop)
    {
        if (Bootloader.Instance == null || Bootloader.Instance.harmony == null)
        {
            Debug.LogError("Harmony is null! Please ensure the bootloader is present in the scene!");
            return;
        }
        if (prop != null && prop.GetSetMethod() != null)
        {
            MethodInfo setmet = prop.GetSetMethod();
            if (setmet.IsDeclaredMember() is true)
                try
                {
                    Bootloader.Instance.harmony.Patch(setmet, transpiler: new HarmonyMethod(GetType().GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static)));
                } catch(Exception e)
                {
                    Debug.LogWarning("Property " + prop.Name + " can not be networked in realtime, network it manually when making changes. Reason: " + e.Message);
                }
        }
    }

    static MethodInfo propertyChangeMethod = SymbolExtensions.GetMethodInfo(() => NetworkedObject.onPropertyChange());

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        yield return new CodeInstruction(OpCodes.Call, propertyChangeMethod);
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
        }
    }

    public static void onPropertyChange()
    {
        Bootloader.Instance.log(Environment.StackTrace);
        Bootloader.Instance.log("OnPropertyChange");
    }
}
